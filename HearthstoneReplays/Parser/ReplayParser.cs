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
using System.Xml.Linq;
using System.Diagnostics.Eventing;
using System.Globalization;
using System.Runtime.Remoting.Messaging;
using System.Runtime.CompilerServices;

#endregion

namespace HearthstoneReplays.Parser

{
    public class ReplayParser
    {
        public static DateTime start; // The log is not aware of absolute time, time zones, etc. So we just represent it based on the user's computer

        public CombinedState State;
        //private ParserState State;
        private DataHandler dataHandler;
        private PowerDataHandler powerDataHandler;
        private ChoicesHandler choicesHandler;
        private SendChoicesHandler sendChoicesHandler;
        private EntityChosenHandler entityChosenHandler;
        private OptionsHandler optionsHandler;
        private PowerProcessorHandler powerProcessorHandler;

        private Helper helper;

        private DateTime previousTimestamp;

        private List<string> processedLines = new List<string>();
        private long CurrentGameSeed;

        public ReplayParser()
        {
            State = new CombinedState();
            this.helper = new Helper(State);
            dataHandler = new DataHandler(helper);
            powerDataHandler = new PowerDataHandler(helper);
            choicesHandler = new ChoicesHandler(helper);
            sendChoicesHandler = new SendChoicesHandler(helper);
            entityChosenHandler = new EntityChosenHandler(helper);
            optionsHandler = new OptionsHandler(helper);
            powerProcessorHandler = new PowerProcessorHandler(helper);
            previousTimestamp = default;
            start = DateTime.Now; // Don't use UTC, otherwise it won't match with the log info
            Logger.Log("ReplayParser constructor over", State.GSState == null);
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
            //int chunkSize = 500;
            //int totalLines = lines.Length;

            long gameSeed = ExtractGameSeed(lines);
            Logger.Log($"Extracted game seed = {gameSeed}", "");
            if (gameSeed > 0)
            {
                this.CurrentGameSeed = gameSeed;
            }

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                //var debug = line.Contains("D 12:50:21.9664526 PowerTaskList.DebugPrintPower() -     TAG_CHANGE Entity=[entityName=Molten Rock id=524 zone=PLAY zonePos=1 cardId=BGS_127 player=12] tag=ZONE value=GRAVEYARD");
                var debug = i == 3458;
                ReadLine(line, this.CurrentGameSeed, i);
            }
        }

        public void Init()
        {
            Logger.Log("Calling reset from ReplayParser.init()", "");
            previousTimestamp = default;
            //State.Reset();
        }

        private bool resettingGame;
        private int currentResetBlockIndex;
        private List<dynamic> resettingGames = new List<dynamic>();
        private bool ignoringAlternateTimeline;
        private bool inResetBlock;

        public void ReadLine(string line, long gameSeed, int lineIndex)
        {
            if (gameSeed != 0)
            {
                this.CurrentGameSeed = gameSeed;
            }

            if (!this.resettingGame && line.Contains("GameState") && line.Contains("CREATE_GAME"))
            {
                Logger.Log($"Clearing {this.processedLines.Count} processed lines", line);
                this.processedLines.Clear();
            }

            var debug = this.resettingGame && line.Contains("RESET");
            debug = line.Contains("GDB_145");
            if (debug)
            {
                var x = 0;
            }
            if (!this.resettingGame)
            {
                Match resetStartMatch = Regexes.ResetStartMatchRegex.Match(line);
                if (resetStartMatch.Success)
                {
                    this.resettingGame = true;
                    this.currentResetBlockIndex = 0;
                    // TODO: Enqueue reset game event
                    var rawEntity = resetStartMatch.Groups[1].Value;
                    var entityId = helper.ParseEntity(rawEntity);
                    // Only the latest reset appears here, as the previous ones have been removed from the alternate timeline
                    // So we only need to keep the latest "reset" info
                    this.resettingGames.Clear();
                    this.resettingGames.Add(new { originEntity = entityId, index = lineIndex });
                    var linesCopy = this.processedLines.ToArray();
                    this.processedLines.Clear();
                    Read(linesCopy);
                    return;
                }
            }

            if (this.resettingGame)
            {
                var currentEntityIdBlockToIgnore = this.resettingGames[this.currentResetBlockIndex];
                var debug2 = line.Contains("BLOCK_START BlockType=GAME_RESET Entity=[entityName=Portal Vanguard id=5 zone=PLAY");
                if (debug2)
                {
                    var y = 0;
                }

                // The whitespace is important, otherwise an entity=5 can be triggered by id=54
                if (line.Contains("BLOCK_START BlockType=PLAY") && line.Contains($"id={currentEntityIdBlockToIgnore.originEntity} "))
                {
                    this.ignoringAlternateTimeline = true;
                }


                Match resetStartMatch = Regexes.ResetStartMatchRegex.Match(line);
                if (resetStartMatch.Success)
                {
                    this.inResetBlock = true;
                }

                if (this.inResetBlock && line.Contains("BLOCK_END"))
                {
                    this.inResetBlock = false;
                    this.ignoringAlternateTimeline = false;
                    this.currentResetBlockIndex++;
                    if (this.currentResetBlockIndex == this.resettingGames.Count)
                    {
                        this.resettingGame = false;
                    }
                }
            }

            if (this.ignoringAlternateTimeline)
            {
                return;
            }

            this.processedLines.Add(line);
            Match match = Regexes.PowerlogLineRegex.Match(line);
            if (!match.Success)
            {
                if (line.Contains("End Spectator Mode") || (line.Contains("Begin Spectating") && !line.Contains("2nd")))
                {
                    AddData(null, "Spectator", line, gameSeed);
                }
                else
                {
                    Logger.Log("No match", line);
                }
                return;
            }

            //if (!this.resettingGame)
            //{
            //}
            AddData(match.Groups[1].Value, match.Groups[2].Value, match.Groups[3].Value, gameSeed);

        }

        public void AskForGameStateUpdate()
        {
            //Logger.Log("askForGameStateUpdate", "Parser");
            GameStateShort gameState = null;
            try
            {
                gameState = GameEvent.BuildGameState(State.PTLState, State.StateFacade, State.PTLState.GameState);
            }
            catch (Exception ex)
            {
                Logger.Log("askForGameStateUpdate", $"Could not create game state: {ex.ToString()}");
            }
            //Logger.Log("askForGameStateUpdate", "Built game state");
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
                    dataHandler.Handle(normalizedTimestamp, data, State.GSState, StateType.GameState, previousTimestamp, State.StateFacade, gameSeed, this.resettingGame);
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
                    dataHandler.Handle(normalizedTimestamp, data, State.PTLState, StateType.PowerTaskList, previousTimestamp, State.StateFacade, gameSeed, this.resettingGame);
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

            try
            {
                // Use DateTime.ParseExact for faster parsing with a known format
                var logDateTime = DateTime.ParseExact(timestamp, "HH:mm:ss.fffffff", null);
                // Avoid unnecessary comparison if the timestamp is already valid
                return logDateTime < start ? logDateTime.AddDays(1) : logDateTime;
            }
            // Sometimes the logs contain some poorly-formatted timestamps (saw that once)
            catch (Exception e)
            {
                var logDateTime = DateTime.Parse(timestamp);
                // Avoid unnecessary comparison if the timestamp is already valid
                return logDateTime < start ? logDateTime.AddDays(1) : logDateTime;
            }

        }

        public long ExtractGameSeed(string[] lines)
        {
            string pattern = @"tag=GAME_SEED value=(\d+)";
            bool isGameCreation = false;
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
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
                Logger.Log($"CREATE_GAME without seed", lines[lines.Length - 1]);
            }
            return isGameCreation ? -1 : 0;
        }
    }
}