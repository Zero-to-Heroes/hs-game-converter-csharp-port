#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.Handlers;
using HearthstoneReplays.Parser.ReplayData;
using HearthstoneReplays.Parser.ReplayData.Entities;

#endregion

namespace HearthstoneReplays.Parser

{
	public class ReplayParser
	{
		public const string Version = "1.0";
		public const int HearthstoneBuild = 15590;
		
		public ParserState State;
		private DataHandler dataHandler;
        private PowerDataHandler powerDataHandler;
		private ChoicesHandler choicesHandler;
		private EntityChosenHandler entityChosenHandler;
		private OptionsHandler optionsHandler;

        private int previousTimestampTotalSeconds;

        public ReplayParser()
		{
			State = new ParserState();
			dataHandler = new DataHandler();
            powerDataHandler = new PowerDataHandler();
            choicesHandler = new ChoicesHandler();
			entityChosenHandler = new EntityChosenHandler();
			optionsHandler = new OptionsHandler();
            previousTimestampTotalSeconds = 0;

        }

		public HearthstoneReplay FromString(IEnumerable<string> lines, params GameType[] gameTypes)
		{
			Read(lines.ToArray());
			State.Replay.Version = Version;
			State.Replay.Build = HearthstoneBuild.ToString();
			for(var i = 0; i < State.Replay.Games.Count; i++)
			{
				if(gameTypes == null || gameTypes.Length == 1)
					State.Replay.Games[i].Type = (int)gameTypes[0];
				else
					State.Replay.Games[i].Type = gameTypes.Length > i ? (int)gameTypes[i] : 0;
			}
			return State.Replay;
		}

		public void Read(string[] lines)
		{
			Init();
			foreach(var line in lines)
			{
				ReadLine(line);
			}
		}

		public void Init()
        {
            Logger.Log("Calling reset from ReplayParser.init()", "");
            previousTimestampTotalSeconds = 0;
            //State.Reset();
        }

		public void ReadLine(string line)
		{
			Match match;
			Regex logTypeRegex = null;
			if (logTypeRegex == null)
			{
				match = Regexes.PowerlogLineRegex.Match(line);
				if (match.Success)
					logTypeRegex = Regexes.PowerlogLineRegex;
				else
				{
					match = Regexes.OutputlogLineRegex.Match(line);
					if (match.Success)
						logTypeRegex = Regexes.OutputlogLineRegex;
				}
			}
			else
				match = logTypeRegex.Match(line);

			if (!match.Success)
				return;

            //State.FullLog += line + "\n";
            //Logger.Log("Processing new line", line);
            AddData(match.Groups[1].Value, match.Groups[2].Value, match.Groups[3].Value);
            // New game
            //if (State.FullLog.Length == 0)
            //{
            //    State.FullLog += line + "\n";
            //}

        }

        private void AddData(string timestamp, string method, string data)
        {
            switch (method)
			{
				case "GameState.DebugPrintPower":
				case "GameState.DebugPrintGame":
					dataHandler.Handle(timestamp, data, State, previousTimestampTotalSeconds);
                    previousTimestampTotalSeconds = BuildTotalSeconds(timestamp);
                    break;
				//case "GameState.SendChoices":
				//	SendChoicesHandler.Handle(timestamp, data, State);
				//	break;
				//case "GameState.DebugPrintChoices":
				case "GameState.DebugPrintEntityChoices":
                    choicesHandler.Handle(timestamp, data, State);
                    previousTimestampTotalSeconds = BuildTotalSeconds(timestamp);
                    break;
				case "GameState.DebugPrintEntitiesChosen":
					entityChosenHandler.Handle(timestamp, data, State);
                    previousTimestampTotalSeconds = BuildTotalSeconds(timestamp);
                    break;
				case "GameState.DebugPrintOptions":
					optionsHandler.Handle(timestamp, data, State);
                    previousTimestampTotalSeconds = BuildTotalSeconds(timestamp);
                    break;
                case "PowerTaskList.DebugPrintPower":
                    powerDataHandler.Handle(timestamp, data, State);
                    previousTimestampTotalSeconds = BuildTotalSeconds(timestamp);
                    break;
                //case "GameState.SendOption":
                //	SendOptionHandler.Handle(timestamp, data, State);
                //	break;
                //case "GameState.OnEntityChoices":
                //	// Spectator mode noise
                //	break;
                //case "ChoiceCardMgr.WaitThenShowChoices":
                //	// Not needed for replays
                //	break;
                //case "GameState.DebugPrintChoice":
                //	Console.WriteLine("Warning: DebugPrintChoice was removed in 10357. Ignoring.");
                //                break;
                default:
					//if(!method.StartsWith("PowerTaskList.") && !method.StartsWith("PowerProcessor.") && !method.StartsWith("PowerSpellController"))
					//	Console.WriteLine("Warning: Unhandled method: " + method);
					break;
			}
        }

        private int BuildTotalSeconds(String timestamp)
        {
            if (!string.IsNullOrEmpty(timestamp))
            {
                String[] split = timestamp.Split(':');
                int hours = int.Parse(split[0]);
                int minutes = int.Parse(split[1]);
                int seconds = int.Parse(split[2].Split('.')[0]);
                return seconds + 60 * minutes + 3600 * hours;
            }
            return 0;
        }
    }
}