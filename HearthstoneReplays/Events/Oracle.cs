using HearthstoneReplays.Parser;
using HearthstoneReplays.Parser.ReplayData;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using System;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.Entities;
using System.Collections.Generic;
using static HearthstoneReplays.Events.CardIds;
using static HearthstoneReplays.Events.CardIds.Collectible;
using HearthstoneReplays.Parser.ReplayData.Meta;
using System.Linq;

namespace HearthstoneReplays.Events
{
    public class Oracle
    {
        public static string GetCreatorFromTags(GameState gameState, FullEntity entity, Node node)
        {
            var creatorCardId = Oracle.GetCreatorCardIdFromTag(gameState, entity.GetTag(GameTag.CREATOR), entity);
            if (creatorCardId == null)
            {
                creatorCardId = Oracle.GetCreatorCardIdFromTag(gameState, entity.GetTag(GameTag.DISPLAYED_CREATOR), entity);
            }
            return creatorCardId;
        }
        public static string GetCreatorFromTags(GameState gameState, ShowEntity entity, Node node)
        {
            var creatorCardId = Oracle.GetCreatorCardIdFromTag(gameState, entity.GetTag(GameTag.CREATOR), entity);
            if (creatorCardId == null)
            {
                creatorCardId = Oracle.GetCreatorCardIdFromTag(gameState, entity.GetTag(GameTag.DISPLAYED_CREATOR), entity);
            }
            return creatorCardId;
        }

        public static string FindCardCreatorCardId(GameState GameState, FullEntity entity, Node node, bool getLastInfluencedBy = true)
        {
            // If the card is already present in the deck, and was not created explicitely, there is no creator
            if (!getLastInfluencedBy
                && entity.GetTag(GameTag.CREATOR) == -1
                && entity.GetTag(GameTag.DISPLAYED_CREATOR) == -1
                && entity.GetTag(GameTag.CREATOR_DBID) == -1
                && entity.GetTag(GameTag.ZONE) == (int)Zone.DECK)
            {
                return null;
            }

            var creatorCardId = Oracle.FindCardCreatorCardId(GameState, entity.GetTag(GameTag.CREATOR), node);
            if (creatorCardId == null)
            {
                creatorCardId = Oracle.FindCardCreatorCardId(GameState, entity.GetTag(GameTag.DISPLAYED_CREATOR), node);
            }
            return creatorCardId;
        }

        public static string FindCardCreatorCardId(GameState GameState, ShowEntity entity, Node node)
        {
            var creatorCardId = Oracle.FindCardCreatorCardId(GameState, entity.GetTag(GameTag.CREATOR), node);
            if (creatorCardId == null)
            {
                creatorCardId = Oracle.FindCardCreatorCardId(GameState, entity.GetTag(GameTag.DISPLAYED_CREATOR), node);
            }
            return creatorCardId;
        }

        public static int FindCardCreatorEntityId(GameState GameState, FullEntity entity, Node node, bool getLastInfluencedBy = true)
        {
            // If the card is already present in the deck, and was not created explicitely, there is no creator
            if (!getLastInfluencedBy
                && entity.GetTag(GameTag.CREATOR) == -1
                && entity.GetTag(GameTag.DISPLAYED_CREATOR) == -1
                && entity.GetTag(GameTag.CREATOR_DBID) == -1
                && entity.GetTag(GameTag.ZONE) == (int)Zone.DECK)
            {
                return -1;
            }

            return entity.GetTag(GameTag.CREATOR) != -1 ? entity.GetTag(GameTag.CREATOR) : entity.GetTag(GameTag.DISPLAYED_CREATOR);
        }

        public static int FindCardCreatorEntityId(GameState GameState, ShowEntity entity, Node node)
        {
            return entity.GetTag(GameTag.CREATOR) != -1 ? entity.GetTag(GameTag.CREATOR) : entity.GetTag(GameTag.DISPLAYED_CREATOR);
        }

        public static string FindCardCreatorCardId(GameState GameState, int creatorTag, Node node)
        {
            if (creatorTag != -1 && GameState.CurrentEntities.ContainsKey(creatorTag))
            {
                var creator = GameState.CurrentEntities[creatorTag];
                return creator.CardId;
            }
            if (node.Parent.Type == typeof(Parser.ReplayData.GameActions.Action))
            {
                var act = node.Parent.Object as Parser.ReplayData.GameActions.Action;
                if (GameState.CurrentEntities.ContainsKey(act.Entity))
                {
                    var creator = GameState.CurrentEntities[act.Entity];
                    // Spoecial case for Draem Portals, since for some reasons a Dream Portal the nests the next 
                    // action (which can lead to nested dream portal blocks)
                    if (creator.CardId == CardIds.NonCollectible.Druid.YseraUnleashed_DreamPortalToken)
                    {
                        if (node.Object.GetType() == typeof(ShowEntity))
                        {
                            var handledEntity = (node.Object as ShowEntity);
                            if (handledEntity.GetTag(GameTag.ZONE) == (int)Zone.HAND)
                            {
                                return null;
                            }
                        }
                    }
                    return creator.CardId;
                }
            }
            return null;
        }

        private static string GetCreatorCardIdFromTag(GameState gameState, int creatorTag, FullEntity entity)
        {
            if (creatorTag != -1 && gameState.CurrentEntities.ContainsKey(creatorTag))
            {
                var creator = gameState.CurrentEntities[creatorTag];
                return creator.CardId;
            }
            return null;
        }

        private static string GetCreatorCardIdFromTag(GameState GameState, int creatorTag, ShowEntity entity)
        {
            if (creatorTag != -1 && GameState.CurrentEntities.ContainsKey(creatorTag))
            {
                var creator = GameState.CurrentEntities[creatorTag];
                return creator.CardId;
            }
            return null;
        }

        //public static string FindBuffFromCardId(string buffingEntityCardId)
        //{
        //    switch (buffingEntityCardId)
        //    {
        //        case Paladin.GrimestreetEnforcer: return NonCollectible.Neutral.GrimestreetEnforcer_SmugglingEnchantment;
        //        case Paladin.GrimestreetOutfitter: return NonCollectible.Neutral.GrimestreetOutfitter_SmugglingEnchantment;
        //    }
        //    return null;
        //}

        public static string PredictCardId(GameState GameState, string creatorCardId, int creatorEntityId, Node node, string inputCardId = null)
        {
            if (inputCardId != null && inputCardId.Length > 0)
            {
                return inputCardId;
            }
            // Don't know how to support the Libram of Wisdom / Explorer's Hat use case without this
            // Maybe handling the sub_spell blocks could work, but it feels really weird (it's animation stuff inside
            // gamestate logs), and needs a full rework of the XML, which probably will be a lot of work
            var isFunkyDeathrattleEffect = false;
            if (node.Parent != null && node.Parent.Type == typeof(Parser.ReplayData.GameActions.Action))
            {
                var action = node.Parent.Object as Parser.ReplayData.GameActions.Action;
                if (action.Type == (int)BlockType.TRIGGER && action.TriggerKeyword == (int)GameTag.DEATHRATTLE && action.EffectIndex == -1)
                {
                    isFunkyDeathrattleEffect = true;
                }
            }
            if (!isFunkyDeathrattleEffect)
            {
                switch (creatorCardId)
                {
                    case Neutral.AncientShade: return NonCollectible.Neutral.AncientShade_AncientCurseToken;
                    case NonCollectible.Neutral.AngryMobGILNEAS: return NonCollectible.Neutral.CrazedMobGILNEAS;
                    case Neutral.BadLuckAlbatross: return NonCollectible.Neutral.BadLuckAlbatross_AlbatrossToken;
                    case Neutral.BananaBuffoon: return NonCollectible.Neutral.BananaBuffoon_BananasToken;
                    case Neutral.BootyBayBookie: return NonCollectible.Neutral.TheCoin;
                    case Neutral.BurglyBully: return NonCollectible.Neutral.TheCoin;
                    case NonCollectible.Neutral.CoinPouchGILNEAS: return NonCollectible.Neutral.SackOfCoinsGILNEAS;
                    case NonCollectible.Neutral.CreepyCurioGILNEAS: return NonCollectible.Neutral.HauntedCurioGILNEAS;
                    case NonCollectible.Neutral.HauntedCurioGILNEAS: return NonCollectible.Neutral.CursedCurioGILNEAS;
                    case Neutral.Doomcaller: return Neutral.Cthun;
                    case Neutral.EliseTheTrailblazer: return NonCollectible.Neutral.ElisetheTrailblazer_UngoroPackToken;
                    case Neutral.EliseStarseeker: return NonCollectible.Neutral.EliseStarseeker_MapToTheGoldenMonkeyToken;
                    case Neutral.FeralGibberer: return Neutral.FeralGibberer;
                    case Neutral.FireFly: return NonCollectible.Neutral.FireFly_FlameElementalToken;
                    case Neutral.FishyFlyer: return NonCollectible.Neutral.FishyFlyer_SpectralFlyerToken;
                    case Neutral.HakkarTheSoulflayer: return NonCollectible.Neutral.HakkartheSoulflayer_CorruptedBloodToken;
                    case Neutral.HoardingDragon: return NonCollectible.Neutral.TheCoin;
                    case Neutral.IgneousElemental: return NonCollectible.Neutral.FireFly_FlameElementalToken;
                    case Neutral.InfestedGoblin: return NonCollectible.Neutral.ScarabEgg_ScarabToken;
                    case Neutral.KingMukla: return NonCollectible.Neutral.Bananas;
                    case Neutral.LicensedAdventurer: return NonCollectible.Neutral.TheCoin;
                    case NonCollectible.Neutral.MilitiaHornGILNEAS: return NonCollectible.Neutral.VeteransMilitiaHornGILNEAS;
                    case Neutral.MuklaTyrantOfTheVale: return NonCollectible.Neutral.Bananas;
                    case NonCollectible.Neutral.OldMilitiaHornGILNEAS: return NonCollectible.Neutral.MilitiaHornGILNEAS;
                    case Neutral.PortalKeeper: return NonCollectible.Neutral.PortalKeeper_FelhoundPortalToken;
                    case Neutral.PortalOverfiend: return NonCollectible.Neutral.PortalKeeper_FelhoundPortalToken;
                    case Neutral.SeaforiumBomber: return NonCollectible.Neutral.SeaforiumBomber_BombToken;
                    case Neutral.SmugSenior: return NonCollectible.Neutral.SmugSenior_SpectralSeniorToken;
                    case Neutral.SneakyDelinquent: return NonCollectible.Neutral.SneakyDelinquent_SpectralDelinquentToken;
                    case Neutral.SoldierOfFortune: return NonCollectible.Neutral.TheCoin;
                    case Neutral.SparkDrill: return NonCollectible.Neutral.SparkDrill_SparkToken;
                    case Neutral.SparkEngine: return NonCollectible.Neutral.SparkDrill_SparkToken;
                    case NonCollectible.Neutral.SurlyMobGILNEAS: return NonCollectible.Neutral.AngryMobGILNEAS;
                    case NonCollectible.Neutral.TheCandle: return NonCollectible.Neutral.TheCandle;
                    case NonCollectible.Neutral.TheDarkness: return NonCollectible.Neutral.TheDarkness_DarknessCandleToken;
                    case Neutral.WeaselTunneler: return Neutral.WeaselTunneler;
                    case NonCollectible.Neutral.EliseStarseeker_MapToTheGoldenMonkeyToken: return NonCollectible.Neutral.EliseStarseeker_GoldenMonkeyToken;
                    case Demonhunter.UrzulHorror: return NonCollectible.Demonhunter.UrzulHorror_LostSoulToken;
                    case Demonhunter.Marrowslicer: return NonCollectible.Warlock.SchoolSpirits_SoulFragmentToken;
                    case Demonhunter.TwinSlice: return NonCollectible.Demonhunter.TwinSlice_SecondSliceToken;
                    case NonCollectible.Demonhunter.InfernalStrike1: return NonCollectible.Demonhunter.TwinSlice_SecondSliceToken;
                    case Druid.ArchsporeMsshifn: return NonCollectible.Druid.ArchsporeMsshifn_MsshifnPrimeToken;
                    case Druid.AstralTiger: return Druid.AstralTiger;
                    case Druid.JadeIdol: return Druid.JadeIdol;
                    case Druid.JungleGiants: return NonCollectible.Druid.JungleGiants_BarnabusTheStomperToken;
                    case Druid.Malorne: return Druid.Malorne;
                    case Druid.SecureTheDeck: return Druid.Claw;
                    case Druid.WitchwoodApple: return NonCollectible.Druid.WitchwoodApple_TreantToken;
                    case Druid.YseraUnleashed: return NonCollectible.Druid.YseraUnleashed_DreamPortalToken;
                    case Hunter.AdorableInfestation: return NonCollectible.Hunter.AdorableInfestation_MarsuulCubToken;
                    case Hunter.HalazziTheLynx: return NonCollectible.Hunter.Springpaw_LynxToken;
                    case Hunter.RaptorHatchling: return NonCollectible.Hunter.RaptorHatchling_RaptorPatriarchToken;
                    case Hunter.Springpaw: return NonCollectible.Hunter.Springpaw_LynxToken;
                    case Hunter.TheMarshQueen: return NonCollectible.Hunter.TheMarshQueen_QueenCarnassaToken;
                    case Hunter.ZixorApexPredator: return NonCollectible.Hunter.ZixorApexPredator_ZixorPrimeToken;
                    case Mage.ArchmageAntonidas: return Mage.Fireball;
                    case Mage.AstromancerSolarian: return NonCollectible.Mage.AstromancerSolarian_SolarianPrimeToken;
                    case Mage.DeckOfWonders: return NonCollectible.Mage.DeckofWonders_ScrollOfWonderToken;
                    case Mage.FlameGeyser: return NonCollectible.Neutral.FireFly_FlameElementalToken;
                    case Mage.ForgottenTorch: return NonCollectible.Mage.ForgottenTorch_RoaringTorchToken;
                    case Mage.GhastlyConjurer: return Mage.MirrorImage;
                    case Mage.OpenTheWaygate: return NonCollectible.Mage.OpentheWaygate_TimeWarpToken;
                    case Mage.Pyros: return NonCollectible.Mage.Pyros_PyrosToken1;
                    case NonCollectible.Mage.Pyros_PyrosToken1: return NonCollectible.Mage.Pyros_PyrosToken2;
                    case Mage.Rhonin: return Mage.ArcaneMissiles;
                    case Mage.SteamSurger: return Mage.FlameGeyser;
                    case Mage.VioletSpellwing: return Mage.ArcaneMissiles;
                    case Paladin.DrygulchJailor: return NonCollectible.Paladin.SilverHandRecruitToken;
                    case Paladin.MurgurMurgurgle: return NonCollectible.Paladin.MurgurMurgurgle_MurgurglePrimeToken;
                    case Paladin.TheLastKaleidosaur: return NonCollectible.Paladin.TheLastKaleidosaur_GalvadonToken;
                    case Paladin.SandwaspQueen: return NonCollectible.Paladin.SandwaspQueen_SandwaspToken;
                    case Paladin.BronzeHerald: return NonCollectible.Paladin.BronzeHerald_BronzeDragonToken;
                    case Priest.AwakenTheMakers: return NonCollectible.Priest.AwakentheMakers_AmaraWardenOfHopeToken;
                    case Priest.GildedGargoyle: return NonCollectible.Neutral.TheCoin;
                    case Priest.ExcavatedEvil: return Priest.ExcavatedEvil;
                    case Priest.ExtraArms: return NonCollectible.Priest.ExtraArms_MoreArmsToken;
                    case Priest.ReliquaryOfSouls: return NonCollectible.Priest.ReliquaryofSouls_ReliquaryPrimeToken;
                    case Rogue.Akama: return NonCollectible.Rogue.Akama_AkamaPrimeToken;
                    case Rogue.BeneathTheGrounds: return NonCollectible.Rogue.BeneaththeGrounds_NerubianAmbushToken;
                    case Rogue.BloodsailFlybooter: return NonCollectible.Rogue.BloodsailFlybooter_SkyPirateToken;
                    case Rogue.BoneBaron: return NonCollectible.Neutral.GrimNecromancer_SkeletonToken;
                    case Rogue.DeadlyFork: return NonCollectible.Rogue.DeadlyFork_SharpFork;
                    case Rogue.FaldoreiStrider: return NonCollectible.Rogue.FaldoreiStrider_SpiderAmbushEnchantment;
                    case Rogue.RazorpetalLasher: return NonCollectible.Rogue.RazorpetalVolley_RazorpetalToken;
                    case Rogue.RazorpetalVolley: return NonCollectible.Rogue.RazorpetalVolley_RazorpetalToken;
                    case Rogue.ShadowOfDeath: return NonCollectible.Rogue.ShadowofDeath_ShadowToken;
                    case Rogue.TheCavernsBelow: return NonCollectible.Rogue.TheCavernsBelow_CrystalCoreTokenUNGORO;
                    case Rogue.UmbralSkulker: return NonCollectible.Neutral.TheCoin;
                    case Rogue.Wanted: return NonCollectible.Neutral.Coin;
                    case Rogue.Waxadred: return NonCollectible.Rogue.Waxadred_WaxadredsCandleToken;
                    case Shaman.LadyVashj: return NonCollectible.Shaman.LadyVashj_VashjPrimeToken;
                    case Shaman.UniteTheMurlocs: return NonCollectible.Shaman.UnitetheMurlocs_MegafinToken;
                    case Shaman.WhiteEyes: return NonCollectible.Shaman.WhiteEyes_TheStormGuardianToken;
                    case Warlock.CurseOfRafaam: return NonCollectible.Warlock.CurseofRafaam_CursedToken;
                    case Warlock.HighPriestessJeklik: return Warlock.HighPriestessJeklik;
                    case Warlock.Impbalming: return NonCollectible.Warlock.Impbalming_WorthlessImpToken;
                    case Warlock.KanrethadEbonlocke: return NonCollectible.Warlock.KanrethadEbonlocke_KanrethadPrimeToken;
                    case Warlock.LakkariSacrifice: return NonCollectible.Warlock.LakkariSacrifice_NetherPortalToken1;
                    case Warlock.RinTheFirstDisciple: return NonCollectible.Warlock.RintheFirstDisciple_TheFirstSealToken;
                    case NonCollectible.Warlock.RintheFirstDisciple_TheFirstSealToken: return NonCollectible.Warlock.RintheFirstDisciple_TheSecondSealToken;
                    case NonCollectible.Warlock.RintheFirstDisciple_TheSecondSealToken: return NonCollectible.Warlock.RintheFirstDisciple_TheThirdSealToken;
                    case NonCollectible.Warlock.RintheFirstDisciple_TheFourthSealToken: return NonCollectible.Warlock.RintheFirstDisciple_TheFinalSealToken;
                    case NonCollectible.Warlock.RintheFirstDisciple_TheFinalSealToken: return NonCollectible.Warlock.RintheFirstDisciple_AzariTheDevourerToken;
                    case Warlock.SchoolSpirits: return NonCollectible.Warlock.SchoolSpirits_SoulFragmentToken;
                    case Warlock.SoulShear: return NonCollectible.Warlock.SchoolSpirits_SoulFragmentToken;
                    case Warlock.SpiritJailer: return NonCollectible.Warlock.SchoolSpirits_SoulFragmentToken;
                    case Warrior.ClockworkGoblin: return NonCollectible.Neutral.SeaforiumBomber_BombToken;
                    case Warrior.DirehornHatchling: return NonCollectible.Warrior.DirehornHatchling_DirehornMatriarchToken;
                    case Warrior.ExploreUngoro: return NonCollectible.Warrior.ExploreUnGoro_ChooseYourPathToken;
                    case Warrior.FirePlumesHeart: return NonCollectible.Warrior.FirePlumesHeart_SulfurasToken;
                    case Warrior.IronJuggernaut: return NonCollectible.Warrior.IronJuggernaut_BurrowingMineToken;
                    case Warrior.Wrenchcalibur: return NonCollectible.Neutral.SeaforiumBomber_BombToken;
                    case Warrior.KargathBladefist: return NonCollectible.Warrior.KargathBladefist_KargathPrimeToken;

                    case Neutral.BalefulBanker:
                    case Neutral.DollmasterDorian:
                    case Neutral.DragonBreeder:
                    case Neutral.Sathrovarr:
                    case Neutral.ZolaTheGorgon:
                    case Druid.Recycle:
                    case Druid.Splintergraft:
                    case Hunter.DireFrenzy:
                    case Mage.ManicSoulcaster:
                    case Priest.HolyWater:
                    case Priest.Seance:
                    case Rogue.GangUp:
                    case Rogue.LabRecruiter:
                    case Rogue.Shadowcaster:
                    case Rogue.TogwagglesScheme:
                        if (node.Parent.Type == typeof(Parser.ReplayData.GameActions.Action))
                        {
                            var act = node.Parent.Object as Parser.ReplayData.GameActions.Action;
                            var target = GameState.CurrentEntities[act.Target];
                            if (target != null)
                            {
                                return target.CardId;
                            }
                        }
                        return null;

                    case Neutral.AugmentedElekk:
                        // The parent action is Augmented Elekk trigger, which is not the one we're interested in
                        // Its parent is the one that created the new entity
                        if (node.Parent.Parent.Type == typeof(Parser.ReplayData.GameActions.Action))
                        {
                            var act = node.Parent.Parent.Object as Parser.ReplayData.GameActions.Action;
                            // It should be the last ShowEntity of the action children
                            // Otherwise, the last FullEntity
                            for (int i = act.Data.Count - 1; i >= 0; i--)
                            {
                                if (act.Data[i].GetType() == typeof(ShowEntity))
                                {
                                    var showEntity = act.Data[i] as ShowEntity;
                                    return showEntity.CardId;
                                }
                                if (act.Data[i].GetType() == typeof(FullEntity))
                                {
                                    var fullEntity = act.Data[i] as FullEntity;
                                    return fullEntity.CardId;
                                }
                            }
                            // And if nothing matches, then we don't predict anything
                            return null;
                        }
                        return null;

                    case Warlock.ExpiredMerchant:
                        Console.WriteLine("TODO! Implement ExpiredMerchant card guess");
                        return null;

                    case Priest.SpiritOfTheDead:
                        if (node.Parent.Type == typeof(Parser.ReplayData.GameActions.Action))
                        {
                            var act = node.Parent.Object as Parser.ReplayData.GameActions.Action;
                            foreach (var data in act.Data)
                            {
                                if (data.GetType() == typeof(MetaData))
                                {
                                    var info = (data as MetaData).MetaInfo[0];
                                    var targetId = info.Entity;
                                    if (GameState.CurrentEntities.ContainsKey(targetId))
                                    {
                                        return GameState.CurrentEntities[targetId].CardId;
                                    }
                                }
                            }
                        }
                        return null;

                    case Mage.ManaBind:
                    case Mage.FrozenClone:
                        if (node.Parent.Type == typeof(Parser.ReplayData.GameActions.Action)
                            && node.Parent.Parent?.Type == typeof(Parser.ReplayData.GameActions.Action))
                        {
                            var act = node.Parent.Parent.Object as Parser.ReplayData.GameActions.Action;
                            var existingEntity = GameState.CurrentEntities[act.Entity];
                            return existingEntity.CardId;
                        }
                        return null;

                    case Mage.Duplicate:
                        if (node.Parent.Type == typeof(Parser.ReplayData.GameActions.Action))
                        {
                            var act = node.Parent.Object as Parser.ReplayData.GameActions.Action;
                            if (act.Type == (int)BlockType.TRIGGER)
                            {
                                var metaData = act.Data.Where(data => data is MetaData).Select(data => data as MetaData).FirstOrDefault();
                                if (metaData != null && metaData.Meta == (int)MetaDataType.HISTORY_TARGET && metaData.MetaInfo != null && metaData.MetaInfo.Count > 0)
                                {
                                    var entityId = metaData.MetaInfo[0].Entity;
                                    var existingEntity = GameState.CurrentEntities[entityId];
                                    return existingEntity?.CardId;
                                }
                            }
                        }
                        return null;
                }
            }

            // Plagiarize
            if (node.Parent != null && node.Parent.Type == typeof(Parser.ReplayData.GameActions.Action))
            {
                var action = node.Parent.Object as Parser.ReplayData.GameActions.Action;
                if (action.Type == (int)BlockType.TRIGGER && action.TriggerKeyword == (int)GameTag.SECRET)
                {
                    var actionEntity = GameState.CurrentEntities.ContainsKey(action.Entity)
                            ? GameState.CurrentEntities[action.Entity]
                            : null;
                    if (actionEntity != null && actionEntity.KnownEntityIds.Count > 0 && actionEntity.CardId == Rogue.Plagiarize)
                    {
                        var plagiarizeController = actionEntity.GetTag(GameTag.CONTROLLER);
                        var entitiesPlayedByActivePlayer = actionEntity.KnownEntityIds
                            .Select(entityId => GameState.CurrentEntities[entityId])
                            .Where(card => card.GetTag(GameTag.CONTROLLER) != -1 && card.GetTag(GameTag.CONTROLLER) != plagiarizeController)
                            .ToList();
                        if (entitiesPlayedByActivePlayer.Count == 0)
                        {
                            return null;
                        }
                        var nextCardToCreatePlagia = entitiesPlayedByActivePlayer[0].CardId;
                        actionEntity.KnownEntityIds.Remove(entitiesPlayedByActivePlayer[0].Entity);
                        return nextCardToCreatePlagia;
                    }
                }
            }

            // Diligent Notetaker
            if (node.Parent != null && node.Parent.Type == typeof(Parser.ReplayData.GameActions.Action))
            {
                var action = node.Parent.Object as Parser.ReplayData.GameActions.Action;
                if (action.Type == (int)BlockType.TRIGGER && action.TriggerKeyword == (int)GameTag.SPELLBURST)
                {
                    var actionEntity = GameState.CurrentEntities.ContainsKey(action.Entity)
                            ? GameState.CurrentEntities[action.Entity]
                            : null;
                    if (actionEntity != null && GameState.LastCardPlayedEntityId > 0 && actionEntity.CardId == Shaman.DiligentNotetaker)
                    {
                        var lastPlayedEntity = GameState.CurrentEntities.ContainsKey(GameState.LastCardPlayedEntityId)
                            ? GameState.CurrentEntities[GameState.LastCardPlayedEntityId]
                            : null;
                        return lastPlayedEntity?.CardId;
                    }
                }
            }

            // Libram of Wisdom
            if (node.Type == typeof(FullEntity) && (node.Object as FullEntity).SubSpellInEffect == "Librams_SpawnToHand_Book")
            {
                return Paladin.LibramOfWisdom;
            }

            // Keymaster Alabaster
            if (node.Parent != null && node.Parent.Type == typeof(Parser.ReplayData.GameActions.Action))
            {
                var action = node.Parent.Object as Parser.ReplayData.GameActions.Action;
                if (action.Type == (int)BlockType.TRIGGER)
                {
                    var actionEntity = GameState.CurrentEntities.ContainsKey(action.Entity)
                            ? GameState.CurrentEntities[action.Entity]
                            : null;
                    if (actionEntity != null && GameState.LastCardDrawnEntityId > 0 && actionEntity.CardId == Neutral.KeymasterAlabaster)
                    {
                        var lastDrawnEntity = GameState.CurrentEntities.ContainsKey(GameState.LastCardDrawnEntityId)
                            ? GameState.CurrentEntities[GameState.LastCardDrawnEntityId]
                            : null;
                        return lastDrawnEntity?.CardId;
                    }
                }
            }

            // Second card for Archivist Elysiana
            if (node.Parent != null && node.Parent.Type == typeof(Parser.ReplayData.GameActions.Action))
            {
                var action = node.Parent.Object as Parser.ReplayData.GameActions.Action;
                if (action.Type == (int)BlockType.POWER)
                {
                    var actionEntity = GameState.CurrentEntities[action.Entity];
                    if (actionEntity.CardId == Neutral.ArchivistElysiana)
                    {
                        // Now let's find the ID of the card that was created right before
                        var lastTagChange = action.Data
                            .Where(data => data is TagChange)
                            .Select(data => data as TagChange)
                            .Where(tag => tag.Name == (int)GameTag.ZONE && tag.Value == (int)Zone.DECK)
                            .LastOrDefault();
                        if (lastTagChange != null)
                        {
                            var lastEntityId = lastTagChange.Entity;
                            return GameState.CurrentEntities[lastEntityId]?.CardId;
                        }
                    }
                }
            }

            return null;
        }
    }
}
