#region

using System;
using System.Collections.Generic;
using HearthstoneReplays;
using HearthstoneReplays.Events;
using HearthstoneReplays.Parser;
using HearthstoneReplays.Parser.ReplayData;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
            List<string> logFile = TestDataReader.GetInputFile("army_of_the_dead.txt");
            HearthstoneReplay replay = new ReplayParser().FromString(logFile);
			string xml = new ReplayConverter().xmlFromReplay(replay);
        }

        [TestMethod]
        public void FullNonReg()
        {
            NodeParser.DevMode = true;
            var fileOutputs = new[]
            {
                new { FileName = "army_of_the_dead", EventName = "CARD_REMOVED_FROM_DECK", ExpectedEventCount = 3 },
            };

            foreach (dynamic fileOutput in fileOutputs)
            {
                var testedFileName = fileOutput.FileName as string;
                var testedEventName = fileOutput.EventName as string;
                var expectedEventCount = (int)fileOutput.ExpectedEventCount;
                var totalEvents = 0;
                GameEventHandler.EventProvider = (evt) =>
                {
                    var evtName = JsonConvert.DeserializeObject<JObject>(evt).First.First.ToString();
                    if (evtName == testedEventName)
                    {
                        totalEvents++;
                    }
                };
                List<string> logFile = TestDataReader.GetInputFile(testedFileName + ".txt");
                HearthstoneReplay replay = new ReplayParser().FromString(logFile);
                Assert.AreEqual(expectedEventCount, totalEvents, testedFileName + " / " + testedEventName);
            }
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
