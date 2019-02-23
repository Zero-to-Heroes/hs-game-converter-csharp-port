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

		public Dictionary<int, FullEntity> CurrentEntities = new Dictionary<int, FullEntity>();
        public Dictionary<string, int> EntityNames = new Dictionary<string, int>();

		public void Reset(ParserState state)
		{
			CurrentEntities = new Dictionary<int, FullEntity>();
			CurrentEntities.Add(1, new FullEntity { Id = 1, Tags = new List<Tag>() });
			ParserState = state;
		}

        public void UpdateEntityName(string rawEntity)
        {
            var match = Regexes.EntityWithNameAndId.Match(rawEntity);
            if (match.Success)
            {
                var entityName = match.Groups[1].Value;
                var entityId = match.Groups[2].Value;
                EntityNames[entityName] = int.Parse(entityId);
            }
        }

		public void PlayerEntity(PlayerEntity entity)
		{
			if (CurrentEntities.ContainsKey(entity.Id))
			{
				Logger.Log("error while parsing, playerEntity already present in memory", "" + entity.Id);
				return;
			}
			var fullEntity = new FullEntity { Id = entity.Id, Tags = new List<Tag>(), TimeStamp = entity.TimeStamp };
			CurrentEntities.Add(entity.Id, fullEntity);
		}

		public void FullEntity(FullEntity entity, bool updating, string initialLog = null)
		{
			if (updating || CurrentEntities.ContainsKey(entity.Id))
			{
				// This actually happens in a normal scenario, so we just ignore it
				return;
			}
			var fullEntity = new FullEntity { CardId = entity.CardId, Id = entity.Id, Tags = new List<Tag>(), TimeStamp = entity.TimeStamp };
			CurrentEntities.Add(entity.Id, fullEntity);
		}

		public void ShowEntity(ShowEntity entity)
		{
			if (!CurrentEntities.ContainsKey(entity.Entity))
			{
				Logger.Log("error while parsing, showentity doesn't have an entity in memory yet", "" + entity.Entity);
				return;
			}
			CurrentEntities[entity.Entity].CardId = entity.CardId;
		}

		public void TagChange(TagChange tagChange, string defChange, string initialLog = null)
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
			RaiseTagChangeEvents(tagChange, existingTag.Value, defChange, initialLog);
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

        public int PlayerIdFromEntityName(string data)
        {
            //Logger.Log("Getting player Id from EntityName", data);
            int entityId;
            EntityNames.TryGetValue(data, out entityId);
            if (entityId != 0)
            {
                //Logger.Log("Found matching entity id", entityId);
                // Now find the player this entity is attached to
                int playerId = CurrentEntities.Values
                    .Where(e => e.Tags.Find(x => (x.Name == (int)GameTag.HERO_ENTITY && x.Value == entityId)) != null)
                    .Select(e => e.Id)
                    .FirstOrDefault();
                //Logger.Log("Found matching player entity id", playerId);
                return playerId;
            }
            return 0;
        }

        private async void RaiseTagChangeEvents(TagChange tagChange, int previousValue, string defChange, string initialLog = null)
		{
			// Wait until we have all the necessary data
			while (ParserState.CurrentGame.FormatType == 0 || ParserState.CurrentGame.GameType == 0 || ParserState.LocalPlayer == null)
			{
				await Task.Delay(100);
			}

			if (tagChange.Name == (int)GameTag.GOLD_REWARD_STATE)
			{
				ParserState.EndCurrentGame();
			}

            // We detect a given stage of the run in Rumble Run
            //if (ParserState.CurrentGame.ScenarioID == (int)Scenario.RUMBLE_RUN
            //    && tagChange.Name == (int)GameTag.HEALTH
            //    && defChange != null && defChange.Trim().Length > 0)
            //{
            //    var heroEntityId = ParserState.GetTag(
            //        ParserState.GetEntity(ParserState.LocalPlayer.Id).Tags,
            //        GameTag.HERO_ENTITY);
            //    if (tagChange.Entity == heroEntityId)
            //    {
            //        // The player starts with 20 Health, and gains an additional 5 Health per defeated boss, 
            //        // up to 45 Health for the eighth, and final boss.
            //        int runStep = 1 + (tagChange.Value - 20) / 5;
            //        //Logger.Log("rumble step", tagChange.Value + " " + runStep + " " + initialLog);
            //        //Logger.Log("defchange", defChange);
            //        GameEventHandler.Handle(new GameEvent
            //        {
            //            Type = "RUMBLE_RUN_STEP",
            //            Value = runStep
            //        });
            //    }
            //}

   //         if (ParserState.CurrentGame.ScenarioID == (int)Scenario.DUNGEON_RUN
			//	&& tagChange.Name == (int)GameTag.HEALTH
			//	&& defChange != null && defChange.Trim().Length > 0)
			//{
			//	var runStep = 1 + (tagChange.Value - 15) / 5;
			//	var heroEntityId = ParserState.GetTag(
			//		ParserState.GetEntity(ParserState.LocalPlayer.Id).Tags,
			//		GameTag.HERO_ENTITY);
			//	if (tagChange.Entity == heroEntityId)
			//	{
			//		GameEventHandler.Handle(new GameEvent
			//		{
			//			Type = "DUNGEON_RUN_STEP",
			//			Value = runStep
			//		});
			//	}
			//}

			//if (ParserState.CurrentGame.ScenarioID == (int)Scenario.MONSTER_HUNT
			//	&& tagChange.Name == (int)GameTag.HEALTH
			//	&& defChange != null && defChange.Trim().Length > 0)
			//{
			//	var runStep = 1 + (tagChange.Value - 10) / 5;
			//	var heroEntityId = ParserState.GetTag(
			//		ParserState.GetEntity(ParserState.LocalPlayer.Id).Tags,
			//		GameTag.HERO_ENTITY);
			//	if (tagChange.Entity == heroEntityId)
			//	{
			//		GameEventHandler.Handle(new GameEvent
			//		{
			//			Type = "MONSTER_HUNT_STEP",
			//			Value = runStep
			//		});
			//	}
			//}

			if (tagChange.Name == (int)GameTag.ZONE && tagChange.Value == (int)Zone.PLAY)
			{
				var entity = CurrentEntities[tagChange.Entity];
                //Logger.Log("entering play", entity.Id + " " + previousValue + " " + ParserState.GetTag(entity.Tags, GameTag.DUNGEON_PASSIVE_BUFF));
				if (previousValue == (int)Zone.HAND)
				{
					//GameEventHandler.Handle(new GameEvent
					//{
					//	Type = "CARD_PLAYED",
					//	Value = new
					//	{
					//		CardId = entity.CardId,
					//		ControllerId = ParserState.GetTag(entity.Tags, GameTag.CONTROLLER),
					//		LocalPlayer = ParserState.LocalPlayer,
					//		OpponentPlayer = ParserState.OpponentPlayer
					//	}
					//});
				}
				//else if (ParserState.GetTag(entity.Tags, GameTag.DUNGEON_PASSIVE_BUFF) == 1)
				//{
				//	GameEventHandler.Handle(new GameEvent
				//	{
				//		Type = "PASSIVE_BUFF",
				//		Value = new
				//		{
				//			CardId = entity.CardId,
				//			ControllerId = ParserState.GetTag(entity.Tags, GameTag.CONTROLLER),
				//			LocalPlayer = ParserState.LocalPlayer,
				//			OpponentPlayer = ParserState.OpponentPlayer
				//		}
				//	});
				//}
			}
		}
	}
}