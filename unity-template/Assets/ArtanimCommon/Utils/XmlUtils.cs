using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using UnityEngine;

namespace Artanim
{
    public static class XmlUtils
    {
        public static T LoadXmlConfig<T>(string configPath)
        {
            try
            {
                var serializer = new XmlSerializer(typeof(T));
                using (var stream = new FileStream(configPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    var config = (T)serializer.Deserialize(stream);
                    if (config == null)
                    {
                        throw new System.InvalidOperationException("Got a null object from loading XML file at path: " + configPath);
                    }
                    return config;
                }
            }
            catch
            {
                Debug.LogError("Failed to read XML file at path: " + configPath);
                throw;
            }
        }

        public static void SaveXmlConfig<T>(string configPath, T config)
        {
            var xmlSettings = new XmlWriterSettings() { Indent = true, IndentChars = "\t", Encoding = Encoding.UTF8, };

            //Create streaming assets directory if needed
            Directory.CreateDirectory(Path.GetDirectoryName(configPath));

            try
            {
                using (var writer = XmlWriter.Create(configPath, xmlSettings))
                {
                    var serializer = new XmlSerializer(typeof(T));
                    serializer.Serialize(writer, config);
                }
            }
            catch
            {
                Debug.LogError("Failed to save XML file at path: " + configPath);
                throw;
            }
        }
    }
}