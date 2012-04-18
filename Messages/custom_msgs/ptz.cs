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

namespace Messages.custom_msgs
{

		public class ptz
		{
			public float x;
			public float y;
			public const int CAM_ABS = 0;
			public const int CAM_REL = 1;
			public const int CAM_VEL = 2;
			public int CAM_MODE;
		}
}
