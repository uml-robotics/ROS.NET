using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;

using Messages.std_msgs;
using Messages.roscsharp;
using Messages.geometry_msgs;
using Messages.nav_msgs;
using String=Messages.std_msgs.String;

namespace Messages.std_msgs
{

		public class MultiArrayDimension
		{
			public String label;
			public uint size;
			public uint stride;
		}
}
