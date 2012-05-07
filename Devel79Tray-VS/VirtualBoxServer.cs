using System;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using VirtualBox;

namespace Devel79Tray
{
    /// <summary>
    /// VirtualBox server class.
    /// </summary>
    public class VirtualBoxServer
    {
        /// <summary>
        /// How many seconds wait for server is start after stop.
        /// </summary>
        private const int WAIT_FOR_RESTART_SERVER_SECONDS = 2;

        /// <summary>
        /// Constant for Win32 function ShowWindow. Window is hide.
        /// </summary>
        private const int SW_HIDE = 0;

        /// <summary>
        /// Constant for Win32 function ShowWindow. Window is normal.
        /// </summary>
        private const int SW_NORMAL = 1;

        /// <summary>
        /// Constant for Win32 function ShowWindow. Window is showen.
        /// </summary>
        private const int SW_SHOW = 5;

        /// <summary>
        /// Server state statuses.
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
        /// Devel79 Tray form.
        /// </summary>
        private Devel79Tray tray;

        /// <summary>
        /// Server name.
        /// </summary>
        private string name;

        /// <summary>
        /// VirtualBox machine name.
        /// </summary>
        private string machine;

        /// <summary>
        /// Server IP address.
        /// </summary>
        private string ip;

        /// <summary>
        /// VirtualBox COM object.
        /// </summary>
        private IVirtualBox vbox;

        /// <summary>
        /// VirtualBox machine COM object.
        /// </summary>
        private IMachine vboxMachine;

        /// <summary>
        /// VirtualBox session COM object.
        /// </summary>
        private Session serverSession;

        /// <summary>
        /// VirtualBox event listener.
        /// </summary>
        private VirtualBoxEventListener eventListener;

        /// <summary>
        /// Actual server status.
        /// </summary>
        private Status status;
        
        /// <summary>
        /// VirtualBox console hWnd.
        /// </summary>
        private int consoleHWnd;

        /// <summary>
        /// Is VirtualBox console visible.
        /// </summary>
        private bool consoleVisible;

        /// <summary>
        /// VirtualBox machine is starting.
        /// </summary>
        private bool starting;

        /// <summary>
        /// VirtualBox machine is restarting.
        /// </summary>
        private bool restarting;

        /// <summary>
        /// VirtualBox machine is stoping.
        /// </summary>
        private bool stoping;

        /// <summary>
        /// Initialize VirtualBox server class.
        /// </summary>
        /// <param name="tray">Devel79 form.</param>
        /// <param name="name">Server name.</param>
        /// <param name="machine">VirtualBox machine name.</param>
        /// <param name="ip">Server IP address.</param>
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
        /// Initialize VirtualBox COM.
        /// </summary>
        /// <param name="runServer">Run server after initialize.</param>
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
        /// Call if application is exiting.
        /// </summary>
        public void ApplicationClose()
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
        /// Release VirtualBox sever.
        /// </summary>
        public void Release()
        {
            if (eventListener != null)
            {
                vbox.EventSource.UnregisterListener(eventListener);
            }
        }

        /// <summary>
        /// Set server state.
        /// </summary>
        /// <param name="state">Server state.</param>
        private void SetState(MachineState state)
        {
            UpdateState(state, true);
        }

        /// <summary>
        /// Change server state.
        /// </summary>
        /// <param name="state">Server state.</param>
        public void ChangeState(MachineState state)
        {
            UpdateState(state, false);
        }

        /// <summary>
        /// Update or set server state.
        /// </summary>
        /// <param name="state">Server state.</param>
        /// <param name="setState">True if set state.</param>
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
        /// Start server.
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
        /// Stop server.
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
        /// Restart server.
        /// </summary>
        public void RestartServer()
        {
            restarting = true;
            StopServer();
        }

        /// <summary>
        /// Ping server IP address.
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
        /// Ping to IP address.
        /// </summary>
        /// <param name="ip">IP address.</param>
        /// <returns>Success.</returns>
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
        /// Show VirtualBox machine console.
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
        /// Hide VirtualBox machine console.
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
        /// Show or hide VirtualBox machine console.
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
        /// Resolve VirtualBox machine console hWnd.
        /// </summary>
        /// <returns>VirtualBox machine console hWnd.</returns>
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
        /// Clear VirtualBox console hWnd.
        /// </summary>
        private void ClearConsoleHWnd()
        {
            consoleHWnd = 0;
        }

        /// <summary>
        /// Win32 function to show or hide window.
        /// </summary>
        /// <param name="hWnd">Window hWnd.</param>
        /// <param name="nCmdShow">Window state.</param>
        /// <returns></returns>
        [DllImport("User32")]
        private static extern int ShowWindow(int hWnd, int nCmdShow);

        /// <summary>
        /// Win32 function to move window to foreground.
        /// </summary>
        /// <param name="hWnd">Window hWnd.</param>
        /// <returns>Window state.</returns>
        [DllImport("user32")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

    }

    /// <summary>
    /// VirtualBox event listener class.
    /// </summary>
    public class VirtualBoxEventListener : IEventListener
    {
        /// <summary>
        /// VirtualBox server.
        /// </summary>
        private VirtualBoxServer vboxServer;

        /// <summary>
        /// Initialize VirtualBox event listener.
        /// </summary>
        /// <param name="vboxServer">VirtualBox server.</param>
        public VirtualBoxEventListener(VirtualBoxServer vboxServer)
        {
            this.vboxServer = vboxServer;
        }

        /// <summary>
        /// Listen for VirtualBox events.
        /// </summary>
        /// <param name="aEvent">VirtualBox event.</param>
        void IEventListener.HandleEvent(IEvent aEvent)
        {
            if (aEvent is IMachineStateChangedEvent)
            {
                vboxServer.ChangeState(((IMachineStateChangedEvent)aEvent).State);
            }
        }
    }
}
