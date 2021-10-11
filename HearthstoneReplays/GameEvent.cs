using HearthstoneReplays.Parser;
using HearthstoneReplays.Parser.ReplayData.Meta;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using HearthstoneReplays.Parser.ReplayData.Meta.Options;

namespace HearthstoneReplays
{
    public class GameEvent
    {
        public string Type { get; set; }
        public Object Value { get; set; }

        public override string ToString()
        {
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
            object additionalProps = null,
            System.Action preprocess = null)
        {
            return () =>
            {
                if (preprocess != null)
                {
                    preprocess.Invoke();
                }
                return new GameEvent
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
            };
        }

        // It needs to be built beforehand, as the game state we pass is not immutable
        public static dynamic BuildGameState(ParserState parserState, GameState gameState, TagChange tagChange, ShowEntity showEntity)
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
                    Hero = GameEvent.BuildHero(gameState, parserState.Options, parserState.LocalPlayer.PlayerId, tagChange, showEntity),
                    Weapon = GameEvent.BuildWeapon(gameState, parserState.Options, parserState.LocalPlayer.PlayerId, tagChange, showEntity),
                    Hand = GameEvent.BuildZone(gameState, parserState.Options, Zone.HAND, parserState.LocalPlayer.PlayerId, tagChange, showEntity),
                    Board = GameEvent.BuildBoard(gameState, parserState.Options, parserState.LocalPlayer.PlayerId, tagChange, showEntity),
                    Deck = GameEvent.BuildZone(gameState, parserState.Options, Zone.DECK, parserState.LocalPlayer.PlayerId, tagChange, showEntity),
                },
                Opponent = new
                {
                    Hero = GameEvent.BuildHero(gameState, parserState.Options, parserState.OpponentPlayer.PlayerId, tagChange, showEntity),
                    Weapon = GameEvent.BuildWeapon(gameState, parserState.Options, parserState.OpponentPlayer.PlayerId, tagChange, showEntity),
                    Hand = GameEvent.BuildZone(gameState, parserState.Options, Zone.HAND, parserState.OpponentPlayer.PlayerId, tagChange, showEntity),
                    Board = GameEvent.BuildBoard(gameState, parserState.Options, parserState.OpponentPlayer.PlayerId, tagChange, showEntity),
                    Deck = GameEvent.BuildZone(gameState, parserState.Options, Zone.DECK, parserState.OpponentPlayer.PlayerId, tagChange, showEntity),
                }
            };
            return result;
        }

        private static object BuildHero(GameState gameState, Options options, int playerId, TagChange tagChange, ShowEntity showEntity)
        {
            try
            {
                var hero = gameState.CurrentEntities.Values
                    .Where(entity => entity.GetTag(GameTag.ZONE) == (int)Zone.PLAY)
                    .Where(entity => entity.GetTag(GameTag.CARDTYPE) == (int)CardType.HERO)
                    .Where(entity => entity.GetEffectiveController() == playerId)
                    .OrderBy(entity => entity.GetTag(GameTag.ZONE_POSITION))
                    .Select(entity => BuildSmallEntity(entity, options, tagChange, showEntity))
                    .FirstOrDefault();
                return hero != null ? hero : new
                {

                };
            }
            catch (Exception e)
            {
                Logger.Log("Warning: issue when trying to build hero " + e.Message, e.StackTrace);
                return BuildHero(gameState, options, playerId, tagChange, showEntity);
            }
        }

        private static object BuildWeapon(GameState gameState, Options options, int playerId, TagChange tagChange, ShowEntity showEntity)
        {
            try
            {
                var weapon = gameState.CurrentEntities.Values
                    .Where(entity => entity.GetTag(GameTag.ZONE) == (int)Zone.PLAY)
                    .Where(entity => entity.GetTag(GameTag.CARDTYPE) == (int)CardType.WEAPON)
                    .Where(entity => entity.GetEffectiveController() == playerId)
                    .Select(entity => BuildSmallEntity(entity, options, tagChange, showEntity))
                    .FirstOrDefault();
                return weapon != null ? weapon : new
                {

                };
            }
            catch (Exception e)
            {
                Logger.Log("Warning: issue when trying to build weapon " + e.Message, e.StackTrace);
                return BuildWeapon(gameState, options, playerId, tagChange, showEntity);
            }
        }

        private static List<object> BuildZone(GameState gameState, Options options, Zone zone, int playerId, TagChange tagChange, ShowEntity showEntity)
        {
            try
            {
                var entityToConsiderTC = tagChange?.Name == (int)GameTag.ZONE && tagChange?.Value == (int)zone ? tagChange.Entity : -1;
                entityToConsiderTC = entityToConsiderTC != -1 && gameState.CurrentEntities[entityToConsiderTC]?.GetEffectiveController() == playerId
                    ? entityToConsiderTC
                    : -1;
                var entityToConsiderSE = showEntity?.GetTag(GameTag.ZONE) == (int)zone ? showEntity.Entity : -1;
                entityToConsiderSE = entityToConsiderSE != -1 && gameState.CurrentEntities[entityToConsiderSE]?.GetEffectiveController() == playerId
                    ? entityToConsiderSE
                    : -1;
                var entityToExcludeTC = tagChange?.Name == (int)GameTag.ZONE && tagChange?.Value != (int)zone ? tagChange.Entity : -1;
                var entityToExcludeSE = showEntity?.GetTag(GameTag.ZONE) > 0 && showEntity?.GetTag(GameTag.ZONE) != (int)zone ? showEntity.Entity : -1;
                return gameState.CurrentEntities.Values
                    .Where(entity => entityToExcludeSE != entity.Entity
                        && entityToExcludeTC != entity.Entity
                        && (entity.GetTag(GameTag.ZONE) == (int)zone || entity.Entity == entityToConsiderTC || entity.Entity == entityToConsiderSE))
                    .Where(entity => entity.GetEffectiveController() == playerId || entity.Entity == entityToConsiderTC || entity.Entity == entityToConsiderSE)
                    .OrderBy(entity => entity.GetTag(GameTag.ZONE_POSITION) == -1 ? 99 : entity.GetTag(GameTag.ZONE_POSITION))
                    .Select(entity => BuildSmallEntity(entity, options, tagChange, showEntity))
                    .ToList();
            }
            catch (Exception e)
            {
                Logger.Log("Warning: issue when trying to build zone " + e.Message, e.StackTrace);
                return BuildZone(gameState, options, zone, playerId, tagChange, showEntity);
            }
        }

        private static List<object> BuildBoard(GameState gameState, Options options, int playerId, TagChange tagChange, ShowEntity showEntity)
        {
            try
            {
                return gameState.CurrentEntities.Values
                    .Where(entity => (entity.GetTag(GameTag.ZONE) == (int)Zone.PLAY && !RemovedFromPlay(entity, tagChange, showEntity))
                        || PutInPlay(entity, tagChange, showEntity))
                    .Where(entity => entity.GetEffectiveController() == playerId)
                    .Where(entity => entity.GetTag(GameTag.CARDTYPE) == (int)CardType.MINION)
                    .OrderBy(entity => entity.GetTag(GameTag.ZONE_POSITION))
                    .Select(entity => BuildSmallEntity(entity, options, tagChange, showEntity))
                    .ToList();
            }
            catch (Exception e)
            {
                Logger.Log("Warning: issue when trying to build zone " + e.Message, e.StackTrace);
                return BuildBoard(gameState, options, playerId, tagChange, showEntity);
            }
        }

        private static bool RemovedFromPlay(FullEntity entity, TagChange tagChange, ShowEntity showEntity)
        {
            if (tagChange == null && showEntity == null)
            {
                return false;
            }
            var valueTC = tagChange != null
                && tagChange.Entity == entity.Entity
                && tagChange.Name == (int)GameTag.ZONE
                && tagChange.Value != (int)Zone.PLAY;
            var valueSE = showEntity != null
                && showEntity.Entity == entity.Entity
                && showEntity.GetTag(GameTag.ZONE) > 0
                && showEntity.GetTag(GameTag.ZONE) != (int)Zone.PLAY;
            return valueTC || valueSE;
        }

        private static bool PutInPlay(FullEntity entity, TagChange tagChange, ShowEntity showEntity)
        {
            if (tagChange == null && showEntity == null)
            {
                return false;
            }
            var valueTC = tagChange != null
                && tagChange.Entity == entity.Entity
                && tagChange.Name == (int)GameTag.ZONE
                && tagChange.Value == (int)Zone.PLAY;
            var valueSE = showEntity != null
                && showEntity.Entity == entity.Entity
                && showEntity.GetTag(GameTag.ZONE) == (int)Zone.PLAY;
            return valueTC || valueSE;
        }

        private static object BuildSmallEntity(BaseEntity entity, Options options, TagChange tagChange, ShowEntity showEntity)
        {
            string cardId = null;
            if (entity.GetType() == typeof(FullEntity))
            {
                cardId = (entity as FullEntity).CardId;
            }
            var newTags = tagChange != null && tagChange.Entity == entity.Id ? entity.GetTagsCopy(tagChange) : entity.GetTagsCopy();
            return new
            {
                entityId = entity.Id,
                cardId = cardId,
                attack = tagChange?.Name == (int)GameTag.ATK && tagChange?.Entity == entity.Id
                    ? tagChange.Value
                    : entity.GetTag(GameTag.ATK),
                health = entity.GetTag(GameTag.HEALTH),
                // Doesn't work because we get the options after the game state
                //validOption = options != null && options.OptionList != null 
                //    ? options.OptionList
                //        .Where(option => option.Error == (int)PlayReq.NONE)
                //        .Any(option => option.Entity == entity.Id) 
                //    : false,
                tags = newTags
            };
        }

        private static object BuildSmallHeroEntity(BaseEntity entity)
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
                tags = entity.GetTagsCopy()
            };
        }
    }
}
