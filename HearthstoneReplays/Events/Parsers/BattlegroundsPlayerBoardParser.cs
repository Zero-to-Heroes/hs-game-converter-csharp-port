﻿using HearthstoneReplays.Parser;
using System.Linq;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.Entities;
using System.Collections.Generic;
using static HearthstoneReplays.Events.CardIds;
using HearthstoneReplays.Parser.ReplayData.GameActions;

namespace HearthstoneReplays.Events.Parsers
{
    public class BattlegroundsPlayerBoardParser : ActionParser
    {
        private static List<string> COMPETING_BATTLE_START_HERO_POWERS = new List<string>() {
            RebornRitesBattlegrounds,
            SwattingInsectsBattlegrounds,
            EmbraceYourRageBattlegrounds,
        };

        private static List<string> TAVISH_HERO_POWERS = new List<string>() {
            AimLeftToken,
            AimRightToken,
            AimLowToken,
            AimHighToken,
        };

        static List<string> START_OF_COMBAT_MINION_EFFECT = new List<string>() {
            RedWhelp,
            RedWhelpBattlegrounds,
            PrizedPromoDrake,
            PrizedPromoDrakeBattlegrounds,
            CorruptedMyrmidon,
            CorruptedMyrmidonBattlegrounds,
            MantidQueen,
            MantidQueenBattlegrounds,
        };

        private GameState GameState { get; set; }
        private ParserState ParserState { get; set; }
        private StateFacade StateFacade { get; set; }

        public BattlegroundsPlayerBoardParser(ParserState ParserState, StateFacade helper)
        {
            this.ParserState = ParserState;
            this.GameState = ParserState.GameState;
            this.StateFacade = helper;
        }

        public bool AppliesOnNewNode(Node node, StateType stateType)
        {
            return stateType == StateType.GameState
                && IsApplyOnNewNode(node);
        }

        public bool IsApplyOnNewNode(Node node)
        {
            var isAction = StateFacade.IsBattlegrounds()
                && GameState.GetGameEntity().GetTag(GameTag.TURN) % 2 == 0
                && GameState.BgsCurrentBattleOpponent == null
                && node.Type == typeof(Action);
            if (!isAction)
            {
                return false;
            }

            var actionEntityId = (node.Object as Action).Entity;
            var actionEntity = GameState.CurrentEntities[actionEntityId];
            if (!GameState.CurrentEntities.ContainsKey(actionEntityId))
            {
                return false;
            }

            var isCorrectActionData = ((node.Object as Action).Type == (int)BlockType.ATTACK
                // Why do we want deaths? Can there be a death without an attack or trigger first? AFAIK sacrifices like Tamsin's hero
                // power is the only option?
                //|| (node.Object as Action).Type == (int)BlockType.DEATHS
                // Basically trigger as soon as we can, and just leave some room for the Lich King's hero power
                // Here we assume that hero powers are triggered first, before Start of Combat events
                // The issue is if two hero powers (including the Lich King) compete, which is the case when Al'Akir triggers first for instance
                || ((node.Object as Action).Type == (int)BlockType.TRIGGER
                        // Here we don't want to send the boards when the hero power is triggered, because we want the 
                        // board state to include the effect of the hero power, since the simulator can't guess
                        // what its outcome is (Embrace Your Rage) or what minion it targets (Reborn Rites)
                        && !COMPETING_BATTLE_START_HERO_POWERS.Contains(actionEntity.CardId)
                        && !IsTavishPreparation(node)
                        && (
                            // This was introduced to wait until the damage is done to each hero before sending the board state. However,
                            // forcing the entity to be the root entity means that sometimes we send the info way too late.
                            actionEntity.CardId == CardIds.Baconshop8playerenchantEnchantmentBattlegrounds
                            // This condition has been introduced to solve an issue when the Wingmen hero power triggers. In that case, the parent action of attacks
                            // is not a TB_BaconShop_8P_PlayerE, but the hero power action itself.
                            || actionEntity.GetTag(GameTag.CARDTYPE) == (int)CardType.HERO_POWER
                            // Here we want the boards to be send before the minions start of combat effects happen, because
                            // we want the simulator to include their random effects inside the simulation
                            || START_OF_COMBAT_MINION_EFFECT.Contains(actionEntity.CardId)
                            )
                    )
            );
            if (!isCorrectActionData)
            {
                return false;
            }

            // Check that we have enough information on the opponent to avoid sending the data too soon
            var haveHeroesAllRequiredData = GameState.CurrentEntities.Values
                .Where(entity => entity.GetTag(GameTag.CARDTYPE) == (int)CardType.HERO)
                .Where(entity => entity.GetTag(GameTag.ZONE) == (int)Zone.PLAY)
                // Here we accept to face the ghost
                .Where(entity => entity.CardId != BartenderBobBattlegrounds
                    && entity.CardId != BaconphheroHeroicBattlegrounds)
                //.Select(entity => entity.IsBaconGhost() 
                //    ? GetGhostBaseEntity(entity)
                //    : entity)
                .All(entity => entity.IsBaconGhost() 
                    ? (GetGhostBaseEntity(entity)?.GetTag(GameTag.COPIED_FROM_ENTITY_ID) ?? 0) > 0 
                    : entity.GetTag(GameTag.PLAYER_TECH_LEVEL) > 0);
            var debugList = GameState.CurrentEntities.Values
                .Where(entity => entity.GetTag(GameTag.CARDTYPE) == (int)CardType.HERO)
                .Where(entity => entity.GetTag(GameTag.ZONE) == (int)Zone.PLAY)
                // Here we accept to face the ghost
                .Where(entity => entity.CardId != BartenderBobBattlegrounds
                    && entity.CardId != BaconphheroHeroicBattlegrounds)
                .ToList();
            if (!haveHeroesAllRequiredData)
            {
                return false;
            }

            return true;
        }

        private FullEntity GetGhostBaseEntity(FullEntity ghostEntity)
        {
            var mainPlayer = StateFacade.LocalPlayer;
            var playerEntity = GameState.CurrentEntities.Values
                .Where(entity => entity.GetTag(GameTag.CARDTYPE) == (int)CardType.HERO)
                .Where(entity => entity.GetTag(GameTag.ZONE) == (int)Zone.PLAY)
                .Where(entity => entity.GetEffectiveController() == mainPlayer.PlayerId)
                .Where(entity => entity.CardId != BartenderBobBattlegrounds
                    && entity.CardId != KelthuzadBattlegrounds
                    && entity.CardId != BaconphheroHeroicBattlegrounds)
                .OrderBy(entity => entity.Id)
                .LastOrDefault();
            var nextOpponentPlayerId = playerEntity.GetTag(GameTag.NEXT_OPPONENT_PLAYER_ID);
            var nextOpponentCandidates = GameState.CurrentEntities.Values
                .Where(entity => entity.GetTag(GameTag.CARDTYPE) == (int)CardType.HERO)
                .Where(entity => entity.GetTag(GameTag.PLAYER_ID) == nextOpponentPlayerId)
                .Where(entity => entity.CardId != BartenderBobBattlegrounds
                    && entity.CardId != KelthuzadBattlegrounds
                    && entity.CardId != BaconphheroHeroicBattlegrounds)
                .ToList();
            var nextOpponent = nextOpponentCandidates == null || nextOpponentCandidates.Count == 0 ? null : nextOpponentCandidates[0];
            return nextOpponent ?? ghostEntity;
        }

        public bool AppliesOnCloseNode(Node node, StateType stateType)
        {
            return false;
        }

        private bool IsTavishPreparation(Node node)
        {
            if (node.Type != typeof(Action))
            {
                return false;
            }
            var action = node.Object as Action;
            if (action.Type != (int)BlockType.TRIGGER)
            {
                return false;
            }
            if (!GameState.CurrentEntities.ContainsKey(action.Entity))
            {
                return false;
            }
            var entity = GameState.CurrentEntities[action.Entity];
            if (!TAVISH_HERO_POWERS.Contains(entity.CardId))
            {
                return false;
            }
            // When we're triggering Tavish in battle, the BLOCK also contains a tag change (earlier)
            var parent = node.Parent;
            if (parent.Type != typeof(Action))
            {
                return false;
            }
            var parentAction = parent.Object as Action;
            if (parentAction.Type != (int)BlockType.TRIGGER)
            {
                return false;
            }


            var isInCombat = parentAction.Data
                .Where(data => data.GetType() == typeof(TagChange))
                .Select(data => data as TagChange)
                // Undocumented tag
                .Where(data => data.Name == 2029)
                .FirstOrDefault() != null;
            return !isInCombat;
        }

        // In case a start of combat / Hero Power effect only damages a minion, this is not an issue
        // since we only send the health, not the damage
        // However, if a minion dies in the process, and triggers a chain reaction, this could be an 
        // issue when relying on the "attack" tag.
        // Maybe also consider a DEATHS tag, which can work around this
        // What about DIVINE_SHIELDs being removed by red whelp / Nef though?
        public List<GameEventProvider> CreateGameEventProviderFromNew(Node node)
        {
            // We rely on nested actions to avoid send the event too often. However, this is an issue when 
            // dealing with start of combat triggers like RedWhelp or Prized Promo-Drake
            var parentAction = (node.Parent.Object as Parser.ReplayData.GameActions.Action);
            if (parentAction == null)
            {
                return null;
            }

            var entity = GameState.CurrentEntities[parentAction.Entity];
            if (entity.CardId != "TB_BaconShop_8P_PlayerE")
            {
                return null;
            }



            //var action = node.Parent.Object as Parser.ReplayData.GameActions.Action;
            var opponent = StateFacade.OpponentPlayer;
            var player = StateFacade.LocalPlayer;

            var opponentBoard = CreateProviderFromAction(opponent, true, player, node);
            var playerBoard = CreateProviderFromAction(player, false, player, node);

            //GameState.BgsHasSentNextOpponent = false;

            var result = new List<GameEventProvider>();
            result.Add(GameEventProvider.Create(
                   parentAction.TimeStamp,
                   "BATTLEGROUNDS_PLAYER_BOARD",
                   () => new GameEvent
                   {
                       Type = "BATTLEGROUNDS_PLAYER_BOARD",
                       Value = new
                       {
                           PlayerBoard = playerBoard,
                           OpponentBoard = opponentBoard,
                       }
                   },
                   true,
                   node
               ));
            return result;
        }

        public List<GameEventProvider> CreateGameEventProviderFromClose(Node node)
        {
            return null;
        }

        private PlayerBoard CreateProviderFromAction(Player player, bool isOpponent, Player mainPlayer, Node node)
        {
            //var action = node.Parent.Object as Parser.ReplayData.GameActions.Action;
            //var heroes = GameState.CurrentEntities.Values
            //    .Where(entity => entity.GetTag(GameTag.CARDTYPE) == (int)CardType.HERO)
            //    .Where(entity => entity.GetTag(GameTag.ZONE) == (int)Zone.PLAY)
            //    .ToList();
            var potentialHeroes = GameState.CurrentEntities.Values
                .Where(entity => entity.GetTag(GameTag.CARDTYPE) == (int)CardType.HERO)
                .Where(entity => entity.GetTag(GameTag.ZONE) == (int)Zone.PLAY)
                .Where(entity => entity.GetEffectiveController() == player.PlayerId)
                // Here we accept to face the ghost
                .Where(entity => entity.CardId != BartenderBobBattlegrounds
                    && entity.CardId != BaconphheroHeroicBattlegrounds
                    && entity.CardId != BaconphheroHeroicBattlegrounds)
                .ToList();
            var hero = potentialHeroes
                //.Where(entity => entity.CardId != KelthuzadBattlegrounds)
                .FirstOrDefault()
                ?.Clone();
            var cardId = hero?.CardId;
            if (isOpponent)
            {
                GameState.BgsCurrentBattleOpponent = cardId;
            }

            if (cardId == KelthuzadBattlegrounds)
            {
                // Finding the one that is flagged as the player's NEXT_OPPONENT
                var playerEntity = GameState.CurrentEntities.Values
                    .Where(entity => entity.GetTag(GameTag.CARDTYPE) == (int)CardType.HERO)
                    .Where(entity => entity.GetTag(GameTag.ZONE) == (int)Zone.PLAY)
                    .Where(entity => entity.GetEffectiveController() == mainPlayer.PlayerId)
                    .Where(entity => entity.CardId != BartenderBobBattlegrounds
                        && entity.CardId != KelthuzadBattlegrounds
                        && entity.CardId != BaconphheroHeroicBattlegrounds)
                    .OrderBy(entity => entity.Id)
                    .LastOrDefault();
                var nextOpponentPlayerId = playerEntity.GetTag(GameTag.NEXT_OPPONENT_PLAYER_ID);

                var nextOpponentCandidates = GameState.CurrentEntities.Values
                    .Where(entity => entity.GetTag(GameTag.CARDTYPE) == (int)CardType.HERO)
                    .Where(entity => entity.GetTag(GameTag.PLAYER_ID) == nextOpponentPlayerId)
                    .Where(entity => entity.CardId != BartenderBobBattlegrounds
                        && entity.CardId != KelthuzadBattlegrounds
                        && entity.CardId != BaconphheroHeroicBattlegrounds)
                    .ToList();
                var nextOpponent = nextOpponentCandidates == null || nextOpponentCandidates.Count == 0 ? null : nextOpponentCandidates[0];

                cardId = nextOpponent?.CardId;
            }
            // Happens in the first encounter
            if (cardId == null)
            {
                var activePlayer = GameState.CurrentEntities[StateFacade.LocalPlayer.Id];
                var opponentPlayerId = activePlayer.GetTag(GameTag.NEXT_OPPONENT_PLAYER_ID);
                hero = GameState.CurrentEntities.Values
                    .Where(data => data.GetTag(GameTag.PLAYER_ID) == opponentPlayerId)
                    .FirstOrDefault()
                    ?.Clone();
                cardId = hero?.CardId;
            }
            if (cardId != null)
            {
                // We don't use the game state builder here because we really need the full entities
                var board = GameState.CurrentEntities.Values
                    .Where(entity => entity.GetEffectiveController() == player.PlayerId)
                    .Where(entity => entity.GetTag(GameTag.ZONE) == (int)Zone.PLAY)
                    .Where(entity => entity.GetTag(GameTag.CARDTYPE) == (int)CardType.MINION)
                    .OrderBy(entity => entity.GetTag(GameTag.ZONE_POSITION))
                    .Select(entity => entity.Clone())
                    .ToList();
                var secrets = GameState.CurrentEntities.Values
                    .Where(entity => entity.GetEffectiveController() == player.PlayerId)
                    .Where(entity => entity.GetTag(GameTag.ZONE) == (int)Zone.SECRET)
                    .OrderBy(entity => entity.GetTag(GameTag.ZONE_POSITION))
                    .Select(entity => entity.Clone())
                    .ToList();
                var heroPower = GameState.CurrentEntities.Values
                    .Where(entity => entity.GetEffectiveController() == player.PlayerId)
                    .Where(entity => entity.GetTag(GameTag.ZONE) == (int)Zone.PLAY)
                    .Where(entity => entity.GetTag(GameTag.CARDTYPE) == (int)CardType.HERO_POWER)
                    .Select(entity => entity.Clone())
                    .FirstOrDefault();
                if (heroPower == null)
                {
                    Logger.Log("WARNING: could not find hero power", "");
                }
                var heroPowerUsed = heroPower?.GetTag(GameTag.BACON_HERO_POWER_ACTIVATED) == 1;
                if (heroPower?.CardId == CardIds.EmbraceYourRageBattlegrounds)
                {
                    var parentAction = (node.Parent.Object as Parser.ReplayData.GameActions.Action);
                    var hasTriggerBlock = parentAction.Data
                        .Where(data => data is Parser.ReplayData.GameActions.Action)
                        .Select(data => data as Parser.ReplayData.GameActions.Action)
                        .Where(action => action.Type == (int)BlockType.TRIGGER)
                        .Where(action => GameState.CurrentEntities.ContainsKey(action.Entity)
                            && GameState.CurrentEntities[action.Entity]?.CardId == CardIds.EmbraceYourRageBattlegrounds)
                        .Count() > 0;
                    heroPowerUsed = heroPowerUsed || hasTriggerBlock;
                }
                var result = board.Select(entity => AddEchantments(GameState.CurrentEntities, entity)).ToList();

                if (result.Count > 7)
                {
                    Logger.Log("Too many entities on board", "");
                }

                return new PlayerBoard()
                {
                    Hero = hero,
                    HeroPowerCardId = heroPower?.CardId,
                    HeroPowerUsed = heroPowerUsed,
                    HeroPowerInfo = heroPower?.GetTag(GameTag.TAG_SCRIPT_DATA_NUM_1) ?? 0,
                    CardId = cardId,
                    Board = result,
                    Secrets = secrets,
                };
            }
            return null;
        }

        private object AddEchantments(Dictionary<int, FullEntity> currentEntities, FullEntity fullEntity)
        {
            var enchantments = currentEntities.Values
                .Where(entity => entity.GetTag(GameTag.ATTACHED) == fullEntity.Id)
                .Where(entity => entity.GetTag(GameTag.ZONE) == (int)Zone.PLAY)
                .Select(entity => new
                {
                    EntityId = entity.Id,
                    CardId = entity.CardId
                })
                .ToList();
            dynamic result = new
            {
                CardId = fullEntity.CardId,
                Entity = fullEntity.Entity,
                Id = fullEntity.Id,
                Tags = fullEntity.GetTagsCopy(),
                TimeStamp = fullEntity.TimeStamp,
                Enchantments = enchantments,
            };
            return result;
        }


        internal class PlayerBoard
        {
            public FullEntity Hero { get; set; }

            public string HeroPowerCardId { get; set; }

            public bool HeroPowerUsed { get; set; }

            public int HeroPowerInfo { get; set; }

            public string CardId { get; set; }

            public List<object> Board { get; set; }

            public List<FullEntity> Secrets { get; set; }
        }
    }
}

