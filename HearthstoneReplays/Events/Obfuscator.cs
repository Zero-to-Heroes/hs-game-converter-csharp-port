using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser;
using HearthstoneReplays.Parser.ReplayData.Entities;
using static HearthstoneReplays.Events.CardIds;
using Action = HearthstoneReplays.Parser.ReplayData.GameActions.Action;

namespace HearthstoneReplays.Events
{
    internal class Obfuscator
    {
        internal static bool shouldObfuscateCardDraw(string cardId, GameState gameState, Node node)
        {
            if (node?.Parent?.Type == typeof(Action))
            {
                var action = node.Parent.Object as Action;
                var actionEntityId = action.Entity;
                var actionEntity = gameState.CurrentEntities.GetValueOrDefault(actionEntityId);
                if (action.Type == (int)BlockType.POWER)
                {
                    var actionCardId = actionEntity?.CardId;
                    switch (actionCardId)
                    {
                        case SirFinleySeaGuide:
                            return true;
                    }
                }
            }

            return false;
        }
    }
}
