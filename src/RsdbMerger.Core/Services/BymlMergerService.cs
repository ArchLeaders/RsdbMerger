using BymlLibrary.Nodes.Containers;
using BymlLibrary;

namespace RsdbMerger.Core.Services;

public class BymlMergerService
{
    public static bool Merge(ref Byml src, Byml vanilla)
    {
        if (src.Type != vanilla.Type) {
            return false;
        }

        return src.Type switch {
            BymlNodeType.HashMap32 => MergeMapChangelog(src.GetHashMap32(), vanilla.GetHashMap32()),
            BymlNodeType.HashMap64 => MergeMapChangelog(src.GetHashMap64(), vanilla.GetHashMap64()),
            BymlNodeType.ArrayChangelog => MergeArrayChangelog(ref src, src.GetArrayChangelog(), vanilla.GetArrayChangelog()),
            BymlNodeType.Map => MergeMapChangelog(src.GetMap(), vanilla.GetMap()),
            BymlNodeType.String or
            BymlNodeType.Binary or
            BymlNodeType.BinaryAligned or
            BymlNodeType.Bool or
            BymlNodeType.Int or
            BymlNodeType.Float or
            BymlNodeType.UInt32 or
            BymlNodeType.Int64 or
            BymlNodeType.UInt64 or
            BymlNodeType.Double or
            BymlNodeType.Null => Byml.ValueEqualityComparer.Default.Equals(src, vanilla),
            _ => throw new NotSupportedException($"""
                Merging '{src.Type}' is not supported 
                """)
        };
    }

    private static bool MergeArrayChangelog(ref Byml root, BymlArrayChangelog src, BymlArrayChangelog vanilla)
    {
        return true;
    }

    private static bool MergeMapChangelog<T>(IDictionary<T, Byml> src, IDictionary<T, Byml> vanilla)
    {
        return true;
    }
}
