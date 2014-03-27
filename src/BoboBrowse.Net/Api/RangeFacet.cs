using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BoboBrowse.Facets.data;
using Lucene.Net.Documents;

namespace BoboBrowse.Api
{
    public class RangeFacet : BrowseFacet
    {
        public object Lower { get; set; }
        public object Upper { get; set; }

        internal void SetValues(object lower, object upper)
        {
            Lower = lower;
            Upper = upper;
            if (lower is DateTime)
            {
                lower = DateTools.DateToString((DateTime)lower, DateTools.Resolution.MINUTE);
                upper = DateTools.DateToString((DateTime)upper, DateTools.Resolution.MINUTE);
            }
            Value = string.Concat("[", lower, " TO ", upper, "]");
        }
    }
}
