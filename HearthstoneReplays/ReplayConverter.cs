#region

using System;
using System.Collections.Generic;
using System.Xml;
using System.Text;
using System.Xml.Serialization;
using HearthstoneReplays.Parser;
using HearthstoneReplays.Parser.ReplayData;
using System.IO;

#endregion

namespace HearthstoneReplays
{
	public class ReplayConverter
	{
		//private static readonly 
		
		public string xmlFromLogs(string logString)
		{
			string replaced = logString.Replace("\r\n", "\n");
			Console.Write(replaced);
			string[] lines = replaced.Split('\n');
			Console.Write("Processing " + lines.Length + " lines");
			HearthstoneReplay replay = new ReplayParser().FromString(lines);
			Console.Write("Converted into replay: " + replay);

			String xmlReplay = xmlFromReplay(replay);
			Console.Write("XML from replay: " + xmlReplay);
			return xmlReplay;
		}
		
		public String xmlFromReplay(HearthstoneReplay replay)
		{
			XmlSerializer serializer = new XmlSerializer(typeof(HearthstoneReplay));
			
			var memoryStream = new MemoryStream();
			var streamWriter = new StreamWriter(memoryStream, Encoding.UTF8);

			serializer.Serialize(streamWriter, replay);
			
			memoryStream.Position = 0;
			return new StreamReader(memoryStream).ReadToEnd();
		}

		//public static void Serialize(HearthstoneReplay replay, string filePath)
		//{
		//	var ns = new XmlSerializerNamespaces();
		//	ns.Add("", "");
		//	var settings = new XmlWriterSettings {CloseOutput = true, Indent = true, IndentChars = "\t"};
		//	using(TextWriter writer = new StreamWriter(filePath))
		//	using(var xmlWriter = XmlWriter.Create(writer, settings))
		//	{
		//		xmlWriter.WriteStartDocument();
		//		xmlWriter.WriteDocType("hsreplay", null, string.Format(@"http://hearthsim.info/hsreplay/dtd/hsreplay-{0}.dtd", replay.Version),
		//		                       null);
		//		Serializer.Serialize(xmlWriter, replay, ns);
		//		xmlWriter.WriteEndDocument();
		//	}
		//}

		//public static HearthstoneReplay Deserialize(TextReader reader)
		//{
		//	return (HearthstoneReplay)Serializer.Deserialize(reader);
		//}
	}
}