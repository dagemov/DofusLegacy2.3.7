using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml;

namespace Uplauncher.Classes
{
    public class Configuration
    {
        public static XmlDocument ConfigurationDocument
        {
            get;
            set;
        }

        public static Dictionary<string, string> Cache
        {
            get;
            set;
        }

        private static bool LoadConfiguration()
        {
            bool result;
            try
            {
                if (!File.Exists(".\\data/Launcher/configuration.xml"))
                {
                    Assembly _assembly = Assembly.GetExecutingAssembly();
                    StreamReader _textStreamReader = new StreamReader(_assembly.GetManifestResourceStream
                        ("Uplauncher.data.configuration_origin.xml"));
                    //("Uplauncher.Resources.configuration_origin.xml"));

                    string xmldata = _textStreamReader.ReadToEnd();
                    ConfigurationDocument = new XmlDocument();
                    ConfigurationDocument.LoadXml(xmldata);
                    //File.WriteAllText(".\\data/Launcher/configuration.xml", xmldata);
                }
                else
                {
                    ConfigurationDocument = new XmlDocument();
                    ConfigurationDocument.Load(".\\data/Launcher/configuration.xml");
                }

                result = true;
            }
            catch (Exception e)
            {
                Log.PublishExceptionLog(e);
                result = false;
            }

            return result;
        }

        public static string LoadEntry(string key, string defaultValue = null)
        {
            if (key == null)
            {
                throw new ArgumentNullException("Key");
            }
            string result = defaultValue;
            if (Configuration.Cache == null)
            {
                Configuration.Cache = new Dictionary<string, string>();
            }
            string result2;
            if (Configuration.Cache.TryGetValue(key, out result))
            {
                result2 = result;
            }
            else
            {
                if (Configuration.ConfigurationDocument == null)
                {
                    if (!Configuration.LoadConfiguration())
                    {
                        result2 = defaultValue;
                        return result2;
                    }
                }
                try
                {
                    XmlNode node = Configuration.ConfigurationDocument.SelectSingleNode(string.Format("//SunshineUplauncher/ConfigurationCollection/Configuration[@Entry='{0}']", key));
                    if (node == null)
                    {
                        result2 = defaultValue;
                        return result2;
                    }
                    result = node.Attributes["Value"].Value;
                    Configuration.Cache.Add(key, result);
                }
                catch (Exception e)
                {
                    Log.PublishExceptionLog(e);
                }
                result2 = result;
            }
            return result2;
        }

        public static bool LoadBooleanEntry(string Entry, bool defaultValue = true)
        {
            bool result;
            try
            {
                result = bool.Parse(Configuration.LoadEntry(Entry, null));
            }
            catch
            {
                result = defaultValue;
            }
            return result;
        }
    }
}
