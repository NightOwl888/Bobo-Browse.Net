using System.Collections.Generic;
using Lucene.Net.Index;
using Lucene.Net.Search;
using LuceneExt.Impl;

namespace BoboBrowse.Facets.filter
{
    public class OrFilter : Filter
    {
        private readonly List<Filter> _filters;

        public OrFilter(List<Filter> filters)
        {
            _filters = filters;
        }

        public override DocIdSet GetDocIdSet(IndexReader reader)
        {
            if (_filters.Count == 1)
            {
                return _filters[0].GetDocIdSet(reader);
            }
            else
            {
                List<DocIdSet> list = new List<DocIdSet>(_filters.Count);
                foreach (Filter f in _filters)
                {
                    list.Add(f.GetDocIdSet(reader));
                }
                return new OrDocIdSet(list);
            }
        }
    }
}