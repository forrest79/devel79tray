using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

namespace Devel79Tray
{
    /// <summary>
    /// Main Devel79 Tray class.
    /// </summary>
    public class Devel79Tray : Form
    {
        /// <summary>
        /// Tray icon title.
        /// </summary>
        private const string TRAY_TITLE = "Devel79 Tray";

        /// <summary>
        /// Defatul configuration file name.
        /// </summary>
        private const string DEFAULT_CONFIGURATION_FILE = "devel79tray.conf";

        /// <summary>
        /// Servers list.
        /// </summary>
        private Dictionary<string, Server> servers;

        /// <summary>
        /// Default server.
        /// </summary>
        private Server defaultServer;

        /// <summary>
        /// VirtualBox wrapper.
        /// </summary>
        private VirtualBoxServer vboxServer;

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
        private ToolStripMenuItem miShowDefaultConsole;

        /// <summary>
        /// Show console separator.
        /// </summary>
        private ToolStripSeparator separatorShowDefaultConsole;

        /// <summary>
        /// Server menus.
        /// </summary>
        private Dictionary<Server, ToolStripMenuItem> miServers;

        /// <summary>
        /// Server menus separator.
        /// </summary>
        private ToolStripSeparator separatorServers;

        /// <summary>
        /// Menu with servers to start.
        /// </summary>
        private ToolStripMenuItem miStartServers;

        /// <summary>
        /// Start servers menu separator.
        /// </summary>
        private ToolStripSeparator separatorStartServers;

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
            bool serverList = false;
            List<string> runServers = new List<string>();
            string configurationFile = Application.StartupPath + "\\" + DEFAULT_CONFIGURATION_FILE;

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].ToLower().Equals("--run") || args[i].ToLower().Equals("-r"))
                {
                    if ((args.Length <= i) || (args[i + 1].StartsWith("-")))
                    {
                        ShowError("Inicialization error", "No server for --run parameter specified.");
                    }
                    serverList = true;
                }
                else if ((args[i].ToLower().Equals("--config") || args[i].ToLower().Equals("-c")) && (args.Length > i))
                {
                    if ((args.Length <= i) || (args[i + 1].StartsWith("-")))
                    {
                        ShowError("Inicialization error", "No file for --config parameter specified.");
                    }
                    configurationFile = args[++i];
                    serverList = false;
                }
                else if (serverList)
                {
                    runServers.Add(args[i]);
                }
            }
            
            Devel79Tray devel79Tray = null;

            try
            {
                devel79Tray = new Devel79Tray();
                devel79Tray.Initialize(configurationFile, runServers.ToArray());
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
            miShowDefaultConsole = new ToolStripMenuItem();
            miShowDefaultConsole.Font = new Font(miShowDefaultConsole.Font, miShowDefaultConsole.Font.Style | FontStyle.Bold);
            miShowDefaultConsole.Visible = false;
            miShowDefaultConsole.Click += MenuShowDefaultConsole;

            separatorShowDefaultConsole = new ToolStripSeparator();
            separatorShowDefaultConsole.Visible = false;

            separatorServers = new ToolStripSeparator();
            separatorServers.Visible = false;

            miStartServers = new ToolStripMenuItem("&Start server");

            separatorStartServers = new ToolStripSeparator();

            ToolStripMenuItem miExit = new ToolStripMenuItem("E&xit");
            miExit.Click += MenuExit;

            trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add(miShowDefaultConsole);
            trayMenu.Items.Add(separatorShowDefaultConsole);
            trayMenu.Items.Add(separatorServers);
            trayMenu.Items.Add(miStartServers);
            trayMenu.Items.Add(separatorStartServers);
            trayMenu.Items.Add(miExit);  

            trayIcon = new NotifyIcon();
            trayIcon.Icon = Properties.Resources.IconServer;

            // Add menu to tray icon and show it.
            trayIcon.MouseDoubleClick += MenuShowDefaultConsole;
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
        /// <param name="configurationFile">Configuration file</param>
        /// <param name="runServers">Servers name to run</param>
        public void Initialize(string configurationFile, string[] runServers)
        {
            if (IsAlreadyRunning())
            {
                throw new ProgramException("Only one instance of Devel79 Tray is allowed.");
            }

            vboxServer = new VirtualBoxServer();

            ConfigurationReader configurationReader = new ConfigurationReader(this, vboxServer, configurationFile);
            servers = configurationReader.GetServers();

            if (servers.Count == 0)
            {
                throw new ProgramException("No server is defined in \"" + configurationReader.GetConfigurationFile() + "\".");
            }

            miServers = new Dictionary<Server, ToolStripMenuItem>();

            int position = trayMenu.Items.IndexOf(separatorShowDefaultConsole);
            foreach (Server server in servers.Values)
            {
                ToolStripMenuItem miServer = server.GetMenuServer(); // because server.GetMenuServer().Visible = true/false not working...
                miServers.Add(server, miServer);
                trayMenu.Items.Insert(++position, miServer);
                miStartServers.DropDownItems.Add(server.GetMenuStartServer());
            }

            trayIcon.Visible = true;

            try
            {
                // Initialize VirtualBox servers
                vboxServer.Initialize(servers);

                if (runServers.Length > 0)
                {
                    foreach (string runServer in runServers)
                    {
                        if (servers.ContainsKey(runServer.ToLower()))
                        {
                            servers[runServer.ToLower()].StartServer();
                        }
                        else
                        {
                            throw new ProgramException("Server to run \"" + runServer + "\" is not specified in configuration file \"" + configurationReader.GetConfigurationFile() + "\".");
                        }
                    }
                }
            }
            catch (ProgramException e)
            {
                trayIcon.Visible = false;
                throw e;
            }

            UpdateTray();
        }

        /// <summary>
        /// Update tray icon.
        /// </summary>
        public void UpdateTray()
        {
            Server defaultServer = null;
            bool isSomeServerPoweredOff = false;
            string title = "";

            foreach (Server server in servers.Values)
            {
                ToolStripMenuItem miServer = miServers[server];
                if (server.IsRunning())
                {
                    if (defaultServer == null)
                    {
                        defaultServer = server;
                    }
                    miServer.Visible = true;
                    miServer.Enabled = true;

                    title += server.GetName() + " | ";
                }
                else if (server.IsRestarting())
                {
                    miServer.Enabled = false;
                }
                else
                {
                    isSomeServerPoweredOff = true;
                    miServer.Visible = false;
                }
            }

            if (defaultServer == null) // No server is running...
            {
                trayIcon.Icon = iconStop;

                miShowDefaultConsole.Visible = false;
                separatorShowDefaultConsole.Visible = false;
                separatorServers.Visible = false;
            }
            else
            {
                trayIcon.Icon = iconRun;

                miShowDefaultConsole.Text = "Show " + defaultServer.GetName() + " &console";
                miShowDefaultConsole.Visible = true;
                separatorShowDefaultConsole.Visible = true;
                separatorServers.Visible = true;
            }

            if (isSomeServerPoweredOff)
            {
                miStartServers.Visible = true;
                separatorStartServers.Visible = true;
            }
            else
            {
                miStartServers.Visible = false;
                separatorStartServers.Visible = false;
            }

            trayIcon.Text = Devel79Tray.TRAY_TITLE + ((defaultServer == null) ? "" : (" [" + title.Substring(0, title.Length - 3) + "]"));

            this.defaultServer = defaultServer;
        }

        /// <summary>
        /// Close running server(s) on exit.
        /// </summary>
        public void CloseServersOnExit()
        {
            int runningServersCount = 0;
            string runningServers = "";
            foreach (Server server in servers.Values)
            {
                if (server.IsRunning())
                {
                    runningServersCount++;
                    runningServers += server.GetName() + ", ";
                }
            }
            if (runningServersCount > 0)
            {
                if (ShowQuestion((runningServersCount == 1) ? "Server is running" : "Some servers are running", "Do you want to stop " + runningServers.Substring(0, runningServers.Length - 2) + "?"))
                {
                    foreach (Server server in servers.Values)
                    {
                        if (server.GetStatus() == VirtualBoxServer.Status.RUNNING)
                        {
                            server.StopServer();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Show info in tray icon.
        /// </summary>
        /// <param name="caption">Info caption</param>
        /// <param name="text">Info text</param>
        public void ShowTrayInfo(string caption, string text)
        {
            ShowTrayBalloonTip(3000, caption, text, ToolTipIcon.Info);
        }

        /// <summary>
        /// Show info in tray and register click callback.
        /// </summary>
        /// <param name="caption">Info caption</param>
        /// <param name="text">Info text</param>
        /// <param name="callback">Click callback</param>
        public void ShowTrayInfo(string caption, string text, ICallable callback)
        {
            trayCallback = callback;
            ShowTrayBalloonTip(5000, caption, text, ToolTipIcon.Info);
        }

        /// <summary>
        /// Show warning in tray icon.
        /// </summary>
        /// <param name="caption">Warning caption</param>
        /// <param name="text">Warning text</param>
        public void ShowTrayWarning(string caption, string text)
        {
            ShowTrayBalloonTip(3000, caption, text, ToolTipIcon.Warning);
        }

        /// <summary>
        /// Show error in tray icon.
        /// </summary>
        /// <param name="caption">Error caption</param>
        /// <param name="text">Error text</param>
        public void ShowTrayError(string caption, string text)
        {
            ShowTrayBalloonTip(3000, caption, text, ToolTipIcon.Error);
        }

        /// <summary>
        /// Show tray icon balloon tip with icon.
        /// </summary>
        /// <param name="timeout">Balloon tip timeout in ms</param>
        /// <param name="caption">Balloon tip caption</param>
        /// <param name="text">Balloon tip text</param>
        /// <param name="icon">Balloon tip icon</param>
        private void ShowTrayBalloonTip(int timeout, string caption, string text, ToolTipIcon icon)
        {
            trayIcon.ShowBalloonTip(timeout, caption, text, icon);
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
        /// Clear click callback.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void BalloonTipClosed(object sender, EventArgs e)
        {
            trayCallback = null;
        }

        /// <summary>
        /// Show default server console menu.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuShowDefaultConsole(object sender, EventArgs e)
        {
            if (defaultServer == null)
            {
                return;
            }

            defaultServer.ShowConsole();
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
                    CloseServersOnExit();
                }

                vboxServer.Release();
            }
        }

        /// <summary>
        /// Show error message box.
        /// </summary>
        /// <param name="caption">Message box caption</param>
        /// <param name="text">Message box body</param>
        private static void ShowError(string caption, string text)
        {
            MessageBox.Show(text, caption, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        /// <summary>
        /// Show question box.
        /// </summary>
        /// <param name="caption">Question box caption</param>
        /// <param name="text">Question box body</param>
        /// <returns>Answer Yes=true, No=false</returns>
        private bool ShowQuestion(string caption, string text)
        {
            if (MessageBox.Show(text, caption, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Check if another instance of application is already running.
        /// </summary>
        /// <returns>True if another instance is running, false otherwise</returns>
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
