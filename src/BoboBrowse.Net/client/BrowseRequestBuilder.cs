using BoboBrowse.Api;
using Lucene.Net.Search;

namespace BoboBrowse.client
{
    public class BrowseRequestBuilder
	{
		private BrowseRequest _req;
		private string _qString;

		public BrowseRequestBuilder()
		{
            clear();
		}

		public virtual void addSelection(string name, string val, bool isNot)
		{
			BrowseSelection sel = _req.GetSelection(name);
			if (sel==null)
			{
				sel = new BrowseSelection(name);
			}
			if (isNot)
			{
				sel.AddNotValue(val);
			}
			else
			{
				sel.AddValue(val);
			}
			_req.AddSelection(sel);
		}

		public virtual void clearSelection(string name)
		{
			_req.RemoveSelection(name);
		}

		public virtual void applyFacetSpec(string name, int minHitCount, int maxCount, bool expand, FacetSpec.FacetSortSpec orderBy)
		{
			FacetSpec fspec = new FacetSpec();
			fspec.MinHitCount = minHitCount;
			fspec.MaxCount = maxCount;
			fspec.ExpandSelection = expand;
			fspec.OrderBy = orderBy;
			_req.SetFacetSpec(name, fspec);
		}

		public virtual void applySort(SortField[] sorts)
		{
			if (sorts==null)
			{
				_req.ClearSort();
			}
			else
			{
				_req.Sort = sorts;
			}
		}

		public virtual void clearFacetSpecs()
		{
			_req.FacetSpecs.Clear();
		}
		public virtual void clearFacetSpec(string name)
		{
			_req.FacetSpecs.Remove(name);
		}

		public virtual void setOffset(int offset)
		{
			_req.Offset = offset;
		}

		public virtual void setCount(int count)
		{
			_req.Count = count;
		}

		public virtual void setQuery(string qString)
		{
			_qString = qString;
		}

		public virtual void clear()
		{
			_req = new BrowseRequest();
			_req.Offset = 0;
			_req.Count = 5;
			_req.FetchStoredFields = true;
			_qString = null;
		}

		public virtual void clearSelections()
		{
			_req.ClearSelections();
		}

		public virtual BrowseRequest getRequest()
		{
			return _req;
		}

		public virtual string getQueryString()
		{
			return _qString;
		}
	}

}