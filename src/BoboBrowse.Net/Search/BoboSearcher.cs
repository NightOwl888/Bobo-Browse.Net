//* 
//* Copyright (C) 2005-2006  John Wang
//*
//* This library is free software; you can redistribute it and/or
//* modify it under the terms of the GNU Lesser General Public
//* License as published by the Free Software Foundation; either
//* version 2.1 of the License, or (at your option) any later version.
//*
//* This library is distributed in the hope that it will be useful,
//* but WITHOUT ANY WARRANTY; without even the implied warranty of
//* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//* Lesser General Public License for more details.
//*
//* You should have received a copy of the GNU Lesser General Public
//* License along with this library; if not, write to the Free Software
//* Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
//* 
//* To contact the project administrators for the bobo-browse project, 
//* please go to https://sourceforge.net/projects/bobo-browse/, or 
//* send mail to owner@browseengine.com. 

namespace BoboBrowse.Net.Search
{
    using System;
using System.Collections.Generic;
using System.Linq;
using BoboBrowse.Api;
using Lucene.Net.Index;
using Lucene.Net.Search;

namespace BoboBrowse.Search
{
    public class BoboSearcher : IndexSearcher
    {
        protected internal ICollection<FacetHitCollector> facetCollectors;

        public BoboSearcher(BoboIndexReader reader)
            : base(reader)
        {
            facetCollectors = new LinkedList<FacetHitCollector>();
        }

        public virtual void SetFacetHitCollectorList(List<FacetHitCollector> facetHitCollectors)
        {
            if (facetHitCollectors != null)
            {
                facetCollectors = facetHitCollectors;
            }
        }

        ///    
        ///	 <summary> * This method validates the doc against any multi-select enabled fields. </summary>
        ///	 * <param name="docid"> </param>
        ///	 * <returns> false if not all a match on all fields. Facet count may still be collected. </returns>
        ///	 
        private static bool validateAndIncrement(int docid, FacetHitCollector[] facetCollectors, bool doValidate)
        {
            if (doValidate)
            {
                int misses = 0;
                int _marker = -1;
                for (int i = 0; i < facetCollectors.Length; ++i)
                {
                    FacetHitCollector facetCollector = facetCollectors[i];
                    if (facetCollector.PostDocIDSetIterator == null)
                    {
                        continue;
                    }
                    if (facetCollector.PostDocIDSetIterator.SkipTo(docid))
                    {
                        if (facetCollector.PostDocIDSetIterator.Doc() != docid)
                        {
                            misses++;
                            if (misses > 1)
                            {
                                return false;
                            }
                            else
                            {
                                _marker = i;
                            }
                        }
                    }
                    else
                    {
                        misses++;
                        if (misses > 1)
                        {
                            return false;
                        }
                        else
                        {
                            _marker = i;
                        }
                    }
                }
                if (misses == 1)
                {
                    for (int i = 0; i < facetCollectors.Length; ++i)
                    {
                        FacetHitCollector facetCollector = facetCollectors[i];
                        if (_marker == i)
                        {
                            facetCollector.FacetCountCollector.Collect(docid);
                        }
                    }
                }
                else
                {
                    foreach (FacetHitCollector facetCollector in facetCollectors)
                    {
                        facetCollector.FacetCountCollector.Collect(docid);
                    }
                }
                return misses == 0;
            }
            else
            {
                foreach (FacetHitCollector facetCollector in facetCollectors)
                {
                    facetCollector.FacetCountCollector.Collect(docid);
                }
                return true;
            }
        }

        public override void Search(Weight weight, Filter filter, Collector results)
        {
            IndexReader reader = GetIndexReader();

            bool doValidate = false;
            FacetHitCollector[] facetCollectors = this.facetCollectors.ToArray();
            foreach (FacetHitCollector facetCollector in facetCollectors)
            {
                if (facetCollector.PostDocIDSetIterator != null)
                {
                    doValidate = true;
                    break;
                }
            }

            Scorer scorer = weight.Scorer(reader, true, false);
            if (scorer == null)
            {
                return;
            }

            results.SetScorer(scorer);

            if (filter == null)
            {
                while (scorer.Next())
                {
                    int doc = scorer.Doc();
                    if (validateAndIncrement(doc, facetCollectors, doValidate))
                    {
                        results.Collect(doc);
                    }
                }
                return;
            }

            DocIdSetIterator filterDocIdIterator = filter.GetDocIdSet(reader).Iterator(); // CHECKME: use ConjunctionScorer here?

            bool more = filterDocIdIterator.Next() && scorer.SkipTo(filterDocIdIterator.Doc());

            while (more)
            {
                int filterDocId = filterDocIdIterator.Doc();
                if (filterDocId > scorer.Doc() && !scorer.SkipTo(filterDocId))
                {
                    more = false;
                }
                else
                {
                    int scorerDocId = scorer.Doc();
                    if (scorerDocId == filterDocId) // permitted by filter
                    {
                        if (validateAndIncrement(scorerDocId, facetCollectors, doValidate))
                        {
                            results.Collect(scorerDocId);
                        }
                        more = filterDocIdIterator.Next();
                    }
                    else
                    {
                        more = filterDocIdIterator.SkipTo(scorerDocId);
                    }
                }
            }
        }
    }
}