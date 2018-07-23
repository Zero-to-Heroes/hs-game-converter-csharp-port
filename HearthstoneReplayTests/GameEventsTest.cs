#region

using System; 
using System.Collections.Generic;
using HearthstoneReplays;
using HearthstoneReplays.Parser;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

#endregion

namespace HearthstoneReplayTests
{ 
	[TestClass]
	public class GameEventsTest
	{
		[TestMethod] 
		public void Test()
		{ 
			//List<string> logFile = TestDataReader.GetInputFile("Power_2.log.txt");
			//ReplayParser parser = new ReplayParser();
			//parser = new ReplayParser();
			//Action<object> initialCallback = (call) => Console.WriteLine(call);
			//Action<object> jsonCallback = (gameEvent) => initialCallback(JsonConvert.SerializeObject(gameEvent));
			
			//parser.Init(jsonCallback);
			////parser.Init(initialCallback);

			//foreach (string logLine in logFile) 
			//{
			//	parser.ReadLine(logLine);
			//}
		} 
	}
}
