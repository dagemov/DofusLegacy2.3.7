using Sunshine.MySql.Database.Managers;
using Sunshine.MySql.Database.World.Characters;
using Sunshine.WorldServer.Game.Characters;
using System;
using System.Linq;

namespace Sunshine.BaseServer.Loaders.World.Characters
{
    public static class CharactersLoader
    {
        public static void Initialize()
        {
            Logs.Logger.Write("[ World ] Loading Characters...");

            CharacterManager.Instance.Characters.Clear();

            var records = CharacterManager.Instance.GetAllCharacters().ToList();
            int loaded = 0;
            int skipped = 0;

            foreach (var record in records)
            {
                try
                {
                    if (record == null)
                    {
                        skipped++;
                        continue;
                    }

                    if (CharacterManager.Instance.Characters.ContainsKey(record.Id))
                    {
                        Logs.Logger.WriteError($"[ World ] Personnage dupliqué ignoré : {record.Id} / {record.Name}");
                        skipped++;
                        continue;
                    }

                    CharacterManager.Instance.Characters.Add(record.Id, new Character(record));
                    loaded++;
                }
                catch (Exception ex)
                {
                    Logs.Logger.WriteError($"[ World ] Impossible de charger le personnage {record?.Id} ({record?.Name}) : {ex}");
                    skipped++;
                }
            }

            Logs.Logger.Write($"[ World ] {loaded} Characters Loaded ({skipped} ignorés)");
        }
    }
}
