using BymlLibrary;
using CommunityToolkit.HighPerformance;
using CommunityToolkit.HighPerformance.Buffers;
using TotkCommon;

namespace RsdbMerger.Core.Models;

public class RsdbFile
{
    private readonly Range _nameRange;
    private readonly Range _suffixRange;

    public string FilePath { get; }

    public ReadOnlySpan<char> Name {
        get => FilePath.AsSpan()[_nameRange];
    }

    public ReadOnlySpan<char> Suffix {
        get => FilePath.AsSpan()[_suffixRange];
    }

    public int Version { get; }

    public RsdbFile(string target)
    {
        FilePath = target;

        ReadOnlySpan<char> targetSpan = target.AsSpan();
        int nameStartIndex = targetSpan
            .LastIndexOf(Path.DirectorySeparatorChar);
        ReadOnlySpan<char> name = targetSpan[++nameStartIndex..];

        int wordEndIndex = name.IndexOf('.');
        _nameRange = nameStartIndex..(nameStartIndex + wordEndIndex);

        int nextRelativeIndex = name[++wordEndIndex..].IndexOf('.');
        Range versionRange = (wordEndIndex += ++nextRelativeIndex)..(wordEndIndex += name[wordEndIndex..].IndexOf('.'));
        _suffixRange = (nameStartIndex + ++wordEndIndex)..;

        Version = int.Parse(name[versionRange]);
    }

    public string GetOutputPath(string outputRsdbFolder)
    {
        return Path.Combine(outputRsdbFolder, $"{Name}.Product.{Suffix}");
    }

    public Byml OpenVanilla()
    {
        string path = Path.Combine(Totk.Config.GamePath, RSDB, $"{Name}.Product.{Totk.Config.Version}.{Suffix}");
        using FileStream fs = File.OpenRead(path);
        int size = Convert.ToInt32(fs.Length);
        using SpanOwner<byte> buffer = SpanOwner<byte>.Allocate(size);
        fs.Read(buffer.Span);

        using SpanOwner<byte> decompressed = SpanOwner<byte>.Allocate(Zstd.GetDecompressedSize(buffer.Span));
        Totk.Zstd.Decompress(buffer.Span, decompressed.Span);

        return Byml.FromBinary(decompressed.Span);
    }
}
