using HearthstoneReplays.Parser;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.Entities;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using System.Collections.Generic;

namespace HearthstoneReplays.Events.Parsers
{
    public class HeroPowerChangedParser : ActionParser
    {
        private GameState GameState { get; set; }
        private ParserState ParserState { get; set; }
        private StateFacade StateFacade { get; set; }

        public HeroPowerChangedParser(ParserState ParserState, StateFacade facade)
        {
            this.ParserState = ParserState;
            this.GameState = ParserState.GameState;
            this.StateFacade = facade;
        }

        public bool AppliesOnNewNode(Node node, StateType stateType)
        {
            return false;
        }

        public bool AppliesOnCloseNode(Node node, StateType stateType)
        {
            return stateType == StateType.PowerTaskList
                && node.Type == typeof(FullEntity)
                && (node.Object as FullEntity).GetTag(GameTag.ZONE) == (int)Zone.PLAY
                && (node.Object as FullEntity).GetTag(GameTag.CARDTYPE) == (int)CardType.HERO_POWER;
                //&& !ParserState.ReconnectionOngoing;
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
            var gameState = GameEvent.BuildGameState(ParserState, StateFacade, GameState, null, null);
            var creatorEntityId = fullEntity.GetTag(GameTag.CREATOR);
            var creatorEntityCardId = GameState.CurrentEntities.ContainsKey(creatorEntityId) 
                ? GameState.CurrentEntities[creatorEntityId].CardId
                : null;
            return new List<GameEventProvider> { GameEventProvider.Create(
                fullEntity.TimeStamp,
                "HERO_POWER_CHANGED",
                GameEvent.CreateProvider(
                    "HERO_POWER_CHANGED",
                    cardId,
                    controllerId,
                    fullEntity.Id,
                    StateFacade,
                    gameState,
                    new {
                        CreatorCardId = creatorEntityCardId,
                    }
                ),
                true,
                node) };
        }
    }
}
