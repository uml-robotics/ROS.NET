1. You need to be using Windows 7 or higher (no ARM versions).
2. You need to have Visual Studio 2010 or 2012
3. Instal the .NET 4 SDK and Windows 7 SDKs (both available on microsot.com somewhere)
4. Open "ROS_dotNET.sln" in Visual Studio
5. Right click on the "Talker" project in your solution explorur, and select "use as startup project"
6. Add environment variables for ROS_MASTER_URI (and ROS_HOSTNAME, if applicable)
      -Expand the Talker project in the solution explorer
      -Open Talker.cs
      -The ROS_MASTER_URI is set to my lab machine, and should be running roscore already
      -The ROS_HOSTNAME needs to be set to the "Assigned IP" as visible in the attached screenshot.
7. Hit "F5" to run the program, and start publishing /Chatter messages.
