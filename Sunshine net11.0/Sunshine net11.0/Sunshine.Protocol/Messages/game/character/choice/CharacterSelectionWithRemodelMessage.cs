using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types.Custom;

namespace Sunshine.Protocol.Messages
{
    public class CharacterSelectionWithRemodelMessage : CharacterSelectionMessage
    {
        public const uint Id = 6549;
        public override uint MessageId => Id;

        public RemodelingInformation remodel;

        public CharacterSelectionWithRemodelMessage()
        {
        }

        public CharacterSelectionWithRemodelMessage(int id, RemodelingInformation remodel)
            : base(id)
        {
            this.remodel = remodel;
        }

        public override void Serialize(IDataWriter writer)
        {
            base.Serialize(writer);
            if (remodel != null)
                remodel.Serialize(writer);
        }

        public override void Deserialize(IDataReader reader)
        {
            base.Deserialize(reader);
            remodel = new RemodelingInformation();
            remodel.Deserialize(reader);
        }
    }
}
