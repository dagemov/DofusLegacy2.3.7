using Sunshine.Protocol.Enums;
using Sunshine.Protocol.Types;
using Sunshine.WorldServer.Game.Characters;
using Sunshine.WorldServer.Handlers.Context.RolePlay.Party;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Parties
{
    public class Party : IParty
    {
        private List<Character> _guests;
        private List<Character> _members;

        public Party(int id, Character leader)
        {
            Id = id;
            Leader = leader;
            _guests = new List<Character>();
            _members = new List<Character>();
            PartyHandler.SendPartyJoinMessage(leader.Client, this);
        }

        public int Id { get; set; }

        public Character Leader { get; set; }

        public Character Sender { get; set; }

        public int MaxMemberCount { get { return 8; } }

        public PartyTypeEnum Type { get { return PartyTypeEnum.PARTY_TYPE_CLASSICAL; } }

        public bool IsFull { get { return _members.Count >= 8 ? true : false; } }

        public List<PartyInvitationMemberInformations> GetPartyInvitationMemberInformations
        {
            get
            {
                List<PartyInvitationMemberInformations> memberinvitationinfos = new List<PartyInvitationMemberInformations>();
                foreach (var member in _members)
                    memberinvitationinfos.Add(member.GetPartyInvitationMemberInformations);

                return memberinvitationinfos;
            }
        }

        public List<PartyMemberInformations> GetPartyMemberInformations
        {
            get
            {
                List<PartyMemberInformations> memberinfos = new List<PartyMemberInformations>();
                foreach (var member in _members)
                    memberinfos.Add(member.GetPartyMemberInformations);

                return memberinfos;
            }
        }

        public List<PartyGuestInformations> GetPartyGuestInformations
        {
            get
            {
                List<PartyGuestInformations> guestinfos = new List<PartyGuestInformations>();
                foreach (var guest in _guests)
                    guestinfos.Add(guest.GetPartyGuestInformations);   
                
                return guestinfos;
            }
        }   

        public void AcceptMember(Character member)
        {
            if (member.Party != null && member.Party.Id != Id)
                member.Party.RemoveMember(member);
            else
                AddMember(member);
        }

        public void Kick(Character member)
        {
            PartyHandler.SendPartyKickedByMessage(member.Client, Leader);
            RemoveMember(member);
        }

        public void AddMember(Character member)
        {
            _members.Add(member);
            if(member != Leader)
                RemoveGuest(member);
            member.Party = this;
            PartyHandler.SendPartyJoinMessage(member.Client, this);
            _members.ForEach(x => PartyHandler.SendPartyUpdateMessage(x.Client, member));
        }

        public void RemoveMember(Character member)
        {
            if (_members.Contains(member))
            {
                _members.Remove(member);
                member.Party = null;
                PartyHandler.SendPartyLeaveMessage(member.Client);
                _members.ForEach(x => PartyHandler.SendPartyMemberRemoveMessage(x.Client, member));
            }

            if ((_members.Count == 1))
                DeleteParty();
        }

        public Character GetMember(int memberid)
        {
            return _members.FirstOrDefault(x => x.Id == memberid);               
        }

        public void AddGuest(Character guest)
        {
            _guests.Add(guest);
            guest.Party = this;
            _members.ForEach(x => PartyHandler.SendPartyNewGuestMessage(x.Client, guest));
        }

        public void RemoveGuest(Character guest)
        {
            if (_guests.Contains(guest))
                _guests.Remove(guest);
                
            if ((_members.Count == 1))
                DeleteParty();

            guest.Party = null;

        }

        public void RemoveGuest(Character guest, bool cancel)
        {
            if (_guests.Contains(guest))
            {
                _guests.Remove(guest);
                PartyHandler.SendPartyLeaveMessage(guest.Client);
                _members.ForEach(x => PartyHandler.SendPartyMemberRemoveMessage(x.Client, guest));
            }

            if (cancel)
            {
                PartyHandler.SendPartyInvitationCancelledForGuestMessage(guest.Client, this, Sender);
                _members.ForEach(x => PartyHandler.SendPartyCancelInvitationNotificationMessage(x.Client, Sender, guest));
            }
            else
            {
                PartyHandler.SendPartyInvitationCancelledForGuestMessage(guest.Client, this, guest);
                _members.ForEach(x => PartyHandler.SendPartyRefuseInvitationNotificationMessage(x.Client, guest));
            }

            guest.Party = null;

            if ((_members.Count == 1))
                DeleteParty();
        }

        private void DeleteParty()
        {
            RemoveMember(_members.First());
            PartyManager.Instance.RemoveParty(this);
        }

        public Character GetGuest(int guestid)
        {
            return _guests.FirstOrDefault(x => x.Id == guestid);
        }
    }
}
