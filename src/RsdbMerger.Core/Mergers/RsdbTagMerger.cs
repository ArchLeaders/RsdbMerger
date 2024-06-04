using BymlLibrary;
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
        Byml changelog = TagTable.LogChanges(root.GetMap(), target.OpenVanilla(), out bool hasChanges);

        if (!hasChanges) {
            return false;
        }

        changelog.WriteBinary(output, endianness, bymlVersion);
        return true;
    }

    public void Merge(ReadOnlySpan<char> canonical, ArraySegment<byte>[] merge, RsdbFile target, Stream output)
    {
        throw new NotImplementedException();
    }
}
