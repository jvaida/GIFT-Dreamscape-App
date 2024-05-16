using System;
using Mil.Arl.Gift.Unity.Messaging.Incoming;

public class SimanTypeJson : JsonCodec
{
    public override object Deserialize(JSONObject jObj)
    {
        throw new NotImplementedException();
    }

    public override string Serialize(object obj)
    {
        SimanType simanType = (SimanType) obj;
        return simanType.ToString();
    }
}