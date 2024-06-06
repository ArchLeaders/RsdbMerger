namespace RsdbMerger.Core.Components;

public class TagTableKeyComparer : IComparer<(string Prefix, string Name, string Suffix)>
{
    public static readonly TagTableKeyComparer Instance = new();

    public int Compare((string Prefix, string Name, string Suffix) x, (string Prefix, string Name, string Suffix) y)
    {
        int prefixComparison = StringComparer.Ordinal.Compare(x.Prefix, y.Prefix);
        int nameComparison;

        return prefixComparison switch {
            0 => (nameComparison = StringComparer.Ordinal.Compare(x.Name, y.Name)) switch {
                0 => StringComparer.Ordinal.Compare(x.Suffix, y.Suffix),
                _ => nameComparison
            },
            _ => prefixComparison
        };
    }
}
