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
                    case AbyssalWave: return SirakessCultist_AbyssalCurseToken;
                    case AdorableInfestation: return AdorableInfestation_MarsuulCubToken;
                    case Akama_BT_713: return Akama_AkamaPrimeToken;
                    case AncientShade: return AncientShade_AncientCurseToken;
                    case AngryMob: return CrazedMob;
                    case ArchmageAntonidas: return FireballCore_CORE_CS2_029;
                    case ArchsporeMsshifn: return ArchsporeMsshifn_MsshifnPrimeToken;
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
                    case BeneathTheGrounds: return BeneathTheGrounds_NerubianAmbushToken;
                    case BloodsailFlybooter: return BloodsailFlybooter_SkyPirateToken;
                    case BoneBaron: return GrimNecromancer_SkeletonToken;
                    case BootyBayBookie: return TheCoinCore;
                    case Bottomfeeder: return Bottomfeeder;
                    case BringOnRecruitsTavernBrawl: return SilverHandRecruitLegacyToken;
                    case BronzeHerald: return BronzeHerald_BronzeDragonToken;
                    case BuildASnowman_BuildASnowbruteToken: return BuildASnowman_BuildASnowgreToken;
                    case BuildASnowman: return BuildASnowman_BuildASnowbruteToken;
                    case BumperCar: return BumperCar_DarkmoonRiderToken;
                    case BurglyBully: return TheCoinCore;
                    case ChainsOfDread_AV_316hp: return DreadlichTamsin_FelRiftToken;
                    case ClockworkGoblin_DAL_060: return SeaforiumBomber_BombToken;
                    case CoinPouch_SackOfCoinsTavernBrawl: return CoinPouch_HeftySackOfCoinsTavernBrawl;
                    case CoinPouch: return SackOfCoins;
                    case CoinPouchTavernBrawl: return CoinPouch_SackOfCoinsTavernBrawl;
                    case CommandTheElements_TameTheFlamesToken: return CommandTheElements_StormcallerBrukanToken;
                    case ConfectionCyclone: return ConfectionCyclone_SugarElementalToken;
                    case ConjureManaBiscuit: return ConjureManaBiscuit_ManaBiscuitToken;
                    case CreepyCurio_HauntedCurioTavernBrawl: return CreepyCurio_CursedCurioTavernBrawl;
                    case CreepyCurio: return HauntedCurio;
                    case CreepyCurioTavernBrawl: return CreepyCurio_HauntedCurioTavernBrawl;
                    case CurseOfAgony: return CurseOfAgony_AgonyToken;
                    case CurseOfRafaam: return CurseOfRafaam_CursedToken;
                    case DeadlyFork: return DeadlyFork_SharpFork;
                    case DeckOfWonders: return DeckOfWonders_ScrollOfWonderToken;
                    case DefendTheDwarvenDistrict_KnockEmDownToken: return DefendTheDwarvenDistrict_TavishMasterMarksmanToken;
                    case DirehornHatchling: return DirehornHatchling_DirehornMatriarchToken;
                    case Doomcaller: return Cthun_OG_280;
                    case DraggedBelow: return SirakessCultist_AbyssalCurseToken;
                    case DragonbaneShot: return DragonbaneShot;
                    case DrawOffensivePlayTavernBrawlEnchantment: return OffensivePlayTavernBrawl;
                    case DreadlichTamsin_AV_316: return DreadlichTamsin_FelRiftToken;
                    case DrygulchJailor: return SilverHandRecruitLegacyToken;
                    case EliseStarseekerCore: return UnearthedRaptor_MapToTheGoldenMonkeyToken;
                    case EliseTheTrailblazer: return EliseTheTrailblazer_UngoroPackToken;
                    case EncumberedPackMule: return EncumberedPackMule;
                    case ExcavatedEvil: return ExcavatedEvil;
                    case ExploreUngoro: return ExploreUngoro_ChooseYourPathToken;
                    case ExtraArms: return ExtraArms_MoreArmsToken;
                    case FaldoreiStrider: return FaldoreiStrider_SpiderAmbush;
                    case FeralGibberer: return FeralGibberer;
                    case FinalShowdown_CloseThePortalToken: return DemonslayerKurtrusToken;
                    case FireFly: return FireFly_FlameElementalToken;
                    case FirePlumesHeart: return FirePlumesHeart_SulfurasToken;
                    case FirstFlame: return FirstFlame_SecondFlameToken;
                    case FishyFlyer: return FishyFlyer_SpectralFlyerToken;
                    case FlameGeyser: return FireFly_FlameElementalToken;
                    case ForgottenTorch: return ForgottenTorch_RoaringTorchToken;
                    case FullBlownEvil: return FullBlownEvil;
                    case GhastlyConjurer: return MirrorImageLegacy_CS2_027;
                    case GildedGargoyle: return TheCoinCore;
                    case HakkarTheSoulflayer: return HakkarTheSoulflayer_CorruptedBloodToken;
                    case HalazziTheLynx: return Springpaw_LynxToken;
                    case HauntedCurio: return CursedCurio;
                    case HighPriestessJeklik: return HighPriestessJeklik;
                    case HoardingDragon_LOOT_144: return TheCoinCore;
                    case IgneousElemental: return FireFly_FlameElementalToken;
                    case Ignite: return Ignite;
                    case Impbalming: return Impbalming_WorthlessImpToken;
                    case InfernalStrikeTavernBrawl: return TwinSlice_SecondSliceToken;
                    case InfestedGoblin: return WrappedGolem_ScarabToken;
                    case IronJuggernaut: return IronJuggernaut_BurrowingMineToken;
                    case JadeIdol: return JadeIdol;
                    case JungleGiants: return JungleGiants_BarnabusTheStomperToken;
                    case KanrethadEbonlocke: return KanrethadEbonlocke_KanrethadPrimeToken;
                    case KargathBladefist_BT_123: return KargathBladefist_KargathPrimeToken;
                    case KingMukla_CORE_EX1_014: return KingMukla_BananasLegacyToken;
                    case KingMuklaLegacy: return KingMukla_BananasLegacyToken;
                    case KingMuklaVanilla: return KingMukla_BananasLegacyToken;
                    case KoboldTaskmaster: return KoboldTaskmaster_ArmorScrapToken;
                    case LadyVashj_BT_109: return LadyVashj_VashjPrimeToken;
                    case LakkariSacrifice: return LakkariSacrifice_NetherPortalToken_UNG_829t1;
                    case LicensedAdventurer: return TheCoinCore;
                    case LoanShark: return TheCoinCore;
                    case MailboxDancer: return TheCoinCore;
                    case Malorne: return Malorne;
                    case Mankrik: return Mankrik_OlgraMankriksWifeToken;
                    case Marrowslicer: return SchoolSpirits_SoulFragmentToken;
                    case MidaPureLight_ONY_028: return MidaPureLight_FragmentOfMidaToken;
                    case MilitiaHorn: return VeteransMilitiaHorn;
                    case MuklaTyrantOfTheVale: return KingMukla_BananasLegacyToken;
                    case MurgurMurgurgle: return MurgurMurgurgle_MurgurglePrimeToken;
                    case OldMilitiaHorn_MilitiaHornTavernBrawl: return OldMilitiaHorn_VeteransMilitiaHornTavernBrawl;
                    case OldMilitiaHorn: return MilitiaHorn;
                    case OldMilitiaHornTavernBrawl: return OldMilitiaHorn_MilitiaHornTavernBrawl;
                    case OpenTheWaygate: return OpenTheWaygate_TimeWarpToken;
                    case Parrrley_DED_005: return Parrrley_DED_005;
                    case PlagueOfMurlocs: return TwistPlagueOfMurlocs_SurpriseMurlocsToken;
                    case PortalKeeper: return PortalKeeper_FelhoundPortalToken;
                    case PortalOverfiend: return PortalKeeper_FelhoundPortalToken;
                    case Pyros_PyrosToken_UNG_027t2: return Pyros_PyrosToken_UNG_027t4;
                    case Pyros: return Pyros_PyrosToken_UNG_027t2;
                    case Queldelar_ForgingQueldelarToken_LOOTA_842t: return QueldelarTavernBrawl;
                    case Queldelar_ForgingQueldelarToken_ONY_005tc7t: return Queldelar_ForgingQueldelarToken_ONY_005tc7t;
                    case RaidTheDocks_SecureTheSuppliesToken: return RaidTheDocks_CapnRokaraToken;
                    case RamCommander: return RamCommander_BattleRamToken;
                    case RaptorHatchling: return RaptorHatchling_RaptorPatriarchToken;
                    case RazorpetalLasher: return RazorpetalVolley_RazorpetalToken;
                    case RazorpetalVolley: return RazorpetalVolley_RazorpetalToken;
                    case ReliquaryOfSouls: return ReliquaryOfSouls_ReliquaryPrimeToken;
                    case RemoteControlledGolem_SW_097: return RemoteControlledGolem_GolemPartsToken;
                    case Rhonin: return ArcaneMissilesLegacy;
                    case RinTheFirstDisciple_TheFinalSealToken: return RinTheFirstDisciple_AzariTheDevourerToken;
                    case RinTheFirstDisciple_TheFirstSealToken: return RinTheFirstDisciple_TheSecondSealToken;
                    case RinTheFirstDisciple_TheFourthSealToken: return RinTheFirstDisciple_TheFinalSealToken;
                    case RinTheFirstDisciple_TheSecondSealToken: return RinTheFirstDisciple_TheThirdSealToken;
                    case RinTheFirstDisciple: return RinTheFirstDisciple_TheFirstSealToken;
                    case RunawayGyrocopter: return RunawayGyrocopter;
                    case SackOfCoins: return HeftySackOfCoins;
                    case SandwaspQueen: return SandwaspQueen_SandwaspToken;
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
                    case SirakessCultist: return SirakessCultist_AbyssalCurseToken;
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
                    case Springpaw: return Springpaw_LynxToken;
                    case SteamSurger: return FlameGeyser;
                    case SunscaleRaptor: return SunscaleRaptor;
                    case SurlyMob_AngryMobTavernBrawl: return SurlyMob_CrazedMobTavernBrawl;
                    case SurlyMob: return AngryMob;
                    case SurlyMobTavernBrawl: return SurlyMob_AngryMobTavernBrawl;
                    case TheCandle: return TheCandle;
                    case TheCavernsBelow: return TheCavernsBelow_CrystalCoreToken;
                    case TheDarkness_LOOT_526: return TheDarkness_DarknessCandleToken;
                    case TheDemonSeed_CompleteTheRitualToken: return TheDemonSeed_BlightbornTamsinToken;
                    case TheLastKaleidosaur: return TheLastKaleidosaur_GalvadonToken;
                    case TheMarshQueen: return TheMarshQueen_QueenCarnassaToken;
                    case ThrowGlaive: return ThrowGlaive; // TO CHECK
                    case TwinSlice_BT_175: return TwinSlice_SecondSliceToken;
                    case UmbralSkulker: return TheCoinCore;
                    case UnearthedRaptor_MapToTheGoldenMonkeyToken: return UnearthedRaptor_GoldenMonkeyToken;
                    case UniteTheMurlocs: return UniteTheMurlocs_MegafinToken;
                    case UrzulHorror: return UrzulHorror_LostSoulToken;
                    case VioletSpellwing: return ArcaneMissilesLegacy;
                    case Wanted: return Coin;
                    case Waxadred: return Waxadred_WaxadredsCandleToken;
                    case WeaselTunneler: return WeaselTunneler;
                    case WhiteEyes: return WhiteEyes_TheStormGuardianToken;
                    case WitchwoodApple: return WitchwoodApple_TreantToken;
                    case Wrenchcalibur: return SeaforiumBomber_BombToken;
                    case YseraUnleashed: return YseraUnleashed_DreamPortalToken;
                    case Zaqul_TSC_959: return SirakessCultist_AbyssalCurseToken;
                    case ZixorApexPredator: return ZixorApexPredator_ZixorPrimeToken;

                    case BalefulBanker:
                    case DollmasterDorian:
                    case DragonBreeder:
                    case Sathrovarr:
                    case ZolaTheGorgon:
                    case ZolaTheGorgonCore:
                    case Recycle:
                    case Splintergraft:
                    case MarkOfTheSpikeshell:
                    case DireFrenzy:
                    case ManicSoulcaster:
                    case HolyWater:
                    case Seance:
                    case GangUp:
                    case LabRecruiter:
                    case Shadowcaster:
                    case TogwagglesScheme:
                        if (node.Parent.Type == typeof(Action))
                        {
                            var act = node.Parent.Object as Action;
                            var target = GameState.CurrentEntities[act.Target];
                            if (target != null)
                            {
                                return target.CardId;
                            }
                        }
                        return null;

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
                                    if (GameState.CurrentEntities.ContainsKey(targetId))
                                    {
                                        return GameState.CurrentEntities[targetId].CardId;
                                    }
                                }
                            }
                        }
                        return null;

                    case ManaBind:
                    case FrozenClone:
                        if (node.Parent.Type == typeof(Parser.ReplayData.GameActions.Action)
                            && node.Parent.Parent?.Type == typeof(Parser.ReplayData.GameActions.Action))
                        {
                            var act = node.Parent.Parent.Object as Parser.ReplayData.GameActions.Action;
                            var existingEntity = GameState.CurrentEntities[act.Entity];
                            return existingEntity.CardId;
                        }
                        return null;

                    case Duplicate:
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

                    case PotionOfIllusion:
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

                    case YseraTheDreamerCore:
                        if (node.Parent.Type == typeof(Parser.ReplayData.GameActions.Action))
                        {
                            var act = node.Parent.Object as Parser.ReplayData.GameActions.Action;
                            var existingEntity = GameState.CurrentEntities[act.Entity];
                            var dreamCardsLeft = existingEntity.CardIdsToCreate;
                            if (dreamCardsLeft.Count == 0)
                            {
                                dreamCardsLeft = new List<string>()
                                {
                                    NightmareLegacy,
                                    DreamLegacy,
                                    LaughingSisterLegacy,
                                    YseraAwakensLegacy,
                                    EmeraldDrakeLegacy,
                                };
                                existingEntity.CardIdsToCreate = dreamCardsLeft;
                            }
                            var cardId = dreamCardsLeft[0];
                            dreamCardsLeft.RemoveAt(0);
                            return cardId;
                        }
                        return null;

                    case FindTheImposter_SpymasterScabbsToken:
                        if (node.Parent.Type == typeof(Parser.ReplayData.GameActions.Action))
                        {
                            var act = node.Parent.Object as Parser.ReplayData.GameActions.Action;
                            var existingEntity = GameState.CurrentEntities[act.Entity];
                            var gizmosLeft = existingEntity.CardIdsToCreate;
                            if (gizmosLeft.Count == 0)
                            {
                                gizmosLeft = new List<string>()
                                {
                                    FindTheImposter_SpyOMaticToken,
                                    FindTheImposter_FizzflashDistractorToken,
                                    FindTheImposter_HiddenGyrobladeToken,
                                    UndercoverMoleToken,
                                    FindTheImposter_NoggenFogGeneratorToken,
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

            if (node.Parent != null && node.Parent.Type == typeof(Action))
            {
                var action = node.Parent.Object as Action;

                if (action.Type == (int)BlockType.TRIGGER)
                {
                    var actionEntity = GameState.CurrentEntities.ContainsKey(action.Entity)
                            ? GameState.CurrentEntities[action.Entity]
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
                                var playedEntity = GameState.CurrentEntities.ContainsKey(playAction.Entity)
                                    ? GameState.CurrentEntities[playAction.Entity]
                                    : null;
                                return playedEntity?.CardId;
                            }
                        }
                    }

                    // Keymaster Alabaster
                    if (actionEntity != null && GameState.LastCardDrawnEntityId > 0 && actionEntity.CardId == KeymasterAlabaster)
                    {
                        var lastDrawnEntity = GameState.CurrentEntities.ContainsKey(GameState.LastCardDrawnEntityId)
                            ? GameState.CurrentEntities[GameState.LastCardDrawnEntityId]
                            : null;
                        return lastDrawnEntity?.CardId;
                    }

                    // Plagiarize
                    if (action.TriggerKeyword == (int)GameTag.SECRET && actionEntity != null && actionEntity.KnownEntityIds.Count > 0 && actionEntity.CardId == Plagiarize)
                    {
                        var plagiarizeController = actionEntity.GetEffectiveController();
                        var entitiesPlayedByActivePlayer = actionEntity.KnownEntityIds
                            .Select(entityId => GameState.CurrentEntities[entityId])
                            .Where(card => card.GetEffectiveController() != -1 && card.GetEffectiveController() != plagiarizeController)
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
                    if (action.TriggerKeyword == (int)GameTag.SPELLBURST && actionEntity != null && GameState.LastCardPlayedEntityId > 0 && actionEntity.CardId == DiligentNotetaker)
                    {
                        var lastPlayedEntity = GameState.CurrentEntities.ContainsKey(GameState.LastCardPlayedEntityId)
                            ? GameState.CurrentEntities[GameState.LastCardPlayedEntityId]
                            : null;
                        return lastPlayedEntity?.CardId;
                    }

                    // Felsoul Jailer
                    if (actionEntity.CardId == FelsoulJailerCore && actionEntity.CardIdsToCreate.Count > 0)
                    {
                        var result = actionEntity.CardIdsToCreate[0];
                        actionEntity.CardIdsToCreate.RemoveAt(0);
                        return result;
                    }

                    // Nellie
                    if (actionEntity != null && actionEntity.CardId == NellieTheGreatThresher_NelliesPirateShipToken && action.TriggerKeyword == (int)GameTag.DEATHRATTLE)
                    {
                        var pirateShipEntity = GameState.CurrentEntities[creatorEntityId];
                        var nellieEntity = GameState.CurrentEntities[pirateShipEntity.GetTag(GameTag.CREATOR)];
                        if (pirateShipEntity.KnownEntityIds.Count == 0)
                        {
                            var crewmates = GameState.CurrentEntities.Values
                                .Where(entity => entity.GetTag(GameTag.CREATOR) == nellieEntity.Entity)
                                .Where(entity => entity.CardId != NellieTheGreatThresher_NelliesPirateShipToken)
                                .ToList();
                            var crewmatesEntityIds = crewmates.Select(entity => entity.Entity).ToList();
                            pirateShipEntity.KnownEntityIds = crewmatesEntityIds;
                        }
                        if (pirateShipEntity.KnownEntityIds.Count > 0)
                        {
                            var entities = pirateShipEntity.KnownEntityIds.Select(entityId => GameState.CurrentEntities[entityId]).ToList();
                            var nextCard = entities[0].CardId;
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
                        return GameState.CurrentEntities.ContainsKey(candidateEntityIds[0])
                            ? GameState.CurrentEntities[candidateEntityIds[0]]?.CardId
                            : null;
                    }
                }

                if (action.Type == (int)BlockType.POWER)
                {
                    var actionEntity = GameState.CurrentEntities[action.Entity];
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
                            return GameState.CurrentEntities[lastEntityId]?.CardId;
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
                            return GameState.CurrentEntities[lastEntityId]?.CardId;
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
                            .Where(tag => GameState.CurrentEntities.ContainsKey(tag.Entity) && GameState.CurrentEntities[tag.Entity].CardId?.Count() > 0)
                            .FirstOrDefault();
                        return cardDrawn != null ? GameState.CurrentEntities[cardDrawn.Entity].CardId : null;
                    }
                    // Vanessa VanCleed
                    else if (actionEntity.CardId == VanessaVancleefCore)
                    {
                        var vanessaControllerId = GameState.CurrentEntities[actionEntity.Entity].GetController();
                        var playerIds = GameState.CardsPlayedByPlayerEntityIdByTurn.Keys;
                        foreach (var playerId in playerIds)
                        {
                            if (playerId != vanessaControllerId)
                            {
                                var cardsPlayedByOpponentByTurn = GameState.CardsPlayedByPlayerEntityIdByTurn[playerId];
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
                    else if (actionEntity.CardId == AceInTheHoleTavernBrawlToken)
                    {
                        var actionControllerId = actionEntity.GetController();
                        if (actionEntity.KnownEntityIds.Count == 0)
                        {
                            var cardsPlayedByPlayerByTurn = GameState.CardsPlayedByPlayerEntityIdByTurn[actionControllerId];
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
                    // Ace in the Hole
                    else if (actionEntity.CardId == CommanderSivara_TSC_087)
                    {
                        if (actionEntity.PlayedWhileInHand.Count > 0)
                        {
                            var spells = actionEntity.PlayedWhileInHand
                                .Select(entityId => GameState.CurrentEntities[entityId])
                                .Where(entity => entity.IsSpell())
                                .ToList();
                            var firstSpellEntity = spells[0];
                            actionEntity.PlayedWhileInHand.Remove(firstSpellEntity.Entity);
                            return firstSpellEntity.CardId;
                        }
                    }
                    // Horde Operative
                    else if (actionEntity.CardId == HordeOperative)
                    {
                        var actionControllerId = actionEntity.GetController();
                        if (actionEntity.KnownEntityIds.Count == 0)
                        {
                            // Find all secrets currently in play
                            var allOpponentSecrets = GameState.CurrentEntities.Values
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
                            var currentSecretCardIds = GameState.CurrentEntities.Values
                                .Where(e => e.GetController() == actionControllerId)
                                .Where(e => e.GetZone() == (int)Zone.SECRET)
                                .Where(e => e.GetTag(GameTag.SECRET) == 1)
                                .OrderBy(e => e.GetTag(GameTag.ZONE_POSITION))
                                .Select(e => e.CardId)
                                .ToList();
                            var entities = actionEntity.KnownEntityIds
                                .Select(entityId => GameState.CurrentEntities[entityId])
                                .Where(e => !currentSecretCardIds.Contains(e.CardId))
                                .ToList();
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
                return LibramOfWisdom_BT_025;
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
