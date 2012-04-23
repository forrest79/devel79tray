using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Devel79Tray
{
    public class ConfigurationReader
    {
        private const string DEFAULT_NAME = "Devel79 Server";

        private const string DEFAULT_MACHINE = "devel79";
        
        private const string DEFAULT_IP = "192.168.56.1";

        private string configurationFile;

        private string name;

        private string machine;

        private string ip;

        public ConfigurationReader(string configurationFile)
        {
            this.configurationFile = configurationFile;
            this.name = DEFAULT_NAME;
            this.machine = DEFAULT_MACHINE;
            this.ip = DEFAULT_IP;
        }

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

        public string GetConfigurationFile()
        {
            return configurationFile;
        }

        public string GetName()
        {
            return name;
        }

        public string GetMachine()
        {
            return machine;
        }

        public string GetIp()
        {
            return ip;
        }

    }
}
