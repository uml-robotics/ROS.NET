using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;

using Messages.std_msgs;
using Messages.geometry_msgs;
using Messages.nav_msgs;

namespace Messages.geometry_msgs
{

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct Vector3Stamped
		{
			public Header header;
			public Vector3 vector;
		}
}
