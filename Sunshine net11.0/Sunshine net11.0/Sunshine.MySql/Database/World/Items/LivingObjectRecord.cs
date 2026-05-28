using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;

namespace Sunshine.MySql.Database.World.Items
{
    [Table("items_livingobjects")]
    public class LivingObjectRecord
    {
        private List<int> _skins = new List<int>();
        private string _skinsCsv;
        private byte[] _moodsBin;
        private List<List<int>> _moods = new List<List<int>>();

        [ExplicitKey]
        public int Id { get; set; }

        public byte[] MoodsBin
        {
            get => _moodsBin;
            set
            {
                _moodsBin = value;
                _moods = DeserializeMoods(value);
            }
        }

        [Write(false)]
        public List<List<int>> Moods
        {
            get => _moods ?? new List<List<int>>();
            set
            {
                _moods = value ?? new List<List<int>>();
                _moodsBin = SerializeMoods(_moods);
            }
        }

        public int ItemType { get; set; }

        public string SkinsCSV
        {
            get => _skinsCsv;
            set
            {
                _skinsCsv = value;
                _skins = string.IsNullOrWhiteSpace(value)
                    ? new List<int>()
                    : value
                        .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(x =>
                        {
                            int parsed;
                            return int.TryParse(x.Trim(), out parsed) ? parsed : 0;
                        })
                        .Where(x => x > 0)
                        .ToList();
            }
        }

        [Write(false)]
        public List<int> Skins
        {
            get => _skins;
            set
            {
                _skins = value ?? new List<int>();
                _skinsCsv = _skins.Count == 0 ? string.Empty : string.Join(",", _skins);
            }
        }

        private static List<List<int>> DeserializeMoods(byte[] value)
        {
            if (value == null || value.Length == 0)
                return new List<List<int>>();

            try
            {
#pragma warning disable SYSLIB0011
                using (var stream = new MemoryStream(value))
                {
                    var formatter = new BinaryFormatter();
                    return formatter.Deserialize(stream) as List<List<int>> ?? new List<List<int>>();
                }
#pragma warning restore SYSLIB0011
            }
            catch
            {
                return new List<List<int>>();
            }
        }

        private static byte[] SerializeMoods(List<List<int>> value)
        {
            if (value == null || value.Count == 0)
                return null;

            try
            {
#pragma warning disable SYSLIB0011
                using (var stream = new MemoryStream())
                {
                    var formatter = new BinaryFormatter();
                    formatter.Serialize(stream, value);
                    return stream.ToArray();
                }
#pragma warning restore SYSLIB0011
            }
            catch
            {
                return null;
            }
        }
    }
}
