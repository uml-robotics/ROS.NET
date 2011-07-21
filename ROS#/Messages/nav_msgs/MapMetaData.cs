using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;

using Messages.std_msgs;
using Messages.geometry_msgs;
using Messages.nav_msgs;

namespace Messages.nav_msgs
{

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct MapMetaData
		{
			public DateTime map_load_time;
			public double resolution;
			public uint width;
			public uint height;
			public Pose origin;
		}
}
