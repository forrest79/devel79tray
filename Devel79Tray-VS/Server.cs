using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace Devel79Tray
{
    public class Server
    {
        /// <summary>
        /// Constant for Win32 function ShowWindow. Window is showen.
        /// </summary>
        private const int SW_RESTORE = 9;

        /// <summary>
        /// Devel79 Tray form.
        /// </summary>
        private Devel79Tray tray;

        /// <summary>
        /// Main program.
        /// </summary>
        private VirtualBoxServer vboxServer;

        /// <summary>
        /// Server name.
        /// </summary>
        private string name;

        /// <summary>
        /// VirtualBox machine name.
        /// </summary>
        private string machine;

        /// <summary>
        /// SSH client shell command.
        /// </summary>
        private string sshCommand;

        /// <summary>
        /// SSH client process.
        /// </summary>
        private Process sshProcess;

        /// <summary>
        /// Watching directories list <name, directoryMonitor>
        /// </summary>
        private Dictionary<string, DirectoryMonitor> watchingDirectories;

        /// <summary>
        /// Commands list <name, command>.
        /// </summary>
        private Dictionary<string, string> commands;

        /// <summary>
        /// Is server running?
        /// </summary>
        private bool running;

        /// <summary>
        /// Ïs server starting?
        /// </summary>
        private bool starting;

        /// <summary>
        /// Is server restarting?
        /// </summary>
        private bool restarting;

        /// <summary>
        /// Is server stoping?
        /// </summary>
        private bool stoping;

        /// <summary>
        /// Is menu already generated?
        /// </summary>
        private bool menuGenerated;

        /// <summary>
        /// Start server menu.
        /// </summary>
        private ToolStripMenuItem miStartServer;

        /// <summary>
        /// Server control menu.
        /// </summary>
        private ToolStripMenuItem miServer;

        /// <summary>
        /// Actual server status.
        /// </summary>
        private VirtualBoxServer.Status status;

        /// <summary>
        /// Create new server.
        /// </summary>
        public Server(Devel79Tray tray, VirtualBoxServer vboxServer)
        {
            this.tray = tray;
            this.vboxServer = vboxServer;
            this.watchingDirectories = new Dictionary<string, DirectoryMonitor>();
            this.commands = new Dictionary<string, string>();

            this.menuGenerated = false;

            this.running = false;
            this.status = VirtualBoxServer.Status.POWEREDOFF;
        }

        /// <summary>
        /// Set server name.
        /// </summary>
        /// <param name="name">Server name</param>
        public void SetName(string name)
        {
            if (menuGenerated == true)
            {
                throw new Exception("Can't set name after menu was generated.");
            }

            this.name = name;
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
        /// Set VirtualBox machine name.
        /// </summary>
        /// <param name="machine">Machine name</param>
        public void SetMachine(string machine)
        {
            this.machine = machine;
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
        /// Set show SSH console command.
        /// </summary>
        /// <param name="sshCommand">Show SSH console command</param>
        public void SetSSHCommand(string sshCommand)
        {
            this.sshCommand = sshCommand;
        }

        /// <summary>
        /// Add new directory to watching.
        /// </summary>
        /// <param name="name">Name</param>
        /// <param name="message">Message to show</param>
        /// <param name="directory">Directory to watch</param>
        public void AddDirectoryWatching(string name, string message, string directory)
        {
            if (menuGenerated == true)
            {
                throw new ServerException("Can't add directory to watching after menu was generated.");
            }

            watchingDirectories.Add(name, new DirectoryMonitor(tray, message, directory));
        }

        /// <summary>
        /// Add new command to server.
        /// </summary>
        /// <param name="name">Name</param>
        /// <param name="command">Command to run</param>
        public void AddCommand(string name, string command)
        {
            if (menuGenerated == true)
            {
                throw new ServerException("Can't add command after menu was generated.");
            }

            commands.Add(name, command);
        }

        /// <summary>
        /// Set server as running.
        /// </summary>
        /// <param name="initializing">True if is running during application initializing, false otherwise</param>
        public void SetRunning(bool initializing)
        {
            foreach (DirectoryMonitor watchingDirectory in watchingDirectories.Values)
            {
                watchingDirectory.StartMonitoring();
            }

            if (restarting)
            {
                tray.ShowTrayInfo(name, name + " was successfully restarted.");
            }
            else if (starting)
            {
                tray.ShowTrayInfo(name, name + " was successfully started.");
            }
            else if (initializing)
            {
                tray.ShowTrayWarning(name, name + " is already started.");
            }
            else
            {
                tray.ShowTrayWarning(name, name + " was started.");
            }

            running = true;
            starting = false;
            restarting = false;
            stoping = false;

            miStartServer.Visible = false;

            tray.UpdateTray();
        }

        /// <summary>
        /// Set server as powered off.
        /// </summary>
        public void SetPoweredOff()
        {
            KillConsole();

            foreach (DirectoryMonitor watchingDirectory in watchingDirectories.Values)
            {
                watchingDirectory.StopMonitoring();
            }

            if (restarting)
            {
                StartServer();
            }
            else
            {
                if (stoping)
                {
                    tray.ShowTrayInfo(name, name + " was successfully powered off.");
                }
                else
                {
                    tray.ShowTrayWarning(name, name + " was powered off.");
                }
            }

            running = false;
            starting = false;
            stoping = false;

            miStartServer.Visible = true;

            tray.UpdateTray();
        }

        /// <summary>
        /// Set server as starting.
        /// </summary>
        public void SetStarting()
        {
            this.starting = true;
        }

        /// <summary>
        /// Set server as stopping.
        /// </summary>
        public void SetStoping()
        {
            this.stoping = true;
        }

        /// <summary>
        /// Is server running?
        /// </summary>
        /// <returns>True=yes, false=no</returns>
        public bool IsRunning()
        {
            return this.running;
        }

        /// <summary>
        /// Is server restarting?
        /// </summary>
        /// <returns>True=yes, false=no</returns>
        public bool IsRestarting()
        {
            return this.restarting;
        }

        /// <summary>
        /// Set server status.
        /// </summary>
        /// <param name="status">Server status</param>
        /// <param name="initializing">True if is seting status during application initializing, false otherwise</param>
        public void SetStatus(VirtualBoxServer.Status status, bool initializing)
        {
            this.status = status;

            if (status == VirtualBoxServer.Status.POWEREDOFF)
            {
                SetPoweredOff();
            }
            else if (status == VirtualBoxServer.Status.RUNNING)
            {
                SetRunning(initializing);
            }
        }

        /// <summary>
        /// Get server status.
        /// </summary>
        /// <returns>Server status</returns>
        public VirtualBoxServer.Status GetStatus()
        {
            return this.status;
        }

        /// <summary>
        /// Get server start menu.
        /// </summary>
        /// <returns>Server start menu</returns>
        public ToolStripMenuItem GetMenuStartServer()
        {
            GenerateMenu();
            return miStartServer;
        }

        /// <summary>
        /// Get server control menu.
        /// </summary>
        /// <returns>Server control menu</returns>
        public ToolStripMenuItem GetMenuServer()
        {
            GenerateMenu();
            return miServer;
        }

        /// <summary>
        /// Generate server menus.
        /// </summary>
        private void GenerateMenu()
        {
            if (menuGenerated == false)
            {
                miStartServer = new ToolStripMenuItem();
                miStartServer.Text = name;
                miStartServer.Click += MenuStartServer;

                miServer = new ToolStripMenuItem();
                miServer.Text = name;
                miServer.Visible = false;

                ToolStripMenuItem miConsole = new ToolStripMenuItem();
                miConsole.Text = "Show &console";
                miConsole.Click += MenuShowConsole;
                this.miServer.DropDownItems.Add(miConsole);

                ToolStripMenuItem miRestartServer = new ToolStripMenuItem();
                miRestartServer.Text = "&Restart server";
                miRestartServer.Click += MenuRestartServer;
                this.miServer.DropDownItems.Add(miRestartServer);

                ToolStripMenuItem miStopServer = new ToolStripMenuItem();
                miStopServer.Text = "&Stop server";
                miStopServer.Click += MenuStopServer;
                this.miServer.DropDownItems.Add(miStopServer);

                if (this.watchingDirectories.Count > 0)
                {
                    ToolStripSeparator miWatchingDirectoriesSeparator = new ToolStripSeparator();
                    this.miServer.DropDownItems.Add(miWatchingDirectoriesSeparator);

                    foreach (string watchingDirectoryName in watchingDirectories.Keys)
                    {
                        ToolStripMenuItem miWatchingDirectory = new ToolStripMenuItem("Open " + watchingDirectoryName + " directory");
                        miWatchingDirectory.Tag = watchingDirectoryName;
                        miWatchingDirectory.Click += MenuOpenWatchingDirectory;
                        this.miServer.DropDownItems.Add(miWatchingDirectory);
                    }
                }

                if (this.commands.Count > 0)
                {
                    ToolStripSeparator miCommandsSeparator = new ToolStripSeparator();
                    this.miServer.DropDownItems.Add(miCommandsSeparator);

                    foreach (string commnadName in commands.Keys)
                    {
                        ToolStripMenuItem miCommand = new ToolStripMenuItem(commnadName);
                        miCommand.Click += MenuRunCommand;
                        this.miServer.DropDownItems.Add(miCommand);
                    }
                }
            }
        }

        /// <summary>
        /// Start server.
        /// </summary>
        public void StartServer()
        {
            try
            {
                vboxServer.StartServer(this);
            }
            catch (VirtualBoxServerException ex)
            {
                tray.ShowTrayError("Start server " + name, ex.Message);
            }
        }

        /// <summary>
        /// Restart server.
        /// </summary>
        private void RestartServer()
        {
            try
            {
                restarting = true;
                vboxServer.StopServer(this);
            }
            catch (VirtualBoxServerException ex)
            {
                tray.ShowTrayError("Restart server " + name, ex.Message);
            }
        }

        /// <summary>
        /// Stop server.
        /// </summary>
        public void StopServer()
        {
            try
            {
                vboxServer.StopServer(this);
            }
            catch (VirtualBoxServerException ex)
            {
                tray.ShowTrayError("Stop server " + name, ex.Message);
            }

            KillConsole();
        }

        /// <summary>
        /// Show SSH client.
        /// </summary>
        public void ShowConsole()
        {
            if (string.IsNullOrEmpty(sshCommand))
            {
                tray.ShowTrayError(name + ": SSH", "SSH command can't be empty.");
            }

            if (status == VirtualBoxServer.Status.RUNNING)
            {
                if (IsConsoleRunning())
                {
                    IntPtr hWnd = sshProcess.MainWindowHandle;

                    if (IsIconic(hWnd))
                    {
                        ShowWindow(hWnd, SW_RESTORE);
                    }

                    SetForegroundWindow(hWnd);
                }
                else
                {
                    sshProcess = new System.Diagnostics.Process();

                    try
                    {
                        string[] sshClient = sshCommand.Split(new Char[] { ' ', '\t' }, 2);

                        switch (sshClient.Length)
                        {
                            case 1:
                                sshProcess.StartInfo.FileName = sshClient[0];
                                break;
                            case 2:
                                sshProcess.StartInfo.Arguments = sshClient[1];
                                goto case 1;
                            default:
                                return;
                        }

                        sshProcess.Start();
                    }
                    catch (System.ComponentModel.Win32Exception)
                    {
                        tray.ShowTrayError(name + ": SSH client", "Can't run SSH client with command '" + sshCommand + "'");
                    }
                }
            }
        }

        /// <summary>
        /// Kill SSH client process.
        /// </summary>
        public void KillConsole()
        {
            if (IsConsoleRunning())
            {
                sshProcess.Kill();
                sshProcess = null;
            }
        }

        /// <summary>
        /// Check if SSH client is running.
        /// </summary>
        /// <returns>True if SSH client is running</returns>
        private bool IsConsoleRunning()
        {
            if (sshProcess == null)
            {
                return false;
            }

            return !sshProcess.HasExited;
        }

        /// <summary>
        /// Run command and read output.
        /// </summary>
        /// <param name="name">Command name</param>
        /// <param name="command">Command executables</param>
        private void RunCommand(string name, string command)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(command))
            {
                throw new ServerException("Command and name can't be empty.");
            }

            string[] cmd = command.Split(new Char[] { ' ', '\t' }, 2);

            ThreadPool.QueueUserWorkItem(delegate
            {
                Process process = new Process();

                switch (cmd.Length)
                {
                    case 1:
                        process.StartInfo.FileName = cmd[0];
                        break;
                    case 2:
                        process.StartInfo.Arguments = cmd[1];
                        goto case 1;
                    default:
                        return;
                }

                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.CreateNoWindow = true;

                try
                {
                    process.Start();

                    string output = process.StandardOutput.ReadToEnd();

                    if (!process.WaitForExit(60000)) // 1 minute
                    {
                        process.Kill();
                        return;
                    }

                    if (process.ExitCode != 0)
                    {
                        tray.ShowTrayError(this.name + " [" + name + "]", "Exit code: " + process.ExitCode.ToString() + "." + (string.IsNullOrEmpty(output) ? "" : "\n" + output));
                        return;
                    }

                    tray.ShowTrayInfo(this.name + " [" + name + "]", string.IsNullOrEmpty(output) ? "Command was successfully run." : output);
                }
                catch (Exception e)
                {
                    tray.ShowTrayError(this.name + " [" + name + "]", "Error while running command: " + e.Message);
                }
            });
        }

        /// <summary>
        /// Start server menu commant.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuStartServer(object sender, EventArgs e)
        {
            StartServer();
        }

        /// <summary>
        /// Restart server menu command.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuRestartServer(object sender, EventArgs e)
        {
            RestartServer();
        }

        /// <summary>
        /// Stop server menu command.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuStopServer(object sender, EventArgs e)
        {
            StopServer();
        }

        /// <summary>
        /// Show server console menu command.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuShowConsole(object sender, EventArgs e)
        {
            ShowConsole();
        }

        /// <summary>
        /// Open watching directory menu command.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuOpenWatchingDirectory(object sender, EventArgs e)
        {
            DirectoryMonitor directoryMonitor = watchingDirectories[((ToolStripMenuItem)sender).Tag.ToString()];
            directoryMonitor.OpenDirectory();
        }

        /// <summary>
        /// Run server command menu command.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuRunCommand(object sender, EventArgs e)
        {
            string commandName = ((ToolStripMenuItem)sender).Text.ToString();
            RunCommand(commandName, commands[commandName]);
        }

        /// <summary>
        /// Win32 function to show or hide window.
        /// </summary>
        /// <param name="hWnd"></param>
        /// <param name="nCmdShow"></param>
        /// <returns></returns>
        [DllImport("User32")]
        private static extern int ShowWindow(IntPtr hWnd, int nCmdShow);

        /// <summary>
        /// Win32 function to move window to foreground.
        /// </summary>
        /// <param name="hWnd"></param>
        /// <returns></returns>
        [DllImport("User32")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        /// <summary>
        /// Win32 function to check window minized state.
        /// </summary>
        /// <param name="hWnd"></param>
        /// <returns>True if minimized</returns>
        [DllImport("User32")]
        private static extern bool IsIconic(IntPtr hWnd);
    }

    /// <summary>
    /// Server exception.
    /// </summary>
    public class ServerException : ProgramException
    {
        /// <summary>
        /// Blank initialization.
        /// </summary>
        public ServerException()
        {
        }

        /// <summary>
        /// Initialization with message.
        /// </summary>
        /// <param name="message"></param>
        public ServerException(string message) : base(message)
        {
        }
    }
}
