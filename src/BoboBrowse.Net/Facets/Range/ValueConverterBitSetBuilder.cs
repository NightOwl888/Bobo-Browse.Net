﻿// Version compatibility level: 3.1.0
namespace BoboBrowse.Net.Facets.Range
{
    using BoboBrowse.Net.Facets.Data;
    using BoboBrowse.Net.Facets.Filter;
    using Lucene.Net.Util;
    using System;

    public class ValueConverterBitSetBuilder<T> : IBitSetBuilder
    {
        private readonly FacetValueConverter facetValueConverter;
        private readonly String[] vals;
        private readonly boolean takeCompliment;

        public ValueConverterBitSetBuilder(FacetValueConverter facetValueConverter, String[] vals, bool takeCompliment) 
        {
            this.facetValueConverter = facetValueConverter;
            this.vals = vals;
            this.takeCompliment = takeCompliment;    
        }

        public override OpenBitSet BitSet(FacetDataCache<T> dataCache)
        {
            int[] index = facetValueConverter.Convert(dataCache, vals);

            OpenBitSet bitset = new OpenBitSet(dataCache.valArray.Size);
            foreach (int i in index)
            {
                bitset.FastSet(i);
            }
            if (takeCompliment)
            {
                // flip the bits
                for (int i = 0; i < index.Length; ++i)
                {
                    bitset.FastFlip(i);
                }
            }
            return bitset;
        }
    }
}