using System;
using System.IO;

namespace Devel79Tray
{
    /// <summary>
    /// Email monitor class.
    /// </summary>
    public class EmailMonitor
    {
        /// <summary>
        /// Devel79 Tray form.
        /// </summary>
        private Devel79Tray tray;

        /// <summary>
        /// Directory to watch.
        /// </summary>
        private string directory;

        /// <summary>
        /// Is watching active?
        /// </summary>
        private bool active;

        /// <summary>
        /// Directory watcher.
        /// </summary>
        private FileSystemWatcher fileSystemWatcher;

        /// <summary>
        /// Initialize monitor.
        /// </summary>
        /// <param name="tray">Main program.</param>
        public EmailMonitor(Devel79Tray tray)
        {
            this.tray = tray;

            this.active = false;
        }

        /// <summary>
        /// Start monitoring in monitor.
        /// </summary>
        /// <param name="directory">Directory to monitor</param>
        public void StartMonitoring(string directory)
        {
            if (!IsActive())
            {
                if (Directory.Exists(directory))
                {
                    try
                    {
                        this.fileSystemWatcher = new FileSystemWatcher(directory);
                        this.fileSystemWatcher.EnableRaisingEvents = true;
                        this.fileSystemWatcher.Created += new FileSystemEventHandler(EmailCreated);

                        this.directory = directory;

                        this.active = true;
                    }
                    catch (Exception e)
                    {
                        throw new EmailMonitorException("Email monitor service: " + e.Message);
                    }
                }
                else
                {
                    throw new EmailMonitorException("Email monitor service: directory '" + directory + "' does not exists.");
                }
            }
            else
            {
                throw new EmailMonitorException("Email monitor service: service is already active.");
            }
        }

        /// <summary>
        /// Stop email monitoring.
        /// </summary>
        public void StopMonitoring()
        {
            if (IsActive())
            {
                this.fileSystemWatcher.EnableRaisingEvents = false;
                this.fileSystemWatcher.Created -= new FileSystemEventHandler(EmailCreated);
                this.fileSystemWatcher = null;
            }
            else
            {
                throw new EmailMonitorException("Email monitor service: service is not active.");
            }
        }

        /// <summary>
        /// Call when new email is created in directory.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">New file properties.</param>
        private void EmailCreated(Object sender, FileSystemEventArgs e)
        {
            if (e.Name.ToLower().EndsWith(".eml"))
            {
                tray.ShowTrayInfo("Email monitor [NEW EMAIL]", "Click to open email '" + e.Name + "'.", new OpenEmail(this, e.Name, e.FullPath));
            }
        }

        /// <summary>
        /// Is watching active?
        /// </summary>
        /// <returns>True if is active.</returns>
        public bool IsActive()
        {
            return active;
        }

        /// <summary>
        /// Open directory in shell.
        /// </summary>
        public void OpenEmailDirectory()
        {
            if (IsActive())
            {
                if (Directory.Exists(directory))
                {
                    System.Diagnostics.Process.Start(directory);
                }
                else
                {
                    tray.ShowTrayError("Email monitor [ERROR]", "Directory '" + directory + "' does not exists.");
                }
            }
            else
            {
                throw new EmailMonitorException("Email monitor service: service is not active.");
            }
        }

        /// <summary>
        /// Open email in shell.
        /// </summary>
        /// <param name="name">File name.</param>
        /// <param name="filename">Full file path.</param>
        public void OpenEmail(string name, string filename)
        {
            if (IsActive())
            {
                if (File.Exists(filename))
                {
                    System.Diagnostics.Process.Start(filename);
                }
                else
                {
                    tray.ShowTrayError("Email monitor [ERROR]", "Email '" + name + "' does not exists.");
                }
            }
            else
            {
                throw new EmailMonitorException("Email monitor service: service is not active.");
            }

        }
    }

    /// <summary>
    /// Open email callback.
    /// </summary>
    public class OpenEmail : ICallable
    {
        /// <summary>
        /// Email monitor.
        /// </summary>
        private EmailMonitor emailMonitor;

        /// <summary>
        /// Email name.
        /// </summary>
        private string name;

        /// <summary>
        /// Email full path.
        /// </summary>
        private string filename;

        /// <summary>
        /// Initialize callback.
        /// </summary>
        /// <param name="emailMonitor">Email monitor.</param>
        /// <param name="name">Email name.</param>
        /// <param name="filename">Full email path.</param>
        public OpenEmail(EmailMonitor emailMonitor, string name, string filename)
        {
            this.emailMonitor = emailMonitor;
            this.name = name;
            this.filename = filename;
        }

        /// <summary>
        /// Open email.
        /// </summary>
        public void Call()
        {
            emailMonitor.OpenEmail(name, filename);
        }
    }

    /// <summary>
    /// Email monitor exception.
    /// </summary>
    public class EmailMonitorException : ProgramException
    {

        /// <summary>
        /// Blank initialization.
        /// </summary>
        public EmailMonitorException()
        {
        }

        /// <summary>
        /// Initialization with message.
        /// </summary>
        /// <param name="message"></param>
        public EmailMonitorException(string message) : base(message)
        {
        }

    }
}
