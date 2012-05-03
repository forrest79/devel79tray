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

        private VirtualBoxServer vboxServer;

        /// <summary>
        /// 
        /// </summary>
        private NotifyIcon trayIcon;
        
        /// <summary>
        /// 
        /// </summary>
        private ContextMenuStrip trayMenu;

        /// <summary>
        /// 
        /// </summary>
        private ToolStripMenuItem miShowConsole;
        
        /// <summary>
        /// 
        /// </summary>
        private ToolStripMenuItem miHideConsole;
        
        /// <summary>
        /// 
        /// </summary>
        private ToolStripMenuItem miStartServer;
        
        /// <summary>
        /// 
        /// </summary>
        private ToolStripMenuItem miRestartServer;
        
        /// <summary>
        /// 
        /// </summary>
        private ToolStripMenuItem miStopServer;
        
        /// <summary>
        /// 
        /// </summary>
        private ToolStripMenuItem miPingServer;

        /// <summary>
        /// 
        /// </summary>
        private Icon iconRun;

        /// <summary>
        /// 
        /// </summary>
        private Icon iconStop;

        /// <summary>
        /// 
        /// </summary>
        static Mutex oneInstanceMutex;

        /// <summary>
        /// 
        /// </summary>
        public delegate void SetCallback();
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
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

            Devel79Tray devel79Tray;

            try
            {
                devel79Tray = new Devel79Tray(runServerAtStartup, new ConfigurationReader(Application.StartupPath + "\\" + configurationFile));
            }
            catch
            {
                return;
            }

            Application.Run(devel79Tray);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="runServerAtStartup"></param>
        /// <param name="configurationReader"></param>
        public Devel79Tray(bool runServerAtStartup, ConfigurationReader configurationReader)  
        {
            // Test only one instace is running...
            if (IsAlreadyRunning())
            {
                ShowError("Startup error", "Only one instance of Devel79 Tray is allowed.");
                throw new Exception();
            }

            // Read configuration...
            if (!configurationReader.Read())
            {
                ShowError("Configuration error", "Configuration file \"" + configurationReader.GetConfigurationFile() + "\" doesn't exist or can't be read.");
                throw new Exception();
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
                ShowError("Error", e.Message);
                throw e;
            }
        }

        private void ToggleConsole(object sender, EventArgs e)
        {
            vboxServer.ToggleConsole();
        }

        private void MenuShowConsole(object sender, EventArgs e)
        {
            vboxServer.ShowConsole();
        }

        private void MenuHideConsole(object sender, EventArgs e)
        {
            vboxServer.HideConsole();
        }

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

        private void MenuPingServer(object sender, EventArgs e)
        {
            vboxServer.PingServer();
        }

        private void MenuExit(object sender, EventArgs e)
        {
            //if (serverIsRunning && (MessageBox.Show("Do you want to stop " + NAME + "?", NAME + " [Stop]", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes))
            //{
            //    StopServer(false, true);
            //}
            //else
            //{
            //    if (serverIsRunning)
            //    {
            //        ShowConsole();
            //    }

            //    CloseApp();
            //}
            Application.Exit();
        }

        /// <summary>
        /// 
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
        /// 
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
        /// 
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
                miStartServer.Visible = true;
                miStopServer.Visible = false;
                miRestartServer.Visible = false;
                miPingServer.Visible = false;
            }
        }

        /// <summary>
        /// 
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
        /// 
        /// </summary>
        /// <param name="caption"></param>
        /// <param name="text"></param>
        public void ShowError(string caption, string text)
        {
            MessageBox.Show(text, caption, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        public void ShowTrayInfo(string caption, string text)
        {
            ShowTrayBalloonTip(caption, text, ToolTipIcon.Info);
        }

        public void ShowTrayWarning(string caption, string text)
        {
            ShowTrayBalloonTip(caption, text, ToolTipIcon.Warning);
        }

        public void ShowTrayError(string caption, string text)
        {
            ShowTrayBalloonTip(caption, text, ToolTipIcon.Error);
        }

        private void ShowTrayBalloonTip(string caption, string text, ToolTipIcon icon)
        {
            trayIcon.ShowBalloonTip(3000, caption, text, icon);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private static bool IsAlreadyRunning()
        {
            string strLoc = Assembly.GetExecutingAssembly().Location;

            FileSystemInfo fileInfo = new FileInfo(strLoc);
            string sExeName = fileInfo.Name;
            oneInstanceMutex = new Mutex(true, sExeName);

            if (oneInstanceMutex.WaitOne(0, false))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e)
        {
            Visible = false; // Hide form window.
            ShowInTaskbar = false; // Remove from taskbar.

            base.OnLoad(e);
        }

        /// <summary>
        /// 
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
        /// 
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

    }
}