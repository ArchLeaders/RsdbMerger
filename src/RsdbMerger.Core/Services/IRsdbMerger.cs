using RsdbMerger.Core.Models;

namespace RsdbMerger.Core.Services;

public interface IRsdbMerger
{
    public bool CreateChangelog(ReadOnlySpan<char> canonical, ArraySegment<byte> data, RsdbFile target, Stream output);
    public void Merge(ReadOnlySpan<char> canonical, IEnumerable<ArraySegment<byte>> targets, RsdbFile target, Stream output);
}
