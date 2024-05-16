using System;
using System.Collections.Generic;
using Mil.Arl.Gift.Unity.Messaging;
using Mil.Arl.Gift.Unity.Messaging.Incoming;
using Mil.Arl.Gift.Unity.Messaging.Outgoing;

public static class JsonCodecMapper
{
    public static JsonCodec GetCodec<T>()
    {
        try
        {
            return codecLookup[typeof(T)];
        }

        catch(KeyNotFoundException)
        {
            throw new Exception("There was no JSON codec found for the type " + typeof(T).Name);
        }
    }

    private static Dictionary<Type, JsonCodec> codecLookup = new Dictionary<Type, JsonCodec>
    {
        { typeof(Message), new MessageJson() },
        { typeof(Siman), new SimanJson() },
        { typeof(SimanType), new SimanTypeJson() },
        { typeof(SingleStringPayload), new SingleStringPayloadJson() },
        { typeof(StopFreeze), new StopFreezeJson() },
        { typeof(SimpleExampleState), new SimpleExampleStateJson() }
    };
}