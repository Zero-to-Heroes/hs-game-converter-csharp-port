﻿#region

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
using System.Xml.Linq;
using System.Diagnostics.Eventing;

#endregion

namespace HearthstoneReplays.Parser

{
    public class ReplayParser
    {
        public static DateTime start; // The log is not aware of absolute time, time zones, etc. So we just represent it based on the user's computer

        private CombinedState State;
        //private ParserState State;
        private DataHandler dataHandler;
        private PowerDataHandler powerDataHandler;
        private ChoicesHandler choicesHandler;
        private SendChoicesHandler sendChoicesHandler;
        private EntityChosenHandler entityChosenHandler;
        private OptionsHandler optionsHandler;
        private PowerProcessorHandler powerProcessorHandler;

        private DateTime previousTimestamp;

        private List<string> processedLines = new List<string>();
        private long CurrentGameSeed;

        public ReplayParser()
        {
            State = new CombinedState();
            Helper helper = new Helper(State);
            dataHandler = new DataHandler(helper);
            powerDataHandler = new PowerDataHandler(helper);
            choicesHandler = new ChoicesHandler(helper);
            sendChoicesHandler = new SendChoicesHandler(helper);
            entityChosenHandler = new EntityChosenHandler(helper);
            optionsHandler = new OptionsHandler(helper);
            powerProcessorHandler = new PowerProcessorHandler(helper);
            previousTimestamp = default;
            start = DateTime.Now; // Don't use UTC, otherwise it won't match with the log info
        }

        public HearthstoneReplay FromString(IEnumerable<string> lines, params GameType[] gameTypes)
        {
            Read(lines.ToArray());
            var finalState = State.GSState;
            for (var i = 0; i < finalState.Replay.Games.Count; i++)
            {
                if (gameTypes == null || gameTypes.Length == 1)
                    finalState.Replay.Games[i].Type = (int)gameTypes[0];
                else
                    finalState.Replay.Games[i].Type = gameTypes.Length > i ? (int)gameTypes[i] : 0;
            }
            return finalState.Replay;
        }

        public void Read(string[] lines)
        {
            Init();
            // Use chunks to recompute the game seed when parsing multiple games at the same time
            int chunkSize = 500;
            for (var i = 0; i < lines.Length; i += chunkSize)
            {
                string[] processedLines = lines.ToList().GetRange(i, Math.Min(chunkSize, lines.Length - i)).ToArray();
                var gameSeed = ExtractGameSeed(processedLines);
                Logger.Log($"Extracted game seed = {gameSeed}", "");
                if (gameSeed > 0)
                {
                    this.CurrentGameSeed = gameSeed;
                }
                foreach (var line in processedLines)
                {
                    ReadLine(line, this.CurrentGameSeed);
                }
            }
        }

        public void Init()
        {
            Logger.Log("Calling reset from ReplayParser.init()", "");
            previousTimestamp = default;
            //State.Reset();
        }

        public void ReadLine(string line, long gameSeed)
        {
            if (gameSeed != 0)
            {
                this.CurrentGameSeed = gameSeed;
            }
            //processedLines.Add(line);
            // Ignore timestamps when catching up with past events
            //if (line == "START_CATCHING_UP")
            //{
            //    State.StartDevMode();
            //    return;
            //}
            //if (line == "END_CATCHING_UP")
            //{
            //    State.NodeParser.StopDevMode();
            //    return;
            //    //NodeParser.DevMode = false;
            //    //Logger.Log("Setting Stop DevMode", NodeParser.DevMode);

            //}
            Match match;
            Regex logTypeRegex = null;
            if (logTypeRegex == null)
            {
                match = Regexes.PowerlogLineRegex.Match(line);
                if (match.Success)
                {
                    logTypeRegex = Regexes.PowerlogLineRegex;
                }
                else
                {
                    match = Regexes.OutputlogLineRegex.Match(line);
                    if (match.Success)
                    {
                        logTypeRegex = Regexes.OutputlogLineRegex;
                    }
                }
            }
            else
                match = logTypeRegex.Match(line);

            if (!match.Success)
            {
                if (line.Contains("End Spectator Mode") || (line.Contains("Begin Spectating") && !line.Contains("2nd")))
                {
                    AddData(null, "Spectator", line, gameSeed);
                }
                return;
            }

            //State.FullLog += line + "\n";
            //Logger.Log("Processing new line", line);
            AddData(match.Groups[1].Value, match.Groups[2].Value, match.Groups[3].Value, gameSeed);
            // New game
            //if (State.FullLog.Length == 0)
            //{
            //    State.FullLog += line + "\n";
            //}

        }

        public void AskForGameStateUpdate()
        {
            //Logger.Log("askForGameStateUpdate", "Parser");
            var gameState = GameEvent.BuildGameState(State.PTLState, State.StateFacade, State.PTLState.GameState);
            Func<GameEvent> eventSupplier = () =>
            {
                //Logger.Log("Returning new event", "GAME_STATE_UPDATE");
                return new GameEvent
                {
                    Type = "GAME_STATE_UPDATE",
                    Value = new { LocalPlayer = State.StateFacade.LocalPlayer, OpponentPlayer = State.StateFacade.OpponentPlayer, GameState = gameState, }
                };
            };
            var provider = GameEventProvider.Create(
                DateTime.Now,
                "GAME_STATE_UPDATE",
                eventSupplier,
                true,
                null
            );
            //Logger.Log("askForGameStateUpdate", "built provider");
            State.PTLState.NodeParser.EnqueueGameEvent(new List<GameEventProvider> { provider });
        }

        private void AddData(string timestamp, string method, string data, long gameSeed)
        {
            var normalizedTimestamp = NormalizeTimestamp(timestamp);
            switch (method)
            {
                case "GameState.DebugPrintPower":
                case "GameState.DebugPrintGame":
                case "Spectator":
                    dataHandler.Handle(normalizedTimestamp, data, State.GSState, StateType.GameState, previousTimestamp, State.StateFacade, gameSeed);
                    previousTimestamp = normalizedTimestamp;
                    State.StateFacade.LastProcessedGSLine = data;
                    break;
                //case "GameState.SendChoices":
                //    sendChoicesHandler.Handle(normalizedTimestamp, data, State.GSState);
                //    break;
                //case "GameState.DebugPrintChoices":
                case "GameState.DebugPrintEntityChoices":
                    choicesHandler.Handle(normalizedTimestamp, data, State.GSState);
                    // Assumption here is that the choices are highlighted once the PTL has caught up
                    // Update: that doesn't seem to be the case. Some choices appear after the GS has completed, 
                    // but the PTL FullEntity blocks have not appeared yet
                    // So for now keep the choices purely on the GS side - hoping the timings will be good enough
                    //choicesHandler.Handle(normalizedTimestamp, data, State.PTLState);
                    previousTimestamp = normalizedTimestamp;
                    break;
                case "GameState.DebugPrintEntitiesChosen":
                    entityChosenHandler.Handle(normalizedTimestamp, data, State.GSState);
                    //entityChosenHandler.Handle(normalizedTimestamp, data, State.PTLState);
                    previousTimestamp = normalizedTimestamp;
                    break;
                case "GameState.DebugPrintOptions":
                    optionsHandler.Handle(normalizedTimestamp, data, State.GSState, StateType.GameState, State.StateFacade);
                    optionsHandler.Handle(normalizedTimestamp, data, State.PTLState, StateType.PowerTaskList, State.StateFacade);
                    previousTimestamp = normalizedTimestamp;
                    break;
                case "PowerTaskList.DebugPrintPower":
                    // Process the actual stuff
                    dataHandler.Handle(normalizedTimestamp, data, State.PTLState, StateType.PowerTaskList, previousTimestamp, State.StateFacade, gameSeed);
                    // Update entity names
                    powerDataHandler.Handle(normalizedTimestamp, data, State.PTLState);
                    // See comment in OptionsHandler
                    if (State.StateFacade.ShouldUpdateToRoot(data))
                    {
                        Logger.Log("Update to root", data);
                        State.StateFacade.UpdatePTLToRoot();
                    }
                    previousTimestamp = normalizedTimestamp;
                    State.StateFacade.LastProcessedPTLLine = data;
                    break;
                //case "GameState.SendOption":
                //	SendOptionHandler.Handle(timestamp, data, State);
                //	break;
                //case "GameState.OnEntityChoices":
                //	// Spectator mode noise
                //	break;
                case "ChoiceCardMgr.WaitThenShowChoices":
                    choicesHandler.Handle(normalizedTimestamp, data, State.GSState);
                    previousTimestamp = normalizedTimestamp;
                    break;
                case "PowerProcessor.EndCurrentTaskList":
                    powerProcessorHandler.Handle(normalizedTimestamp, data, State.GSState, StateType.PowerTaskList, State.StateFacade);
                    previousTimestamp = normalizedTimestamp;
                    break;
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

        public long ExtractGameSeed(string[] lines)
        {
            string pattern = @"tag=GAME_SEED value=(\d+)";
            var firstLines = lines
                //.Take(40)
                .ToArray();
            var lastLine = firstLines.Last();
            bool isGameCreation = false;
            //Logger.Log($"Last line for seed extraction", lastLine);
            foreach (var line in firstLines)
            {
                if (line.Contains("CREATE_GAME"))
                {
                    isGameCreation = true;
                }
                if (!line.Contains("GAME_SEED"))
                {
                    continue;
                }

                Match match = Regex.Match(line, pattern);
                if (match.Success)
                {
                    string someValue = match.Groups[1].Value;
                    Logger.Log($"Extracted seed", someValue);
                    return long.Parse(someValue);
                }
            }
            // Special status if this includes a CREATE_GAME log but doesn't have the game seed, because
            // this will cause issues when trying to spot a reconnect
            if (isGameCreation)
            {
                Logger.Log($"CREATE_GAME without seed", lastLine);
            }
            return isGameCreation ? -1 : 0;
        }
    }
}