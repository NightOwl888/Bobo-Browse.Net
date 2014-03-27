using System;
using Lucene.Net.Util;

namespace LuceneExt.Utils
{
    [Serializable]
    public class LongSegmentArray : PrimitiveArray<OpenBitSet>
    {
        public LongSegmentArray(int len)
            : base(len)
        {
        }

        public LongSegmentArray()
        {
        }

        protected internal override object BuildArray(int len)
        {
            return new long[len][];
        }

        public virtual void Add(long[] val)
        {
            EnsureCapacity(Count + 1);
            long[][] array = (long[][])base.Array;
            array[Count] = val;
            Count++;
        }

        public virtual void Get(int index, long[] @ref)
        {
            EnsureCapacity(index);
            ((long[][])Array)[index] = @ref;
            Count = Math.Max(Count, index + 1);
        }

        public virtual long[] Get(int index)
        {
            return ((long[][])Array)[index];
        }
    }

}