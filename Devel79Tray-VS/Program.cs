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
        /// Menu Hide console.
        /// </summary>
        private ToolStripMenuItem miHideConsole;
        
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
        /// Main method.
        /// </summary>
        /// <param name="args">Command line arguments</param>
        [STAThread]
        static void Main(string[] args)
        {
            bool runServerAtStartup = false;
            string configurationFile = DEFAULT_CONFIGURATION_FILE;

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
                devel79Tray = new Devel79Tray(runServerAtStartup, new ConfigurationReader(Application.StartupPath + "\\" + configurationFile));
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
        /// <param name="runServerAtStartup">Run server at startup.</param>
        /// <param name="configurationReader">Configuration read from file.</param>
        public Devel79Tray(bool runServerAtStartup, ConfigurationReader configurationReader)  
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
            vboxServer = new VirtualBoxServer(this, configurationReader.GetName(), configurationReader.GetMachine(), configurationReader.GetIp());

            // Prepare menu
            miShowConsole = new ToolStripMenuItem("Show &console");
            miShowConsole.Font = new Font(miShowConsole.Font, miShowConsole.Font.Style | FontStyle.Bold);
            miShowConsole.Visible = false;
            miShowConsole.Click += MenuShowConsole;

            miHideConsole = new ToolStripMenuItem("Hide &console");
            miHideConsole.Font = new Font(miShowConsole.Font, miShowConsole.Font.Style | FontStyle.Bold);
            miHideConsole.Visible = false;
            miHideConsole.Click += MenuHideConsole;

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

            ToolStripMenuItem miExit = new ToolStripMenuItem("E&xit");
            miExit.Click += MenuExit;

            trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add(miShowConsole);
            trayMenu.Items.Add(miHideConsole);
            trayMenu.Items.Add(miStartServer);
            trayMenu.Items.Add(miStopServer);
            trayMenu.Items.Add(miRestartServer);
            trayMenu.Items.Add(miPingServer);
            trayMenu.Items.Add("-");
            trayMenu.Items.Add(miExit);  

            trayIcon = new NotifyIcon();
            trayIcon.Text = configurationReader.GetName();
            trayIcon.Icon = Properties.Resources.IconServer;

            // Add menu to tray icon and show it.
            trayIcon.MouseDoubleClick += ToggleConsole;
            trayIcon.ContextMenuStrip = trayMenu;
            trayIcon.Visible = true;

            // Load icons
            iconRun = Properties.Resources.IconServerRun;
            iconStop = Properties.Resources.IconServerStop;

            // Initialize VirtualBox machine
            try
            {
                vboxServer.Initialize(runServerAtStartup);
            }
            catch (Exception e)
            {
                throw e;
            }
        }
        
        /// <summary>
        /// Show or hide console.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ToggleConsole(object sender, EventArgs e)
        {
            vboxServer.ToggleConsole();
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
        /// Hide console.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuHideConsole(object sender, EventArgs e)
        {
            vboxServer.HideConsole();
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
        /// Exit application.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuExit(object sender, EventArgs e)
        {
            Application.Exit();
        }

        /// <summary>
        /// Callback called when console is hided.
        /// </summary>
        public void SetConsoleHidden()
        {
            if (trayMenu.InvokeRequired)
            {
                SetCallback callback = new SetCallback(SetConsoleHidden);
                this.Invoke(callback);
            }
            else
            {
                miShowConsole.Visible = true;
                miHideConsole.Visible = false;
            }
        }

        /// <summary>
        /// Callback called when console is showen.
        /// </summary>
        public void SetConsoleShown()
        {
            if (trayMenu.InvokeRequired)
            {
                SetCallback callback = new SetCallback(SetConsoleShown);
                this.Invoke(callback);
            }
            else
            {
                miShowConsole.Visible = false;
                miHideConsole.Visible = true;
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
                miHideConsole.Visible = false;
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
            ShowTrayBalloonTip(caption, text, ToolTipIcon.Info);
        }

        /// <summary>
        /// Show warning in tray icon.
        /// </summary>
        /// <param name="caption">Warning caption.</param>
        /// <param name="text">Warning text.</param>
        public void ShowTrayWarning(string caption, string text)
        {
            ShowTrayBalloonTip(caption, text, ToolTipIcon.Warning);
        }

        /// <summary>
        /// Show error in tray icon.
        /// </summary>
        /// <param name="caption">Error caption.</param>
        /// <param name="text">Error text.</param>
        public void ShowTrayError(string caption, string text)
        {
            ShowTrayBalloonTip(caption, text, ToolTipIcon.Error);
        }

        /// <summary>
        /// Show tray icon balloon tip with icon.
        /// </summary>
        /// <param name="caption">Balloon tip caption.</param>
        /// <param name="text">Balloon tip text.</param>
        /// <param name="icon">Balloon tip icon.</param>
        private void ShowTrayBalloonTip(string caption, string text, ToolTipIcon icon)
        {
            trayIcon.ShowBalloonTip(3000, caption, text, icon);
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
}