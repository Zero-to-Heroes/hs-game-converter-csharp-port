using HearthstoneReplays.Parser;
using HearthstoneReplays.Parser.ReplayData;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using System.Linq;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.Entities;
using System.Collections.Generic;
using static HearthstoneReplays.Events.CardIds;

namespace HearthstoneReplays.Events.Parsers
{
    public class BattlegroundsNextOpponnentParser : ActionParser
    {
        private GameState GameState { get; set; }
        private ParserState ParserState { get; set; }

        public BattlegroundsNextOpponnentParser(ParserState ParserState)
        {
            this.ParserState = ParserState;
            this.GameState = ParserState.GameState;
        }

        public bool AppliesOnNewNode(Node node)
        {
            return node.Type == typeof(TagChange)
                && (node.Object as TagChange).Name == (int)GameTag.NEXT_OPPONENT_PLAYER_ID;
        }

        public bool AppliesOnCloseNode(Node node)
        {
            var isPlayerNode = node.Type == typeof(Player);
            return node.Type == typeof(PlayerEntity)
                && (node.Object as PlayerEntity).Tags.Find(tag => tag.Name == (int)GameTag.NEXT_OPPONENT_PLAYER_ID) != null;
        }

        public List<GameEventProvider> CreateGameEventProviderFromNew(Node node)
        {
            var tagChange = node.Object as TagChange;
            var isInAction = node.Parent != null && node.Parent.Type == typeof(Action);
            // Only consider the tag changes that are at the root
            // This double tag change notif was introduced in a recent update (19.0 or 19.2). 
            // It can probably be useful if we're able to see for each player who their next opponent will be,
            // but for now since we only care about the main player we keep things simple
            if (isInAction)
            {
                return null;
            }

            var heroes = GameState.CurrentEntities.Values
                .Where(entity => entity.GetTag(GameTag.CARDTYPE) == (int)CardType.HERO)
                .Where(entity => entity.GetTag(GameTag.PLAYER_ID) == tagChange.Value)
                .Where(entity => entity.CardId != NonCollectible.Neutral.BobsTavernTavernBrawl
                    && entity.CardId != NonCollectible.Neutral.KelthuzadTavernBrawl2)
                .ToList();
            var hero = heroes == null || heroes.Count == 0 ? null : heroes[0];
            // Happens in some circumstances, though it's not clear for me which ones. Maybe
            // when the future opponent isn't here yet, or when players take too long to join?
            if (hero == null)
            {
                GameState.NextBgsOpponentPlayerId = tagChange.Value;
            }
            //Logger.Log("Next opponent player id", hero?.CardId);
            if (hero?.CardId != null && hero.CardId != NonCollectible.Neutral.BobsTavernTavernBrawl)
            {
                return new List<GameEventProvider> {  
                    GameEventProvider.Create(
                        tagChange.TimeStamp,
                        "BATTLEGROUNDS_NEXT_OPPONENT",
                        () => new GameEvent
                        {
                            Type = "BATTLEGROUNDS_NEXT_OPPONENT",
                            Value = new
                            {
                                CardId = hero.CardId,
                            }
                        },
                        true,
                        node,
                        false,
                        false) 
                };
            }
            return null;
        }

        public List<GameEventProvider> CreateGameEventProviderFromClose(Node node)
        {
            // Because the first player is handled differently than the rest, we kind of hack our way
            // there
            GameState.NextBgsOpponentPlayerId = (node.Object as PlayerEntity).Tags
                .Find(tag => tag.Name == (int)GameTag.NEXT_OPPONENT_PLAYER_ID)
                .Value;
            return null;
        }
    }
}
