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
    public class CthunParser : ActionParser
    {
        private GameState GameState { get; set; }
        private ParserState ParserState { get; set; }

        public CthunParser(ParserState ParserState)
        {
            this.ParserState = ParserState;
            this.GameState = ParserState.GameState;
        }

        public bool AppliesOnNewNode(Node node)
        {
            return node.Type == typeof(TagChange)
                && (node.Object as TagChange).Name == (int)GameTag.ATK
                && GameState.CurrentEntities.ContainsKey((node.Object as TagChange).Entity)
                && GameState.CurrentEntities[((node.Object as TagChange).Entity)].CardId == NonCollectible.Neutral.Cthun;
        }

        public bool AppliesOnCloseNode(Node node)
        {
            return false;
        }

        public List<GameEventProvider> CreateGameEventProviderFromNew(Node node)
        {
            // Special case for Crystal Core which, for an unknown reason, shows C'Thun even if it's not present in the deck
            if (node.Parent != null && node.Parent.Type == typeof(Action))
            {
                var parentAction = node.Parent.Object as Action;
                if (GameState.CurrentEntities.ContainsKey(parentAction.Entity))
                {
                    var parentEntity = GameState.CurrentEntities[parentAction.Entity];
                    if (parentAction.Type == (int)BlockType.POWER && parentEntity.CardId == NonCollectible.Rogue.TheCavernsBelow_CrystalCoreTokenUNGORO)
                    {
                        return null;
                    }
                }
            }

            var tagChange = node.Object as TagChange;
            var entity = GameState.CurrentEntities[tagChange.Entity];
            var cardId = entity.CardId;
            var controllerId = entity.GetTag(GameTag.CONTROLLER);
            var gameState = GameEvent.BuildGameState(ParserState, GameState, tagChange, null);
            return new List<GameEventProvider> { GameEventProvider.Create(
                tagChange.TimeStamp,
                "CTHUN",
                GameEvent.CreateProvider(
                    "CTHUN",
                    cardId,
                    controllerId,
                    entity.Id,
                    ParserState,
                    GameState,
                    gameState,
                    new {
                        CthunSize = tagChange.Value,
                    }),
                true,
                node.CreationLogLine) };
        }

        public List<GameEventProvider> CreateGameEventProviderFromClose(Node node)
        {
            return null;
        }
    }
}
