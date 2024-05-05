using CommunityToolkit.HighPerformance;
using System.Collections.Frozen;

namespace RsdbMerger.Core.Services;

public class RsdbIndexMappingService
{
    private static readonly FrozenDictionary<ulong, FrozenDictionary<ulong, int>> _indexMapping;

    static RsdbIndexMappingService()
    {
        using Stream? stream = typeof(RsdbIndexMappingService).Assembly
            .GetManifestResourceStream("RsdbMerger.Core.Resources.RsdbIndexMapping.bin");

        ArgumentNullException.ThrowIfNull(stream, nameof(stream));

        Dictionary<ulong, FrozenDictionary<ulong, int>> indexMapping = [];

        int count = stream.Read<int>();
        for (int i = 0; i < count; i++) {
            ulong hash = stream.Read<ulong>();
            int entryCount = stream.Read<int>();
            indexMapping.Add(hash,
                ReadEntries(stream, entryCount)
            );
        }

        _indexMapping = indexMapping.ToFrozenDictionary();
    }

    public static int GetIndex(ulong rsdbNameHash, ulong rowId)
    {
        return _indexMapping[rsdbNameHash].GetValueOrDefault(rowId, -1);
    }

    private static FrozenDictionary<ulong, int> ReadEntries(Stream stream, int count)
    {
        Dictionary<ulong, int> entries = [];

        for (int i = 0; i < count; i++) {
            entries.Add(stream.Read<ulong>(), stream.Read<int>());
        }

        return entries.ToFrozenDictionary();
    }
}
