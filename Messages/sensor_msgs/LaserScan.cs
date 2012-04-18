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

		public class LaserScan
		{
			public Header header;
			public float angle_min;
			public float angle_max;
			public float angle_increment;
			public float time_increment;
			public float scan_time;
			public float range_min;
			public float range_max;
			public float[] ranges;
			public float[] intensities;
		}
}
