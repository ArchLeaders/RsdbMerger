namespace RsdbMerger.Core.Components;

public class VersionComparer : IComparer<int>
{
    public static readonly VersionComparer Shared = new();

    public int Compare(int x, int y)
    {
        if (x == 100) {
            return int.MaxValue;
        }

        return x - y;
    }
}
