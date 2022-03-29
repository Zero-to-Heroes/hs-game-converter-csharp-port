using HearthstoneReplays.Parser.ReplayData.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HearthstoneReplays.Parser
{
    public class StateFacade
    {
        private CombinedState State { get; set; }

        public StateFacade(CombinedState combined)
        {
            this.State = combined;
        }

        public Player LocalPlayer
        {
            get
            {
                return State?.GSState?.LocalPlayer;
            }
        }

        public Player OpponentPlayer
        {
            get
            {
                return State?.GSState?.OpponentPlayer;
            }
        }

        public int ScenarioID
        {
            get
            {
                return State.GSState.CurrentGame.ScenarioID;
            }
        }

        internal bool HasMetaData()
        {
            return State.GSState.CurrentGame.FormatType != -1 && State.GSState.CurrentGame.GameType != -1 && LocalPlayer != null;
        }

        internal bool IsBattlegrounds()
        {
            return State.GSState.IsBattlegrounds();
        }
    }
}
