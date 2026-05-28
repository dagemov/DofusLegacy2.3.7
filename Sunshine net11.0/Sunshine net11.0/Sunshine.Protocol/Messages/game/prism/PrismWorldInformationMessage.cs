
















// Generated on 10/13/2017 02:19:06
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class PrismWorldInformationMessage : Message
{

public const uint Id = 5854;
public override uint MessageId
{
    get { return Id; }
}

public int nbSubOwned;
        public int subTotal;
        public int maxSub;
        public IEnumerable<Types.PrismSubAreaInformation> subAreasInformation;
        public int nbConqsOwned;
        public int conqsTotal;
        public IEnumerable<Types.PrismConquestInformation> conquetesInformation;
        

public PrismWorldInformationMessage()
{
}

public PrismWorldInformationMessage(int nbSubOwned, int subTotal, int maxSub, IEnumerable<Types.PrismSubAreaInformation> subAreasInformation, int nbConqsOwned, int conqsTotal, IEnumerable<Types.PrismConquestInformation> conquetesInformation)
        {
            this.nbSubOwned = nbSubOwned;
            this.subTotal = subTotal;
            this.maxSub = maxSub;
            this.subAreasInformation = subAreasInformation;
            this.nbConqsOwned = nbConqsOwned;
            this.conqsTotal = conqsTotal;
            this.conquetesInformation = conquetesInformation;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteInt(nbSubOwned);
            writer.WriteInt(subTotal);
            writer.WriteInt(maxSub);
            var subAreasInformation_before = writer.Position;
            var subAreasInformation_count = 0;
            writer.WriteUShort(0);
            foreach (var entry in subAreasInformation)
            {
                 entry.Serialize(writer);
                 subAreasInformation_count++;
            }
            var subAreasInformation_after = writer.Position;
            writer.Seek((int)subAreasInformation_before);
            writer.WriteUShort((ushort)subAreasInformation_count);
            writer.Seek((int)subAreasInformation_after);

            writer.WriteInt(nbConqsOwned);
            writer.WriteInt(conqsTotal);
            var conquetesInformation_before = writer.Position;
            var conquetesInformation_count = 0;
            writer.WriteUShort(0);
            foreach (var entry in conquetesInformation)
            {
                 entry.Serialize(writer);
                 conquetesInformation_count++;
            }
            var conquetesInformation_after = writer.Position;
            writer.Seek((int)conquetesInformation_before);
            writer.WriteUShort((ushort)conquetesInformation_count);
            writer.Seek((int)conquetesInformation_after);

            

}

public override void Deserialize(IDataReader reader)
{

nbSubOwned = reader.ReadInt();
            if (nbSubOwned < 0)
                throw new Exception("Forbidden value on nbSubOwned = " + nbSubOwned + ", it doesn't respect the following condition : nbSubOwned < 0");
            subTotal = reader.ReadInt();
            if (subTotal < 0)
                throw new Exception("Forbidden value on subTotal = " + subTotal + ", it doesn't respect the following condition : subTotal < 0");
            maxSub = reader.ReadInt();
            if (maxSub < 0)
                throw new Exception("Forbidden value on maxSub = " + maxSub + ", it doesn't respect the following condition : maxSub < 0");
            var limit = reader.ReadUShort();
            var subAreasInformation_ = new Types.PrismSubAreaInformation[limit];
            for (int i = 0; i < limit; i++)
            {
                 subAreasInformation_[i] = new Types.PrismSubAreaInformation();
                 subAreasInformation_[i].Deserialize(reader);
            }
            subAreasInformation = subAreasInformation_;
            nbConqsOwned = reader.ReadInt();
            if (nbConqsOwned < 0)
                throw new Exception("Forbidden value on nbConqsOwned = " + nbConqsOwned + ", it doesn't respect the following condition : nbConqsOwned < 0");
            conqsTotal = reader.ReadInt();
            if (conqsTotal < 0)
                throw new Exception("Forbidden value on conqsTotal = " + conqsTotal + ", it doesn't respect the following condition : conqsTotal < 0");
            limit = reader.ReadUShort();
            var conquetesInformation_ = new Types.PrismConquestInformation[limit];
            for (int i = 0; i < limit; i++)
            {
                 conquetesInformation_[i] = new Types.PrismConquestInformation();
                 conquetesInformation_[i].Deserialize(reader);
            }
            conquetesInformation = conquetesInformation_;
            

}


}


}
