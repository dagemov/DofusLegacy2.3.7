
















// Generated on 10/13/2017 02:17:26
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;

namespace Sunshine.Protocol.Types
{

public class AdditionalTaxCollectorInformations
{

public const short Id = 165;
public virtual short TypeId
{
    get { return Id; }
}

public string CollectorCallerName;
        public int date;
        

public AdditionalTaxCollectorInformations()
{
}

public AdditionalTaxCollectorInformations(string CollectorCallerName, int date)
        {
            this.CollectorCallerName = CollectorCallerName;
            this.date = date;
        }
        

public virtual void Serialize(IDataWriter writer)
{

writer.WriteUTF(CollectorCallerName);
            writer.WriteInt(date);
            

}

public virtual void Deserialize(IDataReader reader)
{

CollectorCallerName = reader.ReadUTF();
            date = reader.ReadInt();
            if (date < 0)
                throw new Exception("Forbidden value on date = " + date + ", it doesn't respect the following condition : date < 0");
            

}



}


}
