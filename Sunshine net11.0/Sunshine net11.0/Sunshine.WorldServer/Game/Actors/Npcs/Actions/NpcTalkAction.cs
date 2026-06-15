using Sunshine.WorldServer.Game.Actors.Npcs.Replies;
using Sunshine.WorldServer.Game.Characters;
using Sunshine.WorldServer.Handlers.Context.Roleplay;
using Sunshine.WorldServer.Handlers.Dialogs;
using Sunshine.Logs;
using System.Collections.Generic;
using System.Linq;

namespace Sunshine.WorldServer.Game.Actors.Npcs.Actions
{
    public class NpcTalkAction : NpcAction
    {
        private readonly Character _character;
        private readonly Npc _npc;
        private short _currentMessageId;

        public Npc Npc => _npc;

        public NpcTalkAction(Npc npc, Character character)
        {
            _npc = npc;
            _character = character;
        }

        public override void Execute()
        {
            _character.Dialog = this;
            _currentMessageId = _npc != null ? _npc.GetFirstDialogMessageId() : (short)0;
            ContextRoleplayHandler.SendNpcDialogCreationMessage(_character.Client, _npc);
            ContextRoleplayHandler.SendNpcDialogQuestionMessage(_character.Client, _npc);
        }

        public void ChangeMessage(short reply)
        {
            if (_npc == null || _npc.GetAllDialogs == null || _npc.GetAllDialogs.Count == 0)
            {
                DialogHandler.SendLeaveDialogMessage(_character.Client);
                return;
            }

            short currentMessage = _currentMessageId;
            if (currentMessage <= 0 || !_npc.GetDialogReplies(currentMessage).Contains(reply))
            {
                currentMessage = 0;
                foreach (var dialog in _npc.GetAllDialogs)
                {
                    if (dialog.Value.Contains(reply))
                    {
                        currentMessage = dialog.Key;
                        break;
                    }
                }
            }

            if (currentMessage <= 0)
            {
                DialogHandler.SendLeaveDialogMessage(_character.Client);
                return;
            }

            int replyIndex;
            if (_npc.TryGetReplyIndex(currentMessage, reply, out replyIndex) &&
                replyIndex >= 0 && replyIndex < _npc.GetNpcTypes.Count)
            {
                int type = _npc.GetNpcTypes[replyIndex];
                var args = _npc.GetParameters.Count > replyIndex ? _npc.GetParameters[replyIndex] : string.Empty;

                if (type == 1)
                {
                    NpcReplyActionDiagnostics.LogReplySelection(_character.Client, _npc, currentMessage, reply, type, args, "Navigate");
                }
                else if (!ReplyDispatcher.Dispatch(_character.Client, _npc, currentMessage, reply, new List<object>
                {
                    type,
                    args
                }))
                {
                    return;
                }

                if (_character.Dialog == null || _character.Dialog != this)
                    return;
            }

            short nextMessage = _npc.GetNextDialogMessageId(currentMessage);
            if (nextMessage <= 0)
            {
                _character.Dialog = null;
                DialogHandler.SendLeaveDialogMessage(_character.Client);
                return;
            }

            _currentMessageId = nextMessage;
            var visibleReplies = _npc.GetDialogReplies(nextMessage);
            var dialogParams = _npc.GetDialogParameters(_character, nextMessage) ?? new string[0];
            ContextRoleplayHandler.SendNpcDialogQuestionMessage(_character.Client, nextMessage, dialogParams, visibleReplies);
        }
    }
}
