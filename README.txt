1. You need to be using Windows 7. (As a VPN is being used to communicate with the master and the other nodes, there shouldn't be any issues from the VM's network being virtual)
2. If you don't already have Visual Studio 2010, you can get an installer from the office next to the computer labs on the third floor (the desk between Ken's and Tuyen's offices)
3. Instal the .NET 4 SDK and Windows 7 SDKs (both available on microsot.com somewhere)
4. Install OpenVPN
         (for 32bit windows, download: http://swupdate.openvpn.org/community/releases/openvpn-install-2.3.0-I005-i686.exe )
         (for 64bit windows, download: http://swupdate.openvpn.org/community/releases/openvpn-install-2.3.0-I005-x86_64.exe )
5. Install putty (for SSHing into the robot/master)
6. Download and run http://www.cs.uml.edu/~emccann/robotics/ROS_Sharp.exe
7. Find OpenVPN GUI in your start menu, run it, then find the icon it created in your system tray, and choose "connect".
8. Mouse over the icon in your system tray. Make note of your assigned IP, as you will need to use that IP address in the ROS.ROS_HOSTNAME assignment in the nodes' initialization code.
                            
9. Browse to C:\ROS_Sharp on your hard drive, and double-click on "ROS_dotNET.sln" (if it doesn't open in visual studio 2010, right click on it and choose VS2010 from the open with menu)
10. Right click on the "Talker" project in your solution explorur, and select "use as startup project"
11. Customize it for your specific IP address
      -Expand the Talker project in the solution explorer
      -Open Talker.cs
      -The ROS_MASTER_URI is set to my lab machine, and should be running roscore already
      -The ROS_HOSTNAME needs to be set to the "Assigned IP" as visible in the attached screenshot.
12. Hit "F5" to run the program, and start publishing /Chatter messages.
13. To make sure they're visible to other nodes, open up putty, and connect to csrobot@10.0.3.88 (password = 4o6ot)... once connected, rostopic echo /Chatter... if you think you're seeing other students' messages, feel free to change the topic name to something more unique

Let me know how it goes!