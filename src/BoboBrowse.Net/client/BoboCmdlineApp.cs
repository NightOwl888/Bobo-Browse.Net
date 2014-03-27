using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using BoboBrowse.Api;
using BoboBrowse.impl;
using BoboBrowse.service;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;

namespace BoboBrowse.client
{
    public class BoboCmdlineApp
	{
		private readonly BrowseRequestBuilder _reqBuilder;

		private readonly BrowseService _svc;

		public BoboCmdlineApp(BrowseService svc)
		{
		  _svc = svc;
		  _reqBuilder = new BrowseRequestBuilder();
		}

///    
///	 * <param name="args"> </param>
///	 
		static void Main(string[] args)
		{
			FileInfo idxDir = new FileInfo(args[0]);
			Console.WriteLine("index: " + idxDir.FullName);

			BrowseService svc = new BrowseServiceImpl(idxDir);

			BoboCmdlineApp app = new BoboCmdlineApp(svc);

			while(true)
			{
				try
				{
					Console.Write("> ");
					string line = System.Console.ReadLine();
					while(true)
					{
						try
						{
						  app.processCommand(line);
						}
						catch(BrowseException ne)
						{
							System.Console.WriteLine(ne.StackTrace);
						}
						Console.Write("> ");

                        line = System.Console.ReadLine();
                    }

				}
                catch (ThreadInterruptedException ie)
				{
					throw new Exception(ie.Message,ie);
				    break;
				}
			}
		}

        internal virtual void processCommand(string line) // throws BrowseException, InterruptedException, ExecutionException
		{
			if (line == null || line.Length == 0)
				return;
			string[] parsed = line.Split(new string [] {" "},StringSplitOptions.RemoveEmptyEntries);
			if (parsed.Length == 0)
				return;

			string cmd = parsed[0];

			string[] args = new string[parsed.Length -1];
			if (args.Length > 0)
			{
				System.Array.Copy(parsed, 1, args, 0, args.Length);
			}

            if ("exit".Equals(cmd, StringComparison.InvariantCultureIgnoreCase))
			{
			    throw new ThreadInterruptedException();
			}
            else if ("help".Equals(cmd, StringComparison.InvariantCultureIgnoreCase))
			{
				Console.WriteLine("help - prints this message");
				Console.WriteLine("exit - quits");
				Console.WriteLine("query <query string> - sets query text");
				Console.WriteLine("facetspec <name>:<minHitCount>:<maxCount>:<sort> - add facet spec");
				Console.WriteLine("page <offset>:<count> - set paging parameters");
				Console.WriteLine("select <name>:<value1>,<value2>... - add selection, with ! in front of value indicates a not");
				Console.WriteLine("sort <name>:<dir>,... - set sort specs");
				Console.WriteLine("showReq: shows current request");
				Console.WriteLine("clear: clears current request");
				Console.WriteLine("clearSelections: clears all selections");
				Console.WriteLine("clearSelection <name>: clear selection specified");
				Console.WriteLine("clearFacetSpecs: clears all facet specs");
				Console.WriteLine("clearFacetSpec <name>: clears specified facetspec");
				Console.WriteLine("browse - executes a search");
			}
            else if ("query".Equals(cmd, StringComparison.InvariantCultureIgnoreCase))
			{
				if (parsed.Length<2)
				{
					Console.WriteLine("query not defined.");
				}
				else
				{
					string qString = parsed[1];
					string queryString = qString;
					if (queryString!=null)
					{
					  QueryParser qparser = new QueryParser("contents",new StandardAnalyzer());
					  Query q;
					  try
					  {
						q = qparser.Parse(queryString);
						_reqBuilder.getRequest().Query = q;
					  }
					  catch (Exception e)
					  {
					      System.Console.WriteLine(e.StackTrace);
					  }
					}
				}
			}
			else if ("facetspec".Equals(cmd, StringComparison.InvariantCultureIgnoreCase))
			{
				if (parsed.Length<2)
				{
					Console.WriteLine("facetspec not defined.");
				}
				else
				{
					try
					{
						string fspecString = parsed[1];
						string[] parts = fspecString.Split(new string[] { ":"},StringSplitOptions.RemoveEmptyEntries);
						string name = parts[0];
						string fvalue =parts[1];
                        string[] valParts = fvalue.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
						if (valParts.Length != 4)
						{
							Console.WriteLine("spec must of of the form <minhitcount>,<maxcount>,<isExpand>,<orderby>");
						}
						else
						{
							int minHitCount = 1;
							int maxCount = 5;
							bool expand =false;
							FacetSpec.FacetSortSpec sort = FacetSpec.FacetSortSpec.OrderHitsDesc;
							try
							{
								   minHitCount = Convert.ToInt32(valParts[0]);
							}
							catch(Exception e)
							{
								Console.WriteLine("default min hitcount = 1 is applied.");
							}
							try
							{
								maxCount = Convert.ToInt32(valParts[1]);
							}
							catch(Exception e)
							{
								Console.WriteLine("default maxCount = 5 is applied.");
							}
							try
							{
								expand =Convert.ToBoolean(valParts[2]);
							}
							catch(Exception e)
							{
								Console.WriteLine("default expand=false is applied.");
							}

							if ("hits".Equals(valParts[3]))
							{
								sort = FacetSpec.FacetSortSpec.OrderHitsDesc;
							}
							else
							{
								sort = FacetSpec.FacetSortSpec.OrderValueAsc;
							}

							_reqBuilder.applyFacetSpec(name, minHitCount, maxCount, expand, sort);
						}

					}
					catch(Exception e)
					{
                        System.Console.WriteLine(e.StackTrace);
					}
				}
			}
            else if ("select".Equals(cmd, StringComparison.InvariantCultureIgnoreCase))
			{
				if (parsed.Length<2)
				{
					Console.WriteLine("selection not defined.");
				}
				else
				{
					try
					{
						string selString = parsed[1];
                        string[] parts = selString.Split(new string[] { ":" }, StringSplitOptions.RemoveEmptyEntries);
						string name = parts[0];
						string selList = parts[1];
                        string[] sels = selList.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
						foreach (string sel in sels)
						{
							bool isNot =false;
							string val = sel;
							if (sel.StartsWith("!"))
							{
								isNot=true;
								val = sel.Substring(1);
							}
							if (val!=null && val.Length > 0)
							{
								_reqBuilder.addSelection(name, val, isNot);
							}
						}
					}
					catch(Exception e)
					{
                        System.Console.WriteLine(e.StackTrace);
					}
				}
			}
            else if ("page".Equals(cmd, StringComparison.InvariantCultureIgnoreCase))
			{
				try
				{
					string pageString = parsed[1];
                    string[] parts = pageString.Split(new string[] { ":" }, StringSplitOptions.RemoveEmptyEntries);
					_reqBuilder.setOffset(Convert.ToInt32(parts[0]));
					_reqBuilder.setCount(Convert.ToInt32(parts[1]));
				}
				catch(Exception e)
				{
                    System.Console.WriteLine(e.StackTrace);
				}
			}
            else if ("clearFacetSpec".Equals(cmd, StringComparison.InvariantCultureIgnoreCase))
			{
				if (parsed.Length<2)
				{
					Console.WriteLine("facet spec not defined.");
				}
				else
				{
					string name = parsed[1];
					_reqBuilder.clearFacetSpec(name);
				}
			}
            else if ("clearSelection".Equals(cmd, StringComparison.InvariantCultureIgnoreCase))
			{
				if (parsed.Length<2)
				{
					Console.WriteLine("selection name not defined.");
				}
				else
				{
					string name = parsed[1];
					_reqBuilder.clearSelection(name);
				}
			}
            else if ("clearSelections".Equals(cmd, StringComparison.InvariantCultureIgnoreCase))
			{
				_reqBuilder.clearSelections();
			}
            else if ("clearFacetSpecs".Equals(cmd, StringComparison.InvariantCultureIgnoreCase))
			{
				_reqBuilder.clearFacetSpecs();
			}
            else if ("clear".Equals(cmd, StringComparison.InvariantCultureIgnoreCase))
			{
				_reqBuilder.clear();
			}
            else if ("showReq".Equals(cmd, StringComparison.InvariantCultureIgnoreCase))
			{
				BrowseRequest req = _reqBuilder.getRequest();
				Console.WriteLine(req.ToString());
			}
            else if ("sort".Equals(cmd, StringComparison.InvariantCultureIgnoreCase))
			{
				if (parsed.Length == 2)
				{
					string sortString = parsed[1];
                    string[] sorts = sortString.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries); ;
					List<SortField> sortList = new List<SortField>();
					foreach (string sort in sorts)
					{
                        string[] sortParams = sort.Split(new string[] { ":" }, StringSplitOptions.RemoveEmptyEntries);
						bool rev = false;
						if (sortParams.Length>0)
						{
						  string sortName = sortParams[0];
						  if (sortParams.Length>1)
						  {
							try
							{
							  rev = Convert.ToBoolean(sortParams[1]);
							}
							catch(Exception e)
							{
								Console.WriteLine(e.Message+", default rev to false");
							}
						  }
						  sortList.Add(new SortField(sortName,rev));
						}
					}
					_reqBuilder.applySort(sortList.ToArray());
				}
				else
				{
					_reqBuilder.applySort(null);
				}
			}
            else if ("browse".Equals(cmd, StringComparison.InvariantCultureIgnoreCase))
			{
				BrowseRequest req = _reqBuilder.getRequest();

				BrowseResult res = _svc.Browse(req);
				string output = BrowseResultFormatter.formatResults(res);
				Console.WriteLine(output);
			}
			else
			{
				Console.WriteLine("Unknown command: "+cmd+", do help for list of supported commands");
			}
		}

	}

}