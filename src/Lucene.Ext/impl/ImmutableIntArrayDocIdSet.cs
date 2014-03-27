using Lucene.Net.Search;
using LuceneExt.Utils;

namespace LuceneExt.Impl
{
    public class ImmutableIntArrayDocIdSet : DocIdSet
    {
        private readonly int[] array;

        public ImmutableIntArrayDocIdSet(int[] array)
        {
            this.array = array;
        }

        public override DocIdSetIterator Iterator()
        {
            return new ImmutableIntArrayDocIdSetIterator(array);
        }

        public override bool IsCacheable()
        {
            return true;
        }

        public class ImmutableIntArrayDocIdSetIterator : DocIdSetIterator
        {
            private int doc;
            private int cursor;
            private readonly int[] array;

            public ImmutableIntArrayDocIdSetIterator(int[] array)
            {
                this.array = array;
                doc = -1;
                cursor = -1;
            }

            public override int DocID()
            {
                return doc;
            }

            public override int NextDoc()
            {
                if (++cursor < array.Length)
                {
                    doc = array[cursor];
                }
                else
                {
                    doc = DocIdSetIterator.NO_MORE_DOCS;
                }
                return doc;
            }

            public override int Advance(int target)
            {
                if (cursor >= array.Length || array.Length == -1)
                {
                    return DocIdSetIterator.NO_MORE_DOCS;
                }
                if (target <= doc)
                {
                    target = doc + 1;
                }

                int index = IntArray.BinarySearch(array, cursor, array.Length, target);

                if (index > 0)
                {
                    cursor = index;
                    doc = array[cursor];
                    return doc;
                }
                else
                {
                    cursor = -(index + 1);
                    if (cursor > array.Length)
                    {
                        doc = DocIdSetIterator.NO_MORE_DOCS;
                    }
                    else
                    {
                        doc = array[cursor];
                    }
                    return doc;
                }
            }
        }
    }
}