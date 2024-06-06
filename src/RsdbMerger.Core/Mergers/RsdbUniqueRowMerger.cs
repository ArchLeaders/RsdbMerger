using BymlLibrary;
using BymlLibrary.Nodes.Containers;
using Revrs;
using RsdbMerger.Core.Components;
using RsdbMerger.Core.Models;
using RsdbMerger.Core.Services;
using System.IO.Hashing;
using System.Runtime.InteropServices;
using TotkCommon;

namespace RsdbMerger.Core.Mergers;

/// <summary>
/// Merges array based RSDB files using the a unique id.
/// </summary>
public class RsdbUniqueRowMerger(string idKey, Func<BymlMap, ulong> getRowIdHash) : IRsdbMerger
{
    private readonly string _idKey = idKey;
    private readonly Func<BymlMap, ulong> _getRowIdHash = getRowIdHash;
    private readonly RsdbRowComparer _rowComparer = new(idKey);

    public bool CreateChangelog(ReadOnlySpan<char> canonical, ArraySegment<byte> data, RsdbFile target, Stream output)
    {
        BymlArray vanillaRows = target.OpenVanilla(out _, out _).GetArray();
        ulong rsdbNameHash = XxHash3.HashToUInt64(MemoryMarshal.Cast<char, byte>(canonical));

        RevrsReader reader = new(data);
        ImmutableByml byml = new(ref reader);
        Endianness endianness = byml.Endianness;
        ushort bymlVersion = byml.Header.Version;

        Byml root = Byml.FromImmutable(byml);
        BymlArray array = root.GetArray();

        for (int i = 0; i < array.Count; i++) {
            bool isVanillaRow = LogRowChanges(vanillaRows, array[i], rsdbNameHash, target.Version);

            if (isVanillaRow) {
                array.RemoveAt(i);
                i--;
            }
        }

        if (array.Count == 0) {
            return false;
        }

        root.WriteBinary(output, endianness, bymlVersion);
        return true;
    }

    private bool LogRowChanges(BymlArray vanillaRows, Byml row, ulong rsdbNameHash, int version)
    {
        BymlMap map = row.GetMap();
        ulong rowId = _getRowIdHash(map);
        int vanillaIndex = RsdbIndexMappingService.GetIndex(rsdbNameHash, rowId);

        if (vanillaIndex < 0) {
            return false;
        }

        if (RsdbOverflowService.TryGetVanilla(rsdbNameHash, rowId, version) is not Byml vanilla) {
            vanilla = vanillaRows[vanillaIndex];
        }

        if (!BymlChangelogService.LogChanges(ref row, vanilla)) {
            map[_idKey] = rowId;
            return false;
        }

        return true;
    }

    public void Merge(ReadOnlySpan<char> canonical, IEnumerable<ArraySegment<byte>> targets, RsdbFile target, Stream output)
    {
        Byml vanillaRoot = target.OpenVanilla(out Endianness endianness, out ushort version, extension: ".zs");
        BymlArray vanillaRows = vanillaRoot.GetArray();
        ulong rsdbNameHash = XxHash3.HashToUInt64(MemoryMarshal.Cast<char, byte>(canonical));

        foreach (ArraySegment<byte> changelog in targets) {
            MergeChangelog(changelog, vanillaRows, rsdbNameHash);
        }

        vanillaRows.Sort(_rowComparer);
        vanillaRoot.WriteBinary(output, endianness, version);
    }

    private void MergeChangelog(ArraySegment<byte> changelog, BymlArray vanillaRows, ulong rsdbHashName)
    {
        BymlArray entries = Byml.FromBinary(changelog).GetArray();
        foreach (Byml entry in entries) {
            BymlMap map = entry.GetMap();
            switch (map[_idKey].Value) {
                case ulong keyId:
                    Byml vanilla = GetVanillaRow(keyId, vanillaRows, rsdbHashName);
                    map.Remove(_idKey);
                    BymlMergerService.Merge(entry, vanilla);
                    break;
                default:
                    vanillaRows.Add(entry);
                    break;
            }
        }
    }

    private static Byml GetVanillaRow(ulong rowId, BymlArray vanillaRows, ulong rsdbNameHash)
    {
        int vanillaIndex = RsdbIndexMappingService.GetIndex(rsdbNameHash, rowId);
        return vanillaRows[vanillaIndex];
    }
}
