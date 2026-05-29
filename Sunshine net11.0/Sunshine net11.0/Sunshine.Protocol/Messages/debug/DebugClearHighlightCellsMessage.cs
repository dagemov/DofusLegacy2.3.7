














// Restored for legacy protocol compatibility.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class DebugClearHighlightCellsMessage : Message
{

public const uint Id = 2002;
public override uint MessageId
{
    get { return Id; }
}

public IEnumerable<short> cells;


public DebugClearHighlightCellsMessage()
{
}

public DebugClearHighlightCellsMessage(IEnumerable<short> cells)
        {
            this.cells = cells;
        }


public override void Serialize(IDataWriter writer)
{

var cells_before = writer.Position;
            var cells_count = 0;
            writer.WriteUShort(0);
            foreach (var entry in cells)
            {
                 writer.WriteShort(entry);
                 cells_count++;
            }
            var cells_after = writer.Position;
            writer.Seek((int)cells_before);
            writer.WriteUShort((ushort)cells_count);
            writer.Seek((int)cells_after);


}

public override void Deserialize(IDataReader reader)
{

var limit = reader.ReadUShort();
            var cells_ = new short[limit];
            for (int i = 0; i < limit; i++)
            {
                 cells_[i] = reader.ReadShort();
                 if (cells_[i] < 0 || cells_[i] > 559)
                     throw new Exception("Forbidden value on cells_[" + i + "] = " + cells_[i] + ", it doesn't respect the following condition : cells_[i] < 0 || cells_[i] > 559");
            }
            cells = cells_;


}


}


}
