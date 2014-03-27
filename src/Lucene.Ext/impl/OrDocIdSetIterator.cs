using System.Collections.Generic;
using Lucene.Net.Search;

namespace LuceneExt.Impl
{
    public class OrDocIdSetIterator : DocIdSetIterator
    {
        private sealed class Item
        {
            public readonly DocIdSetIterator Iter;
            public int Doc;

            public Item(DocIdSetIterator iter)
            {
                Iter = iter;
                Doc = -1;
            }
        }

        private int curDoc;
        private readonly Item[] heap;
        private int size;

        internal OrDocIdSetIterator(List<DocIdSet> sets) // throws IOException
        {
            curDoc = -1;
            heap = new Item[sets.Count];
            size = 0;

            foreach (DocIdSet set in sets)
            {
                heap[size++] = new Item(set.Iterator());
            }
            if (size == 0)
            {
                curDoc = DocIdSetIterator.NO_MORE_DOCS;
            }
        }

        public override int DocID()
        {
            return curDoc;
        }

        public override int NextDoc()
        {
            if (curDoc == DocIdSetIterator.NO_MORE_DOCS)
            {
                return DocIdSetIterator.NO_MORE_DOCS;
            }

            Item top = heap[0];
            while (true)
            {
                DocIdSetIterator topIter = top.Iter;
                int docid;
                if ((docid = topIter.NextDoc()) != DocIdSetIterator.NO_MORE_DOCS)
                {
                    top.Doc = docid;
                    HeapAdjust();
                }
                else
                {
                    heapRemoveRoot();
                    if (size == 0)
                        return (curDoc = DocIdSetIterator.NO_MORE_DOCS);
                }
                top = heap[0];
                int topDoc = top.Doc;
                if (topDoc > curDoc)
                {
                    return (curDoc = topDoc);
                }
            }
        }

        public override int Advance(int target) 
        {
            if (curDoc == DocIdSetIterator.NO_MORE_DOCS)
            {
                return DocIdSetIterator.NO_MORE_DOCS;
            }

            if (target <= curDoc)
            {
                target = curDoc + 1;
            }

            Item top = heap[0];
            while (true)
            {
                DocIdSetIterator topIter = top.Iter;
                int docid;
                if ((docid = topIter.Advance(target)) != DocIdSetIterator.NO_MORE_DOCS)
                {
                    top.Doc = docid;
                    HeapAdjust();
                }
                else
                {
                    heapRemoveRoot();
                    if (size == 0)
                    {
                        return (curDoc = DocIdSetIterator.NO_MORE_DOCS);
                    }
                }
                top = heap[0];
                int topDoc = top.Doc;
                if (topDoc >= target)
                {
                    return (curDoc = topDoc);
                }
            }
        }

        // Organize subScorers into a min heap with scorers generating the earlest document on top.
        //  
        //  private final void heapify() {
        //      int size = _size;
        //      for (int i=(size>>1)-1; i>=0; i--)
        //          heapAdjust(i);
        //  }
        //  
        //   The subtree of subScorers at root is a min heap except possibly for its root element.
        //   * Bubble the root down as required to make the subtree a heap.
        private void HeapAdjust()
        {
            Item[] heap = this.heap;
            Item top = heap[0];
            int doc = top.Doc;
            int size = this.size;
            int i = 0;

            while (true)
            {
                int lchild = (i << 1) + 1;

                if (lchild >= size)
                {
                    break;
                }

                Item left = heap[lchild];
                int ldoc = left.Doc;

                int rchild = lchild + 1;
                if (rchild < size)
                {
                    Item right = heap[rchild];
                    int rdoc = right.Doc;

                    if (rdoc <= ldoc)
                    {
                        if (doc <= rdoc)
                        {
                            break;
                        }

                        heap[i] = right;
                        i = rchild;
                        continue;
                    }
                }

                if (doc <= ldoc)
                {
                    break;
                }

                heap[i] = left;
                i = lchild;
            }
            heap[i] = top;
        }

        // Remove the root Scorer from subScorers and re-establish it as a heap
        private void heapRemoveRoot()
        {
            size--;
            if (size > 0)
            {
                Item tmp = heap[0];
                heap[0] = heap[size];
                heap[size] = tmp; // keep the finished iterator at the end for debugging
                HeapAdjust();
            }
        }
    }
}