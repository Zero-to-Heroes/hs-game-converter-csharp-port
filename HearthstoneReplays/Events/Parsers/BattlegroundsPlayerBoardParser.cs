using HearthstoneReplays.Parser;
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

        // We want to send the board states before these hero powers trigger
        private static List<string> START_OF_COMBAT_HERO_POWER = new List<string>() {
            //TeronGorefiend_RapidReanimation
            RapidReanimation_ImpendingDeathEnchantment,
            WaxWarbandBattlegrounds,
            // We need to send the board state before it triggers, because the simulator needs to handle it, so that
            // it is not broken if Ozumat + Tavish (or other hero power that is managed by the simulator) happen
            Ozumat_Tentacular,
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
            InterrogatorWhitemane_BG24_704,
            InterrogatorWhitemane_BG24_704_G,
            Soulsplitter,
            SoulsplitterBattlegrounds,
            AmberGuardian,
            AmberGuardianBattlegrounds,
            ChoralMrrrglr,
            ChoralMrrrglrBattlegrounds,
            SanctumRester,
            SanctumResterBattlegrounds,
        };

        static List<string> START_OF_COMBAT_QUEST_REWARD_EFFECT = new List<string>() {
            EvilTwin,
            StaffOfOrigination_BG24_Reward_312,
            TheSmokingGun,
            StolenGold,
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
            // Use PTL to be able to "see the future", even if it means the info will be delayed
            // Using PTL instead of GS will add a few seconds delay, but it shouldn't be too much
            return stateType == StateType.PowerTaskList
                && IsApplyOnNewNode(node);
        }

        public bool IsApplyOnNewNode(Node node)
        {
            var isAction = StateFacade.IsBattlegrounds()
                && GameState.GetGameEntity() != null
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
                            || START_OF_COMBAT_HERO_POWER.Contains(actionEntity.CardId)
                            || START_OF_COMBAT_QUEST_REWARD_EFFECT.Contains(actionEntity.CardId)
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
            //var debugList = GameState.CurrentEntities.Values
            //    .Where(entity => entity.GetTag(GameTag.CARDTYPE) == (int)CardType.HERO)
            //    .Where(entity => entity.GetTag(GameTag.ZONE) == (int)Zone.PLAY)
            //    // Here we accept to face the ghost
            //    .Where(entity => entity.CardId != BartenderBobBattlegrounds
            //        && entity.CardId != BaconphheroHeroicBattlegrounds)
            //    .ToList();
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

            // If we move back to GS logs, this probably needs to be in another parser
            GameState.BgsHasSentNextOpponent = false;

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
                    .Where(entity => entity.GetTag(GameTag.BACON_IS_BOB_QUEST) != 1)
                    .Where(entity => entity.GetTag(GameTag.QUEST) != 1)
                    .Where(entity => entity.GetTag(GameTag.SIDEQUEST) != 1)
                    .OrderBy(entity => entity.GetTag(GameTag.ZONE_POSITION))
                    .Select(entity => entity.Clone())
                    .ToList();
                var hand = GameState.CurrentEntities.Values
                    .Where(entity => entity.GetEffectiveController() == player.PlayerId)
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
                        .Select(e => GetEntitySpawnedFromHand(e.Id) ?? e)
                        .Select(entity => entity.Clone())
                        .Select(e => e.SetTag(GameTag.DAMAGE, 0).SetTag(GameTag.ZONE, (int)Zone.HAND) as FullEntity)
                        .ToList();
                    hand = revealedHand;
                }
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

                var questRewards = GameState.CurrentEntities.Values
                    .Where(entity => entity.GetEffectiveController() == player.PlayerId)
                    .Where(entity => entity.GetTag(GameTag.ZONE) == (int)Zone.PLAY)
                    .Where(entity => entity.GetTag(GameTag.CARDTYPE) == (int)CardType.BATTLEGROUND_QUEST_REWARD)
                    .Select(entity => entity.Clone())
                    .Select(entity => entity.CardId)
                    .ToList();

                var eternalKnightBonus = GetPlayerEnchantmentValue(player.PlayerId, CardIds.EternalKnightPlayerEnchantEnchantment);
                // Includes Anub'arak, Nerubian Deathswarmer
                var undeadAttackBonus = GetPlayerEnchantmentValue(player.PlayerId, CardIds.UndeadBonusAttackPlayerEnchantDntEnchantment);
                var frostlingBonus = GetPlayerEnchantmentValue(player.PlayerId, CardIds.FlourishingFrostlingPlayerEnchantDntEnchantment);
                var bloodGemEnchant = GameState.CurrentEntities.Values
                    .Where(entity => entity.GetEffectiveController() == player.PlayerId)
                    .Where(entity => entity.GetTag(GameTag.ZONE) == (int)Zone.PLAY)
                    .Where(entity => entity.CardId == CardIds.BloodGemPlayerEnchantEnchantment)
                    .FirstOrDefault();
                var bloodGemAttackBonus = bloodGemEnchant?.GetTag(GameTag.TAG_SCRIPT_DATA_NUM_1, 0) ?? 0;
                var bloodGemHealthBonus = bloodGemEnchant?.GetTag(GameTag.TAG_SCRIPT_DATA_NUM_2, 0) ?? 0;
                var choralEnchantments = StateFacade.GsState.GameState.CurrentEntities.Values
                    .Where(e => e.CardId == CardIds.ChoralMrrrglr_ChorusEnchantment)
                    .Where(e => board.Select(b => b.Id).Contains(e.GetTag(GameTag.ATTACHED)));
                var choralEnchantment = choralEnchantments.FirstOrDefault();

                var heroPowerInfo = heroPower?.GetTag(GameTag.TAG_SCRIPT_DATA_NUM_1) ?? 0;
                if (heroPower?.CardId == CardIds.TeronGorefiend_RapidReanimation)
                {
                    var impendingDeathEnchantments = GameState.CurrentEntities.Values
                        .Where(entity => entity.GetEffectiveController() == player.PlayerId)
                        .Where(entity => entity.CardId == CardIds.RapidReanimation_ImpendingDeathEnchantment);
                    var impendingDeath = impendingDeathEnchantments
                        .Where(entity => entity.GetTag(GameTag.ZONE) == (int)Zone.PLAY || entity.GetTag(GameTag.ZONE) == (int)Zone.SETASIDE)
                        .Where(entity => entity.GetTag(GameTag.COPIED_FROM_ENTITY_ID) == -1)
                        .FirstOrDefault()
                        ?? impendingDeathEnchantments
                            .Where(entity => entity.GetTag(GameTag.ZONE) == (int)Zone.PLAY || entity.GetTag(GameTag.ZONE) == (int)Zone.SETASIDE)
                            .FirstOrDefault()
                        ?? impendingDeathEnchantments
                            .Where(entity => entity.GetTag(GameTag.ZONE) == (int)Zone.GRAVEYARD)
                            .FirstOrDefault();
                    // Can be null if the player didn't use the hero power
                    if (impendingDeath != null)
                    {
                        heroPowerInfo = impendingDeath.GetTag(GameTag.ATTACHED);
                    }
                }

                return new PlayerBoard()
                {
                    Hero = hero,
                    HeroPowerCardId = heroPower?.CardId,
                    HeroPowerUsed = heroPowerUsed,
                    HeroPowerInfo = heroPowerInfo,
                    CardId = cardId,
                    Board = result,
                    QuestRewards = questRewards,
                    Secrets = secrets,
                    Hand = hand,
                    GlobalInfo = new BgsPlayerGlobalInfo()
                    {
                        EternalKnightsDeadThisGame = eternalKnightBonus,
                        UndeadAttackBonus = undeadAttackBonus,
                        FrostlingBonus = frostlingBonus,
                        BloodGemAttackBonus = bloodGemAttackBonus,
                        BloodGemHealthBonus = bloodGemHealthBonus,
                        // TODO: always show the base version, even for golden
                        ChoralAttackBuff = choralEnchantment?.GetTag(GameTag.TAG_SCRIPT_DATA_NUM_1, 0) ?? 0,
                        ChoralHealthBuff = choralEnchantment?.GetTag(GameTag.TAG_SCRIPT_DATA_NUM_2, 0) ?? 0,
                    }
                };
            }
            return null;
        }

        private FullEntity GetEntitySpawnedFromHand(int id)
        {
            var tmp = StateFacade.GsState.GameState.CurrentEntities.Values
                .Where(e => e.GetTag(GameTag.COPIED_FROM_ENTITY_ID) == id);
            var tmp2 = StateFacade.GsState.GameState.CurrentEntities.GetValueOrDefault(15571);
            var tmp3 = StateFacade.GsState.GameState.CurrentEntities.Values
                .Select(e => e.Entity);
            var tmp4 = StateFacade.GsState.GameState.CurrentEntities.Values
                .Select(e => e.Id);
            var result = StateFacade.GsState.GameState.CurrentEntities.Values
                .Where(e => e.GetTag(GameTag.COPIED_FROM_ENTITY_ID) == id
                    || e.AllPreviousTags.Any(t => t.Name == (int)GameTag.COPIED_FROM_ENTITY_ID && t.Value == id))
                .FirstOrDefault();
            return result;
        }

        private int GetPlayerEnchantmentValue(int playerId, string enchantment)
        {
            return GameState.CurrentEntities.Values
                .Where(entity => entity.GetEffectiveController() == playerId)
                .Where(entity => entity.GetTag(GameTag.ZONE) == (int)Zone.PLAY)
                .Where(entity => entity.CardId == enchantment)
                .FirstOrDefault()
                ?.GetTag(GameTag.TAG_SCRIPT_DATA_NUM_1) ?? 0;
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

            public List<string> QuestRewards { get; set; }

            public List<object> Board { get; set; }

            public List<FullEntity> Secrets { get; set; }

            public List<FullEntity> Hand { get; set; }

            public BgsPlayerGlobalInfo GlobalInfo { get; set; }
        }

        internal class BgsPlayerGlobalInfo
        {
            public int EternalKnightsDeadThisGame { get; set; }
            public int UndeadAttackBonus { get; set; }
            public int FrostlingBonus { get; set; }
            public int BloodGemAttackBonus { get; set; }
            public int BloodGemHealthBonus { get; set; }
            public int ChoralHealthBuff { get; set; }
            public int ChoralAttackBuff { get; set; }
        }
    }
}

