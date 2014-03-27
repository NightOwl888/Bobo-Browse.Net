using System;
using System.Collections.Generic;
using BoboBrowse.Api;
using BoboBrowse.LangUtils;
using log4net;
using Lucene.Net.Search;

namespace BoboBrowse.impl
{
    public class SortedFieldBrowseHitComparator : IComparer<BrowseHit>
    {
        private static readonly log4net.ILog logger = LogManager.GetLogger(typeof(SortedFieldBrowseHitComparator));

        private readonly SortField[] sortFields;

        public SortedFieldBrowseHitComparator(SortField[] sortFields)
        {
            this.sortFields = sortFields;
        }

        private int Compare(BrowseHit h1, BrowseHit h2, SortField sort)
        {
            int type = sort.GetType();

            int c = 0;

            switch (type)
            {
                case SortField.SCORE:
                    {
                        float r1 = h1.Score;
                        float r2 = h2.Score;
                        if (r1 > r2)
                            c = -1;
                        if (r1 < r2)
                            c = 1;
                        break;
                    }
                case SortField.DOC:
                    {
                        int i1 = h1.DocId;
                        int i2 = h2.DocId;
                        c = i2 - i1;
                        break;
                    }
                case SortField.INT:
                    {
                        int i1 = ((int)h1.GetComparable(sort.GetField()));
                        int i2 = ((int)h2.GetComparable(sort.GetField()));
                        c = i1 - i2;
                        break;
                    }
                case SortField.LONG:
                    {
                        long l1 = ((long)h1.GetComparable(sort.GetField()));
                        long l2 = ((long)h2.GetComparable(sort.GetField()));
                        if (l1 < l2)
                            c = -1;
                        if (l1 > l2)
                            c = 1;
                        break;
                    }
                case SortField.STRING:
                    {
                        string s1 = (string)h1.GetField(sort.GetField());
                        string s2 = (string)h2.GetField(sort.GetField());
                        if (s1 == null)
                        {
                            if (s2 == null)
                                c = 0;
                            else
                                c = 1;
                        }
                        else
                        {
                            c = s1.CompareTo(s2);
                        }
                        break;
                    }
                case SortField.FLOAT:
                    {
                        float f1 = ((float)h1.GetComparable(sort.GetField()));
                        float f2 = ((float)h2.GetComparable(sort.GetField()));
                        if (f1 < f2)
                            c = -1;
                        if (f1 > f2)
                            c = 1;
                        break;
                    }
                case SortField.DOUBLE:
                    {
                        double d1 = ((double)h1.GetComparable(sort.GetField()));
                        double d2 = ((double)h2.GetComparable(sort.GetField()));
                        if (d1 < d2)
                            c = -1;
                        if (d1 > d2)
                            c = 1;
                        break;
                    }
                case SortField.BYTE:
                    {
                        int i1 = ((sbyte)h1.GetComparable(sort.GetField()));
                        int i2 = ((sbyte)h2.GetComparable(sort.GetField()));
                        c = i1 - i2;
                        break;
                    }
                case SortField.SHORT:
                    {
                        int i1 = ((short)h1.GetComparable(sort.GetField()));
                        int i2 = ((short)h2.GetComparable(sort.GetField()));
                        c = i1 - i2;
                        break;
                    }
                case SortField.CUSTOM:
                case SortField.AUTO:
                    {
                        string field = sort.GetField();
                        IComparable obj1 = h1.GetComparable(field);
                        IComparable obj2 = h2.GetComparable(field);
                        if (obj1 == null)
                        {
                            if (obj2 == null)
                                c = 0;
                            else
                                c = 1;
                        }
                        else
                        {
                            c = obj1.CompareTo(obj2);
                        }
                        break;
                    }
                default:
                    {
                        throw new RuntimeException("invalid SortField type: " + type);
                    }
            }

            if (sort.GetReverse())
            {
                c = -c;
            }

            return c;
        }

        public virtual int Compare(BrowseHit h1, BrowseHit h2)
        {
            foreach (SortField sort in sortFields)
            {
                int val = Compare(h1, h2, sort);
                if (val != 0)
                    return val;
            }
            return h2.DocId - h1.DocId;
        }
    }
}