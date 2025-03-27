using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using HearthstoneReplays.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Action = HearthstoneReplays.Parser.ReplayData.GameActions.Action;
using HearthstoneReplays.Parser.ReplayData.Entities;

namespace HearthstoneReplays.Events.Cards
{
    internal class DemonicProject
    {
        public static string PredctCardId(GameState gameState, int entityId, int creatorEntityId, Node node, StateFacade stateFacade)
        {
            if (node.Parent?.Type != typeof(Action))
            {
                return null;
            }

            var act = node.Parent.Object as Action;
            var actionEntity = gameState.CurrentEntities.GetValueOrDefault(act.Entity);
            if (actionEntity.CardIdsToCreate.Count == 0)
            {
                var linkedEntities = new List<int>() { actionEntity.GetTag(GameTag.TAG_SCRIPT_DATA_ENT_1), actionEntity.GetTag(GameTag.TAG_SCRIPT_DATA_ENT_2) };
                actionEntity.CardIdsToCreate = linkedEntities
                    .Select(id => gameState.CurrentEntities.GetValueOrDefault(id)?.CardId)
                    .ToList();
            }

            if (actionEntity.CardIdsToCreate.Count > 0)
            {
                var result = actionEntity.CardIdsToCreate[0];
                actionEntity.CardIdsToCreate.RemoveAt(0);
                return result;
            }
            return null;
        }
    }
}
