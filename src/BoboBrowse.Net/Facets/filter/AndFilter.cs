using System.Collections.Generic;
using System.Linq;
using Lucene.Net.Index;
using Lucene.Net.Search;
using LuceneExt.Impl;

namespace BoboBrowse.Facets.filter
{
    public class AndFilter : Filter
    {
        private readonly ICollection<Filter> filters;

        public AndFilter(ICollection<Filter> filters)
        {
            this.filters = filters;
        }

        public override DocIdSet GetDocIdSet(IndexReader reader)
        {
            if (filters.Count == 1)
            {
                return filters.First().GetDocIdSet(reader);
            }
            else
            {
                List<DocIdSet> list = new List<DocIdSet>(filters.Count);
                foreach (Filter f in filters)
                {
                    list.Add(f.GetDocIdSet(reader));
                }
                return new AndDocIdSet(list);
            }
        }
    }
}