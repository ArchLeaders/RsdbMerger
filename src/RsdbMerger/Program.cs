using RsdbMerger.Core.Services;

for (int i = 0; i < (args.Length - 1); i++) {
    string arg = args[i];
    RsdbChangelogService changelogServiceM1 = new(arg, args[i] += ".Changelog");
    changelogServiceM1.CreateChangelogs();
}

RsdbMergerService mergerService = new(args.AsSpan()[..^1], args[^1]);
mergerService.Merge();
