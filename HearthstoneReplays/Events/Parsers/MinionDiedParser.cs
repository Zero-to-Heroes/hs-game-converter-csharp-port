using HearthstoneReplays.Parser;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.Entities;
using System.Collections.Generic;
using HearthstoneReplays.Parser.ReplayData;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using System.Linq;

namespace HearthstoneReplays.Events.Parsers
{
    public class MinionDiedParser : ActionParser
    {
        private GameState GameState { get; set; }
        private ParserState ParserState { get; set; }

        public MinionDiedParser(ParserState ParserState)
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
            return node.Type == typeof(Action)
                && (node.Object as Action).Type == (int)BlockType.DEATHS;
        }

        public List<GameEventProvider> CreateGameEventProviderFromNew(Node node)
        {
            return null;
        }

        public List<GameEventProvider> CreateGameEventProviderFromClose(Node node)
        {
            var action = node.Object as Action;
            var deathTags = action.GetDataRecursive()
                .Where(data => data is TagChange)
                .Select(data => data as TagChange)
                .Where(tag => tag.Name == (int)GameTag.ZONE && tag.Value == (int)Zone.GRAVEYARD)
                // Checking the current zone doesn't work, because we work on a Close node. However, since 
                // we are in the DEATHS block, we should be good
                //.Where(tag => GameState.CurrentEntities[tag.Entity].GetTag(GameTag.ZONE) == (int)Zone.PLAY)
                .Where(tag => GameState.CurrentEntities[tag.Entity].GetTag(GameTag.CARDTYPE) == (int)CardType.MINION);
            var deadMinions = deathTags.Select(tag =>
            {
                var entity = GameState.CurrentEntities[tag.Entity];
                var cardId = entity.CardId;
                var controllerId = entity.GetEffectiveController();
                return new
                {
                    CardId = cardId,
                    EntityId = entity.Id,
                    ControllerId = controllerId,
                    Timestamp = tag.TimeStamp,
                };
            });

            var gameState = GameEvent.BuildGameState(ParserState, GameState, null, null);
            return new List<GameEventProvider> { GameEventProvider.Create(
                action.TimeStamp,
                "MINIONS_DIED",
                GameEvent.CreateProvider(
                    "MINIONS_DIED",
                    null,
                    -1,
                    -1,
                    ParserState,
                    GameState,
                    gameState,
                    new {
                        DeadMinions = deadMinions,
                    }),
                true,
                node) };
        }
    }
}
