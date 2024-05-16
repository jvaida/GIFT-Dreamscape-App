/*
*   File Name: StringPayload.cs
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

namespace Mil.Arl.Gift.Unity.Messaging.Incoming
{
    /// <summary>
    /// Wrapper around a single string. Used only for 
    /// deserializing a feedback request's feedback string. 
    /// Only used by payloads that come from GIFT. Never used
    /// by payloads that are being sent to GIFT.
    /// </summary>
    [Serializable]
    public class SingleStringPayload
    {
        /// <summary>
        /// Initializes a SingleStringPayload's string
        /// field with the given value
        /// </summary>
        /// <param name="var">The value to initialize the string field with</param>
        public SingleStringPayload(string var)
        {
            StringPayload = var;
        }
        
        /// <summary>
        /// The string value
        /// </summary>
        public string StringPayload { get; set; }

        /// <summary>
        /// Converts the StringPayload to a JSON string to be used for 
        /// debugging purposes, NOT for transmission. For sending the payload
        /// as a JSON string use the Serialize method in an available 
        /// AbstractGiftConnector
        /// </summary>
        /// <returns>A JSON string representing the instance</returns>
        public override string ToString()
        {
            return String.Format("{{ \"{0}\": \"{1}\" }}", "StringPayload", StringPayload);
        }
    }
}