using System;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using VirtualBox;

namespace Devel79Tray
{
    /// <summary>
    /// 
    /// </summary>
    public class VirtualBoxServer
    {
        /// <summary>
        /// 
        /// </summary>
        private const int WAIT_FOR_RESTART_SERVER_SECONDS = 2;

        /// <summary>
        /// 
        /// </summary>
        private enum Status
        {
            NONE,
            POWEREDOFF,
            STARTING,
            RUNNING,
            STOPING
        }

        /// <summary>
        /// 
        /// </summary>
        private Devel79Tray tray;

        /// <summary>
        /// 
        /// </summary>
        private string name;

        /// <summary>
        /// 
        /// </summary>
        private string machine;

        /// <summary>
        /// 
        /// </summary>
        private string ip;

        /// <summary>
        /// 
        /// </summary>
        private IVirtualBox vbox;

        /// <summary>
        /// 
        /// </summary>
        private IMachine vboxMachine;

        /// <summary>
        /// 
        /// </summary>
        private Session serverSession;

        /// <summary>
        /// 
        /// </summary>
        private VirtualBoxEventListener eventListener;

        /// <summary>
        /// 
        /// </summary>
        private Status status;
        
        /// <summary>
        /// 
        /// </summary>
        private int consoleHWnd;

        /// <summary>
        /// 
        /// </summary>
        private bool consoleVisible;

        /// <summary>
        /// 
        /// </summary>
        private bool starting;

        /// <summary>
        /// 
        /// </summary>
        private bool restarting;

        /// <summary>
        /// 
        /// </summary>
        private bool stoping;

        /// <summary>
        /// 
        /// </summary>
        private const int SW_HIDE = 0;
        
        /// <summary>
        /// 
        /// </summary>
        private const int SW_NORMAL = 1;
        
        /// <summary>
        /// 
        /// </summary>
        private const int SW_SHOW = 5;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tray"></param>
        /// <param name="name"></param>
        /// <param name="machine"></param>
        /// <param name="ip"></param>
        public VirtualBoxServer(Devel79Tray tray, String name, String machine, String ip)
        {
            this.tray = tray;
            this.name = name;
            this.machine = machine;
            this.ip = ip;

            this.status = Status.NONE;

            this.consoleVisible = false;

            this.starting = false;
            this.restarting = false;
            this.stoping = false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="runServer"></param>
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

            if (runServer)
            {
                StartServer();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void Close()
        {
            if (status == Status.RUNNING)
            {
                if (tray.ShowQuestion("Server is running", "Do you want to stop server?"))
                {
                    StopServer();
                }
                else
                {
                    ShowConsole();
                }
            }
            else
            {
                ShowConsole();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void Release()
        {
            if (eventListener != null)
            {
                vbox.EventSource.UnregisterListener(eventListener);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="state"></param>
        private void SetState(MachineState state)
        {
            UpdateState(state, true);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="state"></param>
        public void ChangeState(MachineState state)
        {
            UpdateState(state, false);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="state"></param>
        /// <param name="setState"></param>
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

            if (newStatus == status)
            {
                return;
            }

            status = newStatus;

            if (newStatus == Status.POWEREDOFF)
            {
                if (restarting)
                {
                    StartServer();
                }
                else if (!setState)
                {
                    if (stoping)
                    {
                        stoping = false;
                        tray.ShowTrayInfo(name, name + " was successfully powered off.");
                    }
                    else
                    {
                        tray.ShowTrayError(name, name + " was powered off.");
                    }
                }

                tray.SetServerPoweredOff();
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
                    stoping = false;
                    tray.ShowTrayInfo(name, name + " was successfully restarted.");
                }
                else if (!setState && starting)
                {
                    starting = false;
                    tray.ShowTrayInfo(name, name + " was successfully started.");
                }
                else if (!setState && !starting)
                {
                    tray.ShowTrayWarning(name, name + " was started.");
                }
                else
                {
                    tray.ShowTrayInfo(name, name + " is already running.");
                }

                tray.SetServerRunning();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void StartServer()
        {
            if (status != Status.POWEREDOFF)
            {
                throw new Exception("Server " + name + " is not powered off.");
            }

            try
            {
                DateTime startTime = DateTime.Now;

                while (vboxMachine.SessionState != SessionState.SessionState_Unlocked)
                {
                    long ticks = DateTime.Now.Ticks - startTime.Ticks;

                    if (ticks >= (WAIT_FOR_RESTART_SERVER_SECONDS * 10000000))
                    {
                        break;
                    }
                }

                if (vboxMachine.SessionState == SessionState.SessionState_Unlocked)
                {
                    starting = true;

                    serverSession = new SessionClass();
                    IProgress progress = vboxMachine.LaunchVMProcess(serverSession, "gui", "");
                }
                else if (restarting)
                {
                    restarting = false;
                }
            }
            catch
            {
                throw new Exception("Server " + name + " can't be run.");
            }
        }

        /// <summary>
        /// 
        /// </summary>
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

            stoping = true;

            vboxMachine.LockMachine(serverSession, LockType.LockType_Shared);
            serverSession.Console.PowerButton();
            serverSession.UnlockMachine();
        }

        /// <summary>
        /// 
        /// </summary>
        public void RestartServer()
        {
            restarting = true;
            StopServer();
        }

        /// <summary>
        /// 
        /// </summary>
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ip"></param>
        /// <returns></returns>
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

        /// <summary>
        /// 
        /// </summary>
        public void ShowConsole()
        {
            if (status == Status.RUNNING)
            {
                if (vboxMachine.CanShowConsoleWindow() != 0)
                {
                    int hWnd = (int)vboxMachine.ShowConsoleWindow();
                    ShowWindow(hWnd, SW_NORMAL);
                    ShowWindow(hWnd, SW_SHOW);
                    SetForegroundWindow(new IntPtr(hWnd));
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

        /// <summary>
        /// 
        /// </summary>
        public void HideConsole()
        {
            if (status == Status.RUNNING)
            {
                ShowWindow(GetConsoleHWnd(), SW_HIDE);

                consoleVisible = false;
                tray.SetConsoleHidden();
            }
        }

        /// <summary>
        /// 
        /// </summary>
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

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// 
        /// </summary>
        private void ClearConsoleHWnd()
        {
            consoleHWnd = 0;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hWnd"></param>
        /// <param name="nCmdShow"></param>
        /// <returns></returns>
        [DllImport("User32")]
        private static extern int ShowWindow(int hWnd, int nCmdShow);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hWnd"></param>
        /// <returns></returns>
        [DllImport("user32")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

    }

    /// <summary>
    /// 
    /// </summary>
    public class VirtualBoxEventListener : IEventListener
    {
        /// <summary>
        /// 
        /// </summary>
        private VirtualBoxServer vboxServer;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="vboxServer"></param>
        public VirtualBoxEventListener(VirtualBoxServer vboxServer)
        {
            this.vboxServer = vboxServer;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="aEvent"></param>
        void IEventListener.HandleEvent(IEvent aEvent)
        {
            if (aEvent is IMachineStateChangedEvent)
            {
                vboxServer.ChangeState(((IMachineStateChangedEvent)aEvent).State);
            }
        }
    }
}
