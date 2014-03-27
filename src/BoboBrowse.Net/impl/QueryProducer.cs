using System.Collections.Generic;
using BoboBrowse.Api;
using BoboBrowse.Facets;
using log4net;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;

///
/// <summary> * Bobo Browse Engine - High performance faceted/parametric search implementation 
/// * that handles various types of semi-structured data.  Written in Java.
/// * 
/// * Copyright (C) 2005-2006  John Wang
/// *
/// * This library is free software; you can redistribute it and/or
/// * modify it under the terms of the GNU Lesser General Public
/// * License as published by the Free Software Foundation; either
/// * version 2.1 of the License, or (at your option) any later version.
/// *
/// * This library is distributed in the hope that it will be useful,
/// * but WITHOUT ANY WARRANTY; without even the implied warranty of
/// * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
/// * Lesser General Public License for more details.
/// *
/// * You should have received a copy of the GNU Lesser General Public
/// * License along with this library; if not, write to the Free Software
/// * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
/// * 
/// * To contact the project administrators for the bobo-browse project, 
/// * please go to https://sourceforge.net/projects/bobo-browse/, or 
/// * send mail to owner@browseengine.com. </summary>
/// 

namespace BoboBrowse.impl
{
    public class QueryProducer
	{
		private static log4net.ILog logger =LogManager.GetLogger(typeof(QueryProducer));

		public const string CONTENT_FIELD ="contents";
		public static Query convert(string queryString, string defaultField)// throws ParseException
		{
			if (queryString==null || queryString.Length==0)
			{
				return null;
			}
			else
			{
				Analyzer analyzer =new StandardAnalyzer();
				if (defaultField==null)
					defaultField="contents";
				return new QueryParser(defaultField, analyzer).Parse(queryString);
			}
		}

		internal static readonly SortField[] DEFAULT_SORT =new SortField[]{SortField.FIELD_SCORE};

        private class QuerySortComparatorSource : SortComparatorSource
        {
            private FacetHandler facetHandler;

            public QuerySortComparatorSource(FacetHandler facetHandler)
            {
                this.facetHandler = facetHandler;
            }

            public ScoreDocComparator NewComparator(IndexReader reader, string fieldname)
            {
                return facetHandler.GetScoreDocComparator();
            }
        }

		public static SortField[] convertSort(SortField[] sortSpec, BoboIndexReader idxReader)
		{
			 SortField[] retVal =DEFAULT_SORT;
			if (sortSpec!=null && sortSpec.Length>0)
			{
				List<SortField> sortList =new List<SortField>(sortSpec.Length+1);
				bool relevanceSortAdded =false;
				for (int i=0;i<sortSpec.Length;++i)
				{
					if (SortField.FIELD_DOC.Equals(sortSpec[i]))
					{
					  sortList.Add(SortField.FIELD_DOC);
					}
					else if (SortField.FIELD_SCORE.Equals(sortSpec[i]))
					{
					  sortList.Add(SortField.FIELD_SCORE);
					  relevanceSortAdded=true;
					}
					else
					{
						string fieldname =sortSpec[i].GetField();
						if (fieldname!=null)
						{
						  SortField sf =null;
						  FacetHandler facetHandler =idxReader.GetFacetHandler(fieldname);
						  if (facetHandler!=null)
						  {
                              sf = new SortField(fieldname.ToLower(), new QuerySortComparatorSource(facetHandler), sortSpec[i].GetReverse());
						  }
						  else
						  {
							  sf=sortSpec[i];
						  }
						  sortList.Add(sf);
						}
					}
				}
				if (!relevanceSortAdded)
				{
				  sortList.Add(SortField.FIELD_SCORE);
				}
				retVal=sortList.ToArray();
			}
			return retVal;
		}

//	public static DocIdSet buildBitSet( BrowseSelection[] selections,BoboIndexReader reader) throws IOException{
//		if (selections==null || selections.length == 0) return null;
//		DocIdSet finalBits=null;
//		FieldConfiguration fConf=reader.getFieldConfiguration();
//		FieldPlugin plugin;
//		DocIdSet finalNotBits=null;
//		
//        for(int i=0;i<selections.length;++i) {
//        	String fieldName=selections[i].getFieldName();
//        	plugin=fConf.getFieldPlugin(fieldName);
//        	
//        	if (plugin==null){
//        		throw new IOException("Undefined field: "+fieldName+" please check your field configuration.");
//        	}
//        	BoboFilter[] f=plugin.buildFilters(selections[i],false);        	
//        	DocIdSet bs=FieldPlugin.mergeSelectionBitset(reader, f, selections[i].getSelectionOperation());
//        	if (bs!=null){
//	        	if (finalBits==null){	        	
//	        			finalBits=bs;	        
//	        	}
//	        	else{
//	        		finalBits.and(bs);
//	        	}
//        	}        
//        	
//        	if (plugin.supportNotSelections()){
//	        	BoboFilter[] notF=plugin.buildFilters(selections[i],true);
//	        	DocIdSet notBS=FieldPlugin.mergeSelectionBitset(reader,notF, ValueOperation.ValueOperationOr);
//	        	
//	        	if (notBS!=null){
//	        		if (finalNotBits==null){
//	        			finalNotBits=notBS;
//	        		}
//	        		else{
//	        			finalNotBits.or(notBS);
//	        		}
//	        	}
//	        	/*
//	        	DocSet emptyVals=new TermFilter(new Term(fieldName,"")).getDocSet(reader);
//	        	if (emptyVals!=null && emptyVals.cardinality()>0 && finalNotBits!=null){
//	        		finalNotBits.or(emptyVals);
//	        	}*/
//        	}
//        }
//        
//        if (finalNotBits!=null && finalNotBits.cardinality()>0){		// we have "not" selections
//        	if (finalBits!=null){
//        		finalNotBits.flip(0, finalBits.length());
//        		finalBits.and(finalNotBits);
//        	}
//        	else{        		
//        		finalNotBits.flip(0,reader.maxDoc());
//        		finalBits=finalNotBits;
//        	}
//        }
//        
//        return finalBits;  
//    }

		public virtual Query buildQuery(string query) // throws ParseException
		{
			return convert(query, CONTENT_FIELD);
		}
	}
}