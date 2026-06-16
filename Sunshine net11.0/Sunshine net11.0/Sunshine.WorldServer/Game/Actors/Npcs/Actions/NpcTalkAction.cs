using Sunshine.WorldServer.Game.Actors.Npcs.Replies;
using Sunshine.WorldServer.Game.Characters;
using Sunshine.WorldServer.Handlers.Context.Roleplay;
using Sunshine.WorldServer.Handlers.Dialogs;
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
        public short CurrentMessageId => _currentMessageId;

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

            short currentMessage = ResolveCurrentMessage(reply);
            if (currentMessage <= 0)
            {
                NpcReplyActionDiagnostics.LogReplyRaw(_character.Client, _npc, _currentMessageId, reply, 0,
                    FormatKnownReplies(_currentMessageId), _npc.FormatDbRepliesForLog(), "None", false, "UnknownMessage");
                DialogHandler.SendLeaveDialogMessage(_character.Client);
                return;
            }

            NpcReplyResolution resolution;
            if (!_npc.TryResolveReply(currentMessage, reply, out resolution))
            {
                NpcReplyActionDiagnostics.LogReplyRaw(_character.Client, _npc, currentMessage, reply, 0,
                    FormatKnownReplies(currentMessage), _npc.FormatDbRepliesForLog(), "None", false, "Unresolved");
                DialogHandler.SendLeaveDialogMessage(_character.Client);
                return;
            }

            NpcReplyActionDiagnostics.LogReplyRaw(_character.Client, _npc, currentMessage, reply, resolution.ResolvedMessageId,
                FormatKnownReplies(currentMessage), _npc.FormatDbRepliesForLog(), resolution.Source, resolution.Ambiguous, "Resolved");

            if (resolution.Ambiguous)
            {
                NpcReplyActionDiagnostics.LogReplySelection(_character.Client, _npc, currentMessage, reply, resolution.Type, resolution.Args, "AmbiguousClose");
                _character.Dialog = null;
                DialogHandler.SendLeaveDialogMessage(_character.Client);
                return;
            }

            int type = resolution.Type;
            var args = resolution.Args ?? string.Empty;

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
                NpcReplyActionDiagnostics.LogReplySelection(_character.Client, _npc, currentMessage, reply, type, args, "Failed");
                _character.Dialog = null;
                DialogHandler.SendLeaveDialogMessage(_character.Client);
                return;
            }
            else if (IsTerminalReplyType(type))
            {
                NpcReplyActionDiagnostics.LogReplySelection(_character.Client, _npc, currentMessage, reply, type, args, "Success");
                _character.Dialog = null;
                DialogHandler.SendLeaveDialogMessage(_character.Client);
                return;
            }

            if (_character.Dialog == null || _character.Dialog != this)
                return;

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

        private short ResolveCurrentMessage(short reply)
        {
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

            return currentMessage;
        }

        private string FormatKnownReplies(short messageId)
        {
            var replies = _npc.GetDialogReplies(messageId);
            return replies == null ? "[]" : "[" + string.Join(",", replies) + "]";
        }

        private static bool IsTerminalReplyType(int type)
        {
            return type == 0 || type == 2 || type == 5 || type == 8;
        }
    }
}
