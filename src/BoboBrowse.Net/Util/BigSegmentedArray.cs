using Lucene.Net.Util;

namespace BoboBrowse.Util
{
    public abstract class BigSegmentedArray
    {
        private readonly int size;
        private readonly int blockSize;
        private readonly int shiftSize;

        protected internal int numrows;

        protected BigSegmentedArray(int size, int blockSize, int shiftSize)
        {
            this.size = size;
            this.blockSize = blockSize;
            this.shiftSize = shiftSize;
            numrows = (size >> shiftSize) + 1;
        }

        public virtual int Size()
        {
            return size;
        }

        public abstract int Get(int docId);

        public virtual int Capacity()
        {
            return numrows * blockSize;
        }

        public abstract void Add(int docId, int val);

        public abstract void Fill(int val);

        public abstract void EnsureCapacity(int size);

        public abstract int MaxValue();

        public abstract int FindValue(int val, int docId, int maxId);

        public abstract int FindValues(OpenBitSet bitset, int docId, int maxId);

        public abstract int FindValueRange(int minVal, int maxVal, int docId, int maxId);

        public abstract int FindBits(int bits, int docId, int maxId);
    }
}