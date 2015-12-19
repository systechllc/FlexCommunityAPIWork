Moxa creates a big line of ethernet based tools.  See www.moxa.com

These tools are not cheap but in my experience in device control their
products have been far superior to many of the knock off or alternate products.

In particular I have made extensive use of their NPort serial device servers.
They make these things in 1, 4, and many more port versions.  Once configured
and plugged into your LAN they allow access to serial devices over ethernet.

Two modes are supported:

Real comm mode which creates a COM## port that regular software can use as if
it was really a comm port.  To use this mode you must install their free admin software
which creates the comm port and attaches it to the remote server.

TCP Server mode which basically allows your program to open a standard TCP/IP connection
to the Moxa NPort device on a specific port and send/receive serial data through that connection.
This mode is nice because things become less tied to windows and more usable on other platforms.

The two installers here are programs and libraries provided by Moxa to talk to these devices.
I am putting them into the FlexCommunityAPIWork repository in case others want to use
these devices.  I am planning to use them in my attempt at full station automation.