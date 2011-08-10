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

namespace Messages.std_msgs
{

		public class Time
		{
			public TimeData data;


			public Time(uint s, uint ns) : this(new TimeData{ sec=s, nsec = ns}){}
			public Time(TimeData s){ data = s; }
			public Time() : this(0,0){}

		}
}
