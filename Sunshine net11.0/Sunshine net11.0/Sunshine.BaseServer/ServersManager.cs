using Sunshine.AuthServer;
using Sunshine.Mysql.Database;
using Sunshine.Protocol.Messages;
using Sunshine.BaseServer.Messages;
using Sunshine.WorldServer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sunshine.BaseClient;
using Sunshine.AuthServer.Client;
using System.Net.Sockets;
using Sunshine.WorldServer.Client;
using Sunshine.Protocol.Types;
using Sunshine.BaseServer.Loaders.World.Characters;
using Sunshine.BaseServer.Loaders.World.Monsters;
using Sunshine.BaseServer.Loaders.World.Maps;
using Sunshine.BaseServer.Loaders.World.Stats;
using Sunshine.BaseServer.Loaders.World.Spells;
using Sunshine.BaseServer.Loaders.Commands;
using Sunshine.BaseServer.Loaders.World.Effects;
using Sunshine.Protocol.Utils;
using Sunshine.BaseServer.Loaders.World.Items;
using Sunshine.MySql.Database.Managers;
using Sunshine.Logs;
using Sunshine.Protocol.Enums;
using Sunshine.AuthServer.Handlers.Connection;
using Sunshine.BaseServer.Loaders.World.Npcs;
using Sunshine.BaseServer.Loaders.World.BidsHouse;
using Sunshine.BaseServer.Loaders.World.Maps.Interactives;
using Sunshine.BaseServer.Loaders.World.Maps.Triggers;
using Sunshine.BaseServer.Loaders.World.Quests;
using Sunshine.WorldServer.Game.Actors.Monsters;
using Sunshine.WorldServer.Game.Maps;
using Sunshine.BaseServer.Loaders.World.Guilds;
using Sunshine.BaseServer.Loaders.World.Houses;
using Sunshine.BaseServer.Loaders.World.Paddocks;
using Sunshine.BaseServer.Loaders.World.PaddockInstances;
using Sunshine.WorldServer.Game.Maps.Houses;
using Sunshine.WorldServer.Game.Actors.TaxCollectors;
using Sunshine.WorldServer.Game.Actors;
using Sunshine.BaseServer.Configuration;

namespace Sunshine.Servers
{
    public class ServersManager : Singleton<ServersManager>
    {
        private AuthServer.AuthServer _authServer;
        private WorldServer.WorldServer _worldServer;
        private System.Timers.Timer _timer;
        private System.Timers.Timer _timer2;

        private void Step(int index, int total, string label, Action action, string detail = null)
        {
            Program.BeginLoadingStep(index, total, label, detail ?? $"Initialisation de {label}...");
            var stopwatch = Stopwatch.StartNew();

            try
            {
                action();
                stopwatch.Stop();
                Program.CompleteLoadingStep(index, total, label, stopwatch.Elapsed);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Logs.Logger.WriteError($"Erreur pendant le chargement de {label} : {ex}");
                Program.FailLoadingStep(index, total, label, ex);
                throw;
            }
        }

        public void Start()
        {
            _authServer = new AuthServer.AuthServer();
            _worldServer = new WorldServer.WorldServer();

            const int totalSteps = 24;
            int step = 0;

            Step(++step, totalSteps, "Connexion MySQL", () => DatabaseManager.Initilize(), "Ouverture de la connexion MySQL...");
            Step(++step, totalSteps, "Messages", () => MessageInitializer.Initialize(), "Chargement des messages réseau...");
            Step(++step, totalSteps, "Types protocole", () => ProtocolTypeManager.Initialize(), "Initialisation des types du protocole...");
            Step(++step, totalSteps, "Serveur auth", () =>
            {
                SetWorldsStatus(ServerStatusEnum.STARTING, false);
            }, "Préparation de l'auth avant l'ouverture finale des serveurs...");
            Step(++step, totalSteps, "Effets", () => EffectsLoader.Initialize(), "Chargement des effets...");
            Step(++step, totalSteps, "Sorts", () => SpellsLoader.Initialize(), "Chargement des sorts...");
            Step(++step, totalSteps, "Items", () => ItemsLoader.Initialize(), "Chargement des objets...");
            Step(++step, totalSteps, "Commandes", () => CommandsLoader.Initialize(), "Chargement des commandes...");
            Step(++step, totalSteps, "Expériences", () => ExperienceManager.Instance.LoadAllExperiences(), "Chargement des courbes d'expérience...");
            Step(++step, totalSteps, "Maps", () => MapsLoader.Initialize(), "Chargement des cartes...");
            Step(++step, totalSteps, "Triggers", () => TriggersLoader.Initialize(), "Chargement des triggers de cartes...");
            Step(++step, totalSteps, "Interactives", () => InteractivesLoader.Initialize(), "Chargement des éléments interactifs...");
            Step(++step, totalSteps, "Monstres", () => MonstersLoader.Initialize(), "Chargement des monstres...");
            Step(++step, totalSteps, "Donjons", () => DungeonsLoader.Initialize(), "Chargement des donjons...");
            Step(++step, totalSteps, "Hôtel de vente", () => BidsHouseLoader.Initialize(), "Chargement des hôtels de vente...");
            Step(++step, totalSteps, "Maisons", () => HousesLoader.Initialize(), "Chargement des maisons...");
            Step(++step, totalSteps, "Enclos", () => PaddocksLoader.Initialize(), "Chargement des enclos de guilde...");
            Step(++step, totalSteps, "Enclos instanciés", () => PaddockInstancesLoader.Initialize(), "Chargement des portes d'enclos instanciés...");
            Step(++step, totalSteps, "Systèmes sociaux / banque / marchands", () =>
            {
                WorldServer.Game.Social.SocialRelationManager.Instance.EnsureTables();
                MerchantManager.Instance.Initialize();
                WorldServer.Game.Parties.DungeonPartyFinderManager.Instance.Initialize();
            }, "Création des tables amis / ennemis / banque / marchands / recherche de groupe...");
            Step(++step, totalSteps, "PNJ", () => NpcsLoader.Initialize(), "Chargement des PNJ...");
            Step(++step, totalSteps, "Quêtes / classes", () =>
            {
                QuestsLoader.Initialize();
                BreedsLoader.Initialize();
            }, "Chargement des quêtes et des classes...");
            Step(++step, totalSteps, "Guildes", () => GuildsLoader.Initialize(), "Chargement des guildes et percepteurs...");
            Step(++step, totalSteps, "Personnages", () => CharactersLoader.Initialize(), "Chargement des personnages et rattachements de compte...");
            Step(++step, totalSteps, "Serveur world", () =>
            {
                _authServer.Initialize();
                _worldServer.Initialize();
                InitializeTimer();
                UpdateServerStatus(ServerStatusEnum.ONLINE);
            }, "Ouverture simultanée de l'auth et du world, puis passage du serveur en ligne...");

        }

        public IEnumerable<T> GetClients<T>() where T : class
        {
            if (typeof(T).Name == "AuthClient")
                return _authServer.authClients.Select(x => (T)x.Value);
            else
                return _worldServer.worldClients.Select(x => (T)x.Value);
        }

        public void AddClient(Socket sck, IBaseClient baseClient)
        {
            if (baseClient is AuthClient)
                _authServer.authClients.Add(sck, (AuthClient)baseClient);
            else
                _worldServer.worldClients.Add(sck, (WorldClient)baseClient);
        }

        public void RemoveClient(IBaseClient baseClient)
        {
            try
            {
                if (baseClient is AuthClient)
                {
                    if (_authServer.authClients.Count > 0)
                    {
                        if (baseClient != null && _authServer.authClients.ContainsValue(baseClient))
                        {
                            Socket sck = _authServer.authClients.FirstOrDefault(x => x.Value == baseClient).Key;
                            if (sck != null)
                            {
                                _authServer.authClients.Remove(sck);
                                sck.Disconnect(false);
                                sck.Dispose();
                            }
                        }
                    }
                }
                else
                {
                    if (_worldServer.worldClients.Count > 0)
                    {
                        if (baseClient != null && _worldServer.worldClients.ContainsValue(baseClient))
                        {
                            Socket sck = _worldServer.worldClients.FirstOrDefault(x => x.Value == baseClient).Key;
                            if (sck != null)
                            {
                                _worldServer.worldClients.Remove(sck);
                                sck.Disconnect(false);
                                sck.Dispose();
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                if (baseClient is AuthClient)
                {
                    if (_authServer.authClients.Count > 0)
                    {
                        if (baseClient != null && _authServer.authClients.ContainsValue(baseClient))
                        {
                            for (int i = 0; i < _authServer.authClients.Count; i++)
                            {
                                if (_authServer.authClients.ElementAt(i).Value == baseClient)
                                {
                                    Socket sck = _authServer.authClients.ElementAt(i).Key;
                                    if (sck != null)
                                    {
                                        _authServer.authClients.Remove(sck);
                                        sck.Disconnect(false);
                                        sck.Dispose();
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (_worldServer.worldClients.Count > 0)
                    {
                        if (baseClient != null && _worldServer.worldClients.ContainsValue(baseClient))
                        {
                            for (int i = 0; i < _worldServer.worldClients.Count; i++)
                            {
                                if (_worldServer.worldClients.ElementAt(i).Value == baseClient)
                                {
                                    Socket sck = _worldServer.worldClients.ElementAt(i).Key;
                                    if (sck != null)
                                    {
                                        _worldServer.worldClients.Remove(sck);
                                        sck.Disconnect(false);
                                        sck.Dispose();
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public void UpdateServerStatus(ServerStatusEnum status)
        {
            SetWorldsStatus(status, true);
        }

        private void SetWorldsStatus(ServerStatusEnum status, bool notifyClients)
        {
            var worlds = WorldServerManager.Instance.GetWorlds();

            foreach (var world in worlds)
            {
                world.Status = (int)status;
                WorldServerManager.Instance.UpdateWorld(world);
            }

            if (!notifyClients || _authServer == null)
                return;

            foreach (AuthClient client in _authServer.authClients.Values)
                ConnectionHandler.SendServerStatusUpdateMessage(client);
        }

        public void SendAnnounce(TextInformationTypeEnum type, short messageId, params string[] parameters)
        {
            var characters = CharacterManager.Instance.Characters;

            foreach (var character in characters.Values.Where(x => x.IsInWorld()))
                character.SendInformationMessage(type, messageId, parameters);
        }

        public void Save()
        {

            try
            {
                Logger.WriteInfo("Saving world...");
                SendAnnounce(TextInformationTypeEnum.TEXT_INFORMATION_ERROR, 164, new string[0]);
                UpdateServerStatus(ServerStatusEnum.SAVING);
                var characters = CharacterManager.Instance.Characters.Values.ToList();
                foreach (var character in characters)
                    CharacterManager.Instance.Save(character);
                BidHouseManager.Instance.Save();
                HouseManager.Instance.Save();
                GuildManager.Instance.Save();
                SendAnnounce(TextInformationTypeEnum.TEXT_INFORMATION_ERROR, 165, new string[0]);
                Logger.WriteInfo("World saved !");
            }
            finally
            {
                UpdateServerStatus(ServerStatusEnum.ONLINE);
            }
        }

        public void Save(object sender, System.Timers.ElapsedEventArgs e)
        {
            _timer.Stop();
            try
            {
                Save();
            }
            finally
            {
                _timer.Start();
            }
        }

        public void MoveRandomForRolePlayActors(object sender, System.Timers.ElapsedEventArgs e)
        {
            _timer2.Stop();

            var maps = MapManager.Instance.Maps.Values;

            foreach (var map in maps)
            {
                var actors = map.RolePlayActors.Where(x => x is MonsterGroup || x is TaxCollector).ToList();

                for (int i = 0; i < actors.Count; i++)
                {
                    short cell = actors[i].GetGameRolePlayActorInformations().disposition.cellId;

                    MapPoint point = new MapPoint(cell);

                    MapPoint[] newPoints = point.GetAdjacentCells((short entry) => map.CellsInfoProvider.IsCellWalkable(entry)).ToArray();

                    if (newPoints.Length <= 0)
                        return;


                }
            }

            _timer2.Start();
        }

        public void InitializeTimer()
        {
            int autoSaveIntervalMinutes = Math.Max(1, GameConfig.GetInt("AutoSaveInterval", 5));
            _timer = new System.Timers.Timer(TimeSpan.FromMinutes(autoSaveIntervalMinutes).TotalMilliseconds);
            _timer.Elapsed += Save;
            _timer.Start();
            Logger.WriteInfo($"AutoSave interval configured to {autoSaveIntervalMinutes} minute(s).");

            _timer2 = new System.Timers.Timer(20000);
            _timer2.Elapsed += MoveRandomForRolePlayActors;
            _timer2.Start();
        }
    }
}
