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
        /// Main program.
        /// </summary>
        private Devel79Tray tray;

        /// <summary>
        /// VirtualBox wrapper.
        /// </summary>
        private VirtualBoxServer vboxServer;

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
        /// <param name="tray">Main program</param>
        /// <param name="vboxServer">VirtualBox wrapper</param>
        /// <param name="configurationFile">Absolute path to configuration file</param>
        public ConfigurationReader(Devel79Tray tray, VirtualBoxServer vboxServer, string configurationFile)
        {
            this.tray = tray;
            this.vboxServer = vboxServer;
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
                            server = new Server(this.tray, this.vboxServer);
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
                            case "ssh":
                                server.SetSSHCommand(value);
                                break;
                            case "watch":
                                string[] watchingDirectory = value.Split("|".ToCharArray(), 3);

                                if (watchingDirectory.Length != 3)
                                {
                                    throw new ConfigurationReaderException("Parameter \"watch\" must have 3 parameters divided by \"|\" (name | message | directory)");
                                }
                                else if (!Directory.Exists(watchingDirectory[2]))
                                {
                                    throw new ConfigurationReaderException("Directory \"" + watchingDirectory[2].Trim() + "\" for watching not exists.");
                                }
                                server.AddDirectoryWatching(watchingDirectory[0].Trim(), watchingDirectory[1].Trim(), watchingDirectory[2].Trim());
                                break;
                            case "command":
                                string[] command = value.Split("|".ToCharArray(), 2);

                                if (command.Length != 2)
                                {
                                    throw new ConfigurationReaderException("Parameter \"command\" must have 2 parameters divided by \"|\" (name | command)");
                                }

                                server.AddCommand(command[0].Trim(), command[1].Trim());
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
            if (string.IsNullOrEmpty(newServer.GetName()))
            {
                throw new ConfigurationReaderException("Name is required for all servers.");
            }
            else if (string.IsNullOrEmpty(newServer.GetMachine()))
            {
                throw new ConfigurationReaderException("Machine is required for all servers.");
            }

            if (servers.ContainsKey(newServer.GetMachine().ToLower()))
            {
                throw new ConfigurationReaderException("Server machine \"" + newServer.GetMachine() + "\" is already registered.");
            }

            servers.Add(newServer.GetMachine().ToLower(), newServer);
        }

        /// <summary>
        /// Return configuration file absolute path.
        /// </summary>
        /// <returns>Configuration file absolute path</returns>
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
