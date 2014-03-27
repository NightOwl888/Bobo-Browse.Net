using Lucene.Net.Search;
using Lucene.Net.Util;

namespace BoboBrowse.docidset
{
    public class BitsetDocSet : DocIdSet
    {
        private readonly OpenBitSet bs;
        public BitsetDocSet()
        {
            bs = new OpenBitSet();
        }

        public BitsetDocSet(int nbits)
        {
            bs = new OpenBitSet(nbits);
        }

        public virtual void AddDoc(int docid)
        {
            bs.Set(docid);
        }

        public virtual long Size()
        {
            return bs.Cardinality();
        }

        public override DocIdSetIterator Iterator()
        {
            return new BitsDocIdSetIterator(bs);
        }

        private class BitsDocIdSetIterator : DocIdSetIterator
        {
            private readonly OpenBitSet bs;
            private int current;

            internal BitsDocIdSetIterator(OpenBitSet bs)
            {
                this.bs = bs;
                current = -1;
            }

            public override int Doc()
            {
                return current;
            }

            public override bool Next()
            {
                current = bs.NextSetBit(current + 1);
                return current != -1;
            }

            public override bool SkipTo(int target)
            {
                current = bs.NextSetBit(target);
                return current != -1;
            }
        }
    }
}