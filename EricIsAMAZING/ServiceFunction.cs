using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ros_CSharp
{
    public delegate bool ServiceFunction<MReq,MRes>(MReq req, ref MRes res);
}
