using Sunshine.MySql.Database.Managers;
using Sunshine.MySql.Database.World.Guilds;
using Sunshine.Protocol.Enums;
using Sunshine.Protocol.Messages;
using Sunshine.Protocol.Types;
using Sunshine.WorldServer.Client;
using Sunshine.WorldServer.Game.Characters;
using Sunshine.WorldServer.Game.Guilds;
using Sunshine.WorldServer.Game.Maps.Houses;
using Sunshine.WorldServer.Game.Maps.Paddocks;
using Sunshine.WorldServer.Game.Maps.Interactives.Skills;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Handlers.Guilds
{
    public class GuildHandler : WorldPacketHandler
    {
        [WorldHandler(5546)]
        public static void HandleGuildCreationValidMessage(WorldClient client, GuildCreationValidMessage message)
        {
            if (client.Character.IsInDialog() && client.Character.Dialog is SkillUse)
            {
                if (client.Character.Guild == null)
                {
                    if (GuildManager.Instance.GetGuildByName(message.guildName) == null)
                    {
                        if (client.Character.Inventory.HasItem(1575))
                        {
                            GuildRecord guildRecord = new GuildRecord
                            {
                                Id = GuildManager.Instance.GenerateId(),
                                Date = DateTime.Now,
                                Name = message.guildName,
                                MaxTaxCollectors = 1,
                                SpellsCSV = "451,452,453,454,456,457,458,459,460,461,462",
                                SpellsLevelsCSV = "1,1,1,1,1,1,1,1,1,1,1",
                                Pods = 1000,
                                Prospecting = 100,
                                EmblemBackgroundShape = message.guildEmblem.backgroundShape,
                                EmblemBackgroundColor = message.guildEmblem.backgroundColor,
                                EmblemForegroundShape = message.guildEmblem.symbolShape,
                                EmblemForegroundColor = message.guildEmblem.symbolColor
                            };

                            GuildMemberRecord guildMemberRecord = new GuildMemberRecord
                            {
                                Owner = client.Character.Id,
                                Account = client.Account.Id,
                                Guild = guildRecord.Id,
                                Rank = 1,
                                Rights = 127998
                            };

                            Guild guild = new Guild(guildRecord);

                            Game.Guilds.GuildMember guildMember = new Game.Guilds.GuildMember(guildMemberRecord, client.Character);

                            client.Character.GuildMember = guildMember;

                            GuildManager.Instance.CreateGuild(guild, guildMember);

                            GuildHandler.SendGuildCreationResultMessage(client, GuildCreationResultEnum.GUILD_CREATE_OK);
                        }
                        else
                            GuildHandler.SendGuildCreationResultMessage(client, GuildCreationResultEnum.GUILD_CREATE_ERROR_REQUIREMENT_UNMET);
                    }
                    else
                        GuildHandler.SendGuildCreationResultMessage(client, GuildCreationResultEnum.GUILD_CREATE_ERROR_NAME_ALREADY_EXISTS);
                }
                else
                {
                    GuildHandler.SendGuildCreationResultMessage(client, GuildCreationResultEnum.GUILD_CREATE_ERROR_ALREADY_IN_GUILD);
                    client.Character.Dialog = null;
                }
            }
        }

        [WorldHandler(5550)]
        public static void HandleGuildGetInformationsMessage(WorldClient client, GuildGetInformationsMessage message)
        {
            if (client.Character.Guild != null)
            {
                switch (message.infoType)
                {
                    case 1:
                        GuildHandler.SendGuildInformationsGeneralMessage(client, client.Character.Guild);
                        break;
                    case 2:
                        GuildHandler.SendGuildInformationsMembersMessage(client, client.Character.Guild);
                        break;
                    case 3:
                        GuildHandler.SendGuildInfosUpgradeMessage(client, client.Character.Guild);
                        break;
                    case 4:
                        GuildHandler.SendGuildInformationsPaddocksMessage(client, client.Character.Guild);
                        break;
                    case 5:
                        GuildHandler.SendGuildHousesInformationMessage(client, client.Character.Guild);
                        break;
                    case 6:
                        TaxCollectorHandler.SendTaxCollectorListMessage(client, client.Character.Guild);
                        break;
                }
            }
        }

        [WorldHandler(5699)]
        public static void HandleGuildSpellUpgradeRequestMessage(WorldClient client, GuildSpellUpgradeRequestMessage message)
        {
            if (client.Character.Guild != null && client.Character.Guild.UpgradeSpell(message.spellId))
            {
                for (int i = 0; i < client.Character.Guild.Members.Count; i++)
                {
                    if (client.Character.Guild.Members[i].IsConnected())
                        GuildHandler.SendGuildInfosUpgradeMessage(client.Character.Guild.Members[i].Client, client.Character.Guild);
                }
            }
        }

        [WorldHandler(5551)]
        public static void HandleGuildInvitationMessage(WorldClient client, GuildInvitationMessage message)
        {
            if (client.Character.Guild != null)
            {
                if (!client.Character.GuildMember.HasRight(GuildRightsBitEnum.GUILD_RIGHT_INVITE_NEW_MEMBERS) && !client.Character.GuildMember.IsBoss)
                    client.Character.SendInformationMessage(TextInformationTypeEnum.TEXT_INFORMATION_ERROR, 207, new object[0]);
                else
                {
                    Character character = WorldServerManager.Instance.GetCharacter(message.targetId);
                    if (character == null)
                        client.Character.SendInformationMessage(TextInformationTypeEnum.TEXT_INFORMATION_ERROR, 208, new object[0]);
                    else
                    {
                        if (character.Guild != null)
                            client.Character.SendInformationMessage(TextInformationTypeEnum.TEXT_INFORMATION_ERROR, 206, new object[0]);
                        else
                        {
                            if (character.IsBusy())
                                client.Character.SendInformationMessage(TextInformationTypeEnum.TEXT_INFORMATION_ERROR, 209, new object[0]);
                            else
                            {
                                if (!client.Character.Guild.CanAddMember())
                                {
                                    client.Character.SendInformationMessage(TextInformationTypeEnum.TEXT_INFORMATION_ERROR, 55, new object[]
                                    {
                                        50
                                    });
                                }
                                else
                                {
                                    GuildDialog guildDialog = new GuildDialog(client.Character, character);
                                    guildDialog.SendInvitation();
                                }
                            }
                        }
                    }
                }
            }
        }

        [WorldHandler(6115)]
        public static void HandleGuildInvitationByNameMessage(WorldClient client, GuildInvitationByNameMessage message)
        {
            if (client.Character.Guild != null)
            {
                if (!client.Character.GuildMember.HasRight(GuildRightsBitEnum.GUILD_RIGHT_INVITE_NEW_MEMBERS) && !client.Character.GuildMember.IsBoss)
                    client.Character.SendInformationMessage(TextInformationTypeEnum.TEXT_INFORMATION_ERROR, 207, new object[0]);
                else
                {
                    Character character = WorldServerManager.Instance.GetCharacter(message.name);
                    if (character == null)
                        client.Character.SendInformationMessage(TextInformationTypeEnum.TEXT_INFORMATION_ERROR, 208, new object[0]);
                    else
                    {
                        if (character.Guild != null)
                            client.Character.SendInformationMessage(TextInformationTypeEnum.TEXT_INFORMATION_ERROR, 206, new object[0]);
                        else
                        {
                            if (character.IsBusy())
                                client.Character.SendInformationMessage(TextInformationTypeEnum.TEXT_INFORMATION_ERROR, 209, new object[0]);
                            else
                            {
                                if (!client.Character.Guild.CanAddMember())
                                {
                                    client.Character.SendInformationMessage(TextInformationTypeEnum.TEXT_INFORMATION_ERROR, 55, new object[]
                                    {
                                        50
                                    });
                                }
                                else
                                {
                                    GuildDialog guildDialog = new GuildDialog(client.Character, character);
                                    guildDialog.SendInvitation();
                                }
                            }
                        }
                    }
                }
            }
        }

        [WorldHandler(5887)]
        public static void HandleGuildKickRequestMessage(WorldClient client, GuildKickRequestMessage message)
        {
            if (client.Character.Guild != null)
            {
                Game.Guilds.GuildMember guildMember = client.Character.Guild.GetGuildMember(message.kickedId);
                if (guildMember != null)
                    guildMember.Guild.KickMember(client.Character.GuildMember, guildMember);
            }
        }

        [WorldHandler(5556)]
        public static void HandleGuildInvitationAnswerMessage(WorldClient client, GuildInvitationAnswerMessage message)
        {
            if (client.Character.IsInDialog() && client.Character.Dialog is GuildDialog)
            {
                GuildDialog guildDialog = client.Character.Dialog as GuildDialog;

                if (guildDialog != null)
                {
                    if (!message.accept || client.Character == guildDialog.Recruter)
                        guildDialog.CancelInvitation();
                    else
                    {
                        if (message.accept)
                            guildDialog.AcceptInvitation();
                        else
                            guildDialog.DenyInvitation();
                    }
                }
            }
        }

        [WorldHandler(5549)]
        public static void HandleGuildChangeMemberParametersMessage(WorldClient client, GuildChangeMemberParametersMessage message)
        {
            if (client.Character.Guild != null)
            {
                Game.Guilds.GuildMember guildMember = client.Character.Guild.GetGuildMember(message.memberId);

                if (guildMember != null)
                    client.Character.Guild.UpdateMember(client.Character.GuildMember, guildMember, message.rank, message.experienceGivenPercent, message.rights);
            }
        }

        public static void SendGuildCreationStartedMessage(WorldClient client)
        {
            client.Send(new GuildCreationStartedMessage());
        }

        public static void SendGuildCreationResultMessage(WorldClient client, GuildCreationResultEnum result)
        {
            client.Send(new GuildCreationResultMessage((sbyte)result));
        }

        public static void SendGuildJoinedMessage(WorldClient client, Game.Guilds.GuildMember member)
        {
            client.Send(new GuildJoinedMessage(member.Guild.GetGuildInformations(), (uint)member.Rights));
        }

        public static void SendGuildInformationsMembersMessage(WorldClient client, Guild guild)
        {
            client.Send(new GuildInformationsMembersMessage(guild.Members.Select(x => x.GetGuildMember())));
        }

        public static void SendGuildInformationsGeneralMessage(WorldClient client, Guild guild)
        {
            client.Send(new GuildInformationsGeneralMessage(true, guild.Level, guild.ExpLevelFloor, guild.Experience, guild.ExpNextLevelFloor));
        }

        public static void SendGuildMembershipMessage(WorldClient client, Game.Guilds.GuildMember member)
        {
            client.Send(new GuildMembershipMessage(member.Guild.GetGuildInformations(), (uint)member.Rights));
        }

        public static void SendGuildInformationsMemberUpdateMessage(WorldClient client, Game.Guilds.GuildMember member)
        {
            client.Send(new GuildInformationsMemberUpdateMessage(member.GetGuildMember()));
        }

        public static void SendGuildMemberWarnOnConnectionStateMessage(WorldClient client, bool state)
        {
            client.Send(new GuildMemberSetWarnOnConnectionMessage(state));
        }

        public static void SendGuildInvitedMessage(WorldClient client, Character recruter)
        {
            client.Send(new GuildInvitedMessage(recruter.Id, recruter.Name, recruter.Guild.GetBasicGuildInformations()));
        }

        public static void SendGuildInvitationStateRecrutedMessage(WorldClient client, GuildInvitationStateEnum state)
        {
            client.Send(new GuildInvitationStateRecrutedMessage((sbyte)state));
        }

        public static void SendGuildInvitationStateRecruterMessage(WorldClient client, Character recruted, GuildInvitationStateEnum state)
        {
            client.Send(new GuildInvitationStateRecruterMessage(recruted.Name, (sbyte)state));
        }

        public static void SendGuildInfosUpgradeMessage(WorldClient client, Guild guild)
        {
            client.Send(new GuildInfosUpgradeMessage(guild.MaxTaxCollectors, (sbyte)guild.TaxCollectors.Count, guild.TaxCollectorHealth, guild.TaxCollectorDamageBonus, guild.Pods, guild.Prospecting, guild.Wisdom, guild.Boost, guild.Spells, guild.SpellsLevels));
        }

        public static void SendGuildInformationsPaddocksMessage(WorldClient client, Guild guild)
        {
            if (client == null || guild == null)
                return;

            var paddocks = PaddockManager.Instance.GetPaddocksByGuild(guild.Id)
                .Select(x => x.GetGuildContentInformations())
                .ToArray();

            client.Send(new GuildInformationsPaddocksMessage((sbyte)Math.Max(0, guild.MaxPaddocks), paddocks));
        }

        public static void SendGuildHousesInformationMessage(WorldClient client, Guild guild)
        {
            var houses = HouseManager.Instance.GetHouses()
                .Where(x => x.GuildId.HasValue &&
                            x.GuildId.Value == guild.Id &&
                            x.IsGuildShared())
                .Select(x => x.GetInformationsForGuild())
                .ToArray();

            client.Send(new GuildHousesInformationMessage(houses));
        }

        public static void SendGuildMemberLeavingMessage(WorldClient client, Game.Guilds.GuildMember member, bool kicked)
        {
            client.Send(new GuildMemberLeavingMessage(kicked, member.Owner.Id));
        }

        public static void SendGuildLeftMessage(WorldClient client)
        {
            client.Send(new GuildLeftMessage());
        }
    }
}
