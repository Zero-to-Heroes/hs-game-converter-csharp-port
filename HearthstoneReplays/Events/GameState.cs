#region
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.Entities;
using HearthstoneReplays.Parser.ReplayData.GameActions;
#endregion

namespace HearthstoneReplays.Parser.ReplayData.Entities
{
	public class GameState
	{
		public ParserState ParserState;

		private Dictionary<int, FullEntity> CurrentEntities = new Dictionary<int, FullEntity>();

		public void Reset(ParserState state)
		{
			CurrentEntities = new Dictionary<int, FullEntity>();
			ParserState = state;
		}

		public void ShowEntity(ShowEntity entity)
		{
			if (!CurrentEntities.ContainsKey(entity.Entity)) {
				Logger.Log("error while parsing, showentity doesn't have an entity in memory yet", "" + entity.Entity);
				return;
			}
			CurrentEntities[entity.Entity].CardId = entity.CardId;
		}

		public void PlayerEntity(PlayerEntity entity)
		{
			if (CurrentEntities.ContainsKey(entity.Id))
			{
				Logger.Log("error while parsing, fullentity already present in memory", "" + entity.Id);
				return;
			}
			var fullEntity = new FullEntity { Id = entity.Id, Tags = new List<Tag>(), TimeStamp = entity.TimeStamp };
			CurrentEntities.Add(entity.Id, fullEntity);
		}

		public void FullEntity(FullEntity entity)
		{
			if (CurrentEntities.ContainsKey(entity.Id))
			{
				Logger.Log("error while parsing, fullentity already present in memory", "" + entity.Id);
				return;
			}
			var fullEntity = new FullEntity { CardId = entity.CardId, Id = entity.Id, Tags = new List<Tag>(), TimeStamp = entity.TimeStamp };
			CurrentEntities.Add(entity.Id, fullEntity);
		}

		public void TagChange(TagChange tagChange, string defChange)
		{
			if (!CurrentEntities.ContainsKey(tagChange.Entity))
			{
				//Logger.Log("error while parsing, tagchange doesn't have an entity in memory yet", "" + tagChange.Entity);
				return;
			}
			var existingTag = CurrentEntities[tagChange.Entity].Tags.Find((tag) => tag.Name == tagChange.Name);
			if (existingTag == null)
			{
				existingTag = new Tag { Name = tagChange.Name };
				CurrentEntities[tagChange.Entity].Tags.Add(existingTag);
			}
			RaiseTagChangeEvents(tagChange, existingTag.Value, defChange);
			existingTag.Value = tagChange.Value;
		}

		public void Tag(Tag tag, int entityId)
		{
			if (!CurrentEntities.ContainsKey(entityId))
			{
				Logger.Log("error while parsing, tagchange doesn't have an entity in memory yet", "" + entityId);
				return;
			}
			var existingTag = CurrentEntities[entityId].Tags.Find((t) => tag.Name == t.Name);
			if (existingTag == null)
			{
				existingTag = new Tag { Name = tag.Name };
				CurrentEntities[entityId].Tags.Add(existingTag);
			}
			existingTag.Value = tag.Value;
		}

		private async void RaiseTagChangeEvents(TagChange tagChange, int previousValue, string defChange)
		{
			// Wait until we have all the necessary data
			while (ParserState.CurrentGame.FormatType == 0 || ParserState.CurrentGame.GameType == 0 || ParserState.LocalPlayer == null)
			{
				await Task.Delay(100);
			}

			if (tagChange.Name == (int)GameTag.PLAYSTATE)
			{
				Logger.Log("Found a PLAYSTATE change", tagChange.Entity);
				if (tagChange.Value == (int)PlayState.WON)
				{
					Logger.Log("Found a winner", tagChange.Entity);
					var winner = (PlayerEntity)ParserState.GetEntity(tagChange.Entity);
					Logger.Log("Winner is", "" + winner.Id);
					GameEventHandler.Handle(new GameEvent
					{
						Type = "WINNER",
						Value = new
						{
							Winner = winner,
							LocalPlayer = ParserState.LocalPlayer,
							OpponentPlayer = ParserState.OpponentPlayer
						}
					});
				}
				else if (tagChange.Value == (int)PlayState.TIED)
				{
					GameEventHandler.Handle(new GameEvent
					{
						Type = "TIE"
					});
				}
			}

			if (tagChange.Name == (int)GameTag.GOLD_REWARD_STATE)
			{
				var xmlReplay = new ReplayConverter().xmlFromReplay(ParserState.Replay);
				GameEventHandler.Handle(new GameEvent
				{
					Type = "GAME_END",
					Value = new
					{
						Game = ParserState.CurrentGame,
						ReplayXml = xmlReplay
					}
				});
				ParserState.EndCurrentGame();
			}

			if (tagChange.Name == (int)GameTag.MULLIGAN_STATE && tagChange.Value == (int)Mulligan.INPUT)
			{
				GameEventHandler.Handle(new GameEvent
				{
					Type = "MULLIGAN_INPUT"
				});
			}

			if (tagChange.Name == (int)GameTag.MULLIGAN_STATE && tagChange.Value == (int)Mulligan.DONE)
			{
				GameEventHandler.Handle(new GameEvent
				{
					Type = "MULLIGAN_DONE"
				});
			}

			if (tagChange.Name == (int)GameTag.TURN)
			{
				GameEventHandler.Handle(new GameEvent
				{
					Type = "TURN_START",
					Value = (int)tagChange.Value
				});
			}

			// We detect a given stage of the run in Rumble Run
			if (ParserState.CurrentGame.ScenarioID == (int)Scenario.RUMBLE_RUN 
				&& tagChange.Name == (int)GameTag.HEALTH 
				&& defChange != null)
			{
				var heroEntityId = ParserState.GetTag(
					ParserState.GetEntity(ParserState.LocalPlayer.Id).Tags, 
					GameTag.HERO_ENTITY);
				if (tagChange.Entity == heroEntityId)
				{
					// The player starts with 20 Health, and gains an additional 5 Health per defeated boss, 
					// up to 45 Health for the eighth, and final boss.
					int runStep = (tagChange.Value - 20) / 5;
					GameEventHandler.Handle(new GameEvent
					{
						Type = "RUMBLE_RUN_STEP",
						Value = runStep
					});
				}
			}

			if (tagChange.Name == (int)GameTag.ZONE && tagChange.Value == (int)Zone.PLAY && previousValue == (int)Zone.HAND)
			{
				var entity = CurrentEntities[tagChange.Entity];
				GameEventHandler.Handle(new GameEvent
				{
					Type = "CARD_PLAYED",
					Value = new
					{
						CardId = entity.CardId,
						ControllerId = ParserState.GetTag(entity.Tags, GameTag.CONTROLLER),
						LocalPlayer = ParserState.LocalPlayer,
						OpponentPlayer = ParserState.OpponentPlayer
					}
				});
			}
		}
	}
}