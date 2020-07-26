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
using System.Threading.Tasks;

#endregion

namespace HearthstoneReplays.Parser.Handlers
{
    public class PowerDataHandler
    {

        private Helper helper = new Helper();

        public void Handle(DateTime timestamp, string data, ParserState state)
        {
            var trimmed = data.Trim();
            var indentLevel = data.Length - trimmed.Length;
            data = trimmed;

            if (data == "BLOCK_END")
            {
                // Logger.Log("Current node after end action", state.Node.CreationLogLine);
                state.UpdateCurrentNode(typeof(Game), typeof(Action));
                // Logger.Log("Preparing to end action", timestamp);
                state.EndAction();
                // Logger.Log("Current node after update // " + state.Node.Type + " // " + (state.Node.Type == typeof(Action)), state.Node.CreationLogLine);
                state.Node = state.Node.Parent ?? state.Node;
                // Logger.Log("Current node is now", state.Node.CreationLogLine);
                return;
            }

            var match = Regexes.ActionCreategameRegex.Match(data);
            if (match.Success)
            {
                if (state.ReconnectionOngoing)
                {
                    return;
                }
                var id = match.Groups[1].Value;
                //Debug.Assert(id == "1");
                var gEntity = new GameEntity { Id = int.Parse(id), Tags = new List<Tag>() };
                state.CurrentGame.AddData(gEntity);
                var newNode = new Node(typeof(GameEntity), gEntity, indentLevel, state.Node, data);
                state.CreateNewNode(newNode);
                state.Node = newNode;
                return;
            }


            match = Regexes.ActionCreategamePlayerRegex.Match(data);
            if (match.Success)
            {
                //Logger.Log("Handling PlayerEntity", "");
                if (state.ReconnectionOngoing)
                {
                    //Logger.Log("Reconnection ongoing", "returning");
                    return;
                }
                var id = match.Groups[1].Value;
                var playerId = match.Groups[2].Value;
                var accountHi = match.Groups[3].Value;
                var accountLo = match.Groups[4].Value;
                //Logger.Log("Handling PlayerEntity with id", id);
                //var playerNameAssigment = state.GetPlayerAssignment(int.Parse(playerId));
                //var playerNameMatch = Regexes.PlayerNameAssignment.Match(playerNameAssigment);
                //var playerName = playerNameMatch.Groups[2].Value;
                var pEntity = new PlayerEntity
                {
                    Id = int.Parse(id),
                    AccountHi = accountHi,
                    AccountLo = accountLo,
                    PlayerId = int.Parse(playerId),
                    //Name = playerName,
                    //InitialName = playerName,
                    Tags = new List<Tag>()
                };
                state.UpdateCurrentNode(typeof(Game));
                state.CurrentGame.AddData(pEntity);
                //TryAssignLocalPlayer(state, timestamp, playerNameAssigment);

                var newNode = new Node(typeof(PlayerEntity), pEntity, indentLevel, state.Node, data);
                state.CreateNewNode(newNode);
                state.Node = newNode;
                //Logger.Log("Updated current node", state.Node.CreationLogLine);
                return;
            }

            match = Regexes.ActionStartRegex.Match(data);
            if (match.Success)
            {
                state.ReconnectionOngoing = false;
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
                return;
            }


            match = Regexes.ActionStartRegex_Short.Match(data);
            if (match.Success)
            {
                state.ReconnectionOngoing = false;
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
                return;
            }

            match = Regexes.ActionStartRegex_8_4.Match(data);
            if (match.Success)
            {
                state.ReconnectionOngoing = false;
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
                return;
            }

            match = Regexes.ActionMetadataRegex.Match(data);
            if (match.Success)
            {
                state.ReconnectionOngoing = false;
                var rawMeta = match.Groups[1].Value;
                var rawData = match.Groups[2].Value;
                var info = match.Groups[3].Value;
                var parsedData = helper.ParseEntity(rawData, state);
                var meta = helper.ParseEnum<MetaDataType>(rawMeta);
                var metaData = new MetaData { Data = parsedData, Info = int.Parse(info), Meta = meta, MetaInfo = new List<Info>() };
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
                return;
            }

            match = Regexes.ActionMetaDataInfoRegex.Match(data);
            if (match.Success)
            {
                state.ReconnectionOngoing = false;
                var index = match.Groups[1].Value;
                var rawEntity = match.Groups[2].Value;
                var entity = helper.ParseEntity(rawEntity, state);
                var metaInfo = new Info { Id = entity, Index = int.Parse(index), Entity = entity, TimeStamp = timestamp };
                if (state.Node.Type == typeof(MetaData))
                    ((MetaData)state.Node.Object).MetaInfo.Add(metaInfo);
                else
                    throw new Exception("Invalid node " + state.Node.Type + " while parsing " + data);
                state.CreateNewNode(new Node(typeof(Info), metaInfo, indentLevel, state.Node, data));
                return;
            }

            match = Regexes.ActionShowEntityRegex.Match(data);
            if (match.Success)
            {
                state.ReconnectionOngoing = false;
                var rawEntity = match.Groups[1].Value;
                var cardId = match.Groups[2].Value;
                var entity = helper.ParseEntity(rawEntity, state);

                var showEntity = new ShowEntity { CardId = cardId, Entity = entity, Tags = new List<Tag>(), TimeStamp = timestamp };
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
                //state.GameState.ShowEntity(showEntity);
                return;
            }

            match = Regexes.ActionChangeEntityRegex.Match(data);
            if (match.Success)
            {
                state.ReconnectionOngoing = false;
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
                return;
            }

            match = Regexes.ActionHideEntityRegex.Match(data);
            if (match.Success)
            {
                state.ReconnectionOngoing = false;
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
                return;
            }

            match = Regexes.ActionFullEntityUpdatingRegex.Match(data);
            bool updating = true;
            if (!match.Success)
            {
                match = Regexes.ActionFullEntityCreatingRegex.Match(data);
                updating = false;
            }
            if (match.Success)
            {
                var rawEntity = match.Groups[1].Value;
                var cardId = match.Groups[2].Value;
                var entity = helper.ParseEntity(rawEntity, state);
                //Console.WriteLine("updating entityname " + rawEntity + " for full log " + timestamp + " " + data);
                state.GameState.UpdateEntityName(rawEntity);

                var showEntity = new FullEntity { CardId = cardId, Id = entity, Tags = new List<Tag>(), TimeStamp = timestamp };
                state.UpdateCurrentNode(typeof(Game), typeof(Action));

                var newNode = new Node(typeof(FullEntity), showEntity, indentLevel, state.Node, data);
                if (!state.ReconnectionOngoing)
                {
                    if (state.Node.Type == typeof(Game))
                        ((Game)state.Node.Object).AddData(showEntity);
                    else if (state.Node.Type == typeof(Action))
                        ((Action)state.Node.Object).Data.Add(showEntity);
                    else
                        throw new Exception("Invalid node " + state.Node.Type);
                    state.CreateNewNode(newNode);
                }
                // Logger.Log("Creating full entity", newNode.CreationLogLine);
                state.Node = newNode;
                //state.GameState.FullEntity(showEntity, updating, timestamp + " " + data);
                return;
            }

            match = Regexes.ActionTagChangeRegex.Match(data);
            if (match.Success)
            {
                state.ReconnectionOngoing = false;
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
                    return;
                }
                state.GameState.UpdateEntityName(rawEntity);


                // Since we now use the PowerTaskList, the first PLAYSTATE=PLAYING tag change happens
                // before the FIRST_PLAYER tag is set. We don't rely on that tag for now, so we 
                // ignore it
                if (state.FirstPlayerEntityId == -1)
                {
                    return;
                }

                int entity = -1;
                try
                {
                    entity = helper.ParseEntity(rawEntity, state);
                }
                catch (Exception e)
                {
                    if (state.FirstPlayerEntityId != -1)
                    {
                        Logger.Log("ERROR Exception while parsing entity", rawEntity);
                    }
                }

                if (tag.Name == (int)GameTag.FIRST_PLAYER)
                {
                    state.FirstPlayerEntityId = entity;
                }

                if (tag.Name == (int)GameTag.CURRENT_PLAYER)
                {
                    UpdateCurrentPlayer(state, rawEntity, tag);
                    if (state.FirstPlayerEntityId == -1)
                    {
                        state.FirstPlayerEntityId = entity;
                    }
                }
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
                    DefChange = defChange
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
                return;
            }

            match = Regexes.ActionTagRegex.Match(data);
            if (match.Success)
            {
                // This is not supported yet
                if (data.Contains("CACHED_TAG_FOR_DORMANT_CHANGE"))
                {
                    return;
                }
                // Handled with GameState logs
                if (state.Node.Type == typeof(Game))
                {
                    return;
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
                    return;
                }

                if (tag.Name == (int)GameTag.CURRENT_PLAYER)
                    state.FirstPlayerEntityId = ((PlayerEntity)state.Node.Object).Id;

                if (state.Node.Type == typeof(GameEntity))
                    ((GameEntity)state.Node.Object).Tags.Add(tag);
                else if (state.Node.Type == typeof(PlayerEntity))
                    ((PlayerEntity)state.Node.Object).Tags.Add(tag);
                else if (state.Node.Type == typeof(FullEntity))
                {
                    ((FullEntity)state.Node.Object).Tags.Add(tag);
                    //state.GameState.Tag(tag, ((FullEntity)state.Node.Object).Id);
                }
                else if (state.Node.Type == typeof(ShowEntity))
                {
                    ((ShowEntity)state.Node.Object).Tags.Add(tag);
                    //state.GameState.Tag(tag, ((ShowEntity)state.Node.Object).Entity);
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
                return;
            }

            //var match = Regexes.ActionStartRegex.Match(data);
            //if (match.Success)
            //{
            //             //Console.WriteLine("Updating entity name from power data: " + data);
            //	var rawEntity = match.Groups[2].Value;
            //             state.GameState.UpdateEntityName(rawEntity);
            //             return;
            //}

            //match = Regexes.ActionFullEntityUpdatingRegex.Match(data);
            //if(!match.Success)
            //         {
            //	match = Regexes.ActionFullEntityCreatingRegex.Match(data);
            //         }
            //if(match.Success)
            //{
            //	var rawEntity = match.Groups[1].Value;
            //             //Console.WriteLine("powerdata updating entityname " + rawEntity + " for full log " + timestamp + " " + data);
            //             state.GameState.UpdateEntityName(rawEntity);
            //	return;
            //}

            //match = Regexes.ActionTagChangeRegex.Match(data);
            //if(match.Success)
            //{
            //	var rawEntity = match.Groups[1].Value;
            //             state.GameState.UpdateEntityName(rawEntity);
            //	return;
            //}
        }

        private void TryAssignLocalPlayer(ParserState state, DateTime timestamp, string playerNameAssigment)
        {
            state.TryAssignLocalPlayer(timestamp, playerNameAssigment);
            //while (!state.TryAssignLocalPlayer(timestamp, playerNameAssigment))
            //{
            //    await Task.Delay(5);
            //}
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