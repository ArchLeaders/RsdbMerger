using Revrs.Buffers;
using RsdbMerger.Core.Components;
using RsdbMerger.Core.Models;
using TotkCommon;
using TotkCommon.Extensions;

namespace RsdbMerger.Core.Services;

public class RsdbChangelogService
{
    private readonly string _romfs;
    private readonly string _output;
    private readonly IEnumerable<RsdbFile>? _targets;

    public RsdbChangelogService(string romfs, string output)
    {
        _romfs = romfs;
        string rsdb = Path.Combine(romfs, RSDB);
        _output = Path.Combine(output, RSDB);

        if (!Directory.Exists(rsdb)) {
            return;
        }

        _targets = Directory.EnumerateFiles(rsdb)
            .Select(x => new RsdbFile(x))
            .OrderBy(x => x.Version, VersionComparer.Shared)
            .DistinctBy(x => x.Name.ToString());
    }

    public void CreateChangelogs()
    {
        if (_targets is null) {
            return;
        }

        Directory.CreateDirectory(_output);

        foreach (RsdbFile target in _targets) {
            CreateChangelog(target);
        }
    }

    public async Task CreateChangelogsAsync()
    {
        if (_targets is null) {
            return;
        }

        Directory.CreateDirectory(_output);

        await Parallel.ForEachAsync(_targets, (target, cancellationToken) => {
            CreateChangelog(target);
            return ValueTask.CompletedTask;
        });
    }

    private void CreateChangelog(RsdbFile target)
    {
        using FileStream fs = File.OpenRead(target.FilePath);
        int size = Convert.ToInt32(fs.Length);
        using ArraySegmentOwner<byte> data = ArraySegmentOwner<byte>.Allocate(size);
        fs.Read(data.Segment);

        if (Zstd.IsCompressed(data.Segment)) {
            using ArraySegmentOwner<byte> decompressed = ArraySegmentOwner<byte>.Allocate(Zstd.GetDecompressedSize(data.Segment));
            Totk.Zstd.Decompress(data.Segment, decompressed.Segment);
            CreateChangelog(target, decompressed.Segment);
            return;
        }

        CreateChangelog(target, data.Segment);
    }

    private void CreateChangelog(RsdbFile target, ArraySegment<byte> data)
    {
        ReadOnlySpan<char> canonical = target.FilePath.ToCanonical(_romfs);
        IRsdbMerger merger = RsdbMergerService.GetMerger(canonical);

        string output = target.GetOutputPath(_output);
        using FileStream fs = File.Create(output);
        merger.CreateChangelog(canonical, data, target, fs);
    }
}
