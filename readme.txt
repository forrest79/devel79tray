Devel79 Tray © Jakub Trmota, 2012 (http://forrest79.net)


VirtualBox developer server tray icon.


HOW TO USE:
===========
devel79tray [--runserver|-r] [--config|-c filename]

Settings: --runserver, -r  run VirtualBox machine after application starts
          --config, -c     specify filename with configuration (default is "devel79.conf" in application directory)

Configuration file:
name = Server name to display
machine = VirtualBox machine name (case sensitive)
ip = VirtualBox Network adapter IP address (used for ping command)
ssh = shell connect command to SSH server (used for Show console, can be Putty, ssh from msys or other SSH client)
email = directory witch is watching for new saved email

Tray icon menu:
Double click - if server is running show VirtualBox console (SSH client)
Show console - if server is running show VirtualBox console (SSH client)
Start server - if server is not running, start server
Restart server - if server is running, stop and start server
Stop server - if server is running, stop server
Ping server - if server is running, ping to server IP address a show result
Open email directory - open email directory in Windows Explorer
Exit - exit tray icon, if server is running, show question for stop server

Email monitoring:
If email in config is set to existing directory, all new files with .eml extension are shown in balloon tooltip and if you click on this tooltip, the email is open in associated application.


HISTORY
=======
1.0.0 [2010-01-01] - First public version in C#
2.0.0 [2012-05-07] - Rewritten using VirtualBox SDK, icons changed.
2.0.1 [2012-05-28] - Update to VirtualBoxSDK-4.1.16-78094.
2.0.2 [2012-06-06] - Bug fix, bad state detection when external server start after server stop.
2.0.3 [2012-06-23] - Update to VirtualBoxSDK-4.1.18-78361.
2.0.4 [2012-09-17] - Update to VirtualBoxSDK-4.2.0-80737.
2.1.0 [2012-12-20] - Run server in headless mode, replace VirtualBox console with SSH client, add email monitoring, update to VirtualBoxSDK-4.2.6-82870
2.1.1 [2013-03-22] - Custom configuration file may not be in the directory with the application, update to VirtualBoxSDK-4.2.10-84104.

REQUIREMENTS
============
You need .NET Framework 4 to run this application (http://www.microsoft.com/en-us/download/details.aspx?id=17851).


LICENSE
=======
Devel79 Tray is distributed under BSD license. See license.txt.


https://github.com/forrest79/devel79tray
