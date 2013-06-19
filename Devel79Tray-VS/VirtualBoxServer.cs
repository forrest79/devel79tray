using System;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using VirtualBox;
using System.Collections;
using System.Threading;

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
        public enum Status
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
        /// Server name set after stop.
        /// </summary>
        private string nameAfterStop;

        /// <summary>
        /// VirtualBox machine name after stop.
        /// </summary>
        private string machineAfterStop;

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
        public VirtualBoxServer(Devel79Tray tray)
        {
            this.tray = tray;

            this.status = Status.NONE;

            this.starting = false;
            this.restarting = false;
            this.stoping = false;
        }

        /// <summary>
        /// Initialize VirtualBox COM.
        /// </summary>
        /// <param name="runServer">Run server after initialize.</param>
        public void Initialize()
        {
            try
            {
                vbox = new VirtualBoxClass();
            }
            catch
            {
                throw new VirtualBoxServerException("Error while connecting to VirtualBox.");
            }

            eventListener = new VirtualBoxEventListener(this);
            vbox.EventSource.RegisterListener(eventListener, new VBoxEventType[] { VBoxEventType.VBoxEventType_OnMachineStateChanged }, 1);
        }

        /// <summary>
        /// Register new server.
        /// </summary>
        /// <param name="name">Server name</param>
        /// <param name="machine">VirtualBox machine name</param>
        public void RegisterServer(string name, string machine)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(machine))
            {
                throw new VirtualBoxServerException("You must specify name and machine.");
            }

            if (this.machine == machine)
            {
                return;
            }

            if (vboxMachine != null)
            {
                switch (GetStatus())
                {
                    case Status.RUNNING :
                        ChangeServer(name, machine);
                        return;
                    case Status.STARTING :
                    case Status.STOPING :
                        return;
                }
            }

            this.name = name;
            this.machine = machine;

            this.nameAfterStop = null;
            this.machineAfterStop = null;

            try
            {
                vboxMachine = vbox.FindMachine(machine);
            }
            catch
            {
                throw new VirtualBoxServerException("Machine '" + machine + "' not found.");
            }

            SetState(vboxMachine.State);
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
        /// 
        /// </summary>
        /// <returns></returns>
        public Status GetStatus()
        {
            return status;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string GetMachine()
        {
            return machine;
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
                    if (!string.IsNullOrEmpty(nameAfterStop) && !string.IsNullOrEmpty(machineAfterStop))
                    {
                        RegisterServer(nameAfterStop, machineAfterStop);
                        restarting = false;
                    }

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
            if (vboxMachine == null)
            {
                throw new VirtualBoxServerException("No server is registered.");
            }

            if (status != Status.POWEREDOFF)
            {
                throw new VirtualBoxServerException("Server " + name + " is not powered off.");
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
                throw new VirtualBoxServerException("Server " + name + " can't be run.");
            }
        }

        /// <summary>
        /// Stop server.
        /// </summary>
        public void StopServer()
        {
            if (vboxMachine == null)
            {
                throw new VirtualBoxServerException("No server is registered.");
            }

            if (status != Status.RUNNING)
            {
                throw new VirtualBoxServerException("Server " + name + " is not running.");
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
        /// Change running server.
        /// </summary>
        /// <param name="name">Server name</param>
        /// <param name="machine">VirtualBox machine name</param>
        private void ChangeServer(string name, string machine)
        {
            restarting = true;

            nameAfterStop = name;
            machineAfterStop = machine;

            StopServer();
        }

        /// <summary>
        /// Check if server is running.
        /// </summary>
        /// <param name="machine">Machine name</param>
        /// <returns>True if server is running, false otherwise</returns>
        public static bool IsServerRunning(string machine)
        {
            IVirtualBox vbox;

            try
            {
                vbox = new VirtualBoxClass();
            }
            catch
            {
                throw new VirtualBoxServerException("Error while connecting to VirtualBox.");
            }

            IMachine server;

            try
            {
                server = vbox.FindMachine(machine);
            }
            catch
            {
                throw new VirtualBoxServerException("Server '" + machine + "' not found.");
            }

            return server.State == MachineState.MachineState_Running;
        }

        /// <summary>
        /// Ping to IP address.
        /// </summary>
        /// <param name="ip">IP address</param>
        public void PingServer(string ip)
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                Ping ping = new Ping();
                PingReply pingReply = ping.Send(ip, 5000);

                try
                {
                    if (pingReply.Status == IPStatus.Success)
                    {
                        tray.ShowTrayInfo("Ping [OK]", "Successfully ping " + name + " in " + pingReply.RoundtripTime + "ms (" + machine + "@" + pingReply.Address + ")");
                    }
                    else
                    {
                        tray.ShowTrayWarning("Ping [" + pingReply.Status.ToString().ToUpper() + "]", "Ping " + name + " (" + machine + "@" + ip + ") " + pingReply.Status.ToString().ToLower() + ".");
                    }
                }
                catch
                {
                    tray.ShowTrayError("Ping [ERROR]", "An error occured while ping to " + name + " (" + machine + "@" + ip + ").");
                }
            });
        }

        /// <summary>
        /// Run command and read output.
        /// </summary>
        /// <param name="name">Command name</param>
        /// <param name="command">Command executables</param>
        public void RunCommand(string name, string command)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(command))
            {
                throw new VirtualBoxServerException("Command and name can't be empty.");
            }

            string[] cmd = command.Split(new Char[] { ' ', '\t' }, 2);


            ThreadPool.QueueUserWorkItem(delegate
            {
                Process process = new Process();

                switch (cmd.Length)
                {
                    case 1:
                        process.StartInfo.FileName = cmd[0];
                        break;
                    case 2:
                        process.StartInfo.Arguments = cmd[1];
                        goto case 1;
                    default:
                        return;
                }

                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.CreateNoWindow = true;

                try
                {
                    process.Start();

                    string output = process.StandardOutput.ReadToEnd();

                    if (!process.WaitForExit(15000))
                    {
                        process.Kill();
                        return;
                    }

                    if (process.ExitCode != 0)
                    {
                        tray.ShowTrayError(name, "Exit code: " + process.ExitCode.ToString() + "." + (string.IsNullOrEmpty(output) ? "" : "\n" + output));
                        return;
                    }

                    tray.ShowTrayInfo(name, string.IsNullOrEmpty(output) ? "Command was successfully run." : output);
                }
                catch (Exception e)
                {
                    tray.ShowTrayError(name, "Error while running command: " + e.Message);
                }
            });
        }

        /// <summary>
        /// Show SSH client.
        /// </summary>
        /// <param name="sshCommand">Command to run SSH console</param>
        public void ShowConsole(string sshCommand)
        {
            if (string.IsNullOrEmpty(sshCommand)) 
            {
                throw new VirtualBoxServerException("SSH command can't be empty.");
            }

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
                        string[] sshClient = sshCommand.Split(new Char[] { ' ', '\t' }, 2);

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
                        tray.ShowTrayError("SSH client [ERROR]", "Can't run SSH client for " + name + ": '" + sshCommand + "'");
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
        private bool IsConsoleRunning()
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

    /// <summary>
    /// VirtualBox server exception.
    /// </summary>
    public class VirtualBoxServerException : ProgramException
    {

        /// <summary>
        /// Blank initialization.
        /// </summary>
        public VirtualBoxServerException()
        {
        }

        /// <summary>
        /// Initialization with message.
        /// </summary>
        /// <param name="message"></param>
        public VirtualBoxServerException(string message) : base(message)
        {
        }
    }
}
