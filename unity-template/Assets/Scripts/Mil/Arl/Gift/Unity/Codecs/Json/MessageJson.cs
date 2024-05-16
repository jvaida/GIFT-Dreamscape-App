using System;
using System.Text;
using Mil.Arl.Gift.Unity.Messaging;

public class MessageJson : JsonCodec
{
    private const string TYPE_KEY = "type";
    private const string PAYLOAD_KEY = "payload";

    public override object Deserialize(JSONObject jObj)
    {
        //Ensures that the expected fields are present within the JSONObject
        CheckForExpectedFields(jObj, TYPE_KEY, PAYLOAD_KEY);
        
        //Deserialize the fields
        MessageType messageType = (MessageType) ParseFieldAsEnum(jObj, TYPE_KEY, typeof(MessageType));
        string payload = ParseFieldAsString(jObj, PAYLOAD_KEY);
        string unescapedPayload = UnescapeJsonString(payload);

        return new Message(messageType, unescapedPayload);
    }

    public override string Serialize(object obj)
    {
        Message message = (Message) obj;
        string payload = EscapeJsonString(message.Payload);
        
        JSONObject jObj = new JSONObject();
        jObj.AddField(TYPE_KEY, message.Type.ToString());
        jObj.AddField(PAYLOAD_KEY, payload);
        return jObj.Print();
    }

    /// <summary>
    /// Escapes the literals that would otherwise be parsed as significant JSON
    /// characters. Example { "foo": "bar\subbar" } -> { \"foo\": \"bar\\subbar\" }
    /// </summary>
    /// <param name="json">The string to convert</param>
    /// <returns>The converted string</returns>
    private string EscapeJsonString(string json)
    {
        StringBuilder sb = new StringBuilder();
        for(int i = 0; i < json.Length; i++)
        {
            if(json[i] == '\"' || json[i] == '\\')
                sb.Append('\\');
            sb.Append(json[i]);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Unescape the escape characters within a string. Converting them back to 
    /// their literal form. Example { \"foo\": \"bar\\subbar\" } -> { "foo": "bar\subbar" }
    /// </summary>
    /// <param name="json">The string to convert</param>
    /// <returns>The converted string</returns>
    private string UnescapeJsonString(string json)
    {
        StringBuilder sb = new StringBuilder();
        for(int i = 0; i < json.Length; i++)
        {
            if(json[i] == '\\')
                sb.Append(json[++i]);
            else
                sb.Append(json[i]);
        }

        return sb.ToString();
    }
}