using System;
using System.IO;
using System.Collections.Generic;

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
        /// Default SSH client shell command.
        /// </summary>
        private const string DEFAULT_SSH = "ssh devel@devel79";

        /// <summary>
        /// Absolute path to configuration file.
        /// </summary>
        private string configurationFile;

        /// <summary>
        /// Servers from configuration file
        /// </summary>
        private Dictionary<string, Server> servers;

        /// <summary>
        /// Class constructor.
        /// </summary>
        /// <param name="configurationFile">Absolute path to configuration file.</param>
        public ConfigurationReader(string configurationFile)
        {
            this.configurationFile = configurationFile;
            this.servers = new Dictionary<string, Server>();
        }

        /// <summary>
        /// Read configuration file.
        /// </summary>
        private void ReadConfiguration()
        {
            if (File.Exists(configurationFile))
            {
                try
                {
                    Server server = null;

                    StreamReader configuration = File.OpenText(configurationFile);

                    string line = null;
                    while ((line = configuration.ReadLine()) != null)
                    {
                        line = line.Trim();

                        if (line.Equals("") || line.StartsWith("#"))
                        {
                            continue;
                        }
                        else if (line.ToLower() == "[server]")
                        {
                            if (server != null)
                            {
                                AddServer(server);
                            }
                            server = new Server(DEFAULT_NAME, DEFAULT_MACHINE, DEFAULT_IP, DEFAULT_SSH);
                            continue;
                        }

                        if (server == null)
                        {
                            throw new ConfigurationReaderException("No [server] section defined for data.");
                        }

                        string[] settings = new string[2];
                        settings = line.Split("=".ToCharArray(), 2);

                        string key = settings[0].Trim();
                        string value = settings[1].Trim();

                        switch (key.ToLower())
                        {
                            case "name":
                                server.SetName(value);
                                break;
                            case "machine":
                                server.SetMachine(value);
                                break;
                            case "ip":
                                server.SetIP(value);
                                break;
                            case "ssh":
                                server.SetSSH(value);
                                break;
                            case "email":
                                server.SetEmailDirectory(value);
                                break;
                            case "command":
                                server.AddCommand(value);
                                break;
                        }
                    }

                    configuration.Close();

                    if (server == null)
                    {
                        throw new ConfigurationReaderException("No server defined in configuration.");
                    }

                    AddServer(server);
                }
                catch (ConfigurationReaderException e)
                {
                    throw e;
                }
                catch
                {
                    throw new ConfigurationReaderException("Can't read from configuration file \"" + configurationFile + "\".");
                }
            }
            else
            {
                throw new ConfigurationReaderException("Configuration file \"" + configurationFile + "\" not exists.");
            }
        }

        /// <summary>
        /// Add new server definition to server.
        /// </summary>
        /// <param name="newServer">Server definition</param>
        private void AddServer(Server newServer)
        {
            if (servers.ContainsKey(newServer.GetMachine().ToLower())) {
                throw new ConfigurationReaderException("Server machine \"" + newServer.GetMachine() + "\" is already registered.");
            }

            servers.Add(newServer.GetMachine().ToLower(), newServer);
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
        /// Get server list.
        /// </summary>
        /// <returns>Server list keys=lowered machine names, values=Server</returns>
        public Dictionary<string, Server> GetServers()
        {
            if (servers.Count == 0)
            {
                ReadConfiguration();
            }

            return servers;
        }
    }

    /// <summary>
    /// Configuration reader exception.
    /// </summary>
    public class ConfigurationReaderException : ProgramException
    {

        /// <summary>
        /// Blank initialization.
        /// </summary>
        public ConfigurationReaderException()
        {
        }

        /// <summary>
        /// Initialization with message.
        /// </summary>
        /// <param name="message"></param>
        public ConfigurationReaderException(string message) : base(message)
        {
        }
    }
}
