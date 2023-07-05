using HearthstoneReplays.Parser;
using HearthstoneReplays.Parser.ReplayData;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using System;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.Entities;
using System.Collections.Generic;
using static HearthstoneReplays.Events.CardIds;
using HearthstoneReplays.Parser.ReplayData.Meta;
using System.Linq;
using Action = HearthstoneReplays.Parser.ReplayData.GameActions.Action;
using Newtonsoft.Json.Linq;

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
                    if (creator?.CardId == YseraUnleashed_DreamPortalToken)
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

        public static string PredictCardId(GameState gameState, string creatorCardId, int creatorEntityId, Node node, string inputCardId = null, StateFacade stateFacade = null)
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
                    case AbyssalWave: return SirakessCultist_AbyssalCurseToken;
                    case Acornbearer: return Acornbearer_SquirrelToken;
                    case AdorableInfestation: return AdorableInfestation_MarsuulCubToken;
                    case AirRaid_YOD_012: return AirRaid_YOD_012ts;
                    case Akama_BT_713: return Akama_AkamaPrimeToken;
                    case AncientShade: return AncientShade_AncientCurseToken;
                    case AngryMob: return CrazedMob;
                    case ArcaneWyrm: return ArcaneBolt;
                    case ArchmageAntonidas: return FireballCore_CORE_CS2_029;
                    case ArchsporeMsshifn: return ArchsporeMsshifn_MsshifnPrimeToken;
                    case Arcsplitter: return ArcaneBolt;
                    case AstalorBloodsworn: return AstalorBloodsworn_AstalorTheProtectorToken;
                    case AstalorBloodsworn_AstalorTheProtectorToken: return AstalorBloodsworn_AstalorTheFlamebringerToken;
                    case AstralTiger: return AstralTiger;
                    case AstromancerSolarian: return AstromancerSolarian_SolarianPrimeToken;
                    case AwakenTheMakers: return AwakenTheMakers_AmaraWardenOfHopeToken;
                    case AzsharanDefector: return AzsharanDefector_SunkenDefectorToken;
                    case AzsharanGardens: return AzsharanGardens_SunkenGardensToken;
                    case AzsharanMooncatcher_TSC_644: return AzsharanMooncatcher_SunkenMooncatcherToken;
                    case AzsharanRitual: return AzsharanRitual_SunkenRitualToken;
                    case AzsharanSaber: return AzsharanSaber_SunkenSaberToken;
                    case AzsharanScavenger: return AzsharanScavenger_SunkenScavengerToken;
                    case AzsharanScroll: return AzsharanScroll_SunkenScrollToken;
                    case AzsharanSentinel: return AzsharanSentinel_SunkenSentinelToken;
                    case AzsharanSweeper_TSC_776: return AzsharanSweeper_SunkenSweeperToken;
                    case AzsharanTrident: return AzsharanTrident_SunkenTridentToken;
                    case AzsharanVessel: return AzsharanVessel_SunkenVesselToken;
                    case BadLuckAlbatross: return BadLuckAlbatross_AlbatrossToken;
                    case BagOfCoins_LOOTA_836: return TheCoinCore;
                    case BagOfCoinsTavernBrawl: return TheCoinCore;
                    case BananaBuffoon: return BananaBuffoon_BananasToken;
                    case BananaVendor: return BananaVendor_BananasToken;
                    case BarrelOfMonkeys: return BarrelOfMonkeys_BarrelOfMonkeysToken_ETC_207t;
                    case BarrelOfMonkeys_BarrelOfMonkeysToken_ETC_207t: return BarrelOfMonkeys_BarrelOfMonkeysToken_ETC_207t2;
                    case BaubleOfBeetles_ULDA_307: return BaubleOfBeetles_ULDA_307ts;
                    case BeOurGuestTavernBrawl: return TheCountess_LegendaryInvitationToken;
                    case BeneathTheGrounds: return BeneathTheGrounds_NerubianAmbushToken;
                    case BlessingOfTheAncients_DAL_351: return BlessingOfTheAncients_DAL_351ts;
                    case BloodsailFlybooter: return BloodsailFlybooter_SkyPirateToken;
                    case BoneBaron_CORE_ICC_065: return GrimNecromancer_SkeletonToken;
                    case BoneBaron_ICC_065: return GrimNecromancer_SkeletonToken;
                    case BookOfWonders: return DeckOfWonders_ScrollOfWonderToken;
                    case BootyBayBookie: return TheCoinCore;
                    case Bottomfeeder: return Bottomfeeder;
                    case BringOnRecruitsTavernBrawl: return SilverHandRecruitLegacyToken;
                    case BronzeHerald: return BronzeHerald_BronzeDragonToken;
                    case BuildASnowman_BuildASnowbruteToken: return BuildASnowman_BuildASnowgreToken;
                    case BuildASnowman: return BuildASnowman_BuildASnowbruteToken;
                    case BumperCar: return BumperCar_DarkmoonRiderToken;
                    case BunchOfBananas: return BunchOfBananas_BunchOfBananasToken_ETC_201t;
                    case BunchOfBananas_BunchOfBananasToken_ETC_201t: return BunchOfBananas_BunchOfBananasToken_ETC_201t2;
                    case BurglyBully: return TheCoinCore;
                    case TwistTheCoffers_CacheOfCashToken: return TheCoinCore;
                    case ChainsOfDread_AV_316hp: return DreadlichTamsin_FelRiftToken;
                    case ClockworkGoblin_DAL_060: return SeaforiumBomber_BombToken;
                    case CoinPouch_SackOfCoinsTavernBrawl: return CoinPouch_HeftySackOfCoinsTavernBrawl;
                    case CoinPouch: return SackOfCoins;
                    case CoinPouchTavernBrawl: return CoinPouch_SackOfCoinsTavernBrawl;
                    case CommandTheElements_TameTheFlamesToken: return CommandTheElements_StormcallerBrukanToken;
                    case ConfectionCyclone: return ConfectionCyclone_SugarElementalToken;
                    case ConjureManaBiscuit: return ConjureManaBiscuit_ManaBiscuitToken;
                    case ConjurersCalling_DAL_177: return ConjurersCalling_DAL_177ts;
                    case CreepyCurio_HauntedCurioTavernBrawl: return CreepyCurio_CursedCurioTavernBrawl;
                    case CreepyCurio: return HauntedCurio;
                    case CreepyCurioTavernBrawl: return CreepyCurio_HauntedCurioTavernBrawl;
                    case CurseOfAgony: return CurseOfAgony_AgonyToken;
                    case CurseOfRafaam: return CurseOfRafaam_CursedToken;
                    case Cutpurse: return TheCoinCore;
                    case DeadlyFork: return DeadlyFork_SharpFork;
                    case DeathbringerSaurfangCore_RLK_082: return DeathbringerSaurfangCore_RLK_082;
                    case DeckOfWonders: return DeckOfWonders_ScrollOfWonderToken;
                    case DesperateMeasures_DAL_141: return DesperateMeasures_DAL_141ts;
                    case DefendTheDwarvenDistrict_KnockEmDownToken: return DefendTheDwarvenDistrict_TavishMasterMarksmanToken;
                    case DirehornHatchling: return DirehornHatchling_DirehornMatriarchToken;
                    case Doomcaller: return Cthun_OG_279;
                    case DraggedBelow: return SirakessCultist_AbyssalCurseToken;
                    case DragonbaneShot: return DragonbaneShot;
                    case DrawOffensivePlayTavernBrawlEnchantment: return OffensivePlayTavernBrawl;
                    case DreadlichTamsin_AV_316: return DreadlichTamsin_FelRiftToken;
                    case DrygulchJailor: return SilverHandRecruitLegacyToken;
                    case EliseStarseeker_CORE_LOE_079: return UnearthedRaptor_MapToTheGoldenMonkeyToken;
                    case EliseStarseeker_LOE_079: return UnearthedRaptor_MapToTheGoldenMonkeyToken;
                    case EliseTheTrailblazer: return EliseTheTrailblazer_UngoroPackToken;
                    case EliteTaurenChampion: return EliteTaurenChampion_MoltenPickOfRockToken;
                    case EliteTaurenChampion_MoltenPickOfRockToken: return EliteTaurenChampion_MoltenPickOfRockToken;
                    case EncumberedPackMule: return EncumberedPackMule;
                    case ExcavatedEvil: return ExcavatedEvil;
                    case ExploreUngoro: return ExploreUngoro_ChooseYourPathToken;
                    case ExtraArms: return ExtraArms_MoreArmsToken;
                    case FaldoreiStrider: return FaldoreiStrider_SpiderAmbush;
                    case FeralGibberer: return FeralGibberer;
                    case FinalShowdown_CloseThePortalToken: return DemonslayerKurtrusToken;
                    case FindTheImposter_MarkedATraitorToken: return FindTheImposter_SpymasterScabbsToken;
                    case FireFly: return FireFly_FlameElementalToken;
                    case FirePlumesHeart: return FirePlumesHeart_SulfurasToken;
                    case FirstFlame: return FirstFlame_SecondFlameToken;
                    // Doesn't work that way. The card created in deck should be the wish, but we don't know about the card in hand
                    //case FirstWishTavernBrawl: return SecondWishTavernBrawl;
                    //case SecondWishTavernBrawl: return ThirdWishTavernBrawl;
                    case FishyFlyer: return FishyFlyer_SpectralFlyerToken;
                    case FlameGeyserCore: return FireFly_FlameElementalToken;
                    case FlameGeyser: return FireFly_FlameElementalToken;
                    case ForgottenTorch: return ForgottenTorch_RoaringTorchToken;
                    case Framester: return Framester_FramedToken;
                    case FreshScent_YOD_005: return FreshScent_YOD_005ts;
                    case FrostShardsTavernBrawl: return FrostShards_IceShardTavernBrawl;
                    case FrozenTouch: return FrozenTouch_FrozenTouchToken;
                    case FrozenTouch_FrozenTouchToken: return FrozenTouch;
                    case FullBlownEvil: return FullBlownEvil;
                    case GhastlyConjurer_CORE_ICC_069: return MirrorImageLegacy_CS2_027;
                    case GhastlyConjurer_ICC_069: return MirrorImageLegacy_CS2_027;
                    case GildedGargoyle_LOOT_534: return TheCoinCore;
                    case HakkarTheSoulflayer: return HakkarTheSoulflayer_CorruptedBloodToken;
                    case HalazziTheLynx: return Springpaw_LynxToken;
                    case Harpoon: return ArcaneShot;
                    case HauntedCurio: return CursedCurio;
                    case HeadcrackLegacy: return HeadcrackLegacy;
                    case HeadcrackVanilla: return HeadcrackVanilla;
                    case HighPriestessJeklik_CORE_TRL_252: return HighPriestessJeklik_CORE_TRL_252;
                    case HighPriestessJeklik_TRL_252: return HighPriestessJeklik_TRL_252;
                    case HoardingDragon_LOOT_144: return TheCoinCore;
                    case IgneousElemental: return FireFly_FlameElementalToken;
                    case Ignite: return Ignite;
                    case Impbalming: return Impbalming_WorthlessImpToken;
                    case InfernalStrikeTavernBrawl: return TwinSlice_SecondSliceToken;
                    case InfestedGoblin: return WrappedGolem_ScarabToken;
                    case InfinitizeTheMaxitude_InfinitizeTheMaxitudeEnchantment: return InfinitizeTheMaxitude;
                    case IronJuggernaut: return IronJuggernaut_BurrowingMineToken;
                    case JadeIdol: return JadeIdol;
                    case JadeIdol_JadeStash: return JadeIdol;
                    case JungleGiants: return JungleGiants_BarnabusTheStomperToken;
                    case KangorDancingKing: return KangorDancingKing;
                    case KanrethadEbonlocke: return KanrethadEbonlocke_KanrethadPrimeToken;
                    case KargathBladefist_BT_123: return KargathBladefist_KargathPrimeToken;
                    case KingMukla_CORE_EX1_014: return KingMukla_BananasLegacyToken;
                    case KingMuklaLegacy: return KingMukla_BananasLegacyToken;
                    case KingMuklaVanilla: return KingMukla_BananasLegacyToken;
                    case Kingsbane_LOOT_542: return Kingsbane_LOOT_542;
                    case KoboldTaskmaster: return KoboldTaskmaster_ArmorScrapToken;
                    case LadyVashj_BT_109: return LadyVashj_VashjPrimeToken;
                    case LakkariSacrifice: return LakkariSacrifice_NetherPortalToken_UNG_829t1;
                    case LicensedAdventurer: return TheCoinCore;
                    case LightforgedBlessing_DAL_568: return LightforgedBlessing_DAL_568ts;
                    case LoanShark: return TheCoinCore;
                    case Locuuuusts_ULDA_036: return GiantLocust_Locuuuusts;
                    case LocuuuustsTavernBrawl: return Locuuuusts_LocuuuustsTavernBrawl;
                    case Locuuuusts_ONY_005tb3: return Locuuuusts_GiantLocustToken_ONY_005tb3t2;
                    case LostInThePark_FeralFriendsyToken: return LostInThePark_GuffTheToughToken;
                    case MagneticMinesTavernBrawl: return SeaforiumBomber_BombToken;
                    case MailboxDancer: return TheCoinCore;
                    case Malorne: return Malorne;
                    case Mankrik: return Mankrik_OlgraMankriksWifeToken;
                    case Marrowslicer: return SchoolSpirits_SoulFragmentToken;
                    case MarvelousMyceliumTavernBrawlToken: return MarvelousMyceliumTavernBrawlToken;
                    case MidaPureLight_ONY_028: return MidaPureLight_FragmentOfMidaToken;
                    case MilitiaHorn: return VeteransMilitiaHorn;
                    case MisterMukla: return KingMukla_BananasLegacyToken;
                    case MuklaTyrantOfTheVale: return KingMukla_BananasLegacyToken;
                    case MurgurMurgurgle: return MurgurMurgurgle_MurgurglePrimeToken;
                    case MysticalMirage_ULDA_035: return MysticalMirage_ULDA_035ts;
                    case OldMilitiaHorn_MilitiaHornTavernBrawl: return OldMilitiaHorn_VeteransMilitiaHornTavernBrawl;
                    case OldMilitiaHorn: return MilitiaHorn;
                    case OldMilitiaHornTavernBrawl: return OldMilitiaHorn_MilitiaHornTavernBrawl;
                    case OpenTheWaygate: return OpenTheWaygate_TimeWarpToken;
                    case Parrrley_DED_005: return Parrrley_DED_005;
                    case PhotographerFizzle: return PhotographerFizzle_FizzlesSnapshotToken;
                    case PlagueOfMurlocs: return TwistPlagueOfMurlocs_SurpriseMurlocsToken;
                    case PortalKeeper: return PortalKeeper_FelhoundPortalToken;
                    case PortalOverfiend: return PortalKeeper_FelhoundPortalToken;
                    case PozzikAudioEngineer: return PozzikAudioEngineer_AudioBotToken;
                    case Pyros_PyrosToken_UNG_027t2: return Pyros_PyrosToken_UNG_027t4;
                    case Pyros: return Pyros_PyrosToken_UNG_027t2;
                    case Queldelar_ForgingQueldelarToken_LOOTA_842t: return QueldelarTavernBrawl;
                    case Queldelar_ForgingQueldelarToken_ONY_005tc7t: return Queldelar_ForgingQueldelarToken_ONY_005tc7t;
                    case RaidTheDocks_SecureTheSuppliesToken: return RaidTheDocks_CapnRokaraToken;
                    case RamCommander: return RamCommander_BattleRamToken;
                    case RapidFire_DAL_373: return RapidFire_DAL_373ts;
                    case RaptorHatchling: return RaptorHatchling_RaptorPatriarchToken;
                    case RatsOfExtraordinarySize: return RodentNest_RatToken;
                    case RayOfFrost_DAL_577: return RayOfFrost_DAL_577ts;
                    case RazorpetalLasher: return RazorpetalVolley_RazorpetalToken;
                    case RazorpetalVolley: return RazorpetalVolley_RazorpetalToken;
                    case ReliquaryOfSouls: return ReliquaryOfSouls_ReliquaryPrimeToken;
                    case RemoteControlledGolem_SW_097: return RemoteControlledGolem_GolemPartsToken;
                    case Rhonin: return ArcaneMissilesLegacy;
                    case RisingWinds: return Eagle_RisingWinds;
                    case RinTheFirstDisciple_TheFinalSealToken: return RinTheFirstDisciple_AzariTheDevourerToken;
                    case RinTheFirstDisciple_TheFirstSealToken: return RinTheFirstDisciple_TheSecondSealToken;
                    case RinTheFirstDisciple_TheFourthSealToken: return RinTheFirstDisciple_TheFinalSealToken;
                    case RinTheFirstDisciple_TheSecondSealToken: return RinTheFirstDisciple_TheThirdSealToken;
                    case RiseToTheOccasion_AvengeTheFallenToken: return RiseToTheOccasion_LightbornCarielToken;
                    case RinTheFirstDisciple: return RinTheFirstDisciple_TheFirstSealToken;
                    case RunawayGyrocopter: return RunawayGyrocopter;
                    case SackOfCoins: return HeftySackOfCoins;
                    case SandwaspQueen: return SandwaspQueen_SandwaspToken;
                    case SaxophoneSoloist: return SaxophoneSoloist;
                    case Schooling: return PiranhaSwarmer_PiranhaSwarmerToken_TSC_638t;
                    case SchoolSpirits_SCH_307: return SchoolSpirits_SoulFragmentToken;
                    case SchoolTeacher: return SchoolTeacher_NagalingToken;
                    case Scrapsmith: return Scrapsmith_ScrappyGruntToken;
                    case SeaforiumBomber: return SeaforiumBomber_BombToken;
                    case SecureTheDeck: return ClawLegacy;
                    case SeedsOfDestruction: return DreadlichTamsin_FelRiftToken;
                    case SeekGuidance_IlluminateTheVoidToken: return SeekGuidance_XyrellaTheSanctifiedToken;
                    case SeekGuidance_XyrellaTheSanctifiedToken: return XyrellaTheSanctified_PurifiedShard;
                    case SerpentWig_TSC_215: return SerpentWig_TSC_215;
                    case ShadowOfDeath_ULD_286: return ShadowOfDeath_ShadowToken;
                    case SinfulSousChef: return SilverHandRecruitLegacyToken;
                    case SirakessCultist: return SirakessCultist_AbyssalCurseToken;
                    case SisterSvalna: return SisterSvalna_VisionOfDarknessToken;
                    case Sleetbreaker: return Windchill_AV_266;
                    case SmugSenior: return SmugSenior_SpectralSeniorToken;
                    case Sn1pSn4p: return Sn1pSn4p;
                    case SneakyDelinquent: return SneakyDelinquent_SpectralDelinquentToken;
                    case SoldierOfFortune: return TheCoinCore;
                    case SorcerersGambit_ReachThePortalRoomToken: return SorcerersGambit_ArcanistDawngraspToken;
                    case SoulShear_SCH_701: return SchoolSpirits_SoulFragmentToken;
                    case SparkDrill_BOT_102: return SparkDrill_SparkToken;
                    case SparkEngine: return SparkDrill_SparkToken;
                    case SpiritJailer_SCH_700: return SchoolSpirits_SoulFragmentToken;
                    case Springpaw_TRL_348: return Springpaw_LynxToken;
                    case Springpaw_CORE_TRL_348: return Springpaw_LynxToken;
                    case StaffOfAmmunae_ULDA_041: return StaffOfAmmunae_ULDA_041ts;
                    case Starseeker: return MoonfireLegacy;
                    case SteamSurger: return FlameGeyser;
                    case SunscaleRaptor: return SunscaleRaptor;
                    case SurlyMob_AngryMobTavernBrawl: return SurlyMob_CrazedMobTavernBrawl;
                    case SurlyMob: return AngryMob;
                    case SurlyMobTavernBrawl: return SurlyMob_AngryMobTavernBrawl;
                    case TheCandle: return TheCandle;
                    case TheCandlesquestion: return TheCandlesquestion_TheCandlesquestion_DALA_714a;
                    case TheCandlesquestion_TheCandlesquestion_DALA_714a: return TheCandlesquestion_TheCandlesquestion_DALA_714b;
                    case TheCandlesquestion_TheCandlesquestion_DALA_714b: return TheCandlesquestion_TheCandlesquestion_DALA_714c;
                    case TheCavernsBelow: return TheCavernsBelow_CrystalCoreToken;
                    case TheCountess: return TheCountess_LegendaryInvitationToken;
                    case TheDarkness_LOOT_526: return TheDarkness_DarknessCandleToken;
                    case TheDemonSeed_CompleteTheRitualToken: return TheDemonSeed_BlightbornTamsinToken;
                    case TheForestsAid_DAL_256: return TheForestsAid_DAL_256ts;
                    case TheLastKaleidosaur: return TheLastKaleidosaur_GalvadonToken;
                    case TheHandOfRafaam: return CurseOfRafaam_CursedToken;
                    case TheMarshQueen: return TheMarshQueen_QueenCarnassaToken;
                    case TheMarshQueen_QueenCarnassaToken: return TheMarshQueen_CarnassasBroodToken;
                    case ThrowGlaive: return ThrowGlaive; // TO CHECK
                    case TinyThimbleTavernBrawl: return TinyThimble_RegularSizeThimbleTavernBrawl;
                    case TombPillager_LOE_012: return TheCoinCore;
                    case TombPillager_CORE_LOE_012: return TheCoinCore;
                    case TwinSlice_BT_175: return TwinSlice_SecondSliceToken;
                    case UmbralSkulker: return TheCoinCore;
                    case UnearthedRaptor_MapToTheGoldenMonkeyToken: return UnearthedRaptor_GoldenMonkeyToken;
                    case UnleashTheBeast_DAL_378: return UnleashTheBeast_DAL_378ts;
                    case UniteTheMurlocs: return UniteTheMurlocs_MegafinToken;
                    case UrzulHorror: return UrzulHorror_LostSoulToken;
                    case VioletSpellwing: return ArcaneMissilesLegacy;
                    case Wanted: return Coin;
                    case Waxadred: return Waxadred_WaxadredsCandleToken;
                    case WeaselTunneler: return WeaselTunneler;
                    case WhiteEyes: return WhiteEyes_TheStormGuardianToken;
                    case WildGrowthCore: return WildGrowth_ExcessManaLegacyToken;
                    case WildGrowthLegacy: return WildGrowth_ExcessManaLegacyToken;
                    case WildGrowthVanilla: return WildGrowth_ExcessManaLegacyToken;
                    case WitchwoodAppleCore: return WitchwoodApple_TreantToken;
                    case WitchwoodApple: return WitchwoodApple_TreantToken;
                    case Wrenchcalibur: return SeaforiumBomber_BombToken;
                    case YseraUnleashed: return YseraUnleashed_DreamPortalToken;
                    case Zaqul_TSC_959: return SirakessCultist_AbyssalCurseToken;
                    case ZixorApexPredator: return ZixorApexPredator_ZixorPrimeToken;

                        // Action targets
                    case BalefulBanker:
                    case DireFrenzy_CORE_GIL_828:
                    case DireFrenzy_GIL_828:
                    case DollmasterDorian:
                    case DragonBreeder:
                    case GangUp:
                    case HolyWater:
                    case LabRecruiter:
                    case ManicSoulcaster:
                    case MarkOfTheSpikeshell:
                    case PowerChordSynchronize:
                    case Recycle:
                    case Sathrovarr:
                    case Seance:
                    case Shadowcaster:
                    case Splintergraft:
                    case TogwagglesScheme:
                    case ZolaTheGorgon:
                    case ZolaTheGorgonCore:
                        if (node.Parent.Type == typeof(Action))
                        {
                            var act = node.Parent.Object as Action;
                            var target = gameState.CurrentEntities.GetValueOrDefault(act.Target);
                            if (target != null)
                            {
                                return target.CardId;
                            }
                        }
                        return null;

                        // Multiple known cards
                    case YseraTheDreamerCore:
                        return AddMultipleKnownCards(gameState, node, new List<string>()
                            {
                                NightmareLegacy,
                                DreamLegacy,
                                LaughingSisterLegacy,
                                YseraAwakensLegacy,
                                EmeraldDrakeLegacy,
                            });
                    case RivendareWarrider:
                        return AddMultipleKnownCards(gameState, node, new List<string>()
                            {
                                    RivendareWarrider_BlaumeuxFamineriderToken,
                                    RivendareWarrider_KorthazzDeathriderToken,
                                    RivendareWarrider_ZeliekConquestriderToken
                            });
                    case FindTheImposter_SpymasterScabbsToken:
                        return AddMultipleKnownCards(gameState, node, new List<string>()
                            {
                                    FindTheImposter_SpyOMaticToken,
                                    FindTheImposter_FizzflashDistractorToken,
                                    FindTheImposter_HiddenGyrobladeToken,
                                    UndercoverMoleToken,
                                    FindTheImposter_NoggenFogGeneratorToken,
                            });
                    case MoonbeastTavernBrawlToken:
                    case KiriChosenOfElune:
                    case KiriChosenOfEluneCore:
                        return AddMultipleKnownCards(gameState, node, new List<string>()
                            {
                                    SolarEclipse,
                                    LunarEclipse,
                            });

                    case AugmentedElekk:
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

                    case ExpiredMerchant:
                        Console.WriteLine("TODO! Implement ExpiredMerchant card guess");
                        return null;

                    case FightOverMe:
                        if (node.Parent.Type == typeof(Parser.ReplayData.GameActions.Action))
                        {
                            var act = node.Parent.Object as Parser.ReplayData.GameActions.Action;
                            var actionEntity = gameState.CurrentEntities.GetValueOrDefault(act.Entity);
                            if (actionEntity != null)
                            {
                                if (actionEntity.KnownEntityIds.Count == 0)
                                {
                                    // This is a hack, because the DEATHS block is processed after the entities are added to
                                    // hand
                                    var fightingEntities = act.Data
                                        .Where(d => d is TagChange)
                                        .Select(d => d as TagChange)
                                        // Not sure what this is
                                        .Where(d => d.Name == 1715)
                                        // This works because the check is done asynchronously, so the DEATHS block should have been processed at this point
                                        // Actually, it doesn't work, as the deaths are processed too long after
                                        //.Where(d => GameState.CurrentEntities.GetValueOrDefault(d.Entity)?.InGraveyard() ?? false)
                                        .Select(d => d.Entity)
                                        .Select(d => gameState.CurrentEntities.GetValueOrDefault(d, default(FullEntity)))
                                        .Where(d => d != null)
                                        .Where(d => d.IsInGraveyard())
                                        .ToList();
                                    actionEntity.KnownEntityIds = fightingEntities
                                        .Select(d => d.Entity)
                                        .ToList();
                                }
                                if (actionEntity?.KnownEntityIds?.Count > 0)
                                {
                                    var nextEntity = actionEntity.KnownEntityIds[0];
                                    actionEntity.KnownEntityIds.RemoveAt(0);
                                    return gameState.CurrentEntities.GetValueOrDefault(nextEntity)?.CardId;
                                }
                            }
                        }
                        return null;

                    case SpiritOfTheDead:
                        if (node.Parent.Type == typeof(Parser.ReplayData.GameActions.Action))
                        {
                            var act = node.Parent.Object as Parser.ReplayData.GameActions.Action;
                            foreach (var data in act.Data)
                            {
                                if (data.GetType() == typeof(MetaData))
                                {
                                    var info = (data as MetaData).MetaInfo[0];
                                    var targetId = info.Entity;
                                    if (gameState.CurrentEntities.ContainsKey(targetId))
                                    {
                                        return gameState.CurrentEntities[targetId].CardId;
                                    }
                                }
                            }
                        }
                        return null;

                    case ManaBind:
                    case FrozenClone_CORE_ICC_082:
                    case FrozenClone_ICC_082:
                        if (node.Parent.Type == typeof(Action) && node.Parent.Parent?.Type == typeof(Action))
                        {
                            var act = node.Parent.Parent.Object as Action;
                            var existingEntity = gameState.CurrentEntities.GetValueOrDefault(act.Entity);
                            return existingEntity?.CardId;
                        }
                        return null;

                    case Duplicate:
                    case CheatDeathCore:
                    case CheatDeath:
                        if (node.Parent.Type == typeof(Parser.ReplayData.GameActions.Action))
                        {
                            var act = node.Parent.Object as Parser.ReplayData.GameActions.Action;
                            if (act.Type == (int)BlockType.TRIGGER)
                            {
                                var metaData = act.Data
                                    .Where(data => data is MetaData)
                                    .Select(data => data as MetaData)
                                    .Where(data => data.Meta == (int)MetaDataType.HISTORY_TARGET)
                                    .Where(data => data.MetaInfo != null)
                                    .Where(data => data.MetaInfo.Count > 0)
                                    .FirstOrDefault();
                                if (metaData != null)
                                {
                                    var entityId = metaData.MetaInfo[0].Entity;
                                    var existingEntity = gameState.CurrentEntities.GetValueOrDefault(entityId);
                                    return existingEntity?.CardId;
                                }
                            }
                        }
                        return null;

                    case PotionOfIllusion:
                        if (node.Parent.Type == typeof(Parser.ReplayData.GameActions.Action))
                        {
                            var act = node.Parent.Object as Parser.ReplayData.GameActions.Action;
                            var existingEntity = gameState.CurrentEntities.GetValueOrDefault(act.Entity);
                            if (existingEntity == null)
                            {
                                return null;
                            }

                            var controllerId = existingEntity.GetController();

                            if (gameState.EntityIdsOnBoardWhenPlayingPotionOfIllusion != null)
                            {
                                var boardLeftToHandleForPlayer = gameState.EntityIdsOnBoardWhenPlayingPotionOfIllusion[controllerId];
                                if (boardLeftToHandleForPlayer.Count > 0)
                                {
                                    var entityToCopy = boardLeftToHandleForPlayer[0];
                                    gameState.EntityIdsOnBoardWhenPlayingPotionOfIllusion[controllerId].RemoveAt(0);
                                    return entityToCopy.CardId;
                                }
                            }
                        }
                        return null;


                    case SuspiciousAlchemist_AMysteryEnchantment:
                        var enchantmentEntity = gameState.CurrentEntities.GetValueOrDefault(creatorEntityId);
                        if (enchantmentEntity == null)
                        {
                            return null;
                        }

                        var attachedToEntityId = enchantmentEntity.GetTag(GameTag.ATTACHED);
                        var sourceEntity = gameState.CurrentEntities.GetValueOrDefault(attachedToEntityId);
                        if (sourceEntity?.KnownEntityIds?.Count > 0)
                        {
                            var nextEntity = sourceEntity.KnownEntityIds[0];
                            sourceEntity.KnownEntityIds.RemoveAt(0);
                            return gameState.CurrentEntities.GetValueOrDefault(nextEntity)?.CardId;
                        }
                        return null;
                }

                // Handle echo
                var creatorEntity = gameState.CurrentEntities.GetValueOrDefault(creatorEntityId);
                if (creatorEntity?.GetTag(GameTag.ECHO) == 1 || creatorEntity?.GetTag(GameTag.NON_KEYWORD_ECHO) == 1)
                {
                    return creatorCardId;
                }
            }

            if (node.Parent != null && node.Parent.Type == typeof(Action))
            {
                var action = node.Parent.Object as Action;

                if (action.Type == (int)BlockType.TRIGGER)
                {
                    var actionEntity = gameState.CurrentEntities.ContainsKey(action.Entity)
                            ? gameState.CurrentEntities[action.Entity]
                            : null;
                    // Tamsin Roana
                    if (actionEntity != null && actionEntity.CardId == TamsinRoame_BAR_918)
                    {
                        // Now get the parent PLAY action
                        if (node.Parent.Parent != null && node.Parent.Parent.Type == typeof(Parser.ReplayData.GameActions.Action))
                        {
                            var playAction = node.Parent.Parent.Object as Parser.ReplayData.GameActions.Action;
                            if (playAction.Type == (int)BlockType.PLAY)
                            {
                                // Normally at this stage the game state entity has already been updated
                                var playedEntity = gameState.CurrentEntities.ContainsKey(playAction.Entity)
                                    ? gameState.CurrentEntities[playAction.Entity]
                                    : null;
                                return playedEntity?.CardId;
                            }
                        }
                    }

                    // Keymaster Alabaster
                    if (actionEntity != null && gameState.LastCardDrawnEntityId > 0 && actionEntity.CardId == KeymasterAlabaster)
                    {
                        var lastDrawnEntity = gameState.CurrentEntities.ContainsKey(gameState.LastCardDrawnEntityId)
                            ? gameState.CurrentEntities[gameState.LastCardDrawnEntityId]
                            : null;
                        return lastDrawnEntity?.CardId;
                    }

                    // Plagiarize
                    if (action.TriggerKeyword == (int)GameTag.SECRET && actionEntity != null && actionEntity.KnownEntityIds.Count > 0 
                        && (actionEntity.CardId == PlagiarizeCore || actionEntity.CardId == Plagiarize))
                    {
                        var plagiarizeController = actionEntity.GetEffectiveController();
                        var entitiesPlayedByActivePlayer = actionEntity.KnownEntityIds
                            .Select(entityId => gameState.CurrentEntities.GetValueOrDefault(entityId))
                            .Where(card => card != null && card.GetEffectiveController() != -1 && card.GetEffectiveController() != plagiarizeController)
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
                    if (action.TriggerKeyword == (int)GameTag.SPELLBURST && actionEntity != null && gameState.LastCardPlayedEntityId > 0 && actionEntity.CardId == DiligentNotetaker)
                    {
                        var lastPlayedEntity = gameState.CurrentEntities.ContainsKey(gameState.LastCardPlayedEntityId)
                            ? gameState.CurrentEntities[gameState.LastCardPlayedEntityId]
                            : null;
                        return lastPlayedEntity?.CardId;
                    }

                    // Felsoul Jailer
                    if ((actionEntity.CardId == FelsoulJailer || actionEntity.CardId == FelsoulJailerLegacy) && actionEntity.CardIdsToCreate.Count > 0)
                    {
                        var result = actionEntity.CardIdsToCreate[0];
                        actionEntity.CardIdsToCreate.RemoveAt(0);
                        return result;
                    }

                    // Nellie
                    if (actionEntity != null && actionEntity.CardId == NellieTheGreatThresher_NelliesPirateShipToken && action.TriggerKeyword == (int)GameTag.DEATHRATTLE)
                    {
                        var pirateShipEntity = gameState.CurrentEntities.GetValueOrDefault(creatorEntityId);
                        var nellieEntity = gameState.CurrentEntities.GetValueOrDefault(pirateShipEntity?.GetTag(GameTag.CREATOR) ?? -1);
                        if (pirateShipEntity?.KnownEntityIds.Count == 0)
                        {
                            var crewmates = gameState.CurrentEntities.Values
                                .Where(entity => entity.GetTag(GameTag.CREATOR) == nellieEntity?.Entity)
                                .Where(entity => entity.CardId != NellieTheGreatThresher_NelliesPirateShipToken)
                                .ToList();
                            var crewmatesEntityIds = crewmates.Select(entity => entity.Entity).ToList();
                            pirateShipEntity.KnownEntityIds = crewmatesEntityIds;
                        }
                        if (pirateShipEntity?.KnownEntityIds.Count > 0)
                        {
                            var entities = pirateShipEntity.KnownEntityIds.Select(entityId => gameState.CurrentEntities.GetValueOrDefault(entityId)).ToList();
                            var nextCard = entities[0]?.CardId;
                            pirateShipEntity.KnownEntityIds.RemoveAt(0);
                            return nextCard;
                        }
                        return null;
                    }

                    // Ice Trap
                    if (actionEntity != null
                        && (actionEntity.CardId == IceTrap || actionEntity.CardId == BeaststalkerTavish_ImprovedIceTrapToken)
                        && action.TriggerKeyword == (int)GameTag.SECRET)
                    {
                        var candidateEntityIds = action.Data
                            .Where(d => d is MetaData)
                            .Select(d => d as MetaData)
                            .Where(m => m.Meta == (int)MetaDataType.TARGET)
                            .SelectMany(m => m.MetaInfo)
                            .Select(info => info.Entity)
                            .ToList();
                        if (candidateEntityIds.Count != 1)
                        {
                            Logger.Log("WARN: could not determine with full accuracy Ice Trap's target", candidateEntityIds.Count);
                        }
                        if (candidateEntityIds.Count == 0)
                        {
                            return null;
                        }
                        return gameState.CurrentEntities.ContainsKey(candidateEntityIds[0])
                            ? gameState.CurrentEntities[candidateEntityIds[0]]?.CardId
                            : null;
                    }

                    // Getaway Kodo
                    if (actionEntity != null && actionEntity.CardId == GetawayKodo && action.TriggerKeyword == (int)GameTag.SECRET)
                    {
                        var candidateEntityIds = action.Data
                            .Where(d => d is MetaData)
                            .Select(d => d as MetaData)
                            .Where(m => m.Meta == (int)MetaDataType.HISTORY_TARGET)
                            .SelectMany(m => m.MetaInfo)
                            .Select(info => info.Entity)
                            .ToList();
                        if (candidateEntityIds.Count != 1)
                        {
                            Logger.Log("WARN: could not determine with full accuracy Getaway Kodo's target", candidateEntityIds.Count);
                        }
                        if (candidateEntityIds.Count == 0)
                        {
                            return null;
                        }
                        return gameState.CurrentEntities.ContainsKey(candidateEntityIds[0])
                            ? gameState.CurrentEntities[candidateEntityIds[0]]?.CardId
                            : null;
                    }

                    // Flesh Behemoth
                    if (actionEntity != null
                        && (actionEntity.CardId == IceTrap || actionEntity.CardId == FleshBehemoth_RLK_830)
                        && action.TriggerKeyword == (int)GameTag.DEATHRATTLE)
                    {
                        var candidateEntityIds = action.Data
                            .Where(d => d is ShowEntity)
                            .Select(d => d as ShowEntity)
                            .Select(e => e.Entity)
                            .ToList();
                        if (candidateEntityIds.Count != 1)
                        {
                            Logger.Log("WARN: could not determine with full accuracy Flesh Behemoth's target", candidateEntityIds.Count);
                        }
                        if (candidateEntityIds.Count == 0)
                        {
                            return null;
                        }
                        return gameState.CurrentEntities.ContainsKey(candidateEntityIds[0])
                            ? gameState.CurrentEntities[candidateEntityIds[0]]?.CardId
                            : null;
                    }
                }

                if (action.Type == (int)BlockType.POWER)
                {
                    var actionEntity = gameState.CurrentEntities.GetValueOrDefault(action.Entity);
                    if (actionEntity == null)
                    {
                        return null;
                    }

                    if (actionEntity.CardId == Griftah)
                    {
                        var candidates = action.Data
                            .Where(d => d is FullEntity)
                            .Select(d => d as FullEntity)
                            .Select(d => d.Entity)
                            .Select(d => gameState.CurrentEntities.GetValueOrDefault(d))
                            // Don't know what this tag means, but it's set for cards picked when playing Griftah
                            .Where(e => e != null && e.Tags.FirstOrDefault(t => t.Name == 2509)?.Value != 1)
                            .ToList();
                        var candidateCardIds = candidates.Select(e => e.CardId);
                        foreach (var cardId in candidateCardIds)
                        {
                            var totalCardIds = candidateCardIds.Where(c => c == cardId).Count();
                            // The card received by the opponent has not been revealed
                            if (totalCardIds == 1)
                            {
                                return cardId;
                            }
                        }
                        return null;
                    }

                    if (actionEntity.CardId == PowerOfCreation)
                    {
                        // At this point, the summoned cards have not yet been revealed, so we look for the GS logs for insight
                        if (stateFacade != null)
                        {
                            return stateFacade.GsState.GameState.CurrentEntities.Values
                                .Where(d => d.GetTag(GameTag.CREATOR) == actionEntity.Entity)
                                .Where(d => d.IsInPlay())
                                .FirstOrDefault()
                                ?.CardId;
                        }
                        return null;
                    }

                    if (actionEntity.CardId == DevouringSwarm)
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
                                    .Select(tag => gameState.CurrentEntities.GetValueOrDefault(tag.Entity))
                                    .Where(entity => entity?.GetController() == controller);
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
                    else if (actionEntity.CardId == ArchivistElysiana)
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
                            return gameState.CurrentEntities.GetValueOrDefault(lastEntityId)?.CardId;
                        }
                    }
                    // Second card for Kazakusan
                    else if (actionEntity.CardId == Kazakusan_ONY_005)
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
                            return gameState.CurrentEntities.GetValueOrDefault(lastEntityId)?.CardId;
                        }
                    }
                    // Southsea Scoundrel
                    else if (actionEntity.CardId == SouthseaScoundrel_BAR_081)
                    {
                        // If we are the ones who draw it, it's all good, and if it's teh opponent, 
                        // then we know it's the same one
                        var cardDrawn = action.Data
                            .Where(data => data is TagChange)
                            .Select(data => data as TagChange)
                            .Where(tag => tag.Name == (int)GameTag.ZONE && tag.Value == (int)Zone.HAND)
                            .Where(tag => gameState.CurrentEntities.ContainsKey(tag.Entity) && gameState.CurrentEntities[tag.Entity].CardId?.Count() > 0)
                            .FirstOrDefault();
                        return cardDrawn != null ? gameState.CurrentEntities.GetValueOrDefault(cardDrawn.Entity)?.CardId : null;
                    }
                    // Vanessa VanCleed
                    else if (actionEntity.CardId == VanessaVancleef_CORE_CS3_005 || actionEntity.CardId == VanessaVancleefLegacy)
                    {
                        var vanessaControllerId = gameState.CurrentEntities.GetValueOrDefault(actionEntity.Entity)?.GetController();
                        var playerIds = gameState.CardsPlayedByPlayerEntityIdByTurn.Keys;
                        foreach (var playerId in playerIds)
                        {
                            if (playerId != vanessaControllerId)
                            {
                                var cardsPlayedByOpponentByTurn = gameState.CardsPlayedByPlayerEntityIdByTurn[playerId];
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
                                var lastCardPlayedByOpponent = gameState.CurrentEntities.GetValueOrDefault(lastCardPlayedByOpponentEntityId);
                                return lastCardPlayedByOpponent?.CardId;
                            }
                        }
                    }
                    // Ace in the Hole
                    else if (actionEntity.CardId == AceInTheHoleTavernBrawlToken)
                    {
                        var actionControllerId = actionEntity.GetController();
                        if (actionEntity.KnownEntityIds.Count == 0)
                        {
                            var cardsPlayedByPlayerByTurn = gameState.CardsPlayedByPlayerEntityIdByTurn[actionControllerId];
                            if (cardsPlayedByPlayerByTurn == null || cardsPlayedByPlayerByTurn.Count == 0)
                            {
                                return null;
                            }

                            var lastTurn = gameState.GetGameEntity().GetTag(GameTag.TURN) - 2;
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
                            var entities = actionEntity.KnownEntityIds.Select(entityId => gameState.CurrentEntities.GetValueOrDefault(entityId)).ToList();
                            var nextCard = entities[0]?.CardId;
                            actionEntity.KnownEntityIds.Remove(entities[0].Entity);
                            return nextCard;
                        }
                    }
                    else if (actionEntity.CardId == PhotographerFizzle_FizzlesSnapshotToken)
                    {
                        if (actionEntity?.KnownEntityIds?.Count > 0)
                        {
                            var nextEntity = actionEntity.KnownEntityIds[0];
                            actionEntity.KnownEntityIds.RemoveAt(0);
                            return gameState.CurrentEntities.GetValueOrDefault(nextEntity)?.CardId;
                        }
                        return null;
                    }
                    // Sivara
                    else if (actionEntity.CardId == CommanderSivara_TSC_087)
                    {
                        if (actionEntity.PlayedWhileInHand.Count > 0)
                        {
                            var spells = actionEntity.PlayedWhileInHand
                                .Select(entityId => gameState.CurrentEntities.GetValueOrDefault(entityId))
                                .Where(entity => entity.IsSpell())
                                .ToList();
                            var firstSpellEntity = spells[0];
                            actionEntity.PlayedWhileInHand.Remove(firstSpellEntity.Entity);
                            return firstSpellEntity.CardId;
                        }
                    }
                    // TODO: move this to the more simple "action target" case?
                    else if (actionEntity.CardId == ColdStorage)
                    {
                        var targetEntityId = action.Data
                            .Where(data => data is MetaData)
                            .Select(data => data as MetaData)
                            .Where(meta => meta.Meta == (int)MetaDataType.TARGET)
                            .SelectMany(meta => meta.MetaInfo)
                            .Where(info => info != null)
                            .FirstOrDefault()
                            ?.Entity;
                        return targetEntityId != null ? gameState.CurrentEntities.GetValueOrDefault(targetEntityId.Value)?.CardId : null;
                    }
                    // Horde Operative
                    else if (actionEntity.CardId == HordeOperative)
                    {
                        var actionControllerId = actionEntity.GetController();
                        if (actionEntity.KnownEntityIds.Count == 0)
                        {
                            // Find all secrets currently in play
                            var allOpponentSecrets = gameState.CurrentEntities.Values
                                .Where(e => e.GetController() != actionControllerId)
                                .Where(e => e.GetZone() == (int)Zone.SECRET)
                                .Where(e => e.GetTag(GameTag.SECRET) == 1)
                                .OrderBy(e => e.GetTag(GameTag.ZONE_POSITION))
                                .ToList();
                            actionEntity.KnownEntityIds = allOpponentSecrets
                                .Select(e => e.Entity)
                                .ToList();
                        }

                        if (actionEntity.KnownEntityIds.Count > 0)
                        {
                            var currentSecretCardIds = gameState.CurrentEntities.Values
                                .Where(e => e.GetController() == actionControllerId)
                                .Where(e => e.GetZone() == (int)Zone.SECRET)
                                .Where(e => e.GetTag(GameTag.SECRET) == 1)
                                .OrderBy(e => e.GetTag(GameTag.ZONE_POSITION))
                                .Select(e => e.CardId)
                                .ToList();
                            var entities = actionEntity.KnownEntityIds
                                .Select(entityId => gameState.CurrentEntities.GetValueOrDefault(entityId))
                                .Where(e => e != null &&  !currentSecretCardIds.Contains(e.CardId))
                                .ToList();
                            var nextCard = entities[0].CardId;
                            actionEntity.KnownEntityIds.Remove(entities[0].Entity);
                            return nextCard;
                        }
                    }
                    // Conqueror's Banner
                    else if (actionEntity.CardId == ConquerorsBanner && node.Type == typeof(TagChange))
                    {
                        var tagChange = node.Object as TagChange;
                        // We need to use this trick because the creator is computed once the full block is completed
                        // (see comment in CardDrawFromDeckParser)
                        var nodeElement = action.Data
                            .Where(d => d is TagChange)
                            .Select(d => d as TagChange)
                            .Where(t => t.Entity == tagChange.Entity && t.Name == tagChange.Name && t.Value == tagChange.Value)
                            .FirstOrDefault();
                        var nodeIndex = action.Data.IndexOf(nodeElement);
                        var joustAction = action.Data
                            .GetRange(0, nodeIndex)
                            .Where(d => d is Action)
                            .Select(d => d as Action)
                            .Where(a => a.Index < node.Index)
                            .Where(a => a.Type == (int)BlockType.JOUST)
                            .LastOrDefault();
                        var lastJoust = joustAction?.Data
                            .Where(d => d is MetaData)
                            .Select(d => d as MetaData)
                            .Where(d => d.Meta == (int)MetaDataType.JOUST)
                            .LastOrDefault();
                        if (lastJoust != null && gameState.CurrentEntities.ContainsKey(lastJoust.Data))
                        {
                            var pickedEntity = gameState.CurrentEntities.GetValueOrDefault(lastJoust.Data);
                            return pickedEntity?.CardId;
                        }
                    }
                    // Doesn't really work for now because of timing issues (only works in test when there are no pauses)
                    else if (actionEntity.CardId == CardIds.DeathBlossomWhomper)
                    {
                        var enchantment = gameState.CurrentEntities.Values
                            .Where(e => e.GetCardType() == (int)CardType.ENCHANTMENT)
                            .Where(e => e.GetTag(GameTag.ATTACHED) == actionEntity.Entity)
                            .Where(e => e.GetTag(GameTag.CREATOR) == actionEntity.Entity)
                            .LastOrDefault();
                        var referencedEntityId = enchantment?.GetTag(GameTag.TAG_SCRIPT_DATA_NUM_1) ?? -1;
                        var linkedEntity = gameState.CurrentEntities.GetValueOrDefault(referencedEntityId);
                        if (linkedEntity != null)
                        {
                            return linkedEntity.CardId;
                        }
                    }

                    // Lady Liadrin
                    // The spells are created in random order, so we can't flag them
                }
            }

            // Libram of Wisdom
            if (node.Type == typeof(FullEntity) && (node.Object as FullEntity).SubSpellInEffect?.Prefab == "Librams_SpawnToHand_Book")
            {
                return LibramOfWisdom_BT_025;
            }

            return null;
        }

        private static string AddMultipleKnownCards(GameState gameState, Node node, List<string> cardsList)
        {
            if (node.Parent.Type == typeof(Parser.ReplayData.GameActions.Action))
            {
                var act = node.Parent.Object as Parser.ReplayData.GameActions.Action;
                var existingEntity = gameState.CurrentEntities.GetValueOrDefault(act.Entity);
                if (existingEntity == null)
                {
                    return null;
                }

                var cardsLeft = existingEntity.CardIdsToCreate;
                if (cardsLeft.Count == 0)
                {
                    cardsLeft = cardsList;
                    existingEntity.CardIdsToCreate = cardsLeft;
                }
                var cardId = cardsLeft[0];
                cardsLeft.RemoveAt(0);
                return cardId;
            }
            return null;
        }

        public static string GetBuffCardId(int creatorEntityId, string creatorCardId)
        {
            switch (creatorCardId)
            {
                case TamsinRoame_BAR_918: return TamsinRoame_GatheredShadowsEnchantment;
                default: return null;
            }

        }

        public static string GetBuffingCardCardId(int creatorEntityId, string creatorCardId)
        {
            switch (creatorCardId)
            {
                case TamsinRoame_BAR_918: return creatorCardId;
                default: return null;
            }
        }
    }
}
