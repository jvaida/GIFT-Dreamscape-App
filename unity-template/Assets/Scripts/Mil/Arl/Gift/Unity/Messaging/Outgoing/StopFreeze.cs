/*
*   File Name: StopFreeze.cs
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
using UnityEngine;

namespace Mil.Arl.Gift.Unity.Messaging.Outgoing
{
    /// <summary>
    /// Class that represents a StopFreeze message.
    /// Used to tell GIFT to stop the scenario and advance to 
    /// the next course object.
    /// </summary>
    [Serializable]
    public class StopFreeze
    {
        /// <summary>
        /// The real-world time (UTC, milliseconds since midnight Jan 1, 1970) at which the entity is to start/resume in the exercise.
        /// </summary>
        #pragma warning disable 0618
        public long RealWorldTime 
        { 
            get { return realWorldTime; }
            set { realWorldTime = value; }
        }
        #pragma warning restore 0618

        /// <summary>
        /// ID for the reason that an entity or exercise was stopped/frozen
        /// </summary>
        #pragma warning disable 0618
        public int Reason 
        { 
            get { return reason; }
            set { reason = value; }
        }
        #pragma warning restore 0618

        /// <summary>
        /// ID for the frozen behavior, that will indicate how the entity or exercise acts while frozen
        /// </summary>
        #pragma warning disable 0618
        public int FrozenBehavior 
        { 
            get { return frozenBehavior; }
            set { frozenBehavior = value; }
        }
        #pragma warning restore 0618

        /// <summary>
        /// Unique ID for this request
        /// </summary>
        #pragma warning disable 0618
        public long RequestID 
        { 
            get { return requestID; }
            set { requestID = value;}
        }
        #pragma warning restore 0618
        
        /// <summary>
        /// The real-world time (UTC, milliseconds since midnight Jan 1, 1970) at which the entity is to start/resume in the exercise.
        /// </summary>
        [Obsolete("Use the RealWorldTime property instead")]
        public long realWorldTime;

        /// <summary>
        /// ID for the reason that an entity or exercise was stopped/frozen
        /// </summary>
        [Obsolete("Use the Reason property instead")]
        public int reason;

        /// <summary>
        /// ID for the frozen behavior, that will indicate how the entity or exercise acts while frozen
        /// </summary>
        [Obsolete("Use the FrozenBehavior property instead")]
        public int frozenBehavior;

        /// <summary>
        /// Unique ID for this request
        /// </summary>
        [Obsolete("Use the RequestID property instead")]
        public long requestID;

        /// <summary>
        /// Converts the StopFreeze payload to a JSON string to be used for 
        /// debugging purposes, NOT for transmission. For sending the payload
        /// as a JSON string use the Serialize method in an available 
        /// AbstractGiftConnector
        /// </summary>
        /// <returns>A JSON string representing the instance</returns>
        public override string ToString()
        {
            return string.Format("{{ \"{0}\": {1}, \"{2}\": {3}, \"{4}\": {5}, \"{6}\": {7} }}",
                "RealWorldTime",
                RealWorldTime,
                "Reason",
                Reason,
                "FrozenBehavior",
                FrozenBehavior,
                "RequestID",
                RequestID);
        }
    }
}