using System;
using System.Collections.Generic;
using System.IO;
using log4net;
using Lucene.Net.Search;

namespace LuceneExt.Impl
{
    [Serializable]
    public class OrDocIdSet : ImmutableDocSet
    {
        private const int INVALID = -1;

        private static log4net.ILog log = LogManager.GetLogger(typeof(OrDocIdSet));

        [Serializable]
        public class AescDocIdSetComparator : IComparer<DocIdSetIterator>
        {
            public virtual int Compare(DocIdSetIterator o1, DocIdSetIterator o2)
            {
                return o1.DocID() - o2.DocID();
            }
        }

        internal List<DocIdSet> sets;

        private int size = INVALID;

        public OrDocIdSet(List<DocIdSet> docSets)
        {
            this.sets = docSets;
            int size = 0;
            if (sets != null)
            {
                foreach (DocIdSet set in sets)
                {
                    if (set != null)
                    {
                        size++;
                    }
                }
            }
        }

        public override DocIdSetIterator Iterator()
        {
            return new OrDocIdSetIterator(sets);
            //    
            //    List<DocIdSetIterator> list = new ArrayList<DocIdSetIterator>(sets.size());
            //    for (DocIdSet set : sets)
            //    {
            //      list.add(set.Iterator());
            //    }
            //    return new DisjunctionDISI(list);
            //    
        }


        ///<summary>Find existence in the set with index
        ///   * NOTE :  Expensive call. Avoid. </summary>
        ///   * <param name="val"> value to find the index for </param>
        ///   * <returns> index where the value is </returns>
        public override int FindWithIndex(int val)
        {
            DocIdSetIterator finder = new OrDocIdSetIterator(sets);
            int cursor = -1;
            try
            {
                int docid;

                while ((docid = finder.NextDoc()) != DocIdSetIterator.NO_MORE_DOCS)
                {
                    if (docid > val)
                    {
                        return -1;
                    }
                    else if (docid == val)
                    {
                        return ++cursor;
                    }
                    else
                    {
                        ++cursor;
                    }
                }
            }
            catch (IOException e)
            {
                return -1;
            }
            return -1;
        }

        public override int Size()
        {
            if (size == INVALID)
            {
                size = 0;
                DocIdSetIterator it = this.Iterator();

                try
                {
                    while (it.NextDoc() != DocIdSetIterator.NO_MORE_DOCS)
                        size++;
                }
                catch (IOException e)
                {
                    log.Error(e.StackTrace);
                    size = INVALID;
                }
            }
            return size;
        }
    }
}