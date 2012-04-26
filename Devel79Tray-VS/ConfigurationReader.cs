using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Devel79Tray
{
    /// <summary>
    /// Configuration reader class.
    /// </summary>
    public class ConfigurationReader
    {
        /// <summary>
        /// Default server name.
        /// </summary>
        private const string DEFAULT_NAME = "Devel79 Server";

        /// <summary>
        /// Default VirtualBox machine name.
        /// </summary>
        private const string DEFAULT_MACHINE = "devel79";
        
        /// <summary>
        /// Default server IP address.
        /// </summary>
        private const string DEFAULT_IP = "192.168.56.1";

        /// <summary>
        /// Absolute path to configuration file.
        /// </summary>
        private string configurationFile;

        /// <summary>
        /// Server name.
        /// </summary>
        private string name;

        /// <summary>
        /// VirtualBox machine name.
        /// </summary>
        private string machine;

        /// <summary>
        /// Server IP address
        /// </summary>
        private string ip;

        /// <summary>
        /// Class constructor.
        /// </summary>
        /// <param name="configurationFile">Absolute path to configuration file.</param>
        public ConfigurationReader(string configurationFile)
        {
            this.configurationFile = configurationFile;
            this.name = DEFAULT_NAME;
            this.machine = DEFAULT_MACHINE;
            this.ip = DEFAULT_IP;
        }

        /// <summary>
        /// Read configuration file.
        /// </summary>
        /// <returns>Success.</returns>
        public bool Read()
        {
            if (File.Exists(configurationFile))
            {
                try
                {
                    StreamReader configuration = File.OpenText(configurationFile);

                    string line = null;
                    while ((line = configuration.ReadLine()) != null)
                    {
                        line = line.Trim();

                        if (line.Equals("") || line.StartsWith("#"))
                        {
                            continue;
                        }

                        string[] settings = new string[2];
                        settings = line.Split("=".ToCharArray(), 2);

                        string key = settings[0].Trim().ToLower();
                        string value = settings[1].Trim();

                        switch (key)
                        {
                            case "name":
                                name = value;
                                break;
                            case "machine":
                                machine = value;
                                break;
                            case "ip":
                                ip = value;
                                break;
                        }
                    }
                    configuration.Close();

                    return true;
                }
                catch
                {
                    return false;
                }
            }

            return false;
        }

        /// <summary>
        /// Return configuration file absolute path.
        /// </summary>
        /// <returns>Configuration file absolute path.</returns>
        public string GetConfigurationFile()
        {
            return configurationFile;
        }

        /// <summary>
        /// Return server name.
        /// </summary>
        /// <returns>Server name.</returns>
        public string GetName()
        {
            return name;
        }

        /// <summary>
        /// Return VirtualBox machine name.
        /// </summary>
        /// <returns>VirtualBox machine name.</returns>
        public string GetMachine()
        {
            return machine;
        }

        /// <summary>
        /// Return server IP address.
        /// </summary>
        /// <returns>Server IP address.</returns>
        public string GetIp()
        {
            return ip;
        }
    }
}
