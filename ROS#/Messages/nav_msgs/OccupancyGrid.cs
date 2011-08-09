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

namespace Messages.nav_msgs
{

		public class OccupancyGrid
		{
			public Header header;
			public MapMetaData info;
			public sbyte[] data;
		}
}
