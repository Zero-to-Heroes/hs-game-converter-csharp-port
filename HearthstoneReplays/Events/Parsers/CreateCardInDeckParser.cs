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

namespace HearthstoneReplays.Events.Parsers
{
    public class CreateCardInDeckParser : ActionParser
    {
        private GameState GameState { get; set; }
        private ParserState ParserState { get; set; }

        public CreateCardInDeckParser(ParserState ParserState)
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
            // In this case, a FullEntity is created with minimal info, and the real
            // card creation happens in the ShowEntity
            var appliesOnShowEntity = node.Type == typeof(ShowEntity)
                && (node.Object as ShowEntity).GetTag(GameTag.ZONE) == (int)Zone.DECK;
            var appliesOnFullEntity = node.Type == typeof(FullEntity)
                && (node.Object as FullEntity).GetTag(GameTag.ZONE) == (int)Zone.DECK
                // We don't want to include the entities created when the game starts
                && node.Parent.Type == typeof(Parser.ReplayData.GameActions.Action);
            return appliesOnShowEntity || appliesOnFullEntity;
        }

        public List<GameEventProvider> CreateGameEventProviderFromNew(Node node)
        {
            return null;
        }

        public List<GameEventProvider> CreateGameEventProviderFromClose(Node node)
        {
            if (node.Type == typeof(ShowEntity))
            {
                return CreateFromShowEntity(node);
            }
            else if (node.Type == typeof(FullEntity))
            {
                return CreateFromFullEntity(node);
            }
            return null;
        }

        private List<GameEventProvider> CreateFromShowEntity(Node node)
        {
            var showEntity = node.Object as ShowEntity;
            var currentCard = GameState.CurrentEntities[showEntity.Entity];
            // If the card is already present in the deck, do nothing
            if (currentCard.GetTag(GameTag.ZONE) == (int)Zone.DECK)
            {
                return null;
            }
            var creatorCardId = FindCardCreatorCardId(showEntity.GetTag(GameTag.CREATOR), node);
            var cardId = PredictCardId(creatorCardId, node);
            var controllerId = showEntity.GetTag(GameTag.CONTROLLER);
            var gameState = GameEvent.BuildGameState(ParserState, GameState);
            return new List<GameEventProvider> { GameEventProvider.Create(
                showEntity.TimeStamp,
                GameEvent.CreateProvider(
                    "CREATE_CARD_IN_DECK",
                    cardId,
                    controllerId,
                    showEntity.Entity,
                    ParserState,
                    gameState),
                true,
                node.CreationLogLine) };
        }

        private List<GameEventProvider> CreateFromFullEntity(Node node)
        {
            var fullEntity = node.Object as FullEntity;
            var creatorCardId = FindCardCreatorCardId(fullEntity.GetTag(GameTag.CREATOR), node);
            var cardId = PredictCardId(creatorCardId, node);
            var controllerId = fullEntity.GetTag(GameTag.CONTROLLER);
            var gameState = GameEvent.BuildGameState(ParserState, GameState);
            return new List<GameEventProvider> { GameEventProvider.Create(
                fullEntity.TimeStamp,
                GameEvent.CreateProvider(
                    "CREATE_CARD_IN_DECK",
                    cardId,
                    controllerId,
                    fullEntity.Id,
                    ParserState,
                    gameState,
                    new {
                        CreatorCardId = creatorCardId, // Used when there is no cardId, so we can show "created by ..."
                    }),
                true,
                node.CreationLogLine) };
        }

        private string FindCardCreatorCardId(int creatorTag, Node node)
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
                    return creator.CardId;
                }
            }
            return null;
        }

        private string PredictCardId(string creatorCardId, Node node)
        {
            switch(creatorCardId)
            {
                case Neutral.AncientShade: return NonCollectible.Neutral.AncientShade_AncientCurseToken;
                case NonCollectible.Neutral.AngryMob: return NonCollectible.Neutral.CrazedMob;
                case Neutral.BananaBuffoon: return NonCollectible.Neutral.BananaBuffoon_BananasToken;
                case Neutral.BootyBayBookie: return NonCollectible.Neutral.TheCoin;
                case Neutral.BurglyBully: return NonCollectible.Neutral.TheCoin;
                case NonCollectible.Neutral.CoinPouch: return NonCollectible.Neutral.SackOfCoins;
                case NonCollectible.Neutral.CreepyCurio: return NonCollectible.Neutral.HauntedCurio;
                case NonCollectible.Neutral.HauntedCurio: return NonCollectible.Neutral.CursedCurio;
                case Neutral.Doomcaller: return Neutral.Cthun;
                case Neutral.EliseTheTrailblazer: return NonCollectible.Neutral.ElisetheTrailblazer_UngoroPackToken;
                case Neutral.EliseStarseeker: return NonCollectible.Neutral.EliseStarseeker_MapToTheGoldenMonkeyToken;
                case Neutral.FeralGibberer: return Neutral.FeralGibberer;
                case Neutral.FireFly: return NonCollectible.Neutral.FireFly_FlameElementalToken;
                case Neutral.HakkarTheSoulflayer: return NonCollectible.Neutral.HakkartheSoulflayer_CorruptedBloodToken;
                case Neutral.HoardingDragon: return NonCollectible.Neutral.TheCoin;
                case Neutral.IgneousElemental: return NonCollectible.Neutral.FireFly_FlameElementalToken;
                case Neutral.KingMukla: return NonCollectible.Neutral.Bananas;
                case NonCollectible.Neutral.MilitiaHorn: return NonCollectible.Neutral.VeteransMilitiaHorn;
                case Neutral.MuklaTyrantOfTheVale: return NonCollectible.Neutral.Bananas;
                case NonCollectible.Neutral.OldMilitiaHorn: return NonCollectible.Neutral.MilitiaHorn;
                case Neutral.PortalKeeper: return NonCollectible.Neutral.PortalKeeper_FelhoundPortalToken;
                case Neutral.PortalOverfiend: return NonCollectible.Neutral.PortalKeeper_FelhoundPortalToken;
                case Neutral.SeaforiumBomber: return NonCollectible.Neutral.SeaforiumBomber_BombToken;
                case Neutral.SparkDrill: return NonCollectible.Neutral.SparkDrill_SparkToken;
                case Neutral.SparkEngine: return NonCollectible.Neutral.SparkDrill_SparkToken;
                case NonCollectible.Neutral.SurlyMob: return NonCollectible.Neutral.AngryMob;
                case NonCollectible.Neutral.TheCandle: return NonCollectible.Neutral.TheCandle;
                case NonCollectible.Neutral.TheDarkness: return NonCollectible.Neutral.TheDarkness_DarknessCandleToken;
                case Neutral.WeaselTunneler: return Neutral.WeaselTunneler;
                case NonCollectible.Neutral.EliseStarseeker_MapToTheGoldenMonkeyToken: return NonCollectible.Neutral.EliseStarseeker_GoldenMonkeyToken;
                case Druid.AstralTiger: return Druid.AstralTiger;
                case Druid.JadeIdol: return Druid.JadeIdol;
                case Druid.JungleGiants: return NonCollectible.Druid.JungleGiants_BarnabusTheStomperToken;
                case Druid.Malorne: return Druid.Malorne;
                case Hunter.HalazziTheLynx: return NonCollectible.Hunter.Springpaw_LynxToken;
                case Hunter.RaptorHatchling: return NonCollectible.Hunter.RaptorHatchling_RaptorPatriarchToken;
                case Hunter.Springpaw: return NonCollectible.Hunter.Springpaw_LynxToken;
                case Hunter.TheMarshQueen: return NonCollectible.Hunter.TheMarshQueen_QueenCarnassaToken;
                case Mage.ArchmageAntonidas: return Mage.Fireball;
                case Mage.DeckOfWonders: return NonCollectible.Mage.DeckofWonders_ScrollOfWonderToken;
                case Mage.FlameGeyser: return NonCollectible.Neutral.FireFly_FlameElementalToken;
                case Mage.ForgottenTorch: return NonCollectible.Mage.ForgottenTorch_RoaringTorchToken;
                case Mage.GhastlyConjurer: return Mage.MirrorImage;
                case Mage.OpenTheWaygate: return NonCollectible.Mage.OpentheWaygate_TimeWarpToken;
                case Mage.Pyros: return NonCollectible.Mage.Pyros_PyrosToken1;
                case Mage.Rhonin: return Mage.ArcaneMissiles;
                case NonCollectible.Mage.Pyros_PyrosToken1: return NonCollectible.Mage.Pyros_PyrosToken2;
                case Paladin.DrygulchJailor: return NonCollectible.Paladin.Reinforce_SilverHandRecruitToken;
                case Paladin.TheLastKaleidosaur: return NonCollectible.Paladin.TheLastKaleidosaur_GalvadonToken;
                case Priest.AwakenTheMakers: return NonCollectible.Priest.AwakentheMakers_AmaraWardenOfHopeToken;
                case Priest.GildedGargoyle: return NonCollectible.Neutral.TheCoin;
                case Priest.ExcavatedEvil: return Priest.ExcavatedEvil;
                case Priest.ExtraArms: return NonCollectible.Priest.ExtraArms_MoreArmsToken;
                case Rogue.BeneathTheGrounds: return NonCollectible.Rogue.BeneaththeGrounds_NerubianAmbushToken;
                case Rogue.BoneBaron: return NonCollectible.Neutral.GrimNecromancer_SkeletonToken;
                case Rogue.DeadlyFork: return NonCollectible.Rogue.DeadlyFork_SharpFork;
                case Rogue.FaldoreiStrider: return NonCollectible.Rogue.FaldoreiStrider_SpiderAmbushEnchantment;
                case Rogue.RazorpetalLasher: return NonCollectible.Rogue.RazorpetalVolley_RazorpetalToken;
                case Rogue.RazorpetalVolley: return NonCollectible.Rogue.RazorpetalVolley_RazorpetalToken;
                case Rogue.ShadowOfDeath: return NonCollectible.Rogue.ShadowofDeath_ShadowToken;
                case Rogue.TheCavernsBelow: return NonCollectible.Rogue.TheCavernsBelow_CrystalCoreTokenUNGORO;
                case Rogue.Wanted: return NonCollectible.Neutral.Coin;
                case Shaman.UniteTheMurlocs: return NonCollectible.Shaman.UnitetheMurlocs_MegafinToken;
                case Shaman.WhiteEyes: return NonCollectible.Shaman.WhiteEyes_TheStormGuardianToken;
                case Warlock.CurseOfRafaam: return NonCollectible.Warlock.CurseofRafaam_CursedToken;
                case Warlock.HighPriestessJeklik: return Warlock.HighPriestessJeklik;
                case Warlock.Impbalming: return NonCollectible.Warlock.Impbalming_WorthlessImpToken;
                case Warlock.LakkariSacrifice: return NonCollectible.Warlock.LakkariSacrifice_NetherPortalToken1;
                case Warlock.RinTheFirstDisciple: return NonCollectible.Warlock.RintheFirstDisciple_TheFirstSealToken;
                case NonCollectible.Warlock.RintheFirstDisciple_TheFirstSealToken : return NonCollectible.Warlock.RintheFirstDisciple_TheSecondSealToken;
                case NonCollectible.Warlock.RintheFirstDisciple_TheSecondSealToken: return NonCollectible.Warlock.RintheFirstDisciple_TheThirdSealToken;
                case NonCollectible.Warlock.RintheFirstDisciple_TheFourthSealToken: return NonCollectible.Warlock.RintheFirstDisciple_TheFinalSealToken;
                case NonCollectible.Warlock.RintheFirstDisciple_TheFinalSealToken: return NonCollectible.Warlock.RintheFirstDisciple_AzariTheDevourerToken;
                case Warrior.ClockworkGoblin: return NonCollectible.Neutral.SeaforiumBomber_BombToken;
                case Warrior.DirehornHatchling: return NonCollectible.Warrior.DirehornHatchling_DirehornMatriarchToken;
                case Warrior.ExploreUngoro: return NonCollectible.Warrior.ExploreUnGoro_ChooseYourPathToken;
                case Warrior.FirePlumesHeart: return NonCollectible.Warrior.FirePlumesHeart_SulfurasToken;
                case Warrior.IronJuggernaut: return NonCollectible.Warrior.IronJuggernaut_BurrowingMineToken;
                case Warrior.Wrenchcalibur: return NonCollectible.Neutral.SeaforiumBomber_BombToken;

                case Neutral.BalefulBanker:
                case Neutral.DollmasterDorian:
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
            }
            return null;
        }
    }
}
