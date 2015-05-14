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
    public class emQuaternion
    {
        public double w;
        public double x, y, z;

        public emQuaternion()
            : this(0, 0, 0, 1)
        {
        }

        public emQuaternion(double W, double X, double Y, double Z)
        {
            x = X;
            y = Y;
            z = Z;
            w = W;
        }

        public emQuaternion(emQuaternion shallow)
            : this(shallow.w, shallow.x, shallow.y, shallow.z)
        {
        }

        public emQuaternion(gm.Quaternion shallow)
            : this(shallow.w, shallow.x, shallow.y, shallow.z)
        {
        }

        public gm.Quaternion ToMsg()
        {
            return new gm.Quaternion { w = w, x = x, y = y, z = z };
        }

        public static emQuaternion operator +(emQuaternion v1, emQuaternion v2)
        {
            return new emQuaternion(v1.w + v2.w, v1.x + v2.x, v1.y + v2.y, v1.z + v2.z);
        }

        public static emQuaternion operator *(emQuaternion v1, float d)
        {
            return v1 * ((double)d);
        }

        public static emQuaternion operator *(emQuaternion v1, int d)
        {
            return v1 * ((double)d);
        }

        public static emQuaternion operator *(emQuaternion v1, double d)
        {
            return new emQuaternion(v1.x * d, v1.y * d, v1.z * d, v1.w * d);
        }

        public static emQuaternion operator *(float d, emQuaternion v1)
        {
            return v1 * ((double)d);
        }

        public static emQuaternion operator *(int d, emQuaternion v1)
        {
            return v1 * ((double)d);
        }

        public static emQuaternion operator *(double d, emQuaternion v1)
        {
            return new emQuaternion(v1.x * d, v1.y * d, v1.z * d, v1.w * d);
        }

        public static emQuaternion operator *(emQuaternion v1, emQuaternion v2)
        {
            return new emQuaternion(v1.w * v2.x + v1.x * v2.x + v1.y * v2.z - v1.z * v2.y,
                v1.w * v2.y + v1.y * v2.w + v1.z * v2.x - v1.x * v2.z,
                v1.w * v2.z + v1.z * v2.w + v1.x * v2.y - v1.y * v2.x,
                v1.w * v2.w - v1.x * v2.x - v1.y * v2.y - v1.z * v2.z);
        }

        public static emQuaternion operator /(emQuaternion v1, float s)
        {
            return v1 / ((double)s);
        }

        public static emQuaternion operator /(emQuaternion v1, int s)
        {
            return v1 / ((double)s);
        }

        public static emQuaternion operator /(emQuaternion v1, double s)
        {
            return v1 * (1.0 / s);
        }

        public emQuaternion inverse()
        {
            return new emQuaternion(-x, -y, -z, w);
        }

        public double dot(emQuaternion q)
        {
            return x * q.x + y * q.y + z * q.z + w * q.w;
        }

        public double length2()
        {
            return dot(this);
        }

        public double length()
        {
            return Math.Sqrt(length2());
        }

        public override string ToString()
        {
            return string.Format("({0},{1},{2},{3})", w, x, y, z);
        }

        public emVector3 getRPY()
        {
            emVector3 tmp = new emMatrix3x3(this).getYPR();
            return new emVector3(tmp.z, tmp.y, tmp.x);
            emVector3 ret = new emVector3();
            double w2 = w * w;
            double x2 = x * x;
            double y2 = y * y;
            double z2 = z * z;
            double unitLength = length(); // Normalized == 1, otherwise correction divisor.
            double abcd = w * x + y * z;
            double eps = Math.E;
            double pi = Math.PI;
            if (abcd > (0.5 - eps) * unitLength)
            {
                ret.z = 2 * Math.Atan2(y, w);
                ret.y = pi;
                ret.x = 0;
            }
            else if (abcd < (-0.5 + eps) * unitLength)
            {
                ret.z = -2 * Math.Atan2(y, w);
                ret.y = -pi;
                ret.x = 0;
            }
            else
            {
                double adbc = w * z - x * y;
                double acbd = w * y - x * z;
                ret.z = Math.Atan2(2 * adbc, 1 - 2 * (z2 + x2));
                ret.y = Math.Asin(2 * abcd / unitLength);
                ret.x = Math.Atan2(2 * acbd, 1 - 2 * (y2 + x2));
            }
            return ret;
        }

        public static emQuaternion FromRPY(emVector3 rpy)
        {
            double halfroll = rpy.x / 2;
            double halfpitch = rpy.y / 2;
            double halfyaw = rpy.z / 2;

            double sin_r2 = Math.Sin(halfroll);
            double sin_p2 = Math.Sin(halfpitch);
            double sin_y2 = Math.Sin(halfyaw);

            double cos_r2 = Math.Cos(halfroll);
            double cos_p2 = Math.Cos(halfpitch);
            double cos_y2 = Math.Cos(halfyaw);

            return new emQuaternion(
                cos_r2 * cos_p2 * cos_y2 + sin_r2 * sin_p2 * sin_y2,
                sin_r2 * cos_p2 * cos_y2 - cos_r2 * sin_p2 * sin_y2,
                cos_r2 * sin_p2 * cos_y2 + sin_r2 * cos_p2 * sin_y2,
                cos_r2 * cos_p2 * sin_y2 - sin_r2 * sin_p2 * cos_y2
                );
        }

        public double angleShortestPath(emQuaternion q)
        {
            double s = Math.Sqrt(length2() * q.length2());
            if (dot(q) < 0)
                return Math.Acos(dot(-1 * q) / s) * 2.0;
            return Math.Acos(dot(q) / s) * 2.0;
        }

        public emQuaternion slerp(emQuaternion q, double t)
        {
            double theta = angleShortestPath(q);
            if (theta != 0)
            {
                double d = 1.0 / Math.Sin(theta);
                double s0 = Math.Sin(1.0 - t) * theta;
                double s1 = Math.Sin(t * theta);
                if (dot(q) < 0)
                {
                    return new emQuaternion(
                        (w * s0 + -1 * q.w * s1) * d,
                        (x * s0 + -1 * q.x * s1) * d,
                        (y * s0 + -1 * q.y * s1) * d,
                        (z * s0 + -1 * q.z * s1) * d);
                }
                return new emQuaternion((w * s0 + q.w * s1) * d,
                    (x * s0 + q.x * s1) * d,
                    (y * s0 + q.y * s1) * d,
                    (z * s0 + q.z * s1) * d);
            }
            return new emQuaternion(this);
        }
    }
}