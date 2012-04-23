using System;
using System.Collections.Generic;
using System.Text;
using VirtualBox;
using System.Net.NetworkInformation;

namespace Devel79Tray
{
    public class VirtualBoxMachine
    {
        private Devel79Tray tray;

        private string name;

        private string ip;

        //TServer tServerCheck = null;
        //Thread checkServerThread = null;

        int hWnd_Console = 0;

        private const int SW_HIDE = 0;
        private const int SW_NORMAL = 1;
        private const int SW_SHOW = 5;

        public const int PROCESS_WAIT_FOR_EXIT = 5000;
        public const int STOP_WAIT_FOR_EXIT_SECONDS = 60;
        public const int STOP_WAIT_FOR_RUN = 100;
        public const int WAIT_BEFORE_FIRST_CHECK = 10000;

        private bool serverIsRunning = false;

        private static string vbMachine = "";
        private static string vbIP = "";
        private int vbCheckServer = 60000;

        public VirtualBoxMachine(Devel79Tray tray, String name, String ip)
        {
            this.tray = tray;
            this.name = name;
            this.ip = ip;

            IVirtualBox vbox = new VirtualBoxClass();
            try
            {
                //Session session = new SessionClass();
                //IMachine machine = vbox.FindMachine("Test");
                //IProgress progress = machine.LaunchVMProcess(session, "gui", "");
            }
            catch (Exception e)
            {
                //MessageBox.Show(e.Message);
            }
        }
        /*
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

        public bool TestPing()
        {
            bool result = false;

            Ping pingSender = new Ping();
            PingOptions options = new PingOptions();
            options.DontFragment = true;
            byte[] buffer = Encoding.ASCII.GetBytes("ping");
            int timeout = 120;
            PingReply reply = pingSender.Send(ip, timeout, buffer, options);

            if (reply.Status == IPStatus.Success)
            {
                result = true;
            }

            return result;
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

                trayIcon.Icon = new Icon(System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("Devel79Tray.Icon_Server_Run.ico"));
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

                trayIcon.Icon = new Icon(System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("Devel79Tray.Icon_Server_Stop.ico"));
                if (showErrorInfo)
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

        [DllImport("User32")]
        private static extern int ShowWindow(int hwnd, int nCmdShow);

        [DllImport("user32")]
        public static extern bool SetForegroundWindow(IntPtr hwnd);

    }
    public class TServer
    {
        private Devel79Tray devel = null;

        private bool startServer = false;
        private bool restartServer = false;
        private bool exitApp = false;
        private bool check = false;
        private int checkTime = 0;

        private bool vbDirExists = false;

        public TServer(Devel79Tray devel)
        {
            this.devel = devel;
            this.vbDirExists = File.Exists(devel.GetVBDir() + "\\VBoxManage.exe");
        }

        public void Run()
        {
            bool run = false;

            if (vbDirExists)
            {
                if (Devel79Tray.TestRunning())
                {
                    run = true;
                }
                else if (startServer)
                {
                    Process pRunServer = new Process();
                    pRunServer.StartInfo.FileName = devel.GetVBDir() + "\\VBoxManage.exe";
                    pRunServer.StartInfo.Arguments = "startvm " + devel.GetVBMachine();
                    pRunServer.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    pRunServer.StartInfo.CreateNoWindow = true;

                    pRunServer.Start();

                    pRunServer.WaitForExit(Devel79Tray.PROCESS_WAIT_FOR_EXIT);

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
                pStopServer.WaitForExit(Devel79Tray.PROCESS_WAIT_FOR_EXIT);
                pStopServer.Close();

                for (int i = 0; i < Devel79Tray.STOP_WAIT_FOR_EXIT_SECONDS; i++)
                {
                    if (!Devel79Tray.TestRunning())
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
            bool run = Devel79Tray.TestRunning();
            bool ping = Devel79Tray.TestPing();

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
                    if (check)
                    {
                        if (first)
                        {
                            first = false;
                            Thread.Sleep(Devel79Tray.WAIT_BEFORE_FIRST_CHECK); // Nez se spusti prvni kontrola, cekej 15 vterin...
                        }

                        if (!Devel79Tray.TestRunning())
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
        private Devel79Tray devel = null;

        public TConsole(Devel79Tray devel)
        {
            this.devel = devel;
        }

        public void Hide()
        {
            for (int i = 0; i < Devel79Tray.STOP_WAIT_FOR_RUN; i++)
            {
                if (Devel79Tray.TestRunning())
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
         */
    }
}
