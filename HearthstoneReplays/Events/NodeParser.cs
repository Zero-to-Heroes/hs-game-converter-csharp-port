#region
using System;
using System.Timers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.Entities;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using HearthstoneReplays.Parser;
using HearthstoneReplays.Events.Parsers;
#endregion

namespace HearthstoneReplays.Events
{
    public class NodeParser
    {
        public static bool DevMode = false;
        public List<GameEventProvider> eventQueue;

        private List<ActionParser> parsers;
        private Timer timer;
        private ParserState ParserState;

        private readonly Object listLock = new object();

        public NodeParser()
        {
            eventQueue = new List<GameEventProvider>();
            // Check the queue every 100 ms
            timer = new Timer(100);
            timer.Elapsed += ProcessGameEventQueue;
            timer.AutoReset = true;
            timer.Enabled = true;
        }

        public void Reset(ParserState ParserState)
        {
            //ClearQueue();
            // Logger.Log("before reset", eventQueue.Count);
            this.ParserState = ParserState;
            parsers = BuildActionParsers(ParserState);
            // Logger.Log("after reset", eventQueue.Count);
        }

        public void StartDevMode()
        {
            lock (listLock)
            {
                Logger.Log("Enqueuing start dev mode", eventQueue.Count);
                eventQueue.Add(new StartDevModeProvider());
            }
        }

        public async void StopDevMode()
        {
            Logger.Log("Will enqueue stop dev mode", "");
            await Task.Delay(5000);
            lock (listLock)
            {
                Logger.Log("Enqueuing stop dev mode", eventQueue.Count);
                eventQueue.Add(new StopDevModeProvider());
            }
        }

        public void NewNode(Node node)
        {
            try
            {
                if (node == null)
                {
                    return;
                }
                //Logger.Log("Receiving new node", node.CreationLogLine);
                foreach (ActionParser parser in parsers)
                {
                    if (parser.AppliesOnNewNode(node))
                    {
                        List<GameEventProvider> providers = parser.CreateGameEventProviderFromNew(node);
                        if (providers != null)
                        {
                            EnqueueGameEvent(providers.Where(provider => provider != null).ToList());
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Log("Coulnt not apply parsers to new node, ignoring node and moving on " + e.Message, e.StackTrace);
                throw e;
            }
        }

        public void CloseNode(Node node)
        {
            if (node == null)
            {
                return;
            }
            //Logger.Log("Receiving close node", node.CreationLogLine);
            foreach (ActionParser parser in parsers)
            {
                if (!node.Closed && parser.AppliesOnCloseNode(node))
                {
                    List<GameEventProvider> providers = parser.CreateGameEventProviderFromClose(node);
                    if (providers != null && providers.Count > 0)
                    {
                        EnqueueGameEvent(providers.Where(provider => provider != null).ToList());
                    }
                }
            }
            // Make sure we don't process the same node twice
            node.Closed = true;
        }

        public void EnqueueGameEvent(List<GameEventProvider> providers)
        {
            //Logger.Log("[csharp] Enqueueing game event", providers != null ? providers[0].CreationLogLine : null);
            lock (listLock)
            {
                //Logger.Log("Acquierd list lock in queneGameEvent", "");
                // Remove outstanding events
                if (providers.Any(provider => provider.CreationLogLine.Contains("CREATE_GAME")) && eventQueue.Count > 0)
                {
                    Logger.Log("Purging queue of outstanding events", eventQueue.Count);
                    ClearQueue();
                }

                var shouldUnqueuePredicates = providers
                    .Select(provider => provider.isDuplicatePredicate)
                    .ToList();
                // Remove duplicate events
                // As we process the queue when the animation is ready, we should not have a race condition 
                // here, but it's still risky (vs preventing the insertion if a future event is a duplicate, but 
                // which requires a lot of reengineering of the loop)
                if (eventQueue != null
                    && eventQueue.Count > 0
                    && shouldUnqueuePredicates != null
                    && shouldUnqueuePredicates.Count > 0)
                {
                    //Logger.Log("Before culling dupes", eventQueue.Count);
                    eventQueue = eventQueue
                        .Where(queued => queued != null)
                        .Where((queued) => !shouldUnqueuePredicates.Any((predicate) => predicate(queued)))
                        .ToList();
                    //Logger.Log("After culling dupes", eventQueue.Count);
                }
                eventQueue.AddRange(providers);
                // Don't touch the start/stop dev mode processors
                var startDevModeIndex = eventQueue.FindIndex(item => item is StartDevModeProvider);
                var stopDevModeIndex = eventQueue.FindIndex(item => item is StopDevModeProvider);
                //Logger.Log("Found start and dev mode index", startDevModeIndex + " // " + stopDevModeIndex);
                eventQueue = eventQueue
                    .Where(p => !(p is StartDevModeProvider))
                    .Where(p => !(p is StopDevModeProvider))
                    .OrderByDescending(p => p.ShortCircuit)
                    .ThenBy(p => p.Timestamp)
                    .ThenBy(p => p.Index)
                    .ToList();
                if (startDevModeIndex >= 0)
                {
                    eventQueue.Insert(0, new StartDevModeProvider());
                }
                if (stopDevModeIndex >= 0)
                {
                    eventQueue.Insert(Math.Min(stopDevModeIndex, eventQueue.Count - 1), new StopDevModeProvider());
                }
                //Logger.Log("Enqueued game event", providers != null ? providers[0].CreationLogLine : null);
            }
        }

        public void ReceiveAnimationLog(string data)
        {
            lock (listLock)
            {
                //Logger.Log("Acquierd list lock in receiveanimationlog", "");
                //if (data.Contains("BOT_535"))
                //{
                //    Logger.Log("[csharp] ready for animation processing ", data);
                //    eventQueue.ForEach(provider => Logger.Log("\t[csharp] In queue", provider.CreationLogLine));
                //}
                if (eventQueue.Count > 0)
                {
                    var readyProviders = new List<string>();
                    foreach (GameEventProvider provider in eventQueue)
                    {
                        //var debug = data.Contains("BOT_535") && provider.CreationLogLine.Contains("BOT_535");
                        //provider.debug = debug;
                        //if (debug)
                        //{
                        //    Logger.Log("[csharp] Will debuggg provider", provider.CreationLogLine);
                        //}
                        // Some events are recurring and have the same activation line (mostly those linked 
                        // to the game entity), so we do this to not mark several animations as ready
                        // from the same power log
                        if (readyProviders.Contains(provider.EventName))
                        {
                            //if (debug)
                            //{
                            //    Logger.Log("[csharp] animation already ready ", readyProviders);
                            //}
                            continue;
                        }
                        var animationNowReady = provider.ReceiveAnimationLog(data, ParserState);
                        //if (debug)
                        //{
                        //    Logger.Log("[csharp] animationNowReady", animationNowReady);
                        //}
                        if (animationNowReady)
                        {
                            //if (data.Contains("BOT_535"))
                            //{
                            //    Logger.Log("[csharp] animation ready " + provider.EventName, data);
                            //}
                            readyProviders.Add(provider.EventName);
                        }
                    }
                }
                else
                {
                    //Logger.Log("event queue is empty", eventQueue.Count);
                }
            }
        }

        public void ClearQueue()
        {
            lock (listLock)
            {
                //Logger.Log("Acquierd list lock in clearqueue", "");
                // We process all pending events. It can happen (typically with the Hearthstone spell) that 
                // not all events receive their animation log, so we do it just to be sure there aren't any 
                // left overs
                eventQueue.ForEach(provider => ProcessGameEvent(provider));
                eventQueue.Clear();
            }
        }

        // Not sure what that second condition is about, but these logs are all over the place in 
        // Battlegrounds, and are not specific to anything, so we can't really use them
        // as indicators that things have progressed
        private List<string> ignoredLogLines = new List<string>()
        {
            "EffectCardId=System.Collections.Generic.List`1[System.String] EffectIndex=-1 Target=0 SubOption=-1 TriggerKeyword=0",
            "EffectCardId=System.Collections.Generic.List`1[System.String] EffectIndex=6 Target=0 SubOption=-1 TriggerKeyword=0",

        };
        private async void ProcessGameEventQueue(Object source, ElapsedEventArgs e)
        {
            // If both the first events has just been added, wait a bit, so that we're sure there's no 
            // other event that should be processed first
            // Warning: this means the whole event parsing works in real-time, and is not suited for 
            // post-processing of games
            //if (eventQueue.Any(p => p.CreationLogLine.Contains("TAG_CHANGE Entity=[entityName=Voidwalker id=3651 zone=PLAY zonePos=3 cardId=CS2_065 player=13] tag=ZONE value=REMOVEDFROMGAME")))
            //{
            //    Logger.Log("Provider to process", eventQueue[0].CreationLogLine);
            //}
            while (IsEventToProcess())
            {
                //Logger.Log("There is an event to process", eventQueue.Count == 0 ? "nothing" : eventQueue[0].CreationLogLine);
                try
                {
                    GameEventProvider provider;
                    lock (listLock)
                    {
                        //Logger.Log("Acquierd list lock in processgameevent", "");
                        if (eventQueue.Count == 0)
                        {
                            //Logger.Log("Queue empty", "");
                            return;
                        }
                        if (eventQueue.First() is StartDevModeProvider)
                        {
                            NodeParser.DevMode = true;
                            eventQueue.RemoveAt(0);
                            Logger.Log("Setting DevMode", DevMode);
                            continue;
                        }
                        if (eventQueue.First() is StopDevModeProvider)
                        {
                            NodeParser.DevMode = false;
                            eventQueue.RemoveAt(0);
                            Logger.Log("Setting DevMode", DevMode);
                            continue;
                        }
                        //Logger.Log("[csharp] Events to process", eventQueue.Count);
                        // TODO: this spoils events in BGS, how to do it?
                        // We don't use the other form, as in BGS some lines are very similar and could trigger some false
                        // animation ready calls (more specifically, things related to the GameEntity, like MAIN_STEP)
                        //if (!eventQueue.All(p => !p.CreationLogLine.Contains("GameEntity")) 
                        //    && !eventQueue.Where(p => !p.CreationLogLine.Contains("GameEntity")).Any(p => p.AnimationReady))
                        // Heck for Battlegrounds
                        if (!DevMode 
                            && !eventQueue.First().ShortCircuit
                            && !eventQueue
                                .Where(p => !(p.CreationLogLine.Contains("GameEntity") && p.CreationLogLine.Contains("MAIN_READY")))
                                .Where(p => !p.CreationLogLine.Contains("BLOCK_START BlockType=TRIGGER") 
                                    && ignoredLogLines.All(line => !p.CreationLogLine.Contains(line)))
                                .Where(p => p.EventName != "ENTITY_UPDATE")
                                .Any(p => p.AnimationReady))
                        // Safeguard - Don't wait too long for the animation in case we never receive it
                        // With the arrival of Battlegrounds we can't do this anymore, as it spoils the game very fast
                        //&& DateTimeOffset.UtcNow.Subtract(eventQueue.First().Timestamp).TotalMilliseconds < 5000)
                        {
                            //Logger.Log("No animation ready", eventQueue[0].CreationLogLine);
                            return;
                        }
                        provider = eventQueue[0];
                        eventQueue.RemoveAt(0);
                        if (provider.debug)
                        {
                            Logger.Log("Will process event", provider.EventName);
                            Logger.Log("creationLogLine", provider.CreationLogLine);
                            Logger.Log("animatiuonReady", provider.AnimationReady);
                            Logger.Log("ShortCircuit", provider.ShortCircuit);
                            Logger.Log("First event queue ShortCircuit", eventQueue.First().ShortCircuit);
                            var animationReady = eventQueue
                                .Where(p => !(p.CreationLogLine.Contains("GameEntity") && p.CreationLogLine.Contains("MAIN_READY")))
                                .Where(p => !p.CreationLogLine.Contains("BLOCK_START BlockType=TRIGGER")
                                    && ignoredLogLines.All(line => !p.CreationLogLine.Contains(line)))
                                .Where(p => p.EventName != "ENTITY_UPDATE")
                                .ToList();
                            Logger.Log("First event queue animationReady", animationReady.Any(p => p.AnimationReady));
                            Logger.Log("First event queue animationReady event", animationReady.Any(p => p.AnimationReady) 
                                ? animationReady.First().CreationLogLine : null );

                        }
                    }
                    //if (provider.CreationLogLine.Contains("TAG_CHANGE Entity=[entityName=Voidwalker id=3651 zone=PLAY zonePos=3 cardId=CS2_065 player=13] tag=ZONE value=REMOVEDFROMGAME"))
                    //{
                    //    Logger.Log("Provider to process 1.1", provider.NeedMetaData + " // " + ParserState.CurrentGame.FormatType 
                    //        + " // " + ParserState.CurrentGame.GameType + " // " + ParserState.LocalPlayer);
                    //}
                    if (provider.NeedMetaData)
                    {
                        // Wait until we have all the necessary data
                        while (ParserState.CurrentGame.FormatType == -1 || ParserState.CurrentGame.GameType == -1 || ParserState.LocalPlayer == null)
                        {
                            //Logger.Log("[csharp] waiting for metadata", "");
                            await Task.Delay(100);
                        }
                    }
                    if (provider.debug)
                    {
                        Logger.Log("[csharp] Will process next event " + provider.CreationLogLine, provider.AnimationReady);
                        Logger.Log("[csharp] Next animation ready ", eventQueue.Find(p => p.AnimationReady)?.CreationLogLine + " // "
                            + eventQueue.Find(p => p.AnimationReady)?.GameEvent.Type);
                    }
                    lock (listLock)
                    {
                        //Logger.Log("Acquierd list lock in processgameevent 2", "");
                        ProcessGameEvent(provider);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log("Exception while parsing event queue " + ex.Message, ex.StackTrace);
                    return;
                }
            }
        }

        private void ProcessGameEvent(GameEventProvider provider)
        {
            if (provider is StartDevModeProvider)
            {
                NodeParser.DevMode = true;
                Logger.Log("Setting DevMode", DevMode);
                return;
            }
            if (provider is StopDevModeProvider)
            {
                NodeParser.DevMode = false;
                Logger.Log("Setting DevMode", DevMode);
                return;
            }
            if (provider.SupplyGameEvent == null && provider.GameEvent == null)
            {
                return;
            }
            var gameEvent = provider.GameEvent != null ? provider.GameEvent : provider.SupplyGameEvent();
            //if (provider.CreationLogLine.Contains("3651")) // == "TAG_CHANGE Entity=[entityName=Voidwalker id=3651 zone=PLAY zonePos=3 cardId=CS2_065 player=13] tag=ZONE value=REMOVEDFROMGAMETAG_CHANGE Entity=[entityName=Voidwalker id=3651 zone=PLAY zonePos=3 cardId=CS2_065 player=13] tag=ZONE value=REMOVEDFROMGAME")
            //{
            //    Logger.Log("Handling debug provider", provider.CreationLogLine);
            //}
            if (provider.debug)
            {
                Logger.Log("[csharp] should provide event? " + (gameEvent != null), provider.CreationLogLine + " // " + provider.AnimationReady);
                Logger.Log(
                    "[csharp] animation ready stuff",
                    string.Join("\\n", eventQueue.Where(p => p.AnimationReady).Select(p => p.CreationLogLine)));
            }
            // This can happen because there are some conditions that are only resolved when we 
            // have the full meta data, like dungeon run step
            if (gameEvent != null)
            {
                if (provider.debug)
                {
                    Logger.Log("[csharp] Handling event", gameEvent.Type);
                }
                GameEventHandler.Handle(gameEvent);
                //if (gameEvent.Type == "GAME_END")
                //{   
                //    while (eventQueue.Count > 0)
                //    {
                //        provider = eventQueue[0];
                //        eventQueue.RemoveAt(0);
                //        ProcessGameEvent(provider);
                //    }
                //}
                //Logger.Log(DateTime.Now, "Handled event " + provider.Timestamp + " " + provider.CreationLogLine);
            }
            else
            {
                //Logger.Log("[csharp] Game event is null, so doing nothing", provider.CreationLogLine);
            }
        }

        private bool IsEventToProcess()
        {
            try
            {
                var isEvent = false;

                lock (listLock)
                {
                    //Logger.Log("Acquierd list lock in iseventtoprocess", "");
                    // We leave some time so that events parsed later can be processed sooner (typiecally the case 
                    // for end-of-block events vs start-of-block events, like tag changes)
                    isEvent = eventQueue.Count > 0
                        && (
                            DevMode
                            || eventQueue.First() is StartDevModeProvider
                            || eventQueue.First().ShortCircuit
                            || DateTime.Now.Subtract(eventQueue.First().Timestamp).TotalMilliseconds > 500);
                    //if (eventQueue.Count > 0)
                    //{
                    //    //    Logger.Log("Is event to process? " + isEvent, DateTime.Now + " // "
                    //    //        + eventQueue.First().Timestamp
                    //    //        + " // " + DateTime.Now.Subtract(eventQueue.First().Timestamp).TotalMilliseconds);
                    //    Logger.Log("Is event to process? " + isEvent, eventQueue[0].CreationLogLine);
                    //}
                }
                //if (eventQueue.Count > 0 && !isEvent)
                //{
                //    Logger.Log("[csharp] too soon to process events", eventQueue.First().CreationLogLine);
                //    Logger.Log(DateTime.Now.Subtract(eventQueue.First().Timestamp).TotalMilliseconds, "");
                //    Logger.Log(DateTime.Now, eventQueue.First().Timestamp);
                //}
                return isEvent;
            }
            catch (Exception ex)
            {
                Logger.Log(ex.StackTrace, "" + eventQueue.Count);
                Logger.Log("Exception while trying to determine event to process", ex.Message);
                return false;
            }
        }

        private List<ActionParser> BuildActionParsers(ParserState ParserState)
        {
            return new List<ActionParser>()
            {
                new NewGameParser(),
                new WinnerParser(ParserState),
                new GameEndParser(ParserState),
                new TurnStartParser(ParserState),
                new FirstPlayerParser(ParserState),
                new MainStepReadyParser(ParserState),
                new CardPlayedFromHandParser(ParserState),
                new SecretPlayedFromHandParser(ParserState),
                new MulliganInputParser(ParserState),
                new MulliganDealingParser(ParserState),
                new MulliganDoneParser(ParserState),
                new RumbleRunStepParser(ParserState),
                new DungeonRunStepParser(ParserState),
                new MonsterRunStepParser(ParserState),
                new PassiveBuffParser(ParserState),
                new CardPresentOnGameStartParser(ParserState),
                new CardDrawFromDeckParser(ParserState),
                new ReceiveCardInHandParser(ParserState),
                new CardBackToDeckParser(ParserState),
                new DiscardedCardParser(ParserState),
                new CardRemovedFromDeckParser(ParserState),
                new CreateCardInDeckParser(ParserState),
                new EndOfEchoInHandParser(ParserState),
                new CardChangedParser(ParserState),
                new CardUpdatedInDeckParser(ParserState),
                new CardRemovedFromHandParser(ParserState),
                new CardRemovedFromBoardParser(ParserState),
                new MinionOnBoardAttackUpdatedParser(ParserState),
                new RecruitParser(ParserState),
                new MinionBackOnBoardParser(ParserState),
                new CardRevealedParser(ParserState),
                new InitialCardInDeckParser(ParserState),
                new MinionSummonedParser(ParserState),
                new FatigueParser(ParserState),
                new DamageParser(ParserState),
                new HealingParser(ParserState),
                new BurnedCardParser(ParserState),
                new MinionDiedParser(ParserState),
                new SecretPlayedFromDeckParser(ParserState),
                new SecretCreatedInGameParser(ParserState),
                new SecretDestroyedParser(ParserState),
                new ArmorChangeParser(ParserState),
                new CardStolenParser(ParserState),
                new SecretTriggeredParser(ParserState),
                new DeathrattleTriggeredParser(ParserState),
                new HealthDefChangeParser(ParserState),
                new ChangeCardCreatorParser(ParserState),
                new LocalPlayerLeaderboardPlaceChangedParser(ParserState),
                new HeroPowerChangedParser(ParserState),
                new WeaponEquippedParser(ParserState),
                new WeaponDestroyedParser(ParserState),
                new BattlegroundsPlayerBoardParser(ParserState),
                new BattlegroundsPlayerTechLevelUpdatedParser(ParserState),
                new BattlegroundsPlayerLeaderboardPlaceUpdatedParser(ParserState),
                new BattlegroundsHeroSelectionParser(ParserState),
                new BattlegroundsNextOpponnentParser(ParserState),
                new BattlegroundsTriplesCountUpdatedParser(ParserState),
                //new BattlegroundsStartOfCombatParser(ParserState),
                new BattlegroundsOpponentRevealedParser(ParserState),
                new BattlegroundsHeroSelectedParser(ParserState),
                new BattlegroundsBattleOverParser(ParserState),
                new DecklistUpdateParser(ParserState),
                new GameRunningParser(ParserState),
                new AttackParser(ParserState),
                new NumCardsPlayedThisTurnParser(ParserState),
                new HeroPowerUsedParser(ParserState),
                new GalakrondInvokedParser(ParserState),
                new CardBuffedInHandParser(ParserState),
                new MinionGoDormantParser(ParserState),
                new BlockEndParser(ParserState),
                new JadeGolemParser(ParserState),
                new CthunParser(ParserState),
                new EntityUpdateParser(ParserState),
            };
        }
    }
}