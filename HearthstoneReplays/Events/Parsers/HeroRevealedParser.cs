using HearthstoneReplays.Parser;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.Entities;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using System.Collections.Generic;

namespace HearthstoneReplays.Events.Parsers
{
    public class HeroRevealedParser : ActionParser
    {
        private GameState GameState { get; set; }
        private ParserState ParserState { get; set; }

        public HeroRevealedParser(ParserState ParserState)
        {
            this.ParserState = ParserState;
            this.GameState = ParserState.GameState;
        }

        public bool AppliesOnNewNode(Node node)
        {
            return false;
        }

        public bool AppliesOnCloseNode(Node node)
        {
            FullEntity fullEntity;
            return node.Type == typeof(FullEntity)
                && (fullEntity = node.Object as FullEntity).GetTag(GameTag.ZONE) == (int)Zone.PLAY
                && fullEntity.GetCardType() == (int)CardType.HERO;

        }

        public List<GameEventProvider> CreateGameEventProviderFromNew(Node node)
        {
            return null;
        }

        public List<GameEventProvider> CreateGameEventProviderFromClose(Node node)
        {
            var fullEntity = node.Object as FullEntity;
            var cardId = fullEntity.CardId;
            var controllerId = fullEntity.GetEffectiveController();
            var health = fullEntity.GetTag(GameTag.HEALTH);
            return new List<GameEventProvider> { GameEventProvider.Create(
                fullEntity.TimeStamp,
                "HERO_REVEALED",
                GameEvent.CreateProvider(
                    "HERO_REVEALED",
                    cardId,
                    controllerId,
                    fullEntity.Id,
                    ParserState,
                    GameState,
                    null,
                    new {
                        Health = health,
                    }
                ),
                true,
                node) };
        }
    }
}
