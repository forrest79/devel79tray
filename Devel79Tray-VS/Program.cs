using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Devel79Tray
{
    /// <summary>
    /// Main Devel79 Tray class.
    /// </summary>
    public class Devel79Tray : Form
    {
        /// <summary>
        /// Defatul configuration file name.
        /// </summary>
        private const string DEFAULT_CONFIGURATION_FILE = "devel79.conf";

        /// <summary>
        /// Servers list.
        /// </summary>
        private Dictionary<string, Server> servers;

        /// <summary>
        /// Active server.
        /// </summary>
        private Server activeServer;

        /// <summary>
        /// VirtualBox wrapper.
        /// </summary>
        private VirtualBoxServer vboxServer;

        /// <summary>
        /// Email monitor service.
        /// </summary>
        private EmailMonitor emailMonitor;

        /// <summary>
        /// Tray icon.
        /// </summary>
        private NotifyIcon trayIcon;
        
        /// <summary>
        /// Tray menu.
        /// </summary>
        private ContextMenuStrip trayMenu;

        /// <summary>
        /// Menu Show console.
        /// </summary>
        private ToolStripMenuItem miShowConsole;

        /// <summary>
        /// Menu Start server.
        /// </summary>
        private ToolStripMenuItem miStartServer;

        /// <summary>
        /// Menu Restart server.
        /// </summary>
        private ToolStripMenuItem miRestartServer;
        
        /// <summary>
        /// Menu Stop server.
        /// </summary>
        private ToolStripMenuItem miStopServer;
        
        /// <summary>
        /// Menu Ping server.
        /// </summary>
        private ToolStripMenuItem miPingServer;

        /// <summary>
        /// Seperator for menu with commands.
        /// </summary>
        private ToolStripSeparator miCommandsSeparator;

        /// <summary>
        /// Menu with commands.
        /// </summary>
        private ToolStripMenuItem miCommands;

        /// <summary>
        /// Seperator for menu with servers.
        /// </summary>
        private ToolStripSeparator miServersSeparator;

        /// <summary>
        /// Menu with servers.
        /// </summary>
        private ToolStripMenuItem miServers;

        /// <summary>
        /// Seperator for menu Open email directory.
        /// </summary>
        private ToolStripSeparator miOpenEmailDirectorySeparator;

        /// <summary>
        /// Menu Open email directory.
        /// </summary>
        private ToolStripMenuItem miOpenEmailDirectory;

        /// <summary>
        /// Icon server running.
        /// </summary>
        private Icon iconRun;

        /// <summary>
        /// Icon server stop.
        /// </summary>
        private Icon iconStop;

        /// <summary>
        /// Mutex for only one instance testing.
        /// </summary>
        private static Mutex oneInstanceMutex;

        /// <summary>
        /// Delegate callback for menu update.
        /// </summary>
        private delegate void SetCallback();

        /// <summary>
        /// Balloon tip click callback.
        /// </summary>
        private ICallable trayCallback;
        
        /// <summary>
        /// Main method.
        /// </summary>
        /// <param name="args">Command line arguments</param>
        [STAThread]
        static void Main(string[] args)
        {
            string defaultServer = null;
            bool runServerAtStartup = false;
            string configurationFile = Application.StartupPath + "\\" + DEFAULT_CONFIGURATION_FILE;

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].ToLower().Equals("--default") || args[i].ToLower().Equals("-d"))
                {
                    if ((args.Length <= i) || (args[i + 1].StartsWith("-")))
                    {
                        ShowError("Inicialization error", "No server for --default parameter specified.");
                    }
                    defaultServer = args[++i];
                }
                else if (args[i].ToLower().Equals("--run") || args[i].ToLower().Equals("-r"))
                {
                    runServerAtStartup = true;
                }
                else if ((args[i].ToLower().Equals("--config") || args[i].ToLower().Equals("-c")) && (args.Length > i))
                {
                    if ((args.Length <= i) || (args[i + 1].StartsWith("-")))
                    {
                        ShowError("Inicialization error", "No file for --config parameter specified.");
                    }
                    configurationFile = args[++i];
                }
            }
            
            Devel79Tray devel79Tray = null;

            try
            {
                devel79Tray = new Devel79Tray();
                devel79Tray.Initialize(new ConfigurationReader(configurationFile), defaultServer, runServerAtStartup);
            }
            catch (ProgramException e)
            {
                ShowError("Inicialization error", e.Message);
                return;
            }
            catch (Exception e)
            {
                ShowError("Unhandled inicialization exception", "Message: " + e.Message + "\nSource: " + e.Source + "\nStack trace: " + e.StackTrace);
                return;
            }

            Application.Run(devel79Tray);
        }

        /// <summary>
        /// Create Devel79Tray.
        /// </summary>
        public Devel79Tray()  
        {
            // Prepare menu
            miShowConsole = new ToolStripMenuItem("Show &console");
            miShowConsole.Font = new Font(miShowConsole.Font, miShowConsole.Font.Style | FontStyle.Bold);
            miShowConsole.Visible = false;
            miShowConsole.Click += MenuShowConsole;

            miStartServer = new ToolStripMenuItem("&Start server");
            miStartServer.Visible = false;
            miStartServer.Click += MenuStartServer;

            miStopServer = new ToolStripMenuItem("S&top server");
            miStopServer.Visible = false;
            miStopServer.Click += MenuStopServer;

            miRestartServer = new ToolStripMenuItem("&Restart server");
            miRestartServer.Visible = false;
            miRestartServer.Click += MenuRestartServer;

            miPingServer = new ToolStripMenuItem("&Ping server");
            miPingServer.Visible = false;
            miPingServer.Click += MenuPingServer;

            miServersSeparator = new ToolStripSeparator();
            miServersSeparator.Visible = false;

            miServers = new ToolStripMenuItem("&Servers");
            miServers.Visible = false;

            miCommandsSeparator = new ToolStripSeparator();
            miCommandsSeparator.Visible = false;

            miCommands = new ToolStripMenuItem("&Commands");
            miCommands.Visible = false;

            miOpenEmailDirectorySeparator = new ToolStripSeparator();
            miOpenEmailDirectorySeparator.Visible = false;

            miOpenEmailDirectory = new ToolStripMenuItem("&Open email directory");
            miOpenEmailDirectory.Visible = false;
            miOpenEmailDirectory.Click += MenuOpenEmailDirectory;

            ToolStripMenuItem miExit = new ToolStripMenuItem("E&xit");
            miExit.Click += MenuExit;

            trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add(miShowConsole);
            trayMenu.Items.Add(miStartServer);
            trayMenu.Items.Add(miStopServer);
            trayMenu.Items.Add(miRestartServer);
            trayMenu.Items.Add(miPingServer);
            trayMenu.Items.Add(miCommandsSeparator);
            trayMenu.Items.Add(miCommands);
            trayMenu.Items.Add(miServersSeparator);
            trayMenu.Items.Add(miServers);
            trayMenu.Items.Add(miOpenEmailDirectorySeparator);
            trayMenu.Items.Add(miOpenEmailDirectory);
            trayMenu.Items.Add(new ToolStripSeparator());
            trayMenu.Items.Add(miExit);  

            trayIcon = new NotifyIcon();
            trayIcon.Icon = Properties.Resources.IconServer;

            // Add menu to tray icon and show it.
            trayIcon.MouseDoubleClick += MenuShowConsole;
            trayIcon.BalloonTipClicked += BalloonTipClicked;
            trayIcon.BalloonTipClosed += BalloonTipClosed;
            trayIcon.ContextMenuStrip = trayMenu;
            trayIcon.Visible = false;

            // Load icons
            iconRun = Properties.Resources.IconServerRun;
            iconStop = Properties.Resources.IconServerStop;

        }

        /// <summary>
        /// Initialize Devel79 Tray.
        /// </summary>
        /// <param name="configurationReader">Configuration read from file.</param>
        /// <param name="defaultServer">Server name to be default at startup.</param>
        /// <param name="runServerAtStartup">Server name to run at startup.</param>
        public void Initialize(ConfigurationReader configurationReader, string defaultServer, bool runServerAtStartup)
        {
            Server activateServer;

            if (IsAlreadyRunning())
            {
                throw new ProgramException("Only one instance of Devel79 Tray is allowed.");
            }

            servers = configurationReader.GetServers();

            if (defaultServer != null)
            {
                if (servers.ContainsKey(defaultServer.ToLower()))
                {
                    activateServer = servers[defaultServer.ToLower()];
                }
                else
                {
                    throw new Exception("Default server \"" + defaultServer + "\" is not specified in configuration file \"" + configurationReader.GetConfigurationFile() + "\".");
                }
            }
            else
            {
                ArrayList values = new ArrayList(servers.Values);
                activateServer = (Server)values[0];
            }

            if (servers.Count == 0)
            {
                throw new Exception("No server is defined in \"" + configurationReader.GetConfigurationFile() + "\".");
            }
            else if (servers.Count > 1)
            {
                foreach (Server server in servers.Values)
                {
                    if (VirtualBoxServer.IsServerRunning(server.GetMachine()))
                    {
                        activateServer = server;
                        runServerAtStartup = false;
                    }

                    ToolStripMenuItem miServer = new ToolStripMenuItem(server.GetName());
                    miServer.Tag = server.GetMachine();
                    miServer.Click += MenuChangeServer;
                    miServers.DropDownItems.Add(miServer);
                }
                miServersSeparator.Visible = true;
                miServers.Visible = true;
            }

            trayIcon.Visible = true;

            try
            {
                // Create VirtualBox machine
                vboxServer = new VirtualBoxServer(this);

                // Create email monitor service
                emailMonitor = new EmailMonitor(this);

                // Initialize VirtualBox servers
                vboxServer.Initialize();

                ChangeServer(activateServer);

                if (runServerAtStartup && !VirtualBoxServer.IsServerRunning(activateServer.GetMachine()))
                {
                    vboxServer.StartServer();
                }
            }
            catch (ProgramException e)
            {
                trayIcon.Visible = false;
                throw e;
            }
        }

        /// <summary>
        /// Show console menu.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuShowConsole(object sender, EventArgs e)
        {
            vboxServer.ShowConsole(activeServer.GetSSH());
        }

        /// <summary>
        /// Start server menu.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuStartServer(object sender, EventArgs e)
        {
            try
            {
                vboxServer.StartServer();
            }
            catch (VirtualBoxServerException ex) 
            {
                ShowError("Error", ex.Message);
            }
        }

        /// <summary>
        /// Stop server menu.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuStopServer(object sender, EventArgs e)
        {
            try
            {
                vboxServer.StopServer();
            }
            catch (VirtualBoxServerException ex)
            {
                ShowError("Error", ex.Message);
            }
        }

        /// <summary>
        /// Restart server menu.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuRestartServer(object sender, EventArgs e)
        {
            try
            {
                vboxServer.RestartServer();
            }
            catch (VirtualBoxServerException ex)
            {
                ShowError("Error", ex.Message);
            }
        }

        /// <summary>
        /// Ping server menu.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuPingServer(object sender, EventArgs e)
        {
            vboxServer.PingServer(activeServer.GetIP());
        }

        /// <summary>
        /// Change server menu.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuChangeServer(object sender, EventArgs e)
        {
            string machine = ((ToolStripMenuItem)sender).Tag.ToString();
            ChangeServer(servers[machine.ToLower()]);
        }

        /// <summary>
        /// Run command menu.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuCommand(object sender, EventArgs e)
        {
            vboxServer.RunCommand(((ToolStripMenuItem)sender).Text, ((ToolStripMenuItem)sender).Tag.ToString());
        }

        /// <summary>
        /// Open email directory.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuOpenEmailDirectory(object sender, EventArgs e)
        {
            emailMonitor.OpenEmailDirectory();
        }

        /// <summary>
        /// Exit application.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuExit(object sender, EventArgs e)
        {
            Application.Exit();
        }

        /// <summary>
        /// Clear click callback.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void BalloonTipClosed(object sender, EventArgs e)
        {
            trayCallback = null;
        }

        /// <summary>
        /// Call click callback, if there is one.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void BalloonTipClicked(object sender, EventArgs e)
        {
            if (trayCallback != null)
            {
                trayCallback.Call();
                trayCallback = null;
            }
        }
        
        /// <summary>
        /// Callback called when server is powered off.
        /// </summary>
        public void SetServerPoweredOff()
        {
            trayIcon.Icon = iconStop;
            if (trayMenu.InvokeRequired)
            {
                SetCallback callback = new SetCallback(SetServerPoweredOff);
                this.Invoke(callback);
            }
            else
            {
                miShowConsole.Visible = false;
                miStartServer.Visible = true;
                miStopServer.Visible = false;
                miRestartServer.Visible = false;
                miPingServer.Visible = false;

                if (emailMonitor.IsActive())
                {
                    emailMonitor.StopMonitoring();
                    miOpenEmailDirectorySeparator.Visible = false;
                    miOpenEmailDirectory.Visible = false;
                }

                if (miCommands.DropDownItems.Count > 0)
                {
                    for (int i = 0; i < miCommands.DropDownItems.Count; i++)
                    {
                        miCommands.DropDownItems.RemoveAt(0);
                    }
                }

                miCommandsSeparator.Visible = false;
                miCommands.Visible = false;
            }
        }

        /// <summary>
        /// Callback called when server is started.
        /// </summary>
        public void SetServerRunning()
        {
            trayIcon.Icon = iconRun;
            if (trayMenu.InvokeRequired)
            {
                SetCallback callback = new SetCallback(SetServerRunning);
                this.Invoke(callback);
            }
            else
            {
                miShowConsole.Visible = true;
                miStartServer.Visible = false;
                miStopServer.Visible = true;
                miRestartServer.Visible = true;
                miPingServer.Visible = true;

                if (activeServer.GetMachine() != vboxServer.GetMachine())
                {
                    SetActiveServer(servers[vboxServer.GetMachine().ToLower()]);
                }

                if (!string.IsNullOrEmpty(activeServer.GetEmailDirectory()))
                {
                    try
                    {
                        emailMonitor.StartMonitoring(activeServer.GetEmailDirectory());
                        miOpenEmailDirectorySeparator.Visible = true;
                        miOpenEmailDirectory.Visible = true;
                    }
                    catch (EmailMonitorException e)
                    {
                        ShowTrayError("Email monitoring", e.Message);
                    }
                }

                Dictionary<string, string> commands = activeServer.GetCommands();
                if (commands.Count > 0)
                {
                    foreach (string name in commands.Keys)
                    {
                        ToolStripMenuItem miCommand = new ToolStripMenuItem(name);
                        miCommand.Tag = commands[name];
                        miCommand.Click += MenuCommand;
                        miCommands.DropDownItems.Add(miCommand);
                    }

                    miCommandsSeparator.Visible = true;
                    miCommands.Visible = true;
                }
            }
        }

        /// <summary>
        /// Change active server.
        /// </summary>
        /// <param name="server">New active server</param>
        private void ChangeServer(Server server)
        {
            if (server == activeServer)
            {
                return;
            }

            if (vboxServer.GetStatus() == VirtualBoxServer.Status.STARTING)
            {
                ShowTrayWarning("Change server", "Server \"" + activeServer.GetName() + "\" is starting now, please wait a second and try it again.");
                return;
            }

            if (vboxServer.GetStatus() == VirtualBoxServer.Status.STOPING)
            {
                ShowTrayWarning("Change server", "Server \"" + activeServer.GetName() + "\" is stoping now, please wait a second and try it again.");
                return;
            }

            if ((vboxServer.GetStatus() == VirtualBoxServer.Status.RUNNING) && !ShowQuestion("Change server", "Server \"" + activeServer.GetName() + "\" is running. Do you want to stop this server and run \"" + server.GetName() + "\"?"))
            {
                return;
            }

            if (vboxServer.GetStatus() != VirtualBoxServer.Status.RUNNING)
            {
                SetActiveServer(server);
            }

            vboxServer.RegisterServer(server.GetName(), server.GetMachine());
        }

        /// <summary>
        /// Active new server.
        /// </summary>
        /// <param name="server">Server to activate</param>
        private void SetActiveServer(Server server)
        {
            activeServer = server;

            trayIcon.Text = activeServer.GetName();

            for (int i = 0; i < miServers.DropDownItems.Count; i++)
            {
                ToolStripMenuItem miServer = (ToolStripMenuItem)miServers.DropDownItems[i];
                miServer.Checked = miServer.Tag.ToString() == activeServer.GetMachine();
            }
        }

        /// <summary>
        /// Show error message box.
        /// </summary>
        /// <param name="caption">Message box caption.</param>
        /// <param name="text">Message box body.</param>
        public static void ShowError(string caption, string text)
        {
            MessageBox.Show(text, caption, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        /// <summary>
        /// Show question box.
        /// </summary>
        /// <param name="caption">Question box caption..</param>
        /// <param name="text">Question box body.</param>
        /// <returns>Answer Yes=true, No=false</returns>
        public bool ShowQuestion(string caption, string text)
        {
            if (MessageBox.Show(text, caption, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Show info in tray icon.
        /// </summary>
        /// <param name="caption">Info caption.</param>
        /// <param name="text">Info text.</param>
        public void ShowTrayInfo(string caption, string text)
        {
            ShowTrayBalloonTip(3000, caption, text, ToolTipIcon.Info);
        }

        /// <summary>
        /// Show info in tray and register click callback.
        /// </summary>
        /// <param name="caption">Info caption.</param>
        /// <param name="text">Info text.</param>
        /// <param name="callback">Click callback.</param>
        public void ShowTrayInfo(string caption, string text, ICallable callback)
        {
            trayCallback = callback;
            ShowTrayBalloonTip(5000, caption, text, ToolTipIcon.Info);
        }

        /// <summary>
        /// Show warning in tray icon.
        /// </summary>
        /// <param name="caption">Warning caption.</param>
        /// <param name="text">Warning text.</param>
        public void ShowTrayWarning(string caption, string text)
        {
            ShowTrayBalloonTip(3000, caption, text, ToolTipIcon.Warning);
        }

        /// <summary>
        /// Show error in tray icon.
        /// </summary>
        /// <param name="caption">Error caption.</param>
        /// <param name="text">Error text.</param>
        public void ShowTrayError(string caption, string text)
        {
            ShowTrayBalloonTip(3000, caption, text, ToolTipIcon.Error);
        }

        /// <summary>
        /// Show tray icon balloon tip with icon.
        /// </summary>
        /// <param name="timeout">Balloon tip timeout in ms.</param>
        /// <param name="caption">Balloon tip caption.</param>
        /// <param name="text">Balloon tip text.</param>
        /// <param name="icon">Balloon tip icon.</param>
        private void ShowTrayBalloonTip(int timeout, string caption, string text, ToolTipIcon icon)
        {
            trayIcon.ShowBalloonTip(timeout, caption, text, icon);
        }

        /// <summary>
        /// Check if another instance of application is already running.
        /// </summary>
        /// <returns>True if another instance is running, false otherwise.</returns>
        private static bool IsAlreadyRunning()
        {
            string location = Assembly.GetExecutingAssembly().Location;

            FileSystemInfo fileInfo = new FileInfo(location);
            string sExeName = fileInfo.Name;
            oneInstanceMutex = new Mutex(true, sExeName);

            if (oneInstanceMutex.WaitOne(0, false))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Set form properties.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e)
        {
            Visible = false; // Hide form window.
            ShowInTaskbar = false; // Remove from taskbar.

            base.OnLoad(e);
        }

        /// <summary>
        /// Dispose form.
        /// </summary>
        /// <param name="isDisposing"></param>
        protected override void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {
                // Release the icon resource.
                trayIcon.Dispose();
            }

            base.Dispose(isDisposing);
        }

        /// <summary>
        /// Initialize form component.
        /// </summary>
        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // Devel79Tray
            // 
            this.ClientSize = new System.Drawing.Size(100, 100);
            this.Name = "Devel79Tray";
            this.ResumeLayout(false);

        }

        /// <summary>
        /// Form closing.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);

            if (vboxServer != null)
            {
                if (e.CloseReason == CloseReason.ApplicationExitCall)
                {
                    vboxServer.ApplicationClose();
                }

                vboxServer.Release();
            }
        }

    }

    /// <summary>
    /// Callback interface.
    /// </summary>
    public interface ICallable
    {
        /// <summary>
        /// Call on callback.
        /// </summary>
        void Call();
    }

    /// <summary>
    /// Program exception.
    /// </summary>
    public class ProgramException : Exception
    {

        /// <summary>
        /// Blank initialization.
        /// </summary>
        public ProgramException()
        {
        }

        /// <summary>
        /// Initialization with message.
        /// </summary>
        /// <param name="message"></param>
        public ProgramException(string message) : base(message)
        {
        }
    }
}