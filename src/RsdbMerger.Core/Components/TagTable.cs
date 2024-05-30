using BymlLibrary;
using BymlLibrary.Nodes.Containers;
using CommunityToolkit.HighPerformance.Buffers;
using System.Collections.Frozen;

namespace RsdbMerger.Core.Components;

public class TagTable
{
    public const string PATH_LIST = "PathList";
    public const string TAG_LIST = "TagList";
    public const string BIT_TABLE = "BitTable";

    public static Byml LogChanges(BymlMap src, Byml vanillaByml)
    {
        using FrozenTagTable vanilla = new(vanillaByml.GetMap());

        BymlArray entries = [];
        BymlArray paths = src[PATH_LIST].GetArray();
        BymlArray tags = src[TAG_LIST].GetArray();
        byte[] bitTable = src[BIT_TABLE].GetBinary();

        for (int i = 0; i < paths.Count; i++) {
            int entryIndex = i / 3;
            bool isKeyVanilla = vanilla.HasEntry(paths, ref i, out int vanillaEntryIndex, out (Byml Prefix, Byml Name, Byml Suffix) entry);
            HashSet<string> entryTags = GetEntryTags<HashSet<string>>(entryIndex, tags, bitTable);

            int removedCount = 0;
            SpanOwner<string> removed = SpanOwner<string>.Empty;
            if (isKeyVanilla && IsEntryVanilla(entryTags, vanilla.EntryTags[vanillaEntryIndex], out removed, out removedCount)) {
                continue;
            }

            entries.AddRange(paths[(i - 2)..(i + 1)]);
            entries.Add(CreateEntry(entryTags, removed.Span[..removedCount]));
            removed.Dispose();
        }

        BymlArray addedTags = [];

        foreach (Byml tag in tags) {
            if (!vanilla.HasTag(tag)) {
                addedTags.Add(tag);
            }
        }

        return new BymlMap() {
            { "Entries", entries },
            { "Tags", addedTags }
        };
    }

    private static Byml CreateEntry(HashSet<string> entryTags, Span<string> removed)
    {
        int index = -1;
        BymlArrayChangelog changelog = [];

        foreach (string tag in removed) {
            changelog[++index] = (BymlChangeType.Remove, tag);
        }

        foreach (string tag in entryTags) {
            changelog[++index] = (BymlChangeType.Add, tag);
        }

        return changelog;
    }

    private static bool IsEntryVanilla(HashSet<string> entryTags, FrozenSet<string> vanillaEntryTags, out SpanOwner<string> removed, out int removedCount)
    {
        removed = SpanOwner<string>.Allocate(vanillaEntryTags.Count);
        removedCount = 0;

        foreach (string tag in vanillaEntryTags) {
            if (!entryTags.Remove(tag)) {
                removed.Span[removedCount] = tag;
                removedCount++;
            }
        }

        return entryTags.Count == 0;
    }

    public unsafe static T GetEntryTags<T>(int entryIndex, BymlArray tags, Span<byte> bitTable) where T : ICollection<string>, new()
    {
        T entryTags = new();

        int index = entryIndex * tags.Count;
        int bitOffset = index % 8;

        fixed (byte* ptr = &bitTable[index / 8]) {
            byte* current = ptr;

            for (int i = 0; i < tags.Count; i++) {
                int value = (*current >> bitOffset) & 1;
                if (((*current >> bitOffset) & 1) == 1) {
                    entryTags.Add(tags[i].GetString());
                }

                switch (bitOffset) {
                    case 7:
                        bitOffset = 0;
                        current++;
                        continue;
                    default:
                        bitOffset++;
                        continue;
                }
            }
        }

        return entryTags;
    }
}
