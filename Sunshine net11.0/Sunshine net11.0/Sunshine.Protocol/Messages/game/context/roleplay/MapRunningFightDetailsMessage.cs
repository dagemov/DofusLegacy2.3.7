
















// Generated on 10/13/2017 02:18:48
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class MapRunningFightDetailsMessage : Message
{

public const uint Id = 5751;
public override uint MessageId
{
    get { return Id; }
}

public int fightId;
        public IEnumerable<string> names;
        public IEnumerable<short> levels;
        public sbyte teamSwap;
        public IEnumerable<bool> alives;
        

public MapRunningFightDetailsMessage()
{
}

public MapRunningFightDetailsMessage(int fightId, IEnumerable<string> names, IEnumerable<short> levels, sbyte teamSwap, IEnumerable<bool> alives)
        {
            this.fightId = fightId;
            this.names = names;
            this.levels = levels;
            this.teamSwap = teamSwap;
            this.alives = alives;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteInt(fightId);
            var names_before = writer.Position;
            var names_count = 0;
            writer.WriteUShort(0);
            foreach (var entry in names)
            {
                 writer.WriteUTF(entry);
                 names_count++;
            }
            var names_after = writer.Position;
            writer.Seek((int)names_before);
            writer.WriteUShort((ushort)names_count);
            writer.Seek((int)names_after);

            var levels_before = writer.Position;
            var levels_count = 0;
            writer.WriteUShort(0);
            foreach (var entry in levels)
            {
                 writer.WriteShort(entry);
                 levels_count++;
            }
            var levels_after = writer.Position;
            writer.Seek((int)levels_before);
            writer.WriteUShort((ushort)levels_count);
            writer.Seek((int)levels_after);

            writer.WriteSByte(teamSwap);
            var alives_before = writer.Position;
            var alives_count = 0;
            writer.WriteUShort(0);
            foreach (var entry in alives)
            {
                 writer.WriteBoolean(entry);
                 alives_count++;
            }
            var alives_after = writer.Position;
            writer.Seek((int)alives_before);
            writer.WriteUShort((ushort)alives_count);
            writer.Seek((int)alives_after);

            

}

public override void Deserialize(IDataReader reader)
{

fightId = reader.ReadInt();
            if (fightId < 0)
                throw new Exception("Forbidden value on fightId = " + fightId + ", it doesn't respect the following condition : fightId < 0");
            var limit = reader.ReadUShort();
            var names_ = new string[limit];
            for (int i = 0; i < limit; i++)
            {
                 names_[i] = reader.ReadUTF();
            }
            names = names_;
            limit = reader.ReadUShort();
            var levels_ = new short[limit];
            for (int i = 0; i < limit; i++)
            {
                 levels_[i] = reader.ReadShort();
            }
            levels = levels_;
            teamSwap = reader.ReadSByte();
            if (teamSwap < 0)
                throw new Exception("Forbidden value on teamSwap = " + teamSwap + ", it doesn't respect the following condition : teamSwap < 0");
            limit = reader.ReadUShort();
            var alives_ = new bool[limit];
            for (int i = 0; i < limit; i++)
            {
                 alives_[i] = reader.ReadBoolean();
            }
            alives = alives_;
            

}


}


}
