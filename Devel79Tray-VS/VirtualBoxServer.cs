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
        /// Constant for Win32 function ShowWindow. Window is showen.
        /// </summary>
        private const int SW_RESTORE = 9;

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
        /// SSH client shell command.
        /// </summary>
        private string ssh;

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
        /// SSH client process.
        /// </summary>
        private Process sshProcess;

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
        /// <param name="ssh">SSH client shell command.</param>
        public VirtualBoxServer(Devel79Tray tray, String name, String machine, String ip, String ssh)
        {
            this.tray = tray;
            this.name = name;
            this.machine = machine;
            this.ip = ip;
            this.ssh = ssh;

            this.status = Status.NONE;

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

            if (runServer && (status == Status.POWEREDOFF))
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
                    KillConsole();
                    StopServer();
                }
            }
        }

        /// <summary>
        /// Release VirtualBox sever.
        /// </summary>
        public void Release()
        {
            if (eventListener != null)
            {
                try
                {
                    vbox.EventSource.UnregisterListener(eventListener);
                }
                catch (System.Runtime.InteropServices.COMException)
                {
                    eventListener = null; // Better system shutdown handle...
                }
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
        public void ChangeState()
        {
            UpdateState(vboxMachine.State, false);
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
                KillConsole();

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
                    IProgress progress = vboxMachine.LaunchVMProcess(serverSession, "headless", "");
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

            if (serverSession.State == SessionState.SessionState_Locked)
            {
                serverSession.UnlockMachine();
            }
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
            try
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
            catch (System.ComponentModel.Win32Exception)
            {
                tray.ShowTrayError("Ping [ERROR]", "An error occured while ping to " + name + " (" + machine + "@" + ip + ").");
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
        /// Show SSH client.
        /// </summary>
        public void ShowConsole()
        {
            if (status == Status.RUNNING)
            {
                if (IsConsoleRunning())
                {
                    IntPtr hWnd = sshProcess.MainWindowHandle;
                    
                    if (IsIconic(hWnd))
                    {
                        ShowWindow(hWnd, SW_RESTORE);
                    }

                    SetForegroundWindow(hWnd);
                }
                else
                {
                    sshProcess = new System.Diagnostics.Process();

                    try
                    {
                        string[] sshClient = ssh.Split(new Char[] { ' ', '\t' }, 2);

                        switch (sshClient.Length) {
                            case 1:
                                sshProcess.StartInfo.FileName = sshClient[0];
                                break;
                            case 2:
                                sshProcess.StartInfo.Arguments = sshClient[1];
                                goto case 1;
                            default :
                                return;
                        }

                        sshProcess.Start();
                    }
                    catch (System.ComponentModel.Win32Exception)
                    {
                        tray.ShowTrayError("SSH client [ERROR]", "Can't run SSH client: '" + ssh + "'");
                    }
                }
            }
        }

        /// <summary>
        /// Kill SSH client process.
        /// </summary>
        public void KillConsole()
        {
            if (IsConsoleRunning())
            {
                sshProcess.Kill();
                sshProcess = null;
            }
        }

        /// <summary>
        /// Check if SSH client is running.
        /// </summary>
        /// <returns>true if SSH client is running.</returns>
        public bool IsConsoleRunning()
        {
            if (sshProcess == null)
            {
                return false;
            }

            return !sshProcess.HasExited;
        }

        /// <summary>
        /// Win32 function to show or hide window.
        /// </summary>
        /// <param name="hWnd">Window hWnd.</param>
        /// <param name="nCmdShow">Window state.</param>
        /// <returns></returns>
        [DllImport("User32")]
        private static extern int ShowWindow(IntPtr hWnd, int nCmdShow);

        /// <summary>
        /// Win32 function to move window to foreground.
        /// </summary>
        /// <param name="hWnd">Window hWnd.</param>
        /// <returns>Window state.</returns>
        [DllImport("User32")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        /// <summary>
        /// Win32 function to check window minized state.
        /// </summary>
        /// <param name="hWnd">Windows hWnd.</param>
        /// <returns>True if minimized.</returns>
        [DllImport("User32")]
        private static extern bool IsIconic(IntPtr hWnd);
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
            if (aEvent.Type == VBoxEventType.VBoxEventType_OnMachineStateChanged)
            {
                vboxServer.ChangeState();
            }
        }
    }
}
