Devel79 Tray © Jakub Trmota, 2018 (http://forrest79.net)


VirtualBox developer server tray icon.


HOW TO USE:
===========
devel79tray [--run|-r servers] [--config|-c filename]

Settings: --run, -r        run server (or servers divided by space) after application starts
          --config, -c     specify filename with configuration (default is "devel79tray.conf" in application directory)

Configuration file (make one or more [server] sections):
name = Server name to display
machine = VirtualBox machine name (case sensitive)
ssh = shell connect command to SSH server (used for Show console, can be Putty, ssh from msys or other SSH client)
watch = directory in which is watching for new files, format: NAME | MESSAGE | DIRECTORY, NAME is diplayed in menu, MESSAGE is shown while new file is created (optional, can be multiple directories)
command = command to run in format NAME : COMMAND, NAME is displayed in menu, COMMAND is run (optional, can be multiple commands)

Tray icon menu:
Double click - if server is running show VirtualBox console (SSH client) for top running server (running server which is more top in configuration file)
Show "SERVER" console - same as double click (only if some server is running)
Server:
- SERVER 1:
-- Restart server - stop and start server (for running
-- Stop server - stop server
-- Open NAME directory - open directory in Windows Explorer
-- COMMAND - run command
- SERVER 2:
...
Start server:
- SERVER 1 - run server (only if is stopped)
- SERVER 2 - run server (only if is stopped)
Exit - exit tray icon, if server(s) is/are running, show question for stop server(s)

Directory monitoring:
If watch in configuration is set to existing directory, all new files with are shown in balloon tooltip and if you click on this tooltip, the file is open in associated application.


HISTORY
=======
4.0.12 [2018-05-23] - Update to VirtualBoxSDK-5.2.12-122591.
4.0.11 [2018-03-07] - Update to VirtualBoxSDK-5.2.8-121009.
4.0.10 [2018-01-30] - Update to VirtualBoxSDK-5.2.6-120293.
4.0.9  [2017-12-06] - Update to VirtualBoxSDK-5.2.2-119230.
4.0.8  [2017-09-27] - Update to VirtualBoxSDK-5.2.0-118431.
4.0.7  [2017-09-27] - Update to VirtualBoxSDK-5.1.28-117968.
4.0.6  [2017-07-28] - Update to VirtualBoxSDK-5.1.26-117224.
4.0.5  [2017-05-08] - Update to VirtualBoxSDK-5.1.22-115126.
4.0.4  [2017-04-19] - Fix double click on tray icon with no server running.
4.0.3  [2017-03-20] - Update to VirtualBoxSDK-5.1.18-114002.
4.0.2  [2017-03-15] - Update to VirtualBoxSDK-5.1.16-113841.
4.0.1  [2017-01-31] - Update to VirtualBoxSDK-5.1.14-112924.
4.0.0  [2016-12-21] - Add support for running multiple servers in one time, add support for watching more directories, update to VirtualBoxSDK-5.1.12-112440.
3.0.21 [2016-08-26] - Update to VirtualBoxSDK-5.1.4-110228.
3.0.20 [2016-08-05] - Update to VirtualBoxSDK-5.1.2-108956.
3.0.19 [2016-07-14] - Update to VirtualBoxSDK-5.1.0-108711.
3.0.18 [2016-07-02] - Update to VirtualBoxSDK-5.0.24-108355.
3.0.17 [2016-04-30] - Update to VirtualBoxSDK-5.0.20-106931.
3.0.16 [2016-04-27] - Update to VirtualBoxSDK-5.0.18-106667.
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
