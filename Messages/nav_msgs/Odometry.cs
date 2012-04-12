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

namespace Messages.nav_msgs
{

		public class Odometry
		{
			public Header header;
			public String child_frame_id;
			public PoseWithCovariance pose;
			public TwistWithCovariance twist;
		}
}
