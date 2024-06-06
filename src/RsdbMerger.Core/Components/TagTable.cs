﻿using BymlLibrary;
using BymlLibrary.Nodes.Containers;

namespace RsdbMerger.Core.Components;

public class TagTable
{
    public const string PATH_LIST = "PathList";
    public const string TAG_LIST = "TagList";
    public const string BIT_TABLE = "BitTable";
    public const string RANK_TABLE = "RankTable";

    private readonly byte[] _rankTable;
    public Dictionary<(string, string, string), List<string>> Entries { get; } = [];
    public List<string> Tags { get; }

    public TagTable(BymlMap root)
    {
        BymlArray paths = root[PATH_LIST].GetArray();
        BymlArray tags = root[TAG_LIST].GetArray();
        Span<byte> bitTable = root[BIT_TABLE].GetBinary();
        _rankTable = root[RANK_TABLE].GetBinary();

        for (int i = 0; i < paths.Count; i++) {
            int entryIndex = i / 3;
            (string, string, string) key = (
                paths[i].GetString(), paths[++i].GetString(), paths[++i].GetString()
            );

            Entries[key] = GetEntryTags<List<string>>(entryIndex, tags, bitTable);
        }

        Tags = [.. tags.Select(x => x.GetString())];
    }

    public Byml Compile()
    {
        List<KeyValuePair<(string, string, string), List<string>>> entries = [..
            Entries.OrderBy(x => x.Key, TagTableKeyComparer.Instance)
        ];

        BymlArray paths = CollectPaths(entries);
        Tags.Sort(StringComparer.Ordinal);

        return new BymlMap {
            { BIT_TABLE, CompileBitTable(entries) },
            { PATH_LIST, paths },
            { RANK_TABLE, _rankTable },
            { TAG_LIST, new BymlArray(Tags.Select(x => (Byml)x)) },
        };
    }

    private byte[] CompileBitTable(List<KeyValuePair<(string, string, string), List<string>>> entries)
    {
        BitTableWriter writer = new(Tags, entries.Select(x => x.Value), entries.Count);
        return writer.Compile();
    }

    private BymlArray CollectPaths(List<KeyValuePair<(string, string, string), List<string>>> entries)
    {
        BymlArray paths = new(Entries.Count * 3);
        foreach (((string Prefix, string Name, string Suffix) entry, List<string> tags) in entries) {
            paths.Add(entry.Prefix);
            paths.Add(entry.Name);
            paths.Add(entry.Suffix);
            tags.Sort(StringComparer.Ordinal);
        }

        return paths;
    }

    public unsafe static T GetEntryTags<T>(int entryIndex, BymlArray tags, Span<byte> bitTable) where T : ICollection<string>, new()
    {
        T entryTags = new();

        int index = entryIndex * tags.Count;
        int bitOffset = index % 8;

        fixed (byte* ptr = &bitTable[index / 8]) {
            byte* current = ptr;

            for (int i = 0; i < tags.Count; i++) {
                int value = *current >> bitOffset & 1;
                if ((*current >> bitOffset & 1) == 1) {
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
