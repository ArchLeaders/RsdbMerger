using RsdbMerger.Core.Services;
using System.Diagnostics;

Stopwatch watch = Stopwatch.StartNew();

for (int i = 0; i < (args.Length - 1); i++) {
    RsdbChangelogService changelogServiceM1 = new(args[i], args[i] += ".Changelog");
    changelogServiceM1.CreateChangelogs();
}

RsdbMergerService mergerService = new(args.AsSpan()[..^1], args[^1]);
mergerService.Merge();

watch.Stop();
Console.WriteLine(watch.ElapsedMilliseconds);
