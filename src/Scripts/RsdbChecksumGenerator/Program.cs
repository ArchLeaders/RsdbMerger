using BymlLibrary;
using BymlLibrary.Nodes.Containers;
using CommunityToolkit.HighPerformance;
using Revrs;
using Revrs.Buffers;
using RsdbChecksumGenerator.Models;
using System.IO.Hashing;
using System.Runtime.InteropServices;
using TotkCommon;
using TotkCommon.Extensions;

Dictionary<ulong, RsdbCache> rsdbCache = [];

foreach (string romfs in args[2..]) {
    string rsdbFolder = Path.Combine(romfs, "RSDB");
    int version = romfs.GetRomfsVersionOrDefault(100);
    foreach (string rsdbFile in Directory.EnumerateFiles(rsdbFolder)) {
        CacheRsdb(romfs, rsdbFile, version, rsdbCache);
    }
}

foreach (var rsdbHash in rsdbCache.Keys.ToArray()) {
    RsdbCache cache = rsdbCache[rsdbHash];
    foreach (ulong rowId in cache.Keys.ToArray()) {
        List<(Byml Node, int Version, int Count, int HashCode)> entries = cache[rowId];
        if (entries.Count < 2) {
            cache.Remove(rowId);
        }
    }
}

using FileStream fs = File.Create(args[0]);
fs.Write(rsdbCache.Count);

using FileStream fsi = File.Create(args[1]);
fsi.Write(rsdbCache.Count);

foreach (var (rsdbNameHash, cache) in rsdbCache) {
    fsi.Write(rsdbNameHash);
    fsi.Write(cache.IndexMapping.Count);
    for (int i = 0; i < cache.IndexMapping.Count; i++) {
        fsi.Write(cache.IndexMapping[i]);
        fsi.Write(i);
    }

    if (rsdbCache.Count == 0) {
        continue;
    }

    fs.Write(rsdbNameHash);
    fs.Write(cache.Count);

    foreach (var (rowIdHash, entries) in cache) {
        fs.Write(rowIdHash);
        fs.Write(entries.Count);
        foreach (var (row, version, _, _) in entries) {
            fs.Write(version);

            MemoryStream ms = new();
            row.WriteBinary(ms, Endianness.Little);
            fs.Write(Convert.ToInt32(ms.Length));
            ms.Seek(0, SeekOrigin.Begin);
            ms.CopyTo(fs);
        }
    }
}


static void CacheRsdb(string romfs, string target, int version, Dictionary<ulong, RsdbCache> rsdbCache)
{
    ReadOnlySpan<char> canonical = target.ToCanonical(romfs);
    if (GetId(canonical) is not string rowId) {
        return;
    }

    ulong hash = XxHash3.HashToUInt64(MemoryMarshal.Cast<char, byte>(canonical));

    if (!rsdbCache.TryGetValue(hash, out RsdbCache? cache)) {
        rsdbCache[hash] = cache = [];
    }

    using FileStream fs = File.OpenRead(target);
    int size = Convert.ToInt32(fs.Length);
    using ArraySegmentOwner<byte> data = ArraySegmentOwner<byte>.Allocate(size);
    fs.Read(data.Segment);

    if (Zstd.IsCompressed(data.Segment)) {
        using ArraySegmentOwner<byte> decompressed = ArraySegmentOwner<byte>.Allocate(Zstd.GetDecompressedSize(data.Segment));
        Totk.Zstd.Decompress(data.Segment, decompressed.Segment);
        CacheEntries(canonical, decompressed.Segment, rowId, version, cache);
        return;
    }

    CacheEntries(canonical, data.Segment, rowId, version, cache);
}

static void CacheEntries(ReadOnlySpan<char> canonical, ArraySegment<byte> data, string rowId, int version, RsdbCache cache)
{
    Byml byml = Byml.FromBinary(data);

    foreach (Byml row in byml.GetArray()) {
        BymlMap map = row.GetMap();
        ulong hash = rowId switch {
            "NameHash" => map[rowId].GetUInt32(),
            _ => XxHash3.HashToUInt64(MemoryMarshal.Cast<char, byte>(map[rowId].GetString()))
        };

        if (!cache.IsIndexMappingFilled) {
            cache.IndexMapping.Add(hash);
        }

        int hashCode = Byml.ValueEqualityComparer.Default.GetHashCode(row);

        if (!cache.TryGetValue(hash, out var entries)) {
            cache[hash] = entries = [
                (row, version, map.Count, hashCode)
            ];

            continue;
        }

        var (lastNode, _, _, lastHashCode) = entries[^1];

        if (Byml.ValueEqualityComparer.Default.Equals(lastNode, row)) {
            continue;
        }

        if (lastHashCode == hashCode) {
            string id = rowId switch {
                "NameHash" => map[rowId].GetUInt32().ToString(),
                _ => map[rowId].GetString()
            };

            throw new InvalidDataException($"""
                Hash collision in '{canonical}' id '{id}'
                """);
        }

        entries.Add(
            (row, version, map.Count, hashCode)
        );
    }

    cache.IsIndexMappingFilled = true;
}

static string? GetId(ReadOnlySpan<char> canonical)
{
    return canonical switch {
        "RSDB/GameSafetySetting.Product.rstbl.byml" => "NameHash",
        "RSDB/RumbleCall.Product.rstbl.byml" or
        "RSDB/UIScreen.Product.rstbl.byml" => "Name",
        "RSDB/TagDef.Product.rstbl.byml" => "FullTagId",
        "RSDB/ActorInfo.Product.rstbl.byml" or
        "RSDB/AttachmentActorInfo.Product.rstbl.byml" or
        "RSDB/Challenge.Product.rstbl.byml" or
        "RSDB/EnhancementMaterialInfo.Product.rstbl.byml" or
        "RSDB/EventPlayEnvSetting.Product.rstbl.byml" or
        "RSDB/EventSetting.Product.rstbl.byml" or
        "RSDB/GameActorInfo.Product.rstbl.byml" or
        "RSDB/GameAnalyzedEventInfo.Product.rstbl.byml" or
        "RSDB/GameEventBaseSetting.Product.rstbl.byml" or
        "RSDB/GameEventMetadata.Product.rstbl.byml" or
        "RSDB/LoadingTips.Product.rstbl.byml" or
        "RSDB/Location.Product.rstbl.byml" or
        "RSDB/LocatorData.Product.rstbl.byml" or
        "RSDB/PouchActorInfo.Product.rstbl.byml" or
        "RSDB/XLinkPropertyTable.Product.rstbl.byml" or
        "RSDB/XLinkPropertyTableList.Product.rstbl.byml" => "__RowId",
        _ => null
    };
}