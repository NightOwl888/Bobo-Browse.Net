﻿// Version compatibility level: 3.1.0
namespace BoboBrowse.Net.Facets.Impl
{
    using BoboBrowse.Net.Facets.Data;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    public class CombinedLongFacetIterator : LongFacetIterator
    {
        public class LongIteratorNode
        {
            public LongFacetIterator _iterator;
            public long _curFacet;
            public int _curFacetCount;

            public LongIteratorNode(LongFacetIterator iterator)
            {
                _iterator = iterator;
                _curFacet = TermLongList.VALUE_MISSING;
                _curFacetCount = 0;
            }

            public bool Fetch(int minHits)
            {
                if (minHits > 0)
                    minHits = 1;
                if ((_curFacet = _iterator.NextLong(minHits)) != TermLongList.VALUE_MISSING)
                {
                    _curFacetCount = _iterator.Count;
                    return true;
                }
                _curFacet = TermLongList.VALUE_MISSING;
                _curFacetCount = 0;
                return false;
            }

            public string Peek()//bad
            {
                throw new NotSupportedException();
                //      if(_iterator.hasNext()) 
                //      {
                //        return _iterator.getFacet();
                //      }
                //      return null;
            }
        }

        private readonly LongFacetPriorityQueue _queue;

        private IList<LongFacetIterator> _iterators;

        private CombinedLongFacetIterator(int length)
        {
            _queue = new LongFacetPriorityQueue();
            _queue.Initialize(length);
        }

        public CombinedLongFacetIterator(IList<LongFacetIterator> iterators)
            : this(iterators.Count)
        {
            _iterators = iterators;
            foreach (LongFacetIterator iterator in iterators)
            {
                LongIteratorNode node = new LongIteratorNode(iterator);
                if (node.Fetch(1))
                    _queue.Add(node);
            }
            _facet = TermLongList.VALUE_MISSING;
            _count = 0;
        }

        public CombinedLongFacetIterator(List<LongFacetIterator> iterators, int minHits)
            : this(iterators.Count)
        {
            _iterators = iterators;
            foreach (LongFacetIterator iterator in iterators)
            {
                LongIteratorNode node = new LongIteratorNode(iterator);
                if (node.Fetch(minHits))
                    _queue.Add(node);
            }
            _facet = TermLongList.VALUE_MISSING;
            _count = 0;
        }

        /* (non-Javadoc)
         * @see com.browseengine.bobo.api.FacetIterator#getFacet()
         */
        public virtual string GetFacet()
        {
            if (_facet == TermLongList.VALUE_MISSING) return null;
            return Format(_facet);
        }
        public override string Format(long val)
        {
            return _iterators[0].Format(val);
        }
        public override string Format(Object val)
        {
            return _iterators[0].Format(val);
        }
        /* (non-Javadoc)
         * @see com.browseengine.bobo.api.FacetIterator#getFacetCount()
         */
        public virtual int FacetCount
        {
            get { return _count; }
        }

        /* (non-Javadoc)
         * @see com.browseengine.bobo.api.FacetIterator#next()
         */
        public override string Next()
        {
            if (!HasNext())
                throw new IndexOutOfRangeException("No more facets in this iteration");

            LongIteratorNode node = _queue.Top();

            _facet = node._curFacet;
            long next = TermLongList.VALUE_MISSING;
            _count = 0;
            while (HasNext())
            {
                node = _queue.Top();
                next = node._curFacet;
                if ((next != TermLongList.VALUE_MISSING) && (next != _facet))
                {
                    return Format(_facet);
                }
                _count += node._curFacetCount;
                if (node.Fetch(1))
                    _queue.UpdateTop();
                else
                    _queue.Pop();
            }
            return null;
        }

        /**
         * This version of the next() method applies the minHits from the _facet spec before returning the _facet and its hitcount
         * @param minHits the minHits from the _facet spec for CombinedFacetAccessible
         * @return        The next _facet that obeys the minHits 
         */
        public override string Next(int minHits)
        {
            int qsize = _queue.Size();
            if (qsize == 0)
            {
                _facet = TermLongList.VALUE_MISSING;
                _count = 0;
                return null;
            }

            LongIteratorNode node = _queue.Top();
            _facet = node._curFacet;
            _count = node._curFacetCount;
            while (true)
            {
                if (node.Fetch(minHits))
                {
                    node = _queue.UpdateTop();
                }
                else
                {
                    _queue.Pop();
                    if (--qsize > 0)
                    {
                        node = _queue.Top();
                    }
                    else
                    {
                        // we reached the end. check if this _facet obeys the minHits
                        if (_count < minHits)
                        {
                            _facet = TermLongList.VALUE_MISSING;
                            _count = 0;
                            return null;
                        }
                        break;
                    }
                }
                long next = node._curFacet;
                if (next != _facet)
                {
                    // check if this _facet obeys the minHits
                    if (_count >= minHits)
                        break;
                    // else, continue iterating to the next _facet
                    _facet = next;
                    _count = node._curFacetCount;
                }
                else
                {
                    _count += node._curFacetCount;
                }
            }
            return Format(_facet);
        }

        /* (non-Javadoc)
         * @see java.util.Iterator#hasNext()
         */
        public virtual bool HasNext()
        {
            return (_queue.Size() > 0);
        }

        /* (non-Javadoc)
         * @see java.util.Iterator#remove()
         */
        public virtual void Remove()
        {
            throw new NotSupportedException("remove() method not supported for Facet Iterators");
        }

        /**
         * Lucene PriorityQueue
         * 
         */
        public class LongFacetPriorityQueue
        {
            private int size;
            private int maxSize;
            protected LongIteratorNode[] heap;

            /** Subclass constructors must call this. */
            public void Initialize(int maxSize)
            {
                size = 0;
                int heapSize;
                if (0 == maxSize)
                    // We allocate 1 extra to avoid if statement in top()
                    heapSize = 2;
                else
                    heapSize = maxSize + 1;
                heap = new LongIteratorNode[heapSize];
                this.maxSize = maxSize;
            }

            public void Put(LongIteratorNode element)
            {
                size++;
                heap[size] = element;
                UpHeap();
            }

            public LongIteratorNode Add(LongIteratorNode element)
            {
                size++;
                heap[size] = element;
                UpHeap();
                return heap[1];
            }

            public virtual bool Insert(LongIteratorNode element)
            {
                return InsertWithOverflow(element) != element;
            }

            public virtual LongIteratorNode InsertWithOverflow(LongIteratorNode element)
            {
                if (size < maxSize)
                {
                    Put(element);
                    return null;
                }
                else if (size > 0 && !(element._curFacet < heap[1]._curFacet))
                {
                    LongIteratorNode ret = heap[1];
                    heap[1] = element;
                    AdjustTop();
                    return ret;
                }
                else
                {
                    return element;
                }
            }

            /** Returns the least element of the PriorityQueue in constant time. */
            public LongIteratorNode Top()
            {
                // We don't need to check size here: if maxSize is 0,
                // then heap is length 2 array with both entries null.
                // If size is 0 then heap[1] is already null.
                return heap[1];
            }

            /**
             * Removes and returns the least element of the PriorityQueue in log(size)
             * time.
             */
            public LongIteratorNode Pop()
            {
                if (size > 0)
                {
                    LongIteratorNode result = heap[1]; // save first value
                    heap[1] = heap[size]; // move last to first
                    heap[size] = null; // permit GC of objects
                    size--;
                    DownHeap(); // adjust heap
                    return result;
                }
                else
                    return null;
            }

            public void AdjustTop()
            {
                DownHeap();
            }

            public LongIteratorNode UpdateTop()
            {
                DownHeap();
                return heap[1];
            }

            /** Returns the number of elements currently stored in the PriorityQueue. */
            public int Size()
            {
                return size;
            }

            /** Removes all entries from the PriorityQueue. */
            public void Clear()
            {
                for (int i = 0; i <= size; i++)
                {
                    heap[i] = null;
                }
                size = 0;
            }

            private void UpHeap()
            {
                int i = size;
                LongIteratorNode node = heap[i]; // save bottom node
                int j = (int)(((uint)i) >> 1);
                while (j > 0 && (node._curFacet < heap[j]._curFacet))
                {
                    heap[i] = heap[j]; // shift parents down
                    i = j;
                    j = (int)(((uint)j) >> 1);
                }
                heap[i] = node; // install saved node
            }

            private void DownHeap()
            {
                int i = 1;
                LongIteratorNode node = heap[i]; // save top node
                int j = i << 1; // find smaller child
                int k = j + 1;
                if (k <= size && (heap[k]._curFacet < heap[j]._curFacet))
                {
                    j = k;
                }
                while (j <= size && (heap[j]._curFacet < node._curFacet))
                {
                    heap[i] = heap[j]; // shift up child
                    i = j;
                    j = i << 1;
                    k = j + 1;
                    if (k <= size && (heap[k]._curFacet < heap[j]._curFacet))
                    {
                        j = k;
                    }
                }
                heap[i] = node; // install saved node
            }
        }

        public override long NextLong()
        {
            if (!HasNext())
                throw new IndexOutOfRangeException("No more facets in this iteration");

            LongIteratorNode node = _queue.Top();

            _facet = node._curFacet;
            long next = TermLongList.VALUE_MISSING;
            _count = 0;
            while (HasNext())
            {
                node = _queue.Top();
                next = node._curFacet;
                if ((next != TermLongList.VALUE_MISSING) && (next != _facet))
                {
                    return _facet;
                }
                _count += node._curFacetCount;
                if (node.Fetch(1))
                    _queue.UpdateTop();
                else
                    _queue.Pop();
            }
            return TermLongList.VALUE_MISSING;
        }

        public override long NextLong(int minHits)
        {
            int qsize = _queue.Size();
            if (qsize == 0)
            {
                _facet = TermLongList.VALUE_MISSING;
                _count = 0;
                return TermLongList.VALUE_MISSING;
            }

            LongIteratorNode node = _queue.Top();
            _facet = node._curFacet;
            _count = node._curFacetCount;
            while (true)
            {
                if (node.Fetch(minHits))
                {
                    node = _queue.UpdateTop();
                }
                else
                {
                    _queue.Pop();
                    if (--qsize > 0)
                    {
                        node = _queue.Top();
                    }
                    else
                    {
                        // we reached the end. check if this _facet obeys the minHits
                        if (_count < minHits)
                        {
                            _facet = TermLongList.VALUE_MISSING;
                            _count = 0;
                        }
                        break;
                    }
                }
                long next = node._curFacet;
                if (next != _facet)
                {
                    // check if this _facet obeys the minHits
                    if (_count >= minHits)
                        break;
                    // else, continue iterating to the next _facet
                    _facet = next;
                    _count = node._curFacetCount;
                }
                else
                {
                    _count += node._curFacetCount;
                }
            }
            return _facet;
        }
    }
}

  