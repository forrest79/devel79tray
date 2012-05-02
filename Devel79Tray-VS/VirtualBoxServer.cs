using System;
using System.Net.NetworkInformation;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;
using VirtualBox;

namespace Devel79Tray
{
    public class VirtualBoxServer
    {
        private enum Status
        {
            NONE,
            POWEREDOFF,
            STARTING,
            RUNNING,
            STOPING
        }

        private Devel79Tray tray;

        private string name;

        private string machine;

        private string ip;

        private IVirtualBox vbox;

        private IMachine vboxMachine;

        private Session serverSession;

        private VirtualBoxEventListener eventListener;

        private Status status;
        
        private int consoleHWnd;

        private bool consoleVisible;

        private const int SW_HIDE = 0;
        private const int SW_NORMAL = 1;
        private const int SW_SHOW = 5;

        //TServer tServerCheck = null;
        //Thread checkServerThread = null;

        public VirtualBoxServer(Devel79Tray tray, String name, String machine, String ip)
        {
            this.tray = tray;
            this.name = name;
            this.machine = machine;
            this.ip = ip;

            this.status = Status.NONE;

            this.consoleVisible = false;
        }

        public void Initialize(bool runServer)
        {
            ClearConsoleHWnd();

            try
            {
                vbox = new VirtualBoxClass();
            }
            catch
            {
                throw new Exception("Error while connecting to VirtualBox.");
            }

            try
            {
                vboxMachine = vbox.FindMachine(machine);
            }
            catch
            {
                throw new Exception("Machine '" + machine + "' not found.");
            }

            changeState(vboxMachine.State);

            eventListener = new VirtualBoxEventListener(this);
            vbox.EventSource.RegisterListener(eventListener, new VBoxEventType[] { VBoxEventType.VBoxEventType_OnMachineStateChanged }, 1);
        }

        public void Release()
        {
            if (eventListener != null)
            {
                vbox.EventSource.UnregisterListener(eventListener);
            }
        }

        public void changeState(MachineState state)
        {
            Status newStatus = status;
            Console.WriteLine(state);
            switch (state)
            {
                case MachineState.MachineState_Running:
                    newStatus = Status.RUNNING;
                    break;
                case MachineState.MachineState_Starting:
                case MachineState.MachineState_Restoring:
                    newStatus = Status.STARTING;
                    break;
                case MachineState.MachineState_Stopping:
                case MachineState.MachineState_Saving:
                    newStatus = Status.STOPING;
                    break;
                default :
                    ClearConsoleHWnd();
                    newStatus = Status.POWEREDOFF;
                    break;
            }

            if (newStatus != status)
            {
                if (newStatus == Status.POWEREDOFF)
                {
                    tray.SetServerPoweredOff();
                }
                else if (newStatus == Status.RUNNING)
                {
                    if (!consoleVisible)
                    {
                        HideConsole();
                    }

                    tray.SetServerRunning();
                }

                status = newStatus;
            }
        }

        public void StartServer()
        {
            if (status != Status.POWEREDOFF)
            {
                throw new Exception("Server " + name + " is not powered off.");
            }

            try
            {
                serverSession = new SessionClass();
                IProgress progress = vboxMachine.LaunchVMProcess(serverSession, "gui", "");
            }
            catch
            {
                throw new Exception("Server " + name + " can't be run.");
            }
        }

        public void StopServer()
        {
            if (status != Status.RUNNING)
            {
                throw new Exception("Server " + name + " is not running.");
            }

            if (serverSession == null)
            {
                serverSession = new SessionClass();
            }

            vboxMachine.LockMachine(serverSession, LockType.LockType_Shared);
            serverSession.Console.PowerButton();
            serverSession.UnlockMachine();
        }

        public bool RestartServer()
        {
            return false;
        }

        public bool PingServer()
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

        public void ShowConsole()
        {
            //ShowWindow(GetConsoleHWnd(), SW_NORMAL);
            //ShowWindow(GetConsoleHWnd(), SW_SHOW);
            //SetForegroundWindow(new IntPtr(GetConsoleHWnd()));
            if (vboxMachine.CanShowConsoleWindow() != 0)
            {
                tray.ShowError("Error", "Window");
                vboxMachine.ShowConsoleWindow();
            }

            consoleVisible = true;
            tray.SetConsoleShown();
        }

        public void HideConsole()
        {
            ShowWindow(GetConsoleHWnd(), SW_HIDE);

            consoleVisible = false;
            tray.SetConsoleHidden();
        }

        public void ToggleConsole()
        {
            if (consoleVisible)
            {
                HideConsole();
            }
            else
            {
                ShowConsole();
            }
        }

        private int GetConsoleHWnd()
        {
            if (consoleHWnd == 0)
            {
                Process[] processRunning = Process.GetProcesses();
                foreach (Process process in processRunning)
                {
                    if ((process.ProcessName.ToLower() == "virtualbox") && (process.MainWindowTitle.ToLower().Contains(machine.ToLower())))
                    {
                        consoleHWnd = process.MainWindowHandle.ToInt32();
                        break;
                    }
                }
            }

            return consoleHWnd;
        }

        private void ClearConsoleHWnd()
        {
            consoleHWnd = 0;
        }

        [DllImport("User32")]
        private static extern int ShowWindow(int hWnd, int nCmdShow);

        [DllImport("user32")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        /*
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

         */
    }

    public class VirtualBoxEventListener : IEventListener
    {
        private VirtualBoxServer vboxServer;

        public VirtualBoxEventListener(VirtualBoxServer vboxServer)
        {
            this.vboxServer = vboxServer;
        }

        void IEventListener.HandleEvent(IEvent aEvent)
        {
            if (aEvent is IMachineStateChangedEvent)
            {
                vboxServer.changeState(((IMachineStateChangedEvent)aEvent).State);
            }
        }
    }
}
