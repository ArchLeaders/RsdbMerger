using BymlLibrary;
using BymlLibrary.Nodes.Containers;

namespace RsdbMerger.Core.Services;

public class BymlMergerService
{
    public static bool CreateChangelog(ref Byml src, Byml vanilla)
    {
        if (src.Type != vanilla.Type) {
            return false;
        }

        return src.Type switch {
            BymlNodeType.HashMap32 => CreateMapChangelog(src.GetHashMap32(), vanilla.GetHashMap32()),
            BymlNodeType.HashMap64 => CreateMapChangelog(src.GetHashMap64(), vanilla.GetHashMap64()),
            BymlNodeType.Array => CreateArrayChangelog(ref src, src.GetArray(), vanilla.GetArray()),
            BymlNodeType.Map => CreateMapChangelog(src.GetMap(), vanilla.GetMap()),
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

    private static bool CreateArrayChangelog(ref Byml root, BymlArray src, BymlArray vanilla)
    {
        (bool isVanillaSmaller, BymlArray larger, BymlArray smaller) = (vanilla.Count < src.Count)
            ? (true, src, vanilla)
            : (false, vanilla, src);

        BymlArrayChangelog changelog = [];

        int i = 0;

        for (; i < smaller.Count; i++) {
            Byml srcEntry = src[i];
            if (!CreateChangelog(ref srcEntry, vanilla[i])) {
                changelog[i] = (BymlChangeType.Edit, srcEntry);
            }
        }

        for (; i < larger.Count; i++) {
            changelog[i] = isVanillaSmaller switch {
                true => (BymlChangeType.Add, src[i]),
                false => (BymlChangeType.Remove, new())
            };
        }

        root = changelog;
        return changelog.Count == 0;
    }

    private static bool CreateMapChangelog<T>(IDictionary<T, Byml> src, IDictionary<T, Byml> vanilla)
    {
        foreach (T key in src.Keys.Concat(vanilla.Keys).Distinct().ToArray()) { // TODO: Avoid copying keys
            if (!src.TryGetValue(key, out Byml? srcValue)) {
                src[key] = BymlChangeType.Remove;
                continue;
            }

            if (vanilla.TryGetValue(key, out Byml? vanillaNode) && CreateChangelog(ref srcValue, vanillaNode)) {
                src.Remove(key);
                continue;
            }

            // CreateChangelog can mutate
            // srcValue, so reassign the key
            src[key] = srcValue;
        }

        return src.Count == 0;
    }
}
