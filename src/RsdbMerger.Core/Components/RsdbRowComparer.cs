using BymlLibrary;
using BymlLibrary.Nodes.Containers;

namespace RsdbMerger.Core.Components;

public class RsdbRowComparer(string idKey) : IComparer<Byml>
{
    public int Compare(Byml? x, Byml? y)
    {
        return (x?.Value, y?.Value) switch {
            (BymlMap xMap, BymlMap yMap) => (xMap.GetValueOrDefault(idKey)?.Value, yMap.GetValueOrDefault(idKey)?.Value) switch {
                (string xStringValue, string yStringValue) => StringComparer.Ordinal.Compare(xStringValue, yStringValue),
                (uint xUIntValue, uint yUIntValue) => xUIntValue.CompareTo(yUIntValue),
                (int xIntValue, int yIntValue) => xIntValue.CompareTo(yIntValue),
                _ => 0
            },
            _ => 0
        };
    }
}
