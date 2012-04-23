using System;
using System.Drawing;
using System.Windows.Forms;
using System.Text;
using System.Threading;
using System.Reflection;
using System.IO;

namespace Devel79Tray
{
    public class Devel79Tray : Form
    {
        private const string DEFAULT_CONFIGURATION_FILE = "devel79.conf";

        private NotifyIcon trayIcon = null;
        private ContextMenu trayMenu = null;

        private MenuItem miShowConsole = null;
        private MenuItem miHideConsole = null;
        private MenuItem miStart = null;
        private MenuItem miRestart = null;
        private MenuItem miStop = null;
        private MenuItem miTest = null;

        static Mutex oneInstanceMutex;
        
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

            ConfigurationReader configurationReader = new ConfigurationReader(Application.StartupPath + "\\" + configurationFile);

            Application.Run(new Devel79Tray(runServerAtStartup, configurationReader));
        }

        public Devel79Tray(bool runServerAtStartup, ConfigurationReader configurationReader)  
        {
            // Test only one instace is running...
            if (IsAlreadyRunning())
            {
                ShowError("Startup error", "Only one instance of Devel79 Tray is allowed.");
                Application.Exit();
            }

            // Read configuration...
            if (!configurationReader.Read())
            {
                ShowError("Configuration error", "Configuration file \"" + configurationReader.GetConfigurationFile() + "\" doesn't exist or can't be read.");
                Application.Exit();
            }

            // Prepare menu
            miShowConsole = new MenuItem();
            miShowConsole.Text = "Show console";
            miShowConsole.Visible = false;
            miShowConsole.Click += ShowConsoleMenu;

            miHideConsole = new MenuItem();
            miHideConsole.Text = "Hide console";
            miHideConsole.Visible = false;
            miHideConsole.Click += HideConsoleMenu;

            miStart = new MenuItem();
            miStart.Text = "Start server";
            miStart.Visible = false;
            miStart.Click += StartMenu;

            miStop = new MenuItem();
            miStop.Text = "Stop server";
            miStop.Visible = false;
            miStop.Click += StopMenu;

            miRestart = new MenuItem();
            miRestart.Text = "Restart server";
            miRestart.Visible = false;
            miRestart.Click += RestartMenu;

            miTest = new MenuItem();
            miTest.Text = "Test server";
            miTest.Visible = false;
            miTest.Click += TestMenu;

            trayMenu = new ContextMenu();
            trayMenu.MenuItems.Add(miShowConsole);
            trayMenu.MenuItems.Add(miHideConsole);
            trayMenu.MenuItems.Add(miStart);
            trayMenu.MenuItems.Add(miStop);
            trayMenu.MenuItems.Add(miRestart);
            trayMenu.MenuItems.Add(miTest);
            trayMenu.MenuItems.Add("-");
            trayMenu.MenuItems.Add("Exit", ExitMenu);  

            trayIcon = new NotifyIcon();
            trayIcon.Text = configurationReader.GetName();
            trayIcon.Icon = Properties.Resources.IconServer;

            // Add menu to tray icon and show it.
            trayIcon.MouseDoubleClick += ConsoleMenu;
            trayIcon.ContextMenu = trayMenu;
            trayIcon.Visible = true;

            // Run server
            //StartServer(runServerAtStartup);
        }

        private void ConsoleMenu(object sender, EventArgs e)
        {
            if (miShowConsole.Visible)
            {
                //ShowConsole();
            }
            else if (miHideConsole.Visible)
            {
                //HideConsole();
            }
        }

        private void ShowConsoleMenu(object sender, EventArgs e)
        {
            //ShowConsole();
        }

        private void HideConsoleMenu(object sender, EventArgs e)
        {
            //HideConsole();
        }

        private void StartMenu(object sender, EventArgs e)
        {
            //StartServer(true);
        }

        private void StopMenu(object sender, EventArgs e)
        {
            //if (MessageBox.Show("Do you realy want to stop " + NAME + "?", NAME + " [Stop]", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            //{
                //StopServer(false, false);
            //}
        }

        private void RestartMenu(object sender, EventArgs e)
        {
            //if (MessageBox.Show("Do you realy want to restart " + NAME + "?", NAME + " [Restart]", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            //{
            //    StopServer(true, false);
            //}
        }

        private void TestMenu(object sender, EventArgs e)
        {
            //if (serverIsRunning)
            //{
            //    TServer tServer = new TServer(this);
            //    Thread testServerThread = new Thread(tServer.Test);
            //    testServerThread.Start();
            //}
        }

        private void ExitMenu(object sender, EventArgs e)
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
        }

        public void ShowError(string caption, string text)
        {
            MessageBox.Show(text, caption, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

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

        protected override void OnLoad(EventArgs e)
        {
            Visible = false; // Hide form window.
            ShowInTaskbar = false; // Remove from taskbar.

            base.OnLoad(e);
        }

        protected override void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {
                // Release the icon resource.
                trayIcon.Dispose();
            }

            base.Dispose(isDisposing);
        }

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