using System;

namespace com.browseengine.bobo.service
{


	using FieldConfiguration = com.browseengine.bobo.config.FieldConfiguration;
	using XStream = com.thoughtworks.xstream.XStream;
	using Converter = com.thoughtworks.xstream.converters.Converter;
	using MarshallingContext = com.thoughtworks.xstream.converters.MarshallingContext;
	using UnmarshallingContext = com.thoughtworks.xstream.converters.UnmarshallingContext;
	using HierarchicalStreamReader = com.thoughtworks.xstream.io.HierarchicalStreamReader;
	using HierarchicalStreamWriter = com.thoughtworks.xstream.io.HierarchicalStreamWriter;
	using DomDriver = com.thoughtworks.xstream.io.xml.DomDriver;

	public class FieldConfConverter : Converter
	{
	  public virtual void marshal(object obj, HierarchicalStreamWriter writer, MarshallingContext ctx)
	  {
//    FieldConfiguration fconf=(FieldConfiguration)obj;
//    Iterator<FieldPlugin> iter=null;//fconf.getIterator();
//    while(iter.hasNext()){
//      FieldPlugin fplugin=iter.next();
//      writer.startNode("field");
//      writer.startNode("name");
//      writer.addAttribute("type", fplugin.getTypeString());
//      writer.setValue(fplugin.getName());
//      writer.endNode();
//      Properties props=fplugin.getProperties();
//      if (props!=null){
//	      Enumeration<?> propIter=props.propertyNames();
//	      while(propIter.hasMoreElements()){
//	        String name=(String)propIter.nextElement();
//	        writer.startNode("param");
//	        writer.addAttribute("name", name);
//	        writer.addAttribute("value", props.getProperty(name));
//	        writer.endNode();
//	      }
//      }
//      writer.endNode();
//    }
	  }

	  public virtual object unmarshal(HierarchicalStreamReader reader, UnmarshallingContext ctx)
	  {
		FieldConfiguration fconf =new FieldConfiguration();
		while(reader.hasMoreChildren())
		{
		  reader.moveDown();
		  string nodeName =reader.getNodeName();
		  if ("field".Equals(nodeName))
		  {
			Properties props =new Properties();
			string pluginName =null;
			string pluginType =null;
			while(reader.hasMoreChildren())
			{
			  reader.moveDown();
			  string fieldNodeName =reader.getNodeName();
			  if ("param".Equals(fieldNodeName))
			  {
				string N =reader.getAttribute("name");
				string V =reader.getAttribute("value");
				props.put(N, V);
			  }
			  else if ("name".Equals(fieldNodeName))
			  {
				pluginName=reader.Value.Trim();
				pluginType=reader.getAttribute("type");
			  }
			  reader.moveUp();
			}
			if (pluginName!=null)
			{
			  fconf.addPlugin(pluginName, pluginType, props);
			}
		  }
		  reader.moveUp();
		}
		return fconf;
	  }

	  public virtual bool canConvert(System.Type cls)
	  {
		return typeof(FieldConfiguration).Equals(cls);
	  }

//JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public static void main(String[] args) throws Exception
	  static void Main(string[] args)
	  {
		File location =new File("/Users/john/project/bobo_dev_1_5_0/bobo/cardata/cartag/field.xml");
		FileReader reader =new FileReader(location);
		XStream xstream =new XStream(new DomDriver());
		xstream.registerConverter(new FieldConfConverter());
		xstream.alias("field-info", typeof(FieldConfiguration));
		FieldConfiguration fconf =(FieldConfiguration)xstream.fromXML(reader);

		Console.WriteLine(xstream.toXML(fconf));
	  }
	}

}