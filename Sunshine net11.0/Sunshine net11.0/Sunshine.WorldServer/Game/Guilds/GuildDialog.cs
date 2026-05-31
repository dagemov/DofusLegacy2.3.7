using Sunshine.MySql.Database.Managers;
using Sunshine.MySql.Database.World.Guilds;
using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Characters;
using Sunshine.WorldServer.Game.Dialogs;
using Sunshine.WorldServer.Handlers.Guilds;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Guilds
{
    public class GuildDialog : IDialog
    {
        public Character Recruter { get; set; }

        public Character Recruted { get; set; }

        public GuildDialog(Character Recruter, Character Recruted)
        {
            this.Recruter = Recruter;
            this.Recruted = Recruted;
        }

        public void SendInvitation()
        {
            GuildHandler.SendGuildInvitationStateRecruterMessage(Recruter.Client, Recruted, GuildInvitationStateEnum.GUILD_INVITATION_SENT);
            GuildHandler.SendGuildInvitationStateRecrutedMessage(Recruted.Client, GuildInvitationStateEnum.GUILD_INVITATION_SENT);
            GuildHandler.SendGuildInvitedMessage(Recruted.Client, Recruter);
            Recruted.Dialog = this;
            Recruter.Dialog = this;
        }

        public void AcceptInvitation()
        {
            GuildHandler.SendGuildInvitationStateRecruterMessage(Recruter.Client, Recruted, GuildInvitationStateEnum.GUILD_INVITATION_OK);
            GuildHandler.SendGuildInvitationStateRecrutedMessage(Recruted.Client, GuildInvitationStateEnum.GUILD_INVITATION_OK);

            if (Recruter.Guild != null)
            {
                GuildMemberRecord guildMemberRecord = new GuildMemberRecord
                {
                    Owner = Recruted.Id,
                    Account = Recruted.Account.Id,
                    Guild = Recruter.Guild.Id
                };

                GuildMember guildMember = new GuildMember(guildMemberRecord, Recruted);

                Recruted.GuildMember = guildMember;

                GuildManager.Instance.AddMember(guildMember);
            }

            Recruted.Dialog = null;
            Recruter.Dialog = null;
        }

        public void DenyInvitation()
        {
            GuildHandler.SendGuildInvitationStateRecruterMessage(Recruter.Client, Recruted, GuildInvitationStateEnum.GUILD_INVITATION_CANCELED);
            GuildHandler.SendGuildInvitationStateRecruterMessage(Recruted.Client, Recruted, GuildInvitationStateEnum.GUILD_INVITATION_CANCELED);
            Recruted.Dialog = null;
            Recruter.Dialog = null;
        }

        public void CancelInvitation()
        {
            GuildHandler.SendGuildInvitationStateRecruterMessage(Recruter.Client, Recruted, GuildInvitationStateEnum.GUILD_INVITATION_CANCELED);
            GuildHandler.SendGuildInvitationStateRecruterMessage(Recruted.Client, Recruted, GuildInvitationStateEnum.GUILD_INVITATION_CANCELED);
            Recruted.Dialog = null;
            Recruter.Dialog = null;
        }
    }
}
