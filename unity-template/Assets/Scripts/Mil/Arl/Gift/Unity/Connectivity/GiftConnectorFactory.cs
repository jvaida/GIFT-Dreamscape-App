/*
*   File Name: GiftConnectorFactory.cs
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
using Mil.Arl.Gift.Unity.Connectivity;

namespace Mil.Arl.Gift.Unity.Connectivity
{
    /// <summary>
    /// The factory class that constructs AbstractGiftConnectors. Responsible for 
    /// ensuring that only one instance of a each type of connector exists at any given 
    /// time.
    /// </summary>
    public class GiftConnectorFactory
    {
        /// <summary>
        /// Creates or fetches the connector of a specified type
        /// </summary>
        /// <param name="connectorType">The type of connector to return</param>
        /// <returns>The connector of the specified type. If one has been previously constructed it is returned</returns>
        public static AbstractGiftConnector CreateGiftConnector(Type connectorType)
        {
            //Validate its the right type
            if (!typeof(AbstractGiftConnector).IsAssignableFrom(connectorType))
                throw new ArgumentException("The supplied type must inherit from AbstractGiftConnector", "connectorType");

            //Check if an instance already exists
            if (instances.ContainsKey(connectorType))
                return instances[connectorType];
            else
            {
                instances[connectorType] = (AbstractGiftConnector)Activator.CreateInstance(connectorType);
                return instances[connectorType];
            }
        }

        /// <summary>
        /// The dictionary that tracks each singleton instances for each
        /// type of AbstractGiftConnector
        /// </summary>
        private static Dictionary<Type, AbstractGiftConnector> instances = new Dictionary<Type, AbstractGiftConnector>();
    }
}