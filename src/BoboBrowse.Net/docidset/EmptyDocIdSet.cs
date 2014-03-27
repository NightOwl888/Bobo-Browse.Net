using Lucene.Net.Search;

namespace BoboBrowse.docidset
{
    public class EmptyDocIdSet : RandomAccessDocIdSet
    {
        private static EmptyDocIdSet SINGLETON = new EmptyDocIdSet();

        private class EmptyDocIdSetIterator : DocIdSetIterator
        {
            public override int Doc()
            {
                return -1;
            }
            public override bool Next()
            {
                return false;
            }
            public override bool SkipTo(int target)
            {
                return false;
            }
        }

        private static EmptyDocIdSetIterator SINGLETON_ITERATOR = new EmptyDocIdSetIterator();

        private EmptyDocIdSet()
        {
        }

        public static EmptyDocIdSet GetInstance()
        {
            return SINGLETON;
        }

        public override DocIdSetIterator Iterator()
        {
            return SINGLETON_ITERATOR;
        }

        public override bool Get(int docId)
        {
            return false;
        }
    }
}