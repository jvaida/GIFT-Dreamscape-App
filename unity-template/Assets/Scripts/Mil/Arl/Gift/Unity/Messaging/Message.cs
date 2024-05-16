/*
*   File Name: Message.cs
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
using System.Runtime.Serialization;
using UnityEngine;

namespace Mil.Arl.Gift.Unity.Messaging
{
    /// <summary>
    /// Defines the type of the message. Tells the receiver
    /// of the message how to interpret the payload and what 
    /// to do with the message.
    /// </summary>
    public enum MessageType
    {
        Siman, 
        Feedback,
        StopFreeze,
        SimpleExampleState,
        GenericJSONState,
        SimanResponse
    }

    /// <summary>
    /// The class that represents a message to or from GIFT.
    /// An instance of Message is serialized as a JSON string
    /// to be between GIFT and the embedded training application 
    /// who then deserializes Message and eventually deserializes 
    /// payload as a specific type of object.
    /// </summary>
    [Serializable]
    public class Message
    {
        public Message(MessageType type, string payload)
        {
            Type = type;
            Payload = payload;
        }

        /// <summary>
        /// The type of the sibling payload field.
        /// Used as a hint on how to deserialize the payload
        /// for the receiver of this message. Should be 
        /// assigned one of the public const strings of the 
        /// Message class.
        /// </summary>
        #pragma warning disable 0618
        public MessageType Type
        {
            get { return type; }
            set { type = value; }
        }
        #pragma warning restore 0618

        /// <summary>
        /// The message content itself as a JSON string.
        /// Uses the above field 'type' as a hint on what 
        /// object to deserialize the object as.
        /// </summary>
        /// <returns>The payload as a JSON string. Not null or empty</returns>
        #pragma warning disable 0618
        public string Payload
        {
            get { return payload; }
            set 
            { 
                if(!string.IsNullOrEmpty(value))
                    payload = value;
                else
                    throw new InvalidOperationException("The value of the 'Payload' property can not be null or empty");
            }
        }
        #pragma warning restore 0618

        /// <summary>
        /// The type of the sibling payload field.
        /// Used as a hint on how to deserialize the payload
        /// for the receiver of this message. Should be 
        /// assigned one of the public const strings of the 
        /// Message class.
        /// </summary>
        [Obsolete("Use the 'Type' property instead")]
        public MessageType type;

        /// <summary>
        /// The message content itself as a JSON string.
        /// Uses the above field 'type' as a hint on what 
        /// object to deserialize the object as.
        /// </summary>
        [Obsolete("Use the 'Payload' property instead")]
        public string payload;

        /// <summary>
        /// Converts the Message object to a JSON string to be used for 
        /// debugging purposes, NOT for transmission. For sending the Message
        /// as a JSON string use the Serialize method in an available 
        /// AbstractGiftConnector
        /// </summary>
        /// <returns>A JSON string representing the instance</returns>
        public override string ToString()
        {
            return string.Format("{{ \"{0}\": \"{1}\", \"{2}\": \"{3}\" }}", 
                "Type", 
                Type.ToString(), 
                "Payload", 
                Payload);
        }
    }
}