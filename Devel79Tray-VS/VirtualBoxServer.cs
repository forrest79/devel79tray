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

        private bool restarting;

        private const int SW_HIDE = 0;
        private const int SW_NORMAL = 1;
        private const int SW_SHOW = 5;

        public VirtualBoxServer(Devel79Tray tray, String name, String machine, String ip)
        {
            this.tray = tray;
            this.name = name;
            this.machine = machine;
            this.ip = ip;

            this.status = Status.NONE;

            this.consoleVisible = false;

            this.restarting = false;
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

            SetState(vboxMachine.State);

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

        private void SetState(MachineState state)
        {
            UpdateState(state, true);
        }

        public void ChangeState(MachineState state)
        {
            UpdateState(state, false);
        }

        private void UpdateState(MachineState state, bool setState)
        {
            Status newStatus = status;
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

                    if (!setState && !restarting)
                    {
                        tray.ShowTrayInfo(name, name + " was successfully powered off.");
                    }
                }
                else if (newStatus == Status.RUNNING)
                {
                    if (consoleVisible)
                    {
                        ShowConsole();
                    }
                    else
                    {
                        HideConsole();
                    }

                    if (serverSession != null)
                    {
                        serverSession.UnlockMachine();
                    }

                    if (restarting)
                    {
                        restarting = false;
                        tray.ShowTrayInfo(name, name + " was successfully restarted.");
                    }
                    else if (!setState)
                    {
                        tray.ShowTrayInfo(name, name + " was successfully started.");
                    }
                    else
                    {
                        tray.ShowTrayInfo(name, name + " is already running.");
                    }

                    tray.SetServerRunning();
                }

                status = newStatus;

                if ((newStatus == Status.POWEREDOFF) && restarting)
                {
                    StartServer();
                }
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
                while (vboxMachine.SessionState != SessionState.SessionState_Unlocked)
                {
                    // TODO: Wait max 1s, than skip and reset restarting.
                }

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

        public void RestartServer()
        {
            restarting = true;
            StopServer();
        }

        public void PingServer()
        {
            if (Ping(ip))
            {
                tray.ShowTrayInfo("Ping [OK]", "Successfully ping " + name + " (" + machine + "@" + ip + ")");
            }
            else
            {
                tray.ShowTrayWarning("Ping [TIMEOUT]", "Ping " + name + " (" + machine + "@" + ip + ") timeout.");
            }
        }

        private bool Ping(string ip)
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
            if (status == Status.RUNNING)
            {
                if (vboxMachine.CanShowConsoleWindow() != 0)
                {
                    int hWnd = (int)vboxMachine.ShowConsoleWindow();
                    ShowWindow(hWnd, SW_NORMAL);
                    ShowWindow(hWnd, SW_SHOW);
                }
                else
                {
                    ShowWindow(GetConsoleHWnd(), SW_NORMAL);
                    ShowWindow(GetConsoleHWnd(), SW_SHOW);
                    SetForegroundWindow(new IntPtr(GetConsoleHWnd()));
                }

                consoleVisible = true;
                tray.SetConsoleShown();
            }
        }

        public void HideConsole()
        {
            if (status == Status.RUNNING)
            {
                ShowWindow(GetConsoleHWnd(), SW_HIDE);

                consoleVisible = false;
                tray.SetConsoleHidden();
            }
        }

        public void ToggleConsole()
        {
            if (status == Status.RUNNING)
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
                vboxServer.ChangeState(((IMachineStateChangedEvent)aEvent).State);
            }
        }
    }
}
