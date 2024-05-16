/*
*   File Name: AbstractGiftConnector.cs
*
*   Classification:  Unclassified
*
*   Prime Contract No.: W911QX13C0027
*
*   This work was generated under U.S. Government contract and the
*   Government has unlimited data rights therein.
*
*   Copyrights:      Copyright 2017
*                    Dignitas Technologies, LLC.
*                    All rights reserved.
*
*   Distribution Statement A: Approved for public release; distribution is unlimited
*
*   Organizations:   Dignitas Technologies, LLC.
*                    3504 Lake Lynda Drive, Suite 170
*                    Orlando, FL 32817
*
*/

using System;
using System.Text;
using System.Threading;
using Mil.Arl.Gift.Unity.Messaging;
using Mil.Arl.Gift.Unity.Messaging.Incoming;
using Mil.Arl.Gift.Unity.Messaging.Outgoing;
using UnityEngine;

namespace Mil.Arl.Gift.Unity.Connectivity
{
    /// <summary>
    /// Represents a platform-agnostic point of connection to GIFT. Provides 
    /// events that represents incoming messages as well as methods for sending
    /// common messages back to GIFT. Derived classes implement a way to communicate
    /// messages to GIFT as well as a way to serialize and deserialize messages to JSON
    /// </summary>
    public abstract class AbstractGiftConnector
    {
        /// <summary>
        /// The enumeration for all the states a GIFT Training application can be in.
        /// Be very careful when changing the contents of this enum. Be sure to check
        /// <see cref="IsValidStateTransition" />
        /// </summary>
        public enum GiftConnectorState
        {
            Error,
            PreHandshake,
            PreLoad,
            Loading,
            Loaded,
            Starting,
            Started,
            Pausing,
            Paused,
            Resuming,
            Stopping,
            Stopped
        }

        /// <summary>
        /// Constructs the connector and sends a handshake message
        /// to GIFT to let it know that the Application Author's code
        /// is now runnable
        /// </summary>
        public AbstractGiftConnector()
        {
            SendHandshake();
        }

        /// <summary>
        /// Deserializes a message arriving from GIFT and raises the appropriate event
        /// </summary>
        /// <param name="messageText">The message serialized as JSON</param>
        public void PostIncomingMessage(string messageText)
        {
            //Deserializes the message
            Message message;
            try
            {
                message = Deserialize<Message>(messageText);
            } 
            
            catch(Exception e)
            {
                throw new Exception("There was a problem deserializing the message " + messageText, e);
            }

            //Determines the type of the message's payload and raises the appropriate event
            switch (message.Type)
            {
                case MessageType.Siman:
                    Siman siman;
                    //Deserializes the Siman message and sends an error to the Tutor client if there is an issue
                    try
                    {
                        siman = Deserialize<Siman>(message.Payload);
                    }

                    catch(Exception ex) 
                    {
                        string description = "There was a problem deserializing the message's payload " 
                            + message.Payload 
                            + " as a Siman.";
                        CreateAndSendErrorMessage(description, ex);
                        return;
                    }

                    //Handles the Siman message and sends an error to the Tutor client if there is an issue
                    try
                    {
                        switch(siman.SimanType)
                        {
                            case SimanType.Load:
                                Status = GiftConnectorState.Loading;
                                break;
                            case SimanType.Start:
                                Status = GiftConnectorState.Starting;
                                break;
                            case SimanType.Pause:
                                Status = GiftConnectorState.Pausing;
                                break;
                            case SimanType.Resume:
                                Status = GiftConnectorState.Resuming;
                                break;
                            case SimanType.Stop:
                                Status = GiftConnectorState.Stopping;
                                break;
                        }
                        var simanHandler = OnSimanReceived;
                        if (simanHandler != null)
                            simanHandler(siman);
                    }

                    catch(Exception ex)
                    {
                        CreateAndSendErrorMessage("There was a problem handling the Siman message " + siman.ToString(), ex);
                        return;
                    }
                    break;
                case MessageType.Feedback:
                    //Deserializes the feedback message and sends an error to the Tutor client if there is an issue
                    SingleStringPayload stringPayload;
                    try
                    {
                        stringPayload = Deserialize<SingleStringPayload>(message.Payload);
                    }
                    
                    catch(Exception ex)
                    {
                        string description = "There was a problem deserializing the message's payload " 
                            + message.Payload 
                            + " as a SingleStringPayload.";
                        CreateAndSendErrorMessage(description, ex);
                        return;
                    }

                    //Handles the feedback message and sends an error to the Tutor client if there is an issue
                    try
                    {
                        var feedbackHandler = OnFeedbackReceived;
                        if (feedbackHandler != null)
                            feedbackHandler(stringPayload.StringPayload);
                    }

                    catch(Exception ex)
                    {
                        string description = "There was a problem handling the feedback message " 
                            + stringPayload.StringPayload;
                        CreateAndSendErrorMessage(description, ex);
                        return;
                    }
                    break;
                default:
                    try
                    {
                        var messageHandler = OnMessageReceived;
                        if (messageHandler != null)
                            messageHandler(message);
                    }

                    catch(Exception ex)
                    {
                        string description = "There was a problem handling the GIFT message " 
                            + messageText;
                        CreateAndSendErrorMessage(description, ex);
                    }
                    break;
            }
        }

        /// <summary>
        /// Evaluates whether or not there are any
        /// subscribers to the OnSimanReceived event.
        /// </summary>
        /// <returns></returns>
        public bool HasSimanHandler()
        {
            return OnSimanReceived != null;
        }

        /// <summary>
        /// Used to send a loaded message once the application is ready to start.
        /// The application should be ready to start before this message is sent.
        /// The user should not yet be able to interact with the application until
        /// the start message is received.
        /// </summary>
        public void SendLoadedMessage()
        {
            Status = GiftConnectorState.Loaded;
            SendMessageToGift(MessageType.SimanResponse, SimanType.Load);
        }

        /// <summary>
        /// Used to send a started message once the application has started.
        /// Only use this once the user can begin to interact with the application
        /// so that GIFT's timekeeping for the simulation is accurate.
        /// </summary>
        public void SendStartedMessage()
        {
            Status = GiftConnectorState.Started;
            SendMessageToGift(MessageType.SimanResponse, SimanType.Start);
        }

        /// <summary>
        /// Used to send a stopped message once the application successfully stopped.
        /// Should not be called until the application has successfully cleaned itself
        /// up since GIFT will be able to force quit the application once this message
        /// is received. 
        /// </summary>
        public void SendStoppedMessage()
        {
            Status = GiftConnectorState.Stopped;
            SendMessageToGift(MessageType.SimanResponse, SimanType.Stop);
        }

        /// <summary>
        /// Used to send a paused message once the application successfully paused.
        /// </summary>
        public void SendPausedMessage()
        {
            Status = GiftConnectorState.Paused;
            SendMessageToGift(MessageType.SimanResponse, SimanType.Pause);
        }

        /// <summary>
        /// Used to send a resume message once the application successfully resumed.
        /// </summary>
        public void SendResumedMessage()
        {
            Status = GiftConnectorState.Started;
            SendMessageToGift(MessageType.SimanResponse, SimanType.Resume);
        }

        /// <summary>
        /// Sends a message that informs GIFT the application's scenario has ended.
        /// </summary>
        public void SendFinishedMessage()
        {
            SendMessageToGift(MessageType.StopFreeze, new StopFreeze());
        }

        /// <summary>
        /// Sends a simple example state message to GIFT
        /// </summary>
        /// <param name="var">The string value to use within the message</param>
        [Obsolete("Use MessageFactory.CreateSimpleExampleState(string) with AbstractGiftConnector.SendMessageToGift(Message) instead")]
        public void SendSimpleExampleState(string var)
        {
            SendMessageToGift(MessageType.SimpleExampleState, new SimpleExampleState(var));
        }

        /// <summary>
        /// Sends a message to GIFT with the specified type as well as a serialized 
        /// payload
        /// </summary>
        /// <param name="type">The type of the message. Provides a hint to the receiver of the message 
        /// on how to deserialize the payload </param>
        /// <param name="payload">The object to serialize as the payload of the message. Should be a 
        /// type found in the Mil.Arl.Gift.Unity.Messaging</param>
        public void SendMessageToGift<T>(MessageType type, T payload)
        {
            try
            {
                string payloadString = Serialize(payload);
                SendMessageToGift(type, payloadString);
            }

            catch(Exception e)
            {
                throw new Exception("There was a problem serializing the payload " + payload, e);
            }
        }

        /// <summary>
        /// Sends a message to GIFT with the given raw string and 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="payload"></param>
        public void SendMessageToGift(MessageType type, string payload)
        {
            SendMessageToGift(new Message(type, payload));
        }

        /// <summary>
        /// Sends the message to GIFT over the derived connector's
        /// chosen form of communication.
        /// </summary>
        /// <param name="msg">The message to send to GIFT</param>
        public void SendMessageToGift(Message msg)
        {
            string msgString;
            try 
            {
                msgString = Serialize(msg);
            }

            catch(Exception e)
            {
                throw new Exception("There was a problem serializing the message " + msg, e);
            }

            SendMessageToGift(msgString);
        }

        /// <summary>
        /// Sends an error message to GIFT that will prematurely end the course.
        /// Use if the training application is in a state that cannot be recovered from.
        /// </summary>
        /// <param name="str">The message to place into the details of the error dialog. 
        /// Include any information that will help with debugging the issue.</param>
        public void SendErrorToGift(string str)
        {
            SendMessageToGift("!" + str);
        }

        /// <summary>
        /// The status of the connector/training application.
        /// Used in order to ensure the proper lifecycle messages
        /// are being sent at the appropriate times.
        /// </summary>
        /// <returns>The current state of the connector/application</returns>
        public GiftConnectorState Status 
        { 
            get { return status; } 
            protected set 
            {
                //Check to make sure that the transition is valid
                if(!IsValidStateTransition(status, value))
                {
                    string errorMsg = "An AbstractGiftConnector cannot transition from " + status + " to " + value + ".";
                    status = GiftConnectorState.Error;
                    throw new InvalidOperationException(errorMsg);
                }

                status = value; 
            }
        }

        /// <summary>
        /// To be overridden by a derived connector. Should send the string
        /// to GIFT to via some form of communication (e.g. window.postMessage()).
        /// </summary>
        /// <param name="message">The string to send to GIFT</param>
        protected abstract void SendMessageToGift(string message);

        /// <summary>
        /// A method that converts a JSON string into a given type.
        /// Should be overridden by a derived connector so that whatever
        /// JSON Serialization library is available can be used.
        /// </summary>
        /// <param name="json">The JSON string to deserialize</param>
        /// <returns>The deserialized object of the type that is supplied via the 
        /// generic method's type parameter.</returns>
        protected abstract T Deserialize<T>(string json);

        /// <summary>
        /// A method that converts a .NET object into a JSON string.
        /// Should be overridden by a derived connector so that whatever 
        /// JSON Serialization library is available can be used.
        /// </summary>
        /// <param name="obj">The object to serialize into JSON</param>
        /// <returns>The object represented as a JSON string</returns>
        protected abstract string Serialize<T>(T obj);

        /// <summary>
        /// Used to send a handshake message once the application is ready to receive
        /// messages from the Gift Tutor. Should be sent as soon as the AbstractGiftConnector
        /// is listening for messages from GIFT.
        /// </summary>
        protected void SendHandshake()
        {
            Status = GiftConnectorState.PreLoad;
            SendMessageToGift("");
        }

        /// <summary>
        /// The event that is raised when the connector 
        /// receives a Siman message from GIFT
        /// </summary>
        public event Action<Siman> OnSimanReceived;

        /// <summary>
        /// The event that is raised when the connector
        /// receives a message from GIFT that is not currently
        /// supported by the API.
        /// </summary>
        public event Action<Message> OnMessageReceived;

        /// <summary>
        /// The event that is raised when the connector 
        /// receives a request to display feedback in the 
        /// training application from GIFT.
        /// </summary>
        public event Action<string> OnFeedbackReceived;

        /// <summary>
        /// The backing field for the Status property
        /// </summary>
        private GiftConnectorState status = GiftConnectorState.PreHandshake;

        /// <summary>
        /// Creates an error message from a custom description and the details of the
        /// exception that caused the error. After creating the error message it sends
        /// the message to GIFT
        /// </summary>
        /// <param name="description">The custom description of the error. Should give context as to what action caused the Exception to be thrown.</param>
        /// <param name="ex">The unrecoverable exception that was thrown.</param>
        private void CreateAndSendErrorMessage(string description, Exception ex)
        {
            Debug.Log("CreateAndSendErrorMessage ");
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(description);

            int tabCount = 0;
            while(ex != null)
            {
                //Tab appropriately
                for(int i = tabCount++; i > 0; i--)
                    sb.Append("   ");
                
                //Write the message
                sb.AppendLine(ex.Message);

                //Move to the inner exception
                ex = ex.InnerException;
            }
            
            SendErrorToGift(sb.ToString());
        }

        private bool IsValidStateTransition(GiftConnectorState from, GiftConnectorState to)
        {
            // A connector can never transition from an Error state
            if(from == GiftConnectorState.Error)
                return false;

            // At any time the connector can transition to an error state or stopping state
            if(to == GiftConnectorState.Error || to == GiftConnectorState.Stopping)
                return true;

            // The connector that transition from Resuming to Started
            if(from == GiftConnectorState.Resuming && to == GiftConnectorState.Started)
                return true;

            // All other valid cases can be described as moving to the next element in the enum
            int fromValue = (int) from;
            int toValue = (int) to;

            return fromValue == toValue - 1;
        }
    }
}