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

            // Manual parsing for PowerlogLineRegex - much faster than regex for the common case
            // Format: "D [timestamp] [method]() - [content]"
            string timestamp = null;
            string method = null;
            string content = null;
            bool matchSuccess = false;

            //var debugLine = lineIndex == 9624 || lineIndex == 9604;

            if (line.Length >= 3 && line[0] == 'D' && line[1] == ' ')
            {
                // Find the first space after "D " (start of timestamp)
                int timestampStart = 2;
                int timestampEnd = line.IndexOf(' ', timestampStart);
                if (timestampEnd > timestampStart)
                {
                    timestamp = line.Substring(timestampStart, timestampEnd - timestampStart);

                    // Find "() - " pattern to split method and content
                    int methodEnd = line.IndexOf("() - ", timestampEnd + 1);
                    if (methodEnd > timestampEnd)
                    {
                        method = line.Substring(timestampEnd + 1, methodEnd - timestampEnd - 1);
                        content = line.Substring(methodEnd + 5); // Skip "() - "
                        matchSuccess = true;
                    }
                }
            }

            if (!this.resettingGame && line.Contains("GameState") && line.Contains("CREATE_GAME"))
            {
                Logger.Log($"Clearing {this.processedLines.Count} processed lines", line);
                this.processedLines.Clear();
            }

            // Only check ResetStartMatchRegex if line contains "BLOCK_START"
            Match resetStartMatch = null;
            if (line.Contains("BLOCK_START"))
            {
                resetStartMatch = Regexes.ResetStartMatchRegex.Match(line);
            }
            if (!this.resettingGame)
            {
                if (resetStartMatch != null && resetStartMatch.Success && line.Contains("GameState.DebugPrintPower()"))
                {
                    //Logger.Log("askForGameStateUpdate", "built provider");
                    var normalizedTimestamp = matchSuccess ? NormalizeTimestamp(timestamp) : DateTime.Now;
                    State.PTLState.NodeParser.EnqueueGameEvent(new List<GameEventProvider> {
                        GameEventProvider.Create(normalizedTimestamp, "REWIND_STARTED", () => new GameEvent { Type = "REWIND_STARTED" }, true, null)
                    });
                    this.resettingGame = true;
                    this.currentResetBlockIndex = 0;
                    // TODO: Enqueue reset game event
                    var rawEntity = resetStartMatch.Groups[1].Value;
                    var entityId = helper.ParseEntity(rawEntity);
                    // Only the latest reset appears here, as the previous ones have been removed from the alternate timeline
                    // So we only need to keep the latest "reset" info
                    this.resettingGames.Clear();
                    this.resettingGames.Add(new { originEntity = entityId, index = lineIndex });
                    // We keep the "RESET_GAME" line so that we know when we need to start ignoring the "recreate game" effect
                    // and when it ends
                    this.processedLines.Add(line);
                    var linesCopy = this.processedLines.ToArray();
                    this.processedLines.Clear();
                    Read(linesCopy);
                    return;
                }
            }

            if (resetStartMatch != null && resetStartMatch.Success)
            {
                this.inResetBlock = true;
            }

            if (this.resettingGame)
            {
                var currentEntityIdBlockToIgnore = this.resettingGames[this.currentResetBlockIndex];

                // The whitespace is important, otherwise an entity=5 can be triggered by id=54
                if (line.Contains("BLOCK_START BlockType=PLAY") && line.Contains($"id={currentEntityIdBlockToIgnore.originEntity} "))
                {
                    this.ignoringAlternateTimeline = true;
                }

                // BLOCK_END is not enough - if the reset triggers a block with a choice, it can end in GameState without a BLOCK_END
                /*
                    * D 10:04:16.6645154 GameState.DebugPrintPower() -             tag=CONTROLLER value=1
                D 10:04:16.6645154 GameState.DebugPrintPower() -             tag=ENTITY_ID value=142
                D 10:04:16.7396242 GameState.DebugPrintEntityChoices() - id=6 Player=Naith#21657 TaskList=360 ChoiceType=GENERAL CountMin=1 CountMax=1
                D 10:04:16.7396242 GameState.DebugPrintEntityChoices() -   Source=[entityName=UNKNOWN ENTITY [cardType=INVALID] id=13 zone=HAND zonePos=4 cardId= player=1]
                D 10:04:16.7396242 GameState.DebugPrintEntityChoices() -   Entities[0]=[entityName=UNKNOWN ENTITY [cardType=INVALID] id=141 zone=SETASIDE zonePos=0 cardId= player=1]
                D 10:04:16.7396242 GameState.DebugPrintEntityChoices() -   Entities[1]=[entityName=UNKNOWN ENTITY [cardType=INVALID] id=142 zone=SETASIDE zonePos=0 cardId= player=1]
                D 10:04:16.7396242 ChoiceCardMgr.WaitThenShowChoices() - id=6 WAIT for taskList 360
                D 10:04:16.7460948 PowerProcessor.EndCurrentTaskList() - m_currentTaskList=348
                D 10:04:16.7522232 PowerTaskList.DebugDump() - ID=349 ParentID=0 PreviousID=345 TaskCount=3
                D 10:04:16.7522232 PowerTaskList.DebugDump() - Block Start=(null)
                D 10:04:16.7522232 PowerTaskList.DebugPrintPower() -     TAG_CHANGE Entity=[entityName=Precursory Strike id=24 zone=PLAY zonePos=0 cardId=TIME_750 player=1] tag=1068 value=4 
                D 10:04:16.7522232 PowerTaskList.DebugPrintPower() -     TAG_CHANGE Entity=[entityName=Precursory Strike id=24 zone=PLAY zonePos=0 cardId=TIME_750 player=1] tag=1068 value=0 
                D 10:04:16.7522232 PowerTaskList.DebugPrintPower() -     TAG_CHANGE Entity=[entityName=Precursory Strike id=24 zone=PLAY zonePos=0 cardId=TIME_750 player=1] tag=ZONE value=GRAVEYARD 
                D 10:04:16.7522232 PowerTaskList.DebugDump() - Block End=(null)
                D 10:04:16.7522232 PowerProcessor.PrepareHistoryForCurrentTaskList() - m_currentTaskList=349
                D 10:04:16.7522232 PowerProcessor.DoTaskListForCard() - unhandled BlockType PLAY for sourceEntity [entityName=Precursory Strike id=24 zone=PLAY zonePos=0 cardId=TIME_750 player=1]
                D 10:04:16.7646769 PowerProcessor.EndCurrentTaskList() - m_currentTaskList=349
                D 10:04:16.7710789 PowerTaskList.DebugDump() - ID=350 ParentID=345 PreviousID=0 TaskCount=15
                */
                // Cut short, usually the GameState is interrupted, something happens on PTL, and the alternative timeline choice starts again on PTL
                // This doesn't work, as you could have some leftover PTL lines after this, like a DEATHS block
                // Maybe logs should be parsed separately for GameState and PTL in case of reset, but the logs parser is not constructed to work like 
                // that at the moment
                // I would need the split the parsers into one parser for GameState (server-side state), and one parser for the rest
                // However the resets should be kept in sync, so that the code that relies on GameState is still accurate
                //if (this.ignoringAlternateTimeline && line.Contains("ChoiceCardMgr.WaitThenShowChoices()"))
                //{
                //    this.ignoringAlternateTimeline = false;
                //}

                // Restricting to PTL might mean we miss some info from GameState though...
                // But otherwise we can have the end of the GameState reset block, then info about the Rewind Timeline being showing in PTL
                // then the PTL reset block. This causes the Rewind Timeline to be part of the parsed logs
                if (this.inResetBlock && line.Contains("BLOCK_END") && line.Contains("PowerTaskList"))
                {
                    //this.inResetBlock = false;
                    this.ignoringAlternateTimeline = false;
                    this.currentResetBlockIndex++;
                    if (this.currentResetBlockIndex == this.resettingGames.Count)
                    {
                        this.resettingGame = false;
                        var normalizedTimestamp = matchSuccess ? NormalizeTimestamp(timestamp) : DateTime.Now;
                        State.PTLState.NodeParser.EnqueueGameEvent(new List<GameEventProvider> {
                            GameEventProvider.Create(normalizedTimestamp, "REWIND_OVER", () => new GameEvent { Type = "REWIND_OVER" }, true, null)
                        });
                    }
                }
            }

            // We need to also handle the case where the "RESET_GAME" in PTL appears *after* the PTL discovery block
            // This probably shouldn't happen in the first case, but we see this behavior if the user picks the
            // "rewind timeline" option quickly
            if (this.inResetBlock && line.Contains("BLOCK_END"))
            {
                this.inResetBlock = false;
                return;
            }

            if (this.ignoringAlternateTimeline || this.inResetBlock)
            {
                return;
            }

            this.processedLines.Add(line);
            if (!matchSuccess)
            {
                if (line.Contains("End Spectator Mode") || (line.Contains("Begin Spectating") && !line.Contains("2nd")))
                {
                    AddData(null, "Spectator", line, gameSeed);
                }
                else if (line != null && line.Trim().Length > 0)
                {
                    Logger.Log("No match", line);
                }
                return;
            }

            //if (!this.resettingGame)
            //{
            //}
            AddData(timestamp, method, content, gameSeed);

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

                // Manual parsing instead of regex: "tag=GAME_SEED value=(\d+)"
                int valueIndex = line.IndexOf("tag=GAME_SEED value=");
                if (valueIndex >= 0)
                {
                    int valueStart = valueIndex + "tag=GAME_SEED value=".Length;
                    int valueEnd = valueStart;
                    // Find the end of the number (space, end of line, or non-digit)
                    while (valueEnd < line.Length && char.IsDigit(line[valueEnd]))
                    {
                        valueEnd++;
                    }
                    if (valueEnd > valueStart)
                    {
                        string seedValue = line.Substring(valueStart, valueEnd - valueStart);
                        Logger.Log($"Extracted seed", seedValue);
                        return long.Parse(seedValue);
                    }
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