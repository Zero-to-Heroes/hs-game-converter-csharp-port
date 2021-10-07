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
            NonCollectible.Neutral.RebornRitesTavernBrawl,
            NonCollectible.Neutral.SwattingInsectsTavernBrawl,
            NonCollectible.Neutral.EmbraceYourRageTavernBrawl,
        }; 

        static List<string> START_OF_COMBAT_MINION_EFFECT = new List<string>() {
            NonCollectible.Neutral.RedWhelp,
            NonCollectible.Neutral.RedWhelpTavernBrawl,
            NonCollectible.Neutral.PrizedPromoDrake,
            NonCollectible.Neutral.PrizedPromoDrake_PrizedPromoDrake,
        };

        private GameState GameState { get; set; }
        private ParserState ParserState { get; set; }

        public BattlegroundsPlayerBoardParser(ParserState ParserState)
        {
            this.ParserState = ParserState;
            this.GameState = ParserState.GameState;
        }

        public bool AppliesOnNewNode(Node node)
        {
            return (ParserState.CurrentGame.GameType == (int)GameType.GT_BATTLEGROUNDS || ParserState.CurrentGame.GameType == (int)GameType.GT_BATTLEGROUNDS_FRIENDLY)
                && GameState.GetGameEntity().GetTag(GameTag.TURN) % 2 == 0
                && GameState.BgsCurrentBattleOpponent == null
                && node.Type == typeof(Action)
                && ((node.Object as Action).Type == (int)BlockType.ATTACK
                        || (node.Object as Action).Type == (int)BlockType.DEATHS
                        // Basically trigger as soon as we can, and just leave some room for the Lich King's hero power
                        // Here we assume that hero powers are triggered first, before Start of Combat events
                        // The issue is if two hero powers (including the Lich King) compete, which is the case when Al'Akir triggers first for instance
                        || ((node.Object as Action).Type == (int)BlockType.TRIGGER
                                // Here we don't want to send the boards when the hero power is triggered, because we want the 
                                // board state to include the effect of the hero power, since the simulator can't guess
                                // what its outcome is (Embrace Your Rage) or what minion it targets (Reborn Rites)
                                && !COMPETING_BATTLE_START_HERO_POWERS.Contains(GameState.CurrentEntities[(node.Object as Action).Entity].CardId)
                                && GameState.CurrentEntities.ContainsKey((node.Object as Action).Entity)
                                && (
                                    // This was introduced to wait until the damage is done to each hero before sending the board state. However,
                                    // forcing the entity to be the root entity means that sometimes we send the info way too late.
                                    GameState.CurrentEntities[(node.Object as Action).Entity].CardId == CardIds.NonCollectible.Neutral.Baconshop8playerenchantTavernBrawl
                                    // This condition has been introduced to solve an issue when the Wingmen hero power triggers. In that case, the parent action of attacks
                                    // is not a TB_BaconShop_8P_PlayerE, but the hero power action itself.
                                    || GameState.CurrentEntities[(node.Object as Action).Entity].GetTag(GameTag.CARDTYPE) == (int)CardType.HERO_POWER
                                    // Here we want the boards to be send before the minions start of combat effects happen, because
                                    // we want the simulator to include their random effects inside the simulation
                                    || START_OF_COMBAT_MINION_EFFECT.Contains(GameState.CurrentEntities[(node.Object as Action).Entity].CardId)
                                   )
                            )
                   );
        }

        public bool AppliesOnCloseNode(Node node)
        {
            return false;
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
            var opponent = ParserState.OpponentPlayer;
            var player = ParserState.LocalPlayer;

            var opponentBoard = CreateProviderFromAction(opponent, true, player, node);
            var playerBoard = CreateProviderFromAction(player, false, player, node);

            GameState.BgsHasSentNextOpponent = false;

            var result = new List<GameEventProvider>();
            result.Add(GameEventProvider.Create(
                   parentAction.TimeStamp,
                   "BATTLEGROUNDS_PLAYER_BOARD",
                   () => (ParserState.CurrentGame.GameType == (int)GameType.GT_BATTLEGROUNDS
                            || ParserState.CurrentGame.GameType == (int)GameType.GT_BATTLEGROUNDS_FRIENDLY)
                        ? new GameEvent
                        {
                            Type = "BATTLEGROUNDS_PLAYER_BOARD",
                            Value = new
                            {
                                PlayerBoard = playerBoard,
                                OpponentBoard = opponentBoard,
                            }
                        }
                        : null,
                   true,
                   node,
                   false,
                   false,
                   true // Don't wait until the animation is ready, so we send the board state right away
               ) );
            return result;
        }

        public List<GameEventProvider> CreateGameEventProviderFromClose(Node node)
        {
            return null;
        }

        private PlayerBoard CreateProviderFromAction(Player player, bool isOpponent, Player mainPlayer, Node node)
        {
            //var action = node.Parent.Object as Parser.ReplayData.GameActions.Action;
            var heroes = GameState.CurrentEntities.Values
                .Where(entity => entity.GetTag(GameTag.CARDTYPE) == (int)CardType.HERO)
                .Where(entity => entity.GetTag(GameTag.ZONE) == (int)Zone.PLAY)
                .ToList();
            var potentialHeroes = GameState.CurrentEntities.Values
                .Where(entity => entity.GetTag(GameTag.CARDTYPE) == (int)CardType.HERO)
                .Where(entity => entity.GetTag(GameTag.ZONE) == (int)Zone.PLAY)
                .Where(entity => entity.GetTag(GameTag.CONTROLLER) == player.PlayerId)
                // Here we accept to face the ghost
                .Where(entity => entity.CardId != NonCollectible.Neutral.BartenderBobTavernBrawl
                    && entity.CardId != NonCollectible.Neutral.BaconphheroTavernBrawl
                    && entity.CardId != NonCollectible.Neutral.BaconphheroTavernBrawl)
                .ToList();
            var hero = potentialHeroes
                //.Where(entity => entity.CardId != NonCollectible.Neutral.KelthuzadTavernBrawl2)
                .FirstOrDefault()
                ?.Clone();
            //Logger.Log("Trying to handle board", "" + ParserState.CurrentGame.GameType + " // " + hero?.CardId);
            //Logger.Log("Hero " + hero.CardId, hero.Entity);
            var cardId = hero?.CardId;
            if (isOpponent)
            {
                GameState.BgsCurrentBattleOpponent = cardId;
            }

            if (cardId == NonCollectible.Neutral.KelthuzadTavernBrawl2)
            {
                // Finding the one that is flagged as the player's NEXT_OPPONENT
                var playerEntity = GameState.CurrentEntities.Values
                    .Where(entity => entity.GetTag(GameTag.CARDTYPE) == (int)CardType.HERO)
                    .Where(entity => entity.GetTag(GameTag.ZONE) == (int)Zone.PLAY)
                    .Where(entity => entity.GetTag(GameTag.CONTROLLER) == mainPlayer.PlayerId)
                    .Where(entity => entity.CardId != NonCollectible.Neutral.BartenderBobTavernBrawl
                        && entity.CardId != NonCollectible.Neutral.KelthuzadTavernBrawl2
                        && entity.CardId != NonCollectible.Neutral.BaconphheroTavernBrawl)
                    .OrderBy(entity => entity.Id)
                    .LastOrDefault();
                var nextOpponentPlayerId = playerEntity.GetTag(GameTag.NEXT_OPPONENT_PLAYER_ID);

                var nextOpponentCandidates = GameState.CurrentEntities.Values
                    .Where(entity => entity.GetTag(GameTag.CARDTYPE) == (int)CardType.HERO)
                    .Where(entity => entity.GetTag(GameTag.PLAYER_ID) == nextOpponentPlayerId)
                    .Where(entity => entity.CardId != NonCollectible.Neutral.BartenderBobTavernBrawl
                        && entity.CardId != NonCollectible.Neutral.KelthuzadTavernBrawl2
                        && entity.CardId != NonCollectible.Neutral.BaconphheroTavernBrawl)
                    .ToList();
                var nextOpponent = nextOpponentCandidates == null || nextOpponentCandidates.Count == 0 ? null : nextOpponentCandidates[0];

                cardId = nextOpponent?.CardId;
            }
            // Happens in the first encounter
            if (cardId == null)
            {
                var activePlayer = GameState.CurrentEntities[ParserState.LocalPlayer.Id];
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
                    .Where(entity => entity.GetTag(GameTag.CONTROLLER) == player.PlayerId)
                    .Where(entity => entity.GetTag(GameTag.ZONE) == (int)Zone.PLAY)
                    .Where(entity => entity.GetTag(GameTag.CARDTYPE) == (int)CardType.MINION)
                    .OrderBy(entity => entity.GetTag(GameTag.ZONE_POSITION))
                    .Select(entity => entity.Clone())
                    .ToList();
                var secrets = GameState.CurrentEntities.Values
                    .Where(entity => entity.GetTag(GameTag.CONTROLLER) == player.PlayerId)
                    .Where(entity => entity.GetTag(GameTag.ZONE) == (int)Zone.SECRET)
                    .OrderBy(entity => entity.GetTag(GameTag.ZONE_POSITION))
                    .Select(entity => entity.Clone())
                    .ToList();
                var heroPower = GameState.CurrentEntities.Values
                    .Where(entity => entity.GetTag(GameTag.CONTROLLER) == player.PlayerId)
                    .Where(entity => entity.GetTag(GameTag.ZONE) == (int)Zone.PLAY)
                    .Where(entity => entity.GetTag(GameTag.CARDTYPE) == (int)CardType.HERO_POWER)
                    .Select(entity => entity.Clone())
                    .FirstOrDefault();
                if (heroPower == null)
                {
                    Logger.Log("WARNING: could not find hero power", "");
                }
                var heroPowerUsed = heroPower?.GetTag(GameTag.EXHAUSTED) == 1 || heroPower?.GetTag(GameTag.BACON_HERO_POWER_ACTIVATED) == 1;
                if (heroPower?.CardId == CardIds.NonCollectible.Neutral.EmbraceYourRageTavernBrawl)
                {
                    var parentAction = (node.Parent.Object as Parser.ReplayData.GameActions.Action);
                    var hasTriggerBlock = parentAction.Data
                        .Where(data => data is Parser.ReplayData.GameActions.Action)
                        .Select(data => data as Parser.ReplayData.GameActions.Action)
                        .Where(action => action.Type == (int)BlockType.TRIGGER)
                        .Where(action => GameState.CurrentEntities.ContainsKey(action.Entity)
                            && GameState.CurrentEntities[action.Entity]?.CardId == CardIds.NonCollectible.Neutral.EmbraceYourRageTavernBrawl)
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

            public string CardId { get; set; }

            public List<object> Board { get; set; }

            public List<FullEntity> Secrets { get; set; }
        }
    }
}

