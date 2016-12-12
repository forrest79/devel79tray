using System;
using System.Collections.Generic;
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
        /// Server state statuses.
        /// </summary>
        public enum Status
        {
            POWEREDOFF,
            STARTING,
            RUNNING,
            STOPING
        }

        /// <summary>
        /// VirtualBox COM object.
        /// </summary>
        private IVirtualBox vbox;

        /// <summary>
        /// Servers list.
        /// </summary>
        private Dictionary<string, Server> servers;

        /// <summary>
        /// Machines list.
        /// </summary>
        private Dictionary<Server, IMachine> machines;

        /// <summary>
        /// VirtualBox session COM objects.
        /// </summary>
        private Dictionary<Server, Session> serverSessions;

        /// <summary>
        /// VirtualBox event listener.
        /// </summary>
        private VirtualBoxEventListener eventListener;

        /// <summary>
        /// Initialize VirtualBox server class.
        /// </summary>
        public VirtualBoxServer()
        {
            machines = new Dictionary<Server, IMachine>();
            serverSessions = new Dictionary<Server, Session>();
        }

        /// <summary>
        /// Initialize VirtualBox COM.
        /// </summary>
        /// <param name="servers">Servers to initialize</param>
        public void Initialize(Dictionary<string, Server> servers)
        {
            this.servers = servers;

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

            foreach (Server server in servers.Values)
            {
                try
                {
                    machines.Add(server, vbox.FindMachine(server.GetMachine()));
                }
                catch
                {
                    throw new VirtualBoxServerException("Machine '" + server.GetMachine() + "' not found.");
                }

                UpdateServerState(server, true);
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
        /// Change server state.
        /// </summary>
        public void UpdateServersState()
        {
            foreach (Server server in servers.Values)
            {
                UpdateServerState(server);
            }
        }

        /// <summary>
        /// Update or ser server state.
        /// </summary>
        /// <param name="server">Server</param>
        private void UpdateServerState(Server server)
        {
            UpdateServerState(server, false);
        }

        /// <summary>
        /// Update or set server state.
        /// </summary>
        /// <param name="server">Server</param>
        /// <param name="initializing">True if is called while initializing application, false otherwise</param>
        private void UpdateServerState(Server server, bool initializing)
        {
            if (!machines.ContainsKey(server))
            {
                throw new VirtualBoxServerException("Server " + server.GetName() + " is not registered.");
            }

            IMachine machine = machines[server];

            Status newStatus;
            Status oldStatus = server.GetStatus();
            
            switch (machine.State)
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

            if (newStatus == oldStatus)
            {
                return;
            }

            server.SetStatus(newStatus, initializing);
        }

        /// <summary>
        /// Start server.
        /// </summary>
        /// <param name="server">Server</param>
        public void StartServer(Server server)
        {
            if (!machines.ContainsKey(server))
            {
                throw new VirtualBoxServerException("Server " + server.GetName() + " is not registered.");
            }

            if (server.GetStatus() == Status.RUNNING)
            {
                return;
            }
            else if (server.GetStatus() != Status.POWEREDOFF)
            {
                throw new VirtualBoxServerException("Server " + server.GetName() + " is not powered off.");
            }

            try
            {
                IMachine machine = machines[server];
                Session serverSession = getSession(server);
                DateTime startTime = DateTime.Now;

                while (machine.SessionState != SessionState.SessionState_Unlocked)
                {
                    long ticks = DateTime.Now.Ticks - startTime.Ticks;

                    if (ticks >= (WAIT_FOR_RESTART_SERVER_SECONDS * 10000000))
                    {
                        break;
                    }
                }

                if (machine.SessionState == SessionState.SessionState_Unlocked)
                {
                    server.SetStarting();

                    IProgress progress = machine.LaunchVMProcess(serverSession, "headless", "");
                }
            }
            catch
            {
                throw new VirtualBoxServerException("Server " + server.GetName() + " can't be run.");
            }
        }

        /// <summary>
        /// Stop server.
        /// </summary>
        /// <param name="server">Server</param>
        public void StopServer(Server server)
        {
            if (!machines.ContainsKey(server))
            {
                throw new VirtualBoxServerException("Server " + server.GetName() + " is not registered.");
            }

            if (server.GetStatus() == Status.POWEREDOFF)
            {
                return;
            }
            else if (server.GetStatus() != Status.RUNNING)
            {
                throw new VirtualBoxServerException("Server " + server.GetName() + " is not running.");
            }

            IMachine machine = machines[server];
            Session serverSession = getSession(server);

            server.SetStoping();

            if (serverSession.State == SessionState.SessionState_Locked)
            {
                serverSession.UnlockMachine();
            }
            machine.LockMachine(serverSession, LockType.LockType_Shared);
            serverSession.Console.PowerButton();
            serverSession.UnlockMachine();
        }

        /// <summary>
        /// Get or create server machine session.
        /// </summary>
        /// <param name="server">Server</param>
        /// <returns>New or existing session</returns>
        private Session getSession(Server server)
        {
            Session serverSession;
            if (!serverSessions.ContainsKey(server))
            {
                serverSession = new SessionClass();
                serverSessions.Add(server, serverSession);
            }
            else
            {
                serverSession = serverSessions[server];
            }

            return serverSession;
        }
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
        /// <param name="vboxServer">VirtualBox server</param>
        public VirtualBoxEventListener(VirtualBoxServer vboxServer)
        {
            this.vboxServer = vboxServer;
        }

        /// <summary>
        /// Listen for VirtualBox events.
        /// </summary>
        /// <param name="aEvent">VirtualBox event</param>
        void IEventListener.HandleEvent(IEvent aEvent)
        {
            if (aEvent.Type == VBoxEventType.VBoxEventType_OnMachineStateChanged)
            {
                vboxServer.UpdateServersState();
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
