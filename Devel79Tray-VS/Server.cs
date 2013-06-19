using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace Devel79Tray
{
    public class Server
    {
        /// <summary>
        /// Server name.
        /// </summary>
        private string name;

        /// <summary>
        /// VirtualBox machine name.
        /// </summary>
        private string machine;

        /// <summary>
        /// Server IP address.
        /// </summary>
        private string ip;

        /// <summary>
        /// SSH client shell command.
        /// </summary>
        private string ssh;

        /// <summary>
        /// Email directory to monitor.
        /// </summary>
        private string emailDirectory;

        /// <summary>
        /// Commands list <name, command>.
        /// </summary>
        private Dictionary<string, string> commands;

        /// <summary>
        /// Create new server.
        /// </summary>
        /// <param name="name">Server name</param>
        /// <param name="machine">VirtualBox machine name</param>
        /// <param name="ip">Server IP address</param>
        /// <param name="ssh">Command to show SSH console</param>
        public Server(string name, string machine, string ip, string ssh)
        {
            this.name = name;
            this.machine = machine;
            this.ip = ip;
            this.ssh = ssh;
            this.commands = new Dictionary<string, string>();
        }

        /// <summary>
        /// Set server name.
        /// </summary>
        /// <param name="name">server name</param>
        /// <returns>Server (provides fluent interface)</returns>
        public Server SetName(string name)
        {
            this.name = name;
            return this;
        }

        /// <summary>
        /// Get server name.
        /// </summary>
        /// <returns>Server name</returns>
        public string GetName()
        {
            return name;
        }

        /// <summary>
        /// Set machine name.
        /// </summary>
        /// <param name="machine">Machine name</param>
        /// <returns>Server (provides fluent interface)</returns>
        public Server SetMachine(string machine)
        {
            this.machine = machine;
            return this;
        }

        /// <summary>
        /// Get VirtualBox machine name.
        /// </summary>
        /// <returns>VirtualBox machine name</returns>
        public string GetMachine()
        {
            return machine;
        }

        /// <summary>
        /// Set server IP address.
        /// </summary>
        /// <param name="ip">Server IP address</param>
        /// <returns>Server (provides fluent interface)</returns>
        public Server SetIP(string ip)
        {
            this.ip = ip;
            return this;
        }

        /// <summary>
        /// Get server IP address.
        /// </summary>
        /// <returns>Server IP address</returns>
        public string GetIP()
        {
            return ip;
        }

        /// <summary>
        /// Set show SSH console command.
        /// </summary>
        /// <param name="ssh">Show SSH console command</param>
        /// <returns>Server (provides fluent interface)</returns>
        public Server SetSSH(string ssh)
        {
            this.ssh = ssh;
            return this;
        }

        /// <summary>
        /// Get show SSH console command.
        /// </summary>
        /// <returns>Show SSH console command.</returns>
        public string GetSSH()
        {
            return ssh;
        }

        /// <summary>
        /// Set directory for stored emails.
        /// </summary>
        /// <param name="emailDirectory">Directory with stored emails</param>
        /// <returns>Server (provides fluent interface)</returns>
        public Server SetEmailDirectory(string emailDirectory)
        {
            this.emailDirectory = emailDirectory;
            return this;
        }

        /// <summary>
        /// Get directory with stored emails.
        /// </summary>
        /// <returns>Directory with stored emails</returns>
        public string GetEmailDirectory()
        {
            return emailDirectory;
        }

        /// <summary>
        /// Add new command to server.
        /// </summary>
        /// <param name="commandData">Command data "name : command"</param>
        /// <returns>Server (provides fluent interface)</returns>
        public Server AddCommand(string commandData)
        {
            string[] command = new string[2];
            command = commandData.Split(":".ToCharArray(), 2);

            string name = command[0].Trim();
            string cmd = command[1].Trim();

            commands.Add(name, cmd);
            return this;
        }

        /// <summary>
        /// Get all server commands.
        /// </summary>
        /// <returns>List of server commands keys=names, values=commands</returns>
        public Dictionary<string, string> GetCommands()
        {
            return commands;
        }
    }
}
