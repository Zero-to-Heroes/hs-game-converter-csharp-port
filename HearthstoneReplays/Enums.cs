namespace HearthstoneReplays.Enums
{
	public enum BlockType
	{
		ATTACK = 1,
		CONTINUOUS = 2,
		JOUST = 2,
		POWER = 3,
		SCRIPT = 4,
		TRIGGER = 5,
		DEATHS = 6,
		PLAY = 7,
		FATIGUE = 8,
		RITUAL = 9,
		REVEAL_CARD = 10,
		GAME_RESET = 11,
		ACTION = 99,
	}

	public enum BnetGameType
	{
		BGT_UNKNOWN = 0,
		BGT_FRIENDS = 1,
		BGT_RANKED_STANDARD = 2,
		BGT_ARENA = 3,
		BGT_VS_AI = 4,
		BGT_TUTORIAL = 5,
		BGT_ASYNC = 6,
		BGT_NEWBIE = 9,
		BGT_CASUAL_STANDARD_NEWBIE = 9,
		BGT_CASUAL_STANDARD_NORMAL = 10,
		BGT_CASUAL_STANDARD = 10,
		BGT_TEST1 = 11,
		BGT_TEST2 = 12,
		BGT_TEST3 = 13,
		BGT_TAVERNBRAWL_PVP = 16,
		BGT_TAVERNBRAWL_1P_VERSUS_AI = 17,
		BGT_TAVERNBRAWL_2P_COOP = 18,
		BGT_RANKED_WILD = 30,
		BGT_CASUAL_WILD = 31,
		BGT_FSG_BRAWL_VS_FRIEND = 40,
		BGT_FSG_BRAWL_PVP = 41,
		BGT_FSG_BRAWL_1P_VERSUS_AI = 42,
		BGT_FSG_BRAWL_2P_COOP = 43,
		BGT_TOURNAMENT = 44,
	}

	public enum BnetRegion
	{
		REGION_UNINITIALIZED = -1,
		REGION_UNKNOWN = 0,
		REGION_US = 1,
		REGION_EU = 2,
		REGION_KR = 3,
		REGION_TW = 4,
		REGION_CN = 5,
		REGION_LIVE_VERIFICATION = 40,
		REGION_PTR_LOC = 41,
		REGION_MSCHWEITZER_BN11 = 52,
		REGION_MSCHWEITZER_BN12 = 53,
		REGION_DEV = 60,
		REGION_PTR = 98,
	}

	public enum Booster
	{
		INVALID = 0,
		CLASSIC = 1,
		GOBLINS_VS_GNOMES = 9,
		THE_GRAND_TOURNAMENT = 10,
		OLD_GODS = 11,
		FIRST_PURCHASE = 17,
		SIGNUP_INCENTIVE = 18,
		MEAN_STREETS = 19,
		UNGORO = 20,
		FROZEN_THRONE = 21,
		GOLDEN_CLASSIC_PACK = 23,
		KOBOLDS_AND_CATACOMBS = 30,
		KOBOLDS_CATACOMBS = 30,
		WITCHWOOD = 31,
		THE_BOOMSDAY_PROJECT = 38,
		MAMMOTH_BUNDLE = 41,
	}

	public enum BrawlType
	{
		BRAWL_TYPE_UNKNOWN = 0,
		BRAWL_TYPE_TAVERN_BRAWL = 1,
		BRAWL_TYPE_FIRESIDE_GATHERING = 2,
		BRAWL_TYPE_COUNT = 3,
	}

	public enum CardClass
	{
		INVALID = 0,
		DEATHKNIGHT = 1,
		DRUID = 2,
		HUNTER = 3,
		MAGE = 4,
		PALADIN = 5,
		PRIEST = 6,
		ROGUE = 7,
		SHAMAN = 8,
		WARLOCK = 9,
		WARRIOR = 10,
		DREAM = 11,
		NEUTRAL = 12,
		WHIZBANG = 13,
	}

	public enum CardSet
	{
		INVALID = 0,
		TEST_TEMPORARY = 1,
		CORE = 2,
		EXPERT1 = 3,
		HOF = 4,
		REWARD = 4,
		MISSIONS = 5,
		DEMO = 6,
		NONE = 7,
		CHEAT = 8,
		BLANK = 9,
		DEBUG_SP = 10,
		PROMO = 11,
		FP1 = 12,
		NAXX = 12,
		GVG = 13,
		PE1 = 13,
		BRM = 14,
		FP2 = 14,
		PE2 = 15,
		TGT = 15,
		TEMP1 = 15,
		CREDITS = 16,
		HERO_SKINS = 17,
		TB = 18,
		SLUSH = 19,
		LOE = 20,
		OG = 21,
		OG_RESERVE = 22,
		KARA = 23,
		KARA_RESERVE = 24,
		GANGS = 25,
		GANGS_RESERVE = 26,
		UNGORO = 27,
		ICECROWN = 1001,
		LOOTAPALOOZA = 1004,
		GILNEAS = 1125,
		BOOMSDAY = 1127,
		TAVERNS_OF_TIME = 1143,
	}

	public enum CardTextBuilderType
	{
		DEFAULT = 0,
		JADE_GOLEM = 1,
		JADE_GOLEM_TRIGGER = 2,
		KAZAKUS_POTION = 3,
		MODULAR_ENTITY = 3,
		KAZAKUS_POTION_EFFECT = 4,
		PRIMORDIAL_WAND = 5,
		DEPRECATED_5 = 5,
		ALTERNATE_CARD_TEXT = 6,
		DEPRECATED_6 = 6,
		SCRIPT_DATA_NUM_1 = 7,
		PLACE_HOLDER_7 = 7,
		DEPRECATED_8 = 8,
		PLACE_HOLDER_8 = 8,
		DECORATE = 9,
		DEPRECATED_10 = 10,
		PLACE_HOLDER_10 = 10,
		DEPRECATED_11 = 11,
		PLACE_HOLDER_11 = 11,
		DEPRECATED_12 = 12,
		PLACE_HOLDER_12 = 12,
		GAMEPLAY_STRING = 13,
		PLACE_HOLDER_13 = 13,
		ZOMBEAST = 14,
		ZOMBEAST_ENCHANTMENT = 15,
		HIDDEN_CHOICE = 16,
		INVESTIGATE = 17,
		PLACE_HOLDER_17 = 17,
		REFERENCE_CREATOR_ENTITY = 18,
		REFERENCE_SCRIPT_DATA_NUM_1_ENTITY = 19,
	}

	public enum CardType
	{
		INVALID = 0,
		GAME = 1,
		PLAYER = 2,
		HERO = 3,
		MINION = 4,
		ABILITY = 5,
		SPELL = 5,
		ENCHANTMENT = 6,
		WEAPON = 7,
		ITEM = 8,
		TOKEN = 9,
		HERO_POWER = 10,
	}

	public enum ChoiceType
	{
		INVALID = 0,
		MULLIGAN = 1,
		GENERAL = 2,
	}

	public enum DeckType
	{
		CLIENT_ONLY_DECK = -1,
		UNKNOWN_DECK_TYPE = 0,
		NORMAL_DECK = 1,
		AI_DECK = 2,
		DRAFT_DECK = 4,
		PRECON_DECK = 5,
		TAVERN_BRAWL_DECK = 6,
		FSG_BRAWL_DECK = 7,
		FRIENDLY_TOURNAMENT_DECK = 8,
		HIDDEN_DECK = 1000,
	}

	public enum DraftSlotType
	{
		DRAFT_SLOT_NONE = 0,
		DRAFT_SLOT_CARD = 1,
		DRAFT_SLOT_HERO = 2,
		DRAFT_SLOT_HERO_POWER = 3,
	}

	public enum DungeonRewardOption
	{
		INVALID = 0,
		LOOT = 1,
		TREASURE = 2,
	}

	public enum EnchantmentVisual
	{
		INVALID = 0,
		POSITIVE = 1,
		NEGATIVE = 2,
		NEUTRAL = 3,
	}

	public enum Faction
	{
		INVALID = 0,
		HORDE = 1,
		ALLIANCE = 2,
		NEUTRAL = 3,
	}

	public enum FormatType
	{
		FT_UNKNOWN = 0,
		FT_WILD = 1,
		FT_STANDARD = 2,
	}

	public enum GameTag
	{
		IGNORE_DAMAGE = 1,
		TAG_SCRIPT_DATA_NUM_1 = 2,
		TAG_SCRIPT_DATA_NUM_2 = 3,
		TAG_SCRIPT_DATA_ENT_1 = 4,
		TAG_SCRIPT_DATA_ENT_2 = 5,
		MISSION_EVENT = 6,
		TIMEOUT = 7,
		TURN_START = 8,
		TURN_TIMER_SLUSH = 9,
		PREMIUM = 12,
		GOLD_REWARD_STATE = 13,
		PLAYSTATE = 17,
		LAST_AFFECTED_BY = 18,
		STEP = 19,
		TURN = 20,
		FATIGUE = 22,
		CURRENT_PLAYER = 23,
		FIRST_PLAYER = 24,
		RESOURCES_USED = 25,
		RESOURCES = 26,
		HERO_ENTITY = 27,
		MAXHANDSIZE = 28,
		STARTHANDSIZE = 29,
		PLAYER_ID = 30,
		TEAM_ID = 31,
		TRIGGER_VISUAL = 32,
		RECENTLY_ARRIVED = 33,
		PROTECTED = 34,
		PROTECTING = 35,
		DEFENDING = 36,
		PROPOSED_DEFENDER = 37,
		ATTACKING = 38,
		PROPOSED_ATTACKER = 39,
		ATTACHED = 40,
		EXHAUSTED = 43,
		DAMAGE = 44,
		HEALTH = 45,
		ATK = 47,
		COST = 48,
		ZONE = 49,
		CONTROLLER = 50,
		OWNER = 51,
		DEFINITION = 52,
		ENTITY_ID = 53,
		HISTORY_PROXY = 54,
		COPY_DEATHRATTLE = 55,
		COPY_DEATHRATTLE_INDEX = 56,
		ELITE = 114,
		MAXRESOURCES = 176,
		CARD_SET = 183,
		CARDTEXT = 184,
		CARDTEXT_INHAND = 184,
		CARDNAME = 185,
		CARD_ID = 186,
		DURABILITY = 187,
		SILENCED = 188,
		WINDFURY = 189,
		TAUNT = 190,
		STEALTH = 191,
		SPELLPOWER = 192,
		DIVINE_SHIELD = 194,
		CHARGE = 197,
		NEXT_STEP = 198,
		CLASS = 199,
		CARDRACE = 200,
		FACTION = 201,
		CARDTYPE = 202,
		RARITY = 203,
		STATE = 204,
		SUMMONED = 205,
		FREEZE = 208,
		ENRAGED = 212,
		OVERLOAD = 215,
		RECALL = 215,
		LOYALTY = 216,
		DEATHRATTLE = 217,
		DEATH_RATTLE = 217,
		BATTLECRY = 218,
		SECRET = 219,
		COMBO = 220,
		CANT_HEAL = 221,
		CANT_DAMAGE = 222,
		CANT_SET_ASIDE = 223,
		CANT_REMOVE_FROM_GAME = 224,
		CANT_READY = 225,
		CANT_EXHAUST = 226,
		CANT_ATTACK = 227,
		CANT_TARGET = 228,
		CANT_DESTROY = 229,
		CANT_DISCARD = 230,
		CANT_PLAY = 231,
		CANT_DRAW = 232,
		INCOMING_HEALING_MULTIPLIER = 233,
		INCOMING_HEALING_ADJUSTMENT = 234,
		INCOMING_HEALING_CAP = 235,
		INCOMING_DAMAGE_MULTIPLIER = 236,
		INCOMING_DAMAGE_ADJUSTMENT = 237,
		INCOMING_DAMAGE_CAP = 238,
		CANT_BE_HEALED = 239,
		IMMUNE = 240,
		CANT_BE_DAMAGED = 240,
		CANT_BE_SET_ASIDE = 241,
		CANT_BE_REMOVED_FROM_GAME = 242,
		CANT_BE_READIED = 243,
		CANT_BE_EXHAUSTED = 244,
		CANT_BE_ATTACKED = 245,
		CANT_BE_TARGETED = 246,
		CANT_BE_DESTROYED = 247,
		AttackVisualType = 251,
		CardTextInPlay = 252,
		CANT_BE_SUMMONING_SICK = 253,
		FROZEN = 260,
		JUST_PLAYED = 261,
		LINKEDCARD = 262,
		LINKED_ENTITY = 262,
		ZONE_POSITION = 263,
		CANT_BE_FROZEN = 264,
		COMBO_ACTIVE = 266,
		CARD_TARGET = 267,
		DevState = 268,
		NUM_CARDS_PLAYED_THIS_TURN = 269,
		CANT_BE_TARGETED_BY_OPPONENTS = 270,
		NUM_TURNS_IN_PLAY = 271,
		NUM_TURNS_LEFT = 272,
		OUTGOING_DAMAGE_CAP = 273,
		OUTGOING_DAMAGE_ADJUSTMENT = 274,
		OUTGOING_DAMAGE_MULTIPLIER = 275,
		OUTGOING_HEALING_CAP = 276,
		OUTGOING_HEALING_ADJUSTMENT = 277,
		OUTGOING_HEALING_MULTIPLIER = 278,
		INCOMING_ABILITY_DAMAGE_ADJUSTMENT = 279,
		INCOMING_COMBAT_DAMAGE_ADJUSTMENT = 280,
		OUTGOING_ABILITY_DAMAGE_ADJUSTMENT = 281,
		OUTGOING_COMBAT_DAMAGE_ADJUSTMENT = 282,
		OUTGOING_ABILITY_DAMAGE_MULTIPLIER = 283,
		OUTGOING_ABILITY_DAMAGE_CAP = 284,
		INCOMING_ABILITY_DAMAGE_MULTIPLIER = 285,
		INCOMING_ABILITY_DAMAGE_CAP = 286,
		OUTGOING_COMBAT_DAMAGE_MULTIPLIER = 287,
		OUTGOING_COMBAT_DAMAGE_CAP = 288,
		INCOMING_COMBAT_DAMAGE_MULTIPLIER = 289,
		INCOMING_COMBAT_DAMAGE_CAP = 290,
		CURRENT_SPELLPOWER = 291,
		ARMOR = 292,
		MORPH = 293,
		IS_MORPHED = 294,
		TEMP_RESOURCES = 295,
		OVERLOAD_OWED = 296,
		RECALL_OWED = 296,
		NUM_ATTACKS_THIS_TURN = 297,
		NEXT_ALLY_BUFF = 302,
		MAGNET = 303,
		FIRST_CARD_PLAYED_THIS_TURN = 304,
		MULLIGAN_STATE = 305,
		TAUNT_READY = 306,
		STEALTH_READY = 307,
		CHARGE_READY = 308,
		CANT_BE_TARGETED_BY_ABILITIES = 311,
		CANT_BE_TARGETED_BY_SPELLS = 311,
		SHOULDEXITCOMBAT = 312,
		CREATOR = 313,
		CANT_BE_DISPELLED = 314,
		DIVINE_SHIELD_READY = 314,
		CANT_BE_SILENCED = 314,
		PARENT_CARD = 316,
		NUM_MINIONS_PLAYED_THIS_TURN = 317,
		PREDAMAGE = 318,
		COLLECTIBLE = 321,
		TARGETING_ARROW_TEXT = 325,
		DATABASE_ID = 327,
		ENCHANTMENT_BIRTH_VISUAL = 330,
		ENCHANTMENT_IDLE_VISUAL = 331,
		CANT_BE_TARGETED_BY_HERO_POWERS = 332,
		WEAPON = 334,
		InvisibleDeathrattle = 335,
		HEALTH_MINIMUM = 337,
		TAG_ONE_TURN_EFFECT = 338,
		SILENCE = 339,
		COUNTER = 340,
		ARTISTNAME = 342,
		LocalizationNotes = 344,
		ZONES_REVEALED = 348,
		HAND_REVEALED = 348,
		ImmuneToSpellpower = 349,
		ADJACENT_BUFF = 350,
		FLAVORTEXT = 351,
		FORCED_PLAY = 352,
		LOW_HEALTH_THRESHOLD = 353,
		IGNORE_DAMAGE_OFF = 354,
		GrantCharge = 355,
		SPELLPOWER_DOUBLE = 356,
		SPELL_HEALING_DOUBLE = 357,
		HEALING_DOUBLE = 357,
		NUM_OPTIONS_PLAYED_THIS_TURN = 358,
		NUM_OPTIONS = 359,
		TO_BE_DESTROYED = 360,
		HealTarget = 361,
		AURA = 362,
		POISONOUS = 363,
		HOW_TO_EARN = 364,
		HOW_TO_EARN_GOLDEN = 365,
		TAG_HERO_POWER_DOUBLE = 366,
		HERO_POWER_DOUBLE = 366,
		AI_MUST_PLAY = 367,
		TAG_AI_MUST_PLAY = 367,
		NUM_MINIONS_PLAYER_KILLED_THIS_TURN = 368,
		NUM_MINIONS_KILLED_THIS_TURN = 369,
		AFFECTED_BY_SPELL_POWER = 370,
		EXTRA_DEATHRATTLES = 371,
		START_WITH_1_HEALTH = 372,
		IMMUNE_WHILE_ATTACKING = 373,
		MULTIPLY_HERO_DAMAGE = 374,
		MULTIPLY_BUFF_VALUE = 375,
		CUSTOM_KEYWORD_EFFECT = 376,
		TOPDECK = 377,
		CANT_BE_TARGETED_BY_BATTLECRIES = 379,
		HERO_POWER = 380,
		OVERKILL = 380,
		SHOWN_HERO_POWER = 380,
		DEATHRATTLE_SENDS_BACK_TO_DECK = 382,
		DEATHRATTLE_RETURN_ZONE = 382,
		STEADY_SHOT_CAN_TARGET = 383,
		DISPLAYED_CREATOR = 385,
		POWERED_UP = 386,
		SPARE_PART = 388,
		FORGETFUL = 389,
		CAN_SUMMON_MAXPLUSONE_MINION = 390,
		OBFUSCATED = 391,
		BURNING = 392,
		OVERLOAD_LOCKED = 393,
		NUM_TIMES_HERO_POWER_USED_THIS_GAME = 394,
		CURRENT_HEROPOWER_DAMAGE_BONUS = 395,
		HEROPOWER_DAMAGE = 396,
		LAST_CARD_PLAYED = 397,
		NUM_FRIENDLY_MINIONS_THAT_DIED_THIS_TURN = 398,
		NUM_CARDS_DRAWN_THIS_TURN = 399,
		AI_ONE_SHOT_KILL = 400,
		EVIL_GLOW = 401,
		HIDE_STATS = 402,
		INSPIRE = 403,
		RECEIVES_DOUBLE_SPELLDAMAGE_BONUS = 404,
		HEROPOWER_ADDITIONAL_ACTIVATIONS = 405,
		HEROPOWER_ACTIVATIONS_THIS_TURN = 406,
		REVEALED = 410,
		NUM_FRIENDLY_MINIONS_THAT_DIED_THIS_GAME = 412,
		CANNOT_ATTACK_HEROES = 413,
		LOCK_AND_LOAD = 414,
		DISCOVER = 415,
		SHADOWFORM = 416,
		NUM_FRIENDLY_MINIONS_THAT_ATTACKED_THIS_TURN = 417,
		NUM_RESOURCES_SPENT_THIS_GAME = 418,
		CHOOSE_BOTH = 419,
		ELECTRIC_CHARGE_LEVEL = 420,
		HEAVILY_ARMORED = 421,
		DONT_SHOW_IMMUNE = 422,
		RITUAL = 424,
		PREHEALING = 425,
		APPEAR_FUNCTIONALLY_DEAD = 426,
		OVERLOAD_THIS_GAME = 427,
		SPELLS_COST_HEALTH = 431,
		HISTORY_PROXY_NO_BIG_CARD = 432,
		PROXY_CTHUN = 434,
		TRANSFORMED_FROM_CARD = 435,
		CTHUN = 436,
		CAST_RANDOM_SPELLS = 437,
		SHIFTING = 438,
		JADE_GOLEM = 441,
		EMBRACE_THE_SHADOW = 442,
		CHOOSE_ONE = 443,
		EXTRA_ATTACKS_THIS_TURN = 444,
		SEEN_CTHUN = 445,
		MINION_TYPE_REFERENCE = 447,
		UNTOUCHABLE = 448,
		RED_MANA_CRYSTALS = 449,
		SCORE_LABELID_1 = 450,
		SCORE_VALUE_1 = 451,
		SCORE_LABELID_2 = 452,
		SCORE_VALUE_2 = 453,
		SCORE_LABELID_3 = 454,
		SCORE_VALUE_3 = 455,
		CANT_BE_FATIGUED = 456,
		AUTOATTACK = 457,
		ARMS_DEALING = 458,
		PENDING_EVOLUTIONS = 461,
		QUEST = 462,
		TAG_LAST_KNOWN_COST_IN_HAND = 466,
		DEFINING_ENCHANTMENT = 469,
		FINISH_ATTACK_SPELL_ON_DAMAGE = 470,
		KAZAKUS_POTION_POWER_1 = 471,
		MODULAR_ENTITY_PART_1 = 471,
		KAZAKUS_POTION_POWER_2 = 472,
		MODULAR_ENTITY_PART_2 = 472,
		MODIFY_DEFINITION_ATTACK = 473,
		MODIFY_DEFINITION_HEALTH = 474,
		MODIFY_DEFINITION_COST = 475,
		MULTIPLE_CLASSES = 476,
		ALL_TARGETS_RANDOM = 477,
		MULTI_CLASS_GROUP = 480,
		CARD_COSTS_HEALTH = 481,
		GRIMY_GOONS = 482,
		JADE_LOTUS = 483,
		KABAL = 484,
		ADDITIONAL_PLAY_REQS_1 = 515,
		ADDITIONAL_PLAY_REQS_2 = 516,
		ELEMENTAL_POWERED_UP = 532,
		QUEST_PROGRESS = 534,
		QUEST_PROGRESS_TOTAL = 535,
		QUEST_CONTRIBUTOR = 541,
		ADAPT = 546,
		IS_CURRENT_TURN_AN_EXTRA_TURN = 547,
		EXTRA_TURNS_TAKEN_THIS_GAME = 548,
		SHIFTING_MINION = 549,
		SHIFTING_WEAPON = 550,
		DEATH_KNIGHT = 554,
		BOSS = 556,
		TREASURE = 557,
		TREASURE_DEFINTIONAL_ATTACK = 558,
		TREASURE_DEFINTIONAL_COST = 559,
		TREASURE_DEFINTIONAL_HEALTH = 560,
		ACTS_LIKE_A_SPELL = 561,
		STAMPEDE = 564,
		EMPOWERED_TREASURE = 646,
		ONE_SIDED_GHOSTLY = 648,
		CURRENT_NEGATIVE_SPELLPOWER = 651,
		IS_VAMPIRE = 680,
		CORRUPTED = 681,
		HIDE_HEALTH = 682,
		HIDE_ATTACK = 683,
		HIDE_COST = 684,
		LIFESTEAL = 685,
		OVERRIDE_EMOTE_0 = 740,
		OVERRIDE_EMOTE_1 = 741,
		OVERRIDE_EMOTE_2 = 742,
		OVERRIDE_EMOTE_3 = 743,
		OVERRIDE_EMOTE_4 = 744,
		OVERRIDE_EMOTE_5 = 745,
		SCORE_FOOTERID = 751,
		RECRUIT = 763,
		LOOT_CARD_1 = 764,
		LOOT_CARD_2 = 765,
		LOOT_CARD_3 = 766,
		HERO_POWER_DISABLED = 777,
		VALEERASHADOW = 779,
		OVERRIDECARDNAME = 781,
		OVERRIDECARDTEXTBUILDER = 782,
		DUNGEON_PASSIVE_BUFF = 783,
		GHOSTLY = 785,
		DISGUISED_TWIN = 788,
		SECRET_DEATHRATTLE = 789,
		RUSH = 791,
		REVEAL_CHOICES = 792,
		HERO_DECK_ID = 793,
		HIDDEN_CHOICE = 813,
		ZOMBEAST = 823,
		HERO_EMOTE_SILENCED = 832,
		MINION_IN_HAND_BUFF = 845,
		ECHO = 846,
		MODULAR = 849,
		IGNORE_HIDE_STATS_FOR_BIG_CARD = 857,
		REAL_TIME_TRANSFORM = 859,
		WAIT_FOR_PLAYER_RECONNECT_PERIOD = 860,
		PHASED_RESTART = 888,
		DISCARD_CARDS = 890,
		HEALTH_DISPLAY = 917,
		ENABLE_HEALTH_DISPLAY = 920,
		VOODOO_LINK = 921,
		ATTACKABLE_BY_RUSH = 930,
		SHIFTING_SPELL = 936,
		USE_ALTERNATE_CARD_TEXT = 955,
		COLLECTIONMANAGER_FILTER_MANA_EVEN = 956,
		COLLECTIONMANAGER_FILTER_MANA_ODD = 957,
		SUPPRESS_DEATH_SOUND = 959,
		ECHOING_OOZE_SPELL = 963,
		ZOMBEAST_DEBUG_CURRENT_BEAST_DATABASE_ID = 964,
		ZOMBEAST_DEBUG_CURRENT_ITERATION = 965,
		ZOMBEAST_DEBUG_MAX_ITERATIONS = 966,
		START_OF_GAME = 968,
		ENCHANTMENT_INVISIBLE = 976,
		PUZZLE = 979,
		PUZZLE_PROGRESS = 980,
		PUZZLE_PROGRESS_TOTAL = 981,
		PUZZLE_TYPE = 982,
		PUZZLE_COMPLETED = 984,
		CONCEDE_BUTTON_ALTERNATIVE_TEXT = 985,
		HIDE_RESTART_BUTTON = 990,
		WILD = 991,
		HALL_OF_FAME = 992,
		DECK_RULE_MOD_DECK_SIZE = 997,
		FAST_BATTLECRY = 998,
		END_TURN_BUTTON_ALTERNATIVE_APPEARANCE = 1000,
		TREAT_AS_PLAYED_HERO_CARD = 1016,
		PUZZLE_NAME = 1026,
		TURN_INDICATOR_ALTERNATIVE_APPEARANCE = 1027,
		PREVIOUS_PUZZLE_COMPLETED = 1042,
		GLORIOUSGLOOP = 1044,
		HEALTH_DISPLAY_COLOR = 1046,
		HEALTH_DISPLAY_NEGATIVE = 1047,
		WHIZBANG_DECK_ID = 1048,
		HIDE_OUT_OF_CARDS_WARNING = 1050,
		GEARS = 1052,
		LUNAHIGHLIGHTHINT = 1054,
		SUPPRESS_JOBS_DONE_VO = 1055,
		ALL_HEALING_DOUBLE = 1058,
		BLOCK_ALL_INPUT = 1071,
		PUZZLE_MODE = 1073,
	}

	public enum GameType
	{
		GT_UNKNOWN = 0,
		GT_VS_AI = 1,
		GT_VS_FRIEND = 2,
		GT_TUTORIAL = 4,
		GT_ARENA = 5,
		GT_TEST_AI_VS_AI = 6,
		GT_TEST = 6,
		GT_RANKED = 7,
		GT_CASUAL = 8,
		GT_TAVERNBRAWL = 16,
		GT_TB_1P_VS_AI = 17,
		GT_TB_2P_COOP = 18,
		GT_FSG_BRAWL_VS_FRIEND = 19,
		GT_FSG_BRAWL = 20,
		GT_FSG_BRAWL_1P_VS_AI = 21,
		GT_FSG_BRAWL_2P_COOP = 22,
		GT_TOURNAMENT = 23,
	}

	public enum GoldRewardState
	{
		INVALID = 0,
		ELIGIBLE = 1,
		WRONG_GAME_TYPE = 2,
		ALREADY_CAPPED = 3,
		BAD_RATING = 4,
		SHORT_GAME = 5,
		SHORT_GAME_BY_TIME = 5,
		OVER_CAIS = 6,
	}

	public enum Locale
	{
		UNKNOWN = -1,
		enUS = 0,
		enGB = 1,
		frFR = 2,
		deDE = 3,
		koKR = 4,
		esES = 5,
		esMX = 6,
		ruRU = 7,
		zhTW = 8,
		zhCN = 9,
		itIT = 10,
		ptBR = 11,
		plPL = 12,
		ptPT = 13,
		jaJP = 14,
		thTH = 15,
	}

	public enum MetaDataType
	{
		META_TARGET = 0,
		TARGET = 0,
		META_DAMAGE = 1,
		DAMAGE = 1,
		META_HEALING = 2,
		HEALING = 2,
		JOUST = 3,
		CLIENT_HISTORY = 4,
		SHOW_BIG_CARD = 5,
		EFFECT_TIMING = 6,
		HISTORY_TARGET = 7,
		OVERRIDE_HISTORY = 8,
		HISTORY_TARGET_DONT_DUPLICATE_UNTIL_END = 9,
		BEGIN_ARTIFICIAL_HISTORY_TILE = 10,
		BEGIN_ARTIFICIAL_HISTORY_TRIGGER_TILE = 11,
		END_ARTIFICIAL_HISTORY_TILE = 12,
		START_DRAW = 13,
		BURNED_CARD = 14,
		EFFECT_SELECTION = 15,
		BEGIN_LISTENING_FOR_TURN_EVENTS = 16,
	}

	public enum Mulligan
	{
		INVALID = 0,
		INPUT = 1,
		DEALING = 2,
		WAITING = 3,
		DONE = 4,
	}

	public enum MultiClassGroup
	{
		INVALID = 0,
		GRIMY_GOONS = 1,
		JADE_LOTUS = 2,
		KABAL = 3,
	}

	public enum OptionType
	{
		PASS = 1,
		END_TURN = 2,
		POWER = 3,
	}

	public enum PlayReq
	{
		INVALID = -1,
		NONE = -1,
		REQ_MINION_TARGET = 1,
		REQ_FRIENDLY_TARGET = 2,
		REQ_ENEMY_TARGET = 3,
		REQ_DAMAGED_TARGET = 4,
		REQ_ENCHANTED_TARGET = 5,
		REQ_MAX_SECRETS = 5,
		REQ_FROZEN_TARGET = 6,
		REQ_CHARGE_TARGET = 7,
		REQ_TARGET_MAX_ATTACK = 8,
		REQ_NONSELF_TARGET = 9,
		REQ_TARGET_WITH_RACE = 10,
		REQ_TARGET_TO_PLAY = 11,
		REQ_NUM_MINION_SLOTS = 12,
		REQ_WEAPON_EQUIPPED = 13,
		REQ_ENOUGH_MANA = 14,
		REQ_YOUR_TURN = 15,
		REQ_NONSTEALTH_ENEMY_TARGET = 16,
		REQ_HERO_TARGET = 17,
		REQ_SECRET_ZONE_CAP = 18,
		REQ_SECRET_CAP = 18,
		REQ_MINION_CAP_IF_TARGET_AVAILABLE = 19,
		REQ_MINION_CAP = 20,
		REQ_TARGET_ATTACKED_THIS_TURN = 21,
		REQ_TARGET_IF_AVAILABLE = 22,
		REQ_MINIMUM_ENEMY_MINIONS = 23,
		REQ_TARGET_FOR_COMBO = 24,
		REQ_NOT_EXHAUSTED_ACTIVATE = 25,
		REQ_UNIQUE_SECRET_OR_QUEST = 26,
		REQ_UNIQUE_SECRET = 26,
		REQ_TARGET_TAUNTER = 27,
		REQ_CAN_BE_ATTACKED = 28,
		REQ_ACTION_PWR_IS_MASTER_PWR = 29,
		REQ_TARGET_MAGNET = 30,
		REQ_ATTACK_GREATER_THAN_0 = 31,
		REQ_ATTACKER_NOT_FROZEN = 32,
		REQ_HERO_OR_MINION_TARGET = 33,
		REQ_CAN_BE_TARGETED_BY_SPELLS = 34,
		REQ_SUBCARD_IS_PLAYABLE = 35,
		REQ_TARGET_FOR_NO_COMBO = 36,
		REQ_NOT_MINION_JUST_PLAYED = 37,
		REQ_NOT_EXHAUSTED_HERO_POWER = 38,
		REQ_CAN_BE_TARGETED_BY_OPPONENTS = 39,
		REQ_ATTACKER_CAN_ATTACK = 40,
		REQ_TARGET_MIN_ATTACK = 41,
		REQ_CAN_BE_TARGETED_BY_HERO_POWERS = 42,
		REQ_ENEMY_TARGET_NOT_IMMUNE = 43,
		REQ_ENTIRE_ENTOURAGE_NOT_IN_PLAY = 44,
		REQ_MINIMUM_TOTAL_MINIONS = 45,
		REQ_MUST_TARGET_TAUNTER = 46,
		REQ_UNDAMAGED_TARGET = 47,
		REQ_CAN_BE_TARGETED_BY_BATTLECRIES = 48,
		REQ_STEADY_SHOT = 49,
		REQ_MINION_OR_ENEMY_HERO = 50,
		REQ_TARGET_IF_AVAILABLE_AND_DRAGON_IN_HAND = 51,
		REQ_LEGENDARY_TARGET = 52,
		REQ_FRIENDLY_MINION_DIED_THIS_TURN = 53,
		REQ_FRIENDLY_MINION_DIED_THIS_GAME = 54,
		REQ_ENEMY_WEAPON_EQUIPPED = 55,
		REQ_TARGET_IF_AVAILABLE_AND_MINIMUM_FRIENDLY_MINIONS = 56,
		REQ_TARGET_WITH_BATTLECRY = 57,
		REQ_TARGET_WITH_DEATHRATTLE = 58,
		REQ_TARGET_IF_AVAILABLE_AND_MINIMUM_FRIENDLY_SECRETS = 59,
		REQ_SECRET_CAP_FOR_NON_SECRET = 60,
		REQ_SECRET_ZONE_CAP_FOR_NON_SECRET = 60,
		REQ_TARGET_EXACT_COST = 61,
		REQ_STEALTHED_TARGET = 62,
		REQ_MINION_SLOT_OR_MANA_CRYSTAL_SLOT = 63,
		REQ_MAX_QUESTS = 64,
		REQ_TARGET_IF_AVAILABE_AND_ELEMENTAL_PLAYED_LAST_TURN = 65,
		REQ_TARGET_NOT_VAMPIRE = 66,
		REQ_TARGET_NOT_DAMAGEABLE_ONLY_BY_WEAPONS = 67,
		REQ_NOT_DISABLED_HERO_POWER = 68,
		REQ_MUST_PLAY_OTHER_CARD_FIRST = 69,
		REQ_HAND_NOT_FULL = 70,
		REQ_TARGET_IF_AVAILABLE_AND_NO_3_COST_CARD_IN_DECK = 71,
		REQ_CAN_BE_TARGETED_BY_COMBOS = 72,
		REQ_CANNOT_PLAY_THIS = 73,
		REQ_FRIENDLY_MINIONS_OF_RACE_DIED_THIS_GAME = 74,
		REQ_DRAG_TO_PLAY = 75,
		REQ_OPPONENT_PLAYED_CARDS_THIS_GAME = 77,
	}

	public enum PlayState
	{
		INVALID = 0,
		PLAYING = 1,
		WINNING = 2,
		LOSING = 3,
		WON = 4,
		LOST = 5,
		TIED = 6,
		DISCONNECTED = 7,
		QUIT = 8,
		CONCEDED = 8,
	}

	public enum PowerType
	{
		FULL_ENTITY = 1,
		SHOW_ENTITY = 2,
		HIDE_ENTITY = 3,
		TAG_CHANGE = 4,
		BLOCK_START = 5,
		ACTION_START = 5,
		BLOCK_END = 6,
		ACTION_END = 6,
		CREATE_GAME = 7,
		META_DATA = 8,
		CHANGE_ENTITY = 9,
		RESET_GAME = 10,
	}

	public enum PuzzleType
	{
		INVALID = 0,
		MIRROR = 1,
		LETHAL = 2,
		SURVIVAL = 3,
		CLEAR = 4,
	}

	public enum Race
	{
		INVALID = 0,
		BLOODELF = 1,
		DRAENEI = 2,
		DWARF = 3,
		GNOME = 4,
		GOBLIN = 5,
		HUMAN = 6,
		NIGHTELF = 7,
		ORC = 8,
		TAUREN = 9,
		TROLL = 10,
		UNDEAD = 11,
		WORGEN = 12,
		GOBLIN2 = 13,
		MURLOC = 14,
		DEMON = 15,
		SCOURGE = 16,
		MECHANICAL = 17,
		ELEMENTAL = 18,
		OGRE = 19,
		BEAST = 20,
		PET = 20,
		TOTEM = 21,
		NERUBIAN = 22,
		PIRATE = 23,
		DRAGON = 24,
		BLANK = 25,
		ALL = 26,
		EGG = 38,
	}

	public enum Rarity
	{
		INVALID = 0,
		COMMON = 1,
		FREE = 2,
		RARE = 3,
		EPIC = 4,
		LEGENDARY = 5,
		UNKNOWN_6 = 6,
	}

	public enum RewardType
	{
		ARCANE_DUST = 0,
		BOOSTER_PACK = 1,
		CARD = 2,
		CARD_BACK = 3,
		CRAFTABLE_CARD = 4,
		FORGE_TICKET = 5,
		GOLD = 6,
		MOUNT = 7,
		CLASS_CHALLENGE = 8,
		EVENT = 9,
		RANDOM_CARD = 10,
		BONUS_CHALLENGE = 11,
	}

	public enum State
	{
		INVALID = 0,
		LOADING = 1,
		RUNNING = 2,
		COMPLETE = 3,
	}

	public enum Step
	{
		INVALID = 0,
		BEGIN_FIRST = 1,
		BEGIN_SHUFFLE = 2,
		BEGIN_DRAW = 3,
		BEGIN_MULLIGAN = 4,
		MAIN_BEGIN = 5,
		MAIN_READY = 6,
		MAIN_RESOURCE = 7,
		MAIN_DRAW = 8,
		MAIN_START = 9,
		MAIN_ACTION = 10,
		MAIN_COMBAT = 11,
		MAIN_END = 12,
		MAIN_NEXT = 13,
		FINAL_WRAPUP = 14,
		FINAL_GAMEOVER = 15,
		MAIN_CLEANUP = 16,
		MAIN_START_TRIGGERS = 17,
	}

	public enum SwissDeckType
	{
		SWISS_DECK_NONE = 0,
		SWISS_DECK_CONQUEST = 1,
		SWISS_DECK_LAST_STAND = 2,
	}

	public enum TavernBrawlMode
	{
		TB_MODE_NORMAL = 0,
		TB_MODE_HEROIC = 1,
	}

	public enum TournamentState
	{
		STATE_OPEN = 1,
		STATE_LOCKED = 2,
		STATE_STARTED = 3,
		STATE_CLOSED = 4,
	}

	public enum TournamentType
	{
		TYPE_UNKNOWN = 0,
		TYPE_SWISS = 1,
	}

	public enum Type
	{
		LOCSTRING = -2,
		UNKNOWN = 0,
		BOOL = 1,
		NUMBER = 2,
		COUNTER = 3,
		ENTITY = 4,
		PLAYER = 5,
		TEAM = 6,
		ENTITY_DEFINITION = 7,
		STRING = 8,
	}

	public enum ZodiacYear
	{
		INVALID = -1,
		PRE_STANDARD = 0,
		KRAKEN = 1,
		MAMMOTH = 2,
		RAVEN = 3,
	}

	public enum Zone
	{
		INVALID = 0,
		PLAY = 1,
		DECK = 2,
		HAND = 3,
		GRAVEYARD = 4,
		REMOVEDFROMGAME = 5,
		SETASIDE = 6,
		SECRET = 7,
	}
}