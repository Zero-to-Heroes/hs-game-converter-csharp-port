using HearthstoneReplays.Parser;
using HearthstoneReplays.Parser.ReplayData.Meta;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HearthstoneReplays
{
	public class GameEvent
	{
		public string Type { get; set; }
		public Object Value { get; set; }

		public override string ToString() {
			return "GameEvent: " + Type + " (" + Value + ")";
		}

        public static Func<GameEvent> CreateProvider(string type, string cardId, int controllerId, int entityId, ParserState parserState, object gameState, object additionalProps = null)
        {
            return () => new GameEvent
            {
                Type = type,
                Value = new
                {
                    CardId = cardId,
                    ControllerId = controllerId,
                    LocalPlayer = parserState.LocalPlayer,
                    OpponentPlayer = parserState.OpponentPlayer,
                    EntityId = entityId,
                    GameState = gameState,
                    AdditionalProps = additionalProps
                }
            };
        }

        // It needs to be built beforehand, as the game state we pass is not immutable
        public static object BuildGameState(ParserState parserState, GameState gameState)
        {
            if (parserState == null ||parserState.LocalPlayer == null || parserState.OpponentPlayer == null)
            {
                return new { };
            }
            var result = new
            {
                Player = new
                {
                    Hand = GameEvent.BuildZone(gameState, Zone.HAND, parserState.LocalPlayer.PlayerId),
                    Board = GameEvent.BuildZone(gameState, Zone.PLAY, parserState.LocalPlayer.PlayerId),
                },
                Opponent = new
                {
                    Hand = GameEvent.BuildZone(gameState, Zone.HAND, parserState.OpponentPlayer.PlayerId),
                    Board = GameEvent.BuildZone(gameState, Zone.PLAY, parserState.OpponentPlayer.PlayerId),
                }
            };
            return result;
        }

        private static List<object> BuildZone(GameState gameState, Zone zone, int playerId)
        {
            return gameState.CurrentEntities.Values
                    .Where(entity => entity.GetTag(GameTag.ZONE) == (int)zone)
                    .Where(entity => entity.GetTag(GameTag.CONTROLLER) == playerId)
                    .OrderBy(entity => entity.GetTag(GameTag.ZONE_POSITION))
                    .Select(entity => BuildSmallEntity(entity))
                    .ToList();
        }

        private static object BuildSmallEntity(BaseEntity entity)
        {
            string cardId = null;
            if (entity.GetType() == typeof(FullEntity))
            {
                cardId = (entity as FullEntity).CardId;
            }
            return new
            {
                entityId = entity.Id,
                cardId = cardId,
            };
        }
	}
}
