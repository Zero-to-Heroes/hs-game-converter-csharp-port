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

#endregion

namespace HearthstoneReplays.Parser.Handlers
{
	public class DataHandler
	{
		public int previousTimestampHours;

		private Helper helper = new Helper();

		public void Handle(string timestamp, string data, ParserState state, int previousTimestampTotalSeconds)
        {
            timestamp = NormalizeTimestamp(timestamp);
            
			var trimmed = data.Trim();
			var indentLevel = data.Length - trimmed.Length;
			data = trimmed;

			if (data == "CREATE_GAME")
            {
                var totalSeconds = BuildTotalSeconds(timestamp);
                if (!state.Ended && state.NumberOfCreates >= 1)
                {
                    Logger.Log("Probable reconnect detected", "" + (totalSeconds - previousTimestampTotalSeconds));
                    state.ReconnectionOngoing = true;
                    state.NumberOfCreates++;
                    state.UpdateCurrentNode(typeof(Game));
                    return;
                }
                state.Reset();
                state.NumberOfCreates++;
				state.CurrentGame = new Game { Data = new List<GameData>(), TimeStamp = timestamp };
				state.Replay.Games.Add(state.CurrentGame);
                var newNode = new Node(typeof(Game), state.CurrentGame, 0, null, data);
                state.CreateNewNode(newNode);
				state.Node = newNode;
                Logger.Log("Created a new game", "" + totalSeconds + "," + previousTimestampTotalSeconds);
				return;
			}

			if (state.Ended)
			{
				return;
			}
            // The app was launched in the middle of a game, we don't support this now but at least don't crash
            if (state.NumberOfCreates == 0)
            {
                // Just selectively do some sampling to avoid logging everything, but we 
                // still want to have a trace that this is what happened
                if (data == "BLOCK_END")
                {
                    Logger.Log("Trying to parse an ongoing game, this is not supported yet", "");
                }
                return;
            }

			if (data == "BLOCK_END")
            {
                state.EndAction();
                state.UpdateCurrentNode(typeof(Game), typeof(Action));
				state.Node = state.Node.Parent ?? state.Node;
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
				Debug.Assert(id == "1");
				var gEntity = new GameEntity { Id = int.Parse(id), Tags = new List<Tag>() };
				state.CurrentGame.AddData(gEntity);
                var newNode = new Node(typeof(GameEntity), gEntity, indentLevel, state.Node, data);
                state.CreateNewNode(newNode);
                state.Node = newNode;
                return;
			}

			match = Regexes.PlayerNameAssignment.Match(data);
			if (match.Success)
            {
                if (state.ReconnectionOngoing)
                {
                    return;
                }
                var playerId = int.Parse(match.Groups[1].Value);
				var playerName = match.Groups[2].Value;
				var matchingPlayer = state.getPlayers()
					.Where(player => player.PlayerId == playerId)
					.First();
				matchingPlayer.Name = playerName;
				state.TryAssignLocalPlayer(timestamp, data);
                Logger.Log("Tried to assign player name", data);
            }

			match = Regexes.BuildNumber.Match(data);
			if (match.Success)
			{
				state.CurrentGame.BuildNumber = int.Parse(match.Groups[1].Value);
			}

			match = Regexes.GameType.Match(data);
			if (match.Success)
			{
				var rawGameType = match.Groups[1].Value;
				var gameType = helper.ParseEnum<GameType>(rawGameType);
				state.CurrentGame.GameType = gameType;
			}

			match = Regexes.FormatType.Match(data);
			if (match.Success)
			{
				var rawFormatType = match.Groups[1].Value;
				var formatType = helper.ParseEnum<FormatType>(rawFormatType);
				state.CurrentGame.FormatType = formatType;
			}

			match = Regexes.ScenarioID.Match(data);
			if (match.Success)
            {
                if (state.ReconnectionOngoing)
                {
                    return;
                }
                state.CurrentGame.ScenarioID = int.Parse(match.Groups[1].Value);
                // This is a very peculiar log info, we don't fit it to the new events archi for now
                var metaData = new
                {
                    BuildNumber = state.CurrentGame.BuildNumber,
                    GameType = state.CurrentGame.GameType,
                    FormatType = state.CurrentGame.FormatType,
                    ScenarioID = state.CurrentGame.ScenarioID,
                };
                state.GameState.MetaData = metaData;
                state.NodeParser.EnqueueGameEvent(new List<GameEventProvider> { GameEventProvider.Create(
                    timestamp,
                    () => new GameEvent
                    {
                        Type = "MATCH_METADATA",
                        Value = metaData
                    },
                    false,
                    data) });
			}

			match = Regexes.ActionCreategamePlayerRegex.Match(data);
			if(match.Success)
			{
                if (state.ReconnectionOngoing)
                {
                    return;
                }
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
                state.Node = newNode;
                return;
			}

			match = Regexes.ActionStartRegex_8_4.Match(data);
			if(match.Success)
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
				if(state.Node.Type == typeof(Game))
					((Game)state.Node.Object).AddData(action);
				else if(state.Node.Type == typeof(Action))
					((Action)state.Node.Object).Data.Add(action);
                else
                    throw new Exception("Invalid node " + state.Node.Type + " while parsing " + data);
                var newNode = new Node(typeof(Action), action, indentLevel, state.Node, data);
                state.CreateNewNode(newNode);
                state.Node = newNode;
                return;
			}

            match = Regexes.ActionMetadataRegex.Match(data);
			if(match.Success)
            {
                state.ReconnectionOngoing = false;
                var rawMeta = match.Groups[1].Value;
				var rawData = match.Groups[2].Value;
				var info = match.Groups[3].Value;
				var parsedData = helper.ParseEntity(rawData, state);
				var meta = helper.ParseEnum<MetaDataType>(rawMeta);
				var metaData = new MetaData {Data = parsedData, Info = int.Parse(info), Meta = meta, MetaInfo = new List<Info>()};
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
			if(match.Success)
            {
                state.ReconnectionOngoing = false;
                var index = match.Groups[1].Value;
				var rawEntity = match.Groups[2].Value;
				var entity = helper.ParseEntity(rawEntity, state);
				var metaInfo = new Info {Id = entity, Index = int.Parse(index), Entity = entity, TimeStamp = timestamp};
				if(state.Node.Type == typeof(MetaData))
					((MetaData)state.Node.Object).MetaInfo.Add(metaInfo);
                else
                    throw new Exception("Invalid node " + state.Node.Type + " while parsing " + data);
                state.CreateNewNode(new Node(typeof(Info), metaInfo, indentLevel, state.Node, data));
                return;
			}

			match = Regexes.ActionShowEntityRegex.Match(data);
			if(match.Success)
            {
                state.ReconnectionOngoing = false;
                var rawEntity = match.Groups[1].Value;
				var cardId = match.Groups[2].Value;
				var entity = helper.ParseEntity(rawEntity, state);

				var showEntity = new ShowEntity {CardId = cardId, Entity = entity, Tags = new List<Tag>(), TimeStamp = timestamp};
				state.UpdateCurrentNode(typeof(Game), typeof(Action));
				if(state.Node.Type == typeof(Game))
					((Game)state.Node.Object).AddData(showEntity);
				else if(state.Node.Type == typeof(Action))
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
			if(match.Success)
            {
                state.ReconnectionOngoing = false;
                var rawEntity = match.Groups[1].Value;
				var tagName = match.Groups[2].Value;
				var value = match.Groups[3].Value;
				var entity = helper.ParseEntity(rawEntity, state);
				var zone = helper.ParseTag(tagName, value);

				var hideEntity = new HideEntity {Entity = entity, Zone = zone.Value, TimeStamp = timestamp};
				state.UpdateCurrentNode(typeof(Game), typeof(Action));

				if (state.Node.Type == typeof(Game))
					((Game)state.Node.Object).AddData(hideEntity);
				else if(state.Node.Type == typeof(Action))
					((Action)state.Node.Object).Data.Add(hideEntity);
				else
					throw new Exception("Invalid node: " + state.Node.Type);
                return;
			}

			match = Regexes.ActionFullEntityUpdatingRegex.Match(data);
            bool updating = true;
			if(!match.Success)
            {
				match = Regexes.ActionFullEntityCreatingRegex.Match(data);
                updating = false;
            }
			if(match.Success)
			{
				var rawEntity = match.Groups[1].Value;
				var cardId = match.Groups[2].Value;
				var entity = helper.ParseEntity(rawEntity, state);
                //Console.WriteLine("updating entityname " + rawEntity + " for full log " + timestamp + " " + data);
                state.GameState.UpdateEntityName(rawEntity);

                var showEntity = new FullEntity {CardId = cardId, Id = entity, Tags = new List<Tag>(), TimeStamp = timestamp};
				state.UpdateCurrentNode(typeof(Game), typeof(Action));

                var newNode = new Node(typeof(FullEntity), showEntity, indentLevel, state.Node, data);
                if (!state.ReconnectionOngoing)
                {
				    if(state.Node.Type == typeof(Game))
					    ((Game)state.Node.Object).AddData(showEntity);
				    else if(state.Node.Type == typeof(Action))
					    ((Action)state.Node.Object).Data.Add(showEntity);
				    else
					    throw new Exception("Invalid node " + state.Node.Type);
                    state.CreateNewNode(newNode);
                }
                state.Node = newNode;
                //state.GameState.FullEntity(showEntity, updating, timestamp + " " + data);
				return;
			}

			match = Regexes.ActionTagChangeRegex.Match(data);
			if(match.Success)
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
                
                var tagChange = new TagChange {
                    Entity = entity,
                    Name = tag.Name,
                    Value = tag.Value,
                    TimeStamp = timestamp,
                    DefChange = defChange
                };
				state.UpdateCurrentNode(typeof(Game), typeof(Action));
                state.CreateNewNode(new Node(typeof(TagChange), tagChange, indentLevel, state.Node, data));

				if(state.Node.Type == typeof(Game))
					((Game)state.Node.Object).AddData(tagChange);
				else if(state.Node.Type == typeof(Action))
					((Action)state.Node.Object).Data.Add(tagChange);
				else
					throw new Exception("Invalid node " + state.Node.Type);
				state.GameState.TagChange(tagChange, defChange, timestamp + " " + data);
				return;
			}

			match = Regexes.ActionTagRegex.Match(data);
			if(match.Success)
			{
                // When in reconnect, we don't parse the GameEntity and 
                // PlayerEntity nodes, so the tags think they are parsed while 
                // under the Game root node
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
					state.FirstPlayerId = ((PlayerEntity)state.Node.Object).Id;

				if(state.Node.Type == typeof(GameEntity))
					((GameEntity)state.Node.Object).Tags.Add(tag);
				else if(state.Node.Type == typeof(PlayerEntity))
					((PlayerEntity)state.Node.Object).Tags.Add(tag);
				else if(state.Node.Type == typeof(FullEntity))
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
					throw new Exception("Invalid node " + state.Node.Type + " -- " + data);
				return;
			}
		}

        private int UpdatePlayerEntity(ParserState state, string rawEntity, Tag tag, int entity)
        {
            int tmp;
            if(!int.TryParse(rawEntity, out tmp) && !rawEntity.StartsWith("[") && rawEntity != "GameEntity")
            {
                if(entity != tag.Value)
                {
                    entity = tag.Value;
                    var tmpName = ((PlayerEntity)state.CurrentGame.Data[1]).Name;
                    ((PlayerEntity)state.CurrentGame.Data[1]).Name = ((PlayerEntity)state.CurrentGame.Data[2]).Name;
                    ((PlayerEntity)state.CurrentGame.Data[2]).Name = tmpName;

                    foreach(var dataObj in ((Game)state.Node.Object).Data)
                    {
                        var tChange = dataObj as TagChange;
                        if(tChange != null)
                            tChange.Entity = tChange.Entity == 2 ? 3 : 2;
                    }
                }
            }

            return entity;
        }

        private void UpdateCurrentPlayer(ParserState state, string rawEntity, Tag tag)
        {
            if(tag.Value == 0)
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
                }
            }
            else if(tag.Value == 1)
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
                }
                state.CurrentPlayerId = helper.ParseEntity(rawEntity, state);
            }
        }

		private String NormalizeTimestamp(String timestamp)
		{
			if (!string.IsNullOrEmpty(timestamp))
			{
				String[] split = timestamp.Split(':');
				int hours = int.Parse(split[0]);
				if (hours < previousTimestampHours)
				{
					hours = previousTimestampHours + 1;
					String newTs = hours + ":" + split[1] + ":" + split[2];
					return newTs;
				}
				previousTimestampHours = hours;
			}
			return timestamp;
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