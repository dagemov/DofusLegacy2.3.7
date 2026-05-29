
















// Generated on 10/13/2017 02:17:27
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;

namespace Sunshine.Protocol.Types
{

public class ShortcutObject : Shortcut
{

public const short Id = 367;
public override short TypeId
{
    get { return Id; }
}



public ShortcutObject()
{
}

public ShortcutObject(int slot)
         : base(slot)
        {
        }
        

public override void Serialize(IDataWriter writer)
{

base.Serialize(writer);
            

}

public override void Deserialize(IDataReader reader)
{

base.Deserialize(reader);
            

}



}


}
