using BymlLibrary;
using BymlLibrary.Nodes.Containers;
using Microsoft.IO;
using Revrs;
using RsdbMerger.Core.Models;
using RsdbMerger.Core.Services;
using System.IO.Hashing;
using System.Runtime.InteropServices;

namespace RsdbMerger.Core.Mergers;

/// <summary>
/// Merges array based RSDB files using the a unique id.
/// </summary>
public class RsdbUniqueRowMerger : IRsdbMerger
{
    private readonly Func<BymlMap, ulong> _getRowIdHash;

    public RsdbUniqueRowMerger(Func<BymlMap, ulong> getRowIdHash)
    {
        _getRowIdHash = getRowIdHash;
    }

    public void CreateChangelog(ReadOnlySpan<char> canonical, ArraySegment<byte> data, RsdbFile target, Stream output)
    {
        BymlArray vanillaRows = target.OpenVanilla().GetArray();
        ulong rsdbNameHash = XxHash3.HashToUInt64(MemoryMarshal.Cast<char, byte>(canonical));

        RevrsReader reader = new(data);
        ImmutableByml byml = new(ref reader);
        Endianness endianness = byml.Endianness;
        ushort bymlVersion = byml.Header.Version;

        Byml root = Byml.FromImmutable(byml);
        BymlArray array = root.GetArray();

        for (int i = 0; i < array.Count; i++) {
            bool isVanillaRow = ProcessRow(vanillaRows, array[i], rsdbNameHash, target.Version);

            if (isVanillaRow) {
                array.RemoveAt(i);
                i--;
            }
        }

        // BYML is much faster to write into memory
        using RecyclableMemoryStream ms = new(MemoryStreamManager);
        root.WriteBinary(ms, endianness, bymlVersion);
        ms.Seek(0, SeekOrigin.Begin);
        ms.CopyTo(output);
    }

    private bool ProcessRow(BymlArray vanillaRows, Byml row, ulong rsdbNameHash, int version)
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

        if (Byml.ValueEqualityComparer.Default.Equals(row, vanilla)) {
            return true;
        }

        // Isolate changes

        return false;
    }

    public void Merge(ArraySegment<byte>[] merge, Stream output)
    {
        throw new NotImplementedException();
    }
}
