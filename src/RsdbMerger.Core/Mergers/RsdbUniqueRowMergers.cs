using System.IO.Hashing;
using System.Runtime.InteropServices;

namespace RsdbMerger.Core.Mergers;

/// <summary>
/// Common <see cref="RsdbUniqueRowMerger{T}"/> instances.
/// </summary>
public class RsdbUniqueRowMergers
{
    private const string ROW_ID_KEY = "__RowId";
    public static readonly RsdbUniqueRowMerger RowId = new(ROW_ID_KEY,
        (row) => {
            return XxHash3.HashToUInt64(MemoryMarshal.Cast<char, byte>(row[ROW_ID_KEY].GetString()));
        }
    );

    private const string NAME_KEY = "Name";
    public static readonly RsdbUniqueRowMerger Name = new(NAME_KEY,
        (row) => {
            return XxHash3.HashToUInt64(MemoryMarshal.Cast<char, byte>(row[NAME_KEY].GetString()));
        }
    );

    private const string FULL_TAG_ID_KEY = "FullTagId";
    private const string DISPLAY_ORDER_KEY = "DisplayOrder";
    public static readonly RsdbUniqueRowMerger FullTagId = new(FULL_TAG_ID_KEY,
        (row) => {
            return XxHash3.HashToUInt64(MemoryMarshal.Cast<char, byte>(row[FULL_TAG_ID_KEY].GetString()));
        },
        sortKey: DISPLAY_ORDER_KEY,
        (row, index) => {
            row[DISPLAY_ORDER_KEY] = index;
        }
    );

    private const string NAME_HASH_KEY = "NameHash";
    public static readonly RsdbUniqueRowMerger NameHash = new(NAME_HASH_KEY,
        (row) => {
            return row[NAME_HASH_KEY].GetUInt32();
        }
    );
}
