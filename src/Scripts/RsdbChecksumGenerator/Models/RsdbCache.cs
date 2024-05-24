using BymlLibrary;

namespace RsdbChecksumGenerator.Models;

public class RsdbCache : Dictionary<ulong, List<(Byml Node, int Version, int Count, int HashCode)>>
{
    public List<ulong> IndexMapping { get; } = [];
    public bool IsIndexMappingFilled { get; set; } = false;
}
