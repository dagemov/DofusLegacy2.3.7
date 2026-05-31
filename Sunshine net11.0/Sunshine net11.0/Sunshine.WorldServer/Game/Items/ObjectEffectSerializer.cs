using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;
using Sunshine.Protocol.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sunshine.WorldServer.Game.Items
{
    public static class ObjectEffectSerializer
    {
        public static string Serialize(IEnumerable<ObjectEffect> effects)
        {
            var list = (effects ?? Enumerable.Empty<ObjectEffect>()).ToList();
            BigEndianWriter writer = new BigEndianWriter();

            writer.WriteShort((short)list.Count);

            foreach (var effect in list)
            {
                writer.WriteShort(effect.TypeId);
                effect.Serialize(writer);
            }

            return BitConverter.ToString(writer.Data).Replace("-", string.Empty);
        }

        public static List<ObjectEffect> Clone(IEnumerable<ObjectEffect> effects)
        {
            var list = (effects ?? Enumerable.Empty<ObjectEffect>()).ToList();
            if (list.Count == 0)
                return new List<ObjectEffect>();

            return Deserialize(Serialize(list));
        }

        public static List<ObjectEffect> Deserialize(string hex)
        {
            List<ObjectEffect> results = new List<ObjectEffect>();

            if (string.IsNullOrWhiteSpace(hex))
                return results;

            try
            {
                byte[] data = Utils.GetHexaToByteArray(hex);
                BigEndianReader reader = new BigEndianReader(data);

                int count = reader.ReadShort();
                for (int i = 0; i < count; i++)
                {
                    short typeId = reader.ReadShort();
                    var effect = ProtocolTypeManager.GetInstance<ObjectEffect>(typeId);
                    effect.Deserialize(reader);
                    results.Add(effect);
                }
            }
            catch
            {
                // Fallback géré ailleurs pour les anciens items sauvegardés au vieux format
            }

            return results;
        }
    }
}