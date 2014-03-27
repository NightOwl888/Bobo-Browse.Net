#region using

using System;
using System.Linq;
using System.Collections.Generic;
using BoboBrowse.Api;
using BoboBrowse.Facets.filter;
using BoboBrowse.LangUtils;
using C5;
using Lucene.Net.Search;
using System.Collections;

#endregion

namespace BoboBrowse.Facets
{
    ///<summary>FacetHandler definition</summary>
    public abstract class FacetHandler : ICloneable
    {
        protected internal readonly string name;
        private readonly List<string> dependsOn;
        private readonly Dictionary<string, FacetHandler> dependedFacetHandlers;
        private TermCountSize termCountSize;

        public enum TermCountSize
        {
            Small,
            Medium,
            Large
        }

        protected FacetHandler(string name)
            : this(name, null)
        {
        }

        ///	 <summary> * Constructor </summary>
        ///	 * <param name="name"> name </param>
        ///	 * <param name="dependsOn"> Set of names of facet handlers this facet handler depend on for loading </param>
        protected FacetHandler(string name, IEnumerable<string> dependsOn)
        {
            this.name = name;
            this.dependsOn = new List<string>();
            if (dependsOn != null)
            {
                this.dependsOn.AddRange(dependsOn);
            }
            dependedFacetHandlers = new Dictionary<string, FacetHandler>();
            termCountSize = TermCountSize.Large;
        }

        public virtual void SetTermCountSize(string termCountSize)
        {
            SetTermCountSize((TermCountSize) Enum.Parse(typeof (TermCountSize), termCountSize.ToLower()));
        }

        public virtual void SetTermCountSize(TermCountSize termCountSize)
        {
            this.termCountSize = termCountSize;
        }

        public virtual TermCountSize GetTermCountSize()
        {
            return termCountSize;
        }

        ///	 <summary> * Gets the name </summary>
        ///	 * <returns> name </returns>
        public string Name
        {
            get { return name; }
        }

        ///    
        ///	 <summary> * Gets names of the facet handler this depends on </summary>
        ///	 * <returns> set of facet handler names </returns>
        ///	 
        public List<string> GetDependsOn()
        {
            return dependsOn;
        }

        ///    
        ///	 <summary> * Adds a list of depended facet handlers </summary>
        ///	 * <param name="facetHandler"> depended facet handler </param>
        ///	 
        public void PutDependedFacetHandler(FacetHandler facetHandler)
        {
            dependedFacetHandlers.Add(facetHandler.name, facetHandler);
        }

        ///    
        ///	 <summary> * Gets a depended facet handler </summary>
        ///	 * <param name="name"> facet handler name </param>
        ///	 * <returns> facet handler instance  </returns>
        ///	 
        public FacetHandler GetDependedFacetHandler(string name)
        {
            return dependedFacetHandlers[name];
        }

        ///    
        ///	 <summary> * Load information from an index reader, initialized by <seealso cref="BoboIndexReader"/> </summary>
        ///	 * <param name="reader"> reader </param>
        ///	 * <exception cref="IOException"> </exception>
        ///	 
        public abstract void Load(BoboIndexReader reader);

        private class CombinedFacetAccessible : IFacetAccessible
        {
            private readonly List<IFacetAccessible> list;
            private readonly FacetSpec fspec;

            internal CombinedFacetAccessible(FacetSpec fspec, List<IFacetAccessible> list)
            {
                this.list = list;
                this.fspec = fspec;
            }

            public override string ToString()
            {
                return "_list:" + list + " _fspec:" + fspec;
            }

            public virtual BrowseFacet GetFacet(string @value)
            {
                int sum = -1;
                object foundValue = null;
                if (list != null)
                {
                    foreach (IFacetAccessible facetAccessor in list)
                    {
                        BrowseFacet facet = facetAccessor.GetFacet(@value);
                        if (facet != null)
                        {
                            foundValue = facet.Value;
                            if (sum == -1)
                                sum = facet.HitCount;
                            else
                                sum += facet.HitCount;
                        }
                    }
                }
                if (sum == -1)
                    return null;
                return new BrowseFacet(foundValue, sum);
            }

            public virtual IEnumerable<BrowseFacet> GetFacets()
            {
                C5.IDictionary<object, BrowseFacet> facetMap;
                if (FacetSpec.FacetSortSpec.OrderValueAsc.Equals(fspec.OrderBy))
                {
                    facetMap = new TreeDictionary<object, BrowseFacet>();
                }
                else
                {
                    facetMap = new HashDictionary<object, BrowseFacet>();
                }

                foreach (IFacetAccessible facetAccessor in this.list)
                {
                    IEnumerator<BrowseFacet> iter = facetAccessor.GetFacets().GetEnumerator();
                    if (facetMap.Count == 0)
                    {
                        while (iter.MoveNext())
                        {
                            BrowseFacet facet = iter.Current;
                            facetMap.Add(facet.Value, facet);
                        }
                    }
                    else
                    {
                        while (iter.MoveNext())
                        {
                            BrowseFacet facet = iter.Current;
                            BrowseFacet existing = facetMap[facet.Value];
                            if (existing == null)
                            {
                                facetMap.Add(facet.Value, facet);
                            }
                            else
                            {
                                existing.HitCount = existing.HitCount + facet.HitCount;
                            }
                        }
                    }
                }

                List<BrowseFacet> list = new List<BrowseFacet>(facetMap.Values);
                // FIXME: we need to reorganize all that stuff with comparators
                Comparer comparer = new Comparer(System.Globalization.CultureInfo.InvariantCulture);
                if (FacetSpec.FacetSortSpec.OrderHitsDesc.Equals(fspec.OrderBy))
                {
                    list.Sort(
                        delegate(BrowseFacet f1, BrowseFacet f2)
                            {
                                int val = f2.HitCount - f1.HitCount;
                                if (val == 0)
                                {
                                    val = -(comparer.Compare(f1.Value, f2.Value));
                                }
                                return val;
                            }
                        );
                }
                return list;
            }
        }

        public virtual IFacetAccessible Merge(FacetSpec fspec, List<IFacetAccessible> facetList)
        {
            return new CombinedFacetAccessible(fspec, facetList);
        }

        public virtual void Load(BoboIndexReader reader, BoboIndexReader.WorkArea workArea)
        {
            Load(reader);
        }

        ///    
        ///	 <summary> * Gets a filter from a given selection </summary>
        ///	 * <param name="sel"> selection </param>
        ///	 * <returns> a filter </returns>
        ///	 * <exception cref="IOException">  </exception>
        ///	 * <exception cref="IOException"> </exception>
        ///	 
        public RandomAccessFilter BuildFilter(BrowseSelection sel)
        {
            string[] selections = sel.Values;
            string[] notSelections = sel.NotValues;
            Properties prop = sel.SelectionProperties;

            RandomAccessFilter filter = null;
            if (selections != null && selections.Length > 0)
            {
                if (sel.SelectionOperation == BrowseSelection.ValueOperation.ValueOperationAnd)
                {
                    filter = BuildRandomAccessAndFilter(selections, prop);
                    if (filter == null)
                    {
                        filter = EmptyFilter.GetInstance();
                    }
                }
                else
                {
                    filter = BuildRandomAccessOrFilter(selections, prop, false);
                    if (filter == null)
                    {
                        return EmptyFilter.GetInstance();
                    }
                }
            }

            if (notSelections != null && notSelections.Length > 0)
            {
                RandomAccessFilter notFilter = BuildRandomAccessOrFilter(notSelections, prop, true);
                if (filter == null)
                {
                    filter = notFilter;
                }
                else
                {
                    RandomAccessFilter andFilter =
                        new RandomAccessAndFilter(new RandomAccessFilter[] {filter, notFilter}.ToList());
                    filter = andFilter;
                }
            }

            return filter;
        }

        public abstract RandomAccessFilter BuildRandomAccessFilter(string @value, Properties selectionProperty);

        public virtual RandomAccessFilter BuildRandomAccessAndFilter(string[] vals, Properties prop)
        {
            List<RandomAccessFilter> filterList = new List<RandomAccessFilter>(vals.Length);

            foreach (string val in vals)
            {
                RandomAccessFilter f = BuildRandomAccessFilter(val, prop);
                if (f != null)
                {
                    filterList.Add(f);
                }
                else
                {
                    // there is no hit in this AND filter because this value has no hit
                    return null;
                }
            }
            if (filterList.Count == 0)
                return null;
            return new RandomAccessAndFilter(filterList);
        }

        public virtual RandomAccessFilter BuildRandomAccessOrFilter(string[] vals, Properties prop, bool isNot)
        {
            List<RandomAccessFilter> filterList = new List<RandomAccessFilter>(vals.Length);

            foreach (string val in vals)
            {
                RandomAccessFilter f = BuildRandomAccessFilter(val, prop);
                if (f != null && !(f is EmptyFilter))
                {
                    filterList.Add(f);
                }
            }

            RandomAccessFilter finalFilter;
            if (filterList.Count == 0)
            {
                finalFilter = EmptyFilter.GetInstance();
            }
            else
            {
                finalFilter = new RandomAccessOrFilter(filterList);
            }

            if (isNot)
            {
                finalFilter = new RandomAccessNotFilter(finalFilter);
            }
            return finalFilter;
        }

        ///    
        ///	 <summary> * Gets a FacetCountCollector </summary>
        ///	 * <param name="sel"> selection </param>
        ///	 * <param name="fspec"> facetSpec </param>
        ///	 * <returns> a FacetCountCollector </returns>
        ///	 
        public abstract IFacetCountCollector GetFacetCountCollector(BrowseSelection sel, FacetSpec fspec);

        ///    
        ///	 <summary> * Gets the field value </summary>
        ///	 * <param name="id"> doc </param>
        ///	 * <returns> array of field values </returns>
        ///	 * <seealso cref= #getFieldValue(int) </seealso>
        ///	 
        public abstract string[] GetFieldValues(int id);

        public abstract object[] GetRawFieldValues(int id);

        ///    
        ///	 <summary> * Gets a single field value </summary>
        ///	 * <param name="id"> doc </param>
        ///	 * <returns> first field value </returns>
        ///	 * <seealso cref= #getFieldValues(int) </seealso>
        ///	 
        public virtual string GetFieldValue(int id)
        {
            return GetFieldValues(id)[0];
        }

        ///    
        ///	 <summary> * builds a comparator to determine how sorting is done </summary>
        ///	 * <returns> a sort comparator </returns>
        ///	 
        public abstract ScoreDocComparator GetScoreDocComparator();

        public virtual object Clone()
        {
            throw new NotImplementedException("implement close"); // TODO: implement clonable
            //return base.clone();
        }
    }
}