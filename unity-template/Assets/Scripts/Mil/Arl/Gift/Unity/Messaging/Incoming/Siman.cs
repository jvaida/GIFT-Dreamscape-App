/*
*   File Name: Siman.cs
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
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Mil.Arl.Gift.Unity.Messaging.Incoming
{
    /// <summary>
    /// Possible types of a Siman message
    /// </summary>
    public enum SimanType
    {
        Load,
        Start,
        Stop,
        Pause,
        Resume, 
        Restart
    }

    /// <summary>
    /// Possible types of a Siman messages route. Messages
    /// consumed by an embedded application should all have 
    /// an embedded route type.
    /// </summary>
    public enum RouteType
    {
        Embedded,
        Interop
    }

    /// <summary>
    /// Represents a Siman message from Unity.
    /// </summary>
    [Serializable]
    public class Siman
    {
        /// <summary>
        /// The type of Siman message that this object represents.
        /// One of the public const strings in the Siman class should
        /// be used for this value.
        /// </summary>
        #pragma warning disable 0618
        public SimanType SimanType
        {
            get { return Siman_Type; }
            set { Siman_Type = value; }
        }
        #pragma warning restore 0618

        /// <summary>
        /// The type of Siman message that this object represents.
        /// One of the public const strings in the Siman class should
        /// be used for this value.
        /// </summary>
        [Obsolete("Use the SimanType property instead")]
        public SimanType Siman_Type;

        /// <summary>
        /// The type of routing the Siman message uses.
        /// Should always be embedded application. Included here
        /// for complete representation of the Siman Message
        /// </summary>
        public RouteType RouteType { get; set; }

        /// <summary>
        /// Key Value pairs that are used as inputs for Siman Load 
        /// messages. Null for all other message types. Populated by 
        /// GIFT before being sent to an embedded application
        /// </summary>
        public Dictionary<string, string> LoadArgs { get; set; }

        /// <summary>
        /// Converts the Siman payload to a JSON string to be used for 
        /// debugging purposes, NOT for transmission. For sending the payload
        /// as a JSON string use the Serialize method in an available 
        /// AbstractGiftConnector
        /// </summary>
        /// <returns>A JSON string representing the instance</returns>
        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("{ ");

            //Converts the simple types to strings
            builder.AppendFormat("\"{0}\": \"{1}\", ", "Siman_Type", SimanType);
            builder.AppendFormat("\"{0}\": \"{1}\", ", "RouteType", RouteType);
            
            //Converts the dictionary to a string
            builder.AppendFormat("\"{0}\": ", "LoadArgs");
            if(LoadArgs != null)
            {
                builder.Append("{ ");
                int i = 0;
                foreach(KeyValuePair<string, string> kv in LoadArgs)
                {
                    if(i++ != 0)
                        builder.Append(", ");
                    builder.AppendFormat("\"{0}\": \"{1}\"", kv.Key, kv.Value);
                }
                builder.Append(" }");
            }

            else
            {
                builder.Append("null");
            }

            builder.Append(" }");
            return builder.ToString();
        }
    }
}