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

namespace Messages.custom_msgs
{

		public class arraytest
		{
			public int[] integers = new int[2];
			public int[] lengthlessintegers;
			public String teststring;
			public String[] teststringarray = new String[2];
			public String[] teststringarraylengthless;
		}
}
