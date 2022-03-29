using HearthstoneReplays.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HearthstoneReplays.Parser
{
    public class CombinedState
    {
        // The "future" state, before the animations happen
        public ParserState GSState { get; private set; }
        // The "real-time" state
        public ParserState PTLState { get; private set; }
        public StateFacade GameInfoHelper { get; private set; }

        public CombinedState()
        {
            GameInfoHelper = new StateFacade(this);
            EventQueueHandler handler = new EventQueueHandler(GameInfoHelper);
            GSState = new ParserState(StateType.GameState, handler, GameInfoHelper);
            PTLState = new ParserState(StateType.PowerTaskList, handler, GameInfoHelper);
        }

        //public void StartDevMode()
        //{

        //    GSState.NodeParser.StartDevMode();
        //    PTLState.NodeParser.StartDevMode();
        //}
    }
}
