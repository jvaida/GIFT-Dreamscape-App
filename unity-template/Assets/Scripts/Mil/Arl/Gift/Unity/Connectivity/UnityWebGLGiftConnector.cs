/*
*   File Name: UnityWebGLGiftConnector.cs
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
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using Mil.Arl.Gift.Unity.Connectivity;
using Mil.Arl.Gift.Unity.Messaging.Incoming;
using UnityEngine;

namespace Mil.Arl.Gift.Unity.Connectivity
{
    /// <summary>
    /// The AbstractGiftConnector implementation for Unity WebGL
    /// applications. Used to communicate with GIFT through 
    /// JavaScript postMessage events. Assumes the Unity WebGL
    /// application is hosted inside an iframe embedded in the 
    /// Tutor.
    /// </summary>
    public class UnityWebGLGiftConnector : AbstractGiftConnector
    {
        public UnityWebGLGiftConnector() : base()
        {
			Application.runInBackground = true;
        }

        /// <summary>
        /// Deserializes a JSON string using Unity's
        /// JSONUtility
        /// </summary>
        /// <param name="json">The JSON string to deserialize</param>
        /// <returns>The deserialized JSON string as the type specified by the type parameter T</returns>
        protected override T Deserialize<T>(string json)
        {
            JSONObject jObj = new JSONObject(json);
            JsonCodec codec = JsonCodecMapper.GetCodec<T>();
            Debug.Log("Attempting to deserialize " + json + " with " + codec.ToString());
            Debug.Log("Json as Object: " + jObj);
            object res = codec.Deserialize(jObj);
            Debug.Log("Deserialized returned");
            Debug.Log("Deserialized object = " + res.ToString());
            T result = (T) res;
            Debug.Log("Successful deserialization! Returning " + result);
            return result;
        }

        /// <summary>
        /// Sends a string as a message to GIFT using the postMessage JavaScript function.
        /// Assumes that the HTML page hosting the Unity WebGL application has specific global 
        /// scripts present. These scripts are injected when the Unity application is uploaded
        /// through the GAT
        /// </summary>
        /// <param name="message">The message to send to GIFT. Should be a JSON string</param>
        protected override void SendMessageToGift(string message)
        {
            SendMessageToGiftNative(message);
        }

        [DllImport("__Internal")]
        private static extern void SendMessageToGiftNative(string message);

        /// <summary>
        /// Converts a given object to a JSON string using Unity's JsonUtility
        /// class
        /// </summary>
        /// <param name="obj">The object to serialize to JSON</param>
        /// <returns>The passed object as a JSON string</returns>
        protected override string Serialize<T>(T obj)
        {
            return JsonCodecMapper.GetCodec<T>().Serialize(obj);
        }
    }
}