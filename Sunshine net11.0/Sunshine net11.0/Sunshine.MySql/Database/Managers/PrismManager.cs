using Dapper;
using Dapper.Contrib.Extensions;
using Sunshine.Mysql.Database;
using Sunshine.MySql.Database.World.Maps.Prisms;
using Sunshine.Protocol.Enums;
using Sunshine.Protocol.Utils;
using Sunshine.WorldServer.Game.Actors;
using Sunshine.WorldServer.Game.Characters;
using Sunshine.WorldServer.Game.Maps;
using Sunshine.WorldServer.Game.Maps.Prisms;
using Sunshine.WorldServer.Game.Fights;
using Sunshine.WorldServer.Game.Fights.Types;
using Sunshine.WorldServer.Handlers.PvP;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sunshine.MySql.Database.Managers
{
    public class PrismManager : Singleton<PrismManager>
    {
        public const int PrismConquestItemTemplateId = 8990;

        private readonly Dictionary<int, WorldMapPrismRecord> m_prisms = new Dictionary<int, WorldMapPrismRecord>();
        private readonly HashSet<int> m_syncingMaps = new HashSet<int>();
        private readonly object m_syncLock = new object();
        private bool m_initialized;

        public void Initialize()
        {
            if (m_initialized)
                return;

            EnsureTable();
            Load();
            PurgeInvalidRecords();
            RebuildAllPrismActors();
            m_initialized = true;
        }

        private void EnsureTable()
        {
            const string sql = @"
CREATE TABLE IF NOT EXISTS `world_maps_prism` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `SubAreaId` int(11) NOT NULL,
  `MapId` int(11) NOT NULL,
  `CellId` smallint(6) NOT NULL DEFAULT '0',
  `WorldX` smallint(6) NOT NULL DEFAULT '0',
  `WorldY` smallint(6) NOT NULL DEFAULT '0',
  `AlignmentSide` tinyint(4) NOT NULL DEFAULT '0',
  `PlacementDate` datetime NOT NULL,
  `IsInFight` tinyint(1) NOT NULL DEFAULT '0',
  `IsFightable` tinyint(1) NOT NULL DEFAULT '1',
  `Defeated` datetime NULL DEFAULT NULL,
  `LastFight` datetime NULL DEFAULT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `idx_world_maps_prism_subarea` (`SubAreaId`),
  KEY `idx_world_maps_prism_map` (`MapId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";

            DatabaseManager.Connection.Execute(sql);
        }

        private void Load()
        {
            m_prisms.Clear();
            var records = DatabaseManager.Connection.Query<WorldMapPrismRecord>("SELECT * FROM world_maps_prism").ToList();
            foreach (var record in records)
                m_prisms[record.SubAreaId] = record;
        }

        public bool IsPrismItem(int templateId)
        {
            return templateId == PrismConquestItemTemplateId;
        }

        public IEnumerable<WorldMapPrismRecord> GetAllPrisms(bool includeDefeated)
        {
            if (includeDefeated)
                return m_prisms.Values.ToList();

            return m_prisms.Values.Where(x => !x.WasDefeated).ToList();
        }

        public WorldMapPrismRecord GetPrism(int subAreaId)
        {
            WorldMapPrismRecord prism;
            if (m_prisms.TryGetValue(subAreaId, out prism) && !prism.WasDefeated)
                return prism;

            return null;
        }

        public WorldMapPrismRecord GetAnyPrism(int subAreaId)
        {
            WorldMapPrismRecord prism;
            if (m_prisms.TryGetValue(subAreaId, out prism))
                return prism;

            return null;
        }

        public int CountActivePrisms()
        {
            return m_prisms.Values.Count(x => !x.WasDefeated && x.AlignmentSide > 0);
        }

        public int CountOwned(AlignmentSideEnum side)
        {
            return m_prisms.Values.Count(x => !x.WasDefeated && x.AlignmentSide == (sbyte)side);
        }

        public IEnumerable<short> GetAlignmentSubAreas(AlignmentSideEnum side)
        {
            return m_prisms.Values
                .Where(x => !x.WasDefeated && x.AlignmentSide == (sbyte)side)
                .Select(x => (short)x.SubAreaId)
                .Distinct()
                .ToList();
        }

        public IEnumerable<Map> GetTeleportMaps(AlignmentSideEnum side)
        {
            return m_prisms.Values
                .Where(x => !x.WasDefeated && !x.IsInFight && x.AlignmentSide == (sbyte)side)
                .Select(x => MapManager.Instance.GetMap(x.MapId))
                .Where(x => x != null)
                .Distinct()
                .ToList();
        }

        public WorldMapPrismRecord GetInFightPrism(AlignmentSideEnum side)
        {
            return m_prisms.Values.FirstOrDefault(x => !x.WasDefeated && x.IsInFight && x.AlignmentSide == (sbyte)side);
        }

        public FightPvPrism GetPrismFight(WorldMapPrismRecord prism)
        {
            if (prism == null)
                return null;

            var map = MapManager.Instance.GetMap(prism.MapId);
            if (map == null)
                return null;

            return map.Fights.OfType<FightPvPrism>().FirstOrDefault(x => x.PrismRecord != null && x.PrismRecord.SubAreaId == prism.SubAreaId);
        }

        public void MarkInFight(WorldMapPrismRecord prism)
        {
            if (prism == null)
                return;

            prism.IsInFight = true;
            prism.LastFight = DateTime.UtcNow;
            DatabaseManager.Connection.Update(prism);
            m_prisms[prism.SubAreaId] = prism;
            RefreshPrismActor(prism);
            BroadcastUpdates();
        }

        public void MarkDefended(WorldMapPrismRecord prism)
        {
            if (prism == null)
                return;

            var defendingAlignment = (AlignmentSideEnum)prism.AlignmentSide;

            prism.IsInFight = false;
            prism.Defeated = null;
            DatabaseManager.Connection.Update(prism);
            m_prisms[prism.SubAreaId] = prism;
            RefreshPrismActor(prism);
            BroadcastUpdates();
            PvPHandler.BroadcastPrismFightEnded(prism, defendingAlignment);
        }

        public void MarkDefeated(WorldMapPrismRecord prism)
        {
            if (prism == null)
                return;

            var defendingAlignment = (AlignmentSideEnum)prism.AlignmentSide;

            prism.IsInFight = false;
            prism.AlignmentSide = 0;
            prism.Defeated = DateTime.UtcNow;
            DatabaseManager.Connection.Update(prism);
            m_prisms[prism.SubAreaId] = prism;
            RefreshPrismActor(prism);
            BroadcastUpdates();
            PvPHandler.BroadcastPrismFightEnded(prism, defendingAlignment);
        }

        public bool TryPlacePrism(Character character, short cellId, out string reason)
        {
            reason = string.Empty;

            if (character == null || character.Map == null)
            {
                reason = "Carte invalide.";
                return false;
            }

            if (character.Map.IsInstance() || !character.Map.IsCanonicalMap())
            {
                reason = "Impossible de poser un prisme sur une carte instanciée.";
                return false;
            }

            if (character.Alignment.Side != AlignmentSideEnum.ALIGNMENT_ANGEL &&
                character.Alignment.Side != AlignmentSideEnum.ALIGNMENT_EVIL)
            {
                reason = "Vous devez être bontarien ou brakmarien pour poser un prisme.";
                return false;
            }

            if (character.Map.SubAreaId <= 0)
            {
                reason = "Sous-zone invalide pour la conquête.";
                return false;
            }

            var activePrism = GetPrism(character.Map.SubAreaId);
            if (activePrism != null)
            {
                reason = "Un prisme existe déjà dans cette sous-zone.";
                return false;
            }

            var anyPrism = GetAnyPrism(character.Map.SubAreaId);
            if (anyPrism != null && anyPrism.WasDefeated)
            {
                var remaining = TimeSpan.FromHours(12) - (DateTime.UtcNow - anyPrism.Defeated.Value);
                if (remaining.TotalSeconds > 0)
                {
                    reason = string.Format("Impossible de poser un prisme pour le moment. Réessayez dans {0}h {1}m.", Math.Max(0, remaining.Hours + (remaining.Days * 24)), Math.Max(0, remaining.Minutes));
                    return false;
                }
            }

            var record = anyPrism ?? new WorldMapPrismRecord();
            record.SubAreaId = character.Map.SubAreaId;
            record.MapId = character.Map.Id;
            record.CellId = cellId;
            record.WorldX = (short)(character.Map.Point != null ? character.Map.Point.X : 0);
            record.WorldY = (short)(character.Map.Point != null ? character.Map.Point.Y : 0);
            record.AlignmentSide = (sbyte)character.Alignment.Side;
            record.PlacementDate = DateTime.UtcNow;
            record.IsInFight = false;
            record.IsFightable = true;
            record.Defeated = null;
            record.LastFight = null;

            if (record.Id <= 0)
                record.Id = (int)DatabaseManager.Connection.Insert(record);
            else
                DatabaseManager.Connection.Update(record);

            m_prisms[record.SubAreaId] = record;
            RefreshPrismActor(record);
            BroadcastUpdates();
            return true;
        }

        public void SpawnAllPrisms()
        {
            RebuildAllPrismActors();
        }

        public WorldMapPrismRecord GetAuthoritativePrismForMap(Map map, bool requireSpawnable = true)
        {
            if (!CanDisplayPrismsOnMap(map))
                return null;

            WorldMapPrismRecord authoritative;
            if (!m_prisms.TryGetValue(map.SubAreaId, out authoritative) || authoritative == null)
                return null;

            if (authoritative.MapId != map.Id)
                return null;

            if (requireSpawnable && !IsSpawnableRecord(authoritative))
                return null;

            return authoritative;
        }

        public PrismActor GetAuthoritativePrismActor(Map map)
        {
            if (map == null)
                return null;

            var authoritative = GetAuthoritativePrismForMap(map, true);
            if (authoritative == null)
                return null;

            return map.RolePlayActors
                .OfType<PrismActor>()
                .FirstOrDefault(x => x.Record != null && x.Record.Id == authoritative.Id && IsAuthoritativePrismActor(map, x));
        }

        public bool IsSyncingMap(Map map)
        {
            if (map == null)
                return false;

            lock (m_syncLock)
                return m_syncingMaps.Contains(map.Id);
        }

        public bool IsAuthoritativePrismActor(Map map, PrismActor actor)
        {
            if (!CanDisplayPrismsOnMap(map) || actor == null || actor.Record == null)
                return false;

            if (actor.Record.MapId != map.Id || actor.Record.SubAreaId != map.SubAreaId)
                return false;

            WorldMapPrismRecord authoritative;
            if (!m_prisms.TryGetValue(actor.Record.SubAreaId, out authoritative) || authoritative == null)
                return false;

            if (!IsSpawnableRecord(authoritative))
                return false;

            return authoritative.Id == actor.Record.Id &&
                   authoritative.MapId == actor.Record.MapId &&
                   authoritative.SubAreaId == actor.Record.SubAreaId &&
                   authoritative.CellId == actor.Record.CellId &&
                   authoritative.AlignmentSide == actor.Record.AlignmentSide;
        }

        public void EnsureMapPrismState(Map map)
        {
            if (map == null)
                return;

            lock (m_syncLock)
            {
                if (m_syncingMaps.Contains(map.Id))
                    return;

                m_syncingMaps.Add(map.Id);
            }

            try
            {
                if (!CanDisplayPrismsOnMap(map))
                {
                    var allPrismActors = map.RolePlayActors
                        .OfType<PrismActor>()
                        .ToList();

                    foreach (var prismActor in allPrismActors)
                        map.LeaveActor(prismActor);

                    return;
                }

                var invalidActors = map.RolePlayActors
                    .OfType<PrismActor>()
                    .Where(x => !IsAuthoritativePrismActor(map, x) ||
                                x.Record == null)
                    .ToList();

                // Nettoyage agressif des prismes fantomatiques : sur une carte donnée il ne doit
                // jamais exister qu'un seul prisme autoritatif, strictement lié au subArea et à la map.
                if (invalidActors.Count == 0)
                {
                    var duplicates = map.RolePlayActors
                        .OfType<PrismActor>()
                        .GroupBy(x => x.Record != null ? x.Record.SubAreaId : 0)
                        .Where(g => g.Count() > 1)
                        .SelectMany(g => g.Skip(1))
                        .ToList();

                    invalidActors.AddRange(duplicates);
                }

                foreach (var actor in invalidActors)
                    map.LeaveActor(actor);

                var authoritative = GetAuthoritativePrismForMap(map, true);
                if (authoritative == null)
                    return;

                if (map.RolePlayActors.OfType<PrismActor>().Any(x => IsAuthoritativePrismActor(map, x)))
                    return;

                map.EnterActor(new PrismActor(ActorManager.Instance.GenerateId(true), authoritative));
            }
            finally
            {
                lock (m_syncLock)
                    m_syncingMaps.Remove(map.Id);
            }
        }

        private void RebuildAllPrismActors()
        {
            RemoveAllPrismActors();

            foreach (var prism in m_prisms.Values.Where(IsSpawnableRecord).ToList())
            {
                var map = MapManager.Instance.GetMap(prism.MapId);
                if (!CanDisplayPrismsOnMap(map))
                    continue;

                // Toujours repasser par EnsureMapPrismState pour bénéficier du garde-fou
                // de réentrance et éviter toute apparition récursive pendant un EnterActor.
                EnsureMapPrismState(map);
            }
        }

        public void RefreshPrismActor(WorldMapPrismRecord record)
        {
            if (record == null)
                return;

            RemovePrismActors(record.SubAreaId, record.Id);

            WorldMapPrismRecord authoritative;
            if (!m_prisms.TryGetValue(record.SubAreaId, out authoritative) || authoritative == null || authoritative.Id != record.Id)
                return;

            if (!IsSpawnableRecord(authoritative))
                return;

            var map = MapManager.Instance.GetMap(authoritative.MapId);
            if (!CanDisplayPrismsOnMap(map))
                return;

            // Centralise toute recréation de prisme via EnsureMapPrismState afin d'éviter
            // les boucles récursives pendant l'envoi des acteurs en roleplay.
            EnsureMapPrismState(map);
        }

        private void RemovePrismActors(int subAreaId, int recordId)
        {
            foreach (var map in MapManager.Instance.Maps.Values)
            {
                var actors = map.RolePlayActors
                    .OfType<PrismActor>()
                    .Where(x => x.Record != null && (x.Record.SubAreaId == subAreaId || x.Record.Id == recordId))
                    .ToList();

                foreach (var actor in actors)
                    map.LeaveActor(actor);
            }
        }

        private void RemoveAllPrismActors()
        {
            foreach (var map in MapManager.Instance.Maps.Values)
            {
                var actors = map.RolePlayActors
                    .OfType<PrismActor>()
                    .ToList();

                foreach (var actor in actors)
                    map.LeaveActor(actor);
            }
        }

        private void PurgeInvalidRecords()
        {
            var invalidRecords = m_prisms.Values.Where(x => !IsPersistentRecordValid(x)).ToList();
            foreach (var record in invalidRecords)
            {
                RemovePrismActors(record.SubAreaId, record.Id);
                m_prisms.Remove(record.SubAreaId);

                if (record.Id > 0)
                    DatabaseManager.Connection.Execute("DELETE FROM world_maps_prism WHERE Id = @Id", new { Id = record.Id });
            }
        }

        private bool IsPersistentRecordValid(WorldMapPrismRecord record)
        {
            if (record == null)
                return false;

            if (record.SubAreaId <= 0 || record.MapId <= 0)
                return false;

            if (record.CellId < 0 || record.CellId >= 560)
                return false;

            var map = MapManager.Instance.GetMap(record.MapId);
            if (map == null)
                return false;

            if (map.IsInstance())
                return false;

            if (map.SubAreaId != record.SubAreaId)
                return false;

            if (map.Cells == null || record.CellId >= map.Cells.Length || !map.Cells[record.CellId].Walkable)
                return false;

            if (record.WorldX != (short)(map.Point != null ? map.Point.X : 0) ||
                record.WorldY != (short)(map.Point != null ? map.Point.Y : 0))
                return false;

            if (record.WasDefeated)
                return true;

            return record.AlignmentSide == (sbyte)AlignmentSideEnum.ALIGNMENT_ANGEL ||
                   record.AlignmentSide == (sbyte)AlignmentSideEnum.ALIGNMENT_EVIL;
        }

        private bool CanDisplayPrismsOnMap(Map map)
        {
            if (map == null)
                return false;

            if (map.IsInstance())
                return false;

            if (!map.IsCanonicalMap())
                return false;

            if (map.CanonicalMapId != map.Id)
                return false;

            return true;
        }

        private bool IsSpawnableRecord(WorldMapPrismRecord record)
        {
            return IsPersistentRecordValid(record) &&
                   !record.WasDefeated &&
                   !record.IsInFight;
        }

        private void BroadcastUpdates()
        {
            foreach (var character in CharacterManager.Instance.Characters.Values)
            {
                if (character == null || character.Client == null)
                    continue;

                PvPHandler.SendAlignmentSubAreasListMessage(character.Client);
                PvPHandler.SendPrismWorldInformationMessage(character.Client);

                if (character.Map != null && character.Map.Id == character.Record.MapId)
                    character.Client.Send(new Protocol.Messages.MapFightCountMessage((short)character.Map.Fights.Count));
            }
        }
    }
}
