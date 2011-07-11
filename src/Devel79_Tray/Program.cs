using System;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;
using System.Reflection;

namespace Devel79_Tray
{
    public class Devel79_Tray : Form
    {
        private string NAME = "Devel79 Server";
        private static string CONFIGURATION_FILE = "devel79.conf";

        public const int PROCESS_WAIT_FOR_EXIT = 5000;
        public const int STOP_WAIT_FOR_EXIT_SECONDS = 60;
        public const int STOP_WAIT_FOR_RUN = 100;
        public const int WAIT_BEFORE_FIRST_CHECK = 10000;

        private bool serverIsRunning = false;

        private static string vbDir = "";
        private static string vbMachine = "";
        private static string vbIP = "";
        private int vbCheckServer = 60000;

        private NotifyIcon trayIcon = null;
        private ContextMenu trayMenu = null;

        private MenuItem miShowConsole = null;
        private MenuItem miHideConsole = null;
        private MenuItem miStart = null;
        private MenuItem miRestart = null;
        private MenuItem miStop = null;
        private MenuItem miTest = null;

        TServer tServerCheck = null;
        Thread checkServerThread = null;

        int hWnd_Console = 0;

        private const int SW_HIDE = 0;
        private const int SW_NORMAL = 1;
        private const int SW_SHOW = 5;

        static Mutex mutex;
        
        [STAThread]
        static void Main(string[] args)
        {
            if (IsAlreadyRunning())
            {
                return;
            }

            bool runServerAtStartup = false;

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].ToLower() == "--runserver")
                {
                    runServerAtStartup = true;
                }
                else if ((args[i].ToLower() == "--config") && (args.Length > i))
                {
                    CONFIGURATION_FILE = args[++i];
                }
            }

            Application.Run(new Devel79_Tray(runServerAtStartup));
        }

        public Devel79_Tray(bool runServerAtStartup)  
        {
            // Read configuration
            if (!ReadConfiguration())
            {
                MessageBox.Show("Configuration file \"" + CONFIGURATION_FILE + "\" doesn't exist!", "Configuration error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit(); // END
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
            trayIcon.Text = NAME;
            trayIcon.Icon = new Icon(System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("Devel79_Tray.Icon_Server.ico"));

            // Add menu to tray icon and show it.
            trayIcon.MouseDoubleClick += ConsoleMenu;
            trayIcon.ContextMenu = trayMenu;
            trayIcon.Visible = true;

            // Run server
            StartServer(runServerAtStartup);
        }

        private void ConsoleMenu(object sender, EventArgs e)
        {
            if (miShowConsole.Visible)
            {
                ShowConsole();
            }
            else if (miHideConsole.Visible)
            {
                HideConsole();
            }
        }

        private void ShowConsoleMenu(object sender, EventArgs e)
        {
            ShowConsole();
        }

        private void HideConsoleMenu(object sender, EventArgs e)
        {
            HideConsole();
        }

        private void StartMenu(object sender, EventArgs e)
        {
            StartServer(true);
        }

        private void StopMenu(object sender, EventArgs e)
        {
            if (MessageBox.Show("Do you realy want to stop " + NAME + "?", NAME + " [Stop]", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                StopServer(false, false);
            }
        }

        private void RestartMenu(object sender, EventArgs e)
        {
            if (MessageBox.Show("Do you realy want to restart " + NAME + "?", NAME + " [Restart]", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                StopServer(true, false);
            }
        }

        private void TestMenu(object sender, EventArgs e)
        {
            if (serverIsRunning)
            {
                TServer tServer = new TServer(this);
                Thread testServerThread = new Thread(tServer.Test);
                testServerThread.Start();
            }
        }

        private void ExitMenu(object sender, EventArgs e)
        {
            if (serverIsRunning && (MessageBox.Show("Do you want to stop " + NAME + "?", NAME + " [Stop]", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes))
            {
                StopServer(false, true);
            }
            else
            {
                if (serverIsRunning)
                {
                    ShowConsole();
                }

                CloseApp();
            }
        }

        private void StartServer(bool startServer)
        {
            if (!serverIsRunning)
            {
                if (tServerCheck != null)
                {
                    tServerCheck.RunCheck(true);
                }

                TServer tServer = new TServer(this);

                if (startServer)
                {
                    tServer.StartServer();
                }

                Thread startServerThread = new Thread(tServer.Run);
                startServerThread.Start();
            }
        }

        private void StopServer(bool restart, bool exitApp)
        {
            if (serverIsRunning)
            {
                if (tServerCheck != null)
                {
                    tServerCheck.RunCheck(false);
                }

                TServer tServer = new TServer(this);

                if (restart)
                {
                    tServer.RestartServer();
                }
                else if (exitApp)
                {
                    tServer.ExitApp();
                }

                Thread stopServerThread = new Thread(tServer.Stop);
                stopServerThread.Start();
            }
        }

        public void SetServerStatus(bool run, bool showErrorInfo)
        {
            miStart.Visible = !run;
            miShowConsole.Visible = run;
            miHideConsole.Visible = false;
            miStop.Visible = run;
            miRestart.Visible = run;
            miTest.Visible = run;

            serverIsRunning = run;

            if (run)
            {
                TConsole tConsole = new TConsole(this);
                Thread hideConsoleThread = new Thread(tConsole.Hide);
                hideConsoleThread.Start();

                trayIcon.Icon = new Icon(System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("Devel79_Tray.Icon_Server_Run.ico"));
                trayIcon.ShowBalloonTip(3000, NAME + " [Run]", NAME + " successfully started...", ToolTipIcon.Info);
                trayIcon.Text = NAME + " [Run]";

                if (checkServerThread == null)
                {
                    tServerCheck = new TServer(this);
                    tServerCheck.SetCheckTime(vbCheckServer);
                    tServerCheck.RunCheck(true);
                    checkServerThread = new Thread(tServerCheck.Check);
                    checkServerThread.Start();
                }
            }
            else
            {
                hWnd_Console = 0;

                trayIcon.Icon = new Icon(System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("Devel79_Tray.Icon_Server_Stop.ico"));
                if(showErrorInfo)
                {
                    trayIcon.ShowBalloonTip(3000, NAME + " [Stop]", NAME + " failed to start...", ToolTipIcon.Error);
                }
                trayIcon.Text = NAME + " [Stop]";
            }
        }

        private void CancelThreads()
        {
            if ((checkServerThread != null) && checkServerThread.IsAlive)
            {
                checkServerThread.Abort();
            }

            checkServerThread = null;
        }

        public static bool TestRunning()
        {
            if (File.Exists(vbDir + "\\VBoxManage.exe"))
            {
                Process testServer = new Process();
                testServer.StartInfo.FileName = vbDir + "\\VBoxManage.exe";
                testServer.StartInfo.Arguments = "list runningvms";
                testServer.StartInfo.UseShellExecute = false;
                testServer.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                testServer.StartInfo.CreateNoWindow = true;
                testServer.StartInfo.RedirectStandardOutput = true;
                testServer.Start();

                string output = testServer.StandardOutput.ReadToEnd();

                testServer.WaitForExit(PROCESS_WAIT_FOR_EXIT);
                testServer.Close();

                return output.Contains("\"" + vbMachine + "\"");
            }
            else
            {
                return false;
            }
        }

        public static bool TestPing()
        {
            bool result = false;

            Ping pingSender = new Ping();
            PingOptions options = new PingOptions();
            options.DontFragment = true;
            byte[] buffer = Encoding.ASCII.GetBytes("ping");
            int timeout = 120;
            PingReply reply = pingSender.Send(vbIP, timeout, buffer, options);

            if (reply.Status == IPStatus.Success)
            {
                result = true;
            }

            return result;
        }

        private bool ReadConfiguration()
        {
            if (File.Exists(Application.StartupPath + "\\" + CONFIGURATION_FILE))
            {
                StreamReader conf = File.OpenText(Application.StartupPath + "\\" + CONFIGURATION_FILE);
                string line = null;

                while ((line = conf.ReadLine()) != null)
                {
                    line = line.Trim();

                    if ((line == "") || (line[0] == '#'))
                    {
                        continue;
                    }

                    string[] settings = new string[2];
                    settings = line.Split('=');

                    string key = settings[0].Trim().ToLower();
                    string value = settings[1].Trim();

                    switch (key)
                    {
                        case "name" :
                            NAME = value;
                            break;
                        case "directory" :
                            vbDir = value;
                            break;
                        case "machine" :
                            vbMachine = value;
                            break;
                        case "ip" :
                            vbIP = value;
                            break;
                        case "checktime" :
                            int intValue = vbCheckServer;

                            try
                            {
                                intValue = Int16.Parse(value);
                            }
                            catch
                            {
                                intValue = vbCheckServer;
                            }
                            
                            vbCheckServer = intValue * 1000;
                            break;
                    }
                }
                conf.Close();

                return true;
            }

            return false;
        }

        public string GetName()
        {
            return NAME;
        }

        public string GetVBMachine()
        {
            return vbMachine;
        }

        public string GetVBIP()
        {
            return vbIP;
        }

        public string GetVBDir()
        {
            return vbDir;
        }

        public NotifyIcon GetTrayIcon()
        {
            return trayIcon;
        }

        public void SetHWnd_Console(int hWnd)
        {
            hWnd_Console = hWnd;
        }

        public void HideConsole()
        {
            if (hWnd_Console != 0)
            {
                miHideConsole.Visible = false;
                miShowConsole.Visible = true;

                ShowWindow(hWnd_Console, SW_HIDE);
            }
        }

        public void ShowConsole()
        {
            if (hWnd_Console != 0)
            {
                miShowConsole.Visible = false;
                miHideConsole.Visible = true;

                ShowWindow(hWnd_Console, SW_NORMAL);
                ShowWindow(hWnd_Console, SW_SHOW);
                SetForegroundWindow(new IntPtr(hWnd_Console));
            }
        }

        public void CloseApp()
        {
            CancelThreads();

            Application.Exit();
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
            // Devel79_Tray
            // 
            this.ClientSize = new System.Drawing.Size(292, 273);
            this.Name = "Devel79_Tray";
            this.ResumeLayout(false);
        }

        [DllImport("User32")]
        private static extern int ShowWindow(int hwnd, int nCmdShow);

        [DllImport("user32")]
        public static extern bool SetForegroundWindow(IntPtr hwnd);

        private static bool IsAlreadyRunning()
        {
            string strLoc = Assembly.GetExecutingAssembly().Location;

            FileSystemInfo fileInfo = new FileInfo(strLoc);
            string sExeName = fileInfo.Name;
            mutex = new Mutex(true, sExeName);

            if (mutex.WaitOne(0, false))
            {
                return false;
            }
            return true;
        }
    }

    public class TServer
    {
        private Devel79_Tray devel = null;

        private bool startServer = false;
        private bool restartServer = false;
        private bool exitApp = false;
        private bool check = false;
        private int checkTime = 0;

        private bool vbDirExists = false;

        public TServer(Devel79_Tray devel)
        {
            this.devel = devel;
            this.vbDirExists = File.Exists(devel.GetVBDir() + "\\VBoxManage.exe");
        }

        public void Run()
        {
            bool run = false;

            if (vbDirExists)
            {
                if (Devel79_Tray.TestRunning())
                {
                    run = true;
                }
                else if(startServer)
                {
                    Process pRunServer = new Process();
                    pRunServer.StartInfo.FileName = devel.GetVBDir() + "\\VBoxManage.exe";
                    pRunServer.StartInfo.Arguments = "startvm " + devel.GetVBMachine();
                    pRunServer.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    pRunServer.StartInfo.CreateNoWindow = true;

                    pRunServer.Start();

                    pRunServer.WaitForExit(Devel79_Tray.PROCESS_WAIT_FOR_EXIT);

                    int exitCode = pRunServer.ExitCode;

                    pRunServer.Close();

                    if (exitCode == 0)
                    {
                        run = true;
                    }
                }
            }

            devel.SetServerStatus(run, startServer);
        }

        public void Stop()
        {
            if (vbDirExists)
            {
                Process pStopServer = new Process();
                pStopServer.StartInfo.FileName = devel.GetVBDir() + "\\VBoxManage.exe";
                pStopServer.StartInfo.Arguments = "controlvm " + devel.GetVBMachine() + " acpipowerbutton";
                pStopServer.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                pStopServer.Start();
                pStopServer.WaitForExit(Devel79_Tray.PROCESS_WAIT_FOR_EXIT);
                pStopServer.Close();

                for (int i = 0; i < Devel79_Tray.STOP_WAIT_FOR_EXIT_SECONDS; i++)
                {
                    if (!Devel79_Tray.TestRunning())
                    {
                        break;
                    }
                    Thread.Sleep(1000);
                }
            }

            if (exitApp)
            {
                devel.CloseApp();
            }
            else
            {

                devel.SetServerStatus(false, false);

                devel.GetTrayIcon().ShowBalloonTip(3000, devel.GetName() + " [Stop]", devel.GetName() + " was successfully stopped...", ToolTipIcon.Info);

                if (restartServer)
                {
                    startServer = true;
                    Run();
                }
            }
        }

        public void Test()
        {
            bool run = Devel79_Tray.TestRunning();
            bool ping = Devel79_Tray.TestPing();

            if (!ping && !run)
            {
                devel.GetTrayIcon().ShowBalloonTip(3000, devel.GetName() + " [Test]", "The virtual machnie \"" + devel.GetVBMachine() + "\" isn't running and ping to VirtualBox Hosts adapter \"" + devel.GetVBIP() + "\" failed!", ToolTipIcon.Error);
            }
            else if (!ping)
            {
                devel.GetTrayIcon().ShowBalloonTip(3000, devel.GetName() + " [Test]", "The virtual machine \"" + devel.GetVBMachine() + "\" is running, but ping to VirtualBox Hosts adapter \"" + devel.GetVBIP() + "\" failed!", ToolTipIcon.Warning);
            }
            else if (!run)
            {
                devel.GetTrayIcon().ShowBalloonTip(3000, devel.GetName() + " [Test]", "The virtual machnie \"" + devel.GetVBMachine() + "\" isn't running, but ping to VirtualBox Hosts adapter \"" + devel.GetVBMachine() + "\" has been successful!", ToolTipIcon.Error);
            }
            else
            {
                devel.GetTrayIcon().ShowBalloonTip(3000, devel.GetName() + " [Test]", "The virtual machine \"" + devel.GetVBMachine() + "\" is running and ping to VirtualBox Hosts adapter \"" + devel.GetVBMachine() + "\" has been successful!", ToolTipIcon.Info);
            }

            if (!run)
            {
                devel.SetServerStatus(false, false);
            }
        }

        public void Check()
        {
            try
            {
                bool first = true;

                while (true)
                {
                    if(check) {
                        if (first)
                        {
                            first = false;
                            Thread.Sleep(Devel79_Tray.WAIT_BEFORE_FIRST_CHECK); // Nez se spusti prvni kontrola, cekej 15 vterin...
                        }

                        if (!Devel79_Tray.TestRunning())
                        {
                            devel.GetTrayIcon().ShowBalloonTip(3000, devel.GetName() + " [Stop]", devel.GetName() + " isn't running...", ToolTipIcon.Error);
                            devel.SetServerStatus(false, false);
                            check = false;
                        }
                    }
                    else if (!first)
                    {
                        first = true;
                    }


                    Thread.Sleep(checkTime);
                }
            }
            catch
            {
            }
        }

        public void StartServer()
        {
            startServer = true;
        }

        public void RestartServer()
        {
            restartServer = true;
        }

        public void ExitApp()
        {
            exitApp = true;
        }

        public void RunCheck(bool run)
        {
            check = run;
        }

        public void SetCheckTime(int time)
        {
            checkTime = time;
        }
    }

    public class TConsole
    {
        private Devel79_Tray devel = null;

        public TConsole(Devel79_Tray devel)
        {
            this.devel = devel;
        }

        public void Hide()
        {
            for (int i = 0; i < Devel79_Tray.STOP_WAIT_FOR_RUN; i++)
            {
                if (Devel79_Tray.TestRunning())
                {
                    break;
                }
                Thread.Sleep(100);
            }

            Process[] processRunning = Process.GetProcesses();
            foreach (Process process in processRunning)
            {
                if ((process.ProcessName.ToLower() == "virtualbox") && (process.MainWindowTitle.ToLower().Contains(devel.GetVBMachine().ToLower())))
                {
                    devel.SetHWnd_Console(process.MainWindowHandle.ToInt32());
                    devel.HideConsole();

                    break;
                }
            }
        }
    }
}