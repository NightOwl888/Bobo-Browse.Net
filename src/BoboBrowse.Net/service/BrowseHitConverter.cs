using System;
using System.Collections.Generic;

namespace com.browseengine.bobo.service
{


	using BrowseHit = com.browseengine.bobo.api.BrowseHit;
	using Converter = com.thoughtworks.xstream.converters.Converter;
	using MarshallingContext = com.thoughtworks.xstream.converters.MarshallingContext;
	using UnmarshallingContext = com.thoughtworks.xstream.converters.UnmarshallingContext;
	using HierarchicalStreamReader = com.thoughtworks.xstream.io.HierarchicalStreamReader;
	using HierarchicalStreamWriter = com.thoughtworks.xstream.io.HierarchicalStreamWriter;

///
/// <summary> * Marshaling code for #BrowseHit </summary>
/// 
	public class BrowseHitConverter : Converter
	{

		public virtual void marshal(object obj, HierarchicalStreamWriter writer, MarshallingContext ctx)
		{
			writer.startNode("hit");
			BrowseHit hit =(BrowseHit)obj;
			writer.addAttribute("score", Convert.ToString(hit.getScore()));
			writer.addAttribute("docid", Convert.ToString(hit.getDocid()));


			Dictionary<string, string[]> fieldVals =hit.getFieldValues();
			writer.addAttribute("numfields", fieldVals==null ? "0" : Convert.ToString(fieldVals.size()));

			if (fieldVals!=null)
			{

				IEnumerator<string> iter =fieldVals.Keys.GetEnumerator();
				while(iter.MoveNext())
				{
					string name =iter.Current;
					writer.startNode(name);
					writer.Value = Arrays.ToString(fieldVals.get(name));
					writer.endNode();
				}
			}
			writer.endNode();
		}

		public virtual object unmarshal(HierarchicalStreamReader reader, UnmarshallingContext ctx)
		{
			BrowseHit hit =new BrowseHit();
			reader.moveDown();
			string scoreString =reader.getAttribute("score");
			if (scoreString!=null)
			{
				hit.setScore(Convert.ToSingle(scoreString));
			}
			string docidString =reader.getAttribute("docid");
			if (docidString!=null)
			{
				hit.setDocid(Convert.ToInt32(docidString));
			}

			int numFields =0;
			string fieldCountString =reader.getAttribute("numfields");
			if (fieldCountString!=null)
			{
				numFields=Convert.ToInt32(fieldCountString);
			}

			Dictionary<string, string[]> fieldVals =new Dictionary<string, string[]>();
			hit.setFieldValues(fieldVals);
			for (int i=0;i<numFields;++i)
			{
				reader.moveDown();
				string fieldname =reader.getNodeName();
				string fieldval = reader.Value;
				string s2 =fieldval.Substring(1, fieldval.Length-1 - 1);
				string[] parts =s2.Split(", ");
				fieldVals.put(fieldname, parts);
				reader.moveUp();
			}
			reader.moveUp();
			return hit;
		}

		public virtual bool canConvert(System.Type clazz)
		{
			return typeof(BrowseHit).Equals(clazz);
		}
	}

}