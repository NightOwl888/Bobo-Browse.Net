using System;
using System.Collections.Generic;
using System.Linq;
using BoboBrowse.Api;
using BoboBrowse.Facets.impl;
using Lucene.Net.Util;
using System.Collections;

namespace BoboBrowse.Util
{
    ///<summary>@author ymatsuda</summary>
    public class ListMerger
    {
        public class MergedIterator<T>
        {
            private class IteratorNode
            {
                private readonly IEnumerator<T> iterator;
                public T CurVal;

                public IteratorNode(IEnumerator<T> iterator)
                {
                    this.iterator = iterator;
                    CurVal = default(T);
                }

                public bool Fetch()
                {
                    if (iterator.MoveNext())
                    {
                        CurVal = iterator.Current;
                        return true;
                    }
                    CurVal = default(T);
                    return false;
                }
            }

            private readonly PriorityQueue queue;

            private class MergedQueue : PriorityQueue
            {
                private readonly IComparer<T> comparator;

                public MergedQueue(int length, IComparer<T> comparator)
                {
                    this.comparator = comparator;
                    this.Initialize(length);
                }

                public override bool LessThan(object o1, object o2)
                {
                    T v1 = ((IteratorNode)o1).CurVal;
                    T v2 = ((IteratorNode)o2).CurVal;

                    return (comparator.Compare(v1, v2) < 0);
                }
            }

            private MergedIterator(int length, IComparer<T> comparator)
            {
                queue = new MergedQueue(length, comparator);
            }

            public MergedIterator(ICollection<IEnumerable<T>> iterators, IComparer<T> comparator)
                : this(iterators.Count, comparator)
            {
                foreach (IEnumerator<T> iterator in iterators)
                {
                    IteratorNode ctx = new IteratorNode(iterator);
                    if (ctx.Fetch())
                    {
                        queue.Insert(ctx);
                    }
                }
            }

            public MergedIterator(IEnumerable<T>[] iterators, IComparer<T> comparator)
                : this(iterators.Length, comparator)
            {
                foreach (IEnumerator<T> iterator in iterators)
                {
                    IteratorNode ctx = new IteratorNode(iterator);
                    if (ctx.Fetch())
                    {
                        queue.Insert(ctx);
                    }
                }
            }

            public virtual bool HasNext()
            {
                return queue.Size() > 0;
            }

            public virtual T Next()
            {
                IteratorNode ctx = (IteratorNode)queue.Top();
                T val = ctx.CurVal;
                if (ctx.Fetch())
                {
                    queue.AdjustTop();
                }
                else
                {
                    queue.Pop();
                }
                return val;
            }

            public virtual void Remove()
            {
                throw new NotSupportedException();
            }
        }

        private ListMerger()
        {
        }

        public static MergedIterator<T> MergeLists<T>(IEnumerable<T>[] iterators, IComparer<T> comparator)
        {
            return new MergedIterator<T>(iterators, comparator);
        }

        public static MergedIterator<T> MergeLists<T>(ICollection<IEnumerable<T>> iterators, IComparer<T> comparator)
        {
            return new MergedIterator<T>(iterators, comparator);
        }

        public static List<T> MergeLists<T>(int offset, int count, IEnumerable<T>[] iterators, IComparer<T> comparator)
        {
            return MergeLists(offset, count, new MergedIterator<T>(iterators, comparator));
        }

        public static List<T> MergeLists<T>(int offset, int count, ICollection<IEnumerable<T>> iterators, IComparer<T> comparator)
        {
            return MergeLists(offset, count, new MergedIterator<T>(iterators, comparator));
        }

        private static List<T> MergeLists<T>(int offset, int count, MergedIterator<T> mergedIter)
        {
            for (int c = 0; c < offset && mergedIter.HasNext(); c++)
            {
                var x = mergedIter.Next();
            }

            List<T> mergedList = new List<T>();

            for (int c = 0; c < count && mergedIter.HasNext(); c++)
            {
                mergedList.Add(mergedIter.Next());
            }

            return mergedList;
        }

        public static IComparer<BrowseFacet> FACET_VAL_COMPARATOR = new BoboBrowse.Api.MultiBoboBrowser.BrowseFacetValueComparator();

        public static Dictionary<string, IFacetAccessible> MergeSimpleFacetContainers(ICollection<Dictionary<string, IFacetAccessible>> subMaps, BrowseRequest req)
        {
            Dictionary<string, Dictionary<object, BrowseFacet>> counts = new Dictionary<string, Dictionary<object, BrowseFacet>>();
            foreach (Dictionary<string, IFacetAccessible> subMap in subMaps)
            {
                foreach (KeyValuePair<string, IFacetAccessible> entry in subMap)
                {
                    Dictionary<object, BrowseFacet> count = counts[entry.Key];
                    if (count == null)
                    {
                        count = new Dictionary<object, BrowseFacet>();
                        counts.Add(entry.Key, count);
                    }
                    foreach (BrowseFacet facet in entry.Value.GetFacets())
                    {
                        object val = facet.Value;
                        BrowseFacet oldValue = count[val];
                        if (oldValue == null)
                        {
                            count.Add(val, new BrowseFacet(val, facet.HitCount));
                        }
                        else
                        {
                            oldValue.HitCount = oldValue.HitCount + facet.HitCount;
                        }
                    }
                }
            }

            Dictionary<string, IFacetAccessible> mergedFacetMap = new Dictionary<string, IFacetAccessible>();

            foreach (string facet in counts.Keys)
            {
                FacetSpec fs = req.GetFacetSpec(facet);

                FacetSpec.FacetSortSpec sortSpec = fs.OrderBy;

                IComparer<BrowseFacet> comparator;
                if (FacetSpec.FacetSortSpec.OrderValueAsc.Equals(sortSpec))
                {
                    comparator = FACET_VAL_COMPARATOR;
                }
                else if (FacetSpec.FacetSortSpec.OrderHitsDesc.Equals(sortSpec))
                {
                    comparator = FacetHitcountComparatorFactory.FACET_HITS_COMPARATOR;
                }
                else
                {
                    comparator = fs.CustomComparatorFactory.NewComparator();
                }

                Dictionary<object, BrowseFacet> facetValueCounts = counts[facet];
                BrowseFacet[] facetArray = facetValueCounts.Values.ToArray();
                System.Array.Sort(facetArray, comparator);

                int numToShow = facetArray.Length;
                if (req != null)
                {
                    FacetSpec fspec = req.GetFacetSpec(facet);
                    if (fspec != null)
                    {
                        int maxCount = fspec.MaxCount;
                        if (maxCount > 0)
                        {
                            numToShow = Math.Min(maxCount, numToShow);
                        }
                    }
                }

                BrowseFacet[] facets;
                if (numToShow == facetArray.Length)
                {
                    facets = facetArray;
                }
                else
                {
                    facets = new BrowseFacet[numToShow];
                    System.Array.Copy(facetArray, 0, facets, 0, numToShow);
                }

                MappedFacetAccessible mergedFacetAccessible = new MappedFacetAccessible(facets);
                mergedFacetMap.Add(facet, mergedFacetAccessible);
            }
            return mergedFacetMap;
        }
    }
}