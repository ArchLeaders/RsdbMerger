using BymlLibrary;
using BymlLibrary.Nodes.Containers;
using Revrs;
using RsdbMerger.Core.Components;
using RsdbMerger.Core.Models;
using RsdbMerger.Core.Services;

namespace RsdbMerger.Core.Mergers;

public class RsdbTagMerger : IRsdbMerger
{
    public static readonly RsdbTagMerger Shared = new();

    public bool CreateChangelog(ReadOnlySpan<char> canonical, ArraySegment<byte> data, RsdbFile target, Stream output)
    {
        RevrsReader reader = new(data);
        ImmutableByml byml = new(ref reader);
        Endianness endianness = byml.Endianness;
        ushort bymlVersion = byml.Header.Version;

        Byml root = Byml.FromImmutable(byml);
        Byml changelog = TagTableChangelogService.LogChanges(root.GetMap(), target.OpenVanilla(out _, out _), out bool hasChanges);

        if (!hasChanges) {
            return false;
        }

        changelog.WriteBinary(output, endianness, bymlVersion);
        return true;
    }

    public void Merge(ReadOnlySpan<char> canonical, IEnumerable<ArraySegment<byte>> targets, RsdbFile target, Stream output)
    {
        TagTable vanilla = new(
            target.OpenVanilla(out Endianness endianness, out ushort version, extension: ".zs").GetMap()
        );

        foreach (ArraySegment<byte> targetData in targets) {
            Byml root = Byml.FromBinary(targetData);
            MergeChangelog(root.GetMap(), vanilla);
        }
        
        vanilla.Compile().WriteBinary(output, endianness, version);
    }

    private static void MergeChangelog(BymlMap root, TagTable vanilla)
    {
        BymlArray paths = root["Entries"].GetArray();

        for (int i = 0; i < paths.Count; i++) {
            (string, string, string) key = (
                paths[i].GetString(), paths[++i].GetString(), paths[++i].GetString()
            );

            BymlArrayChangelog entryTags = paths[++i].GetArrayChangelog();

            if (!vanilla.Entries.TryGetValue(key, out List<string>? vanillaEntryTags)) {
                vanilla.Entries[key] = [..
                    entryTags.Values.Select(x => x.Item2.GetString())
                ];
                continue;
            }

            foreach ((int _, (BymlChangeType change, Byml tag)) in entryTags) {
                switch (change) {
                    case BymlChangeType.Edit:
                    case BymlChangeType.Add:
                        vanillaEntryTags.Add(tag.GetString());
                        break;
                    case BymlChangeType.Remove:
                        vanillaEntryTags.Remove(tag.GetString());
                        break;
                }
            }
        }

        BymlArray tags = root["Tags"].GetArray();
        vanilla.Tags.AddRange(tags.Select(x => x.GetString()));
    }
}
