﻿using BymlLibrary;
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
public class RsdbUniqueRowMerger(string idKey, Func<BymlMap, ulong> getRowIdHash) : IRsdbMerger
{
    private readonly string _idKey = idKey;
    private readonly Func<BymlMap, ulong> _getRowIdHash = getRowIdHash;

    public bool CreateChangelog(ReadOnlySpan<char> canonical, ArraySegment<byte> data, RsdbFile target, Stream output)
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

    public void Merge(ReadOnlySpan<char> canonical, IEnumerable<ArraySegment<byte>> merge, RsdbFile target, Stream output)
    {
        Byml vanillaRoot = target.OpenVanilla();
        BymlArray vanillaRows = vanillaRoot.GetArray();
        ulong rsdbNameHash = XxHash3.HashToUInt64(MemoryMarshal.Cast<char, byte>(canonical));
        
        vanillaRoot.WriteBinary(output, Endianness.Little, 7);
    }
}
