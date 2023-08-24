using HearthstoneReplays.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HearthstoneReplays.Events.Parsers.Controls
{
    internal class BattlegroundsControls
    {
        public static Type[] EXCLUDED_PARSERS = {
            typeof(MinionsWillDieParser),
            typeof(CardRevealedParser),
            typeof(EntityUpdateParser),
            typeof(DataScriptChangedParser),
            typeof(CostChangedParser),
            typeof(MinionOnBoardAttackUpdatedParser),
            //typeof(MinionSummonedParser), // Needed for minions highlights
            typeof(LinkedEntityParser),
            typeof(CopiedFromEntityIdParser),
            // SubSpell is hard-coded in DataHandler
        };
    }
}
