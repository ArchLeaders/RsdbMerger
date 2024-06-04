using RsdbMerger.Core.Models;

namespace RsdbMerger.Core.Services;

public interface IRsdbMerger
{
    public void CreateChangelog(ReadOnlySpan<char> canonical, ArraySegment<byte> data, RsdbFile target, Stream output);
    public void Merge(ReadOnlySpan<char> canonical, ArraySegment<byte>[] merge, RsdbFile target, Stream output);
}
