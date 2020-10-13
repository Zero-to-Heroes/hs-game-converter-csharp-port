#region

using System.Text.RegularExpressions;

#endregion

namespace HearthstoneReplays.Parser
{
	public class Regexes
	{
		private const string Entity = @"(GameEntity|UNKNOWN HUMAN PLAYER|\[.+\]|\d+|.+)";

        public static readonly Regex EntityWithNameAndId = new Regex(@"\[entityName=(.+) id=(\d+) .*\]");
        public static readonly Regex PowerlogLineRegex = new Regex(@"^D ([\d:.]+) ([^(]+)\(\) - (.+)$");
		public static readonly Regex OutputlogLineRegex = new Regex(@"\[Power\] ()([^(]+)\(\) - (.+)$");
		public static readonly Regex EntityRegex = new Regex(@"\[.*\s*id=(\d+)\s*.*\]");

		//public static readonly Regex ChoicesChoiceRegex_OLD =
  //          new Regex(@"id=(\d+) PlayerId=(\d+) ChoiceType=(\w+) CountMin=(\d+) CountMax=(\d+)$");
		public static readonly Regex ChoicesChoiceRegex =
			new Regex(string.Format(@"id=(\d+) Player={0} TaskList=(\d+)? ChoiceType=(\w+) CountMin=(\d+) CountMax=(\d+)$", Entity));
		public static readonly Regex ChoicesSourceRegex = new Regex(string.Format(@"Source={0}$", Entity));
		public static readonly Regex ChoicesEntitiesRegex = new Regex(@"Entities\[(\d+)\]=(\[.+\])$");

		public static readonly Regex ActionCreategameRegex = new Regex(@"GameEntity EntityID=(\d+)");
		public static readonly Regex ActionCreategamePlayerRegex =
			new Regex(@"Player EntityID=(\d+) PlayerID=(\d+) GameAccountId=\[hi=(\d+) lo=(\d+)\]$");
		public static readonly Regex ActionStartRegex_8_4 =
			new Regex(string.Format(@"BLOCK_START (?:SubType|BlockType)=(\w+) Entity={0} EffectCardId=(.*) EffectIndex=(-1|\d+) Target={0}$", Entity));
		public static readonly Regex ActionStartRegex_Short =
			new Regex(string.Format(@"BLOCK_START (?:SubType|BlockType)=(\w+) Entity={0} EffectCardId=(.*) EffectIndex=(-1|\d+) Target={0} SubOption={0}$", Entity));
		public static readonly Regex ActionStartRegex =
			new Regex(string.Format(@"BLOCK_START (?:SubType|BlockType)=(\w+) Entity={0} EffectCardId=(.*) EffectIndex=(-1|\d+) Target={0} SubOption={0} TriggerKeyword={0}$", Entity));

        public static readonly Regex PlayerNameAssignment = new Regex(@"PlayerID=(\d), PlayerName=(.+)");

		public static readonly Regex BuildNumber = new Regex(@"BuildNumber=(\d+)");
		public static readonly Regex GameType = new Regex(@"GameType=(\w+)");
		public static readonly Regex FormatType = new Regex(@"FormatType=(\w+)");
		public static readonly Regex ScenarioID = new Regex(@"ScenarioID=(\d+)");

		public static readonly Regex ActionMetadataRegex = new Regex(string.Format(@"META_DATA - Meta=(\w+) Data={0} (?:Info|InfoCount)=(\d+)", Entity));
		public static readonly Regex ActionMetaDataInfoRegex = new Regex(string.Format(@"Info\[(\d+)\] = {0}", Entity));

		public static readonly Regex ActionChangeEntityRegex = new Regex(string.Format(@"CHANGE_ENTITY - Updating Entity={0} CardID=(\w+)$", Entity));
		public static readonly Regex ActionShowEntityRegex = new Regex(string.Format(@"SHOW_ENTITY - Updating Entity={0} CardID=(\w+)$", Entity));
		public static readonly Regex ActionHideEntityRegex = new Regex(string.Format(@"HIDE_ENTITY - Entity={0} tag=(\w+) value=(\w+)", Entity));
		public static readonly Regex ActionFullEntityUpdatingRegex = new Regex(string.Format(@"FULL_ENTITY - Updating {0} CardID=(\w+)?$", Entity));
		public static readonly Regex ActionFullEntityCreatingRegex = new Regex(@"FULL_ENTITY - Creating ID=(\d+) CardID=(\w+)?$");
		public static readonly Regex SubSpellStartRegex = new Regex(@"SUB_SPELL_START - SpellPrefabGUID=(.+):.*$");

		public static readonly Regex ActionTagChangeRegex = new Regex(string.Format(@"TAG_CHANGE Entity={0} tag=(\w+) value=(\w+)( DEF CHANGE)?", Entity));
		public static readonly Regex ActionTagRegex = new Regex(@"tag=(\w+) value=(\w+)");

		public static readonly Regex EntitiesChosenRegex = new Regex(string.Format(@"id=(\d+) Player={0} EntitiesCount=(\d+)$", Entity));
		public static readonly Regex EntitiesChosenEntitiesRegex = new Regex(string.Format(@"Entities\[(\d+)\]={0}$", Entity));
		
		public static readonly Regex OptionsEntityRegex = new Regex(@"id=(\d+)$");
		public static readonly Regex OptionsOptionRegex = new Regex(string.Format(@"option (\d+) type=(\w+) mainEntity={0}? error=(.*) errorParam=(.*)$", Entity));
		public static readonly Regex OptionsSuboptionRegex = new Regex(string.Format(@"(subOption|target) (\d+) entity={0}? error=(.*) errorParam=(.*)$", Entity));

		public static readonly Regex SendChoicesChoicetypeRegex = new Regex(@"id=(\d+) ChoiceType=(.+)$");
		public static readonly Regex SendChoicesEntitiesRegex = new Regex(@"m_chosenEntities\[(\d+)\]=(\[.+\])$");

		public static readonly Regex SendOptionRegex =
			new Regex(@"selectedOption=(\d+) selectedSubOption=(-1|\d+) selectedTarget=(\d+) selectedPosition=(\d+)");
	}
}