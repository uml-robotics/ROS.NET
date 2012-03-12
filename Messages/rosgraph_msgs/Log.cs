using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;

using Messages.std_msgs;
using Messages.geometry_msgs;
using Messages.nav_msgs;
using String=Messages.std_msgs.String;

namespace Messages.rosgraph_msgs
{

		public class Log
		{
			public const byte DEBUG = 1;
			public const byte INFO = 2;
			public const byte WARN = 4;
			public const byte ERROR = 8;
			public const byte FATAL = 16;
			public Header header;
			public byte level;
			public String name;
			public String msg;
			public String file;
			public String function;
			public uint line;
			public String[] topics;
		}
}
