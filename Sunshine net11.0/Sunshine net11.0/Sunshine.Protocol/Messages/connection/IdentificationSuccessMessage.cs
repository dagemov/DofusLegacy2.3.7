
















// Generated on 10/13/2017 02:18:37
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class IdentificationSuccessMessage : Message
{

public const uint Id = 22;
public override uint MessageId
{
    get { return Id; }
}

public bool hasRights;
        public bool wasAlreadyConnected;
        public string nickname;
        public int accountId;
        public sbyte communityId;
        public string secretQuestion;
        public double remainingSubscriptionTime;
        

public IdentificationSuccessMessage()
{
}

public IdentificationSuccessMessage(bool hasRights, bool wasAlreadyConnected, string nickname, int accountId, sbyte communityId, string secretQuestion, double remainingSubscriptionTime)
        {
            this.hasRights = hasRights;
            this.wasAlreadyConnected = wasAlreadyConnected;
            this.nickname = nickname;
            this.accountId = accountId;
            this.communityId = communityId;
            this.secretQuestion = secretQuestion;
            this.remainingSubscriptionTime = remainingSubscriptionTime;
        }
        

public override void Serialize(IDataWriter writer)
{

byte flag1 = 0;
            flag1 = BooleanByteWrapper.SetFlag(flag1, 0, hasRights);
            flag1 = BooleanByteWrapper.SetFlag(flag1, 1, wasAlreadyConnected);
            writer.WriteByte(flag1);
            writer.WriteUTF(nickname);
            writer.WriteInt(accountId);
            writer.WriteSByte(communityId);
            writer.WriteUTF(secretQuestion);
            writer.WriteDouble(remainingSubscriptionTime);
            

}

public override void Deserialize(IDataReader reader)
{

byte flag1 = reader.ReadByte();
            hasRights = BooleanByteWrapper.GetFlag(flag1, 0);
            wasAlreadyConnected = BooleanByteWrapper.GetFlag(flag1, 1);
            nickname = reader.ReadUTF();
            accountId = reader.ReadInt();
            if (accountId < 0)
                throw new Exception("Forbidden value on accountId = " + accountId + ", it doesn't respect the following condition : accountId < 0");
            communityId = reader.ReadSByte();
            if (communityId < 0)
                throw new Exception("Forbidden value on communityId = " + communityId + ", it doesn't respect the following condition : communityId < 0");
            secretQuestion = reader.ReadUTF();
            remainingSubscriptionTime = reader.ReadDouble();
            if (remainingSubscriptionTime < 0)
                throw new Exception("Forbidden value on remainingSubscriptionTime = " + remainingSubscriptionTime + ", it doesn't respect the following condition : remainingSubscriptionTime < 0");
            

}


}


}
