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
            var currentCard = GameState.CurrentEntities[showEntity.Entity];
            // If the card is already present in the deck, do nothing
            if (currentCard.GetTag(GameTag.ZONE) == (int)Zone.DECK)
            {
                return null;
            }
            var creatorCardId = FindCardCreatorCardId(showEntity.GetTag(GameTag.CREATOR), node);
            var cardId = PredictCardId(creatorCardId, node);
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
            var creatorCardId = FindCardCreatorCardId(fullEntity.GetTag(GameTag.CREATOR), node);
            var cardId = PredictCardId(creatorCardId, node);
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

        private string FindCardCreatorCardId(int creatorTag, Node node)
        {
            if (creatorTag != -1 && GameState.CurrentEntities.ContainsKey(creatorTag))
            {
                var creator = GameState.CurrentEntities[creatorTag];
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

        private string PredictCardId(string creatorCardId, Node node)
        {
            switch(creatorCardId)
            {
                // Raptor Hatchling
                case "UNG_914": return "UNG_914t1";
                // Seaforium Bomber
                case "BOT_511": return "BOT_511t";
                // Clockwork Goblin
                case "DAL_060": return "BOT_511t";
                // Wrenchcalibur
                case "DAL_063": return "BOT_511t";
                // Augmented Elekk creates the same card as was played
                case "BOT_559": 
                    // The parent action is Augmented Elekk trigger, which is not the one we're interested in
                    // Its parent is the one that created the new entity
                    if (node.Parent.Parent.Type == typeof(Parser.ReplayData.GameActions.Action))
                    {
                        var act = node.Parent.Parent.Object as Parser.ReplayData.GameActions.Action;
                        // It should be the last ShowEntity of the action children
                        // Otherwise, the last FullEntity
                        for (int i = act.Data.Count - 1; i >= 0; i--)
                        {
                            if (act.Data[i].GetType() == typeof(ShowEntity))
                            {
                                var showEntity = act.Data[i] as ShowEntity;
                                return showEntity.CardId;
                            }
                            if (act.Data[i].GetType() == typeof(FullEntity))
                            {
                                var fullEntity = act.Data[i] as FullEntity;
                                return fullEntity.CardId;
                            }
                        }
                        // And if nothing matches, then we don't predict anything
                        return null;
                    }
                    return null;
            }
            return null;
        }
    }
}
