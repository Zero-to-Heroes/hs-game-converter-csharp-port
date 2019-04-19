using HearthstoneReplays.Parser;
using HearthstoneReplays.Parser.ReplayData.Meta;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.Entities;

namespace HearthstoneReplays.Events.Parsers
{
    public class DamageParser : ActionParser
    {
        private GameState GameState { get; set; }
        private ParserState ParserState { get; set; }

        public DamageParser(ParserState ParserState)
        {
            this.ParserState = ParserState;
            this.GameState = ParserState.GameState;
        }

        public bool AppliesOnNewNode(Node node)
        {
            return node.Type == typeof(Info)
                && node.Parent.Type == typeof(MetaData)
                && (node.Parent.Object as MetaData).Meta == (int)MetaDataType.DAMAGE;
        }

        public bool AppliesOnCloseNode(Node node)
        {
            return false;
        }

        public GameEventProvider CreateGameEventProviderFromNew(Node node)
        {
            Node parentAction = node;
            while (parentAction != null && parentAction.Type != typeof(Parser.ReplayData.GameActions.Action) && parentAction.Parent != null)
            {
                parentAction = parentAction.Parent;
            }
            if (parentAction == null || parentAction.Type != typeof(Parser.ReplayData.GameActions.Action))
            {
                return null;
            }
            var info = node.Object as Info;
            var damageTarget = GameState.CurrentEntities[info.Entity];
            var targetCardId = damageTarget.CardId;
            var targetControllerId = damageTarget.GetTag(GameTag.CONTROLLER);
            var damageSource = GetDamageSource(damageTarget, parentAction.Object as Parser.ReplayData.GameActions.Action);
            var sourceCardId = damageSource.CardId;
            var sourceControllerId = damageSource.GetTag(GameTag.CONTROLLER);
            return GameEventProvider.Create(
                info.TimeStamp,
                () => new GameEvent
                {
                    Type = "DAMAGE",
                    Value = new
                    {
                        SourceCardId = sourceCardId,
                        SourceControllerId = sourceControllerId,
                        TargetCardId = targetCardId,
                        TargetControllerId = targetControllerId,
                        Damage = (node.Parent.Object as MetaData).Data,
                        LocalPlayer = ParserState.LocalPlayer,
                        OpponentPlayer = ParserState.OpponentPlayer,
                    }
                },
                true,
                node.CreationLogLine);
        }

        public GameEventProvider CreateGameEventProviderFromClose(Node node)
        {
            return null;
        }

        private FullEntity GetDamageSource(FullEntity target, Parser.ReplayData.GameActions.Action action)
        {
            var actionSource = GameState.CurrentEntities[action.Entity];
            if (action.Type == (int)BlockType.ATTACK)
            {
                if (target.Id == action.Entity)
                {
                    return GameState.CurrentEntities[action.Target];
                }
                return GameState.CurrentEntities[action.Entity];
            }
            return actionSource;
        }
    }
}
