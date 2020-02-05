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

        public static Func<GameEvent> CreateProvider(
            string type, 
            string cardId, 
            int controllerId, 
            int entityId, 
            ParserState parserState, 
            GameState fullGameState, 
            object gameState, 
            object additionalProps = null)
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
                    // We do it now so that the zone positions should have been resolved, while 
                    // if we compute it when the event is built, there is no guarantee of that
                    // BUT if we compute it now, we have no guarantee that the state matches what 
                    // the state looked like when we built the event, so building it beforehand
                    // is the way to go
                    GameState = gameState, //fullGameState.BuildGameStateReport(),// gameState,
                    AdditionalProps = additionalProps
                }
            };
        }

        // It needs to be built beforehand, as the game state we pass is not immutable
        public static dynamic BuildGameState(ParserState parserState, GameState gameState)
        {
            if (parserState == null || parserState.LocalPlayer == null || parserState.OpponentPlayer == null)
            {
                //Logger.Log("Can't build game state", "");
                return new { };
            }
            
            var result = new
            {
                ActivePlayerId = gameState.GetActivePlayerId(),
                Player = new
                {
                    Hand = GameEvent.BuildZone(gameState, Zone.HAND, parserState.LocalPlayer.PlayerId),
                    Board = GameEvent.BuildBoard(gameState, parserState.LocalPlayer.PlayerId),
                    Deck = GameEvent.BuildZone(gameState, Zone.DECK, parserState.LocalPlayer.PlayerId),
                },
                Opponent = new
                {
                    Hand = GameEvent.BuildZone(gameState, Zone.HAND, parserState.OpponentPlayer.PlayerId),
                    Board = GameEvent.BuildBoard(gameState, parserState.OpponentPlayer.PlayerId),
                    Deck = GameEvent.BuildZone(gameState, Zone.DECK, parserState.OpponentPlayer.PlayerId),
                }
            };
            return result;
        }

        private static List<object> BuildZone(GameState gameState, Zone zone, int playerId)
        {
            try
            {
                return gameState.CurrentEntities.Values
                    .Where(entity => entity.GetTag(GameTag.ZONE) == (int)zone)
                    .Where(entity => entity.GetTag(GameTag.CONTROLLER) == playerId)
                    .OrderBy(entity => entity.GetTag(GameTag.ZONE_POSITION))
                    .Select(entity => BuildSmallEntity(entity))
                    .ToList();
            }
            catch (Exception e)
            {
                Logger.Log("Warning: issue when trying to build zone " + e.Message, e.StackTrace);
                return BuildZone(gameState, zone, playerId);
            }
        }

        private static List<object> BuildBoard(GameState gameState, int playerId)
        {
            try
            {
                return gameState.CurrentEntities.Values
                    .Where(entity => entity.GetTag(GameTag.ZONE) == (int)Zone.PLAY)
                    .Where(entity => entity.GetTag(GameTag.CONTROLLER) == playerId)
                    .Where(entity => entity.GetTag(GameTag.CARDTYPE) == (int)CardType.MINION)
                    .OrderBy(entity => entity.GetTag(GameTag.ZONE_POSITION))
                    .Select(entity => BuildSmallEntity(entity))
                    .ToList();
            }
            catch (Exception e)
            {
                Logger.Log("Warning: issue when trying to build zone " + e.Message, e.StackTrace);
                return BuildBoard(gameState, playerId);
            }
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
                attack = entity.GetTag(GameTag.ATK),
                health = entity.GetTag(GameTag.HEALTH),
            };
        }
	}
}
