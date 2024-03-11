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
using Action = HearthstoneReplays.Parser.ReplayData.GameActions.Action;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using System.IO;

#endregion

namespace HearthstoneReplayTests
{
    [TestClass]
    public class ReplayDataTests
    {
        [TestMethod]
        public void Test()
        {
            //var serializerSettings = new JsonSerializerSettings()
            //{
            //    ContractResolver = new IgnorePropertiesResolver(new[] {
            //        "GameState",
            //        "ReplayXml",
            //        "LocalPlayer",
            //        "OpponentPlayer",
            //        "GameStateReport",
            //        "Game"
            //    })
            //};
            var serializerSettings = new JsonSerializerSettings()
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };
            GameEventHandler.EventProviderAll = (IList<GameEvent> gameEvents) =>
            {
                foreach (GameEvent gameEvent in gameEvents)
                {
                    dynamic Value = gameEvent.Value;
                    var shouldLog = true;
                    //var shouldLog = gameEvent.Type != "GAME_STATE_UPDATE" && gameEvent.Type != "GAME_END";
                    //var shouldLog = gameEvent.Type == "BATTLEGROUNDS_PLAYER_BOARD" || gameEvent.Type == "BATTLEGROUNDS_NEXT_OPPONENT";
                    if (shouldLog)
                    {
                        //var serialized = JsonConvert.SerializeObject(gameEvent);
                        var serialized = JsonConvert.SerializeObject(gameEvent, serializerSettings);
                        //File.AppendAllText(outputLogFile, serialized + ",\n");
                        Console.WriteLine(serialized + ",");
                    }
                }
            };
            GameEventHandler.EventProvider = (GameEvent gameEvent) =>
            {
                dynamic Value = gameEvent.Value;
                var shouldLog = true;
                //var shouldLog = gameEvent.Type != "GAME_STATE_UPDATE" && gameEvent.Type != "GAME_END";
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
                    //File.AppendAllText(outputLogFile, serialized + ",\n");
                    Console.WriteLine(serialized + ",");
                    //}
                }
            };
            List<string> logFile = TestDataReader.GetInputFile("bugs.txt");
            logFile.Insert(0, "START_CATCHING_UP");
            logFile.Add("END_CATCHING_UP");
            var parser = new ReplayParser();

            HearthstoneReplay replay = parser.FromString(logFile);
            Thread.Sleep(1000);
            string xml = new ReplayConverter().xmlFromReplay(replay);
            //Thread.Sleep(80000);
            //Console.Write(xml);
        }

        [TestMethod]
        public void LeakTest()
        {
            int numberOfLoops = 20;

            //GameEventHandler.EventProvider = (GameEvent gameEvent) =>
            //{
            //    dynamic Value = gameEvent.Value;
            //    var serialized = JsonConvert.SerializeObject(gameEvent);
            //    Console.WriteLine(serialized + ",");
            //};

            List<string> logFile = TestDataReader.GetInputFile("bugs.txt");
            logFile.Insert(0, "START_CATCHING_UP");
            logFile.Add("END_CATCHING_UP");
            var parser = new ReplayParser();

            for (var i = 0; i < numberOfLoops; i++)
            {
                HearthstoneReplay replay = parser.FromString(logFile);
                Thread.Sleep(3000);
                GC.Collect();
                Thread.Sleep(3000);
                string xml = new ReplayConverter().xmlFromReplay(replay);
                GC.Collect();
                Thread.Sleep(3000); // 135, 153, 158, 158, 165
            }

            Thread.Sleep(3000); //168
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
