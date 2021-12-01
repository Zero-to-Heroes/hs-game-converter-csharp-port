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

        private bool waitingForMetaData;

        private readonly Object listLock = new object();

        public NodeParser()
        {
            eventQueue = new List<GameEventProvider>();
            timer = new Timer(100);
            timer.Elapsed += ProcessGameEventQueue;
            timer.AutoReset = true;
            timer.Enabled = true;
        }

        public void Reset(ParserState ParserState)
        {
            this.ParserState = ParserState;
            parsers = BuildActionParsers(ParserState);
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
            //await Task.Delay(5000);
            lock (listLock)
            {
                Logger.Log("Enqueuing stop dev mode", eventQueue.Count);
                eventQueue.Add(new StopDevModeProvider());
            }
        }

        public void NewNode(Node node)
        {
            if (node == null)
            {
                return;
            }
            foreach (ActionParser parser in parsers)
            {
                if (parser.AppliesOnNewNode(node))
                {
                    List<GameEventProvider> providers = parser.CreateGameEventProviderFromNew(node);
                    if (providers != null)
                    {
                        EnqueueGameEvent(providers);
                    }
                }
            }
        }

        public void CloseNode(Node node)
        {
            if (node == null)
            {
                return;
            }
            foreach (ActionParser parser in parsers)
            {
                if (!node.Closed && parser.AppliesOnCloseNode(node))
                {
                    List<GameEventProvider> providers = parser.CreateGameEventProviderFromClose(node);
                    if (providers != null && providers.Count > 0)
                    {
                        EnqueueGameEvent(providers);
                    }
                }
            }
            // Make sure we don't process the same node twice
            node.Closed = true;
        }

        public void EnqueueGameEvent(List<GameEventProvider> providers)
        {
            providers = providers.Where(provider => provider != null).ToList();
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
                if (eventQueue.Count > 0)
                {
                    var readyProviders = new List<string>();
                    foreach (GameEventProvider provider in eventQueue)
                    {
                        // Some events are recurring and have the same activation line (mostly those linked 
                        // to the game entity), so we do this to not mark several animations as ready
                        // from the same power log
                        if (readyProviders.Contains(provider.EventName))
                        {
                            continue;
                        }
                        var animationNowReady = provider.ReceiveAnimationLog(data, ParserState);
                        if (animationNowReady)
                        {
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
            "EffectCardId=System.Collections.Generic.List`1[System.String] EffectIndex=-1 Target=0 SubOption=-1 TriggerKeyword=TAG_NOT_SET",
            "EffectCardId=System.Collections.Generic.List`1[System.String] EffectIndex=0 Target=0 SubOption=-1 TriggerKeyword=0",
            "EffectCardId=System.Collections.Generic.List`1[System.String] EffectIndex=0 Target=0 SubOption=-1 TriggerKeyword=TAG_NOT_SET",
            "EffectCardId=System.Collections.Generic.List`1[System.String] EffectIndex=4 Target=0 SubOption=-1 TriggerKeyword=TAG_NOT_SET",
            "EffectCardId=System.Collections.Generic.List`1[System.String] EffectIndex=5 Target=0 SubOption=-1 TriggerKeyword=TAG_NOT_SET",

        };

        private bool processing;
        private async void ProcessGameEventQueue(Object source, ElapsedEventArgs e)
        {
            if (processing)
            {
                return;
            }
            // If both the first events has just been added, wait a bit, so that we're sure there's no 
            // other event that should be processed first
            // Warning: this means the whole event parsing works in real-time, and is not suited for 
            // post-processing of games
            // TODO: this is starting to become a big hack. It might be better to rethink the whole event 
            // processing, and rely only the on the PowerTaskList instead of the GameState, so that I 
            // can get rid of the timing shennanigans
            // What the GameState processing is good for:
            // - Know ahead of time what will happen (eg MINION_WILL_DIE). Maybe this can be worked around by waiting for a short while in some cases
            // - Start the BG simulation earlier  (this one might be big for me)
            // - There are some stuff that are only present in the GS logs (metadata, player names) (this can be solved by running on the GS first, once)
            while (IsEventToProcess())
            {
                processing = true;
                try
                {
                    GameEventProvider provider;
                    lock (listLock)
                    {
                        if (eventQueue.Count == 0 || ParserState == null)
                        {
                            //Logger.Log("No event", "");
                            processing = false;
                            return;
                        }
                        if (eventQueue.First() is StartDevModeProvider)
                        {
                            NodeParser.DevMode = true;
                            eventQueue.RemoveAt(0);
                            Logger.Log("Setting DevMode", DevMode);
                            processing = false;
                            continue;
                        }
                        if (eventQueue.First() is StopDevModeProvider)
                        {
                            NodeParser.DevMode = false;
                            eventQueue.RemoveAt(0);
                            Logger.Log("Setting DevMode", DevMode);
                            GameEventHandler.Handle(null, NodeParser.DevMode);
                            processing = false;
                            continue;
                        }
                        // TODO: this spoils events in BGS, how to do it?
                        // We don't use the other form, as in BGS some lines are very similar and could trigger some false
                        // animation ready calls (more specifically, things related to the GameEntity, like MAIN_STEP)
                        // So that things don't break while in DevMode
                        if (waitingForMetaData && !eventQueue.First().ShortCircuit)
                        {
                            //Logger.Log("Waiting for metadata", "");
                            processing = false;
                            return;
                        }
                        // Heck for Battlegrounds
                        if (!DevMode
                            && !eventQueue.First().ShortCircuit
                            && !eventQueue
                                .Where(p => !(p.CreationLogLine.Contains("GameEntity") && p.CreationLogLine.Contains("MAIN_READY")))
                                .Where(p => !p.CreationLogLine.Contains("BLOCK_START BlockType=TRIGGER")
                                    && ignoredLogLines.All(line => !p.CreationLogLine.Contains(line)))
                                // ENTITTY_UPDATE events are needed for mindrender illucia
                                // But I have no idea why they were removed in the first place
                                // So here I'm using a crutch to make it work just for this specific case
                                // I think they were excluded for BG
                                .Where(p => p.EventName != "ENTITY_UPDATE" || ((dynamic)p.Props)?.Mindrender)
                                .Where(p => !ParserState.IsBattlegrounds() || p.EventName != "CARD_REMOVED_FROM_DECK")
                                // In some cases, the TURN_START event is processed before the combat ends. I don't know why this
                                // is the case though, so this exclusion is just to try and not have it be processed too soon.
                                // This is NOT a good fix, but I don't understand the underlying cause, so...
                                // I've noticed this happen mostly against Greybough (or at least, more consistently)
                                .Where(p => !p.CreationLogLine.Contains("CardID=TB_BaconShop_3ofKindChecke"))
                                .Any(p => p.AnimationReady))
                        // Safeguard - Don't wait too long for the animation in case we never receive it
                        // With the arrival of Battlegrounds we can't do this anymore, as it spoils the game very fast
                        //&& DateTimeOffset.UtcNow.Subtract(eventQueue.First().Timestamp).TotalMilliseconds < 5000)
                        {
                            //Logger.Log("No event suitable event: " + eventQueue.First().EventName, "" + eventQueue.Count);
                            processing = false;
                            return;
                        }

                        provider = eventQueue[0];
                        eventQueue.RemoveAt(0);
                    }
                    if (provider.NeedMetaData)
                    {
                        waitingForMetaData = true;
                        // Wait until we have all the necessary data
                        while (ParserState.CurrentGame.FormatType == -1 || ParserState.CurrentGame.GameType == -1 || ParserState.LocalPlayer == null)
                        {
                            //Logger.Log("Awaiting metadata", "");
                            await Task.Delay(100);
                        }
                        waitingForMetaData = false;
                    }
                    lock (listLock)
                    {
                        ProcessGameEvent(provider);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log("Exception while parsing event queue " + ex.Message, ex.StackTrace);
                    processing = false;
                    return;
                }
            }
            processing = false;
        }

        private void ProcessGameEvent(GameEventProvider provider)
        {
            if (provider is StartDevModeProvider)
            {
                NodeParser.DevMode = true;
                Logger.Log("Setting DevMode", DevMode);
                return;
            }
            else if (provider is StopDevModeProvider)
            {
                NodeParser.DevMode = false;
                Logger.Log("Setting DevMode", DevMode);
                return;
            }
            else if (provider.SupplyGameEvent == null && provider.GameEvent == null)
            {
                //Logger.Log("No game event", "");
                return;
            }
            var gameEvent = provider.GameEvent != null ? provider.GameEvent : provider.SupplyGameEvent();
            // This can happen because there are some conditions that are only resolved when we 
            // have the full meta data, like dungeon run step
            if (gameEvent != null)
            {
                //Logger.Log("Handling game event", gameEvent.Type);
                GameEventHandler.Handle(gameEvent, NodeParser.DevMode);
            }
        }

        private bool IsEventToProcess()
        {
            try
            {
                var isEvent = false;
                lock (listLock)
                {
                    // We leave some time so that events parsed later can be processed sooner (typiecally the case 
                    // for end-of-block events vs start-of-block events, like tag changes)
                    isEvent = eventQueue.Count > 0
                        && (
                            DevMode
                            || eventQueue.First() is StartDevModeProvider
                            || eventQueue.First().ShortCircuit
                            || DateTime.Now.Subtract(eventQueue.First().Timestamp).TotalMilliseconds > 500);
                }
                //Logger.Log("Is event to process? " + isEvent + " // " + eventQueue.Count, 
                //    eventQueue.Count > 0 ? "" + DateTime.Now.Subtract(eventQueue.First().Timestamp).TotalMilliseconds : "");
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
                new NewGameParser(ParserState),

                // Ordering is important, as we want to have "ability revealed" before 
                // "ability updated" (done by the EntityUpdateParser)
                // Also, MINION_SUMMONED need to happen before any Equipment / Ability revealed
                new MinionSummonedParser(ParserState),

                new MercenariesHeroRevealed(ParserState),
                new MercenariesAbilityRevealedParser(ParserState),
                new MercenariesAbilityActivatedParser(ParserState),
                new MercenariesAbilityCooldownUpdatedParser(ParserState),
                new MercenariesQueuedAbilityParser(ParserState),

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
                new BattlegroundsRerollParser(ParserState),
                new BattlegroundFreezeParser(ParserState),
                new BattlegroundsMinionsBoughtParser(ParserState),
                new BattlegroundsMinionsSoldParser(ParserState),
                new BattlegroundsHeroKilledParser(ParserState),
                new DecklistUpdateParser(ParserState),
                new GameRunningParser(ParserState),
                new AttackParser(ParserState),
                new NumCardsPlayedThisTurnParser(ParserState),
                new NumCardsDrawnThisTurnParser(ParserState),
                new HeroPowerUsedParser(ParserState),
                new GalakrondInvokedParser(ParserState),
                new CardBuffedInHandParser(ParserState),
                new MinionGoDormantParser(ParserState),
                new BlockEndParser(ParserState),
                new JadeGolemParser(ParserState),
                new CthunParser(ParserState),
                new EntityUpdateParser(ParserState),
                new ResourcesThisTurnParser(ParserState),
                new ResourcesUsedThisTurnParser(ParserState),
                new WhizbangDeckParser(ParserState),
                new CopiedFromEntityIdParser(ParserState),
                new BattlegroundsTavernPrizesParser(ParserState),
                new LinkedEntityParser(ParserState),
                new ZoneChangeParser(ParserState),
                new ZonePositionChangedParser(ParserState),
                new CostChangedParser(ParserState),


                new CreateCardInGraveyardParser(ParserState),
                new MindrenderIlluciaParser(ParserState),
            };
        }
    }
}