﻿#region

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
        private Helper helper;
        private GameMetaData metadata;


        public DataHandler(Helper helper)
        {
            this.helper = helper;
        }

        public void Handle(DateTime timestamp, string data, ParserState state, StateType stateType, DateTime previousTimestamp, StateFacade stateFacade, long currentGameSeed)
        {
            var trimmed = data.Trim();
            var indentLevel = data.Length - trimmed.Length;
            data = trimmed;
            bool isApplied = false;
            isApplied = isApplied || HandleNewGame(timestamp, data, state, previousTimestamp, stateType, stateFacade, currentGameSeed);
            isApplied = isApplied || HandleSpectator(timestamp, data, state, stateFacade);

            // When catching up with some log lines, sometimes we get some leftover from a previous game.
            // Only checking the state does not account for these, and parsing fails because there is no
            // game to parse, and Reset() has not been called to initialize everything
            if (state.Ended || state.CurrentGame == null)
            {
                return;
            }

            isApplied = isApplied || HandleCreateGame(timestamp, data, state, indentLevel);
            isApplied = isApplied || HandlePlayerName(timestamp, data, state, stateType);
            isApplied = isApplied || HandleBlockStart(timestamp, data, state, indentLevel);
            isApplied = isApplied || HandleBlockEnd(data, state);
            isApplied = isApplied || HandleMetaData(timestamp, data, state, stateType);
            isApplied = isApplied || HandleCreatePlayer(data, state, stateFacade, indentLevel);
            isApplied = isApplied || HandleActionMetaData(timestamp, data, state, indentLevel);
            isApplied = isApplied || HandleActionMetaDataInfo(timestamp, data, state, indentLevel);
            isApplied = isApplied || HandleSubSpell(timestamp, data, state, stateType, stateFacade);
            isApplied = isApplied || HandleShowEntity(timestamp, data, state, indentLevel);
            isApplied = isApplied || HandleChangeEntity(timestamp, data, state, indentLevel);
            isApplied = isApplied || HandleHideEntity(timestamp, data, state, indentLevel);
            isApplied = isApplied || HandleFullEntity(timestamp, data, state, indentLevel);
            isApplied = isApplied || HandleTagChange(timestamp, data, state, stateType, stateFacade, indentLevel);
            isApplied = isApplied || HandleTag(timestamp, data, state);
            isApplied = isApplied || HandleShuffleDeck(timestamp, data, state, indentLevel);
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
                //var debug = value == "BATTLEGROUND_TRINKET";
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

                // To handle reconnects
                if (tag.Name == (int)GameTag.CURRENT_PLAYER && state.Node.Object is PlayerEntity)
                {
                    state.FirstPlayerEntityId = ((PlayerEntity)state.Node.Object).Id;
                }

                if (state.Node.Type == typeof(GameEntity))
                {
                    ((GameEntity)state.Node.Object).Tags.Add(tag);
                    if (tag.Name == (int)GameTag.GAME_SEED)
                    {
                        state.CurrentGame.GameSeed = tag.Value;
                    }
                }
                else if (state.Node.Type == typeof(PlayerEntity))
                    ((PlayerEntity)state.Node.Object).Tags.Add(tag);
                else if (state.Node.Type == typeof(FullEntity))
                {
                    var fullEntity = ((FullEntity)state.Node.Object);
                    fullEntity.Tags.Add(tag);
                    // Push the changes as they occur, so that it's ok if we miss a block end because of malformed logs
                    // UPDATE: this is in fact not possible, because I need to have the FullEntities in the state with their previous
                    // tags when applying CloseNode effects.
                    // It might be possible to work around that, but it will require too much work and it too risky
                    //state.GameState.CurrentEntities[((FullEntity)state.Node.Object).Entity].Tags.Add(tag);
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

        private bool HandleTagChange(DateTime timestamp, string data, ParserState state, StateType stateType, StateFacade gameHelper, int indentLevel)
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
                    if (state.FirstPlayerEntityId == -1)
                    {
                        // If GameState logs, this is an int, in PowerTaskList, this is the player's BTag
                        int entityId = -1;
                        int.TryParse(rawEntity, out entityId);
                        if (entityId <= 0)
                        {
                            entityId = helper.GetPlayerIdFromName(rawEntity);
                        }
                        state.FirstPlayerEntityId = entityId;
                    }
                    UpdateCurrentPlayer(state, rawEntity, tag);
                }

                if (stateType == StateType.PowerTaskList && tag.Name == (int)GameTag.PLAYSTATE && tag.Value == (int)PlayState.PLAYING)
                {
                    if (state.ReconnectionOngoing)
                    {
                        state.ReconnectionOngoing = false;
                        gameHelper.GsState.ReconnectionOngoing = false;
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
                            new Node(null, null, 0, null, data)) });
                    }
                }

                var entity = helper.ParseEntity(rawEntity);
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
                    SubSpellInEffect = state.CurrentSubSpell?.GetActiveSubSpell(),
                };
                state.UpdateCurrentNode(typeof(Game), typeof(Action));
                state.CreateNewNode(new Node(typeof(TagChange), tagChange, indentLevel, state.Node, data));

                if (state.Node.Type == typeof(Game))
                    ((Game)state.Node.Object).AddData(tagChange);
                else if (state.Node.Type == typeof(Action))
                    ((Action)state.Node.Object).Data.Add(tagChange);
                else
                    throw new Exception("Invalid node " + state.Node.Type);

                state.GameState.TagChange(tagChange, defChange);

                // In BG, it sometimes happen that the BLOCK_END element is missing. This typically happens after 
                // a non-zero NUM_OPTIONS_PLAYED_THIS_TURN tag change.
                // From what I've seen, these should always go back to the root afterwards, so we force it here
                if (tagChange.Name == (int)GameTag.NUM_OPTIONS_PLAYED_THIS_TURN && tagChange.Value > 0)
                {
                    if (state.Node.Type != typeof(Game))
                    {
                        state.EndAction();
                    }
                    state.UpdateCurrentNode(typeof(Game));
                }
                return true;
            }
            return false;
        }

        private bool HandleShuffleDeck(DateTime timestamp, string data, ParserState state, int indentLevel)
        {
            var match = Regexes.ActionShuffleDeckRegex.Match(data);
            if (match.Success)
            {
                var playerId = match.Groups[1].Value;

                var shuffleNode = new ShuffleDeck
                {
                    PlayerId = int.Parse(playerId),
                };
                state.UpdateCurrentNode(typeof(Game), typeof(Action));
                state.CreateNewNode(new Node(typeof(ShuffleDeck), shuffleNode, indentLevel, state.Node, data));

                if (state.Node.Type == typeof(Game))
                    ((Game)state.Node.Object).AddData(shuffleNode);
                else if (state.Node.Type == typeof(Action))
                    ((Action)state.Node.Object).Data.Add(shuffleNode);
                else
                    throw new Exception("Invalid node " + state.Node.Type);

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
                var entity = helper.ParseEntity(rawEntity);
                state.GameState.UpdateEntityName(rawEntity);

                var fullEntity = new FullEntity { CardId = cardId, Id = entity, Tags = new List<Tag>(), TimeStamp = timestamp };
                fullEntity.SubSpellInEffect = state.CurrentSubSpell?.GetActiveSubSpell();
                //state.GameState.FullEntity(fullEntity, false);

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

        private bool HandleHideEntity(DateTime timestamp, string data, ParserState state, int indentLevel)
        {
            var match = Regexes.ActionHideEntityRegex.Match(data);
            if (match.Success)
            {
                var rawEntity = match.Groups[1].Value;
                var tagName = match.Groups[2].Value;
                var value = match.Groups[3].Value;
                var entity = helper.ParseEntity(rawEntity);
                var zone = helper.ParseTag(tagName, value);

                var hideEntity = new HideEntity { Entity = entity, Zone = zone.Value, TimeStamp = timestamp };
                state.UpdateCurrentNode(typeof(Game), typeof(Action));

                if (state.Node.Type == typeof(Game))
                    ((Game)state.Node.Object).AddData(hideEntity);
                else if (state.Node.Type == typeof(Action))
                    ((Action)state.Node.Object).Data.Add(hideEntity);
                else
                    throw new Exception("Invalid node: " + state.Node.Type);

                var newNode = new Node(typeof(HideEntity), hideEntity, indentLevel, state.Node, data);
                state.CreateNewNode(newNode);
                state.Node = newNode;
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
                var entity = helper.ParseEntity(rawEntity);

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
                var entity = helper.ParseEntity(rawEntity);

                var showEntity = new ShowEntity { CardId = cardId, Entity = entity, Tags = new List<Tag>(), TimeStamp = timestamp };
                showEntity.SubSpellInEffect = state.CurrentSubSpell?.GetActiveSubSpell();
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

        private bool HandleSubSpell(DateTime timestamp, string data, ParserState state, StateType stateType, StateFacade stateFacade)
        {
            var match = Regexes.SubSpellStartRegex.Match(data);
            if (match.Success)
            {
                var subSpellPrefab = match.Groups[1].Value;
                var sourceEntityId = int.Parse(match.Groups[2].Value);
                Node parentActionNode = state.Node;
                while (parentActionNode != null && parentActionNode.Type != typeof(Action))
                {
                    parentActionNode = parentActionNode?.Parent;
                }

                Action parentAction = null;
                if (parentActionNode != null)
                {
                    parentAction = parentActionNode.Object as Action;
                }
                if (sourceEntityId == 0)
                {
                    sourceEntityId = parentAction?.Entity ?? -1;
                }
                var sourceEntity = state.GameState.CurrentEntities.ContainsKey(sourceEntityId) ? state.GameState.CurrentEntities[sourceEntityId] : null;
                var spell = new SubSpell()
                {
                    Prefab = subSpellPrefab,
                    Timestamp = timestamp,
                };
                SetActiveSubSpell(state, spell);
                if (parentAction != null)
                {
                    parentAction.SubSpells.Add(spell);
                }

                state.NodeParser.NewNode(new Node(typeof(SubSpell), state.CurrentSubSpell?.GetActiveSubSpell(), 0, state.Node, data), stateType);
                if (stateType == StateType.PowerTaskList && !state.IsBattlegrounds())
                {
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
                                ParentEntityId = parentAction?.Entity,
                                ParentCardId = state.GameState.CurrentEntities.ContainsKey(parentAction?.Entity ?? -1) ? state.GameState.CurrentEntities[parentAction.Entity].CardId : null,
                                LocalPlayer = stateFacade.LocalPlayer,
                                OpponentPlayer = stateFacade.OpponentPlayer,
                                ControllerId = sourceEntity?.GetController(),
                            }
                        },
                        false,
                        new Node(null, null, 0, null, data)
                    )});
                }
                return true;
            }

            match = Regexes.SubSpellSourceRegex.Match(data);
            if (match.Success && state.CurrentSubSpell != null)
            {
                var rawEntity = match.Groups[1].Value;
                var entity = helper.ParseEntity(rawEntity);
                state.CurrentSubSpell.GetActiveSubSpell().Source = entity;
                return true;
            }

            match = Regexes.SubSpellTargetsRegex.Match(data);
            if (match.Success && state.CurrentSubSpell != null)
            {
                var rawEntity = match.Groups[1].Value;
                var entity = helper.ParseEntity(rawEntity);
                if (state.CurrentSubSpell.GetActiveSubSpell().Targets == null)
                {
                    state.CurrentSubSpell.GetActiveSubSpell().Targets = new List<int>();
                }
                state.CurrentSubSpell.GetActiveSubSpell().Targets.Add(entity);
                return true;
            }

            if (data == "SUB_SPELL_END")
            {
                //Logger.Log("Sub spell end", this.currentSubSpell);
                var debug = state.CurrentSubSpell?.GetActiveSubSpell();
                state.NodeParser.CloseNode(new Node(typeof(SubSpell), state.CurrentSubSpell?.GetActiveSubSpell(), 0, state.Node, data), stateType);
                if (stateType == StateType.PowerTaskList && state.CurrentSubSpell != null && !state.IsBattlegrounds())
                {
                    var subSpell = state.CurrentSubSpell.GetActiveSubSpell();
                    Action parentAction = null;
                    if (state.Node?.Type == typeof(Action))
                    {
                        parentAction = state.Node.Object as Action;
                    }
                    var sourceEntityId = subSpell.Source;
                    if (sourceEntityId == 0)
                    {
                        sourceEntityId = parentAction?.Entity ?? -1;
                    }
                    var sourceEntity = state.GameState.CurrentEntities.ContainsKey(sourceEntityId) ? state.GameState.CurrentEntities[sourceEntityId] : null;
                    state.NodeParser.EnqueueGameEvent(new List<GameEventProvider> { GameEventProvider.Create(
                        timestamp,
                        "SUB_SPELL_END",
                        () => new GameEvent
                        {
                            Type = "SUB_SPELL_END",
                            Value = new
                            {
                                PrefabId = subSpell.Prefab,
                                SourceEntityId = sourceEntityId,
                                SourceCardId = sourceEntity?.CardId,
                                TargetEntityIds = subSpell.Targets,
                                LocalPlayer = stateFacade.LocalPlayer,
                                OpponentPlayer = stateFacade.OpponentPlayer,
                                ControllerId = sourceEntity?.GetController(),
                            }
                        },
                        false,
                        new Node(null, null, 0, null, data)
                    )});
                }
                SetActiveSubSpell(state, null);
                return true;
            }
            return false;
        }

        private void SetActiveSubSpell(ParserState state, SubSpell spell)
        {
            var type = state.StateType;
            if (spell != null)
            {
                if (state.CurrentSubSpell == null)
                {
                    state.CurrentSubSpell = spell;
                }
                else
                {
                    state.CurrentSubSpell.GetActiveSubSpell().Spell = spell;
                }
            }
            else
            {
                state.ClearActiveSubSpell();
            }

        }

        private bool HandleActionMetaDataInfo(DateTime timestamp, string data, ParserState state, int indentLevel)
        {
            var match = Regexes.ActionMetaDataInfoRegex.Match(data);
            if (match.Success)
            {
                var index = match.Groups[1].Value;
                var rawEntity = match.Groups[2].Value;
                var entity = helper.ParseEntity(rawEntity);
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
                var parsedData = helper.ParseEntity(rawData);
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

                var entity = helper.ParseEntity(rawEntity);
                var target = helper.ParseEntity(rawTarget);
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
                    TriggerKeyword = triggerKeyword,
                    DebugCreationLine = data,
                };
                if (effectIndex != null && effectIndex.Length > 0)
                {
                    action.EffectIndex = int.Parse(effectIndex);
                }

                // Some battlegrounds files do not balance the BLOCK_START and BLOCK_END
                // This seems to be mainly about ATTACK block
                // see https://github.com/HearthSim/python-hslog/commit/63e9e41976cbec7ef95ced0f49f4b9a06c02cf3c
                if (type == (int)BlockType.PLAY)
                {
                    // PLAY is always at the root
                    state.UpdateCurrentNode(typeof(Game));
                }
                // Attack blocks should only have TRIGGER beneath them. If something else, it certainly
                // means the ATTACK block wasn't correctly closed
                else if (type != (int)BlockType.TRIGGER && state.Node?.Type == typeof(Action))
                {
                    var parentAction = state.Node.Object as Action;
                    if (parentAction.Type == (int)BlockType.ATTACK)
                    {
                        state.UpdateCurrentNode(typeof(Game));
                    }
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

                var entity = helper.ParseEntity(rawEntity);
                var target = helper.ParseEntity(rawTarget);
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
                var entity = helper.ParseEntity(rawEntity);
                var target = helper.ParseEntity(rawTarget);
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

        private static bool HandleCreatePlayer(string data, ParserState state, StateFacade stateFacade, int indentLevel)
        {
            var match = Regexes.ActionCreategamePlayerRegex.Match(data);
            // We already have the player entities while reconnecting, so we don't re-parse them
            if (!state.ReconnectionOngoing && match.Success)
            {
                var id = match.Groups[1].Value;
                var playerId = match.Groups[2].Value;
                var accountHi = match.Groups[3].Value;
                var accountLo = match.Groups[4].Value;
                var gsPlayer = stateFacade.GetPlayers()?.Find(p => p.Id == int.Parse(id));
                var pEntity = new PlayerEntity()
                {
                    Id = int.Parse(id),
                    AccountHi = accountHi,
                    AccountLo = accountLo,
                    PlayerId = int.Parse(playerId),
                    InitialName = gsPlayer?.InitialName,
                    Name = gsPlayer?.Name,
                    Tags = new List<Tag>(),
                    IsMainPlayer = gsPlayer?.IsMainPlayer ?? false,
                    Cardback = gsPlayer?.Cardback,
                    LegendRank = gsPlayer?.LegendRank,
                    Rank = gsPlayer?.Rank,
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

        private bool HandleMetaData(DateTime timestamp, string data, ParserState state, StateType stateType)
        {
            System.Text.RegularExpressions.Match match = Regexes.BuildNumber.Match(data);
            if (match.Success)
            {
                this.metadata.BuildNumber = int.Parse(match.Groups[1].Value);
                state.CurrentGame.BuildNumber = metadata.BuildNumber;
                return true;
            }

            match = Regexes.GameType.Match(data);
            if (match.Success)
            {
                var rawGameType = match.Groups[1].Value;
                var gameType = helper.ParseEnum<GameType>(rawGameType);
                this.metadata.GameType = gameType;
                // We need to assign it right now, otherwise we can't use the meta data while 
                // doing the logic for player assignments, which is needed for mercenaries
                state.CurrentGame.GameType = metadata.GameType;
                return true;
            }

            match = Regexes.FormatType.Match(data);
            if (match.Success)
            {
                var rawFormatType = match.Groups[1].Value;
                var formatType = helper.ParseEnum<FormatType>(rawFormatType);
                this.metadata.FormatType = formatType;
                state.CurrentGame.FormatType = metadata.FormatType;
                return true;
            }

            match = Regexes.ScenarioID.Match(data);
            if (match.Success)
            {
                this.metadata.ScenarioID = int.Parse(match.Groups[1].Value);
                state.CurrentGame.ScenarioID = metadata.ScenarioID;
                if (stateType == StateType.GameState)
                {
                    state.NodeParser.EnqueueGameEvent(new List<GameEventProvider> { GameEventProvider.Create(
                    timestamp,
                    "MATCH_METADATA",
                    () => {
                        state.CurrentGame.BuildNumber = metadata.BuildNumber;
                        state.CurrentGame.GameType = metadata.GameType;
                        state.CurrentGame.FormatType = metadata.FormatType;
                        //state.CurrentGame.ScenarioID = metadata.ScenarioID;
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
                    new Node(null, null, 0, null, data)) });
                }

                return true;
            }
            return false;
        }

        private static bool HandlePlayerName(DateTime timestamp, string data, ParserState state, StateType stateType)
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
                    matchingPlayer.InitialName = Helper.innkeeperNames.Contains(playerName)
                        ? Helper.innkeeperNames[0]
                        : Helper.bobTavernNames.Contains(playerName)
                        ? Helper.bobTavernNames[0]
                        : playerName;
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

        private static bool HandleCreateGame(DateTime timestamp, string data, ParserState state, int indentLevel)
        {
            var match = Regexes.ActionCreategameRegex.Match(data);
            if (match.Success)
            {
                var id = match.Groups[1].Value;
                var gEntity = new GameEntity { Id = int.Parse(id), Tags = new List<Tag>(), TimeStamp = timestamp };
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
                if (state.Node.Type != typeof(Game))
                {
                    // Logger.Log("Current node after end action", state.Node.CreationLogLine);
                    state.UpdateCurrentNode(typeof(Game), typeof(Action));
                    // Logger.Log("Preparing to end action", timestamp);
                    state.EndAction();
                }
                // Logger.Log("Current node after update // " + state.Node.Type + " // " + (state.Node.Type == typeof(Action)), state.Node.CreationLogLine);
                state.Node = state.Node.Parent ?? state.Node;
                // Logger.Log("Current node is now", state.Node.CreationLogLine);
                return true;
            }
            return false;
        }

        private static bool HandleSpectator(DateTime timestamp, string data, ParserState state, StateFacade stateFacade)
        {
            // Only trigger the reset when spectator mode happens for the first time
            if (data.Contains("Begin Spectating") && !data.Contains("2nd"))
            {
                state.Reset(stateFacade);
                state.Spectating = true;
                state.NodeParser.EnqueueGameEvent(new List<GameEventProvider> { GameEventProvider.Create(
                    timestamp,
                    "SPECTATING",
                    () => new GameEvent
                    {
                        Type = "SPECTATING",
                        Value = new
                        {
                            LocalPlayer = stateFacade.LocalPlayer,
                            OpponentPlayer = stateFacade.OpponentPlayer,
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
                if (stateFacade?.LocalPlayer == null)
                {
                    return false;
                }

                var replayCopy = state.Replay;
                var xmlReplay = new ReplayConverter().xmlFromReplay(replayCopy);
                var gameStateReport = state.GameState.BuildGameStateReport(stateFacade);
                state.Spectating = false;
                state.NodeParser.EnqueueGameEvent(new List<GameEventProvider> { GameEventProvider.Create(
                    timestamp,
                    "SPECTATING",
                    () => new GameEvent
                    {
                        Type = "SPECTATING",
                        Value = new
                        {
                            LocalPlayer = stateFacade.LocalPlayer,
                            OpponentPlayer = stateFacade.OpponentPlayer,
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

        private bool HandleNewGame(DateTime timestamp, string data, ParserState state, DateTime previousTimestamp, StateType stateType, StateFacade gameInfoHelper, long currentGameSeed)
        {
            if (data == "CREATE_GAME")
            {
                state.NodeParser.ClearQueue();

                Logger.Log("Handling create game", "");
                var isReconnecting = stateType == StateType.GameState ? state.IsReconnecting(currentGameSeed) : gameInfoHelper.GsState.ReconnectionOngoing;
                if (isReconnecting)
                {
                    if (stateType == StateType.GameState)
                    {
                        Logger.Log(
                            $"Probable reconnect detected {stateType} {timestamp} // {previousTimestamp} // {state.Ended} // {state.NumberOfCreates} // {state.Spectating} // {stateType} // {data}",
                            "" + (timestamp - previousTimestamp));
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
                            new Node(null, null, 0, null, data))
                        });
                    }
                    state.ReconnectionOngoing = true;
                    state.Spectating = false;
                    // Because when reconnecting a BG game (during a zone transition), we don't have the "entities removed" events
                    // so we have no idea if the entities that were previously on board are still there. However, because of how
                    // BG works, all minions (along with their enchantments) are removed and recreated; so we can use the latest
                    // state without fear of losing anything
                    if (state.IsBattlegrounds())
                    {
                        var minionIds = state.GameState.CurrentEntities.Values
                            .Where(e => e.GetCardType() == (int)CardType.MINION)
                            .Select(e => e.Id)
                            .ToList();
                        foreach (var minionId in minionIds)
                        {
                            state.GameState.CurrentEntities.Remove(minionId);
                        }
                    }
                    state.UpdateCurrentNode(typeof(Game));
                    // Don't reset anything
                    return true;
                }

                this.metadata = stateType == StateType.GameState
                    ? new GameMetaData()
                    {
                        BuildNumber = -1,
                        FormatType = -1,
                        GameType = -1,
                        ScenarioID = -1,
                    }
                    : gameInfoHelper.GetMetaData();

                state.Reset(gameInfoHelper);
                state.NumberOfCreates++;
                state.CurrentGame = new Game
                {
                    TimeStamp = timestamp,
                    BuildNumber = this.metadata.BuildNumber,
                    ScenarioID = this.metadata.ScenarioID,
                    FormatType = this.metadata.FormatType,
                    GameType = this.metadata.GameType
                };
                state.Replay.Games.Add(state.CurrentGame);
                var newNode = new Node(typeof(Game), state.CurrentGame, 0, null, data);
                state.CreateNewNode(newNode);
                state.Node = newNode;
                Logger.Log("Created a new game", stateType + " " + timestamp + "," + previousTimestamp);
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
                    helper.ParseEntity(rawEntity);
                }
                catch
                {
                    var currentPlayer =
                        (PlayerEntity)state.CurrentGame.Data.Single(x => (x is PlayerEntity) && ((PlayerEntity)x).Id == state.CurrentPlayerId);
                    currentPlayer.Name = rawEntity;
                    currentPlayer.InitialName = Helper.innkeeperNames.Contains(rawEntity)
                        ? Helper.innkeeperNames[0]
                        : Helper.bobTavernNames.Contains(rawEntity)
                        ? Helper.bobTavernNames[0]
                        : rawEntity;
                }
            }
            else if (tag.Value == 1)
            {
                try
                {
                    helper.ParseEntity(rawEntity);
                }
                catch
                {
                    var currentPlayer =
                        (PlayerEntity)state.CurrentGame.Data.Single(x => (x is PlayerEntity) && ((PlayerEntity)x).Id != state.CurrentPlayerId);
                    currentPlayer.Name = rawEntity;
                    currentPlayer.InitialName = Helper.innkeeperNames.Contains(rawEntity)
                        ? Helper.innkeeperNames[0]
                        : Helper.bobTavernNames.Contains(rawEntity)
                        ? Helper.bobTavernNames[0]
                        : rawEntity;
                }
                state.CurrentPlayerId = helper.ParseEntity(rawEntity);
            }
        }
    }
}