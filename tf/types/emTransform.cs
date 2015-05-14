#region USINGZ

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Messages;
using Messages.std_msgs;
using Messages.tf;
using Ros_CSharp;
using gm = Messages.geometry_msgs;
using Int64 = System.Int64;
using String = Messages.std_msgs.String;

#endregion

namespace tf
{

    [DebuggerStepThrough]
    public class emTransform
    {
        public string child_frame_id;
        public string frame_id;

        public emQuaternion basis;
        public Time stamp;
        public emVector3 origin;

        public emTransform()
            : this(new emQuaternion(), new emVector3(), new Time(new TimeData()), "", "")
        {
        }

        public emTransform(gm.TransformStamped msg)
            : this(new emQuaternion(msg.transform.rotation), new emVector3(msg.transform.translation), msg.header.stamp, msg.header.frame_id.data, msg.child_frame_id.data)
        {
        }

        public emTransform(emQuaternion q, emVector3 v)
            : this(q, v, null, null, null)
        {
        }
        public emTransform(emQuaternion q, emVector3 v, Time t, string fid, string cfi)
        {
            basis = q;
            origin = v;
            stamp = t;

            frame_id = fid;
            child_frame_id = cfi;
        }



        public static emTransform operator *(emTransform t, emTransform v)
        {
            return new emTransform(t.basis * v.basis, t * v.origin);
        }

        public static emVector3 operator *(emTransform t, emVector3 v)
        {
            emMatrix3x3 mat = new emMatrix3x3(t.basis);
            return new emVector3(mat.m_el[0].dot(v) + t.origin.x,
                mat.m_el[1].dot(v) + t.origin.y,
                mat.m_el[2].dot(v) + t.origin.z);
        }

        public static emQuaternion operator *(emTransform t, emQuaternion q)
        {
            return t.basis * q;
        }
    }
}