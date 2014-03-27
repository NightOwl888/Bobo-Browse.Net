using System;
using Lucene.Net.Util;
using LuceneExt.Net.Bitset;

namespace LuceneExt.Net.Compression
{
    ///
    /// <summary> * Implementation of the p4delta algorithm for sorted integer arrays based on
    /// * 
    /// * 1. Original Algorithm from
    /// * http://homepages.cwi.nl/~heman/downloads/msthesis.pdf 2. Optimization and
    /// * variation from http://www2008.org/papers/pdf/p387-zhangA.pdf
    /// * 
    /// * This class is a wrapper around a CompressedSegment based on Lucene OpenBitSet </summary>
    /// 
    [Obsolete, Serializable]
    public class P4DSet : CompressedSortedIntegerSegment
    {
        private static int INVALID = -1;

        // Maximum bits that can be used = 32

        // Byte Mask
        private static int BYTE_MASK = 8;

        // 32 bits for retaining base value
        private static int BASE_MASK = 32;

        // Header size
        private static int HEADER_MASK = BYTE_MASK + BASE_MASK;

        // Parameters for the compressed set
        private int _b = INVALID;

        private int _base = INVALID;

        private int _batchSize = INVALID;

        private int _exceptionCount = INVALID;

        private int _exceptionOffset = INVALID;

        private int[] op = null;

        public virtual void setParam(int @base, int b, int batchSize, int exceptionCount)
        {
            this._base = @base;
            this._b = b;
            this._batchSize = batchSize;
            this._exceptionCount = exceptionCount;
            this._exceptionOffset = HEADER_MASK + _b * _batchSize;
            op = new int[_batchSize];
        }

        ///  
        ///<summary>P4D compression algorithm
        ///   *  </summary>
        ///   * <param name="input">
        ///   * @return </param>
        ///   * <exception cref="ArgumentException"> </exception>
        ///   
        ///  
        ///<summary>Alternate implementation for compress
        ///   *  </summary>
        ///   * <param name="input"> </param>
        ///   * <returns> compressed bitset </returns>
        ///   * <exception cref="ArgumentException"> </exception>
        ///   
        //JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public OpenBitSet compress(int[] input) throws ArgumentException
        public virtual OpenBitSet Compress(int[] input)
        {
            if (_base == INVALID || _b == INVALID)
                throw new ArgumentException(" Codec not initialized correctly ");

            int BATCH_MAX = 1 << (_b - 1);
            // int validCount = (_batchSize - _exceptionCount)*_b +SIZE_MASK+BASE_MASK;

            // Compression mumbo jumbo

            // Set Size -b+base+compressedSet+exception*BASE_MASK
            MyOpenBitSet compressedSet = new MyOpenBitSet((_batchSize) * _b + HEADER_MASK + _exceptionCount * (BASE_MASK));

            // Load the b
            copyBits(compressedSet, _b, 0, BYTE_MASK);

            // copy the base value to BASE_MASK offset
            copyBits(compressedSet, _base, BYTE_MASK, BASE_MASK);

            // Offset is the offset of the next location to place the value
            int offset = BYTE_MASK + BASE_MASK;
            int exceptionOffset = _exceptionOffset;
            int exceptionIndex = 0;

            // 1. Walk the list
            // TODO : Optimize this process.
            for (int i = 0; i < _batchSize; i++)
            {
                // else copy in the end
                if (input[i] < BATCH_MAX)
                {
                    copyBits(compressedSet, input[i], offset, _b);

                }
                else
                {
                    // Copy the value to the exception location
                    // Add a bit marker to place
                    copyBits(compressedSet, 1 << (_b - 1) | exceptionIndex++, offset, _b);

                    // Copy the patch value to patch offset location
                    copyBits(compressedSet, input[i], exceptionOffset, BASE_MASK);

                    // reset exceptionDelta
                    exceptionOffset += BASE_MASK;
                }

                offset += _b;
            }
            return compressedSet;
        }

        private void copyBits(MyOpenBitSet compressedSet, int val, int offset, int length)
        {
            for (int i = 0; i < length; i++)
                compressedSet.FastSetAs(offset + i, val >> i & 1);
        }

        // Method to allow iteration in decompressed form
        public virtual int @get(OpenBitSet compressedSet, int index)
        {
            int retVal = 0;
            // This is an exception
            if (compressedSet.GetBit((index + 1) * _b + HEADER_MASK - 1) == 1)
            {

                // Get the exception index
                for (int j = 0; j < _b - 1; j++)
                {
                    // if(compressedSet.fastGet(i*_b+j+header))
                    retVal |= (compressedSet.GetBit(index * _b + j + HEADER_MASK) << j);
                }

                int exOffset = _exceptionOffset + retVal * BASE_MASK;
                retVal = 0;
                // Get the actual value
                for (int j = 0; j < BASE_MASK; j++)
                {
                    // if(compressedSet.fastGet(i*_b+j+header))
                    retVal |= (compressedSet.GetBit(exOffset + j) << j);
                }
                return retVal;
            }
            else
            {
                for (int j = 0; j < _b - 1; j++)
                {
                    // if(compressedSet.fastGet(i*_b+j+header))
                    retVal |= (compressedSet.GetBit(index * _b + j + HEADER_MASK) << j);
                }
                return retVal;

            }

        }

        public virtual int[] Decompress(OpenBitSet compressedSet)
        {
            // reuse o/p
            op[0] = _base;

            // Offset of the exception list
            int exceptionOffset = HEADER_MASK + _b * _batchSize;

            // explode and patch
            for (int i = 1; i < _batchSize; i++)
            {
                // This is an exception
                if (compressedSet.GetBit((i + 1) * _b + HEADER_MASK - 1) == 1)
                {
                    for (int j = 0; j < BASE_MASK; j++)
                    {
                        // if(compressedSet.fastGet(i*_b+j+header))
                        op[i] |= (compressedSet.GetBit(exceptionOffset + j) << j);
                    }

                    exceptionOffset += BASE_MASK;

                }
                else
                {
                    for (int j = 0; j < _b - 1; j++)
                    {
                        // if(compressedSet.fastGet(i*_b+j+header))
                        op[i] |= (compressedSet.GetBit(i * _b + j + HEADER_MASK) << j);
                    }

                }
                op[i] += op[i - 1];
            }
            return op;
        }

        ///  
        ///<summary>Method not supported
        ///   *  </summary>
        ///   
        //JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public int[] decompress(DocIdBitSet compressedSet) throws ArgumentException
        public virtual int[] Decompress(DocIdBitSet compressedSet)
        {
            return null;
        }

        //JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public long[] compressAlt(int[] inputSet) throws ArgumentException
        public virtual long[] CompressAlt(int[] inputSet)
        {
            // TODO Auto-generated method stub
            return null;
        }

    }

}