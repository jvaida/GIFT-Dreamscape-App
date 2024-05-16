/*
*   File Name: GiftConnection.cs
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
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Mil.Arl.Gift.Unity.Connectivity;
using UnityEngine;
using UnityEngine.UI;

namespace Mil.Arl.Gift.Unity.Connectivity
{
    /// <summary>
    /// Provides a target that the hosting HTML webpage can send messages to.
    /// Forwards incoming messages from GIFT to the GiftConnector and also performs
    /// logic for handling errors when the API consumer does not handle required messsages.
    /// </summary>
    public class GiftConnection : MonoBehaviour
    {
        /// <summary>
        /// The error message to send to Gift if there is no event handler for the OnSimanReceived event
        /// </summary>
        private const string NO_SIMAN_HANDLER_MESSAGE = "No event handler subscribed to AbstractGiftConnection's OnSimanReceived event.";

        /// <summary>
        /// The connector to use to send messages to GIFT message subscribers
        /// </summary>
        private AbstractGiftConnector connector;

        /// <summary>
        /// Method that is called by the Unity Engine to initialize the object.
        /// Fetches the instance of the AbstractGiftConnector used by Unity.
        /// </summary>
        void Start()
        {
            if (connector == null)
                connector = GiftConnectorFactory.CreateGiftConnector(typeof(UnityWebGLGiftConnector));
        }

        /// <summary>
        /// Method that is called externally by the hosting HTML page when a message is 
        /// sent from GIFT, to the Unity WebGL application.
        /// </summary>
        /// <param name="message">The JSON string that is being received from GIFT</param>
        /// <returns>An IEnumerator representing the coroutine</returns>
        public IEnumerator OnExternalMessageReceived(string message)
        {
            if(!connector.HasSimanHandler())
            {
                //Gives the API consumer time to subscribe to the necessary events
                yield return new WaitForSeconds(1f);
                
                //If the connector still does not have a subscriber,
                //send an error to GIFT
                if(!connector.HasSimanHandler())
                {
                    connector.SendErrorToGift(NO_SIMAN_HANDLER_MESSAGE);
                    yield break;
                }
            } 
            
            //Sends the message from GIFT to all subscribers within Unity
            connector.PostIncomingMessage(message);
            yield break;
        }
    }
}