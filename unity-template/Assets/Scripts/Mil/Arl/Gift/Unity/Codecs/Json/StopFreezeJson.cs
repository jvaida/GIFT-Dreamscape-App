using System;
using Mil.Arl.Gift.Unity.Messaging.Outgoing;

public class StopFreezeJson : JsonCodec
{
    private const string REAL_WORLD_TIME_KEY = "realWorldTime";
    private const string REASON_KEY = "reason";
    private const string FROZEN_BEHAVIOR_KEY = "frozenBehavior";
    private const string REQUEST_ID_KEY = "requestID";

    public override object Deserialize(JSONObject jObj)
    {
        CheckForExpectedFields(jObj, 
            REAL_WORLD_TIME_KEY, 
            REASON_KEY, 
            FROZEN_BEHAVIOR_KEY, 
            REQUEST_ID_KEY);

        throw new NotImplementedException();
    }

    public override string Serialize(object obj)
    {
        StopFreeze stopFreeze = (StopFreeze) obj;
        JSONObject jObj = new JSONObject();
        
        jObj.AddField(REAL_WORLD_TIME_KEY, stopFreeze.RealWorldTime);
        jObj.AddField(REASON_KEY, stopFreeze.Reason);
        jObj.AddField(FROZEN_BEHAVIOR_KEY, stopFreeze.FrozenBehavior);
        jObj.AddField(REQUEST_ID_KEY, stopFreeze.RequestID);

        return jObj.Print();
    }
}