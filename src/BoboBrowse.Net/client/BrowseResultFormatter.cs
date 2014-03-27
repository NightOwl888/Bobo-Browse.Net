using System.Collections.Generic;
using System.Text;
using BoboBrowse.Api;

namespace BoboBrowse.client
{
    public class BrowseResultFormatter
    {
        internal static string formatResults(BrowseResult res)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(res.NumHits);
            sb.Append(" hits out of ");
            sb.Append(res.TotalDocs);
            sb.Append(" docs\n");
            BrowseHit[] hits = res.Hits;
            Dictionary<string, IFacetAccessible> map = res.FacetMap;
            Dictionary<string, IFacetAccessible>.KeyCollection keys = map.Keys;
            foreach (string key in keys)
            {
                IFacetAccessible fa = map[key];
                sb.Append(key + "\n");
                IEnumerable<BrowseFacet> lf = fa.GetFacets();
                foreach (BrowseFacet bf in lf)
                {
                    sb.Append("\t" + bf + "\n");
                }
            }
            foreach (BrowseHit hit in hits)
            {
                sb.Append("------------\n");
                sb.Append(FormatHit(hit));
                sb.Append("\n");
            }
            sb.Append("*****************************\n");
            return sb.ToString();
        }

        internal static StringBuilder FormatHit(BrowseHit hit)
        {
            StringBuilder sb = new StringBuilder();
            Dictionary<string, string[]> fields = hit.FieldValues;
            Dictionary<string, string[]>.KeyCollection keys = fields.Keys;
            foreach (string key in keys)
            {
                sb.Append("\t" + key + " :");
                string[] values = fields[key];
                foreach (string value in values)
                {
                    sb.Append(" " + value);
                }
                sb.Append("\n");
            }
            return sb;
        }
    }

}