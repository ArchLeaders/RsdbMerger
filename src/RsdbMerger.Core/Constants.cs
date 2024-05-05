global using static RsdbMerger.Core.Constants;
using Microsoft.IO;

namespace RsdbMerger.Core;

public static class Constants
{
    public const string RSDB = "RSDB";

    public static readonly RecyclableMemoryStreamManager MemoryStreamManager = new();
}
