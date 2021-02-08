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

        // When adding an entity to this list, also add the corresponding buff in the map below
        private List<string> validBuffers = new List<string>()
        {
            CardIds.Collectible.Demonhunter.SkullOfGuldan,
            //CardIds.Collectible.Druid.DreampetalFlorist,
            //CardIds.Collectible.Druid.ImprisonedSatyr, 
            CardIds.Collectible.Druid.PredatoryInstincts,
            //CardIds.Collectible.Hunter.Emeriss,
            //CardIds.Collectible.Hunter.ForlornStalker,
            CardIds.Collectible.Hunter.FreezingTrap,
            //CardIds.Collectible.Hunter.Helboar,
            CardIds.Collectible.Hunter.HiddenCache,
            //CardIds.Collectible.Hunter.ScarletWebweaver,
            CardIds.Collectible.Hunter.ScavengersIngenuity,
            //CardIds.Collectible.Hunter.ScrapShot,
            CardIds.Collectible.Hunter.ShakyZipgunner,
            CardIds.Collectible.Hunter.SmugglersCrate,
            CardIds.Collectible.Hunter.TroggBeastrager,
            //CardIds.Collectible.Mage.ManaBind, // Redundant with creator
            //CardIds.Collectible.Mage.NagaSandWitch,
            //CardIds.Collectible.Mage.UnstablePortal, // Redundant with creator
            CardIds.Collectible.Paladin.CallToAdventure, 
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
            CardIds.Collectible.Priest.Shadowfiend,
            //CardIds.Collectible.Rogue.AnkaTheBuried, 
            CardIds.Collectible.Rogue.BlackjackStunner,
            CardIds.Collectible.Rogue.CheatDeath,
            CardIds.Collectible.Rogue.Shadowcaster,
            CardIds.Collectible.Rogue.Shadowstep,
            CardIds.Collectible.Rogue.SonyaShadowdancer,
            CardIds.Collectible.Rogue.WagglePick,
            CardIds.Collectible.Shaman.BogSlosher,
            CardIds.Collectible.Shaman.FarSight,
            //CardIds.Collectible.Shaman.FirePlumeHarbinger, 
            CardIds.Collectible.Shaman.GrumbleWorldshaker,
            //CardIds.Collectible.Shaman.TheMistcaller,
            //CardIds.Collectible.Warlock.ShadowCouncil, // Redundant with creator
            CardIds.Collectible.Warlock.ClutchmotherZavas,
            //CardIds.Collectible.Warlock.ImprisonedScrapImp
            //CardIds.Collectible.Warlock.SoulInfusion, 
            //CardIds.Collectible.Warlock.SpiritOfTheBat, 
            CardIds.Collectible.Warlock.TheDarkPortal,
            CardIds.NonCollectible.Warlock.SupremeArchaeology_TomeOfOrigination,
            //CardIds.Collectible.Warlock.VoidAnalyst,
            CardIds.Collectible.Warlock.WilfredFizzlebang,
            CardIds.Collectible.Warrior.AkaliTheRhino,
            CardIds.NonCollectible.Warrior.AutoArmaments,
            //CardIds.Collectible.Warrior.Armagedillo, 
            CardIds.Collectible.Warrior.BrassKnuckles,
            //CardIds.Collectible.Warrior.BringItOn, 
            CardIds.Collectible.Warrior.CorsairCache,
            CardIds.Collectible.Warrior.GalakrondTheUnbreakable,
            CardIds.NonCollectible.Warrior.GalakrondtheUnbreakable_GalakrondTheApocalypseToken,
            CardIds.NonCollectible.Warrior.GalakrondtheUnbreakable_GalakrondAzerothsEndToken,
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

            //CardIds.Collectible.Warlock.Felosophy,
            //CardIds.Collectible.Neutral.PotionOfIllusion,
        };

        private Dictionary<string, string> buffs = new Dictionary<string, string>()
        {
            { CardIds.Collectible.Demonhunter.SkullOfGuldan, CardIds.NonCollectible.Neutral.SkullofGuldan_EmbracePowerEnchantment },
            { CardIds.Collectible.Druid.PredatoryInstincts, CardIds.NonCollectible.Neutral.PredatoryInstincts_PredatoryInstinctsEnchantment },
            { CardIds.Collectible.Hunter.FreezingTrap, CardIds.NonCollectible.Hunter.FreezingTrap_TrappedEnchantment},
            { CardIds.Collectible.Hunter.HiddenCache, CardIds.NonCollectible.Hunter.HiddenCache_SmugglingEnchantment},
            { CardIds.Collectible.Hunter.ScavengersIngenuity, CardIds.NonCollectible.Neutral.ScavengersIngenuity_PackTacticsEnchantment},
            { CardIds.Collectible.Hunter.ShakyZipgunner, CardIds.NonCollectible.Neutral.ShakyZipgunner_SmugglingEnchantment},
            { CardIds.Collectible.Hunter.SmugglersCrate, CardIds.NonCollectible.Neutral.SmugglersCrate_SmugglingEnchantment},
            { CardIds.Collectible.Hunter.TroggBeastrager, CardIds.NonCollectible.Hunter.TroggBeastrager_SmugglingEnchantment},
            { CardIds.Collectible.Paladin.CallToAdventure, CardIds.NonCollectible.Neutral.CalltoAdventure_HeroicEnchantment },
            { CardIds.Collectible.Paladin.GrimscaleChum, CardIds.NonCollectible.Neutral.GrimscaleChum_SmugglingEnchantment},
            { CardIds.Collectible.Paladin.GrimestreetEnforcer, CardIds.NonCollectible.Neutral.GrimestreetEnforcer_SmugglingEnchantment},
            { CardIds.Collectible.Paladin.GrimestreetOutfitter, CardIds.NonCollectible.Neutral.GrimestreetOutfitter_SmugglingEnchantment},
            { CardIds.Collectible.Paladin.SmugglersRun, CardIds.NonCollectible.Paladin.SmugglersRun_SmugglingEnchantment},
            { CardIds.Collectible.Paladin.Valanyr, CardIds.NonCollectible.Paladin.Valanyr_ValanyrReequipEffectDummy},
            { CardIds.Collectible.Priest.FateWeaver, CardIds.NonCollectible.Priest.FateWeaver_DraconicFateEnchantment},
            { CardIds.Collectible.Priest.Shadowfiend, CardIds.NonCollectible.Priest.Shadowfiend_ShadowfiendedEnchantment},
            { CardIds.Collectible.Rogue.BlackjackStunner, CardIds.NonCollectible.Neutral.BlackjackStunner_StunnedEnchantment},
            { CardIds.Collectible.Rogue.CheatDeath, CardIds.NonCollectible.Neutral.CheatDeath_CloseCallEnchantment},
            { CardIds.Collectible.Rogue.Shadowcaster, CardIds.NonCollectible.Neutral.Shadowcaster_FlickeringDarknessEnchantment},
            { CardIds.Collectible.Rogue.Shadowstep, CardIds.NonCollectible.Neutral.CheatDeath_CloseCallEnchantment},
            { CardIds.Collectible.Rogue.SonyaShadowdancer, CardIds.NonCollectible.Neutral.SonyaShadowdancer_SonyasShadowEnchantment},
            { CardIds.Collectible.Rogue.WagglePick, CardIds.NonCollectible.Neutral.CheatDeath_CloseCallEnchantment},
            { CardIds.Collectible.Shaman.BogSlosher, CardIds.NonCollectible.Shaman.BogSlosher_SloshedEnchantment},
            { CardIds.Collectible.Shaman.FarSight, CardIds.NonCollectible.Shaman.FarSight_FarSightEnchantment},
            { CardIds.Collectible.Shaman.GrumbleWorldshaker, CardIds.NonCollectible.Neutral.GrumbleWorldshaker_GrumblyTumblyEnchantment},
            { CardIds.Collectible.Warlock.ClutchmotherZavas, CardIds.NonCollectible.Warlock.ClutchmotherZavas_RemembranceEnchantment},
            { CardIds.Collectible.Warlock.TheDarkPortal, CardIds.NonCollectible.Neutral.TheDarkPortal_DarkPortalEnchantment},
            { CardIds.NonCollectible.Warlock.SupremeArchaeology_TomeOfOrigination, CardIds.NonCollectible.Warlock.SupremeArchaeology_OriginationEnchantment},
            { CardIds.Collectible.Warlock.WilfredFizzlebang, CardIds.NonCollectible.Warlock.WilfredFizzlebang_MasterSummonerEnchantment},
            { CardIds.Collectible.Warrior.AkaliTheRhino, CardIds.NonCollectible.Warrior.AkalitheRhino_RhinoSkinEnchantment},
            { CardIds.NonCollectible.Warrior.AutoArmaments, CardIds.NonCollectible.Warrior.AutoArmed},
            { CardIds.Collectible.Warrior.BrassKnuckles, CardIds.NonCollectible.Neutral.BrassKnuckles_SmugglingEnchantment},
            { CardIds.Collectible.Warrior.CorsairCache, CardIds.NonCollectible.Warrior.CorsairCache_VoidSharpenedEnchantment},
            { CardIds.Collectible.Warrior.GalakrondTheUnbreakable, CardIds.NonCollectible.Neutral.GalakrondtheUnbreakable_GalakrondsStrengthEnchantment1},
            { CardIds.NonCollectible.Warrior.GalakrondtheUnbreakable_GalakrondTheApocalypseToken, CardIds.NonCollectible.Neutral.GalakrondtheUnbreakable_GalakrondsStrengthEnchantment2},
            { CardIds.NonCollectible.Warrior.GalakrondtheUnbreakable_GalakrondAzerothsEndToken, CardIds.NonCollectible.Neutral.GalakrondtheUnbreakable_GalakrondsStrengthEnchantment3},
            { CardIds.Collectible.Warrior.GrimestreetPawnbroker, CardIds.NonCollectible.Neutral.GrimestreetPawnbroker_SmugglingEnchantment},
            { CardIds.Collectible.Warrior.GrimyGadgeteer, CardIds.NonCollectible.Neutral.GrimyGadgeteer_SmugglingEnchantment},
            { CardIds.Collectible.Warrior.StolenGoods, CardIds.NonCollectible.Neutral.StolenGoods_SmugglingEnchantment},
            { CardIds.Collectible.Neutral.DonHancho, CardIds.NonCollectible.Neutral.DonHanCho_SmugglingEnchantment },
            { CardIds.Collectible.Neutral.EmperorThaurissan, CardIds.NonCollectible.Neutral.EmperorThaurissan_ImperialFavorEnchantment },
            { CardIds.Collectible.Neutral.GrimestreetSmuggler, CardIds.NonCollectible.Neutral.GrimestreetSmuggler_SmugglingEnchantment},
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
            // Use the meta node and not the action so that we can properly sequence events thanks to the 
            // node's index
            return node.Type == typeof(MetaData)
                 && (node.Object as MetaData).Meta == (int)MetaDataType.TARGET;

        }

        public List<GameEventProvider> CreateGameEventProviderFromNew(Node node)
        {
            return null;
        }

        public List<GameEventProvider> CreateGameEventProviderFromClose(Node node)
        {
            return CreateEventProviderForMeta(node);
        }

        private List<GameEventProvider> CreateEventProviderForMeta(Node node)
        {     
            var isPower = node.Parent.Type == typeof(Parser.ReplayData.GameActions.Action)
                 && (node.Parent.Object as Parser.ReplayData.GameActions.Action).Type == (int)BlockType.POWER;
            var isTrigger = node.Parent.Type == typeof(Parser.ReplayData.GameActions.Action)
                 && (node.Parent.Object as Parser.ReplayData.GameActions.Action).Type == (int)BlockType.TRIGGER;
            if (!isPower && !isTrigger)
            {
                return null;
            }


            var action = node.Parent.Object as Parser.ReplayData.GameActions.Action;
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

            var meta = node.Object as MetaData;
            var entitiesBuffedInHand = meta.MetaInfo
                .Select(info => GameState.CurrentEntities.ContainsKey(info.Entity) ? GameState.CurrentEntities[info.Entity] : null)
                .Where(entity => entity != null)
                .Where(entity => entity.GetTag(GameTag.ZONE) == (int)Zone.HAND)
                .ToList();
            return entitiesBuffedInHand
                .Select(entity =>
                {
                    return GameEventProvider.Create(
                        meta.TimeStamp,
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
                                BuffCardId = buffs.ContainsKey(actionEntity.CardId) ? buffs[actionEntity.CardId] : null,
                            }),
                        true,
                        node);
                })
                .ToList();
        }
    }
}
