using CommunityToolkit.HighPerformance.Buffers;
using RsdbMerger.Core.Components;
using RsdbMerger.Core.Models;
using System.Buffers;
using TotkCommon;
using TotkCommon.Extensions;

namespace RsdbMerger.Core.Services;

public class RsdbMergerService
{
    internal const string SEARCH_PATTERN = "*.Product.*.*";

    private readonly Dictionary<string, List<RsdbFile>> _targets = [];
    private readonly string _outputRsdb;
    private readonly string _output;

    public RsdbMergerService(IEnumerable<string> romfsMods, string output)
    {
        _output = output;
        _outputRsdb = Path.Combine(output, RSDB);

        foreach (string romfs in romfsMods) {
            string rsdb = Path.Combine(romfs, RSDB);

            if (!Directory.Exists(rsdb)) {
                continue;
            }

            foreach (string filePath in Directory.EnumerateFiles(rsdb, SEARCH_PATTERN, SearchOption.TopDirectoryOnly)) {
                RsdbFile target = new(filePath);
                string name = $"{target.Name}.Product.{Totk.Config.Version}.{target.Suffix}.zs";

                if (target.Suffix is not "rstbl.byml") {
                    continue;
                }

                if (!_targets.TryGetValue(name, out List<RsdbFile>? files)) {
                    _targets[name] = [target];
                    continue;
                }

                files.Add(target);
            }
        }
    }

    public void Merge()
    {
        if (_targets.Count < 1) {
            return;
        }

        Directory.CreateDirectory(_outputRsdb);

        foreach ((string name, List<RsdbFile> targets) in _targets) {
            MergeTargets(name, targets);
        }
    }

    public async Task MergeAsync()
    {
        if (_targets.Count < 1) {
            return;
        }

        Directory.CreateDirectory(_outputRsdb);

        await Parallel.ForEachAsync(_targets, (target, _) => {
            MergeTargets(target.Key, target.Value);
            return ValueTask.CompletedTask;
        });
    }

    private void MergeTargets(string name, List<RsdbFile> targets)
    {
        string outputName = $"{RSDB}/{name}";
        ReadOnlySpan<char> canonical = outputName.ToCanonical();
        IRsdbMerger merger = RsdbMergerProvider.GetMerger(canonical);

        using MemoryStream ms = new();
        merger.Merge(canonical, ReadTargets(targets), targets[0], ms);

        int size = Convert.ToInt32(ms.Length);
        using SpanOwner<byte> compressed = SpanOwner<byte>.Allocate(size);

        lock (Totk.Zstd) {
            size = Totk.Zstd.Compress(ms.ToArray(), compressed.Span, zsDictionaryId: 1);
        }

        ArgumentNullException.ThrowIfNull(Totk.AddressTable);

        string output = Path.Combine(_output, Totk.AddressTable.GetValueOrDefault(canonical.ToString(), outputName)) + ".zs";
        using FileStream fs = File.Create(output);
        fs.Write(compressed.Span[..size]);
    }

    private static IEnumerable<ArraySegment<byte>> ReadTargets(IEnumerable<RsdbFile> targets)
    {
        foreach (RsdbFile target in targets) {
            using FileStream fs = File.OpenRead(target.FilePath);
            int size = Convert.ToInt32(fs.Length);
            byte[] buffer = ArrayPool<byte>.Shared.Rent(size);
            fs.Read(buffer.AsSpan()[..size]);
            yield return new ArraySegment<byte>(buffer, 0, size);
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}
