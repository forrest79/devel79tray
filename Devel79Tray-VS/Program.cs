using System;
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
        /// Defatul configuration file name.
        /// </summary>
        private const string DEFAULT_CONFIGURATION_FILE = "devel79.conf";

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
            bool runServerAtStartup = false;
            string configurationFile = Application.StartupPath + "\\" + DEFAULT_CONFIGURATION_FILE;

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].ToLower().Equals("--runserver") || args[i].ToLower().Equals("-r"))
                {
                    runServerAtStartup = true;
                }
                else if ((args[i].ToLower().Equals("--config") || args[i].ToLower().Equals("-c")) && (args.Length > i))
                {
                    configurationFile = args[++i];
                }
            }

            
            Devel79Tray devel79Tray = null;
            
            try
            {
                devel79Tray = new Devel79Tray();
                devel79Tray.Initialize(runServerAtStartup, new ConfigurationReader(configurationFile));
            }
            catch (Exception e)
            {
                ShowError("Inicialization error", e.Message);
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
        /// <param name="runServerAtStartup">Run server at startup.</param>
        /// <param name="configurationReader">Configuration read from file.</param>
        public void Initialize(bool runServerAtStartup, ConfigurationReader configurationReader)
        {
            // Test only one instace is running...
            if (IsAlreadyRunning())
            {
                throw new Exception("Only one instance of Devel79 Tray is allowed.");
            }

            // Read configuration...
            if (!configurationReader.Read())
            {
                throw new Exception("Configuration file \"" + configurationReader.GetConfigurationFile() + "\" doesn't exist or can't be read.");
            }

            // Create VirtualBox machine
            vboxServer = new VirtualBoxServer(this, configurationReader.GetName(), configurationReader.GetMachine(), configurationReader.GetIp(), configurationReader.getSsh());

            // Create email monitor service
            emailMonitor = new EmailMonitor(this, configurationReader.getEmail());

            // Tray update
            trayIcon.Text = configurationReader.GetName();
            miOpenEmailDirectorySeparator.Visible = emailMonitor.IsActive();
            miOpenEmailDirectory.Visible = emailMonitor.IsActive();
            trayIcon.Visible = true;

            // Initialize VirtualBox machine
            try
            {
                vboxServer.Initialize(runServerAtStartup);
            }
            catch (Exception e)
            {
                trayIcon.Visible = false;
                throw e;
            }
        }

        /// <summary>
        /// Show console.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuShowConsole(object sender, EventArgs e)
        {
            vboxServer.ShowConsole();
        }

        /// <summary>
        /// Start server.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuStartServer(object sender, EventArgs e)
        {
            try
            {
                vboxServer.StartServer();
            }
            catch (Exception ex) 
            {
                ShowError("Error", ex.Message);
            }
        }

        /// <summary>
        /// Stop server.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuStopServer(object sender, EventArgs e)
        {
            try
            {
                vboxServer.StopServer();
            }
            catch (Exception ex)
            {
                ShowError("Error", ex.Message);
            }
        }

        /// <summary>
        /// Restart server.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuRestartServer(object sender, EventArgs e)
        {
            try
            {
                vboxServer.RestartServer();
            }
            catch (Exception ex)
            {
                ShowError("Error", ex.Message);
            }
        }

        /// <summary>
        /// Ping server.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuPingServer(object sender, EventArgs e)
        {
            vboxServer.PingServer();
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
            this.ClientSize = new System.Drawing.Size(116, 100);
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
}