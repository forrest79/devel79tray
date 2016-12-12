using System;
using System.IO;

namespace Devel79Tray
{
    /// <summary>
    /// Directory monitor class.
    /// </summary>
    public class DirectoryMonitor
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
        /// Message to show.
        /// </summary>
        private string message;

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
        /// <param name="tray">Main program</param>
        /// <param name="message">Message to show</param>
        /// <param name="directory">Directory to watch</param>
        public DirectoryMonitor(Devel79Tray tray, string message, string directory)
        {
            this.tray = tray;
            this.directory = directory;
            this.message = message;
            this.active = false;
        }

        /// <summary>
        /// Start monitoring in monitor.
        /// </summary>
        public void StartMonitoring()
        {
            if (!active)
            {
                if (Directory.Exists(directory))
                {
                    try
                    {
                        fileSystemWatcher = new FileSystemWatcher(directory);
                        fileSystemWatcher.IncludeSubdirectories = true;
                        fileSystemWatcher.EnableRaisingEvents = true;
                        fileSystemWatcher.Created += new FileSystemEventHandler(FileCreated);

                        active = true;
                    }
                    catch (Exception e)
                    {
                        throw new DirectoryMonitorException("Directory monitor service: " + e.Message);
                    }
                }
                else
                {
                    throw new DirectoryMonitorException("Directory monitor service: directory '" + directory + "' does not exists.");
                }
            }
            else
            {
                throw new DirectoryMonitorException("Directory monitor service: service is already active.");
            }
        }

        /// <summary>
        /// Stop directory monitoring.
        /// </summary>
        public void StopMonitoring()
        {
            if (active)
            {
                fileSystemWatcher.EnableRaisingEvents = false;
                fileSystemWatcher.Created -= new FileSystemEventHandler(FileCreated);
                fileSystemWatcher = null;

                active = false;
            }
            else
            {
                throw new DirectoryMonitorException("Directory monitor service: service is not active.");
            }
        }

        /// <summary>
        /// Call when new file is created in directory.
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">New file properties</param>
        private void FileCreated(Object sender, FileSystemEventArgs e)
        {
            if (!Directory.Exists(e.FullPath))
            {
                tray.ShowTrayInfo(message, "Click to open file '" + e.Name + "'.", new OpenFile(this, e.Name, e.FullPath));
            }
        }

        /// <summary>
        /// Open directory in shell.
        /// </summary>
        public void OpenDirectory()
        {
            if (active)
            {
                if (Directory.Exists(directory))
                {
                    System.Diagnostics.Process.Start(directory);
                }
                else
                {
                    tray.ShowTrayError("Directory monitor", "Directory '" + directory + "' does not exists.");
                }
            }
            else
            {
                throw new DirectoryMonitorException("Directory monitor service: service is not active.");
            }
        }

        /// <summary>
        /// Open file in shell.
        /// </summary>
        /// <param name="name">File name</param>
        /// <param name="filename">Full file path</param>
        public void OpenFile(string name, string filename)
        {
            if (active)
            {
                if (File.Exists(filename))
                {
                    System.Diagnostics.Process.Start(filename);
                }
                else
                {
                    tray.ShowTrayError("Directory monitor", "File '" + name + "' does not exists.");
                }
            }
            else
            {
                throw new DirectoryMonitorException("Directory monitor service: service is not active.");
            }
        }
    }

    /// <summary>
    /// Open file callback.
    /// </summary>
    public class OpenFile : ICallable
    {
        /// <summary>
        /// Directory monitor.
        /// </summary>
        private DirectoryMonitor directoryMonitor;

        /// <summary>
        /// File name.
        /// </summary>
        private string name;

        /// <summary>
        /// File full path.
        /// </summary>
        private string filename;

        /// <summary>
        /// Initialize callback.
        /// </summary>
        /// <param name="directoryMonitor">Directory monitor</param>
        /// <param name="name">File name</param>
        /// <param name="filename">Full file path</param>
        public OpenFile(DirectoryMonitor directoryMonitor, string name, string filename)
        {
            this.directoryMonitor = directoryMonitor;
            this.name = name;
            this.filename = filename;
        }

        /// <summary>
        /// Open file.
        /// </summary>
        public void Call()
        {
            directoryMonitor.OpenFile(name, filename);
        }
    }

    /// <summary>
    /// Directory monitor exception.
    /// </summary>
    public class DirectoryMonitorException : ProgramException
    {
        /// <summary>
        /// Blank initialization.
        /// </summary>
        public DirectoryMonitorException()
        {
        }

        /// <summary>
        /// Initialization with message.
        /// </summary>
        /// <param name="message"></param>
        public DirectoryMonitorException(string message) : base(message)
        {
        }
    }
}
