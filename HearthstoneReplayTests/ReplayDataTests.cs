#region

using System;
using System.Threading;
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
            List<string> logFile = TestDataReader.GetInputFile("treeants_to_hand.txt");
            HearthstoneReplay replay = new ReplayParser().FromString(logFile);
			string xml = new ReplayConverter().xmlFromReplay(replay);
            Console.Write(xml);
        }

        [TestMethod]
        public void FullNonReg()
        {
            NodeParser.DevMode = true;
            var fileOutputs = new[]
            {
                new { FileName = "army_of_the_dead", Events = new[]
                {
                    new { EventName = "CARD_REMOVED_FROM_DECK", ExpectedEventCount = 3 },
                    new { EventName = "BURNED_CARD", ExpectedEventCount = 1 },
                }},
                new { FileName = "burned_cards", Events = new[]
                {
                    new { EventName = "BURNED_CARD", ExpectedEventCount = 1 },
                }},
                new { FileName = "mad_scientist", Events = new[]
                {
                    new { EventName = "SECRET_PLAYED_FROM_DECK", ExpectedEventCount = 1 },
                }},
                new { FileName = "new_meta_log", Events = new[]
                {
                    new { EventName = "CARD_PLAYED", ExpectedEventCount = 41 },
                }},
            };

            foreach (dynamic fileOutput in fileOutputs)
            {
                var testedFileName = fileOutput.FileName as string;
                var events = new Dictionary<string, int>();
                GameEventHandler.EventProvider = (evt) =>
                {
                    var evtName = JsonConvert.DeserializeObject<JObject>(evt).First.First.ToString();
                    var value = 0;
                    if (events.ContainsKey(evtName))
                    {
                        value = events[evtName];
                    }
                    events[evtName] = value + 1;
                };
                List<string> logFile = TestDataReader.GetInputFile(testedFileName + ".txt");
                HearthstoneReplay replay = new ReplayParser().FromString(logFile);
                Thread.Sleep(500);

                foreach (dynamic evt in fileOutput.Events)
                {
                    var expectedEventCount = (int)evt.ExpectedEventCount;
                    var testedEventName = evt.EventName as string;
                    Assert.IsTrue(events.ContainsKey(testedEventName), "Missing event: " + testedFileName + " / " + testedEventName);
                    Assert.AreEqual(expectedEventCount, events[testedEventName], testedFileName + " / " + testedEventName);
                }
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
