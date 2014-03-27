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

namespace BoboBrowse.Net.Search
{
using System;
    using System.Collections.Generic;
using System.Globalization;
using System.IO;
using BoboBrowse.LangUtils;
using log4net;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Search.ExtendedFieldCache_old;
using System.Collections.Generic;

namespace BoboBrowse.Search
{
    public class LuceneSortDocComparatorFactory
    {
        private static log4net.ILog logger = LogManager.GetLogger(typeof(LuceneSortDocComparatorFactory));

        public static ScoreDocComparator BuildScoreDocComparator(IndexReader reader, SortFieldEntry entry)
        {
            string fieldname = entry.Field;
            int type = entry.Type;

            if (type == SortField.DOC)
            {
                return ScoreDocComparator_Fields.INDEXORDER;
            }
            if (type == SortField.SCORE)
            {
                return ScoreDocComparator_Fields.RELEVANCE;
            }

            if (type == SortField.CUSTOM && entry.Custom != null)
            {
                return entry.Custom.NewComparator(reader, fieldname);
            }

            ICollection<string> indexFieldnames = reader.GetFieldNames(IndexReader.FieldOption.INDEXED);
            if (!indexFieldnames.Contains(fieldname))
            {
                logger.Warn(fieldname + " is not a sortable field, ignored.");
                return null;
            }

            CultureInfo locale = entry.Locale;
            SortComparatorSource factory = entry.Custom;
            ScoreDocComparator comparator = null;
            switch (type)
            {
                case SortField.AUTO:
                    comparator = ComparatorAuto(reader, fieldname);
                    break;
                case SortField.INT:
                    comparator = ComparatorInt(reader, fieldname);
                    break;
                case SortField.FLOAT:
                    comparator = ComparatorFloat(reader, fieldname);
                    break;
                case SortField.LONG:
                    comparator = ComparatorLong(reader, fieldname);
                    break;
                case SortField.DOUBLE:
                    comparator = ComparatorDouble(reader, fieldname);
                    break;
                case SortField.SHORT:
                    comparator = ComparatorShort(reader, fieldname);
                    break;
                case SortField.BYTE:
                    comparator = ComparatorByte(reader, fieldname);
                    break;
                case SortField.STRING:
                    if (locale != null)
                        comparator = ComparatorStringLocale(reader, fieldname, locale);
                    else
                        comparator = ComparatorString(reader, fieldname);
                    break;
                case SortField.CUSTOM:
                    if (factory != null)
                    {
                        comparator = factory.NewComparator(reader, fieldname);
                    }
                    else
                    {
                        logger.Warn(fieldname + ": no factory specified, ignored.");
                    }
                    break;
                default:
                    throw new RuntimeException("unknown field type: " + type);
            }
            return comparator;
        }

        private class ByteScoreDocComparator : ScoreDocComparator
        {
            private sbyte[] fieldOrder;

            public ByteScoreDocComparator(sbyte[] fieldOrder)
            {
                this.fieldOrder = fieldOrder;
            }

            public int Compare(ScoreDoc i, ScoreDoc j)
            {
                int fi = fieldOrder[i.doc];
                int fj = fieldOrder[j.doc];
                if (fi < fj)
                    return -1;
                if (fi > fj)
                    return 1;
                return 0;
            }

            public IComparable SortValue(ScoreDoc i)
            {
                return fieldOrder[i.doc];
            }

            public int SortType()
            {
                return SortField.INT;
            }
        }

        ///   
        ///   <summary> * Returns a comparator for sorting hits according to a field containing bytes. </summary>
        ///   * <param name="reader">  Index to use. </param>
        ///   * <param name="fieldname">  Fieldable containg integer values. </param>
        ///   * <returns>  Comparator for sorting hits. </returns>
        ///   * <exception cref="IOException"> If an error occurs reading the index. </exception>
        ///   
        internal static ScoreDocComparator ComparatorByte(IndexReader reader, string fieldname)
        {
            string field = string.Intern(fieldname);
            sbyte[] fieldOrder = FieldCache_Fields.DEFAULT.GetBytes(reader, field);

            return new ByteScoreDocComparator(fieldOrder);
        }

        private class ShortScoreDocComparator : ScoreDocComparator
        {
            private readonly short[] fieldOrder;

            public ShortScoreDocComparator(short[] fieldOrder)
            {
                this.fieldOrder = fieldOrder;
            }

            public int Compare(ScoreDoc i, ScoreDoc j)
            {
                int fi = fieldOrder[i.doc];
                int fj = fieldOrder[j.doc];
                if (fi < fj)
                {
                    return -1;
                }
                if (fi > fj)
                {
                    return 1;
                }
                return 0;
            }

            public IComparable SortValue(ScoreDoc i)
            {
                return fieldOrder[i.doc];
            }

            public int SortType()
            {
                return SortField.SHORT;
            }
        }

        ///   <summary> * Returns a comparator for sorting hits according to a field containing shorts. </summary>
        ///   * <param name="reader">  Index to use. </param>
        ///   * <param name="fieldname">  Fieldable containg integer values. </param>
        ///   * <returns>  Comparator for sorting hits. </returns>
        ///   * <exception cref="IOException"> If an error occurs reading the index. </exception>
        internal static ScoreDocComparator ComparatorShort(IndexReader reader, string fieldname)
        {
            string field = string.Intern(fieldname);
            short[] fieldOrder = FieldCache_Fields.DEFAULT.GetShorts(reader, field);
            return new ShortScoreDocComparator(fieldOrder);
        }

        private class IntScoreDocComparator : ScoreDocComparator
        {
            private readonly int[] fieldOrder;

            public IntScoreDocComparator(int[] fieldOrder)
            {
                this.fieldOrder = fieldOrder;
            }

            public int Compare(ScoreDoc i, ScoreDoc j)
            {
                int fi = fieldOrder[i.doc];
                int fj = fieldOrder[j.doc];
                if (fi < fj)
                    return -1;
                if (fi > fj)
                    return 1;
                return 0;
            }

            public IComparable SortValue(ScoreDoc i)
            {
                return fieldOrder[i.doc];
            }

            public int SortType()
            {
                return SortField.INT;
            }
        }

        ///  
        ///   <summary> * Returns a comparator for sorting hits according to a field containing integers. </summary>
        ///   * <param name="reader">  Index to use. </param>
        ///   * <param name="fieldname">  Fieldable containg integer values. </param>
        ///   * <returns>  Comparator for sorting hits. </returns>
        ///   * <exception cref="IOException"> If an error occurs reading the index. </exception>
        ///   
        internal static ScoreDocComparator ComparatorInt(IndexReader reader, string fieldname)
        {
            string field = string.Intern(fieldname);
            int[] fieldOrder = FieldCache_Fields.DEFAULT.GetInts(reader, field);
            return new IntScoreDocComparator(fieldOrder);
        }

        private class LongScoreDocComparator : ScoreDocComparator
        {
            private readonly long[] fieldOrder;

            public LongScoreDocComparator(long[] fieldOrder)
            {
                this.fieldOrder = fieldOrder;
            }

            public int Compare(ScoreDoc i, ScoreDoc j)
            {
                long li = fieldOrder[i.doc];
                long lj = fieldOrder[j.doc];
                if (li < lj)
                    return -1;
                if (li > lj)
                    return 1;
                return 0;
            }

            public IComparable SortValue(ScoreDoc i)
            {
                return fieldOrder[i.doc];
            }

            public int SortType()
            {
                return SortField.LONG;
            }
        }

        ///   <summary> * Returns a comparator for sorting hits according to a field containing integers. </summary>
        ///   * <param name="reader">  Index to use. </param>
        ///   * <param name="fieldname">  Fieldable containg integer values. </param>
        ///   * <returns>  Comparator for sorting hits. </returns>
        ///   * <exception cref="IOException"> If an error occurs reading the index. </exception>
        internal static ScoreDocComparator ComparatorLong(IndexReader reader, string fieldname)
        {
            string field = string.Intern(fieldname);
            long[] fieldOrder = ExtendedFieldCache_Fields.EXT_DEFAULT.GetLongs(reader, field);
            return new LongScoreDocComparator(fieldOrder);
        }

        private class FloatScoreDocComparator : ScoreDocComparator
        {
            private readonly float[] fieldOrder;

            public FloatScoreDocComparator(float[] fieldOrder)
            {
                this.fieldOrder = fieldOrder;
            }

            public int Compare(ScoreDoc i, ScoreDoc j)
            {
                float fi = fieldOrder[i.doc];
                float fj = fieldOrder[j.doc];
                if (fi < fj)
                    return -1;
                if (fi > fj)
                    return 1;
                return 0;
            }

            public IComparable SortValue(ScoreDoc i)
            {
                return fieldOrder[i.doc];
            }

            public int SortType()
            {
                return SortField.FLOAT;
            }
        }

        ///   <summary> * Returns a comparator for sorting hits according to a field containing floats. </summary>
        ///   * <param name="reader">  Index to use. </param>
        ///   * <param name="fieldname">  Fieldable containg float values. </param>
        ///   * <returns>  Comparator for sorting hits. </returns>
        ///   * <exception cref="IOException"> If an error occurs reading the index. </exception>
        internal static ScoreDocComparator ComparatorFloat(IndexReader reader, string fieldname)
        {
            string field = string.Intern(fieldname);
            float[] fieldOrder = FieldCache_Fields.DEFAULT.GetFloats(reader, field);
            return new FloatScoreDocComparator(fieldOrder);
        }

        private class DoubleScoreDocComparator : ScoreDocComparator
        {
            private readonly double[] fieldOrder;

            public DoubleScoreDocComparator(double[] fieldOrder)
            {
                this.fieldOrder = fieldOrder;
            }

            public int Compare(ScoreDoc i, ScoreDoc j)
            {
                double di = fieldOrder[i.doc];
                double dj = fieldOrder[j.doc];
                if (di < dj)
                    return -1;
                if (di > dj)
                    return 1;
                return 0;
            }

            public IComparable SortValue(ScoreDoc i)
            {
                return fieldOrder[i.doc];
            }

            public int SortType()
            {
                return SortField.DOUBLE;
            }
        }

        ///   <summary> * Returns a comparator for sorting hits according to a field containing doubles. </summary>
        ///   * <param name="reader">  Index to use. </param>
        ///   * <param name="fieldname">  Fieldable containg float values. </param>
        ///   * <returns>  Comparator for sorting hits. </returns>
        ///   * <exception cref="IOException"> If an error occurs reading the index. </exception>
        internal static ScoreDocComparator ComparatorDouble(IndexReader reader, string fieldname)
        {
            string field = string.Intern(fieldname);
            double[] fieldOrder = ExtendedFieldCache_Fields.EXT_DEFAULT.GetDoubles(reader, field);
            return new DoubleScoreDocComparator(fieldOrder);
        }

        private class StringScoreDocComparator : ScoreDocComparator
        {
            private readonly StringIndex index;

            public StringScoreDocComparator(StringIndex index)
            {
                this.index = index;
            }

            public int Compare(ScoreDoc i, ScoreDoc j)
            {
                int fi = index.order[i.doc];
                int fj = index.order[j.doc];
                if (fi < fj)
                    return -1;
                if (fi > fj)
                    return 1;
                return 0;
            }

            public IComparable SortValue(ScoreDoc i)
            {
                return index.lookup[index.order[i.doc]];
            }

            public int SortType()
            {
                return SortField.STRING;
            }
        }

        ///   <summary> * Returns a comparator for sorting hits according to a field containing strings. </summary>
        ///   * <param name="reader">  Index to use. </param>
        ///   * <param name="fieldname">  Fieldable containg string values. </param>
        ///   * <returns>  Comparator for sorting hits. </returns>
        ///   * <exception cref="IOException"> If an error occurs reading the index. </exception>
        internal static ScoreDocComparator ComparatorString(IndexReader reader, string fieldname)
        {
            string field = string.Intern(fieldname);
            StringIndex index = FieldCache_Fields.DEFAULT.GetStringIndex(reader, field);
            return new StringScoreDocComparator(index);
        }

        private class LocaleScoreDocComparator : ScoreDocComparator
        {
            private readonly string[] index;
            private readonly CultureInfo locale;

            public LocaleScoreDocComparator(CultureInfo locale, string[] index)
            {
                this.locale = locale;
                this.index = index;
            }

            public int Compare(ScoreDoc i, ScoreDoc j)
            {
                string @is = index[i.doc];
                string js = index[j.doc];
                if (@is == js)
                {
                    return 0;
                }
                else if (@is == null)
                {
                    return -1;
                }
                else if (js == null)
                {
                    return 1;
                }
                else
                {
                    return string.Compare(@is, js, locale, CompareOptions.None);
                }
            }

            public IComparable SortValue(ScoreDoc i)
            {
                return index[i.doc];
            }

            public int SortType()
            {
                return SortField.STRING;
            }
        }

        ///   <summary> * Returns a comparator for sorting hits according to a field containing strings. </summary>
        ///   * <param name="reader">  Index to use. </param>
        ///   * <param name="fieldname">  Fieldable containg string values. </param>
        ///   * <returns>  Comparator for sorting hits. </returns>
        ///   * <exception cref="IOException"> If an error occurs reading the index. </exception>
        internal static ScoreDocComparator ComparatorStringLocale(IndexReader reader, string fieldname, CultureInfo locale)
        {
            string field = string.Intern(fieldname);
            string[] index = FieldCache_Fields.DEFAULT.GetStrings(reader, field);
            return new LocaleScoreDocComparator(locale, index);
        }

        ///   <summary> * Returns a comparator for sorting hits according to values in the given field.
        ///   * The terms in the field are looked at to determine whether they contain integers,
        ///   * floats or strings.  Once the type is determined, one of the other static methods
        ///   * in this class is called to get the comparator. </summary>
        ///   * <param name="reader">  Index to use. </param>
        ///   * <param name="fieldname">  Fieldable containg values. </param>
        ///   * <returns>  Comparator for sorting hits. </returns>
        ///   * <exception cref="IOException"> If an error occurs reading the index. </exception>
        internal static ScoreDocComparator ComparatorAuto(IndexReader reader, string fieldname)
        {
            string field = string.Intern(fieldname);
            object lookupArray = ExtendedFieldCache_Fields.EXT_DEFAULT.GetAuto(reader, field);
            if (lookupArray is StringIndex)
            {
                return ComparatorString(reader, field);
            }
            else if (lookupArray is int[])
            {
                return ComparatorInt(reader, field);
            }
            else if (lookupArray is long[])
            {
                return ComparatorLong(reader, field);
            }
            else if (lookupArray is float[])
            {
                return ComparatorFloat(reader, field);
            }
            else if (lookupArray is string[])
            {
                return ComparatorString(reader, field);
            }
            else
            {
                throw new RuntimeException("unknown data type in field '" + field + "'");
            }
        }
    }
}