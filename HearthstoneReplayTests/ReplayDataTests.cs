#region

using System;
using System.Collections.Generic;
using HearthstoneReplays;
using HearthstoneReplays.Events;
using HearthstoneReplays.Parser;
using HearthstoneReplays.Parser.ReplayData;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion
 
namespace HearthstoneReplayTests
{
	[TestClass]
	public class ReplayDataTests
	{
		[TestMethod]
		public void Test()  
		{
            NodeParser.DevMode = true;
            GameEventHandler.EventProvider = (evt) => Console.WriteLine(evt);
            //List<string> logFile = TestDataReader.GetInputFile("Power_1.log.txt");
            List<string> logFile = TestDataReader.GetInputFile("mass_hysteria.txt");
            //while (true)
            //{
                HearthstoneReplay replay = new ReplayParser().FromString(logFile);
			    string xml = new ReplayConverter().xmlFromReplay(replay);
            //}
            //System.Threading.Thread.Sleep(500);
            //Console.Write(xml);
        } 

		//[TestMethod]
		//public void TestMetaData()
		//{
		//	List<string> logFile = TestDataReader.GetInputFile("Power_1.log.txt");
		//	HearthstoneReplay replay = new ReplayParser().FromString(logFile);
		//	Assert.AreEqual((int)GameType.GT_ARENA, replay.Games[0].GameType);
		//	Assert.AreEqual(25252, replay.Games[0].BuildNumber);
		//	Assert.AreEqual((int)FormatType.FT_WILD, replay.Games[0].FormatType);
		//	Assert.AreEqual(2901, replay.Games[0].ScenarioID);
		//}
	}
}
