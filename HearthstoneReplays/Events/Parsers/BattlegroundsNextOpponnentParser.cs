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
            return ParserState.CurrentGame.GameType == (int)GameType.GT_BATTLEGROUNDS
                && node.Type == typeof(TagChange)
                && (node.Object as TagChange).Name == (int)GameTag.NEXT_OPPONENT_PLAYER_ID;
        }

        public bool AppliesOnCloseNode(Node node)
        {
            return false;
        }

        public List<GameEventProvider> CreateGameEventProviderFromNew(Node node)
        {
            var tagChange = node.Object as TagChange;
            var heroes = GameState.CurrentEntities.Values
                .Where(entity => entity.GetTag(GameTag.CARDTYPE) == (int)CardType.HERO)
                .Where(entity => entity.GetTag(GameTag.PLAYER_ID) == tagChange.Value)
                .Where(entity => entity.CardId != NonCollectible.Neutral.BobsTavernTavernBrawl
                    && entity.CardId != NonCollectible.Neutral.KelthuzadTavernBrawl2)
                .ToList();
            var hero = heroes == null || heroes.Count == 0 ? null : heroes[0];
            //Logger.Log("Next opponent player id", hero?.CardId);
            if (hero?.CardId != null && hero.CardId != NonCollectible.Neutral.BobsTavernTavernBrawl)
            {
                return new List<GameEventProvider> {  
                    GameEventProvider.Create(
                        tagChange.TimeStamp,
                        () => new GameEvent
                        {
                            Type = "BATTLEGROUNDS_NEXT_OPPONENT",
                            Value = new
                            {
                                CardId = hero.CardId,
                            }
                        },
                        true,
                        node.CreationLogLine,
                        false) 
                };
            }
            return null;
        }

        public List<GameEventProvider> CreateGameEventProviderFromClose(Node node)
        {
            return null;
        }
    }
}
