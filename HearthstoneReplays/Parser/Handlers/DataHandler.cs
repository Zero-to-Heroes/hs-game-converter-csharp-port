#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData;
using HearthstoneReplays.Parser.ReplayData.Entities;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using HearthstoneReplays.Parser.ReplayData.Meta;
using HearthstoneReplays.Events;
using Action = HearthstoneReplays.Parser.ReplayData.GameActions.Action;
using Newtonsoft.Json;

#endregion

namespace HearthstoneReplays.Parser.Handlers
{
    public class DataHandler
    {
        //public int index;

        private Helper helper = new Helper();
        private GameMetaData metadata;
        private SubSpell currentSubSpell;

        public void Handle(DateTime timestamp, string data, ParserState state, DateTime previousTimestamp)
        {
            var trimmed = data.Trim();
            var indentLevel = data.Length - trimmed.Length;
            data = trimmed;
            HandleNewGame(timestamp, data, state, previousTimestamp);
            bool isApplied = false;
            isApplied = isApplied || HandleSpectator(timestamp, data, state);

            // When catching up with some log lines, sometimes we get some leftover from a previous game.
            // Only checking the state does not account for these, and parsing fails because there is no
            // game to parse, and Reset() has not been called to initialize everything
            if (state.Ended || state.CurrentGame == null)
            {
                return;
            }

            isApplied = isApplied || HandleBlockEnd(data, state);
            isApplied = isApplied || HandleCreateGame(data, state, indentLevel);
            isApplied = isApplied || HandlePlayerName(timestamp, data, state);
            isApplied = isApplied || HandleMetaData(timestamp, data, state);
            isApplied = isApplied || HandleCreatePlayer(data, state, indentLevel);
            isApplied = isApplied || HandleBlockStart(timestamp, data, state, indentLevel);
            isApplied = isApplied || HandleActionMetaData(timestamp, data, state, indentLevel);
            isApplied = isApplied || HandleActionMetaDataInfo(timestamp, data, state, indentLevel);
            isApplied = isApplied || HandleSubSpell(timestamp, data, state);
            isApplied = isApplied || HandleShowEntity(timestamp, data, state, indentLevel);
            isApplied = isApplied || HandleChangeEntity(timestamp, data, state, indentLevel);
            isApplied = isApplied || HandleHideEntity(timestamp, data, state);
            isApplied = isApplied || HandleFullEntity(timestamp, data, state, indentLevel);
            isApplied = isApplied || HandleTagChange(timestamp, data, state, indentLevel);
            isApplied = isApplied || HandleTag(timestamp, data, state);
        }

        private bool HandleTag(DateTime timestamp, string data, ParserState state)
        {
            var match = Regexes.ActionTagRegex.Match(data);
            if (match.Success)
            {
                // This is not supported yet
                if (data.Contains("CACHED_TAG_FOR_DORMANT_CHANGE"))
                {
                    return false;
                }

                // When in reconnect, we don't parse the GameEntity and 
                // PlayerEntity nodes, so the tags think they are parsed while 
                // under the Game root node
                if (state.Node.Type == typeof(Game))
                {
                    return false;
                }

                var tagName = match.Groups[1].Value;
                var value = match.Groups[2].Value;
                Tag tag = null;
                try
                {
                    tag = helper.ParseTag(tagName, value);
                }
                catch (Exception e)
                {
                    Logger.Log("Warning when parsing Tag: " + tagName + " with value " + value, e.Message);
                    return false;
                }

                if (tag.Name == (int)GameTag.CURRENT_PLAYER)
                {
                    state.FirstPlayerId = ((PlayerEntity)state.Node.Object).Id;
                }

                if (state.Node.Type == typeof(GameEntity))
                    ((GameEntity)state.Node.Object).Tags.Add(tag);
                else if (state.Node.Type == typeof(PlayerEntity))
                    ((PlayerEntity)state.Node.Object).Tags.Add(tag);
                else if (state.Node.Type == typeof(FullEntity))
                {
                    ((FullEntity)state.Node.Object).Tags.Add(tag);
                }
                else if (state.Node.Type == typeof(ShowEntity))
                {
                    ((ShowEntity)state.Node.Object).Tags.Add(tag);
                }
                else if (state.Node.Type == typeof(ChangeEntity))
                {
                    ((ChangeEntity)state.Node.Object).Tags.Add(tag);
                    state.GameState.Tag(tag, ((ChangeEntity)state.Node.Object).Entity);
                }
                else
                {
                    Logger.Log("Invalid node " + state.Node.Type, data);
                }
                return true;
            }
            return false;
        }

        private bool HandleTagChange(DateTime timestamp, string data, ParserState state, int indentLevel)
        {
            var match = Regexes.ActionTagChangeRegex.Match(data);
            if (match.Success)
            {
                var rawEntity = match.Groups[1].Value;
                var tagName = match.Groups[2].Value;
                var value = match.Groups[3].Value;
                var defChange = match.Groups.Count >= 4 ? match.Groups[4].Value : null;
                Tag tag = null;
                try
                {
                    tag = helper.ParseTag(tagName, value);
                }
                catch (Exception e)
                {
                    Logger.Log("Warning when parsing TagChange: " + tagName + " with value " + value, e.Message);
                    return false;
                }
                state.GameState.UpdateEntityName(rawEntity);

                if (tag.Name == (int)GameTag.CURRENT_PLAYER)
                {
                    if (state.FirstPlayerId == -1)
                    {
                        state.FirstPlayerId = int.Parse(rawEntity);
                    }
                    UpdateCurrentPlayer(state, rawEntity, tag);
                }

                var entity = helper.ParseEntity(rawEntity, state);
                if (tag.Name == (int)GameTag.ENTITY_ID)
                {
                    entity = UpdatePlayerEntity(state, rawEntity, tag, entity);
                }

                var tagChange = new TagChange
                {
                    Entity = entity,
                    Name = tag.Name,
                    Value = tag.Value,
                    TimeStamp = timestamp,
                    DefChange = defChange,
                    SubSpellInEffect = this.currentSubSpell,
                };
                state.UpdateCurrentNode(typeof(Game), typeof(Action));
                state.CreateNewNode(new Node(typeof(TagChange), tagChange, indentLevel, state.Node, data));

                if (state.Node.Type == typeof(Game))
                    ((Game)state.Node.Object).AddData(tagChange);
                else if (state.Node.Type == typeof(Action))
                    ((Action)state.Node.Object).Data.Add(tagChange);
                else
                    throw new Exception("Invalid node " + state.Node.Type);
                state.GameState.TagChange(tagChange, defChange, timestamp + " " + data);
                return true;
            }
            return false;
        }

        private bool HandleFullEntity(DateTime timestamp, string data, ParserState state, int indentLevel)
        {
            var match = Regexes.ActionFullEntityUpdatingRegex.Match(data);
            if (!match.Success)
            {
                match = Regexes.ActionFullEntityCreatingRegex.Match(data);
            }
            if (match.Success)
            {
                var rawEntity = match.Groups[1].Value;
                var cardId = match.Groups[2].Value;
                var entity = helper.ParseEntity(rawEntity, state);
                state.GameState.UpdateEntityName(rawEntity);

                var fullEntity = new FullEntity { CardId = cardId, Id = entity, Tags = new List<Tag>(), TimeStamp = timestamp };
                fullEntity.SubSpellInEffect = this.currentSubSpell;
                state.UpdateCurrentNode(typeof(Game), typeof(Action));

                var newNode = new Node(typeof(FullEntity), fullEntity, indentLevel, state.Node, data);
                if (state.Node.Type == typeof(Game))
                    ((Game)state.Node.Object).AddData(fullEntity);
                else if (state.Node.Type == typeof(Action))
                    ((Action)state.Node.Object).Data.Add(fullEntity);
                else
                    throw new Exception("Invalid node " + state.Node.Type);
                state.CreateNewNode(newNode);
                state.Node = newNode;
                return true;
            }
            return false;
        }

        private bool HandleHideEntity(DateTime timestamp, string data, ParserState state)
        {
            var match = Regexes.ActionHideEntityRegex.Match(data);
            if (match.Success)
            {
                var rawEntity = match.Groups[1].Value;
                var tagName = match.Groups[2].Value;
                var value = match.Groups[3].Value;
                var entity = helper.ParseEntity(rawEntity, state);
                var zone = helper.ParseTag(tagName, value);

                var hideEntity = new HideEntity { Entity = entity, Zone = zone.Value, TimeStamp = timestamp };
                state.UpdateCurrentNode(typeof(Game), typeof(Action));

                if (state.Node.Type == typeof(Game))
                    ((Game)state.Node.Object).AddData(hideEntity);
                else if (state.Node.Type == typeof(Action))
                    ((Action)state.Node.Object).Data.Add(hideEntity);
                else
                    throw new Exception("Invalid node: " + state.Node.Type);
                return true;
            }
            return false;
        }

        private bool HandleChangeEntity(DateTime timestamp, string data, ParserState state, int indentLevel)
        {
            var match = Regexes.ActionChangeEntityRegex.Match(data);
            if (match.Success)
            {
                var rawEntity = match.Groups[1].Value;
                var cardId = match.Groups[2].Value;
                var entity = helper.ParseEntity(rawEntity, state);

                var changeEntity = new ChangeEntity { CardId = cardId, Entity = entity, Tags = new List<Tag>(), TimeStamp = timestamp };
                state.UpdateCurrentNode(typeof(Game), typeof(Action));
                if (state.Node.Type == typeof(Game))
                    ((Game)state.Node.Object).AddData(changeEntity);
                else if (state.Node.Type == typeof(Action))
                    ((Action)state.Node.Object).Data.Add(changeEntity);
                else
                    throw new Exception("Invalid node " + state.Node.Type);
                var newNode = new Node(typeof(ChangeEntity), changeEntity, indentLevel, state.Node, data);
                state.CreateNewNode(newNode);
                state.Node = newNode;
                return true;
            }
            return false;
        }

        private bool HandleShowEntity(DateTime timestamp, string data, ParserState state, int indentLevel)
        {
            var match = Regexes.ActionShowEntityRegex.Match(data);
            if (match.Success)
            {
                var rawEntity = match.Groups[1].Value;
                var cardId = match.Groups[2].Value;
                var entity = helper.ParseEntity(rawEntity, state);

                var showEntity = new ShowEntity { CardId = cardId, Entity = entity, Tags = new List<Tag>(), TimeStamp = timestamp };
                showEntity.SubSpellInEffect = this.currentSubSpell;
                state.UpdateCurrentNode(typeof(Game), typeof(Action));
                if (state.Node.Type == typeof(Game))
                    ((Game)state.Node.Object).AddData(showEntity);
                else if (state.Node.Type == typeof(Action))
                    ((Action)state.Node.Object).Data.Add(showEntity);
                else
                    throw new Exception("Invalid node " + state.Node.Type + " while parsing " + data);
                var newNode = new Node(typeof(ShowEntity), showEntity, indentLevel, state.Node, data);
                state.CreateNewNode(newNode);
                state.Node = newNode;
                return true;
            }
            return false;
        }

        private bool HandleSubSpell(DateTime timestamp, string data, ParserState state)
        {
            var match = Regexes.SubSpellStartRegex.Match(data);
            if (match.Success)
            {
                var subSpellPrefab = match.Groups[1].Value;
                var sourceEntityId = int.Parse(match.Groups[2].Value);
                if (sourceEntityId == 0)
                {
                    if (state.Node.Type == typeof(Action))
                    {
                        var parentAction = state.Node.Object as Action;
                        sourceEntityId = parentAction.Entity;
                    }
                }
                var sourceEntity = state.GameState.CurrentEntities.ContainsKey(sourceEntityId) ? state.GameState.CurrentEntities[sourceEntityId] : null;
                this.currentSubSpell = new SubSpell()
                {
                    Prefab = subSpellPrefab,
                    Timestamp = timestamp,
                };
                state.NodeParser.EnqueueGameEvent(new List<GameEventProvider> { GameEventProvider.Create(
                    timestamp,
                    "SUB_SPELL_START",
                    () => new GameEvent
                    {
                        Type = "SUB_SPELL_START",
                        Value = new
                        {
                            PrefabId = subSpellPrefab,
                            EntityId = sourceEntityId,
                            CardId = sourceEntity?.CardId,
                            LocalPlayer = state.LocalPlayer,
                            OpponentPlayer = state.OpponentPlayer,
                            ControllerId = sourceEntity?.GetController(),
                        }
                    },
                    false,
                    new Node(null, null, 0, null, data)
                )});
                return true;
            }

            match = Regexes.SubSpellSourceRegex.Match(data);
            if (match.Success && this.currentSubSpell != null)
            {
                var rawEntity = match.Groups[1].Value;
                var entity = helper.ParseEntity(rawEntity, state);
                this.currentSubSpell.Source = entity;
                return true;
            }

            match = Regexes.SubSpellTargetsRegex.Match(data);
            if (match.Success && this.currentSubSpell != null)
            {
                var rawEntity = match.Groups[1].Value;
                var entity = helper.ParseEntity(rawEntity, state);
                if (this.currentSubSpell.Targets == null)
                {
                    this.currentSubSpell.Targets = new List<int>();
                }
                this.currentSubSpell.Targets.Add(entity);
                return true;
            }

            if (data == "SUB_SPELL_END")
            {
                //Logger.Log("Sub spell end", this.currentSubSpell);
                state.NodeParser.CloseNode(new Node(typeof(SubSpell), this.currentSubSpell, 0, state.Node, data));
                this.currentSubSpell = null;
                return true;
            }
            return false;
        }

        private bool HandleActionMetaDataInfo(DateTime timestamp, string data, ParserState state, int indentLevel)
        {
            var match = Regexes.ActionMetaDataInfoRegex.Match(data);
            if (match.Success)
            {
                var index = match.Groups[1].Value;
                var rawEntity = match.Groups[2].Value;
                var entity = helper.ParseEntity(rawEntity, state);
                var metaInfo = new Info { Id = entity, Index = int.Parse(index), Entity = entity, TimeStamp = timestamp };
                if (state.Node.Type == typeof(MetaData))
                    ((MetaData)state.Node.Object).MetaInfo.Add(metaInfo);
                else
                    throw new Exception("Invalid node " + state.Node.Type + " while parsing " + data);
                state.CreateNewNode(new Node(typeof(Info), metaInfo, indentLevel, state.Node, data));
                return true;
            }
            return false;
        }

        private bool HandleActionMetaData(DateTime timestamp, string data, ParserState state, int indentLevel)
        {
            var match = Regexes.ActionMetadataRegex.Match(data);
            if (match.Success)
            {
                var rawMeta = match.Groups[1].Value;
                var rawData = match.Groups[2].Value;
                var info = match.Groups[3].Value;
                var parsedData = helper.ParseEntity(rawData, state);
                var meta = helper.ParseEnum<MetaDataType>(rawMeta);
                var metaData = new MetaData { Data = parsedData, Info = int.Parse(info), Meta = meta, MetaInfo = new List<Info>(), TimeStamp = timestamp };
                state.UpdateCurrentNode(typeof(Action));
                if (state.Node.Type == typeof(Action))
                    ((Action)state.Node.Object).Data.Add(metaData);
                else if (state.Node.Type == typeof(Game))
                    ((Game)state.Node.Object).AddData(metaData);
                else
                    throw new Exception("Invalid node " + state.Node.Type + " for " + timestamp + " " + data);
                var newNode = new Node(typeof(MetaData), metaData, indentLevel, state.Node, data);
                state.CreateNewNode(newNode);
                state.Node = newNode;
                return true;
            }
            return false;
        }

        private bool HandleBlockStart(DateTime timestamp, string data, ParserState state, int indentLevel)
        {
            var match = Regexes.ActionStartRegex.Match(data);
            if (match.Success)
            {
                var rawType = match.Groups[1].Value;
                var rawEntity = match.Groups[2].Value;
                var effectId = match.Groups[3].Value;
                var effectIndex = match.Groups[4].Value;
                var rawTarget = match.Groups[5].Value;
                var subOption = int.Parse(match.Groups[6].Value);
                var rawTriggerKeyword = match.Groups[7].Value;

                //Console.WriteLine("Really updating entityname " + rawEntity + " for full log " + data);
                state.GameState.UpdateEntityName(rawEntity);

                var entity = helper.ParseEntity(rawEntity, state);
                var target = helper.ParseEntity(rawTarget, state);
                var type = helper.ParseEnum<BlockType>(rawType);
                var triggerKeyword = helper.ParseEnum<GameTag>(rawTriggerKeyword);
                var action = new Action
                {
                    Data = new List<GameData>(),
                    Entity = entity,
                    Target = target,
                    TimeStamp = timestamp,
                    Type = type,
                    SubOption = subOption,
                    TriggerKeyword = triggerKeyword
                };
                if (effectIndex != null && effectIndex.Length > 0)
                {
                    action.EffectIndex = int.Parse(effectIndex);
                }
                state.UpdateCurrentNode(typeof(Game), typeof(Action));
                if (state.Node.Type == typeof(Game))
                    ((Game)state.Node.Object).AddData(action);
                else if (state.Node.Type == typeof(Action))
                    ((Action)state.Node.Object).Data.Add(action);
                else
                    throw new Exception("Invalid node " + state.Node.Type);
                var newNode = new Node(typeof(Action), action, indentLevel, state.Node, data);
                state.CreateNewNode(newNode);
                // Logger.Log("Creating new node", newNode.CreationLogLine);
                state.Node = newNode;
                return true;
            }


            match = Regexes.ActionStartRegex_Short.Match(data);
            if (match.Success)
            {
                //state.ReconnectionOngoing = false;
                var rawType = match.Groups[1].Value;
                var rawEntity = match.Groups[2].Value;
                var effectId = match.Groups[3].Value;
                var effectIndex = match.Groups[4].Value;
                var rawTarget = match.Groups[5].Value;
                var subOption = int.Parse(match.Groups[6].Value);

                var entity = helper.ParseEntity(rawEntity, state);
                var target = helper.ParseEntity(rawTarget, state);
                var type = helper.ParseEnum<BlockType>(rawType);
                var action = new Action
                {
                    Data = new List<GameData>(),
                    Entity = entity,
                    Target = target,
                    TimeStamp = timestamp,
                    Type = type,
                    SubOption = subOption
                };
                if (effectIndex != null && effectIndex.Length > 0)
                {
                    action.EffectIndex = int.Parse(effectIndex);

                }
                state.UpdateCurrentNode(typeof(Game), typeof(Action));
                if (state.Node.Type == typeof(Game))
                    ((Game)state.Node.Object).AddData(action);
                else if (state.Node.Type == typeof(Action))
                    ((Action)state.Node.Object).Data.Add(action);
                else
                    throw new Exception("Invalid node " + state.Node.Type);
                var newNode = new Node(typeof(Action), action, indentLevel, state.Node, data);
                state.CreateNewNode(newNode);
                // Logger.Log("Creating new node short", newNode.CreationLogLine);
                state.Node = newNode;
                return true;
            }

            match = Regexes.ActionStartRegex_8_4.Match(data);
            if (match.Success)
            {
                //state.ReconnectionOngoing = false;
                var rawType = match.Groups[1].Value;
                var rawEntity = match.Groups[2].Value;
                var effectId = match.Groups[3].Value;
                var effectIndex = match.Groups[4].Value;
                var rawTarget = match.Groups[5].Value;
                var entity = helper.ParseEntity(rawEntity, state);
                var target = helper.ParseEntity(rawTarget, state);
                var type = helper.ParseEnum<BlockType>(rawType);
                var action = new Action
                {
                    Data = new List<GameData>(),
                    Entity = entity,
                    Target = target,
                    TimeStamp = timestamp,
                    Type = type
                };
                if (effectIndex != null && effectIndex.Length > 0)
                {
                    action.EffectIndex = int.Parse(effectIndex);

                }
                state.UpdateCurrentNode(typeof(Game), typeof(Action));
                if (state.Node.Type == typeof(Game))
                    ((Game)state.Node.Object).AddData(action);
                else if (state.Node.Type == typeof(Action))
                    ((Action)state.Node.Object).Data.Add(action);
                else
                    throw new Exception("Invalid node " + state.Node.Type + " while parsing " + data);
                var newNode = new Node(typeof(Action), action, indentLevel, state.Node, data);
                state.CreateNewNode(newNode);
                // Logger.Log("Creating new old", newNode.CreationLogLine);
                state.Node = newNode;
                return true;
            }
            return false;
        }

        private static bool HandleCreatePlayer(string data, ParserState state, int indentLevel)
        {
            var match = Regexes.ActionCreategamePlayerRegex.Match(data);
            if (match.Success)
            {
                var id = match.Groups[1].Value;
                var playerId = match.Groups[2].Value;
                var accountHi = match.Groups[3].Value;
                var accountLo = match.Groups[4].Value;
                var pEntity = new PlayerEntity
                {
                    Id = int.Parse(id),
                    AccountHi = accountHi,
                    AccountLo = accountLo,
                    PlayerId = int.Parse(playerId),
                    Tags = new List<Tag>()
                };
                state.UpdateCurrentNode(typeof(Game));
                state.CurrentGame.AddData(pEntity);

                var newNode = new Node(typeof(PlayerEntity), pEntity, indentLevel, state.Node, data);
                state.CreateNewNode(newNode);
                state.Node = newNode;
                return true;
            }
            return false;
        }

        private bool HandleMetaData(DateTime timestamp, string data, ParserState state)
        {
            System.Text.RegularExpressions.Match match = Regexes.BuildNumber.Match(data);
            if (match.Success)
            {
                this.metadata.BuildNumber = int.Parse(match.Groups[1].Value);
                return true;
            }

            match = Regexes.GameType.Match(data);
            if (match.Success)
            {
                var rawGameType = match.Groups[1].Value;
                var gameType = helper.ParseEnum<GameType>(rawGameType);
                this.metadata.GameType = gameType;
                return true;
            }

            match = Regexes.FormatType.Match(data);
            if (match.Success)
            {
                var rawFormatType = match.Groups[1].Value;
                var formatType = helper.ParseEnum<FormatType>(rawFormatType);
                this.metadata.FormatType = formatType;
                return true;
            }

            match = Regexes.ScenarioID.Match(data);
            if (match.Success)
            {
                this.metadata.ScenarioID = int.Parse(match.Groups[1].Value);
                state.NodeParser.EnqueueGameEvent(new List<GameEventProvider> { GameEventProvider.Create(
                    timestamp,
                    "MATCH_METADATA",
                    () => {
                        state.CurrentGame.BuildNumber = metadata.BuildNumber;
                        state.CurrentGame.GameType = metadata.GameType;
                        state.CurrentGame.FormatType = metadata.FormatType;
                        state.CurrentGame.ScenarioID = metadata.ScenarioID;
                        state.GameState.MetaData = metadata;
                        return new GameEvent
                        {
                            Type = "MATCH_METADATA",
                            Value = new {
                                MetaData = this.metadata,
                                Spectating = state.Spectating,
                            }
                        };
                    },
                    false,
                    new Node(null, null, 0, null, data),
                    false,
                    false,
                    true) });


                if (state.ReconnectionOngoing)
                {
                    state.ReconnectionOngoing = false;
                    state.NodeParser.EnqueueGameEvent(new List<GameEventProvider> { GameEventProvider.Create(
                        timestamp,
                        "RECONNECT_OVER",
                        () => {
                            return new GameEvent
                            {
                                Type = "RECONNECT_OVER",
                            };
                        },
                        false,
                        new Node(null, null, 0, null, data),
                        true,
                        false,
                        false) });
                }
                return true;
            }
            return false;
        }

        private static bool HandlePlayerName(DateTime timestamp, string data, ParserState state)
        {
            var match = Regexes.PlayerNameAssignment.Match(data);
            if (match.Success)
            {
                var playerId = int.Parse(match.Groups[1].Value);
                var playerName = match.Groups[2].Value;
                try
                {
                    var matchingPlayer = state.getPlayers()
                        .Where(player => player.PlayerId == playerId)
                        .First();
                    matchingPlayer.Name = playerName;
                    matchingPlayer.InitialName = playerName;
                    state.TryAssignLocalPlayer(timestamp, data);
                    Logger.Log("Tried to assign player name", data);
                }
                catch (Exception e)
                {
                    Logger.Log("Exceptionw while assigning player name", data);
                    return false;
                }
                return true;
            }
            return false;
        }

        private static bool HandleCreateGame(string data, ParserState state, int indentLevel)
        {
            var match = Regexes.ActionCreategameRegex.Match(data);
            if (match.Success)
            {
                var id = match.Groups[1].Value;
                var gEntity = new GameEntity { Id = int.Parse(id), Tags = new List<Tag>() };
                state.CurrentGame.AddData(gEntity);
                var newNode = new Node(typeof(GameEntity), gEntity, indentLevel, state.Node, data);
                state.CreateNewNode(newNode);
                state.Node = newNode;
                return true;
            }
            return false;
        }

        private static bool HandleBlockEnd(string data, ParserState state)
        {
            if (data == "BLOCK_END")
            {
                // Logger.Log("Current node after end action", state.Node.CreationLogLine);
                state.UpdateCurrentNode(typeof(Game), typeof(Action));
                // Logger.Log("Preparing to end action", timestamp);
                state.EndAction();
                // Logger.Log("Current node after update // " + state.Node.Type + " // " + (state.Node.Type == typeof(Action)), state.Node.CreationLogLine);
                state.Node = state.Node.Parent ?? state.Node;
                // Logger.Log("Current node is now", state.Node.CreationLogLine);
                return true;
            }
            return false;
        }

        private static bool HandleSpectator(DateTime timestamp, string data, ParserState state)
        {
            if (data.Contains("Begin Spectating"))
            {
                state.Reset();
                state.Spectating = true;
                state.NodeParser.EnqueueGameEvent(new List<GameEventProvider> { GameEventProvider.Create(
                    timestamp,
                    "SPECTATING",
                    () => new GameEvent
                    {
                        Type = "SPECTATING",
                        Value = new
                        {
                            LocalPlayer = state.LocalPlayer,
                            OpponentPlayer = state.OpponentPlayer,
                            Spectating = true,
                        }
                    },
                    false,
                    new Node(null, null, 0, null, data),
                    true
                )});
            }
            if (data.Contains("End Spectator Mode"))
            {
                if (state?.LocalPlayer == null)
                {
                    return false;
                }

                var replayCopy = state.Replay;
                var xmlReplay = new ReplayConverter().xmlFromReplay(replayCopy);
                var gameStateReport = state.GameState.BuildGameStateReport();
                state.Spectating = false;
                state.NodeParser.EnqueueGameEvent(new List<GameEventProvider> { GameEventProvider.Create(
                    timestamp,
                    "SPECTATING",
                    () => new GameEvent
                    {
                        Type = "SPECTATING",
                        Value = new
                        {
                            LocalPlayer = state.LocalPlayer,
                            OpponentPlayer = state.OpponentPlayer,
                            Spectating = false,
    }
},
                    false,
                    new Node(null, null, 0, null, data),
                    true
                )});
                state.EndCurrentGame();
                return true;
            }
            return false;
        }

        private bool HandleNewGame(DateTime timestamp, string data, ParserState state, DateTime previousTimestamp)
        {
            if (data == "CREATE_GAME")
            {
                state.NodeParser.ClearQueue();
                //Logger.Log("Handling create game", "");
                var isReconnecting = !state.Ended && state.NumberOfCreates >= 1 && !state.Spectating;
                if (isReconnecting)
                {
                    Logger.Log("Probable reconnect detected " + timestamp + " // " + previousTimestamp, "" + (timestamp - previousTimestamp));
                    state.NodeParser.EnqueueGameEvent(new List<GameEventProvider> { GameEventProvider.Create(
                        timestamp,
                        "RECONNECT_START",
                        () => {
                            return new GameEvent
                            {
                                Type = "RECONNECT_START",
                            };
                        },
                        false,
                        new Node(null, null, 0, null, data),
                        false,
                        false,
                        false) });
                }
                this.metadata = new GameMetaData()
                {
                    BuildNumber = -1,
                    FormatType = -1,
                    GameType = -1,
                    ScenarioID = -1,
                };
                state.Reset();
                state.NumberOfCreates++;
                state.ReconnectionOngoing = isReconnecting;
                state.CurrentGame = new Game { Data = new List<GameData>(), TimeStamp = timestamp };
                state.Replay.Games.Add(state.CurrentGame);
                var newNode = new Node(typeof(Game), state.CurrentGame, 0, null, data);
                state.CreateNewNode(newNode);
                state.Node = newNode;
                Logger.Log("Created a new game", "" + timestamp + "," + previousTimestamp);
                return true;
            }
            return false;
        }

        private int UpdatePlayerEntity(ParserState state, string rawEntity, Tag tag, int entity)
        {
            int tmp;
            if (!int.TryParse(rawEntity, out tmp) && !rawEntity.StartsWith("[") && rawEntity != "GameEntity")
            {
                if (entity != tag.Value)
                {
                    entity = tag.Value;
                    var tmpName = ((PlayerEntity)state.CurrentGame.Data[1]).Name;
                    ((PlayerEntity)state.CurrentGame.Data[1]).Name = ((PlayerEntity)state.CurrentGame.Data[2]).Name;
                    ((PlayerEntity)state.CurrentGame.Data[2]).Name = tmpName;

                    foreach (var dataObj in ((Game)state.Node.Object).Data)
                    {
                        var tChange = dataObj as TagChange;
                        if (tChange != null)
                            tChange.Entity = tChange.Entity == 2 ? 3 : 2;
                    }
                }
            }

            return entity;
        }

        private void UpdateCurrentPlayer(ParserState state, string rawEntity, Tag tag)
        {
            if (tag.Value == 0)
            {
                try
                {
                    helper.ParseEntity(rawEntity, state);
                }
                catch
                {
                    var currentPlayer =
                        (PlayerEntity)state.CurrentGame.Data.Single(x => (x is PlayerEntity) && ((PlayerEntity)x).Id == state.CurrentPlayerId);
                    currentPlayer.Name = rawEntity;
                    currentPlayer.InitialName = rawEntity;
                }
            }
            else if (tag.Value == 1)
            {
                try
                {
                    helper.ParseEntity(rawEntity, state);
                }
                catch
                {
                    var currentPlayer =
                        (PlayerEntity)state.CurrentGame.Data.Single(x => (x is PlayerEntity) && ((PlayerEntity)x).Id != state.CurrentPlayerId);
                    currentPlayer.Name = rawEntity;
                    currentPlayer.InitialName = rawEntity;
                }
                state.CurrentPlayerId = helper.ParseEntity(rawEntity, state);
            }
        }
    }
}