using Mil.Arl.Gift.Unity.Messaging.Outgoing;

namespace Mil.Arl.Gift.Unity.Messaging
{
    public static class MessageFactory
    {
        public static Message CreateSimpleExampleState(string value)
        {
            JsonCodec codec = new SimpleExampleStateJson();
            string payloadString = codec.Serialize(new SimpleExampleState(value));
            
            return new Message(MessageType.SimpleExampleState, payloadString);
        }
    }
}