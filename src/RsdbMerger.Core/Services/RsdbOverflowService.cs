using BymlLibrary;
using CommunityToolkit.HighPerformance;
using CommunityToolkit.HighPerformance.Buffers;
using System.Collections.Frozen;
using MutableOverflowMap = System.Collections.Generic.Dictionary<ulong, System.Collections.Frozen.FrozenDictionary<ulong, (BymlLibrary.Byml Row, int Version)[]>>;
using MutableOverflowMapEntries = System.Collections.Generic.Dictionary<ulong, (BymlLibrary.Byml Row, int Version)[]>;
using OverflowMap = System.Collections.Frozen.FrozenDictionary<ulong, System.Collections.Frozen.FrozenDictionary<ulong, (BymlLibrary.Byml Row, int Version)[]>>;
using OverflowMapEntries = System.Collections.Frozen.FrozenDictionary<ulong, (BymlLibrary.Byml Row, int Version)[]>;
using OverflowMapEntry = (BymlLibrary.Byml Row, int Version);

namespace RsdbMerger.Core.Services;

public class RsdbOverflowService
{
    private static readonly OverflowMap _overflow;

    static RsdbOverflowService()
    {
        using Stream? stream = typeof(RsdbIndexMappingService).Assembly
            .GetManifestResourceStream("RsdbMerger.Core.Resources.RsdbOverflow.bin");

        ArgumentNullException.ThrowIfNull(stream, nameof(stream));

        MutableOverflowMap overflow = [];

        int count = stream.Read<int>();
        for (int i = 0; i < count; i++) {
            ulong hash = stream.Read<ulong>();
            int entryCount = stream.Read<int>();
            overflow.Add(hash,
                ReadEntries(stream, entryCount)
            );
        }

        _overflow = overflow.ToFrozenDictionary();
    }

    public static Byml? TryGetVanilla(ulong rsdbNameHash, ulong rowId, int version)
    {
        if (!_overflow[rsdbNameHash].TryGetValue(rowId, out OverflowMapEntry[]? result)) {
            return null;
        }

        OverflowMapEntry entry = result[0];

        for (int i = 1; i < result.Length; i++) {
            OverflowMapEntry next = result[i];
            if (next.Version > version) {
                break;
            }

            entry = next;
        }

        return entry.Row;
    }

    private static OverflowMapEntries ReadEntries(Stream stream, int count)
    {
        MutableOverflowMapEntries entries = [];

        for (int i = 0; i < count; i++) {
            ulong rowId = stream.Read<ulong>();
            int versionCount = stream.Read<int>();
            entries.Add(rowId,
                ReadVersionEntries(stream, versionCount)
            );
        }

        return entries.ToFrozenDictionary();
    }

    private static OverflowMapEntry[] ReadVersionEntries(Stream stream, int count)
    {
        OverflowMapEntry[] entries = new OverflowMapEntry[count];

        for (int i = 0; i < count; i++) {
            int version = stream.Read<int>();
            int bymlBufferSize = stream.Read<int>();
            using SpanOwner<byte> buffer = SpanOwner<byte>.Allocate(bymlBufferSize);
            stream.Read(buffer.Span);
            entries[i] = (
                Byml.FromBinary(buffer.Span), version
            );
        }

        return entries;
    }
}
