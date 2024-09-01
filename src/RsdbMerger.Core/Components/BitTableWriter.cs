using System.Collections.Frozen;

namespace RsdbMerger.Core.Components;

public unsafe class BitTableWriter
{
    private readonly byte[] _result;
    private readonly IEnumerable<List<string>> _entries;
    private readonly FrozenDictionary<string, int> _tagLookup;

    public BitTableWriter(IList<string> tags, IEnumerable<List<string>> entries, int entryCount)
    {
        int size = (int)double.Ceiling(tags.Count * entryCount / 8.0);
        _result = new byte[size];
        _entries = entries;

        Dictionary<string, int> tagLookup = new(tags.Count);
        for (int i = 0; i < tags.Count; i++) {
            tagLookup[tags[i]] = i;
        }

        _tagLookup = tagLookup.ToFrozenDictionary();
    }

    public byte[] Compile()
    {
        fixed (byte* ptr = &_result[0]) {
            int bitOffset = 0;
            byte** current = &ptr;

            foreach (List<string> tags in _entries) {
                FillEntry(tags, current, ref bitOffset);
            }

            return _result;
        }
    }

    public void FillEntry(IEnumerable<string> tags, byte** current, ref int bitOffset)
    {
        int currentEntryIndex = 0;

        foreach (string tag in tags) {
            int index = _tagLookup[tag];
            MoveBy(index - currentEntryIndex, current, ref bitOffset);
            **current |= (byte)(0x1 << bitOffset);
            currentEntryIndex = index;
        }

        MoveBy(_tagLookup.Count - currentEntryIndex, current, ref bitOffset);
    }

    private static void MoveBy(int bits, byte** current, ref int bitOffset)
    {
        int byteCount = (bits += bitOffset) / 8;
        *current += byteCount;
        bitOffset = bits - (byteCount * 8);
    }
}
