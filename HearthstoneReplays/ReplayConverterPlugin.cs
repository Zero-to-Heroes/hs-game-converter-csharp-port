using System;
using System.Threading.Tasks;
using HearthstoneReplays.Parser;
using HearthstoneReplays.Events;
using Newtonsoft;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace HearthstoneReplays
{
    public class ReplayConverterPlugin
    {
        // a global event that triggers with two parameters:
        //
        // plugin.get().onGlobalEvent.addListener(function(first, second) {
        //  ...
        // });
        public event Action<object, object> onGlobalEvent;
        public event Action<string> onGameEvent;


        // plugin.get().convertLogsToXml(xmlLogs, function(result) {
        //   console.log(result);
        // });
        // 
        // notice how we will always call the callback on a new thread
        public void convertLogsToXml(string logs, Action<object> callback)
        {
            Logger.Log = onGlobalEvent;

            if (callback == null)
            {
                onGlobalEvent("No callback, returning", "");
                return;
            }

            Task.Run(() =>
            {
                try
                {
                    string replayXml = new ReplayConverter().xmlFromLogs(logs);
                    onGlobalEvent("Serialized", replayXml.Length);
                    callback(replayXml);
                }
                catch (Exception e)
                {
                    onGlobalEvent("Exception when parsing game " + e.GetBaseException(), logs);
                    callback(null);
                }
            });
        }

        private ReplayParser parser = new ReplayParser();

        public void initRealtimeLogConversion(Action<object> callback)
        {
            Logger.Log = onGlobalEvent;
            GameEventHandler.EventProvider = (GameEvent gameEvent) => onGameEvent(JsonConvert.SerializeObject(gameEvent, new JsonSerializerSettings()
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            }));
            // The goal here is to make sure the order is kept while in Dev mode, where events are very rapidly firing
            GameEventHandler.EventProviderAll = (IList<GameEvent> gameEvents) => onGameEvent(JsonConvert.SerializeObject(gameEvents));
            parser = new ReplayParser();
            parser.Init();
            callback?.Invoke(null);
        }

        public void realtimeLogProcessing(string[] logLines, Action<object> callback)
        {
            Task.Run(() =>
            {
                try
                {
                    var gameSeed = parser.ExtractGameSeed(logLines);
                    Array.ForEach(logLines, logLine => parser.ReadLine(logLine, gameSeed));
                    callback(null);
                }
                catch (Exception e)
                {
                    Logger.Log("Exception when parsing game " + e?.GetBaseException()
                                + " // " + logLines[logLines.Length - 1], "");
                    callback(null);
                }
            });
        }

        public void triggerGlobalEvent(string first, string second)
        {
            if (onGlobalEvent == null)
            {
                return;
            }

            Task.Run(() =>
            {
                onGlobalEvent(first, second);
            });
        }

        //      public void startDevMode()
        //      {
        //          NodeParser.DevMode = true;
        //	Logger.Log("Setting DevMode", NodeParser.DevMode);
        //}

        //      public void stopDevMode()
        //      {
        //          NodeParser.DevMode = false;
        //	Logger.Log("Setting DevMode", NodeParser.DevMode);
        //}
    }

}
