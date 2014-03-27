using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BoboBrowse.Facets;
using BoboBrowse.query;
using BoboBrowse.Search;
using C5;
using log4net;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Directory = Lucene.Net.Store.Directory;


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


namespace BoboBrowse.Api
{
    ///<summary>bobo browse index reader</summary>
    public class BoboIndexReader : FilterIndexReader
    {
        private const string FIELD_CONFIG = "field.xml";
        private const string SPRING_CONFIG = "bobo.spring";

        private static log4net.ILog logger = LogManager.GetLogger(typeof(BoboIndexReader));

        private Dictionary<string, FacetHandler> facetHandlerMap;

        private Dictionary<SortFieldEntry, ScoreDocComparator> defaultSortFieldCache;

        private readonly System.Collections.Generic.ICollection<FacetHandler> facetHandlers;
        private readonly WorkArea workArea;
        private readonly System.Collections.Generic.ICollection<int> deletedDocs;
        private volatile int[] deletedDocsArray;

        ///<summary>Constructor</summary>
        ///<param name="reader">Index reader</param>
        ///<exception cref="IOException"></exception>
        public static BoboIndexReader GetInstance(IndexReader reader)
        {
            return BoboIndexReader.GetInstance(reader, null, new WorkArea());
        }

        public static BoboIndexReader GetInstance(IndexReader reader, WorkArea workArea)
        {
            return BoboIndexReader.GetInstance(reader, null, workArea);
        }

        private static System.Collections.Generic.ICollection<FacetHandler> LoadFromIndex(FileInfo file)
        {
            throw new NotImplementedException();
            // FIMXE : startup code here
            //FileInfo springFile = new FileInfo(file, SPRING_CONFIG);
        }

        protected override void DoClose()
        {
            try
            {
                base.DoClose();
            }
            finally
            {
                lock (defaultSortFieldCache)
                {
                    defaultSortFieldCache.Clear();
                }
                facetHandlerMap.Clear();
            }
        }

        private void LoadFacetHandler(string name, List<string> loaded, List<string> visited, WorkArea workArea)
        {
            FacetHandler facetHandler = facetHandlerMap[name];
            if (facetHandler != null && !loaded.Contains(name))
            {
                visited.Add(name);
                List<string> dependsOn = facetHandler.GetDependsOn();
                if (dependsOn.Count > 0)
                {
                    List<string>.Enumerator iter = dependsOn.GetEnumerator();
                    while (iter.MoveNext())
                    {
                        string f = iter.Current;
                        if (name.Equals(f))
                            continue;
                        if (!loaded.Contains(f))
                        {
                            if (visited.Contains(f))
                            {
                                throw new IOException("Facet handler dependency cycle detected, facet handler: " + name + " not loaded");
                            }
                            LoadFacetHandler(f, loaded, visited, workArea);
                        }
                        if (!loaded.Contains(f))
                        {
                            throw new IOException("unable to load facet handler: " + f);
                        }
                        facetHandler.PutDependedFacetHandler(facetHandlerMap[f]);
                    }
                }

                long start = System.Environment.TickCount;
                facetHandler.Load(this, workArea);
                long end = System.Environment.TickCount;
                if (logger.IsDebugEnabled)
                {
                    StringBuilder buf = new StringBuilder();
                    buf.Append("facetHandler loaded: ").Append(name).Append(", took: ").Append(end - start).Append(" ms");
                    logger.Debug(buf.ToString());
                }
                loaded.Add(name);
            }
        }

        private void LoadFacetHandlers(WorkArea workArea)
        {
            Dictionary<string, FacetHandler>.KeyCollection facethandlers = facetHandlerMap.Keys;
            Dictionary<string, FacetHandler>.KeyCollection.Enumerator iter = facethandlers.GetEnumerator();
            List<string> loaded = new List<string>();
            List<string> visited = new List<string>();
            List<string> tobeRemoved = new List<string>();

            while (iter.MoveNext())
            {
                string name = iter.Current;
                try
                {
                    LoadFacetHandler(name, loaded, visited, workArea);
                }
                catch (Exception ioe)
                {
                    tobeRemoved.Add(name);
                    logger.Error("facet load failed: " + name + ": " + ioe.Message, ioe);
                }
            }

            IEnumerator<string> iter2 = tobeRemoved.GetEnumerator();
            while (iter2.MoveNext())
            {
                facetHandlerMap.Remove(iter2.Current);
            }
        }

        private void Initialize(System.Collections.Generic.ICollection<FacetHandler> facetHandlers, WorkArea workArea)
        {
            facetHandlerMap = new Dictionary<string, FacetHandler>();
            if (facetHandlers == null) // try to load from index
            {
                // throw new NotImplementedException(); // FIXME : initialization code

                Directory idxDir = null;
                if (idxDir != null && idxDir is FSDirectory)
                {

                    /*FSDirectory fsDir = (FSDirectory)idxDir;
                    File file = fsDir.GetFile();

                    if (new File(file, SPRING_CONFIG).exists())
                    {
                        facetHandlers = loadFromIndex(file);
                    }
                    else if (new File(file, FIELD_CONFIG).exists())
                    {
                        facetHandlers = XMLFieldConfigurationBuilder.loadFieldConfiguration(new File(file, FIELD_CONFIG)).getFacetHandlers();
                    }
                    else*/
                    {
                        facetHandlers = new List<FacetHandler>();
                    }
                }
                else
                {
                    facetHandlers = new List<FacetHandler>();
                }
            }
            foreach (FacetHandler facetHandler in facetHandlers)
            {
                facetHandlerMap.Add(facetHandler.Name, facetHandler);
            }

            LoadFacetHandlers(workArea);

            defaultSortFieldCache = new Dictionary<SortFieldEntry, ScoreDocComparator>();
        }

        public virtual ScoreDocComparator GetDefaultScoreDocComparator(SortField f)
        {
            int type = f.GetType();
            if (type == SortField.DOC)
            {
                return ScoreDocComparator_Fields.INDEXORDER;
            }
            if (type == SortField.SCORE)
            {
                return ScoreDocComparator_Fields.RELEVANCE;
            }

            SortComparatorSource factory = f.GetFactory();
            SortFieldEntry entry = factory == null ? new SortFieldEntry(f.GetField(), type, f.GetLocale()) : new SortFieldEntry(f.GetField(), factory);

            ScoreDocComparator comparator;
            if (!defaultSortFieldCache.TryGetValue(entry, out comparator))
            {
                lock (defaultSortFieldCache)
                {
                    if (!defaultSortFieldCache.TryGetValue(entry, out comparator))
                    {
                        comparator = LuceneSortDocComparatorFactory.BuildScoreDocComparator(this, entry);
                        if (comparator != null)
                        {
                            defaultSortFieldCache.Add(entry, comparator);
                        }
                    }
                }
                return comparator;
            }
            else
            {
                return comparator;
            }
        }

        ///<summary>Constructor.</summary>
        ///<param name="reader">index reader </param>
        ///<param name="facetHandlers">List of facet handlers </param>
        ///<exception cref="IOException"> </exception>
        public static BoboIndexReader GetInstance(IndexReader reader, System.Collections.Generic.ICollection<FacetHandler> facetHandlers)
        {
            return BoboIndexReader.GetInstance(reader, facetHandlers, new WorkArea());
        }

        public static BoboIndexReader GetInstance(IndexReader reader, System.Collections.Generic.ICollection<FacetHandler> facetHandlers, WorkArea workArea)
        {
            BoboIndexReader boboReader = new BoboIndexReader(reader, facetHandlers, workArea);
            boboReader.FacetInit();
            return boboReader;
        }

        protected internal BoboIndexReader(IndexReader reader, System.Collections.Generic.ICollection<FacetHandler> facetHandlers, WorkArea workArea)
            : base(reader)
        {
            this.facetHandlers = facetHandlers;
            this.workArea = workArea;
            deletedDocs = new TreeSet<int>();
            if (reader.HasDeletions())
            {
                int maxDoc = MaxDoc();
                for (int i = 0; i < maxDoc; i++)
                {
                    if (reader.IsDeleted(i))
                    {
                        deletedDocs.Add(i);
                    }
                }
            }
        }

        protected internal virtual void FacetInit()
        {
            Initialize(facetHandlers, workArea);
        }

        public virtual Query GetFastMatchAllDocsQuery()
        {
            int[] deldocs = deletedDocsArray;
            if (deldocs == null)
            {
                lock (deletedDocs)
                {
                    deldocs = deletedDocs.ToArray();
                    deletedDocsArray = deldocs;
                }
            }
            return new FastMatchAllDocsQuery(deldocs, MaxDoc());
        }

        ///   <summary> * Utility method to dump out all fields (name and terms) for a given index.
        ///   *  </summary>
        ///   * <param name="outFile">
        ///   *          File to dump to. </param>
        ///   * <exception cref="IOException"> </exception>
        public virtual void DumpFields(FileInfo outFile)
        {
            throw new NotImplementedException();
            /*
            FileWriter writer = null;
            try
            {
                writer = new FileWriter(outFile);
                PrintWriter @out = new PrintWriter(writer);
                List<string> fieldNames = getFacetNames();
                foreach (string fieldName in fieldNames)
                {
                    TermEnum te = terms(new Term(fieldName, ""));
                    @out.write(fieldName + ":\n");
                    while (te.Next())
                    {
                        Term term = te.term();
                        if (!fieldName.Equals(term.field()))
                        {
                            break;
                        }
                        @out.write(term.text() + "\n");
                    }
                    @out.write("\n\n");
                }
            }
            finally
            {
                if (writer != null)
                {
                    writer.Close();
                }
            }*/
        }

        ///<summary>Gets all the facet field names</summary>
        ///<returns> Set of facet field names </returns>
        public virtual IEnumerable<string> GetFacetNames()
        {
            return facetHandlerMap.Keys;
        }

        ///<summary>Gets a facet handler</summary>
        ///<param name="fieldname">name </param>
        ///<returns>facet handler </returns>
        public virtual FacetHandler GetFacetHandler(string fieldname)
        {
            FacetHandler result;
            return
                facetHandlerMap.TryGetValue(fieldname, out result)
                ? result
                : null;
        }

        ///<summary>Gets the facet handler map</summary>
        ///<returns>facet handler map </returns>
        public virtual Dictionary<string, FacetHandler> GetFacetHandlerMap()
        {
            return facetHandlerMap;
        }

        public override Document Document(int docid)
        {
            Document doc = base.Document(docid);
            System.Collections.Generic.ICollection<FacetHandler> facetHandlers = facetHandlerMap.Values;
            foreach (FacetHandler facetHandler in facetHandlers)
            {
                string[] vals = facetHandler.GetFieldValues(docid);
                if (vals != null)
                {
                    foreach (string val in vals)
                    {
                        if (null != val)
                        {
                            doc.Add(new Field(facetHandler.Name, val, Field.Store.NO, Field.Index.NOT_ANALYZED));
                        }
                    }
                }
            }
            return doc;
        }

        public override void DeleteDocument(int docid)
        {
            base.DeleteDocument(docid);
            lock (deletedDocs)
            {
                deletedDocs.Add(docid);
                // remove the array but do not recreate the array at this point
                // there may be more deleteDocument calls
                deletedDocsArray = null;
            }
        }

        ///<summary>* Work area for loading </summary>
        public class WorkArea
        {
            private Dictionary<Type, object> map = new Dictionary<Type, object>();

            public virtual T Get<T>()
            {
                object @value;
                return
                    map.TryGetValue(typeof(T), out @value)
                        ? (T)@value
                        : default(T);
            }

            public virtual void Put(object obj)
            {
                map.Add(obj.GetType(), obj);
            }

            public virtual void Clear()
            {
                map.Clear();
            }
        }
    }
}