#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData;
using HearthstoneReplays.Parser.ReplayData.Entities;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using HearthstoneReplays.Parser.ReplayData.Meta;
using HearthstoneReplays.Events;
using Action = HearthstoneReplays.Parser.ReplayData.GameActions.Action;
using Newtonsoft.Json;
using HearthstoneReplays.Parser.Handlers;

#endregion

namespace HearthstoneReplays.Parser.Handlers
{
    public class DataHandler
    {
        private Helper helper;
        private GameMetaData metadata = new GameMetaData();


        public DataHandler(Helper helper)
        {
            this.helper = helper;
        }

        public void Handle(DateTime timestamp, string data, ParserState state, StateType stateType, DateTime previousTimestamp, StateFacade stateFacade, long currentGameSeed)
        {

            var trimmed = data.Trim();
            var indentLevel = data.Length - trimmed.Length;
            data = trimmed;
            bool isApplied = false;
            isApplied = isApplied || NewGameHandler.HandleNewGame(timestamp, data, state, previousTimestamp, stateType, stateFacade, currentGameSeed, metadata);
            isApplied = isApplied || SpectatorHandler.HandleSpectator(timestamp, data, state, stateFacade);

            // When catching up with some log lines, sometimes we get some leftover from a previous game.
            // Only checking the state does not account for these, and parsing fails because there is no
            // game to parse, and Reset() has not been called to initialize everything
            if (state.Ended || state.CurrentGame == null)
            {
                return;
            }

            isApplied = isApplied || CreateGameHandler.HandleCreateGame(timestamp, data, state, indentLevel);
            isApplied = isApplied || PlayerNameHandler.HandlePlayerName(timestamp, data, state, stateType);
            isApplied = isApplied || BlockStartHandler.HandleBlockStart(timestamp, data, state, indentLevel, helper);
            isApplied = isApplied || BlockEndHandler.HandleBlockEnd(data, state);
            isApplied = isApplied || MetaDataHandler.HandleMetaData(timestamp, data, state, stateType, metadata, helper);
            isApplied = isApplied || CreatePlayerHandler.HandleCreatePlayer(data, state, stateFacade, indentLevel);
            isApplied = isApplied || ActionMetadataHandler.HandleActionMetaData(timestamp, data, state, indentLevel, helper);
            isApplied = isApplied || ActionMetadataInfoHandler.HandleActionMetaDataInfo(timestamp, data, state, indentLevel, helper);
            isApplied = isApplied || SubSpellHandler.HandleSubSpell(timestamp, data, state, stateType, stateFacade, helper);
            isApplied = isApplied || ShowEntityHandler.HandleShowEntity(timestamp, data, state, indentLevel, helper);
            isApplied = isApplied || ChangeEntityHandler.HandleChangeEntity(timestamp, data, state, indentLevel, helper);
            isApplied = isApplied || HideEntityHandler.HandleHideEntity(timestamp, data, state, indentLevel, helper);
            isApplied = isApplied || FullEntityHandler.HandleFullEntity(timestamp, data, state, indentLevel, helper);
            isApplied = isApplied || TagChangeHandler.HandleTagChange(timestamp, data, state, stateType, stateFacade, indentLevel, helper);
            isApplied = isApplied || TagHandler.HandleTag(timestamp, data, state, helper);
            isApplied = isApplied || ShuffleDeckHandler.HandleShuffleDeck(timestamp, data, state, indentLevel);
        }
    }
}