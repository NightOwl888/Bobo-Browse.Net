﻿//* Bobo Browse Engine - High performance faceted/parametric search implementation 
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

namespace BoboBrowse.Net.Util
{
    using System;

    public class StringArrayComparator : IComparable<StringArrayComparator>, IComparable
    {
        private readonly string[] vals;

        public StringArrayComparator(string[] vals)
        {
            this.vals = vals;
        }

        public virtual int CompareTo(StringArrayComparator node)
        {
            string[] o = node.vals;
            if (vals == o)
            {
                return 0;
            }
            if (vals == null)
            {
                return -1;
            }
            if (o == null)
            {
                return 1;
            }
            for (int i = 0; i < vals.Length; ++i)
            {
                if (i >= o.Length)
                {
                    return 1;
                }
                int compVal = vals[i].CompareTo(o[i]);
                if (compVal != 0)
                {
                    return compVal;
                }
            }
            if (vals.Length == o.Length)
            {
                return 0;
            }
            return -1;
        }

        public int CompareTo(object obj)
        {
            return CompareTo((StringArrayComparator)obj);
        }
    }
}
