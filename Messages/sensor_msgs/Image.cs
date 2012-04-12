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

namespace Messages.sensor_msgs
{

		public class Image
		{
			public Header header;
			public uint height;
			public uint width;
			public String encoding;
			public byte is_bigendian;
			public uint step;
			public byte[] data;
		}
}
