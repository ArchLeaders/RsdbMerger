using RsdbMerger.Core.Models;

namespace RsdbMerger.Core.Services;

public interface IRsdbMerger
{
    public void Merge(ArraySegment<byte>[] merge, Stream output);
    public void CreateChangelog(ReadOnlySpan<char> canonical, ArraySegment<byte> data, RsdbFile target, Stream output);
}
