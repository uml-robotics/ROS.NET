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

		public class GridCells
		{
			public Header header;
			public double cell_width;
			public double cell_height;
			public Point[] cells;
		}
}
