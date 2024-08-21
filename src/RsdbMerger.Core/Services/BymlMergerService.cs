using BymlLibrary.Nodes.Containers;
using BymlLibrary;

namespace RsdbMerger.Core.Services;

public class BymlMergerService
{
    public static void Merge(Byml src, Byml vanilla)
    {
        switch (src.Type) {
            case BymlNodeType.HashMap32:
                MergeMapChangelog(src.GetHashMap32(), vanilla.GetHashMap32());
                break;
            case BymlNodeType.HashMap64:
                MergeMapChangelog(src.GetHashMap64(), vanilla.GetHashMap64());
                break;
            case BymlNodeType.ArrayChangelog:
                MergeArrayChangelog(src.GetArrayChangelog(), vanilla.GetArray());
                break;
            case BymlNodeType.Map:
                MergeMapChangelog(src.GetMap(), vanilla.GetMap());
                break;
            default:
                throw new NotSupportedException($"""
                    Merging '{src.Type}' is not supported 
                    """);
        }
    }

    private static void MergeArrayChangelog(BymlArrayChangelog src, BymlArray vanilla)
    {
        int indexOffset = 0;
        foreach (var (index, (change, entry)) in src) {
            switch (change) {
                case BymlChangeType.Add:
                    vanilla.Add(entry);
                    break;
                case BymlChangeType.Remove:
                    int i = index - indexOffset;
                    if (i >= vanilla.Count) {
                        continue;
                    }

                    vanilla.RemoveAt(i);
                    indexOffset++;
                    break;
                case BymlChangeType.Edit:
                    if (entry.Value is IBymlNode) {
                        Merge(entry, vanilla[index]);
                        continue;
                    }

                    vanilla[index] = entry;
                    break;
            }
        }
    }

    private static void MergeMapChangelog<T>(IDictionary<T, Byml> src, IDictionary<T, Byml> vanilla)
    {
        foreach (var (key, entry) in src) {
            if (entry.Value is BymlChangeType.Remove) {
                vanilla.Remove(key);
                continue;
            }

            if (!vanilla.TryGetValue(key, out Byml? vanillaEntry)) {
                vanilla[key] = entry;
                continue;
            }

            if (entry.Value is IBymlNode) {
                Merge(entry, vanillaEntry);
                continue;
            }

            vanilla[key] = entry;
        }
    }
}
