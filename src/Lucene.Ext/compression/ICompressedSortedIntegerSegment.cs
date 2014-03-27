using Lucene.Net.Util;

namespace LuceneExt.Compression
{
    public interface ICompressedSortedIntegerSegment
    {
        OpenBitSet Compress(int[] inputSet);

        long[] CompressAlt(int[] inputSet);

        int[] Decompress(long[] packedSet); 

        int[] Decompress(OpenBitSet packedSet);
    }
}