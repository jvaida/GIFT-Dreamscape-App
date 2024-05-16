/*
*   File Name: SimpleExampleState.cs
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
    /// Class that represents a SimpleExampleState message.
    /// Used to serialize to and deserialize from the JSON
    /// format that GIFT expects
    /// </summary>
    [Serializable]
    public class SimpleExampleState
    {
        /// <summary>
        /// Constructs a SimpleExampleState and initializes
        /// its VAR payload to a given value
        /// </summary>
        /// <param name="var">The value to initialize VAR to</param>
        public SimpleExampleState(string var)
        {
            Var = var;
        }

        /// <summary>
        /// The string value of the Simple Example state
        /// </summary>
        /// <returns>The string value of the Simple Example state</returns>
        #pragma warning disable 0618
        public string Var
        {
            get { return VAR; }
            set { VAR = value; }
        }
        #pragma warning restore 0618

        [Obsolete("Use the Var property instead")]
        public string VAR;

        /// <summary>
        /// Converts the SimpleExampleState payload to a JSON string to be used for 
        /// debugging purposes, NOT for transmission. For sending the payload
        /// as a JSON string use the Serialize method in an available 
        /// AbstractGiftConnector
        /// </summary>
        /// <returns>A JSON string representing the instance</returns>
        public override string ToString()
        {
            return String.Format("{{ \"{0}\": \"{1}\" }}", "Var", Var);
        }
    }
}