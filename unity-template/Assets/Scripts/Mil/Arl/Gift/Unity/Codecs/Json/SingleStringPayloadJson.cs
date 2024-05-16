using System;
using Mil.Arl.Gift.Unity.Messaging.Incoming;

public class SingleStringPayloadJson : JsonCodec
{
    private const string STRING_PAYLOAD_VALUE = "StringPayload";

    public override object Deserialize(JSONObject jObj)
    {
        CheckForExpectedFields(jObj, STRING_PAYLOAD_VALUE);
        string stringValue = ParseFieldAsString(jObj, STRING_PAYLOAD_VALUE);
        return new SingleStringPayload(stringValue);
    }

    public override string Serialize(object obj)
    {
        throw new NotImplementedException();
    }
}