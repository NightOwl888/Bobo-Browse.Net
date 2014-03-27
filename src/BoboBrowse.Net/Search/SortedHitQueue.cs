using System;
using System.Collections.Generic;
using System.IO;
using BoboBrowse.Api;
using BoboBrowse.Facets;
using log4net;
using Lucene.Net.Search;
using Lucene.Net.Util;


 //* Bobo Browse Engine - High performance faceted/parametric search implementation 
 //* that handles various types of semi-structured data.  Written in Java.
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
 

namespace BoboBrowse.Search
{
    using System;
    using System.Collections.Generic;
    using Common.Logging;
    using Lucene.Net.Search;
    using Lucene.Net.Util;
    using BoboBrowse.Net.Facets;

    public class SortedHitQueue : PriorityQueue
    {
        private static readonly log4net.ILog logger = LogManager.GetLogger(typeof(SortedHitQueue));
        private readonly BoboBrowser boboBrowser;

        /// <summary> Stores a comparator corresponding to each field being sorted by  </summary>
        protected internal ScoreDocComparator[] comparators;
        internal Dictionary<string, ScoreDocComparator> comparatorMap;
        protected internal bool[] isReverse;

        public SortedHitQueue(BoboBrowser boboBrowser, SortField[] sortFields, int size)
        {
            comparatorMap = new Dictionary<string, ScoreDocComparator>();
            this.boboBrowser = boboBrowser;
            int n = sortFields.Length;
            List<ScoreDocComparator> comparatorList = new List<ScoreDocComparator>(n);
            List<bool> reverseList = new List<bool>(n);

            for (int i = 0; i < n; ++i)
            {
                ScoreDocComparator comparator = GetScoreDocComparator(sortFields[i]);

                if (comparator != null)
                {
                    comparatorList.Add(comparator);
                    reverseList.Add(sortFields[i].GetReverse());
                }
            }
            comparators = comparatorList.ToArray();
            isReverse = new bool[reverseList.Count];
            int c = 0;
            foreach (bool revVal in reverseList)
            {
                isReverse[c++] = revVal;
            }
            Initialize(size);
        }

        ///    
        ///	   <summary> * Returns whether <code>a</code> is less relevant than <code>b</code>. </summary>
        ///	   * <param name="a"> ScoreDoc </param>
        ///	   * <param name="b"> ScoreDoc </param>
        ///	   * <returns> <code>true</code> if document <code>a</code> should be sorted after document <code>b</code>. </returns>
        ///	   
        public override bool LessThan(object a, object b)
        {
            ScoreDoc docA = (ScoreDoc)a;
            ScoreDoc docB = (ScoreDoc)b;

            // run comparators
            int c = 0;
            int i = 0;
            foreach (ScoreDocComparator comparator in comparators)
            {
                c = comparator.Compare(docA, docB);
                if (c != 0)
                {
                    return isReverse[i] ? c < 0 : c > 0;
                }
                i++;
            }
            return docA.doc > docB.doc;
        }

        public virtual FieldDoc[] GetTopDocs(int offset, int numHits)
        {
            FieldDoc[] retVal = new FieldDoc[0];
            do
            {
                if (numHits == 0)
                    break;
                int size = this.Size();
                if (size == 0)
                    break;

                if (offset < 0 || offset >= size)
                {
                    throw new System.ArgumentException("Invalid offset: " + offset);
                }

                FieldDoc[] fieldDocs = new FieldDoc[size];
                for (int i = size - 1; i >= 0; i--)
                {
                    fieldDocs[i] = (FieldDoc)Pop();
                }
                //            
                //            if (logger.IsDebugEnabled){
                //                for (int i=0;i<fieldDocs.length;++i){
                //                    logger.Debug(fieldDocs[i]);
                //                }
                //            }
                //            

                int count = Math.Min(numHits, (size - offset));
                retVal = new FieldDoc[count];
                int n = offset + count;
                // if distance is there for 1 hit, it's there for all
                for (int i = offset; i < n; ++i)
                {
                    FieldDoc hit = fieldDocs[i];
                    retVal[i - offset] = hit;
                }
            } while (false);
            return retVal;
        }

        internal virtual ScoreDocComparator GetScoreDocComparator(SortField field)
        {
            int type = field.GetType();
            if (type == SortField.DOC)
            {
                return ScoreDocComparator_Fields.INDEXORDER;
            }
            if (type == SortField.SCORE)
            {
                return ScoreDocComparator_Fields.RELEVANCE;
            }

            string f = field.GetField();
            FacetHandler facetHandler = boboBrowser.GetFacetHandler(f);
            ScoreDocComparator comparator = null;
            if (facetHandler != null)
            {
                comparator = facetHandler.GetScoreDocComparator();
            }
            if (comparator == null) // resort to lucene
            {
                try
                {
                    comparator = boboBrowser.GetIndexReader().GetDefaultScoreDocComparator(field);
                }
                catch (IOException ioe)
                {
                    logger.Error(ioe.Message, ioe);
                }
            }

            if (comparator != null)
            {
                comparatorMap.Add(f, comparator);
            }
            return comparator;
        }
    }
}