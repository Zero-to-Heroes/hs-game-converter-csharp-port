using HearthstoneReplays.Parser;
using System.Linq;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.Entities;
using System.Collections.Generic;
using static HearthstoneReplays.Events.CardIds;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using System.Runtime.InteropServices;
using System;
using static HearthstoneReplays.Events.Parsers.BattlegroundsActivePlayerBoardParser;
using Action = HearthstoneReplays.Parser.ReplayData.GameActions.Action;
using HearthstoneReplays.Parser.ReplayData.Meta;
using static HearthstoneReplays.Events.Parsers.BattlegroundsPlayerBoardParser;
using HearthstoneReplays.Events.Parsers.Utils;

namespace HearthstoneReplays.Events.Parsers
{
    public class BattlegroundsPlayerBoardParser : ActionParser
    {
        private GameState GameState { get; set; }
        private ParserState ParserState { get; set; }
        private StateFacade StateFacade { get; set; }
        private BattlegroundsStartOfBattleLegacySnapshot Snapshot;

        public BattlegroundsPlayerBoardParser(ParserState ParserState, StateFacade helper)
        {
            this.ParserState = ParserState;
            this.GameState = ParserState.GameState;
            this.StateFacade = helper;
            this.Snapshot = new BattlegroundsStartOfBattleLegacySnapshot(ParserState, helper);
        }

        public bool AppliesOnNewNode(Node node, StateType stateType)
        {
            // Use PTL to be able to "see the future", even if it means the info will be delayed
            // Using PTL instead of GS will add a few seconds delay, but it shouldn't be too much
            return stateType == StateType.PowerTaskList && IsApplyOnNewNode(node);
        }

        public bool IsApplyOnNewNode(Node node)
        {
            //return StateFacade.IsBattlegrounds()
            //        && node.Type == typeof(TagChange)
            //        && (node.Object as TagChange).Name == (int)GameTag.BG_BATTLE_STARTING
            //        && (node.Object as TagChange).Value == 0;
            // This seems to always trigger a few seconds before the BATTLE_STARTING tag change, without any significant
            // data being produced
            return StateFacade.IsBattlegrounds()
                    && node.Type == typeof(TagChange)
                    && (node.Object as TagChange).Name == (int)GameTag.BACON_CHOSEN_BOARD_SKIN_ID
                    && (node.Object as TagChange).Value != 0;
        }

        public bool AppliesOnCloseNode(Node node, StateType stateType)
        {
            return false;
        }

        public List<GameEventProvider> CreateGameEventProviderFromNew(Node node)
        {
            Logger.Log("Starting to build player boards", node.CreationLogLine);
            var tagChange = node.Object as TagChange;
            var opponent = StateFacade.OpponentPlayer;
            var player = StateFacade.LocalPlayer;

            var playerBoard = BattlegroundsPlayerBoardParser.CreateProviderFromAction(player.PlayerId, player.Id, false, player, GameState, StateFacade);
            var opponentBoard = BattlegroundsPlayerBoardParser.CreateProviderFromAction(opponent.PlayerId, opponent.Id, true, player, GameState, StateFacade);

            GameState.BgsHasSentNextOpponent = false;
            Logger.Log("Player boards built", "");

            var result = new List<GameEventProvider>();
            result.Add(GameEventProvider.Create(
                   tagChange.TimeStamp,
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

        internal static PlayerBoard CreateProviderFromAction(int playerPlayerId, int playerEntityId, bool isOpponent, Player mainPlayer, GameState GameState, StateFacade StateFacade)
        {
            var potentialHeroes = GameState.CurrentEntities.Values
                .Where(entity => entity.GetTag(GameTag.CARDTYPE) == (int)CardType.HERO)
                .Where(entity => entity.GetTag(GameTag.ZONE) == (int)Zone.PLAY)
                .Where(entity => entity.GetEffectiveController() == playerPlayerId)
                // Here we accept to face the ghost
                .Where(entity => !entity.IsBaconBartender() && !entity.IsBaconEnchantment())
                .ToList();
            var hero = potentialHeroes.FirstOrDefault()?.Clone();
            var cardId = hero?.CardId;
            // We do this, otherwise we always have the same playerId, which is either the "main player" or the "opponent player" defined
            // at the start. However, we are interested in getting the actual player
            int playerId = hero?.GetTag(GameTag.PLAYER_ID) ?? playerPlayerId;

            if (BgsUtils.IsBaconGhost(cardId) || (hero?.IsBaconBartender() ?? false))
            {
                var linkedEntityId = hero.GetTag(GameTag.LINKED_ENTITY);
                var linkedEntity = GameState.CurrentEntities.GetValueOrDefault(linkedEntityId);
                if (linkedEntity != null)
                {
                    if (!linkedEntity.IsBaconBartender() && !linkedEntity.IsBaconGhost())
                    {
                        hero = linkedEntity;
                        cardId = hero.CardId;
                        playerId = hero.GetTag(GameTag.PLAYER_ID);
                    }
                    else
                    {
                        var heroesForTargetPlayerId = GameState.CurrentEntities.Values
                            .Where(entity => entity.GetTag(GameTag.CARDTYPE) == (int)CardType.HERO)
                            .Where(entity => entity.GetTag(GameTag.PLAYER_ID) == playerId)
                            .Where(entity => !entity.IsBaconBartender() && !entity.IsBaconEnchantment() && !entity.IsBaconGhost())
                            .ToList();
                        var candidate = heroesForTargetPlayerId.LastOrDefault();
                        if (candidate != null)
                        {
                            hero = candidate;
                            cardId = candidate.CardId;
                            playerId = candidate.GetTag(GameTag.PLAYER_ID);
                        }

                    }
                }
                //if (linkedEntityId == -1)
                //{
                //    // Finding the one that is flagged as the player's NEXT_OPPONENT
                //    // FIXME: not applicable in Duos
                //    var candidates = GameState.CurrentEntities.Values
                //        .Where(entity => entity.GetTag(GameTag.CARDTYPE) == (int)CardType.HERO)
                //        .Where(entity => entity.GetEffectiveController() == mainPlayer.PlayerId)
                //        .Where(entity => !entity.IsBaconBartender() && !entity.IsBaconGhost() && !entity.IsBaconEnchantment())
                //        .ToList();
                //    var playerEntity = candidates
                //            .Where(entity => entity.GetTag(GameTag.ZONE) == (int)Zone.PLAY)
                //            .OrderBy(entity => entity.Id)
                //            .LastOrDefault()
                //        ?? candidates.OrderBy(entity => entity.Id).LastOrDefault();
                //    // FIXME: this doesn't work in Duos, as the ghost might be the partner
                //    var nextOpponentPlayerId = playerEntity.GetTag(GameTag.NEXT_OPPONENT_PLAYER_ID);

                //    // FIXME: this doesn't work, at least in Duos, as the candidates are the heroes that weren't picked during
                //    // hero selection
                //    var nextOpponentCandidates = GameState.CurrentEntities.Values
                //        .Where(entity => entity.GetTag(GameTag.CARDTYPE) == (int)CardType.HERO)
                //        .Where(entity => entity.GetTag(GameTag.PLAYER_ID) == nextOpponentPlayerId)
                //        .Where(entity => !entity.IsBaconBartender()
                //            && !entity.IsBaconGhost()
                //            && !entity.IsBaconEnchantment())
                //        .ToList();
                //    var nextOpponent = nextOpponentCandidates == null || nextOpponentCandidates.Count == 0 ? null : nextOpponentCandidates[0];

                //    hero = nextOpponent;
                //    cardId = nextOpponent?.CardId;
                //    playerId = nextOpponent?.GetTag(GameTag.PLAYER_ID) ?? playerId;
                //}

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
                playerId = hero?.GetTag(GameTag.PLAYER_ID) ?? playerId;
            }

            if (isOpponent)
            {
                GameState.BgsCurrentBattleOpponent = cardId;
                GameState.BgsCurrentBattleOpponentPlayerId = playerId;
            }

            if (cardId != null)
            {
                // We don't use the game state builder here because we really need the full entities
                var board = GameState.CurrentEntities.Values
                    .Where(entity => entity.GetEffectiveController() == playerPlayerId)
                    // Because when the opponent is the ghost, we don't always know which ones are actually attached to it
                    // Also, when reconnecting, we sometimes get artifacts for the opponent's board, so restricting the list
                    // to the ones who only just appeared removes a lot of issues
                    .Where(entity => isOpponent ? entity.GetTag(GameTag.NUM_TURNS_IN_PLAY) <= 1 : true)
                    .Where(entity => entity.GetTag(GameTag.ZONE) == (int)Zone.PLAY)
                    .Where(entity => entity.TakesBoardSpace())
                    .OrderBy(entity => entity.GetTag(GameTag.ZONE_POSITION))
                    .Select(entity => entity.Clone())
                    .Select(entity => EnhanceEntities(entity, GameState, StateFacade))
                    .ToList();
                var secrets = GameState.CurrentEntities.Values
                    .Where(entity => entity.GetEffectiveController() == playerPlayerId)
                    .Where(entity => entity.GetTag(GameTag.ZONE) == (int)Zone.SECRET)
                    .Where(entity => entity.GetTag(GameTag.BACON_IS_BOB_QUEST) != 1)
                    .Where(entity => entity.GetTag(GameTag.QUEST) != 1)
                    .Where(entity => entity.GetTag(GameTag.SIDE_QUEST) != 1)
                    .OrderBy(entity => entity.GetTag(GameTag.ZONE_POSITION))
                    .Select(entity => entity.Clone())
                    .Select(entity => BuildEntityWithCardIdFromTheFuture(entity, StateFacade.GsState.GameState))
                    .ToList();
                var hand = GameState.CurrentEntities.Values
                    .Where(entity => entity.GetEffectiveController() == playerPlayerId)
                    .Where(entity => entity.GetTag(GameTag.ZONE) == (int)Zone.HAND)
                    .OrderBy(entity => entity.GetTag(GameTag.ZONE_POSITION))
                    .Select(entity => entity.Clone())
                    .ToList();
                if (isOpponent)
                {
                    // Look for all the cards that are in hand at the start of the fight, and that are "copied" by bassgil
                    // We can't use the final stats as they are, as it would include damage + stats change over the course of
                    // the fight
                    // We might be able to simply remove the damage, and use the buffed stats as the base stats. It won't be 
                    // perfect, but probably good enough
                    var boardCardIds = board.Select(e => e.CardId).ToList();
                    var handEntityIds = hand.Select(e => e.Id).ToList();
                    var revealedHand = hand
                        .Select(e => GetEntitySpawnedFromHand(e.Id, board, StateFacade) ?? e)
                        .Select(e => e.SetTag(GameTag.DAMAGE, 0).SetTag(GameTag.ZONE, (int)Zone.HAND) as FullEntity)
                        .ToList();
                    hand = revealedHand;
                }
                var debug = GameState.CurrentEntities.Values
                    .Where(entity => entity.GetEffectiveController() == playerPlayerId)
                    .ToList();
                var heroPower = GameState.CurrentEntities.Values
                    .Where(entity => entity.GetEffectiveController() == playerPlayerId)
                    .Where(entity => entity.GetTag(GameTag.ZONE) == (int)Zone.PLAY)
                    .Where(entity => entity.GetTag(GameTag.CARDTYPE) == (int)CardType.HERO_POWER)
                    .Select(entity => entity.Clone())
                    .FirstOrDefault();
                if (heroPower == null)
                {
                    var debugHP = hero.Entity == 12613;
                    Logger.Log("WARNING: could not find hero power", "");
                }
                var heroPowerUsed = heroPower?.GetTag(GameTag.BACON_HERO_POWER_ACTIVATED) == 1;
                string heroPowerCreatedEntity = null;
                var finalBoard = board.Select(entity => AddEchantments(GameState.CurrentEntities, entity)).ToList();
                if (finalBoard.Count > 7)
                {
                    Logger.Log("Too many entities on board", "");
                }

                var questRewardRawEntities = GameState.CurrentEntities.Values
                    .Where(entity => entity.GetEffectiveController() == playerPlayerId)
                    .Where(entity => entity.GetTag(GameTag.ZONE) == (int)Zone.PLAY)
                    .Where(entity => entity.GetTag(GameTag.CARDTYPE) == (int)CardType.BATTLEGROUND_QUEST_REWARD)
                    .Select(entity => entity.Clone())
                    .ToList();
                var questRewards = questRewardRawEntities
                    .Select(entity => entity.CardId)
                    .ToList();
                var questRewardEntities = questRewardRawEntities
                    .Select(entity => new QuestReward
                    {
                        CardId = entity.CardId,
                        AvengeCurrent = 0,
                        AvengeDefault = 0,
                        ScriptDataNum1 = entity.GetTag(GameTag.TAG_SCRIPT_DATA_NUM_1, 0)
                    })
                    .ToList();
                var questEntities = GameState.CurrentEntities.Values
                    .Where(entity => entity.GetEffectiveController() == playerPlayerId)
                    .Where(entity => entity.GetTag(GameTag.ZONE) == (int)Zone.SECRET)
                    .Where(entity => entity.GetTag(GameTag.CARDTYPE) == (int)CardType.SPELL)
                    .Where(entity => entity.GetTag(GameTag.QUEST) == 1)
                    .Select(entity => new QuestEntity
                    {
                        CardId = entity.CardId,
                        RewardDbfId = entity.GetTag(GameTag.QUEST_REWARD_DATABASE_ID, 0),
                        ProgressCurrent = entity.GetTag(GameTag.QUEST_PROGRESS, 0),
                        ProgressTotal = entity.GetTag(GameTag.QUEST_PROGRESS_TOTAL, 0),
                    })
                    .ToList();
                List<TrinketEntity> trinkets = BuildTrinkets(playerPlayerId, GameState);

                BgsPlayerGlobalInfo globalInfo = BuildGlobalInfo(playerPlayerId, playerEntityId, finalBoard, GameState, StateFacade);

                // String or int
                dynamic heroPowerInfo = heroPower?.GetTag(GameTag.TAG_SCRIPT_DATA_NUM_1) ?? 0;
                var heroPowerInfo2 = heroPower?.GetTag(GameTag.TAG_SCRIPT_DATA_NUM_2) ?? 0;
                UpdateEmbraceYourRageTarget(StateFacade, heroPowerUsed, heroPower?.CardId, heroPower?.Entity, (newValue) => heroPowerInfo = newValue);
                int heroPowerInfoAsInt = heroPowerInfo is int intValue ? intValue : 0;
                UpdateRebornRitesTarget(StateFacade, heroPowerUsed, heroPower?.CardId, heroPower?.Entity, heroPowerInfoAsInt, (newValue) => heroPowerInfo = newValue);

                return new PlayerBoard()
                {
                    Hero = hero,
                    HeroPowerCardId = heroPower?.CardId,
                    HeroPowerEntityId = heroPower?.Entity ?? -1,
                    HeroPowerUsed = heroPowerUsed,
                    HeroPowerInfo = heroPowerInfo,
                    HeroPowerInfo2 = heroPowerInfo2,
                    HeroPowerCreatedEntity = heroPowerCreatedEntity,
                    CardId = cardId,
                    PlayerId = playerId,
                    PlayerEntityId = playerEntityId,
                    Board = finalBoard,
                    QuestEntities = questEntities,
                    QuestRewards = questRewards,
                    QuestRewardEntities = questRewardEntities,
                    Secrets = secrets,
                    Hand = hand,
                    Trinkets = trinkets,
                    GlobalInfo = globalInfo,
                };
            }
            return null;
        }

        public static List<TrinketEntity> BuildTrinkets(int playerPlayerId, GameState gameState)
        {
            return gameState.CurrentEntities.Values
                .Where(entity => entity.GetEffectiveController() == playerPlayerId)
                .Where(entity => entity.GetTag(GameTag.ZONE) == (int)Zone.PLAY)
                .Where(entity => entity.GetCardType() == (int)CardType.BATTLEGROUND_TRINKET)
                .Select(entity => new TrinketEntity
                {
                    cardId = entity.CardId,
                    entityId = entity.Entity,
                    scriptDataNum1 = entity.GetTag(GameTag.TAG_SCRIPT_DATA_NUM_1),
                    scriptDataNum6 = entity.GetTag(GameTag.TAG_SCRIPT_DATA_NUM_6),
                })
                .OrderBy(trinket => trinket.scriptDataNum6)
                .ToList();
        }

        internal static BgsPlayerGlobalInfo BuildGlobalInfo(int playerId, int playerEntityId, List<BgsPlayerBoardEntity> board, GameState GameState, StateFacade StateFacade)
        {
            var currentEntities = GameState.CurrentEntities.Values.ToList();
            var currentEntitiesGs = StateFacade.GsState.GameState.CurrentEntities.Values.ToList();
            var eternalKnightBonus = GetPlayerEnchantmentValue(playerId, CardIds.EternalKnightPlayerEnchantEnchantment, currentEntities);
            var tavernSpellsCastThisGame = GameState.CurrentEntities[playerEntityId]?.GetTag(GameTag.TAVERN_SPELLS_PLAYED_THIS_GAME) ?? 0;
            // Includes Anub'arak, Nerubian Deathswarmer
            var undeadAttackBonus = GetPlayerEnchantmentValue(playerId, CardIds.UndeadBonusAttackPlayerEnchantDntEnchantment, currentEntities);
            var astralAutomatonBonus = GetPlayerEnchantmentValue(playerId, CardIds.AstralAutomatonPlayerEnchantDntEnchantment_BG_TTN_401pe, currentEntities);
            // Looks like the enchantment isn't used anymore, at least for the opponent?
            var frostlingBonus = GetPlayerTag(playerEntityId, GameTag.BACON_ELEMENTALS_PLAYED_THIS_GAME, currentEntities);
            var piratesPlayedThisGame = GetPlayerTag(playerEntityId, GameTag.BACON_PIRATESS_PLAYED_THIS_GAME, currentEntities);
            var bloodGemEnchant = currentEntities
                .Where(entity => entity.GetEffectiveController() == playerId)
                // Don't use the PLAY zone, as it could cause issues with teammate state in Duos? To be tested
                .Where(entity => entity.GetTag(GameTag.ZONE) == (int)Zone.PLAY)
                .Where(entity => entity.CardId == CardIds.BloodGemPlayerEnchantEnchantment)
                .LastOrDefault();
            var bloodGemAttackBonus = bloodGemEnchant?.GetTag(GameTag.TAG_SCRIPT_DATA_NUM_1, 0) ?? 0;
            var bloodGemHealthBonus = bloodGemEnchant?.GetTag(GameTag.TAG_SCRIPT_DATA_NUM_2, 0) ?? 0;
            //var debugList = currentEntitiesGs
            //    .Where(e => e.CardId == CardIds.ChoralMrrrglr_ChorusEnchantment)
            //    .ToList();
            var debug = GameState.CurrentEntities.GetValueOrDefault(14758);
            var choralEnchantments = currentEntitiesGs
                .Where(e => e.CardId == CardIds.ChoralMrrrglr_ChorusEnchantment)
                .Where(e => board.Select(b => b.Id).Contains(e.GetTag(GameTag.ATTACHED)))
                .ToList();
            var choralEnchantment = choralEnchantments.FirstOrDefault();
            return new BgsPlayerGlobalInfo()
            {
                EternalKnightsDeadThisGame = eternalKnightBonus,
                TavernSpellsCastThisGame = tavernSpellsCastThisGame,
                UndeadAttackBonus = undeadAttackBonus,
                FrostlingBonus = frostlingBonus,
                AstralAutomatonsSummonedThisGame = astralAutomatonBonus,
                PiratesPlayedThisGame = piratesPlayedThisGame,
                BloodGemAttackBonus = bloodGemAttackBonus,
                BloodGemHealthBonus = bloodGemHealthBonus,
                ChoralAttackBuff = choralEnchantment?.GetTag(GameTag.TAG_SCRIPT_DATA_NUM_1, 0) ?? 0,
                ChoralHealthBuff = choralEnchantment?.GetTag(GameTag.TAG_SCRIPT_DATA_NUM_2, 0) ?? 0,
            };
        }

        internal static void UpdateEmbraceYourRageTarget(
            StateFacade stateFacade, bool heroPowerUsed, string heroPowerCardId, int? heroPowerEntityId, Action<dynamic> assignHeroPowerInfo)
        {
            if (heroPowerUsed && heroPowerCardId == CardIds.EmbraceYourRage)
            {
                var createdEntity = stateFacade.GsState.GameState.CurrentEntities.Values
                    .Where(e => e.GetTag(GameTag.CREATOR) == heroPowerEntityId)
                    .Where(e => e.GetCardType() == (int)CardType.MINION)
                    .Reverse()
                    .FirstOrDefault();
                assignHeroPowerInfo.Invoke(createdEntity?.CardId);
            }
        }

        internal static void UpdateRebornRitesTarget(
            StateFacade stateFacade, bool heroPowerUsed, string heroPowerCardId, int? heroPowerEntityId, int heroPowerInfo, Action<dynamic> assignHeroPowerInfo)
        {
            if (heroPowerUsed && heroPowerCardId == CardIds.RebornRites && heroPowerInfo <= 0)
            {
                Action lastActionTrigger = stateFacade.GsState.CurrentGame
                    .GetLastAction(action => action.Type == (int)BlockType.TRIGGER && action.Entity == heroPowerEntityId);
                //Action lastActionTrigger = stateFacade.GsState.CurrentGame.FilterGameData(typeof(Action))
                //    .Select(action => action as Action)
                //    .Where(action => action.Type == (int)BlockType.TRIGGER && action.Entity == heroPowerEntityId)
                //    .LastOrDefault();
                if (lastActionTrigger != null)
                {
                    var metaBlock = lastActionTrigger.Data.Where(d => d is MetaData).Select(m => m as MetaData).FirstOrDefault();
                    var targetEntityId = metaBlock?.MetaInfo?.FirstOrDefault()?.Entity;
                    if (targetEntityId != null && targetEntityId > 0)
                    {
                        assignHeroPowerInfo.Invoke(targetEntityId);
                    }
                }
            }
        }

        internal static FullEntity BuildEntityWithCardIdFromTheFuture(FullEntity entity, GameState gsState)
        {
            if (entity.CardId != null && entity.CardId.Length > 0)
            {
                return entity;
            }
            var entityFromTheFuture = gsState.CurrentEntities.GetValueOrDefault(entity.Entity);
            if (entityFromTheFuture.CardId == null || entityFromTheFuture.CardId.Length == 0)
            {
                return entity;
            }
            entity.CardId = entityFromTheFuture.CardId;
            return entity;
        }

        internal static FullEntity EnhanceEntities(FullEntity entity, GameState gameState, StateFacade StateFacade)
        {
            switch (entity.CardId)
            {
                case LovesickBalladist_BG26_814:
                case LovesickBalladist_BG26_814_G:
                    return EnhanceLovesickBalladist(entity, StateFacade);
                default:
                    return entity;
            }
        }

        internal static FullEntity EnhanceLovesickBalladist(FullEntity entity, StateFacade StateFacade)
        {
            var buffEnchantmentValue = StateFacade.GsState.Replay.Games[StateFacade.GsState.Replay.Games.Count - 1]
                .FilterGameData(typeof(ShowEntity))
                .Select(d => d as ShowEntity)
                .Where(e => e.GetCardType() == (int)CardType.ENCHANTMENT)
                .Where(e => e.GetTag(GameTag.ATTACHED) > 0)
                .Where(e => e.GetTag(GameTag.CREATOR) == entity.Id)
                .Select(e => e.GetTag(GameTag.TAG_SCRIPT_DATA_NUM_1))
                // Make sure to take the last one so we are not polluted by values from previous fights
                .LastOrDefault();
            if (buffEnchantmentValue > 0)
            {
                var baseVale = entity.CardId == LovesickBalladist_BG26_814
                    ? buffEnchantmentValue
                    : buffEnchantmentValue / 2;
                entity.SetTag(GameTag.TAG_SCRIPT_DATA_NUM_1, baseVale);
            }
            return entity;
        }

        private static void OverrideTagWithHistory(FullEntity entity, GameTag tag)
        {
            var tags = entity.TagsHistory
                // When a minion is summoned from hand in combat, there is a SHOW_ENTITY node with the buffed value,
                // which includes the buffs from combat
                .TakeWhile(t => t.Name != (int)GameTag.SHOW_ENTITY_START)
                .Where(t => t.Name == (int)tag)
                .ToList();
            var tagInHand = tags.Count == 1
                ? tags[0].Value
                : tags
                    // Because the first value is the "default" value
                    // If the card in hand has no enchantments whatsoever, we should still have a "reset" at the end
                    .Skip(1)
                    .FirstOrDefault()?.Value;
            entity.SetTag(tag, tagInHand ?? entity.GetTag(tag));
        }

        internal static FullEntity GetEntitySpawnedFromHand(int id, List<FullEntity> board, StateFacade StateFacade)
        {
            var entityInHand = StateFacade.GsState.GameState.CurrentEntities.GetValueOrDefault(id);
            var clone = entityInHand.Clone();

            // This seems to have changed with 29.2. Now, the entity is created in hand with its base values,
            // enchantments are reapplied, and a tag change occurs at the end with the new value
            // I don't have many data points yet, so there might be other tags to update, and there might be
            // circumstances in which this doesn't work (e.g. if multiple tags are applied)
            // ISSUE: if the buff is a buff that occurs in combat (and not a buff that happens on the setup phase),
            // it will still be detected
            OverrideTagWithHistory(clone, GameTag.HEALTH);
            OverrideTagWithHistory(clone, GameTag.ATK);
            OverrideTagWithHistory(clone, GameTag.LITERALLY_UNPLAYABLE);
            OverrideTagWithHistory(clone, GameTag.UNPLAYABLE_VISUALS);

            var enchantments = StateFacade.GsState.GameState.CurrentEntities.Values
                .Where(entity => entity.GetTag(GameTag.ATTACHED) == id)
                // Not sure why it can be REMOVEDFROMGAME
                //.Where(entity => entity.GetTag(GameTag.ZONE) != (int)Zone.REMOVEDFROMGAME)
                .ToList();
            //var debug = StateFacade.GsState.GameState.CurrentEntities.Values
            //    .Where(entity => entity.GetTag(GameTag.ATTACHED) == id)
            //    .ToList();
            if (enchantments.Any(e => e.CardId == ExpeditionPlans_UnplayableEnchantment))
            {
                clone.SetTag(GameTag.UNPLAYABLE_VISUALS, 1);
            }

            return clone;
        }

        internal static int GetPlayerEnchantmentValue(int playerId, string enchantment, List<FullEntity> currentEntities)
        {
            return currentEntities
                .Where(entity => entity.GetEffectiveController() == playerId)
                .Where(entity => entity.GetTag(GameTag.ZONE) == (int)Zone.PLAY)
                .Where(entity => entity.CardId == enchantment)
                .FirstOrDefault()
                ?.GetTag(GameTag.TAG_SCRIPT_DATA_NUM_1) ?? 0;
        }

        internal static int GetPlayerTag(int playerEntityId, GameTag tag, List<FullEntity> currentEntities)
        {
            return currentEntities.Find(e => e.Entity == playerEntityId)?.GetTag(tag, 0) ?? 0;
        }

        internal static BgsPlayerBoardEntity AddEchantments(Dictionary<int, FullEntity> currentEntities, FullEntity fullEntity)
        {
            var debug = fullEntity.Id == 8608;
            // For some reason, Teron's RapidReanimation enchantment is sometimes in the GRAVEYARD zone
            List<Enchantment> enchantments = BuildEnchantments(currentEntities, fullEntity);
            BgsPlayerBoardEntity result = new BgsPlayerBoardEntity()
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

        private static List<Enchantment> BuildEnchantments(Dictionary<int, FullEntity> currentEntities, FullEntity fullEntity)
        {
            var enchantmentEntities = currentEntities.Values
                            .Where(entity => entity.GetTag(GameTag.ATTACHED) == fullEntity.Id)
                            .Where(entity => entity.GetTag(GameTag.ZONE) != (int)Zone.REMOVEDFROMGAME)
                            .ToList();
            var enchantments = enchantmentEntities
                .Select(entity => new Enchantment
                {
                    EntityId = entity.Id,
                    CardId = entity.CardId,
                    TagScriptDataNum1 = entity.GetTag(GameTag.TAG_SCRIPT_DATA_NUM_1),
                    TagScriptDataNum2 = entity.GetTag(GameTag.TAG_SCRIPT_DATA_NUM_2),
                })
                .ToList();
            List<Enchantment> additionalEnchantments = BuildAdditionalEnchantments(fullEntity, enchantmentEntities, currentEntities);
            enchantments.AddRange(additionalEnchantments);
            return enchantments;
        }

        internal class PlayerBoard
        {
            public FullEntity Hero { get; set; }
            public string HeroPowerCardId { get; set; }
            public int HeroPowerEntityId { get; set; }
            public bool HeroPowerUsed { get; set; }
            public dynamic HeroPowerInfo { get; set; }
            // Used for Tavish damage for instance
            public int HeroPowerInfo2 { get; set; }
            public string HeroPowerCreatedEntity { get; set; }
            public string CardId { get; set; }
            public int PlayerId { get; set; }
            public int PlayerEntityId { get; set; }
            public List<QuestEntity> QuestEntities { get; set; }
            public List<string> QuestRewards { get; set; }
            public List<QuestReward> QuestRewardEntities { get; set; }
            public List<BgsPlayerBoardEntity> Board { get; set; }
            public List<FullEntity> Secrets { get; set; }
            public List<TrinketEntity> Trinkets { get; set; }
            public List<FullEntity> Hand { get; set; }
            public BgsPlayerGlobalInfo GlobalInfo { get; set; }
        }

        internal class BgsPlayerBoardEntity
        {
            public string CardId;
            public int Entity;
            public int Id;
            public List<Tag> Tags;
            public DateTime TimeStamp;
            public List<Enchantment> Enchantments;
        }

        public class BgsPlayerGlobalInfo
        {
            public int EternalKnightsDeadThisGame { get; set; }
            public int TavernSpellsCastThisGame { get; set; }
            public int PiratesPlayedThisGame { get; set; }
            public int UndeadAttackBonus { get; set; }
            public int FrostlingBonus { get; set; }
            public int AstralAutomatonsSummonedThisGame { get; set; }
            public int BloodGemAttackBonus { get; set; }
            public int BloodGemHealthBonus { get; set; }
            public int ChoralHealthBuff { get; set; }
            public int ChoralAttackBuff { get; set; }
        }

        internal class QuestReward
        {
            public string CardId;
            public int AvengeCurrent;
            public int AvengeDefault;
            public int ScriptDataNum1;
        }

        internal class QuestEntity
        {
            public string CardId;
            public int RewardDbfId;
            public int ProgressCurrent;
            public int ProgressTotal;
        }

        public class TrinketEntity
        {
            public string cardId;
            public int entityId;
            public int scriptDataNum1;
            public int scriptDataNum6;
        }
    }
}

