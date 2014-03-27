using System;
using System.Collections.Generic;

namespace com.browseengine.bobo.service
{


	using BrowseFacet = com.browseengine.bobo.api.BrowseFacet;
	using BrowseHit = com.browseengine.bobo.api.BrowseHit;
	using BrowseResult = com.browseengine.bobo.api.BrowseResult;
	using FacetAccessible = com.browseengine.bobo.api.FacetAccessible;
	using MappedFacetAccessible = com.browseengine.bobo.api.MappedFacetAccessible;
	using Converter = com.thoughtworks.xstream.converters.Converter;
	using MarshallingContext = com.thoughtworks.xstream.converters.MarshallingContext;
	using UnmarshallingContext = com.thoughtworks.xstream.converters.UnmarshallingContext;
	using HierarchicalStreamReader = com.thoughtworks.xstream.io.HierarchicalStreamReader;
	using HierarchicalStreamWriter = com.thoughtworks.xstream.io.HierarchicalStreamWriter;

	public class BrowseResultConverter : Converter
	{

		public virtual void marshal(object obj, HierarchicalStreamWriter writer, MarshallingContext ctx)
		{
			BrowseResult result =(BrowseResult)obj;
			writer.addAttribute("numhits", Convert.ToString(result.getNumHits()));
			writer.addAttribute("totaldocs", Convert.ToString(result.getTotalDocs()));
			writer.addAttribute("time", Convert.ToString(result.getTime()));

			writer.startNode("facets");
			List<KeyValuePair<string, FacetAccessible>> facetAccessors =result.getFacetMap().entrySet();

			writer.addAttribute("count", Convert.ToString(facetAccessors.size()));

			foreach (KeyValuePair<string, FacetAccessible> entry in facetAccessors)
			{
				string choiceName =entry.Key;
				FacetAccessible facetAccessor = entry.Value;

				List<BrowseFacet> facetList = facetAccessor.getFacets();

				writer.startNode("facet");
				writer.addAttribute("name", choiceName);

				writer.addAttribute("facetcount", Convert.ToString(facetList.Count));

				foreach (BrowseFacet facet in facetList)
				{
					writer.startNode("facet-value");
					writer.addAttribute("value", Convert.ToString(facet.Value));
					writer.addAttribute("count", Convert.ToString(facet.getHitCount()));
					writer.endNode();
				}
				writer.endNode();
			}
			writer.endNode();
			writer.startNode("hits");
			BrowseHit[] hits =result.getHits();

			writer.addAttribute("length", Convert.ToString(hits==null ? 0 : hits.Length));

			foreach (BrowseHit hit in hits)
			{
				ctx.convertAnother(hit);
			}

			writer.endNode();
		}

		public virtual object unmarshal(HierarchicalStreamReader reader, UnmarshallingContext ctx)
		{
			BrowseResult res =new BrowseResult();

			string numHitsString =reader.getAttribute("numhits");
			if (numHitsString!=null)
			{
				res.setNumHits(Convert.ToInt32(numHitsString));
			}

			string totalDocsString =reader.getAttribute("totaldocs");
			if (totalDocsString!=null)
			{
				res.setTotalDocs(Convert.ToInt32(totalDocsString));
			}

			string timeString =reader.getAttribute("time");
			if (timeString!=null)
			{
				res.setTime(Convert.ToInt64(timeString));
			}

			while (reader.hasMoreChildren())
			{
				reader.moveDown();
				if ("facets".Equals(reader.getNodeName()))
				{
					Dictionary<string, FacetAccessible> facetMap = new Dictionary<string, FacetAccessible>();
					string facetCountString = reader.getAttribute("count");
					if (facetCountString!=null)
					{
						int count = Convert.ToInt32(facetCountString);
						if (count > 0)
						{
							for (int i=0;i<count;++i)
							{
								reader.moveDown();
								string name = reader.getAttribute("name");
								string countStr = reader.getAttribute("facetcount");
								int fcount = 0;
								if (countStr!=null)
								{
									fcount = Convert.ToInt32(countStr);
								}
								BrowseFacet[] facets = new BrowseFacet[fcount];
								for (int k=0;k<fcount;++k)
								{
									facets[k]=new BrowseFacet();
									reader.moveDown();
									string valueString =reader.getAttribute("value");
									facets[k].Value = valueString;

									string countString =reader.getAttribute("count");
									if (countString!=null)
									{
										facets[k].setHitCount(Convert.ToInt32(countString));
									}
									reader.moveUp();
								}
								facetMap.put(name,new MappedFacetAccessible(facets));
								reader.moveUp();
							}
							res.addAll(facetMap);
						}
					}
				}
				else if ("hits".Equals(reader.getNodeName()))
				{
					string countStr = reader.getAttribute("length");
					int hitLen = Convert.ToInt32(countStr);
					BrowseHit[] hits = new BrowseHit[hitLen];
					for (int i = 0; i< hitLen; ++i)
					{
						hits[i]=(BrowseHit)ctx.convertAnother(res, typeof(BrowseHit));
					}
					res.setHits(hits);
				}
				reader.moveUp();
			}
			return res;
		}

		public virtual bool canConvert(System.Type clazz)
		{
			return typeof(BrowseResult).Equals(clazz);
		}

	}

}