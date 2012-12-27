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
        /// <param name="directory">Directory to watch.</param>
        public EmailMonitor(Devel79Tray tray, String directory)
        {
            this.tray = tray;
            this.directory = directory;

            this.active = false;

            if (directory != null)
            {
                if (Directory.Exists(directory))
                {
                    try
                    {
                        this.fileSystemWatcher = new FileSystemWatcher(directory);
                        this.fileSystemWatcher.EnableRaisingEvents = true;
                        this.fileSystemWatcher.Created += new FileSystemEventHandler(EmailCreated);

                        this.active = true;
                    }
                    catch (Exception e)
                    {
                        throw new Exception("Email monitor service: " + e.Message);
                    }
                }
                else
                {
                    throw new Exception("Email monitor service: directory '" + directory + "' does not exists.");
                }
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
            if (Directory.Exists(directory))
            {
                System.Diagnostics.Process.Start(directory);
            }
            else
            {
                tray.ShowTrayError("Email monitor [ERROR]", "Directory '" + directory + "' does not exists.");
            }
        }

        /// <summary>
        /// Open email in shell.
        /// </summary>
        /// <param name="name">File name.</param>
        /// <param name="filename">Full file path.</param>
        public void OpenEmail(string name, string filename)
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
}
