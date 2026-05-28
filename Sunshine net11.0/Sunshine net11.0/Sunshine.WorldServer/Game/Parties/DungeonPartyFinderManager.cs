using Sunshine.MySql.Database.Managers;
using Sunshine.Protocol.Messages;
using Sunshine.Protocol.Utils;
using Sunshine.Protocol.Types;
using Sunshine.WorldServer.Client;
using Sunshine.WorldServer.Game.Characters;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sunshine.WorldServer.Game.Parties
{
    public class DungeonPartyFinderManager : Singleton<DungeonPartyFinderManager>
    {
        private readonly object _syncRoot = new object();
        private Dictionary<int, HashSet<short>> _registeredByPlayer = new Dictionary<int, HashSet<short>>();
        private Dictionary<short, HashSet<int>> _listenersByDungeon = new Dictionary<short, HashSet<int>>();
        private Dictionary<int, short> _currentDungeonByPlayer = new Dictionary<int, short>();
        private short[] _catalog = Array.Empty<short>();

        public void Initialize()
        {
            lock (_syncRoot)
                _catalog = BuildCatalog().Distinct().OrderBy(x => x).ToArray();
        }

        private void EnsureCatalog()
        {
            lock (_syncRoot)
            {
                if (_catalog != null && _catalog.Length > 0)
                    return;

                _catalog = BuildCatalog().Distinct().OrderBy(x => x).ToArray();
            }
        }
        private IEnumerable<short> BuildCatalog()
        {
            var result = new HashSet<short>();

            try
            {
                foreach (var dungeonId in DungeonManager.Instance.GetSearchDungeonIds())
                {
                    if (dungeonId > 0)
                        result.Add(dungeonId);
                }
            }
            catch
            {
            }

            return result;
        }

        public bool HasCatalog
        {
            get
            {
                EnsureCatalog();
                lock (_syncRoot)
                    return _catalog != null && _catalog.Length > 0;
            }
        }

        public List<short> GetPlayerAvailableDungeons(Character player)
        {
            EnsureCatalog();

            lock (_syncRoot)
            {
                if (_catalog != null && _catalog.Length > 0)
                    return _catalog.ToList();

                if (player != null && _registeredByPlayer.TryGetValue(player.Id, out var registered))
                    return registered.OrderBy(x => x).ToList();

                return new List<short>();
            }
        }

        public bool CanListen(short dungeonId)
        {
            if (dungeonId <= 0)
                return false;

            EnsureCatalog();

            lock (_syncRoot)
                return _catalog == null || _catalog.Length == 0 || _catalog.Contains(dungeonId);
        }

        public List<short> GetPlayerRegisteredDungeons(int playerId)
        {
            lock (_syncRoot)
            {
                if (_registeredByPlayer.TryGetValue(playerId, out var dungeons))
                    return dungeons.OrderBy(x => x).ToList();

                return new List<short>();
            }
        }

        public void RegisterPlayerForDungeons(Character player, IEnumerable<short> dungeonIds)
        {
            if (player == null)
                return;

            short? roomToLeave = null;

            lock (_syncRoot)
            {
                var cleaned = (dungeonIds ?? Enumerable.Empty<short>())
                    .Where(x => x > 0)
                    .Distinct()
                    .ToList();

                if (_catalog != null && _catalog.Length > 0)
                    cleaned = cleaned.Where(x => _catalog.Contains(x)).ToList();

                if (cleaned.Count > 0)
                    _registeredByPlayer[player.Id] = new HashSet<short>(cleaned);
                else
                    _registeredByPlayer.Remove(player.Id);

                if (_currentDungeonByPlayer.TryGetValue(player.Id, out var currentDungeon) && !cleaned.Contains(currentDungeon))
                    roomToLeave = currentDungeon;
            }

            if (roomToLeave.HasValue)
                LeaveRoom(player.Id, roomToLeave.Value);
        }

        public void JoinRoom(Character player, short dungeonId)
        {
            if (player == null)
                return;

            if (dungeonId <= 0)
            {
                LeaveCurrentRoom(player.Id);
                return;
            }

            short? oldDungeon = null;
            List<int> oldTargets = new List<int>();
            List<int> newTargets = new List<int>();
            DungeonPartyFinderPlayer addedPlayer = BuildPlayer(player);

            lock (_syncRoot)
            {
                if (_currentDungeonByPlayer.TryGetValue(player.Id, out var existingDungeon))
                {
                    if (existingDungeon == dungeonId)
                        return;

                    oldDungeon = existingDungeon;
                    oldTargets = GetRoomListenerIdsNoLock(existingDungeon)
                        .Where(x => x != player.Id)
                        .ToList();

                    if (_listenersByDungeon.TryGetValue(existingDungeon, out var oldSet))
                    {
                        oldSet.Remove(player.Id);
                        if (oldSet.Count == 0)
                            _listenersByDungeon.Remove(existingDungeon);
                    }
                    _currentDungeonByPlayer.Remove(player.Id);
                }

                if (!_listenersByDungeon.TryGetValue(dungeonId, out var listeners))
                {
                    listeners = new HashSet<int>();
                    _listenersByDungeon[dungeonId] = listeners;
                }

                listeners.Add(player.Id);
                _currentDungeonByPlayer[player.Id] = dungeonId;

                if (!_registeredByPlayer.TryGetValue(player.Id, out var registrations))
                {
                    registrations = new HashSet<short>();
                    _registeredByPlayer[player.Id] = registrations;
                }
                registrations.Add(dungeonId);

                newTargets = listeners.Where(x => x != player.Id).ToList();
            }

            if (oldDungeon.HasValue && oldTargets.Count > 0)
                NotifyRoomUpdate(oldDungeon.Value, Enumerable.Empty<DungeonPartyFinderPlayer>(), new[] { player.Id }, oldTargets);

            if (newTargets.Count > 0)
                NotifyRoomUpdate(dungeonId, new[] { addedPlayer }, Array.Empty<int>(), newTargets);
        }

        public void LeaveRoom(int playerId, short dungeonId)
        {
            List<int> targets = new List<int>();
            bool removed = false;

            lock (_syncRoot)
            {
                if (_listenersByDungeon.TryGetValue(dungeonId, out var listeners) && listeners.Remove(playerId))
                {
                    removed = true;
                    targets = listeners.ToList();
                    if (listeners.Count == 0)
                        _listenersByDungeon.Remove(dungeonId);
                }

                if (_currentDungeonByPlayer.TryGetValue(playerId, out var currentDungeon) && currentDungeon == dungeonId)
                    _currentDungeonByPlayer.Remove(playerId);
            }

            if (removed && targets.Count > 0)
                NotifyRoomUpdate(dungeonId, Enumerable.Empty<DungeonPartyFinderPlayer>(), new[] { playerId }, targets);
        }

        public void UnregisterPlayer(int playerId)
        {
            lock (_syncRoot)
                _registeredByPlayer.Remove(playerId);

            LeaveCurrentRoom(playerId);
        }

        public void LeaveCurrentRoom(int playerId)
        {
            short? roomToLeave = null;
            lock (_syncRoot)
            {
                if (_currentDungeonByPlayer.TryGetValue(playerId, out var dungeonId))
                    roomToLeave = dungeonId;
            }

            if (roomToLeave.HasValue)
                LeaveRoom(playerId, roomToLeave.Value);
        }

        public List<DungeonPartyFinderPlayer> GetRoomContent(short dungeonId)
        {
            List<int> playerIds;
            lock (_syncRoot)
                playerIds = GetRoomListenerIdsNoLock(dungeonId);

            var result = new List<DungeonPartyFinderPlayer>();
            foreach (var playerId in playerIds)
            {
                if (CharacterManager.Instance.Characters.TryGetValue(playerId, out var character) && character != null && character.Client != null)
                    result.Add(BuildPlayer(character));
            }

            return result.OrderBy(x => x.level).ThenBy(x => x.playerName).ToList();
        }

        public void RefreshPlayer(WorldClient client)
        {
            if (client == null || client.Character == null)
                return;

            client.Send(new DungeonPartyFinderAvailableDungeonsMessage(GetPlayerAvailableDungeons(client.Character)));
            client.Send(new DungeonPartyFinderRegisterSuccessMessage(GetPlayerRegisteredDungeons(client.Character.Id)));

            short? currentRoom = null;
            lock (_syncRoot)
            {
                if (_currentDungeonByPlayer.TryGetValue(client.Character.Id, out var dungeonId))
                    currentRoom = dungeonId;
            }

            if (currentRoom.HasValue)
                client.Send(new DungeonPartyFinderRoomContentMessage(currentRoom.Value, GetRoomContent(currentRoom.Value)));
        }

        private DungeonPartyFinderPlayer BuildPlayer(Character player)
        {
            return new DungeonPartyFinderPlayer(player.Id, player.Name, (sbyte)player.Breed, player.Sex, player.Level);
        }

        private List<int> GetRoomListenerIdsNoLock(short dungeonId)
        {
            if (_listenersByDungeon.TryGetValue(dungeonId, out var listeners))
                return listeners.ToList();

            return new List<int>();
        }

        private void NotifyRoomUpdate(short dungeonId, IEnumerable<DungeonPartyFinderPlayer> addedPlayers, IEnumerable<int> removedPlayersIds, IEnumerable<int> targets)
        {
            var added = (addedPlayers ?? Enumerable.Empty<DungeonPartyFinderPlayer>()).ToArray();
            var removed = (removedPlayersIds ?? Enumerable.Empty<int>()).ToArray();

            foreach (var targetId in (targets ?? Enumerable.Empty<int>()).Distinct())
            {
                if (CharacterManager.Instance.Characters.TryGetValue(targetId, out var target) && target != null && target.Client != null)
                    target.Client.Send(new DungeonPartyFinderRoomContentUpdateMessage(dungeonId, added, removed));
            }
        }
    }
}
