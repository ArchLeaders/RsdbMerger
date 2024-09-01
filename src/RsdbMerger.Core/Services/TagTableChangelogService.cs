using BymlLibrary;
using BymlLibrary.Nodes.Containers;
using CommunityToolkit.HighPerformance.Buffers;
using RsdbMerger.Core.Components;
using System.Collections.Frozen;

namespace RsdbMerger.Core.Services;

public class TagTableChangelogService
{
    public static Byml LogChanges(BymlMap src, Byml vanillaByml, out bool hasChanges)
    {
        using FrozenTagTable vanilla = new(vanillaByml.GetMap());

        BymlArray entries = [];
        BymlArray paths = src[TagTable.PATH_LIST].GetArray();
        BymlArray tags = src[TagTable.TAG_LIST].GetArray();
        byte[] bitTable = src[TagTable.BIT_TABLE].GetBinary();

        for (int i = 0; i < paths.Count; i++)
        {
            int entryIndex = i / 3;
            bool isKeyVanilla = vanilla.HasEntry(paths, ref i, out int vanillaEntryIndex, out (Byml Prefix, Byml Name, Byml Suffix) _);
            HashSet<string> entryTags = TagTable.GetEntryTags<HashSet<string>>(entryIndex, tags, bitTable);

            int removedCount = 0;
            SpanOwner<string> removed = SpanOwner<string>.Empty;
            if (isKeyVanilla && IsEntryVanilla(entryTags, vanilla.EntryTags[vanillaEntryIndex], out removed, out removedCount))
            {
                continue;
            }

            entries.AddRange(paths[(i - 2)..(i + 1)]);
            entries.Add(CreateEntry(entryTags, removed.Span[..removedCount]));
            removed.Dispose();
        }

        BymlArray addedTags = [];

        foreach (Byml tag in tags)
        {
            if (!vanilla.HasTag(tag))
            {
                addedTags.Add(tag);
            }
        }

        hasChanges = entries.Count > 0 || addedTags.Count > 0;
        return new BymlMap() {
            { "Entries", entries },
            { "Tags", addedTags }
        };
    }

    private static Byml CreateEntry(HashSet<string> entryTags, Span<string> removed)
    {
        int index = -1;
        BymlArrayChangelog changelog = [];

        foreach (string tag in removed)
        {
            changelog[++index] = (BymlChangeType.Remove, tag);
        }

        foreach (string tag in entryTags)
        {
            changelog[++index] = (BymlChangeType.Add, tag);
        }

        return changelog;
    }

    private static bool IsEntryVanilla(HashSet<string> entryTags, FrozenSet<string> vanillaEntryTags, out SpanOwner<string> removed, out int removedCount)
    {
        removed = SpanOwner<string>.Allocate(vanillaEntryTags.Count);
        removedCount = 0;

        foreach (string tag in vanillaEntryTags)
        {
            if (!entryTags.Remove(tag))
            {
                removed.Span[removedCount] = tag;
                removedCount++;
            }
        }

        return entryTags.Count == 0;
    }
}
