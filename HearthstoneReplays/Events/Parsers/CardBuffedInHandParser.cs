using HearthstoneReplays.Parser;
using HearthstoneReplays.Parser.ReplayData;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using System;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.Entities;
using System.Collections.Generic;
using System.Linq;
using HearthstoneReplays.Parser.ReplayData.Meta;
using Newtonsoft.Json;
using Action = HearthstoneReplays.Parser.ReplayData.GameActions.Action;

namespace HearthstoneReplays.Events.Parsers
{
    public class CardBuffedInHandParser : ActionParser
    {
        private GameState GameState { get; set; }
        private ParserState ParserState { get; set; }
        private StateFacade StateFacade { get; set; }

        // When adding an entity to this list, also add the corresponding buff in the map below
        private List<string> validBuffers = new List<string>()
        {
            CardIds.FinalShowdown,
            CardIds.FinalShowdown_GainMomentumToken,
            CardIds.SkullOfGuldan_BT_601,
            CardIds.SkullOfGuldanTavernBrawl,
            //CardIds.DreampetalFlorist,
            //CardIds.ImprisonedSatyr, 
            CardIds.PredatoryInstincts,
            //CardIds.Emeriss,
            //CardIds.ForlornStalker,
            CardIds.FreezingTrapCore,
            CardIds.FreezingTrapLegacy,
            CardIds.FreezingTrapVanilla,
            //CardIds.Helboar,
            CardIds.HiddenCache,
            //CardIds.ScarletWebweaver,
            CardIds.ScavengersIngenuity,
            //CardIds.ScrapShot,
            CardIds.ShakyZipgunner,
            CardIds.SmugglersCrate,
            CardIds.TroggBeastrager,
            CardIds.AegwynnTheGuardianCore,
            //CardIds.ManaBind, // Redundant with creator
            //CardIds.NagaSandWitch,
            //CardIds.UnstablePortal, // Redundant with creator
            CardIds.CallToAdventure, 
            //CardIds.DragonriderTalritha, 
            //CardIds.DragonSpeaker,
            //CardIds.FarrakiBattleaxe,
            //CardIds.GlowstoneTechnician,
            CardIds.GrimscaleChum,
            CardIds.GrimestreetEnforcer,
            CardIds.GrimestreetOutfitter,
            CardIds.SmugglersRun,
            CardIds.Valanyr, // not tested
            CardIds.FateWeaver,
            CardIds.Shadowfiend,
            //CardIds.AnkaTheBuried, 
            CardIds.BlackjackStunner,
            CardIds.CheatDeath,
            CardIds.EfficientOctoBot,
            CardIds.Shadowcaster,
            CardIds.ShadowstepCore,
            CardIds.ShadowstepLegacy,
            CardIds.ShadowstepVanilla,
            CardIds.SonyaShadowdancer,
            CardIds.WagglePick,
            CardIds.BogSlosher,
            CardIds.FarSightLegacy,
            CardIds.FarSightVanilla,
            //CardIds.FirePlumeHarbinger, 
            CardIds.GrumbleWorldshaker,
            //CardIds.TheMistcaller,
            //CardIds.ShadowCouncil, // Redundant with creator
            CardIds.ClutchmotherZavas,
            //CardIds.ImprisonedScrapImp
            //CardIds.SoulInfusion, 
            //CardIds.SpiritOfTheBat, 
            CardIds.TheDarkPortal_BT_302,
            CardIds.StealerOfSouls,
            CardIds.SupremeArchaeology_TomeOfOrigination,
            //CardIds.VoidAnalyst,
            CardIds.WilfredFizzlebang,
            CardIds.AkaliTheRhino,
            CardIds.AutoArmamentsTavernBrawlToken,
            //CardIds.Armagedillo, 
            CardIds.BrassKnuckles,
            //CardIds.BringItOn, 
            CardIds.CorsairCache,
            CardIds.GalakrondTheUnbreakable,
            CardIds.GalakrondTheUnbreakable_GalakrondTheApocalypseToken,
            CardIds.GalakrondTheUnbreakable_GalakrondAzerothsEndToken,
            CardIds.GrimestreetPawnbroker,
            CardIds.GrimyGadgeteer,
            //CardIds.HobartGrapplehammer, 
            //CardIds.IntoTheFray, 
            CardIds.StolenGoods,
            //CardIds.BlubberBaron,
            //CardIds.AnubisathWarbringer,
            //CardIds.ArenaFanatic, 
            //CardIds.CorridorCreeper,
            CardIds.DonHancho,  // not tested
            //CardIds.DeathaxePunisher,
            //CardIds.DragonqueenAlexstrasza,  // redundant with creator
            CardIds.EmperorThaurissan_BRM_028,
            CardIds.FarWatchPost, 
            //CardIds.EbonDragonsmith, 
            //CardIds.Galvanizer, 
            CardIds.GrimestreetSmuggler,
            //CardIds.HelplessHatchling, 
            //CardIds.HistoryBuff,
            //CardIds.TastyFlyfish, 

            //CardIds.Felosophy,
            //CardIds.PotionOfIllusion,
            CardIds.WaywardSage,
        };

        private Dictionary<string, string> buffs = new Dictionary<string, string>()
        {
            { CardIds.FinalShowdown, CardIds.FasterMovesEnchantment },
            { CardIds.FinalShowdown_GainMomentumToken, CardIds.FasterMovesEnchantment },
            { CardIds.SkullOfGuldan_BT_601, CardIds.SkullOfGuldan_EmbracePowerEnchantment },
            { CardIds.SkullOfGuldanTavernBrawl, CardIds.SkullOfGuldan_EmbracePowerEnchantment },
            { CardIds.RelicOfDimensions, CardIds.RelicOfDimensions_DimensionalEnchantment },
            { CardIds.PredatoryInstincts, CardIds.PredatoryInstincts_PredatoryInstinctsEnchantment },
            { CardIds.FreezingTrapCore, CardIds.FreezingTrap_TrappedLegacyEnchantment},
            { CardIds.FreezingTrapLegacy, CardIds.FreezingTrap_TrappedLegacyEnchantment},
            { CardIds.FreezingTrapVanilla, CardIds.FreezingTrap_TrappedLegacyEnchantment},
            { CardIds.BeaststalkerTavish_ImprovedFreezingTrapToken, CardIds.ImprovedFreezingTrap_FreezingEnchantment},
            { CardIds.HiddenCache, CardIds.HiddenCache_SmugglingEnchantment},
            { CardIds.ScavengersIngenuity, CardIds.ScavengersIngenuity_PackTacticsEnchantment},
            { CardIds.ShakyZipgunner, CardIds.ShakyZipgunner_SmugglingEnchantment},
            { CardIds.SmugglersCrate, CardIds.SmugglersCrate_SmugglingEnchantment},
            { CardIds.TroggBeastrager, CardIds.TroggBeastrager_SmugglingEnchantment},
            { CardIds.AegwynnTheGuardianCore, CardIds.AegwynnTheGuardian_GuardiansLegacyCoreEnchantment},
            { CardIds.CallToAdventure, CardIds.CallToAdventure_HeroicEnchantment },
            { CardIds.GrimscaleChum, CardIds.GrimscaleChum_SmugglingEnchantment},
            { CardIds.GrimestreetEnforcer, CardIds.GrimestreetEnforcer_SmugglingEnchantment},
            { CardIds.GrimestreetOutfitter, CardIds.GrimestreetOutfitter_SmugglingEnchantment},
            { CardIds.SmugglersRun, CardIds.SmugglersRun_SmugglingEnchantment},
            { CardIds.Valanyr, CardIds.Valanyr_ValanyrReequipEffectDummy},
            { CardIds.FateWeaver, CardIds.FateWeaver_DraconicFateEnchantment},
            { CardIds.Shadowfiend, CardIds.Shadowfiend_ShadowfiendedEnchantment},
            { CardIds.BlackjackStunner, CardIds.BlackjackStunner_StunnedEnchantment},
            { CardIds.CheatDeath, CardIds.CheatDeath_CloseCallEnchantment},
            { CardIds.EfficientOctoBot, CardIds.EfficientOctoBot_TrainingEnchantment},
            { CardIds.Shadowcaster, CardIds.Shadowcaster_FlickeringDarknessEnchantment},
            { CardIds.ShadowstepCore, CardIds.CheatDeath_CloseCallEnchantment},
            { CardIds.ShadowstepLegacy, CardIds.CheatDeath_CloseCallEnchantment},
            { CardIds.ShadowstepVanilla, CardIds.CheatDeath_CloseCallEnchantment},
            { CardIds.SonyaShadowdancer, CardIds.SonyaShadowdancer_SonyasShadowEnchantment},
            { CardIds.WagglePick, CardIds.CheatDeath_CloseCallEnchantment},
            { CardIds.BogSlosher, CardIds.BogSlosher_SloshedEnchantment},
            { CardIds.FarSightLegacy, CardIds.FarSight_FarSightLegacyEnchantment},
            { CardIds.FarSightVanilla, CardIds.FarSight_FarSightLegacyEnchantment},
            { CardIds.GrumbleWorldshaker, CardIds.GrumbleWorldshaker_GrumblyTumblyEnchantment},
            { CardIds.ClutchmotherZavas, CardIds.ClutchmotherZavas_RemembranceEnchantment},
            { CardIds.TheDarkPortal_BT_302, CardIds.TheDarkPortal_DarkPortalEnchantment},
            { CardIds.StealerOfSouls, CardIds.StealerOfSouls_StolenSoulEnchantment},
            { CardIds.SupremeArchaeology_TomeOfOrigination, CardIds.SupremeArchaeology_OriginationEnchantment},
            { CardIds.WilfredFizzlebang, CardIds.WilfredFizzlebang_MasterSummonerEnchantment},
            { CardIds.AkaliTheRhino, CardIds.AkaliTheRhino},
            { CardIds.AutoArmamentsTavernBrawlToken, CardIds.AutoArmaments_AutoArmedTavernBrawlEnchantment},
            { CardIds.BrassKnuckles, CardIds.BrassKnuckles_SmugglingEnchantment},
            { CardIds.CorsairCache, CardIds.CorsairCache_VoidSharpenedEnchantment},
            { CardIds.GalakrondTheUnbreakable, CardIds.GalakrondTheUnbreakable_GalakrondsStrengthEnchantment_DRG_650e},
            { CardIds.GalakrondTheUnbreakable_GalakrondTheApocalypseToken, CardIds.GalakrondTheUnbreakable_GalakrondsStrengthEnchantment_DRG_650e2},
            { CardIds.GalakrondTheUnbreakable_GalakrondAzerothsEndToken, CardIds.GalakrondTheUnbreakable_GalakrondsStrengthEnchantment_DRG_650e3},
            { CardIds.GrimestreetPawnbroker, CardIds.GrimestreetPawnbroker_SmugglingEnchantment},
            { CardIds.GrimyGadgeteer, CardIds.GrimyGadgeteer_SmugglingEnchantment},
            { CardIds.StolenGoods, CardIds.StolenGoods_SmugglingEnchantment},
            { CardIds.DonHancho, CardIds.DonHancho_SmugglingEnchantment },
            { CardIds.EmperorThaurissan_BRM_028, CardIds.EmperorThaurissan_ImperialFavorEnchantment },
            { CardIds.FarWatchPost, CardIds.FarWatchPost_SpottedEnchantment },
            { CardIds.GrimestreetSmuggler, CardIds.GrimestreetSmuggler_SmugglingEnchantment},
            { CardIds.WaywardSage, CardIds.WaywardSage_FoundTheWrongWayEnchantment},
        };

        private List<string> validHoldWhenDrawnBuffers = new List<string>()
        {
            CardIds.SkullOfGuldan_BT_601,
            CardIds.SkullOfGuldanTavernBrawl,
        };

        private List<string> validSubSpellBuffers = new List<string>()
        {
            CardIds.AegwynnTheGuardianCore,
            CardIds.EfficientOctoBot,
        };

        private List<string> validTriggerBuffers = new List<string>()
        {
            CardIds.FarWatchPost,
            CardIds.DemonslayerKurtrus_LudicrousSpeedEnchantment,
            CardIds.Si7Skulker_SpyStuffEnchantment,
        };

        public CardBuffedInHandParser(ParserState ParserState, StateFacade facade)
        {
            this.ParserState = ParserState;
            this.GameState = ParserState.GameState;
            this.StateFacade = facade;
        }

        public bool AppliesOnNewNode(Node node, StateType stateType)
        {
            return false;
        }

        public bool AppliesOnCloseNode(Node node, StateType stateType)
        {
            // Use the meta node and not the action so that we can properly sequence events thanks to the 
            // node's index
            var isCorrectMeta = node.Type == typeof(MetaData)
                && ((node.Object as MetaData).Meta == (int)MetaDataType.TARGET
                    // Skull of Gul'dan doesn't have the TARGET info anymore, but the HOLD_DRAWN_CARD effect is only
                    // present when the card is buffed, so maybe we can use that
                    || (node.Object as MetaData).Meta == (int)MetaDataType.HOLD_DRAWN_CARD);
            return stateType == StateType.PowerTaskList
                && isCorrectMeta
                || (node.Type == typeof(SubSpell) && node.Object != null)
                || (node.Type == typeof(Action) 
                    && (node.Object as Action).Type == (int)BlockType.TRIGGER
                    && (node.Object as Action).TriggerKeyword == (int)GameTag.TRIGGER_VISUAL)
                    && GameState.CurrentEntities.ContainsKey((node.Object as Action).Entity)
                    && validTriggerBuffers.Contains(GameState.CurrentEntities[(node.Object as Action).Entity].CardId);
        }

        public List<GameEventProvider> CreateGameEventProviderFromNew(Node node)
        {
            return null;
        }

        public List<GameEventProvider> CreateGameEventProviderFromClose(Node node)
        {
            if (node.Type == typeof(MetaData))
            {
                return CreateEventProviderForMeta(node);
            }
            else if (node.Type == typeof(SubSpell))
            {
                return CreateEventProviderForSubSpell(node);
            }
            else if (node.Type == typeof(Action))
            {
                return CreateEventProviderForAction(node);
            }
            return null;
        }

        private List<GameEventProvider> CreateEventProviderForSubSpell(Node node)
        {
            var subSpell = (node.Object as SubSpell);
            //Logger.Log("Buff from sub spell", subSpell);
            if (subSpell.Targets == null)
            {
                return null;
            }

            var bufferCardId = BuildSource(subSpell, node);
            //Logger.Log("subSpellEntity", subSpellEntity);
            //Logger.Log("bufferCardId", bufferCardId);
            // Because some cards have an animation that reveal the buffed cards, and others don't, 
            // we have to whitelist the valid cards to avoid info leaks
            if (!validBuffers.Contains(bufferCardId) || !validSubSpellBuffers.Contains(bufferCardId))
            {
                //Logger.Log("buffer not valid", bufferCardId);
                return null;
            }

            var entitiesBuffedInHand = subSpell.Targets
                .Select(target => GameState.CurrentEntities.ContainsKey(target) ? GameState.CurrentEntities[target] : null)
                .Where(entity => entity != null)
                .Where(entity => entity.GetTag(GameTag.ZONE) == (int)Zone.HAND)
                .ToList();
            //Logger.Log("entitiesBuffedInHand", JsonConvert.SerializeObject(entitiesBuffedInHand));
            //Logger.Log("entitiesBuffed", JsonConvert.SerializeObject(subSpell.Targets
            //    .Select(target => GameState.CurrentEntities.ContainsKey(target) ? GameState.CurrentEntities[target] : null)
            //    .Where(entity => entity != null)
            //    .ToList()));
            return entitiesBuffedInHand
                .Select(entity =>
                {
                    return GameEventProvider.Create(
                        subSpell.Timestamp,
                        "CARD_BUFFED_IN_HAND",
                        GameEvent.CreateProvider(
                            "CARD_BUFFED_IN_HAND",
                            entity.CardId,
                            entity.GetEffectiveController(),
                            entity.Entity,
                            StateFacade,
                            null,
                            new
                            {
                                BuffingEntityCardId = bufferCardId,
                                BuffCardId = buffs.ContainsKey(bufferCardId) ? buffs[bufferCardId] : null,
                            }),
                        true,
                        node,
                        // The PowerTaskList doesnt' have an entry for that
                        true);
                })
                .ToList();
        }

        private List<GameEventProvider> CreateEventProviderForAction(Node node)
        {
            var action = node.Object as Action;
            var bufferCardId = GameState.CurrentEntities[action.Entity].CardId;

            var parentAction = node.Parent.Object as Action;

            var entitiesBuffedInHand = parentAction.Data
                .Where(data => data is TagChange)
                .Select(data => data as TagChange)
                .Where(data => data.Name == (int)GameTag.ZONE && data.Value == (int)Zone.HAND)
                .Select(data => GameState.CurrentEntities[data.Entity])
                .ToList();
            return entitiesBuffedInHand
                .Select(entity =>
                {
                    return GameEventProvider.Create(
                        action.TimeStamp,
                        "CARD_BUFFED_IN_HAND",
                        GameEvent.CreateProvider(
                            "CARD_BUFFED_IN_HAND",
                            entity.CardId,
                            entity.GetEffectiveController(),
                            entity.Entity,
                            StateFacade,
                            null,
                            new
                            {
                                BuffingEntityCardId = bufferCardId,
                                BuffCardId = buffs.ContainsKey(bufferCardId) ? buffs[bufferCardId] : null,
                            }),
                        true,
                        node,
                        // The PowerTaskList doesnt' have an entry for that
                        true);
                })
                .ToList();
        }

        private string BuildSource(SubSpell subSpell, Node node)
        {
            switch (subSpell.Prefab)
            {
                case "CS3FX_AegwynnTheGuardian_DrawAndHold_CardBuff_Super":
                    return CardIds.AegwynnTheGuardianCore;
            }

            if (!GameState.CurrentEntities.ContainsKey(subSpell.Source))
            {
                return null;
            }
            var subSpellEntity = GameState.CurrentEntities[subSpell.Source];
            return subSpellEntity.CardId;
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
            var metaType = (node.Object as MetaData).Meta;
            if (metaType == (int)MetaDataType.HOLD_DRAWN_CARD && !validHoldWhenDrawnBuffers.Contains(bufferCardId))
            {
                return null;
            }

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
                            entity.GetEffectiveController(),
                            entity.Entity,
                            StateFacade,
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
