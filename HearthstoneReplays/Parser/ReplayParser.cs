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
using HearthstoneReplays.Parser;
using HearthstoneReplays.Events;

#endregion

namespace HearthstoneReplays.Parser

{
    public class ReplayParser
    {
        public const string Version = "1.0";
        public const int HearthstoneBuild = 43246;

        public static DateTime start; // The log is not aware of absolute time, time zones, etc. So we just represent it based on the user's computer

        public ParserState State;
        private DataHandler dataHandler;
        private PowerDataHandler powerDataHandler;
        private ChoicesHandler choicesHandler;
        private EntityChosenHandler entityChosenHandler;
        private OptionsHandler optionsHandler;

        private DateTime previousTimestamp;

        private bool powerTaskStarted;
        private bool firstPowerTaskOver;
        private bool forceMatchOver;

        public ReplayParser()
        {
            State = new ParserState();
            dataHandler = new DataHandler();
            powerDataHandler = new PowerDataHandler();
            choicesHandler = new ChoicesHandler();
            entityChosenHandler = new EntityChosenHandler();
            optionsHandler = new OptionsHandler();
            previousTimestamp = default;
            powerTaskStarted = false;
            firstPowerTaskOver = false;
            start = DateTime.Now; // Don't use UTC, otherwise it won't match with the log info
        }

        public HearthstoneReplay FromString(IEnumerable<string> lines, params GameType[] gameTypes)
        {
            Read(lines.ToArray());
            State.Replay.Version = Version;
            State.Replay.Build = HearthstoneBuild.ToString();
            for (var i = 0; i < State.Replay.Games.Count; i++)
            {
                if (gameTypes == null || gameTypes.Length == 1)
                    State.Replay.Games[i].Type = (int)gameTypes[0];
                else
                    State.Replay.Games[i].Type = gameTypes.Length > i ? (int)gameTypes[i] : 0;
            }
            return State.Replay;
        }

        public void Read(string[] lines)
        {
            Init();
            foreach (var line in lines)
            {
                ReadLine(line);
            }
        }

        public void Init()
        {
            Logger.Log("Calling reset from ReplayParser.init()", "");
            previousTimestamp = default;
            //State.Reset();
        }

        public void ReadLine(string line)
        {
            // Ignore timestamps when catching up with past events
            if (line == "START_CATCHING_UP")
            {
                State.NodeParser.StartDevMode();
                return;
                //NodeParser.DevMode = true;
                //Logger.Log("Setting Start DevMode", NodeParser.DevMode);
            }
            if (line == "END_CATCHING_UP")
            {
                State.NodeParser.StopDevMode();
                return;
                //NodeParser.DevMode = false;
                //Logger.Log("Setting Stop DevMode", NodeParser.DevMode);

            }
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
            var normalizedTimestamp = NormalizeTimestamp(timestamp);
            if (method == "GameState.DebugPrintPower" && data == "CREATE_GAME")
            {
                powerTaskStarted = false;
                firstPowerTaskOver = false;
            }
            if (method == "GameState.DebugPrintPower" && data.Contains("TAG_CHANGE Entity=GameEntity tag=STATE value=COMPLETE"))
            {
                forceMatchOver = true;
                Logger.Log("force match over", forceMatchOver);
            }
            if (method == "PowerTaskList.DebugPrintPower" && data.Contains("TAG_CHANGE Entity=GameEntity tag=STATE value=COMPLETE"))
            {
                forceMatchOver = false;
                Logger.Log("force match over", forceMatchOver);
            }
            switch (method)
            {
                case "GameState.DebugPrintPower":
                case "GameState.DebugPrintGame":
                    // For the game state init, we rely on the GameState log (mostly because otherwise the player
                    // name assignment and the options won't have any state to rely on)
                    // After that, we only use the powertasklist to avoid having to handle the delay between
                    // state update and animation processing
                    if (powerTaskStarted)
                    {
                        firstPowerTaskOver = true;
                    }
                    if (!firstPowerTaskOver)
                    {
                        // When doing a restart against the AI, the STATE=COMPLETE tagchange is only present in the GameState
                        // and not in the PowerTaskList
                        if (forceMatchOver && data == "CREATE_GAME")
                        {
                            dataHandler.Handle(
                                normalizedTimestamp,
                                "TAG_CHANGE Entity=GameEntity tag=STATE value=COMPLETE ", 
                                State, 
                                previousTimestamp);
                            forceMatchOver = false;
                        }
                        dataHandler.Handle(normalizedTimestamp, data, State, previousTimestamp);
                        previousTimestamp = normalizedTimestamp;
                    }
                    break;
                //case "GameState.SendChoices":
                //	SendChoicesHandler.Handle(timestamp, data, State);
                //	break;
                //case "GameState.DebugPrintChoices":
                case "GameState.DebugPrintEntityChoices":
                    choicesHandler.Handle(normalizedTimestamp, data, State);
                    previousTimestamp = normalizedTimestamp;
                    break;
                case "GameState.DebugPrintEntitiesChosen":
                    entityChosenHandler.Handle(normalizedTimestamp, data, State);
                    previousTimestamp = normalizedTimestamp;
                    break;
                case "GameState.DebugPrintOptions":
                    optionsHandler.Handle(normalizedTimestamp, data, State);
                    previousTimestamp = normalizedTimestamp;
                    break;
                case "PowerTaskList.DebugPrintPower":
                    powerTaskStarted = true;
                    // Use the powertasklist only once the initial state has been computed
                    if (firstPowerTaskOver)
                    {
                        dataHandler.Handle(normalizedTimestamp, data, State, previousTimestamp);
                        previousTimestamp = normalizedTimestamp;
                    }
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

        private DateTime NormalizeTimestamp(string timestamp)
        {
            if (string.IsNullOrEmpty(timestamp))
            {
                return default;
            }

            var logDateTime = DateTime.Parse(timestamp);
            // This means we got back in time, which is not possible, so it means we have gone to the next day
            // This won't work if we have sessions that span more than one day, but I think it's ok
            if (logDateTime < start)
            {
                logDateTime = logDateTime.AddDays(1);
                //Logger.Log("Adding a day to timestamp ", logDateTime + " // " + start + " // " + timestamp + " // " + logDateTime);
            }
            return logDateTime;
        }
    }
}