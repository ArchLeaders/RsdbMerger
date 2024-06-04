using BymlLibrary;
using Microsoft.IO;
using Revrs;
using RsdbMerger.Core.Components;
using RsdbMerger.Core.Models;
using RsdbMerger.Core.Services;

namespace RsdbMerger.Core.Mergers;

public class RsdbTagMerger : IRsdbMerger
{
    public static readonly RsdbTagMerger Shared = new();

    public void CreateChangelog(ReadOnlySpan<char> canonical, ArraySegment<byte> data, RsdbFile target, Stream output)
    {
        RevrsReader reader = new(data);
        ImmutableByml byml = new(ref reader);
        Endianness endianness = byml.Endianness;
        ushort bymlVersion = byml.Header.Version;

        Byml root = Byml.FromImmutable(byml);
        Byml changelog = TagTable.LogChanges(root.GetMap(), target.OpenVanilla());

        // BYML is much faster to write into memory
        using RecyclableMemoryStream ms = new(MemoryStreamManager);
        changelog.WriteBinary(ms, endianness, bymlVersion);
        ms.Seek(0, SeekOrigin.Begin);
        ms.CopyTo(output);
    }

    public void Merge(ReadOnlySpan<char> canonical, ArraySegment<byte>[] merge, RsdbFile target, Stream output)
    {
        throw new NotImplementedException();
    }
}
