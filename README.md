**What is ROS,NET?**
- A series of C# projects and one C++ project (a p/invoke wrapper around XMLRPC++) that allow a MANAGED .NET application co communicate with any other ROS nodes.

**What do the projects do?**
- It has a DYNAMIC RECONFIGURE implementation, and all of the prerequisite features to accomplish one.
- It can generate a C# messages DLL containing standard, as well as custom message classes that will match MD5s (99% of the time) with and successfully send+resceive (100% of the time md5s match) to+from ROS nodes in officially supported ros client languages
- Allows a nearly ROSCPP API for all of the familiar ROS programming elements: publishers, subscribers, rosparam, service clients and servers, etc.

**Is it stable?**
- I could lie to you, but I won't.
- It is not without its quirks, but during the course of its development, it has been used to communicate between various WPF windows and one or more robots for a handful of research projects, and could not have easily been replaced.
- TF is pretty broken. Most of ROS.NET was designed based structurally on ROSCPP from the diamondback era... and TF was one of the LAST things that was added to ROS.NET. As the reason TF was being implemented was critical-path, the decision was made to offload the TF work to a helper node on a linux machine, and it hasn't been revisited since.

* It's generally leak free and fairly light on resources, but doing more things at compile time than are presently being done (de)serialization-wise would be a step in the right direction. *

**How do I use it?**
- Set your ROS_HOSTNAME and ROS_MASTER_URI in your windows environment variables
- Open up ROS_dotNET.sln
- Then, pick one of the samples: (talker, listener, a camera image viewer (WPF) and add2ints (service example) are included.

**We will soon create a more substantial page of how to use it, including magic tricks like...**
- to add a message, bar, from a stack foo, you copy bar.msg into YAMLParser/ROS_MESSAGES/foo and set the "copy to output directory" to "copy if newer" for it, then compile
- or that, to use messages, you need to add a reference to the Messages/Messages.dll directly, rather than the Messages project, because the messages project is GENERATED.

Apologies in advance for the minimal documentation, but I've been busy using it.

I hope this can be of use to someone!

Some of the included code is leftovers from a few projects it was used on over the years, so allow me to apologize in advance about some of the attrocities you might find there-in.

Longer-term, a more programmer-friendly layout (msg/ folders in package folders, for example... rather than one giant message folder in YAMLParser) might facilitate making ROS.NET something that could exist on the system and be used with a variable subset of messages included for specific targets, more similar to how ROSCPP and ROSPY operate under linux.

Enjoy!

-Eric McCann (a.k.a. nuclearmistake) @ the University of Massachusetts Lowell Robotics Lab

P.S.: If anyone has the strong urge, strong C# chops, and likes to read code and hack it to bits... This codebase could really use a revised YAMLParser that can avoid the significant knot of introspection occurring in SerializationHelper. Unfortunately, the gains from such a rewrite place it at around 53 on my priority queue of things to do... but I might get to it someday if nobody is itching to change how it is now.
