using RsdbMerger.Core.Models;
using RsdbMerger.Core.Services;

namespace RsdbMerger.Core.Mergers;

public class RsdbTagMerger : IRsdbMerger
{
    public static readonly RsdbTagMerger Shared = new();

    public void CreateChangelog(ReadOnlySpan<char> canonical, ArraySegment<byte> data, RsdbFile target, Stream output)
    {
        throw new NotImplementedException();
    }

    public void Merge(ArraySegment<byte>[] merge, Stream output)
    {
        throw new NotImplementedException();
    }
}
