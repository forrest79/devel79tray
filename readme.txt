Devel79 Tray © Jakub Trmota, 2016 (http://forrest79.net)


VirtualBox developer server tray icon.


HOW TO USE:
===========
devel79tray [--run|-r] [--default|-d machine] [--config|-c filename]

Settings: --run, -r        run first or default VirtualBox machine after application starts (canceled if another server from list is already running)
          --default, -d    specify default machine
          --config, -c     specify filename with configuration (default is "devel79.conf" in application directory)

Configuration file (make one or more [server] sections):
name = Server name to display
machine = VirtualBox machine name (case sensitive)
ip = VirtualBox Network adapter IP address (used for ping command)
ssh = shell connect command to SSH server (used for Show console, can be Putty, ssh from msys or other SSH client)
email = directory witch is watching for new saved email (optional)
command = command to run in format NAME : COMMAND, example: Clear cache : ssh devel@devel79 clear-cache (optional, can be multiple commands)

Tray icon menu:
Double click - if server is running show VirtualBox console (SSH client)
Show console - if server is running show VirtualBox console (SSH client)
Start server - if server is not running, start server
Restart server - if server is running, stop and start server
Stop server - if server is running, stop server
Ping server - if server is running, ping to server IP address a show result
Commands - if server has one or more commands, run command
Servers - if there is more than one server, switch to other server
Open email directory - open email directory in Windows Explorer
Exit - exit tray icon, if server is running, show question for stop server

Email monitoring:
If email in config is set to existing directory, all new files with .eml extension are shown in balloon tooltip and if you click on this tooltip, the email is open in associated application.


HISTORY
=======
3.0.15 [2016-03-29] - Update to VirtualBoxSDK-5.0.16-105871.
3.0.14 [2016-02-06] - Update to VirtualBoxSDK-5.0.14-105127.
3.0.13 [2015-12-11] - Update to VirtualBoxSDK-5.0.10-104061.
3.0.12 [2015-09-09] - Update to VirtualBoxSDK-5.0.4-102546.
3.0.11 [2015-07-13] - Update to VirtualBoxSDK-5.0.0-101573.
3.0.10 [2015-04-10] - Update to VirtualBoxSDK-4.3.28-100309.
3.0.9  [2015-04-10] - Update to VirtualBoxSDK-4.3.26-98988.
3.0.8  [2015-03-07] - Update to VirtualBoxSDK-4.3.18-96516.
3.0.7  [2014-05-23] - Update to VirtualBoxSDK-4.3.12-93733.
3.0.6  [2014-03-29] - Update to VirtualBoxSDK-4.3.10-93012.
3.0.5  [2013-12-29] - Update to VirtualBoxSDK-4.3.6-91406.
3.0.4  [2013-12-04] - Update to VirtualBoxSDK-4.3.4-91027.
3.0.3  [2013-10-16] - Update to VirtualBoxSDK-4.3.0-89960.
3.0.2  [2013-07-29] - Update to VirtualBoxSDK-4.2.18-88780.
3.0.1  [2013-07-11] - Update to VirtualBoxSDK-4.2.16-86992. Fix bug when no all commands was removed on server stop.
3.0.0  [2013-06-19] - New version with more servers, running commands (in new thread) and run ping in new thread.
2.1.2  [2013-04-28] - Update to VirtualBoxSDK-4.2.12-84980.
2.1.1  [2013-03-22] - Custom configuration file may not be in the directory with the application, update to VirtualBoxSDK-4.2.10-84104.
2.1.0  [2012-12-20] - Run server in headless mode, replace VirtualBox console with SSH client, add email monitoring, update to VirtualBoxSDK-4.2.6-82870
2.0.4  [2012-09-17] - Update to VirtualBoxSDK-4.2.0-80737.
2.0.3  [2012-06-23] - Update to VirtualBoxSDK-4.1.18-78361.
2.0.2  [2012-06-06] - Bug fix, bad state detection when external server start after server stop.
2.0.1  [2012-05-28] - Update to VirtualBoxSDK-4.1.16-78094.
2.0.0  [2012-05-07] - Rewritten using VirtualBox SDK, icons changed.
1.0.0  [2010-01-01] - First public version in C#


REQUIREMENTS
============
You need .NET Framework 4 to run this application (http://www.microsoft.com/en-us/download/details.aspx?id=17851).


LICENSE
=======
Devel79 Tray is distributed under BSD license. See license.txt.


https://github.com/forrest79/devel79tray
