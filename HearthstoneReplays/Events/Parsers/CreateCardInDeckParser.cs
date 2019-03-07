using HearthstoneReplays.Parser;
using HearthstoneReplays.Parser.ReplayData;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using System;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.Entities;

namespace HearthstoneReplays.Events.Parsers
{
    public class CreateCardInDeckParser : ActionParser
    {
        private GameState GameState { get; set; }
        private ParserState ParserState { get; set; }

        public CreateCardInDeckParser(ParserState ParserState)
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
            // In this case, a FullEntity is created with minimal info, and the real
            // card creation happens in the ShowEntity
            var appliesOnShowEntity = node.Type == typeof(ShowEntity)
                && (node.Object as ShowEntity).GetTag(GameTag.ZONE) == (int)Zone.DECK;
            var appliesOnFullEntity = node.Type == typeof(FullEntity)
                && (node.Object as FullEntity).GetTag(GameTag.ZONE) == (int)Zone.DECK
                // We don't want to include the entities created when the game starts
                && node.Parent.Type == typeof(Parser.ReplayData.GameActions.Action);
            return appliesOnShowEntity || appliesOnFullEntity;
        }

        public GameEventProvider CreateGameEventProviderFromNew(Node node)
        {
            return null;
        }

        public GameEventProvider CreateGameEventProviderFromClose(Node node)
        {
            if (node.Type == typeof(ShowEntity))
            {
                return CreateFromShowEntity(node);
            }
            else if (node.Type == typeof(FullEntity))
            {
                return CreateFromFullEntity(node);
            }
            return null;
        }

        private GameEventProvider CreateFromShowEntity(Node node)
        {
            var showEntity = node.Object as ShowEntity;
            var cardId = showEntity.CardId;
            var controllerId = showEntity.GetTag(GameTag.CONTROLLER);
            return GameEventProvider.Create(
                showEntity.TimeStamp,
                () => new GameEvent
                {
                    Type = "CREATE_CARD_IN_DECK",
                    Value = new
                    {
                        CardId = cardId,
                        ControllerId = controllerId,
                        LocalPlayer = ParserState.LocalPlayer,
                        OpponentPlayer = ParserState.OpponentPlayer
                    }
                },
                true,
                node.CreationLogLine);
        }

        private GameEventProvider CreateFromFullEntity(Node node)
        {
            var fullEntity = node.Object as FullEntity;
            var creatorCardId = FindCardCreatorCardId(fullEntity, node);
            var cardId = PredictCardId(creatorCardId);
            var controllerId = fullEntity.GetTag(GameTag.CONTROLLER);
            return GameEventProvider.Create(
                fullEntity.TimeStamp,
                () => new GameEvent
                {
                    Type = "CREATE_CARD_IN_DECK",
                    Value = new
                    {
                        CardId = cardId,
                        ControllerId = controllerId,
                        LocalPlayer = ParserState.LocalPlayer,
                        OpponentPlayer = ParserState.OpponentPlayer,
                        CreatorCardId = creatorCardId, // Used when there is no cardId, so we can show "created by ..."
                    }
                },
                true,
                node.CreationLogLine);
        }

        private string FindCardCreatorCardId(FullEntity fullEntity, Node node)
        {
            if (fullEntity.GetTag(GameTag.CREATOR) != -1 
                    && GameState.CurrentEntities.ContainsKey(fullEntity.GetTag(GameTag.CREATOR)))
            {
                var creator = GameState.CurrentEntities[fullEntity.GetTag(GameTag.CREATOR)];
                return creator.CardId;
            }
            if (node.Parent.Type == typeof(Parser.ReplayData.GameActions.Action))
            {
                var act = node.Parent.Object as Parser.ReplayData.GameActions.Action;
                if (GameState.CurrentEntities.ContainsKey(act.Entity))
                {
                    var creator = GameState.CurrentEntities[act.Entity];
                    return creator.CardId;
                }
            }
            return null;
        }

        private string PredictCardId(string creatorCardId)
        {
            switch(creatorCardId)
            {
                // Raptor Hatchling
                case "UNG_914": return "UNG_914t1";
            }
            return null;
        }
    }
}
