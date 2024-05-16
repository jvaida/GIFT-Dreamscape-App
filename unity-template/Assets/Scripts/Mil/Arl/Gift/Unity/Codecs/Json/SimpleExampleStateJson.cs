using System;
using Mil.Arl.Gift.Unity.Messaging.Outgoing;

public class SimpleExampleStateJson : JsonCodec
{
    private const string VALUE_KEY = "VAR";

    public override object Deserialize(JSONObject jObj)
    {
        CheckForExpectedFields(jObj, VALUE_KEY);
        string value = ParseFieldAsString(jObj, VALUE_KEY);
        return new SimpleExampleState(value);
    }

    public override string Serialize(object obj)
    {
        SimpleExampleState simpleExampleState = (SimpleExampleState) obj;
        JSONObject jObj = new JSONObject();

        jObj.AddField(VALUE_KEY, simpleExampleState.Var);

        return jObj.Print();
    }
}