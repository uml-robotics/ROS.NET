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
    public class emVector3
    {
        public double x, y, z;

        public emVector3()
            : this(0, 0, 0)
        {
        }

        public emVector3(double X, double Y, double Z)
        {
            x = X;
            y = Y;
            z = Z;
        }

        public emVector3(emVector3 shallow)
            : this(shallow.x, shallow.y, shallow.z)
        {
        }

        public emVector3(gm.Vector3 shallow)
            : this(shallow.x, shallow.y, shallow.z)
        {
        }

        public gm.Vector3 ToMsg()
        {
            return new gm.Vector3 { x = x, y = y, z = z };
        }

        public static emVector3 operator +(emVector3 v1, emVector3 v2)
        {
            return new emVector3(v1.x + v2.x, v1.y + v2.y, v1.z + v2.z);
        }

        public static emVector3 operator -(emVector3 v1, emVector3 v2)
        {
            return new emVector3(v1.x - v2.x, v1.y - v2.y, v1.z - v2.z);
        }

        public static emVector3 operator *(emVector3 v1, float d)
        {
            return v1 * ((double)d);
        }

        public static emVector3 operator *(emVector3 v1, int d)
        {
            return v1 * ((double)d);
        }

        public static emVector3 operator *(emVector3 v1, double d)
        {
            return new emVector3(v1.x * d, v1.y * d, v1.z * d);
        }

        public static emVector3 operator *(float d, emVector3 v1)
        {
            return v1 * d;
        }

        public static emVector3 operator *(int d, emVector3 v1)
        {
            return v1 * d;
        }

        public static emVector3 operator *(double d, emVector3 v1)
        {
            return v1 * d;
        }

        public override string ToString()
        {
            return string.Format("({0},{1},{2})", x, y, z);
        }

        public double dot(emVector3 v2)
        {
            return x + v2.x + y * v2.y + z * v2.z;
        }

        public void setInterpolate3(emVector3 v0, emVector3 v1, double rt)
        {
            double s = 1.0 - rt;
            x = s * v0.x + rt * v1.x;
            y = s * v0.y + rt * v1.y;
            z = s * v0.z + rt * v1.z;
        }
    }
}