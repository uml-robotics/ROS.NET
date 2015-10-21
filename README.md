# ROS.NET

## What is ROS,NET?
A series of C# projects and one C++ project (a p/invoke wrapper around XMLRPC++) that allow a MANAGED .NET application co communicate with any other ROS nodes.

## How do I use it?
- Set your ROS_HOSTNAME and ROS_MASTER_URI in your windows environment variables
- Open one of the solutions
- Compile + run (or change the startup project to one of the other runnable projects, and do the same)

### To use ros.net in your project
I've found that creating a new repository, adding ros.net as a submodule, creating a .sln file in the new repository, and adding YOUR project, along with the ros.net dependencies (usually the ones in Template_ROS_dotNET.sln (would require path corrections in notepad to copy+paste) PLUS ROS_ImageUtil, in my experimence) to that .sln file is a reasonably painfree workflow.

## We will soon create a more substantial page of how to use it, including magic tricks like...
- to add a message, bar, from a package foo, add "foo/msg/bar.msg" (and its parent directories, of course) to any folder within the parent directory of your .sln file.
- or that, to use messages, you need to add a reference to the Messages/Messages.dll directly, rather than the Messages project, because the messages project is GENERATED.

### Are there external dependencies?
- You need to have visual studio 2010+ and .Net >=4.0
	(express might?? work, but you need to be able to compile the XmlRpc++ library AND C# applications. XmlRpcWin32 doesn't change much, so you could compile it once, and then use Visual Studio Express to work in C#... but the XmlRpcWin32.dll will need to wind up in your project's output directory)
- For DynamicReconfigure, you need the Microsoft Expression Blend SDK (https://www.microsoft.com/en-us/download/details.aspx?id=10801)

## What is the difference between these .sln files?
- Template_ROS_dotNET.sln
  - the minimum references to ROS.NET-ify your project
- ROS_dotNET.sln
  - Template + rosmaster and rosparam commandline tools
- ROS_dotNET_ROS_SAMPLES
  - Template + talker, listener, service client, service server, and a multi-panel image viewer
- DynamicReconfigure_dotNET.sln
  - Template + DynamicReconfigure WPF window (requires additional dependency linked above)

#### What do the projects do?
- It has a DYNAMIC RECONFIGURE implementation, and all of the prerequisite features to accomplish one.
- It can generate a C# messages DLL containing standard, as well as custom message classes that will match MD5s (99% of the time) with and successfully send+resceive (100% of the time md5s match) to+from ROS nodes in officially supported ros client languages
- Allows a nearly ROSCPP API for all of the familiar ROS programming elements: publishers, subscribers, rosparam, service clients and servers, etc.

#### Is it stable?
- I could lie to you, but I won't.
- It is not without its quirks, but during the course of its development, it has been used to communicate between various WPF windows and one or more robots for a handful of research projects, and could not have easily been replaced.
- Transformers are __SLIGHTLY__ broken, and becoming LESS SO.

* It's generally leak free and fairly light on resources, but doing more things at compile time than are presently being done (de)serialization-wise would be a step in the right direction. *

Apologies in advance for the minimal documentation, but I've been busy using it.

I hope this can be of use to someone!

Some of the included code is leftovers from a few projects it was used on over the years, so allow me to apologize in advance about some of the atrocities you might find there-in.

Longer-term, a more programmer-friendly layout (msg/ folders in package folders, for example... rather than one giant message folder in YAMLParser) might facilitate making ROS.NET something that could exist on the system and be used with a variable subset of messages included for specific targets, more similar to how ROSCPP and ROSPY operate under linux.

Enjoy!

-Eric McCann (a.k.a. nuclearmistake) @ the University of Massachusetts Lowell Robotics Lab

P.S.: If anyone has the strong urge, strong C# chops, and likes to read code and hack it to bits... This codebase could really use a revised YAMLParser that can avoid the significant knot of introspection occurring in SerializationHelper. Unfortunately, the gains from such a rewrite place it at around 53 on my priority queue of things to do... but I might get to it someday if nobody is itching to change how it is now.
