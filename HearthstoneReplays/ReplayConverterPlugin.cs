﻿using System;
using System.Threading.Tasks;
using HearthstoneReplays.Parser;
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
				onGlobalEvent("No callback, returning", logs);
				return;
			}

			Task.Run(() => {
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
			GameEventHandler.EventProvider = onGameEvent;
			parser = new ReplayParser();
            parser.Init();
            callback?.Invoke(null);
        }

		public void realtimeLogProcessing(string[] logLines, Action<object> callback)
		{
			Task.Run(() => {
				try
				{
					Array.ForEach(logLines, logLine => parser.ReadLine(logLine));
					callback(null);
				}
				catch (Exception e)
				{
                    onGlobalEvent("Exception when parsing game " + e.GetBaseException() 
                                + " // " + logLines[logLines.Length - 1],                         
                        parser.State.FullLog + "/#/" + string.Join("\n", logLines));
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
	}
}
