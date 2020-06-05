using HearthstoneReplays.Parser;
using HearthstoneReplays.Parser.ReplayData;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using System;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.Entities;
using System.Collections.Generic;
using System.Linq;
using HearthstoneReplays.Parser.ReplayData.Meta;

namespace HearthstoneReplays.Events.Parsers
{
    public class CardBuffedInHandParser : ActionParser
    {
        private GameState GameState { get; set; }
        private ParserState ParserState { get; set; }

        private List<string> validBuffers = new List<string>()
        {
            //CardIds.Collectible.Druid.DreampetalFlorist,
            //CardIds.Collectible.Druid.ImprisonedSatyr, 
            CardIds.Collectible.Hunter.ScavengersIngenuity, // not tested
            //CardIds.Collectible.Hunter.Emeriss,
            //CardIds.Collectible.Hunter.ForlornStalker,
            CardIds.Collectible.Hunter.FreezingTrap,
            //CardIds.Collectible.Hunter.Helboar,
            CardIds.Collectible.Hunter.HiddenCache,
            //CardIds.Collectible.Hunter.ScarletWebweaver,
            //CardIds.Collectible.Hunter.ScrapShot,
            CardIds.Collectible.Hunter.ShakyZipgunner,
            CardIds.Collectible.Hunter.SmugglersCrate,
            CardIds.Collectible.Hunter.TroggBeastrager,
            //CardIds.Collectible.Mage.ManaBind, // Redundant with creator
            //CardIds.Collectible.Mage.NagaSandWitch,
            //CardIds.Collectible.Mage.UnstablePortal, // Redundant with creator
            //CardIds.Collectible.Paladin.DragonriderTalritha, 
            //CardIds.Collectible.Paladin.DragonSpeaker,
            //CardIds.Collectible.Paladin.FarrakiBattleaxe,
            //CardIds.Collectible.Paladin.GlowstoneTechnician,
            CardIds.Collectible.Paladin.GrimscaleChum,
            CardIds.Collectible.Paladin.GrimestreetEnforcer,
            CardIds.Collectible.Paladin.GrimestreetOutfitter,
            CardIds.Collectible.Paladin.SmugglersRun,
            CardIds.Collectible.Paladin.Valanyr, // not tested
            CardIds.Collectible.Priest.FateWeaver,
            //CardIds.Collectible.Rogue.AnkaTheBuried, 
            CardIds.Collectible.Rogue.BlackjackStunner,
            CardIds.Collectible.Rogue.CheatDeath,
            CardIds.Collectible.Rogue.Shadowcaster,
            CardIds.Collectible.Rogue.Shadowstep,
            CardIds.Collectible.Rogue.SonyaShadowdancer,
            CardIds.Collectible.Rogue.WagglePick,
            CardIds.Collectible.Shaman.BogSlosher,
            //CardIds.Collectible.Shaman.FirePlumeHarbinger, 
            CardIds.Collectible.Shaman.GrumbleWorldshaker,
            //CardIds.Collectible.Shaman.TheMistcaller,
            //CardIds.Collectible.Warlock.ShadowCouncil, // Redundant with creator
            CardIds.Collectible.Warlock.ClutchmotherZavas,
            //CardIds.Collectible.Warlock.ImprisonedScrapImp
            //CardIds.Collectible.Warlock.SoulInfusion, 
            //CardIds.Collectible.Warlock.SpiritOfTheBat, 
            CardIds.Collectible.Warlock.TheDarkPortal,
            //CardIds.Collectible.Warlock.VoidAnalyst,
            //CardIds.Collectible.Warrior.Armagedillo, 
            CardIds.Collectible.Warrior.BrassKnuckles,
            //CardIds.Collectible.Warrior.BringItOn, 
            CardIds.Collectible.Warrior.GrimestreetPawnbroker,
            CardIds.Collectible.Warrior.GrimyGadgeteer,
            //CardIds.Collectible.Warrior.HobartGrapplehammer, 
            //CardIds.Collectible.Warrior.IntoTheFray, 
            CardIds.Collectible.Warrior.StolenGoods,
            //CardIds.Collectible.Neutral.BlubberBaron,
            //CardIds.Collectible.Neutral.AnubisathWarbringer,
            //CardIds.Collectible.Neutral.ArenaFanatic, 
            //CardIds.Collectible.Neutral.CorridorCreeper,
            CardIds.Collectible.Neutral.DonHancho,  // not tested
            //CardIds.Collectible.Neutral.DeathaxePunisher,
            //CardIds.Collectible.Neutral.DragonqueenAlexstrasza,  // redundant with creator
            CardIds.Collectible.Neutral.EmperorThaurissan, 
            //CardIds.Collectible.Neutral.EbonDragonsmith, 
            //CardIds.Collectible.Neutral.Galvanizer, 
            CardIds.Collectible.Neutral.GrimestreetSmuggler,
            //CardIds.Collectible.Neutral.HelplessHatchling, 
            //CardIds.Collectible.Neutral.HistoryBuff,
            //CardIds.Collectible.Neutral.TastyFlyfish, 
        };

        public CardBuffedInHandParser(ParserState ParserState)
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
            var isPower = node.Type == typeof(Parser.ReplayData.GameActions.Action)
                 && (node.Object as Parser.ReplayData.GameActions.Action).Type == (int)BlockType.POWER;
            var isTrigger = node.Type == typeof(Parser.ReplayData.GameActions.Action)
                 && (node.Object as Parser.ReplayData.GameActions.Action).Type == (int)BlockType.TRIGGER;
            return isPower || isTrigger;
        }

        public List<GameEventProvider> CreateGameEventProviderFromNew(Node node)
        {
            return null;
        }

        public List<GameEventProvider> CreateGameEventProviderFromClose(Node node)
        {
            var action = node.Object as Parser.ReplayData.GameActions.Action;
            return CreateEventProviderForAction(node, node.CreationLogLine);
        }

        private List<GameEventProvider> CreateEventProviderForAction(Node node, string creationLogLine)
        {
            var action = node.Object as Parser.ReplayData.GameActions.Action;
            if (!GameState.CurrentEntities.ContainsKey(action.Entity))
            {
                Logger.Log("Missing entity key", "" + action.Entity);
                return null;
            }
            var actionEntity = GameState.CurrentEntities[action.Entity];
            var bufferCardId = actionEntity.CardId;
            // Because some cards have an animation that reveal the buffed cards, and others don't, 
            // we have to whitelist the valid cards to avoid info leaks
            if (!validBuffers.Contains(bufferCardId))
            {
                return null;
            }
            var entitiesBuffedInHand = action.Data
                .Where(data => data.GetType() == typeof(MetaData))
                .Select(data => data as MetaData)
                .Where(meta => meta.Meta == (int)MetaDataType.TARGET)
                .SelectMany(meta => meta.MetaInfo)
                .Select(info => GameState.CurrentEntities[info.Entity])
                .Where(entity => entity.GetTag(GameTag.ZONE) == (int)Zone.HAND)
                .ToList();
            if (entitiesBuffedInHand.Count == 0)
            {
                return null;
            }
            var controllerId = actionEntity.GetTag(GameTag.CONTROLLER);
            var result = entitiesBuffedInHand
                .Select(entity =>
                {
                    return GameEventProvider.Create(
                        action.TimeStamp,
                        "CARD_BUFFED_IN_HAND",
                        GameEvent.CreateProvider(
                            "CARD_BUFFED_IN_HAND",
                            entity.CardId,
                            entity.GetTag(GameTag.CONTROLLER),
                            entity.Entity,
                            ParserState,
                            GameState,
                            null,
                            new
                            {
                                BuffingEntityCardId = actionEntity.CardId,
                            }),
                        true,
                        creationLogLine);
                })
                .ToList();
            return result;
        }
    }
}
