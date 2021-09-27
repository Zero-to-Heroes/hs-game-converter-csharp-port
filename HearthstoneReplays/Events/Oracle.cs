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
using Action = HearthstoneReplays.Parser.ReplayData.GameActions.Action;

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

        public static Tuple<string, int> FindCardCreator(GameState GameState, FullEntity entity, Node node, bool getLastInfluencedBy = true)
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

        public static Tuple<string, int> FindCardCreatorCardId(GameState GameState, ShowEntity entity, Node node)
        {
            var creatorCardId = Oracle.FindCardCreatorCardId(GameState, entity.GetTag(GameTag.CREATOR), node);
            if (creatorCardId == null)
            {
                creatorCardId = Oracle.FindCardCreatorCardId(GameState, entity.GetTag(GameTag.DISPLAYED_CREATOR), node);
            }
            return creatorCardId;
        }

        //public static int FindCardCreatorEntityId(GameState GameState, FullEntity entity, Node node, bool getLastInfluencedBy = true)
        //{
        //    // If the card is already present in the deck, and was not created explicitely, there is no creator
        //    if (!getLastInfluencedBy
        //        && entity.GetTag(GameTag.CREATOR) == -1
        //        && entity.GetTag(GameTag.DISPLAYED_CREATOR) == -1
        //        && entity.GetTag(GameTag.CREATOR_DBID) == -1
        //        && entity.GetTag(GameTag.ZONE) == (int)Zone.DECK)
        //    {
        //        return -1;
        //    }

        //    var creatorFromTags = entity.GetTag(GameTag.CREATOR) != -1 ? entity.GetTag(GameTag.CREATOR) : entity.GetTag(GameTag.DISPLAYED_CREATOR);
        //    if (creatorFromTags == -1)
        //    {

        //    }
        //    return creatorFromTags;
        //}

        //public static int FindCardCreatorEntityId(GameState GameState, ShowEntity entity, Node node)
        //{
        //    return entity.GetTag(GameTag.CREATOR) != -1 ? entity.GetTag(GameTag.CREATOR) : entity.GetTag(GameTag.DISPLAYED_CREATOR);
        //}

        public static Tuple<string, int> FindCardCreatorCardId(GameState GameState, int creatorTag, Node node)
        {
            if (creatorTag != -1 && GameState.CurrentEntities.ContainsKey(creatorTag))
            {
                var creator = GameState.CurrentEntities[creatorTag];
                return new Tuple<string, int>(creator?.CardId, creator?.Entity ?? -1);
            }
            if (node.Parent.Type == typeof(Parser.ReplayData.GameActions.Action))
            {
                var act = node.Parent.Object as Parser.ReplayData.GameActions.Action;
                if (GameState.CurrentEntities.ContainsKey(act.Entity))
                {
                    var creator = GameState.CurrentEntities[act.Entity];
                    // Spoecial case for Draem Portals, since for some reasons a Dream Portal the nests the next 
                    // action (which can lead to nested dream portal blocks)
                    if (creator?.CardId == CardIds.NonCollectible.Druid.YseraUnleashed_DreamPortalToken)
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

                    return new Tuple<string, int>(creator?.CardId, creator?.Entity ?? -1);
                }
            }
            return null;
        }

        private static string GetCreatorCardIdFromTag(GameState gameState, int creatorTag, FullEntity entity)
        {
            if (creatorTag != -1 && gameState.CurrentEntities.ContainsKey(creatorTag))
            {
                var creator = gameState.CurrentEntities[creatorTag];
                return creator?.CardId;
            }
            return null;
        }

        private static string GetCreatorCardIdFromTag(GameState GameState, int creatorTag, ShowEntity entity)
        {
            if (creatorTag != -1 && GameState.CurrentEntities.ContainsKey(creatorTag))
            {
                var creator = GameState.CurrentEntities[creatorTag];
                return creator?.CardId;
            }
            return null;
        }

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
                    case NonCollectible.Demonhunter.FinalShowdown_CloseThePortalToken: return NonCollectible.Demonhunter.FinalShowdown_DemonslayerKurtrusToken;
                    case NonCollectible.Demonhunter.InfernalStrike1: return NonCollectible.Demonhunter.TwinSlice_SecondSliceToken;
                    case Demonhunter.Marrowslicer: return NonCollectible.Warlock.SchoolSpirits_SoulFragmentToken;
                    case Demonhunter.ThrowGlaive: return Collectible.Demonhunter.ThrowGlaive; // TO CHECK
                    case Demonhunter.TwinSlice: return NonCollectible.Demonhunter.TwinSlice_SecondSliceToken;
                    case Demonhunter.UrzulHorror: return NonCollectible.Demonhunter.UrzulHorror_LostSoulToken;
                    case Druid.ArchsporeMsshifn: return NonCollectible.Druid.ArchsporeMsshifn_MsshifnPrimeToken;
                    case Druid.AstralTiger: return Druid.AstralTiger;
                    case Druid.JadeIdol: return Druid.JadeIdol;
                    case Druid.JungleGiants: return NonCollectible.Druid.JungleGiants_BarnabusTheStomperToken;
                    case Druid.Malorne: return Druid.Malorne;
                    case Druid.SecureTheDeck: return Druid.ClawLegacy;
                    case Druid.WitchwoodApple: return NonCollectible.Druid.WitchwoodApple_TreantToken;
                    case Druid.YseraUnleashed: return NonCollectible.Druid.YseraUnleashed_DreamPortalToken;
                    case Hunter.AdorableInfestation: return NonCollectible.Hunter.AdorableInfestation_MarsuulCubToken;
                    case Hunter.HalazziTheLynx: return NonCollectible.Hunter.Springpaw_LynxToken;
                    case Hunter.RaptorHatchling: return NonCollectible.Hunter.RaptorHatchling_RaptorPatriarchToken;
                    case Hunter.SunscaleRaptor: return Collectible.Hunter.SunscaleRaptor;
                    case Hunter.Springpaw: return NonCollectible.Hunter.Springpaw_LynxToken;
                    case Hunter.TheMarshQueen: return NonCollectible.Hunter.TheMarshQueen_QueenCarnassaToken;
                    case Hunter.ZixorApexPredator: return NonCollectible.Hunter.ZixorApexPredator_ZixorPrimeToken;
                    case Mage.ArchmageAntonidas: return Mage.FireballCore;
                    case Mage.AstromancerSolarian: return NonCollectible.Mage.AstromancerSolarian_SolarianPrimeToken;
                    case Mage.ConfectionCyclone: return NonCollectible.Mage.ConfectionCyclone_SugarElementalToken;
                    case Mage.ConjureManaBiscuit: return NonCollectible.Mage.ConjureManaBiscuit_ManaBiscuitToken;
                    case Mage.DeckOfWonders: return NonCollectible.Mage.DeckofWonders_ScrollOfWonderToken;
                    case Mage.FirstFlame: return NonCollectible.Mage.FirstFlame_SecondFlameToken;
                    case Mage.FlameGeyser: return NonCollectible.Neutral.FireFly_FlameElementalToken;
                    case Mage.ForgottenTorch: return NonCollectible.Mage.ForgottenTorch_RoaringTorchToken;
                    case Mage.GhastlyConjurer: return Mage.MirrorImageLegacy;
                    case Mage.Ignite: return Mage.Ignite;
                    case Mage.OpenTheWaygate: return NonCollectible.Mage.OpentheWaygate_TimeWarpToken;
                    case Mage.Pyros: return NonCollectible.Mage.Pyros_PyrosToken1;
                    case NonCollectible.Mage.Pyros_PyrosToken1: return NonCollectible.Mage.Pyros_PyrosToken2;
                    case NonCollectible.Mage.SorcerersGambit_ReachThePortalRoomToken: return NonCollectible.Mage.SorcerersGambit_ArcanistDawngraspToken;
                    case Mage.Rhonin: return Mage.ArcaneMissilesLegacy;
                    case Mage.SteamSurger: return Mage.FlameGeyser;
                    case Mage.VioletSpellwing: return Mage.ArcaneMissilesLegacy;
                    case NonCollectible.Paladin.BringOnRecruits: return NonCollectible.Paladin.SilverHandRecruitLegacyToken;
                    case Paladin.DrygulchJailor: return NonCollectible.Paladin.SilverHandRecruitLegacyToken;
                    case Paladin.MurgurMurgurgle: return NonCollectible.Paladin.MurgurMurgurgle_MurgurglePrimeToken;
                    case Paladin.TheLastKaleidosaur: return NonCollectible.Paladin.TheLastKaleidosaur_GalvadonToken;
                    case Paladin.SandwaspQueen: return NonCollectible.Paladin.SandwaspQueen_SandwaspToken;
                    case Paladin.BronzeHerald: return NonCollectible.Paladin.BronzeHerald_BronzeDragonToken;
                    case Priest.AwakenTheMakers: return NonCollectible.Priest.AwakentheMakers_AmaraWardenOfHopeToken;
                    case Priest.GildedGargoyle: return NonCollectible.Neutral.TheCoinCore;
                    case Priest.ExcavatedEvil: return Priest.ExcavatedEvil;
                    case Priest.ExtraArms: return NonCollectible.Priest.ExtraArms_MoreArmsToken;
                    case Priest.ReliquaryOfSouls: return NonCollectible.Priest.ReliquaryofSouls_ReliquaryPrimeToken;
                    case NonCollectible.Priest.SeekGuidance_IlluminateTheVoidToken: return NonCollectible.Priest.SeekGuidance_XyrellaTheSanctifiedToken;
                    case NonCollectible.Priest.SeekGuidance_XyrellaTheSanctifiedToken: return NonCollectible.Priest.SeekGuidance_PurifiedShardToken;
                    case Rogue.Akama: return NonCollectible.Rogue.Akama_AkamaPrimeToken;
                    case Rogue.BeneathTheGrounds: return NonCollectible.Rogue.BeneaththeGrounds_NerubianAmbushToken;
                    case Rogue.BloodsailFlybooter: return NonCollectible.Rogue.BloodsailFlybooter_SkyPirateToken;
                    case Rogue.BoneBaron: return NonCollectible.Neutral.GrimNecromancer_SkeletonToken;
                    case Rogue.DeadlyFork: return NonCollectible.Rogue.DeadlyFork_SharpFork;
                    case Rogue.FaldoreiStrider: return NonCollectible.Rogue.FaldoreiStrider_SpiderAmbushEnchantment;
                    case Rogue.LoanShark: return NonCollectible.Neutral.TheCoinCore;
                    case Rogue.RazorpetalLasher: return NonCollectible.Rogue.RazorpetalVolley_RazorpetalToken;
                    case Rogue.RazorpetalVolley: return NonCollectible.Rogue.RazorpetalVolley_RazorpetalToken;
                    case Rogue.ShadowOfDeath: return NonCollectible.Rogue.ShadowofDeath_ShadowToken;
                    case Rogue.TheCavernsBelow: return NonCollectible.Rogue.TheCavernsBelow_CrystalCoreTokenUNGORO;
                    case Rogue.UmbralSkulker: return NonCollectible.Neutral.TheCoinCore;
                    case Rogue.Wanted: return NonCollectible.Neutral.Coin;
                    case Rogue.Waxadred: return NonCollectible.Rogue.Waxadred_WaxadredsCandleToken;
                    case Shaman.LadyVashj: return NonCollectible.Shaman.LadyVashj_VashjPrimeToken;
                    case Shaman.UniteTheMurlocs: return NonCollectible.Shaman.UnitetheMurlocs_MegafinToken;
                    case Shaman.WhiteEyes: return NonCollectible.Shaman.WhiteEyes_TheStormGuardianToken;
                    case NonCollectible.Shaman.CommandtheElements_TameTheFlamesToken: return NonCollectible.Shaman.CommandtheElements_StormcallerBrukanToken;
                    case NonCollectible.Warlock.TheDemonSeed_CompleteTheRitualToken: return NonCollectible.Warlock.TheDemonSeed_BlightbornTamsinToken;
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
                    case Warrior.BumperCar: return NonCollectible.Neutral.BumperCar_DarkmoonRiderToken;
                    case Warrior.ClockworkGoblin: return NonCollectible.Neutral.SeaforiumBomber_BombToken;
                    case Warrior.DirehornHatchling: return NonCollectible.Warrior.DirehornHatchling_DirehornMatriarchToken;
                    case Warrior.ExploreUngoro: return NonCollectible.Warrior.ExploreUnGoro_ChooseYourPathToken;
                    case Warrior.FirePlumesHeart: return NonCollectible.Warrior.FirePlumesHeart_SulfurasToken;
                    case Warrior.KargathBladefist: return NonCollectible.Warrior.KargathBladefist_KargathPrimeToken;
                    case Warrior.IronJuggernaut: return NonCollectible.Warrior.IronJuggernaut_BurrowingMineToken;
                    case NonCollectible.Warrior.RaidtheDocks_SecureTheSuppliesToken: return NonCollectible.Warrior.RaidtheDocks_CapnRokaraToken;
                    case Warrior.Wrenchcalibur: return NonCollectible.Neutral.SeaforiumBomber_BombToken;

                    case Neutral.AncientShade: return NonCollectible.Neutral.AncientShade_AncientCurseToken;
                    case Neutral.BadLuckAlbatross: return NonCollectible.Neutral.BadLuckAlbatross_AlbatrossToken;
                    case Neutral.BananaBuffoon: return NonCollectible.Neutral.BananaBuffoon_BananasToken;
                    case Neutral.BananaVendor: return NonCollectible.Neutral.BananaVendor_BananasToken;
                    case Neutral.BootyBayBookie: return NonCollectible.Neutral.TheCoinCore;
                    case Neutral.BurglyBully: return NonCollectible.Neutral.TheCoinCore;
                    case NonCollectible.Neutral.SurlyMobGILNEAS: return NonCollectible.Neutral.AngryMobGILNEAS;
                    case NonCollectible.Neutral.AngryMobGILNEAS: return NonCollectible.Neutral.CrazedMobGILNEAS;
                    case NonCollectible.Neutral.SurlyMobTavernBrawl: return NonCollectible.Neutral.AngryMobTavernBrawl;
                    case NonCollectible.Neutral.AngryMobTavernBrawl: return NonCollectible.Neutral.CrazedMobTavernBrawl;
                    case NonCollectible.Neutral.CoinPouchGILNEAS: return NonCollectible.Neutral.SackOfCoinsGILNEAS;
                    case NonCollectible.Neutral.SackOfCoinsGILNEAS: return NonCollectible.Neutral.HeftySackOfCoinsGILNEAS;
                    case NonCollectible.Neutral.CoinPouchTavernBrawl: return NonCollectible.Neutral.SackOfCoinsTavernBrawl;
                    case NonCollectible.Neutral.SackOfCoinsTavernBrawl: return NonCollectible.Neutral.HeftySackOfCoinsTavernBrawl;
                    case NonCollectible.Neutral.CreepyCurioGILNEAS: return NonCollectible.Neutral.HauntedCurioGILNEAS;
                    case NonCollectible.Neutral.HauntedCurioGILNEAS: return NonCollectible.Neutral.CursedCurioGILNEAS;
                    case NonCollectible.Neutral.CreepyCurioTavernBrawl: return NonCollectible.Neutral.HauntedCurioTavernBrawl;
                    case NonCollectible.Neutral.HauntedCurioTavernBrawl: return NonCollectible.Neutral.CursedCurioTavernBrawl;
                    case NonCollectible.Neutral.MilitiaHornGILNEAS: return NonCollectible.Neutral.VeteransMilitiaHornGILNEAS;
                    case NonCollectible.Neutral.OldMilitiaHornGILNEAS: return NonCollectible.Neutral.MilitiaHornGILNEAS;
                    case NonCollectible.Neutral.MilitiaHornTavernBrawl: return NonCollectible.Neutral.VeteransMilitiaHornTavernBrawl;
                    case NonCollectible.Neutral.OldMilitiaHornTavernBrawl: return NonCollectible.Neutral.MilitiaHornTavernBrawl;
                    case Neutral.Doomcaller: return Neutral.Cthun;
                    case Neutral.EliseTheTrailblazer: return NonCollectible.Neutral.ElisetheTrailblazer_UngoroPackToken;
                    case Neutral.EliseStarseeker: return NonCollectible.Neutral.EliseStarseeker_MapToTheGoldenMonkeyToken;
                    case Neutral.FeralGibberer: return Neutral.FeralGibberer;
                    case Neutral.FireFly: return NonCollectible.Neutral.FireFly_FlameElementalToken;
                    case Neutral.FishyFlyer: return NonCollectible.Neutral.FishyFlyer_SpectralFlyerToken;
                    case NonCollectible.Neutral.ForgingQueldelar: return NonCollectible.Neutral.QueldelarTavernBrawl;
                    case Neutral.EncumberedPackMule: return Neutral.EncumberedPackMule;
                    case Neutral.HakkarTheSoulflayer: return NonCollectible.Neutral.HakkartheSoulflayer_CorruptedBloodToken;
                    case Neutral.HoardingDragon: return NonCollectible.Neutral.TheCoinCore;
                    case Neutral.IgneousElemental: return NonCollectible.Neutral.FireFly_FlameElementalToken;
                    case Neutral.InfestedGoblin: return NonCollectible.Neutral.WrappedGolem_ScarabToken;
                    case Neutral.KingMukla: return NonCollectible.Neutral.BananasMissions;
                    case Neutral.LicensedAdventurer: return NonCollectible.Neutral.TheCoinCore;
                    case Neutral.MailboxDancer: return NonCollectible.Neutral.TheCoinCore;
                    case Neutral.Mankrik: return NonCollectible.Neutral.Mankrik_OlgraMankriksWifeToken;
                    case Neutral.MuklaTyrantOfTheVale: return NonCollectible.Neutral.BananasMissions;
                    case Neutral.PortalKeeper: return NonCollectible.Neutral.PortalKeeper_FelhoundPortalToken;
                    case Neutral.PortalOverfiend: return NonCollectible.Neutral.PortalKeeper_FelhoundPortalToken;
                    case Neutral.SeaforiumBomber: return NonCollectible.Neutral.SeaforiumBomber_BombToken;
                    case Neutral.SmugSenior: return NonCollectible.Neutral.SmugSenior_SpectralSeniorToken;
                    case Neutral.SneakyDelinquent: return NonCollectible.Neutral.SneakyDelinquent_SpectralDelinquentToken;
                    case Neutral.SoldierOfFortune: return NonCollectible.Neutral.TheCoinCore;
                    case Neutral.SparkDrill: return NonCollectible.Neutral.SparkDrill_SparkToken;
                    case Neutral.SparkEngine: return NonCollectible.Neutral.SparkDrill_SparkToken;
                    case NonCollectible.Neutral.TheCandle: return NonCollectible.Neutral.TheCandle;
                    case NonCollectible.Neutral.TheDarkness: return NonCollectible.Neutral.TheDarkness_DarknessCandleToken;
                    case Neutral.WeaselTunneler: return Neutral.WeaselTunneler;
                    case NonCollectible.Neutral.EliseStarseeker_MapToTheGoldenMonkeyToken: return NonCollectible.Neutral.EliseStarseeker_GoldenMonkeyToken;

                    case Neutral.BalefulBanker:
                    case Neutral.DollmasterDorian:
                    case Neutral.DragonBreeder:
                    case Neutral.Sathrovarr:
                    case Neutral.ZolaTheGorgon:
                    case Druid.Recycle:
                    case Druid.Splintergraft:
                    case Druid.MarkOfTheSpikeshell:
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

                    case Neutral.PotionOfIllusion:
                        if (node.Parent.Type == typeof(Parser.ReplayData.GameActions.Action))
                        {
                            var act = node.Parent.Object as Parser.ReplayData.GameActions.Action;
                            var existingEntity = GameState.CurrentEntities[act.Entity];
                            var controllerId = existingEntity.GetController();

                            if (GameState.EntityIdsOnBoardWhenPlayingPotionOfIllusion != null)
                            {
                                var boardLeftToHandleForPlayer = GameState.EntityIdsOnBoardWhenPlayingPotionOfIllusion[controllerId];
                                if (boardLeftToHandleForPlayer.Count > 0)
                                {
                                    var entityToCopy = boardLeftToHandleForPlayer[0];
                                    GameState.EntityIdsOnBoardWhenPlayingPotionOfIllusion[controllerId].RemoveAt(0);
                                    return entityToCopy.CardId;
                                }
                            }
                        }
                        return null;

                    case Neutral.YseraTheDreamerCore:
                        if (node.Parent.Type == typeof(Parser.ReplayData.GameActions.Action))
                        {
                            var act = node.Parent.Object as Parser.ReplayData.GameActions.Action;
                            var existingEntity = GameState.CurrentEntities[act.Entity];
                            var dreamCardsLeft = existingEntity.CardIdsToCreate;
                            if (dreamCardsLeft.Count == 0)
                            {
                                dreamCardsLeft = new List<string>()
                                {
                                    NonCollectible.DreamCards.NightmareExpert1,
                                    NonCollectible.DreamCards.Dream,
                                    NonCollectible.DreamCards.LaughingSister,
                                    NonCollectible.DreamCards.YseraAwakens,
                                    NonCollectible.DreamCards.EmeraldDrake,
                                };
                                existingEntity.CardIdsToCreate = dreamCardsLeft;
                            }
                            var cardId = dreamCardsLeft[0];
                            dreamCardsLeft.RemoveAt(0);
                            return cardId;
                        }
                        return null;

                    case NonCollectible.Rogue.FindtheImposter_SpymasterScabbsToken:
                        if (node.Parent.Type == typeof(Parser.ReplayData.GameActions.Action))
                        {
                            var act = node.Parent.Object as Parser.ReplayData.GameActions.Action;
                            var existingEntity = GameState.CurrentEntities[act.Entity];
                            var gizmosLeft = existingEntity.CardIdsToCreate;
                            if (gizmosLeft.Count == 0)
                            {
                                gizmosLeft = new List<string>()
                                {
                                    NonCollectible.Rogue.FindtheImposter_SpyOMaticToken,
                                    NonCollectible.Rogue.FindtheImposter_FizzflashDistractorToken,
                                    NonCollectible.Rogue.FindtheImposter_HiddenGyrobladeToken,
                                    NonCollectible.Rogue.FindtheImposter_UndercoverMoleToken,
                                    NonCollectible.Rogue.FindtheImposter_NoggenFogGeneratorToken,
                                };
                                existingEntity.CardIdsToCreate = gizmosLeft;
                            }
                            var cardId = gizmosLeft[0];
                            gizmosLeft.RemoveAt(0);
                            return cardId;
                        }
                        return null;
                }
            }

            if (node.Parent != null && node.Parent.Type == typeof(Parser.ReplayData.GameActions.Action))
            {
                var action = node.Parent.Object as Parser.ReplayData.GameActions.Action;

                if (action.Type == (int)BlockType.TRIGGER)
                {
                    var actionEntity = GameState.CurrentEntities.ContainsKey(action.Entity)
                            ? GameState.CurrentEntities[action.Entity]
                            : null;
                    // Tamsin Roana
                    if (actionEntity != null && actionEntity.CardId == Warlock.TamsinRoame)
                    {
                        // Now get the parent PLAY action
                        if (node.Parent.Parent != null && node.Parent.Parent.Type == typeof(Parser.ReplayData.GameActions.Action))
                        {
                            var playAction = node.Parent.Parent.Object as Parser.ReplayData.GameActions.Action;
                            if (playAction.Type == (int)BlockType.PLAY)
                            {
                                // Normally at this stage the game state entity has already been updated
                                var playedEntity = GameState.CurrentEntities.ContainsKey(playAction.Entity)
                                    ? GameState.CurrentEntities[playAction.Entity]
                                    : null;
                                return playedEntity?.CardId;
                            }
                        }
                    }

                    // Keymaster Alabaster
                    if (actionEntity != null && GameState.LastCardDrawnEntityId > 0 && actionEntity.CardId == Neutral.KeymasterAlabaster)
                    {
                        var lastDrawnEntity = GameState.CurrentEntities.ContainsKey(GameState.LastCardDrawnEntityId)
                            ? GameState.CurrentEntities[GameState.LastCardDrawnEntityId]
                            : null;
                        return lastDrawnEntity?.CardId;
                    }

                    // Plagiarize
                    if (action.TriggerKeyword == (int)GameTag.SECRET && actionEntity != null && actionEntity.KnownEntityIds.Count > 0 && actionEntity.CardId == Rogue.Plagiarize)
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

                    // Diligent Notetaker
                    if (action.TriggerKeyword == (int)GameTag.SPELLBURST && actionEntity != null && GameState.LastCardPlayedEntityId > 0 && actionEntity.CardId == Shaman.DiligentNotetaker)
                    {
                        var lastPlayedEntity = GameState.CurrentEntities.ContainsKey(GameState.LastCardPlayedEntityId)
                            ? GameState.CurrentEntities[GameState.LastCardPlayedEntityId]
                            : null;
                        return lastPlayedEntity?.CardId;
                    }
                }

                if (action.Type == (int)BlockType.POWER)
                {
                    var actionEntity = GameState.CurrentEntities[action.Entity];
                    if (actionEntity.CardId == Hunter.DevouringSwarm)
                    {
                        if (actionEntity.CardIdsToCreate.Count == 0)
                        {
                            var controller = actionEntity.GetController();
                            var deathBlock = action.Data
                                .Where(data => data is Action)
                                .Select(data => data as Action)
                                .Where(a => a.Type == (int)BlockType.DEATHS)
                                .FirstOrDefault();
                            if (deathBlock != null)
                            {
                                var deadEntities = deathBlock.Data
                                    .Where(data => data is TagChange)
                                    .Select(data => data as TagChange)
                                    .Where(tag => tag.Name == (int)GameTag.ZONE && tag.Value == (int)Zone.GRAVEYARD)
                                    .Select(tag => GameState.CurrentEntities[tag.Entity])
                                    .Where(entity => entity.GetController() == controller);
                                actionEntity.CardIdsToCreate = deadEntities.Select(entity => entity.CardId).ToList();
                            }
                        }
                        if (actionEntity.CardIdsToCreate.Count > 0)
                        {
                            var cardId = actionEntity.CardIdsToCreate[0];
                            actionEntity.CardIdsToCreate.RemoveAt(0);
                            return cardId;
                        }
                    }

                    // Second card for Archivist Elysiana
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

                    // Southsea Scoundrel
                    if (actionEntity.CardId == Neutral.SouthseaScoundrel)
                    {
                        // If we are the ones who draw it, it's all good, and if it's teh opponent, 
                        // then we know it's the same one
                        var cardDrawn = action.Data
                            .Where(data => data is TagChange)
                            .Select(data => data as TagChange)
                            .Where(tag => tag.Name == (int)GameTag.ZONE && tag.Value == (int)Zone.HAND)
                            .Where(tag => GameState.CurrentEntities.ContainsKey(tag.Entity) && GameState.CurrentEntities[tag.Entity].CardId?.Count() > 0)
                            .FirstOrDefault();
                        return cardDrawn != null ? GameState.CurrentEntities[cardDrawn.Entity].CardId : null;
                    }

                    // Vanessa VanCleed
                    if (actionEntity.CardId == Rogue.VanessaVancleefCore)
                    {
                        var vanessaControllerId = GameState.CurrentEntities[actionEntity.Entity].GetController();
                        var playerIds = GameState.CardsPlayedByPlayerEntityId.Keys;
                        foreach (var playerId in playerIds)
                        {
                            if (playerId != vanessaControllerId)
                            {
                                var cardsPlayedByOpponentByTurn = GameState.CardsPlayedByPlayerEntityId[playerId];
                                if (cardsPlayedByOpponentByTurn == null || cardsPlayedByOpponentByTurn.Count == 0)
                                {
                                    return null;
                                }
                                var cardsPlayedByOpponent = cardsPlayedByOpponentByTurn.SelectMany(entry => entry.Value).ToList();
                                if (cardsPlayedByOpponent == null || cardsPlayedByOpponent.Count == 0)
                                {
                                    return null;
                                }
                                var lastCardPlayedByOpponentEntityId = cardsPlayedByOpponent.Last();
                                var lastCardPlayedByOpponent = GameState.CurrentEntities[lastCardPlayedByOpponentEntityId];
                                return lastCardPlayedByOpponent?.CardId;
                            }
                        }
                    }

                    // Ace in the Hole
                    if (actionEntity.CardId == NonCollectible.Rogue.AceInTheHole)
                    {
                        var actionControllerId = actionEntity.GetController();
                        if (actionEntity.KnownEntityIds.Count == 0)
                        {
                            var cardsPlayedByPlayerByTurn = GameState.CardsPlayedByPlayerEntityId[actionControllerId];
                            if (cardsPlayedByPlayerByTurn == null || cardsPlayedByPlayerByTurn.Count == 0)
                            {
                                return null;
                            }

                            var lastTurn = GameState.GetGameEntity().GetTag(GameTag.TURN) - 2;
                            if (!cardsPlayedByPlayerByTurn.ContainsKey(lastTurn))
                            {
                                return null;
                            }

                            var cardsPlayedLastTurn = cardsPlayedByPlayerByTurn[lastTurn];
                            if (cardsPlayedLastTurn.Count == 0)
                            {
                                return null;
                            }

                            actionEntity.KnownEntityIds = cardsPlayedLastTurn.Select(entityId => entityId).ToList();
                        }

                        if (actionEntity.KnownEntityIds.Count > 0)
                        {
                            var entities = actionEntity.KnownEntityIds.Select(entityId => GameState.CurrentEntities[entityId]).ToList();
                            var nextCard = entities[0].CardId;
                            actionEntity.KnownEntityIds.Remove(entities[0].Entity);
                            return nextCard;
                        }

                    }

                    // Lady Liadrin
                    // The spells are created in random order, so we can't flag them
                }
            }

            // Libram of Wisdom
            if (node.Type == typeof(FullEntity) && (node.Object as FullEntity).SubSpellInEffect?.Prefab == "Librams_SpawnToHand_Book")
            {
                return Paladin.LibramOfWisdom;
            }

            return null;
        }

        public static string GetBuffCardId(int creatorEntityId, string creatorCardId)
        {
            switch (creatorCardId)
            {
                case CardIds.Collectible.Warlock.TamsinRoame: return CardIds.NonCollectible.Warlock.TamsinRoame_GatheredShadowsEnchantment;
                default: return null;
            }

        }

        public static string GetBuffingCardCardId(int creatorEntityId, string creatorCardId)
        {
            switch (creatorCardId)
            {
                case CardIds.Collectible.Warlock.TamsinRoame: return creatorCardId;
                default: return null;
            }
        }
    }
}
