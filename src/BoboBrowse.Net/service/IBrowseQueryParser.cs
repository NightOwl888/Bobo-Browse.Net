using System;
using Lucene.Net.Search;

namespace BoboBrowse.service
{
    ///<summary>Builds a DocSet from an array of SelectioNodes </summary>
    public interface IBrowseQueryParser
    {
        DocIdSet Parse(SelectionNode[] selectionNodes, SelectionNode[] notSelectionNodes, int maxDoc);
    }

    public class SelectionNode
    {
        public String FieldName { get; set; }
        public DocIdSet DocSet { get; set; }

        public SelectionNode()
        {
        }

        public SelectionNode(String fieldName, DocIdSet docSet)
        {
            FieldName = fieldName;
            DocSet = docSet;
        }
    }
}