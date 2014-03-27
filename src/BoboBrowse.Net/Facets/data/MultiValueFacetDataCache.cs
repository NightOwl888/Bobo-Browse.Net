using System;
using System.Collections.Generic;
using System.IO;
using BoboBrowse.Api;
using BoboBrowse.LangUtils;
using BoboBrowse.Util;
using log4net;
using Lucene.Net.Index;
using Lucene.Net.Search;

namespace BoboBrowse.Facets.data
{
    ///
    /// <summary> * @author ymatsuda
    /// * </summary>
    /// 
    public class MultiValueFacetDataCache : FacetDataCache
    {
        private static log4net.ILog logger = LogManager.GetLogger(typeof(MultiValueFacetDataCache));

        public readonly BigNestedIntArray _nestedArray;
        private int _maxItems = BigNestedIntArray.MAX_ITEMS;
        private bool _overflow = false;

        public MultiValueFacetDataCache()
        {
            _nestedArray = new BigNestedIntArray();
        }

        public virtual void SetMaxItems(int maxItems)
        {
            _maxItems = Math.Min(maxItems, BigNestedIntArray.MAX_ITEMS);
            _nestedArray.setMaxItems(_maxItems);
        }

        public override void Load(string fieldName, IndexReader reader, TermListFactory listFactory)
        {
            this.Load(fieldName, reader, listFactory, new BoboIndexReader.WorkArea());
        }

        ///  
        ///   <summary> * loads multi-value facet data. This method uses a workarea to prepare loading. </summary>
        ///   * <param name="fieldName"> </param>
        ///   * <param name="reader"> </param>
        ///   * <param name="listFactory"> </param>
        ///   * <param name="workArea"> </param>
        ///   * <exception cref="IOException"> </exception>
        ///   
        public virtual void Load(string fieldName, IndexReader reader, TermListFactory listFactory, BoboIndexReader.WorkArea workArea)
        {
            int maxdoc = reader.MaxDoc();
            BigNestedIntArray.BufferedLoader loader = GetBufferedLoader(maxdoc, workArea);

            TermEnum tenum = null;
            TermDocs tdoc = null;
            ITermValueList list = (listFactory == null ? new TermStringList() : listFactory.CreateTermList());
            List<int> minIDList = new List<int>();
            List<int> maxIDList = new List<int>();
            List<int> freqList = new List<int>();

            int t = 0; // current term number
            list.Add(null);
            minIDList.Add(-1);
            maxIDList.Add(-1);
            freqList.Add(0);
            t++;

            _overflow = false;
            try
            {
                tdoc = reader.TermDocs();
                tenum = reader.Terms(new Term(fieldName));
                if (tenum != null)
                {
                    do
                    {
                        Term term = tenum.Term();
                        if (term == null || !fieldName.Equals(term.Field()))
                            break;

                        string val = term.Text();

                        // if (val!=null && val.length()>0){
                        if (val != null)
                        {
                            list.Add(val);

                            tdoc.Seek(tenum);
                            //freqList.add(tenum.docFreq()); // removed because the df doesn't take into account the num of deletedDocs
                            int df = 0;
                            int minID = -1;
                            int maxID = -1;
                            if (tdoc.Next())
                            {
                                df++;
                                int docid = tdoc.Doc();
                                if (!loader.Add(docid, t))
                                    LogOverflow(fieldName);
                                minID = docid;
                                while (tdoc.Next())
                                {
                                    df++;
                                    docid = tdoc.Doc();
                                    if (!loader.Add(docid, t))
                                        LogOverflow(fieldName);
                                }
                                maxID = docid;
                            }
                            freqList.Add(df);
                            minIDList.Add(minID);
                            maxIDList.Add(maxID);
                        }

                        t++;
                    }
                    while (tenum.Next());
                }
            }
            finally
            {
                try
                {
                    if (tdoc != null)
                    {
                        tdoc.Close();
                    }
                }
                finally
                {
                    if (tenum != null)
                    {
                        tenum.Close();
                    }
                }
            }

            list.Seal();

            try
            {
                _nestedArray.load(maxdoc, loader);
            }
            catch (IOException e)
            {
                throw e;
            }
            catch (Exception e)
            {
                throw new RuntimeException("failed to load due to " + e.ToString(), e);
            }

            this.valArray = list;
            this.freqs = freqList.ToArray();
            this.minIDs = minIDList.ToArray();
            this.maxIDs = maxIDList.ToArray();
        }

        ///  
        ///   <summary> * loads multi-value facet data. This method uses the count payload to allocate storage before loading data. </summary>
        ///   * <param name="fieldName"> </param>
        ///   * <param name="sizeTerm"> </param>
        ///   * <param name="reader"> </param>
        ///   * <param name="listFactory"> </param>
        ///   * <exception cref="IOException"> </exception>
        ///   
        public virtual void Load(string fieldName, IndexReader reader, TermListFactory listFactory, Term sizeTerm)
        {
            int maxdoc = reader.MaxDoc();
            BigNestedIntArray.Loader loader = new AllocOnlyLoader(_maxItems, sizeTerm, reader);

            try
            {
                _nestedArray.load(maxdoc, loader);
            }
            catch (IOException e)
            {
                throw e;
            }
            catch (Exception e)
            {
                throw new RuntimeException("failed to load due to " + e.ToString(), e);
            }

            TermEnum tenum = null;
            TermDocs tdoc = null;
            ITermValueList list = (listFactory == null ? new TermStringList() : listFactory.CreateTermList());
            List<int> minIDList = new List<int>();
            List<int> maxIDList = new List<int>();
            List<int> freqList = new List<int>();

            int t = 0; // current term number
            list.Add(null);
            minIDList.Add(-1);
            maxIDList.Add(-1);
            freqList.Add(0);
            t++;

            _overflow = false;
            try
            {
                tdoc = reader.TermDocs();
                tenum = reader.Terms(new Term(fieldName, ""));
                if (tenum != null)
                {
                    do
                    {
                        Term term = tenum.Term();
                        if (term == null || !fieldName.Equals(term.Field()))
                            break;

                        string val = term.Text();

                        if (val != null)
                        {
                            list.Add(val);

                            tdoc.Seek(tenum);
                            //freqList.add(tenum.docFreq()); // removed because the df doesn't take into account the num of deletedDocs
                            int df = 0;
                            int minID = -1;
                            int maxID = -1;
                            if (tdoc.Next())
                            {
                                df++;
                                int docid = tdoc.Doc();
                                if (!_nestedArray.addData(docid, t))
                                    LogOverflow(fieldName);
                                minID = docid;
                                while (tdoc.Next())
                                {
                                    df++;
                                    docid = tdoc.Doc();
                                    if (!_nestedArray.addData(docid, t))
                                        LogOverflow(fieldName);
                                }
                                maxID = docid;
                            }
                            freqList.Add(df);
                            minIDList.Add(minID);
                            maxIDList.Add(maxID);
                        }

                        t++;
                    }
                    while (tenum.Next());
                }
            }
            finally
            {
                try
                {
                    if (tdoc != null)
                    {
                        tdoc.Close();
                    }
                }
                finally
                {
                    if (tenum != null)
                    {
                        tenum.Close();
                    }
                }
            }

            list.Seal();

            this.valArray = list;
            this.freqs = freqList.ToArray();
            this.minIDs = minIDList.ToArray();
            this.maxIDs = maxIDList.ToArray();
        }

        private void LogOverflow(string fieldName)
        {
            if (!_overflow)
            {
                logger.Error("Maximum value per document: " + _maxItems + " exceeded, fieldName=" + fieldName);
                _overflow = true;
            }
        }

        private BigNestedIntArray.BufferedLoader GetBufferedLoader(int maxdoc, BoboIndexReader.WorkArea workArea)
        {
            if (workArea == null)
            {
                return new BigNestedIntArray.BufferedLoader(maxdoc, _maxItems, new BigIntBuffer());
            }
            else
            {
                BigIntBuffer buffer = workArea.Get<BigIntBuffer>();
                if (buffer == null)
                {
                    buffer = new BigIntBuffer();
                    workArea.Put(buffer);
                }
                else
                {
                    buffer.Reset();
                }

                BigNestedIntArray.BufferedLoader loader = workArea.Get<BigNestedIntArray.BufferedLoader>();
                if (loader == null || loader.capacity() < maxdoc)
                {
                    loader = new BigNestedIntArray.BufferedLoader(maxdoc, _maxItems, buffer);
                    workArea.Put(loader);
                }
                else
                {
                    loader.reset(maxdoc, _maxItems, buffer);
                }
                return loader;
            }
        }

        ///  
        ///   <summary> * A loader that allocate data storage without loading data to BigNestedIntArray.
        ///   * Note that this loader supports only non-negative integer data. </summary>
        ///   
        public sealed class AllocOnlyLoader : BigNestedIntArray.Loader
        {
            private IndexReader _reader;
            private Term _sizeTerm;
            private int _maxItems;

            public AllocOnlyLoader(int maxItems, Term sizeTerm, IndexReader reader)
            {
                _maxItems = Math.Min(maxItems, BigNestedIntArray.MAX_ITEMS);
                _sizeTerm = sizeTerm;
                _reader = reader;
            }

            public override void Load()
            {
                TermPositions tp = null;
                byte[] payloadBuffer = new byte[4]; // four bytes for an int
                try
                {
                    tp = _reader.TermPositions(_sizeTerm);

                    if (tp == null)
                        return;

                    while (tp.Next())
                    {
                        if (tp.Freq() > 0)
                        {
                            tp.NextPosition();
                            tp.GetPayload(payloadBuffer, 0);
                            int len = BytesToInt(payloadBuffer);
                            allocate(tp.Doc(), Math.Min(len, _maxItems), true);
                        }
                    }
                }
                finally
                {
                    if (tp != null)
                        tp.Close();
                }
            }

            private static int BytesToInt(byte[] bytes)
            {
                return ((bytes[3] & 0xFF) << 24) | ((bytes[2] & 0xFF) << 16) | ((bytes[1] & 0xFF) << 8) | (bytes[0] & 0xFF);
            }
        }

        public virtual ScoreDocComparator getScoreDocComparator()
        {
            return new MultiFacetScoreDocComparator(this);
        }

        public sealed class MultiFacetScoreDocComparator : ScoreDocComparator
        {
            private MultiValueFacetDataCache _dataCache;
            public MultiFacetScoreDocComparator(MultiValueFacetDataCache dataCache)
            {
                _dataCache = dataCache;
            }
            public int Compare(ScoreDoc i, ScoreDoc j)
            {
                return _dataCache._nestedArray.compare(i.doc, j.doc);
            }

            public int SortType()
            {
                return SortField.CUSTOM;
            }

            public IComparable SortValue(ScoreDoc i)
            {
                string[] vals = _dataCache._nestedArray.getTranslatedData(i.doc, _dataCache.valArray);
                return new StringArrayComparator(vals);
            }
        }
    }
}