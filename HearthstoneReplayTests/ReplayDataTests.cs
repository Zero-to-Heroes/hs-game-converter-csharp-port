#region

using System;
using System.Threading;
using System.Collections.Generic;
using System.Threading.Tasks;
using HearthstoneReplays;
using HearthstoneReplays.Events;
using HearthstoneReplays.Parser;
using HearthstoneReplays.Parser.ReplayData;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;
using Newtonsoft.Json.Serialization;
using System.Reflection;

#endregion

namespace HearthstoneReplayTests
{
    [TestClass]
    public class ReplayDataTests
    {
        [TestMethod]
        public void Test()
        {
            //NodeParser.DevMode = true;
            var serializerSettings = new JsonSerializerSettings()
            {
                ContractResolver = new IgnorePropertiesResolver(new[] { 
                    "GameState", 
                    "ReplayXml",
                    "LocalPlayer",
                    "OpponentPlayer", 
                    "GameStateReport", 
                    "Game" 
                })
            };
            GameEventHandler.EventProviderAll = (IList<GameEvent> gameEvents) =>
            {
                foreach (GameEvent gameEvent in gameEvents)
                {
                    dynamic Value = gameEvent.Value;
                    //var shouldLog = true;
                    //var shouldLog = gameEvent.Type != "GAME_STATE_UPDATE" && gameEvent.Type != "GAME_END";
                    var shouldLog = gameEvent.Type == "BATTLEGROUNDS_PLAYER_BOARD" || gameEvent.Type == "BATTLEGROUNDS_NEXT_OPPONENT";
                    if (shouldLog)
                    {
                        //var serialized = JsonConvert.SerializeObject(gameEvent);
                        var serialized = JsonConvert.SerializeObject(gameEvent, serializerSettings);
                        Console.WriteLine(serialized + ",");
                    }
                }
            };
            GameEventHandler.EventProvider = (GameEvent gameEvent) =>
            {
                dynamic Value = gameEvent.Value;
                //var shouldLog = true;
                var shouldLog = gameEvent.Type != "GAME_STATE_UPDATE" && gameEvent.Type != "GAME_END";
                //var shouldLog = new List<string>() {
                //    "BATTLEGROUNDS_PLAYER_BOARD",
                //    "BATTLEGROUNDS_NEXT_OPPONENT",
                //    "TURN_START"
                //}.Contains(gameEvent.Type);
                if (shouldLog)
                {
                    //var serialized = JsonConvert.SerializeObject(gameEvent);
                    var serialized = JsonConvert.SerializeObject(gameEvent, serializerSettings);
                    //if (serialized.Contains("\"TargetCardId\":\"TB_BaconShop_HERO_53\""))
                    //{
                    Console.WriteLine(serialized + ",");
                    //}
                }
            };
            List<string> logFile = TestDataReader.GetInputFile("bugs.txt");
            logFile.Insert(0, "START_CATCHING_UP");
            logFile.Add("END_CATCHING_UP");
            var parser = new ReplayParser();
            HearthstoneReplay replay = parser.FromString(logFile);
            Thread.Sleep(3000);
            GC.Collect();
            Thread.Sleep(3000);
            string xml = new ReplayConverter().xmlFromReplay(replay);
            //Console.Write(xml);
        }

        [TestMethod]
        public void LeakTest()
        {
            var serializerSettings = new JsonSerializerSettings()
            {
                ContractResolver = new IgnorePropertiesResolver(new[] { "GameState", "ReplayXml", "LocalPlayer", "OpponentPlayer", "GameStateReport", "Game" })
            };

            var plugin = new ReplayConverterPlugin();
            plugin.onGlobalEvent += (a, b) => Console.WriteLine(a + " // " + b);
            plugin.initRealtimeLogConversion(null);
            //NodeParser.DevMode = true;
            GameEventHandler.EventProviderAll = (IList<GameEvent> gameEvents) => { };
            List<string> logFile = TestDataReader.GetInputFile("multiple_bg_games.txt");
            List<string> logsForGame = new List<string>();
            for (int i = 0; i < logFile.Count; i++)
            {
                if (logFile[i].Contains("GameState.DebugPrintPower() - CREATE_GAME"))
                {
                    this.ProcessPlugin(plugin, logsForGame);
                    logsForGame.Clear();
                }
                logsForGame.Add(logFile[i]);
            }
            this.ProcessPlugin(plugin, logsForGame);
            Thread.Sleep(3000);
        }

        private void ProcessPlugin(ReplayConverterPlugin plugin, List<string> logsForGame)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            logsForGame.Insert(0, "START_CATCHING_UP");
            logsForGame.Add("END_CATCHING_UP");
            var logsArray = logsForGame.ToArray();
            var result = PluginProcessingAsync(plugin, logsArray).Result;
            Thread.Sleep(3000);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            Thread.Sleep(500);
        }

        private Task<string> PluginProcessingAsync(ReplayConverterPlugin plugin, string[] logLines)
        {
            var t = new TaskCompletionSource<string>();
            plugin.realtimeLogProcessing(logLines, s => t.TrySetResult(null));
            return t.Task;
        }

        //[TestMethod]
        //public void FullNonReg()
        //{
        //    NodeParser.DevMode = true;
        //    var fileOutputs = new[]
        //    {
        //        new { FileName = "desperate_measures", Events = new[]
        //        {
        //            new { EventName = "SECRET_CREATED_IN_GAME", ExpectedEventCount = 2 },
        //        }},
        //        new { FileName = "plague_lord_one_run_defeat", Events = new[]
        //        {
        //            new { EventName = "DECKLIST_UPDATE", ExpectedEventCount = 2 },
        //        }},
        //        new { FileName = "battlegrounds", Events = new[]
        //        {
        //            new { EventName = "MINION_BACK_ON_BOARD", ExpectedEventCount = 84 },
        //            new { EventName = "CARD_REVEALED", ExpectedEventCount = 96 },
        //            new { EventName = "HERO_POWER_CHANGED", ExpectedEventCount = 16 },
        //            new { EventName = "BATTLEGROUNDS_PLAYER_BOARD", ExpectedEventCount = 48 },
        //            new { EventName = "DISCARD_CARD", ExpectedEventCount = 2 },
        //            new { EventName = "DECKLIST_UPDATE", ExpectedEventCount = 0 },
        //        }},
        //        new { FileName = "army_of_the_dead", Events = new[]
        //        {
        //            new { EventName = "CARD_REMOVED_FROM_DECK", ExpectedEventCount = 3 },
        //            new { EventName = "BURNED_CARD", ExpectedEventCount = 1 },
        //            new { EventName = "CARD_STOLEN", ExpectedEventCount = 0 },
        //            new { EventName = "MINION_SUMMONED", ExpectedEventCount = 34 },
        //            new { EventName = "RECRUIT_CARD", ExpectedEventCount = 3 },
        //            new { EventName = "CARD_REVEALED", ExpectedEventCount = 0 },
        //            new { EventName = "HERO_POWER_CHANGED", ExpectedEventCount = 2 },
        //            new { EventName = "BATTLEGROUNDS_PLAYER_BOARD", ExpectedEventCount = 0 },
        //            new { EventName = "DISCARD_CARD", ExpectedEventCount = 0 },
        //            new { EventName = "DECKLIST_UPDATE", ExpectedEventCount = 0 },
        //        }},
        //        new { FileName = "burned_cards", Events = new[]
        //        {
        //            new { EventName = "BURNED_CARD", ExpectedEventCount = 1 },
        //            new { EventName = "CARD_STOLEN", ExpectedEventCount = 0 },
        //            new { EventName = "MINION_SUMMONED", ExpectedEventCount = 1 },
        //            new { EventName = "RECRUIT_CARD", ExpectedEventCount = 0 },
        //            new { EventName = "CARD_REVEALED", ExpectedEventCount = 0 },
        //            new { EventName = "HERO_POWER_CHANGED", ExpectedEventCount = 2 },
        //            new { EventName = "BATTLEGROUNDS_PLAYER_BOARD", ExpectedEventCount = 0 },
        //            new { EventName = "DISCARD_CARD", ExpectedEventCount = 0 },
        //            new { EventName = "DECKLIST_UPDATE", ExpectedEventCount = 0 },
        //        }},
        //        new { FileName = "mad_scientist", Events = new[]
        //        {
        //            new { EventName = "SECRET_PLAYED_FROM_DECK", ExpectedEventCount = 1 },
        //            new { EventName = "CARD_STOLEN", ExpectedEventCount = 0 },
        //            new { EventName = "MINION_SUMMONED", ExpectedEventCount = 7 },
        //            new { EventName = "RECRUIT_CARD", ExpectedEventCount = 0 },
        //            new { EventName = "CARD_REVEALED", ExpectedEventCount = 3 },
        //            new { EventName = "HERO_POWER_CHANGED", ExpectedEventCount = 3 },
        //            new { EventName = "BATTLEGROUNDS_PLAYER_BOARD", ExpectedEventCount = 0 },
        //            new { EventName = "DISCARD_CARD", ExpectedEventCount = 0 },
        //            new { EventName = "DECKLIST_UPDATE", ExpectedEventCount = 0 },
        //        }},
        //        new { FileName = "new_meta_log", Events = new[]
        //        {
        //            new { EventName = "CARD_PLAYED", ExpectedEventCount = 41 },
        //            new { EventName = "CARD_STOLEN", ExpectedEventCount = 0 },
        //            new { EventName = "MINION_SUMMONED", ExpectedEventCount = 13 },
        //            new { EventName = "RECRUIT_CARD", ExpectedEventCount = 0 },
        //            new { EventName = "CARD_REVEALED", ExpectedEventCount = 0 },
        //            new { EventName = "HERO_POWER_CHANGED", ExpectedEventCount = 2 },
        //            new { EventName = "BATTLEGROUNDS_PLAYER_BOARD", ExpectedEventCount = 0 },
        //            new { EventName = "DISCARD_CARD", ExpectedEventCount = 0 },
        //            new { EventName = "DECKLIST_UPDATE", ExpectedEventCount = 0 },
        //        }},
        //        new { FileName = "healing", Events = new[]
        //        {
        //            //new { EventName = "HEALING", ExpectedEventCount = 25 },
        //            new { EventName = "HEALING", ExpectedEventCount = 26 },
        //            new { EventName = "CARD_STOLEN", ExpectedEventCount = 0 },
        //            new { EventName = "MINION_SUMMONED", ExpectedEventCount = 1 },
        //            new { EventName = "RECRUIT_CARD", ExpectedEventCount = 0 },
        //            new { EventName = "CARD_REVEALED", ExpectedEventCount = 0 },
        //            new { EventName = "HERO_POWER_CHANGED", ExpectedEventCount = 2 },
        //            new { EventName = "BATTLEGROUNDS_PLAYER_BOARD", ExpectedEventCount = 0 },
        //            new { EventName = "DISCARD_CARD", ExpectedEventCount = 0 },
        //            new { EventName = "DECKLIST_UPDATE", ExpectedEventCount = 0 },
        //        }},
        //        new { FileName = "armor", Events = new[]
        //        {
        //            new { EventName = "ARMOR_CHANGED", ExpectedEventCount = 55 },
        //            new { EventName = "CARD_STOLEN", ExpectedEventCount = 0 },
        //            new { EventName = "MINION_SUMMONED", ExpectedEventCount = 4 },
        //            new { EventName = "RECRUIT_CARD", ExpectedEventCount = 0 },
        //            new { EventName = "CARD_REVEALED", ExpectedEventCount = 0 },
        //            new { EventName = "HERO_POWER_CHANGED", ExpectedEventCount = 2 },
        //            new { EventName = "BATTLEGROUNDS_PLAYER_BOARD", ExpectedEventCount = 0 },
        //            new { EventName = "DISCARD_CARD", ExpectedEventCount = 0 },
        //            new { EventName = "DECKLIST_UPDATE", ExpectedEventCount = 0 },
        //        }},
        //        new { FileName = "steal_card", Events = new[]
        //        {
        //            new { EventName = "CARD_STOLEN", ExpectedEventCount = 2 },
        //            new { EventName = "MINION_SUMMONED", ExpectedEventCount = 3 },
        //            new { EventName = "RECRUIT_CARD", ExpectedEventCount = 0 },
        //            new { EventName = "CARD_REVEALED", ExpectedEventCount = 0 },
        //            new { EventName = "HERO_POWER_CHANGED", ExpectedEventCount = 2 },
        //            new { EventName = "BATTLEGROUNDS_PLAYER_BOARD", ExpectedEventCount = 0 },
        //            new { EventName = "DISCARD_CARD", ExpectedEventCount = 0 },
        //            new { EventName = "DECKLIST_UPDATE", ExpectedEventCount = 0 },
        //        }},
        //        new { FileName = "bulwark_of_death", Events = new[]
        //        {
        //            new { EventName = "SECRET_TRIGGERED", ExpectedEventCount = 2 },
        //            new { EventName = "DEATHRATTLE_TRIGGERED", ExpectedEventCount = 12 },
        //            new { EventName = "MINION_SUMMONED", ExpectedEventCount = 10 },
        //            new { EventName = "RECRUIT_CARD", ExpectedEventCount = 1 },
        //            new { EventName = "CARD_REVEALED", ExpectedEventCount = 0 },
        //            new { EventName = "HERO_POWER_CHANGED", ExpectedEventCount = 2 },
        //            new { EventName = "BATTLEGROUNDS_PLAYER_BOARD", ExpectedEventCount = 0 },
        //            new { EventName = "DISCARD_CARD", ExpectedEventCount = 0 },
        //            new { EventName = "DECKLIST_UPDATE", ExpectedEventCount = 0 },
        //        }},
        //        // Just check that no error is thrown
        //        new { FileName = "toki_hero_power", Events = new[]
        //        {
        //            new { EventName = "NEW_GAME", ExpectedEventCount = 1 },
        //            new { EventName = "MINION_SUMMONED", ExpectedEventCount = 3 },
        //            new { EventName = "RECRUIT_CARD", ExpectedEventCount = 0 },
        //            new { EventName = "CARD_REVEALED", ExpectedEventCount = 0 },
        //            new { EventName = "HERO_POWER_CHANGED", ExpectedEventCount = 6 },
        //            new { EventName = "BATTLEGROUNDS_PLAYER_BOARD", ExpectedEventCount = 0 },
        //            new { EventName = "DISCARD_CARD", ExpectedEventCount = 0 },
        //            new { EventName = "DECKLIST_UPDATE", ExpectedEventCount = 2 }, // This is incorrect, but toki's hero power messes up a lot of stuff
        //        }},
        //        new { FileName = "local_player_leaderboard", Events = new[]
        //        {
        //            new { EventName = "LOCAL_PLAYER_LEADERBOARD_PLACE_CHANGED", ExpectedEventCount = 7 },
        //            new { EventName = "MINION_SUMMONED", ExpectedEventCount = 216 },
        //            new { EventName = "RECRUIT_CARD", ExpectedEventCount = 0 },
        //            new { EventName = "CARD_REVEALED", ExpectedEventCount = 72 },
        //            new { EventName = "HERO_POWER_CHANGED", ExpectedEventCount = 12 },
        //            new { EventName = "BATTLEGROUNDS_PLAYER_BOARD", ExpectedEventCount = 35 },
        //            new { EventName = "DISCARD_CARD", ExpectedEventCount = 2 },
        //            new { EventName = "DECKLIST_UPDATE", ExpectedEventCount = 0 },
        //        }},
        //    };

        //    var errors = new List<string>();
        //    foreach (dynamic fileOutput in fileOutputs)
        //    {
        //        var testedFileName = fileOutput.FileName as string;
        //        var events = new Dictionary<string, int>();
        //        GameEventHandler.EventProvider = (GameEvent gameEvent) =>
        //        {
        //            var evtName = gameEvent.Type;
        //            var value = 0;
        //            if (events.ContainsKey(evtName))
        //            {
        //                value = events[evtName];
        //            }
        //            events[evtName] = value + 1;
        //        };
        //        List<string> logFile = TestDataReader.GetInputFile(testedFileName + ".txt");
        //        HearthstoneReplay replay = new ReplayParser().FromString(logFile);
        //        Thread.Sleep(500);

        //        foreach (dynamic evt in fileOutput.Events)
        //        {
        //            var expectedEventCount = (int)evt.ExpectedEventCount;
        //            var testedEventName = evt.EventName as string;
        //            var realEventCount = 0;
        //            if (events.ContainsKey(testedEventName))
        //            {
        //                realEventCount = events[testedEventName];
        //            }
        //            try
        //            {
        //                Assert.AreEqual(expectedEventCount, realEventCount, testedFileName + " / " + testedEventName);
        //            }
        //            catch (Exception e)
        //            {
        //                errors.Add(e.Message);
        //                Console.WriteLine("Error while processing " + testedFileName + " / " + testedEventName + " / " + e.Message);
        //            }
        //        }
        //    }

        //    foreach (var error in errors)
        //    {
        //        Console.Error.WriteLine(error);
        //    }

        //    Assert.AreEqual(0, errors.Count);
        //}

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

    // https://stackoverflow.com/questions/10169648/how-to-exclude-property-from-json-serialization
    //short helper class to ignore some properties from serialization
    public class IgnorePropertiesResolver : DefaultContractResolver
    {
        private readonly HashSet<string> ignoreProps;
        public IgnorePropertiesResolver(IEnumerable<string> propNamesToIgnore)
        {
            this.ignoreProps = new HashSet<string>(propNamesToIgnore);
        }

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            JsonProperty property = base.CreateProperty(member, memberSerialization);
            if (this.ignoreProps.Contains(property.PropertyName))
            {
                property.ShouldSerialize = _ => false;
            }
            return property;
        }
    }
}
