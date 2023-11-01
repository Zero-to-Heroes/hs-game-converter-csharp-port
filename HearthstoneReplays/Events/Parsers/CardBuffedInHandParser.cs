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
            CardIds.AegwynnTheGuardianCore,
            CardIds.AkaliTheRhino,
            CardIds.AutoArmamentsTavernBrawlToken,
            CardIds.BlackjackStunner,
            CardIds.BogSlosher,
            CardIds.BrassKnuckles,
            CardIds.CallToAdventure,
            CardIds.CheatDeath,
            CardIds.CheatDeathCore,
            CardIds.ChorusRiff,
            CardIds.ClutchmotherZavas,
            CardIds.CorsairCache,
            CardIds.DinoTrackingTavernBrawl,
            CardIds.DonHancho,
            CardIds.DunBaldarBunker,
            CardIds.EfficientOctoBot,
            CardIds.EmperorThaurissan_BRM_028,
            CardIds.EmperorThaurissan_WON_133,
            CardIds.FarSightCore,
            CardIds.FarSightLegacy,
            CardIds.FarSightVanilla,
            CardIds.FarWatchPost,
            CardIds.FateWeaver,
            CardIds.FinalShowdown_GainMomentumToken,
            CardIds.FinalShowdown,
            CardIds.FreezingTrapCore,
            CardIds.FreezingTrapLegacy,
            CardIds.FreezingTrapVanilla,
            CardIds.GalakrondTheUnbreakable_GalakrondAzerothsEndToken,
            CardIds.GalakrondTheUnbreakable_GalakrondTheApocalypseToken,
            CardIds.GalakrondTheUnbreakable,
            CardIds.GrimestreetEnforcer,
            CardIds.GrimestreetEnforcer_WON_046,
            CardIds.GrimestreetOutfitter,
            CardIds.GrimestreetOutfitterCore,
            CardIds.GrimestreetPawnbroker,
            CardIds.GrimestreetSmuggler,
            CardIds.GrimscaleChum,
            CardIds.GrimyGadgeteer,
            CardIds.GrumbleWorldshaker,
            CardIds.HarnessTheElementsTavernBrawl,
            CardIds.HiddenCache,
            CardIds.HuntersInsight,
            CardIds.HuntersInsightTavernBrawl,
            CardIds.IKnowAGuy_WON_350,
            CardIds.IKnowAGuy,
            CardIds.ManaBind,
            CardIds.OrbOfRevelation_OrbOfRevelationTavernBrawlEnchantment_PVPDR_BAR_Passive08e1,
            CardIds.OrbOfRevelationTavernBrawl,
            CardIds.PredatoryInstincts,
            CardIds.RingOfPhaseshifting_RingOfPhaseshiftingTavernBrawlEnchantment,
            CardIds.RingOfPhaseshiftingTavernBrawl,
            CardIds.RuniTimeExplorer_RuinsOfKoruneToken_WON_053t6,
            CardIds.ScavengersIngenuity,
            CardIds.ScourgeIllusionist,
            CardIds.Shadowcaster,
            CardIds.Shadowfiend,
            CardIds.ShadowstepCore,
            CardIds.ShadowstepLegacy,
            CardIds.ShadowstepVanilla,
            CardIds.ShakyZipgunner,
            CardIds.SkullOfGuldan_BT_601,
            CardIds.SkullOfGuldanTavernBrawl,
            CardIds.SmugglersCrate,
            CardIds.SmugglersRun,
            CardIds.SonyaShadowdancer,
            CardIds.SpyglassTavernBrawl,
            CardIds.StealerOfSouls,
            CardIds.StolenGoods,
            CardIds.SupremeArchaeology_TomeOfOrigination,
            CardIds.TearReality,
            CardIds.TheDarkPortal_BT_302,
            CardIds.TroggBeastrager,
            CardIds.Valanyr,
            CardIds.WagglePick,
            CardIds.WaywardSage,
            CardIds.WilfredFizzlebang,
        };

        private Dictionary<string, string> buffs = new Dictionary<string, string>()
        {      
            { CardIds.AegwynnTheGuardianCore, CardIds.AegwynnTheGuardian_GuardiansLegacyCoreEnchantment},
            { CardIds.AkaliTheRhino, CardIds.AkaliTheRhino},
            { CardIds.AutoArmamentsTavernBrawlToken, CardIds.AutoArmaments_AutoArmedTavernBrawlEnchantment},
            { CardIds.BeaststalkerTavish_ImprovedFreezingTrapToken, CardIds.ImprovedFreezingTrap_FreezingEnchantment},
            { CardIds.BlackjackStunner, CardIds.BlackjackStunner_StunnedEnchantment},
            { CardIds.BogSlosher, CardIds.BogSlosher_SloshedEnchantment},
            { CardIds.BrassKnuckles, CardIds.BrassKnuckles_SmugglingEnchantment},
            { CardIds.CallToAdventure, CardIds.CallToAdventure_HeroicEnchantment },
            { CardIds.CheatDeath, CardIds.CheatDeath_CloseCallEnchantment},
            { CardIds.CheatDeathCore, CardIds.CheatDeath_CloseCallEnchantment},
            { CardIds.ChorusRiff, CardIds.ChorusRiff_ChorusEnchantment },
            { CardIds.ClutchmotherZavas, CardIds.ClutchmotherZavas_RemembranceEnchantment},
            { CardIds.CorsairCache, CardIds.CorsairCache_VoidSharpenedEnchantment},
            { CardIds.DinoTrackingTavernBrawl, CardIds.DinoTracking_DinoTrackingTavernBrawlEnchantment},
            { CardIds.DonHancho, CardIds.DonHancho_SmugglingEnchantment },
            { CardIds.DunBaldarBunker, CardIds.DunBaldarBunker_CloakedSecretsEnchantment},
            { CardIds.EfficientOctoBot, CardIds.EfficientOctoBot_TrainingEnchantment},
            { CardIds.EmperorThaurissan_BRM_028, CardIds.EmperorThaurissan_ImperialFavorEnchantment },
            { CardIds.EmperorThaurissan_WON_133, CardIds.EmperorThaurissan_ImperialFavorEnchantment_WON_133e },
            { CardIds.FarSightCore, CardIds.FarSight_FarSightLegacyEnchantment },
            { CardIds.FarSightLegacy, CardIds.FarSight_FarSightLegacyEnchantment },
            { CardIds.FarSightVanilla, CardIds.FarSight_FarSightLegacyEnchantment },
            { CardIds.FarWatchPost, CardIds.FarWatchPost_SpottedEnchantment },
            { CardIds.FateWeaver, CardIds.FateWeaver_DraconicFateEnchantment},
            { CardIds.FinalShowdown_GainMomentumToken, CardIds.FasterMovesEnchantment },
            { CardIds.FinalShowdown, CardIds.FasterMovesEnchantment },
            { CardIds.FreezingTrapCore, CardIds.FreezingTrap_TrappedLegacyEnchantment},
            { CardIds.FreezingTrapLegacy, CardIds.FreezingTrap_TrappedLegacyEnchantment},
            { CardIds.FreezingTrapVanilla, CardIds.FreezingTrap_TrappedLegacyEnchantment},
            { CardIds.GalakrondTheUnbreakable_GalakrondAzerothsEndToken, CardIds.GalakrondTheUnbreakable_GalakrondsStrengthEnchantment_DRG_650e3},
            { CardIds.GalakrondTheUnbreakable_GalakrondTheApocalypseToken, CardIds.GalakrondTheUnbreakable_GalakrondsStrengthEnchantment_DRG_650e2},
            { CardIds.GalakrondTheUnbreakable, CardIds.GalakrondTheUnbreakable_GalakrondsStrengthEnchantment_DRG_650e},
            { CardIds.GrimestreetEnforcer, CardIds.GrimestreetEnforcer_SmugglingEnchantment},
            { CardIds.GrimestreetEnforcer_WON_046, CardIds.GrimestreetEnforcer_SmugglingEnchantment_WON_046e},
            { CardIds.GrimestreetOutfitter, CardIds.GrimestreetOutfitter_SmugglingEnchantment},
            { CardIds.GrimestreetOutfitterCore, CardIds.GrimestreetOutfitter_SmugglingEnchantment},
            { CardIds.GrimestreetPawnbroker, CardIds.GrimestreetPawnbroker_SmugglingEnchantment},
            { CardIds.GrimestreetSmuggler, CardIds.GrimestreetSmuggler_SmugglingEnchantment},
            { CardIds.GrimscaleChum, CardIds.GrimscaleChum_SmugglingEnchantment},
            { CardIds.GrimyGadgeteer, CardIds.GrimyGadgeteer_SmugglingEnchantment},
            { CardIds.GrumbleWorldshaker, CardIds.GrumbleWorldshaker_GrumblyTumblyEnchantment},
            { CardIds.HarnessTheElements, CardIds.HarnessTheElements_HarnessTheElementsTavernBrawlEnchantment},
            { CardIds.HiddenCache, CardIds.HiddenCache_SmugglingEnchantment},
            { CardIds.HuntersInsight, CardIds.HuntersInsight_InsightfulEnchantment},
            { CardIds.HuntersInsightTavernBrawl, CardIds.HuntersInsight_InsightfulTavernBrawlEnchantment},
            { CardIds.IKnowAGuy_WON_350, CardIds.IKnowAGuy_KnowsAnotherGuyEnchantment_CFM_940e },
            { CardIds.IKnowAGuy, CardIds.IKnowAGuy_KnowsAnotherGuyEnchantment_CFM_940e },
            { CardIds.OrbOfRevelationTavernBrawl, CardIds.OrbOfRevelation_OrbOfRevelationTavernBrawlEnchantment_PVPDR_BAR_Passive08e1},
            { CardIds.OrbOfRevelation_OrbOfRevelationTavernBrawlEnchantment_PVPDR_BAR_Passive08e1, CardIds.OrbOfRevelation_OrbOfRevelationTavernBrawlEnchantment_PVPDR_BAR_Passive08e2},
            { CardIds.PredatoryInstincts, CardIds.PredatoryInstincts_PredatoryInstinctsEnchantment },
            { CardIds.RelicOfDimensions, CardIds.RelicOfDimensions_DimensionalEnchantment },
            { CardIds.RingOfPhaseshifting_RingOfPhaseshiftingTavernBrawlEnchantment, CardIds.RingOfPhaseshifting_PhaseshiftedTavernBrawlEnchantment},
            { CardIds.RuniTimeExplorer_RuinsOfKoruneToken_WON_053t6, CardIds.RuinsOfKorune_KorunesBlessingEnchantment_WON_053t6e},
            { CardIds.ScavengersIngenuity, CardIds.ScavengersIngenuity_PackTacticsEnchantment},
            { CardIds.ScourgeIllusionist, CardIds.ScourgeIllusionist_IllusionEnchantment},
            { CardIds.Shadowcaster, CardIds.Shadowcaster_FlickeringDarknessEnchantment},
            { CardIds.Shadowcasting101_Shadowcasting101TavernBrawlEnchantment_PVPDR_AV_Passive04e1, CardIds.Shadowcasting101_Shadowcasting101TavernBrawlEnchantment_PVPDR_AV_Passive04e2},
            { CardIds.Shadowfiend_WON_061, CardIds.Shadowfiend_ShadowfiendedEnchantment_WON_061e},
            { CardIds.Shadowfiend, CardIds.Shadowfiend_ShadowfiendedEnchantment},
            { CardIds.ShadowstepCore, CardIds.CheatDeath_CloseCallEnchantment},
            { CardIds.ShadowstepLegacy, CardIds.CheatDeath_CloseCallEnchantment},
            { CardIds.ShadowstepVanilla, CardIds.CheatDeath_CloseCallEnchantment},
            { CardIds.ShakyZipgunner, CardIds.ShakyZipgunner_SmugglingEnchantment},
            { CardIds.SkullOfGuldan_BT_601, CardIds.SkullOfGuldan_EmbracePowerEnchantment },
            { CardIds.SkullOfGuldanTavernBrawl, CardIds.SkullOfGuldan_EmbracePowerEnchantment },
            { CardIds.SmugglersCrate, CardIds.SmugglersCrate_SmugglingEnchantment},
            { CardIds.SmugglersRun, CardIds.SmugglersRun_SmugglingEnchantment},
            { CardIds.SonyaShadowdancer, CardIds.SonyaShadowdancer_SonyasShadowEnchantment},
            { CardIds.StealerOfSouls, CardIds.StealerOfSouls_StolenSoulEnchantment},
            { CardIds.StolenGoods_WON_110, CardIds.StolenGoods_SmugglingEnchantment_WON_110e},
            { CardIds.StolenGoods, CardIds.StolenGoods_SmugglingEnchantment},
            { CardIds.SupremeArchaeology_TomeOfOrigination, CardIds.SupremeArchaeology_OriginationEnchantment},
            { CardIds.TearReality, CardIds.TearReality_TornEnchantment},
            { CardIds.TheDarkPortal_BT_302, CardIds.TheDarkPortal_DarkPortalEnchantment},
            { CardIds.TroggBeastrager, CardIds.TroggBeastrager_SmugglingEnchantment},
            { CardIds.Valanyr, CardIds.Valanyr_ValanyrReequipEffectDummy},
            { CardIds.WagglePick, CardIds.CheatDeath_CloseCallEnchantment},
            { CardIds.WaywardSage, CardIds.WaywardSage_FoundTheWrongWayEnchantment},
            { CardIds.WilfredFizzlebang, CardIds.WilfredFizzlebang_MasterSummonerEnchantment},
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
