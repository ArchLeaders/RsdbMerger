using System.IO.Hashing;
using System.Runtime.InteropServices;

namespace RsdbMerger.Core.Mergers;

/// <summary>
/// Common <see cref="RsdbUniqueRowMerger{T}"/> instances.
/// </summary>
public class RsdbUniqueRowMergers
{
    public static readonly RsdbUniqueRowMerger RowId = new(
        (row) => {
            return XxHash3.HashToUInt64(MemoryMarshal.Cast<char, byte>(row["__RowId"].GetString()));
        }
    );

    public static readonly RsdbUniqueRowMerger Name = new(
        (row) => {
            return XxHash3.HashToUInt64(MemoryMarshal.Cast<char, byte>(row["Name"].GetString()));
        }
    );

    public static readonly RsdbUniqueRowMerger FullTagId = new(
        (row) => {
            return XxHash3.HashToUInt64(MemoryMarshal.Cast<char, byte>(row["FullTagId"].GetString()));
        }
    );

    public static readonly RsdbUniqueRowMerger NameHash = new(
        (row) => {
            return row["NameHash"].GetUInt32();
        }
    );
}
