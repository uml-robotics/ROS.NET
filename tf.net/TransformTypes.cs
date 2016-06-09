// File: tf_node.cs
// Project: ROS_C-Sharp
// 
// ROS.NET
// Eric McCann <emccann@cs.uml.edu>
// UMass Lowell Robotics Laboratory
// 
// Reimplementation of the ROS (ros.org) ros_cpp client in C#.
// 
// Created: 04/28/2015
// Updated: 02/10/2016

#if UNITY
#define FOR_UNITY
#endif
#if ENABLE_MONO
#define FOR_UNITY
#endif

#region USINGZ

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Messages;
using Messages.std_msgs;
using Messages.tf;
using gm = Messages.geometry_msgs;

#endregion

namespace tf.net
{
#if !TRACE
    [DebuggerStepThrough]
#endif
    public class emMatrix3x3
    {
        public emVector3[] m_el = new emVector3[3];

        public emMatrix3x3() : this(0, 0, 0, 0, 0, 0, 0, 0, 0)
        {
        }

        public emMatrix3x3(double xx, double xy, double xz,
            double yx, double yy, double yz,
            double zx, double zy, double zz)
        {
            m_el[0] = new emVector3(xx, xy, xz);
            m_el[1] = new emVector3(yx, yy, yz);
            m_el[2] = new emVector3(zx, zy, zz);
        }

        public emMatrix3x3(emQuaternion q)
        {
            setRotation(q);
        }

        public void setValue(double xx, double xy, double xz,
            double yx, double yy, double yz,
            double zx, double zy, double zz)
        {
            m_el[0] = new emVector3(xx, xy, xz);
            m_el[1] = new emVector3(yx, yy, yz);
            m_el[2] = new emVector3(zx, zy, zz);
        }

        public void setRotation(emQuaternion q)
        {
            double d = q.length2();
            double s = 2.0/d;
            double xs = q.x*s, ys = q.y*s, zs = q.z*s;
            double wx = q.w*xs, wy = q.w*ys, wz = q.w*zs;
            double xx = q.x*xs, xy = q.x*ys, xz = q.x*zs;
            double yy = q.y*ys, yz = q.y*zs, zz = q.z*zs;
            setValue(1.0 - (yy + zz), xy - wz, xz + wy,
                xy + wz, 1.0 - (xx + zz), yz - wx,
                xz - wy, yz + wx, 1.0 - (xx + yy));
        }

        internal emVector3 getYPR(uint solution_number = 1)
        {
            Euler euler_out;
            Euler euler_out2; //second solution
            //get the pointer to the raw data

            // Check that pitch is not at a singularity
            // Check that pitch is not at a singularity
            if (Math.Abs(m_el[2].x) >= 1)
            {
                euler_out.yaw = 0;
                euler_out2.yaw = 0;

                // From difference of angles formula
                double delta = Math.Atan2(m_el[2].y, m_el[2].z);
                if (m_el[2].x < 0) //gimbal locked down
                {
                    euler_out.pitch = Math.PI/2.0d;
                    euler_out2.pitch = Math.PI/2.0d;
                    euler_out.roll = delta;
                    euler_out2.roll = delta;
                }
                else // gimbal locked up
                {
                    euler_out.pitch = -Math.PI/2.0d;
                    euler_out2.pitch = -Math.PI/2.0d;
                    euler_out.roll = delta;
                    euler_out2.roll = delta;
                }
            }
            else
            {
                euler_out.pitch = -Math.Asin(m_el[2].x);
                euler_out2.pitch = Math.PI - euler_out.pitch;

                euler_out.roll = Math.Atan2(m_el[2].y/Math.Cos(euler_out.pitch),
                    m_el[2].z/Math.Cos(euler_out.pitch));
                euler_out2.roll = Math.Atan2(m_el[2].y/Math.Cos(euler_out2.pitch),
                    m_el[2].z/Math.Cos(euler_out2.pitch));

                euler_out.yaw = Math.Atan2(m_el[1].x/Math.Cos(euler_out.pitch),
                    m_el[0].x/Math.Cos(euler_out.pitch));
                euler_out2.yaw = Math.Atan2(m_el[1].x/Math.Cos(euler_out2.pitch),
                    m_el[0].x/Math.Cos(euler_out2.pitch));
            }

            if (solution_number == 1)
            {
                /*yaw = euler_out.yaw; 
                pitch = euler_out.pitch;
                roll = euler_out.roll;*/
                return new emVector3(euler_out.yaw, euler_out.pitch, euler_out.roll);
            }
            return new emVector3(euler_out2.yaw, euler_out2.pitch, euler_out2.roll);
        }

        public struct Euler
        {
            public double pitch;
            public double roll;
            public double yaw;
        }
    }

#if !TRACE
    [DebuggerStepThrough]
#endif
    public class emTransform
    {
        public string child_frame_id;
        public string frame_id;

        public emQuaternion basis;
        public Time stamp;
        public emVector3 origin;

#if FOR_UNITY
        public emTransform(UnityEngine.GameObject go, Time t = null, string fid = null, string cfi = null)
                : this(go.transform,t,fid,cfi)
        {
        }

        public emTransform(UnityEngine.Transform T, Time t = null, string fid = null, string cfi = null)
        {
            UnityRotation = T.rotation;
            UnityPosition = T.position;
            stamp = t;
            frame_id = fid;
            child_frame_id = cfi;
        }

        public emTransform(UnityEngine.Vector3 v, UnityEngine.Quaternion q, Time t = null, string fid = null, string cfi = null)
        {
            UnityRotation = q;
            UnityPosition = v;
            stamp = t;
            frame_id = fid;
            child_frame_id = cfi;
        }

        public UnityEngine.Vector3? UnityPosition
        {
            get { return origin != null ? (Nullable<UnityEngine.Vector3>)origin.UnityPosition : null; }
            set { if (origin == null) origin = new emVector3(value.Value); else origin.UnityPosition = value.Value; }
        }

        public UnityEngine.Quaternion? UnityRotation
        {
            get { return basis != null ? (Nullable<UnityEngine.Quaternion>)basis.UnityRotation : null; }
            set { if (basis == null) basis = new emQuaternion(value.Value); else basis.UnityRotation = value.Value; }
        }
#endif

        public emTransform() : this(new emQuaternion(), new emVector3(), new Time(new TimeData()), "", "")
        {
        }

        public emTransform(gm.TransformStamped msg) : this(new emQuaternion(msg.transform.rotation), new emVector3(msg.transform.translation), msg.header.stamp, msg.header.frame_id, msg.child_frame_id)
        {
        }

        public emTransform(emQuaternion q, emVector3 v, Time t = null, string fid = null, string cfi = null)
        {
            basis = q;
            origin = v;
            stamp = t;
            frame_id = fid;
            child_frame_id = cfi;
        }


        public static emTransform operator *(emTransform t, emTransform v)
        {
            return new emTransform(t.basis*v.basis, t*v.origin);
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
            return t.basis*q;
        }

        public override string ToString()
        {
            return "\ttranslation: " + origin + "\n\trotation: " + basis;
        }
    }

#if !TRACE
    [DebuggerStepThrough]
#endif
    public class emQuaternion
    {
        public double w, x, y, z;

        public emQuaternion() : this(0,0,0,1)
        {
        }
        public emQuaternion(double X, double Y, double Z, double W)
        {
            x = X;
            y = Y;
            z = Z;
            w = W;
        }

        public emQuaternion(emQuaternion shallow)
            : this(shallow.x, shallow.y, shallow.z, shallow.w)
        {
        }

        public emQuaternion(gm.Quaternion shallow)
            : this(shallow.x, shallow.y, shallow.z, shallow.w)
        {
        }

#if FOR_UNITY
        public emQuaternion(UnityEngine.Quaternion q)
        {
            UnityRotation = q;
        }

        public UnityEngine.Quaternion UnityRotation
        {
            get { return new UnityEngine.Quaternion((float)y, (float)-z, (float)-x, (float)w); }
            set { x = -value.z; y = value.x; z = -value.y; w = value.w; }
        }
#endif

        public gm.Quaternion ToMsg()
        {
            return new gm.Quaternion {w = w, x = x, y = y, z = z};
        }

        #region add
        public static emQuaternion operator +(emQuaternion v1, emQuaternion v2)
        {
            return new emQuaternion(v1.x+v2.x,v1.y+v2.y,v1.z+v2.z,v1.w+v2.w);
        }
        #endregion

        #region subtract
        public static emQuaternion operator -(emQuaternion v1)
        {
            return new emQuaternion(-v1.x, -v1.y, -v1.z, -v1.w);
        }

        public static emQuaternion operator -(emQuaternion v1, emQuaternion v2)
        {
            return v1 + (-v2);
        }
        #endregion

        #region mult
        public static emQuaternion operator *(emQuaternion v1, float d)
        {
            return v1*(double) d;
        }

        public static emQuaternion operator *(emQuaternion v1, int d)
        {
            return v1 * (double)d;
        }

        public static emQuaternion operator *(emQuaternion v1, double d)
        {
            return new emQuaternion(v1.x*d, v1.y*d, v1.z*d, v1.w);
        }

        public static emQuaternion operator *(float d, emQuaternion v1)
        {
            return v1 * (double)d;
        }

        public static emQuaternion operator *(int d, emQuaternion v1)
        {
            return v1 * (double)d;
        }

        public static emQuaternion operator *(double d, emQuaternion v1)
        {
            return v1 * d;
        }

        public static emQuaternion operator *(emQuaternion v1, emQuaternion v2)
        {
            return new emQuaternion(v1.x * v2.w + v1.y * v2.z - v1.z * v2.y + v1.w * v2.x,
                                    -v1.x * v2.z + v1.y * v2.w + v1.z * v2.x + v1.w * v2.y,
                                    v1.x * v2.y - v1.y * v2.x + v1.z * v2.w + v1.w * v2.z,
                                    -v1.x * v2.x - v1.y * v2.y - v1.z * v2.z + v1.w * v2.w);
        }

        public static emQuaternion operator *(emQuaternion v1, emVector3 v2)
        {
            return v1 * new emQuaternion(v2.x, v2.y, v2.z, 0.0);
        }
        #endregion

        #region div
        public static emQuaternion operator /(emQuaternion v1, float s)
        {
            return v1/(double) s;
        }

        public static emQuaternion operator /(emQuaternion v1, int s)
        {
            return v1 / (double)s;
        }

        public static emQuaternion operator /(emQuaternion v1, double s)
        {
            return v1*(1.0/s);
        }
        #endregion

        #region other ops
        public emQuaternion inverse()
        {
            return new emQuaternion(-x / norm, -y / norm, -z / norm, w / norm);
        }

        public double dot(emQuaternion q)
        {
            return x*q.x + y*q.y + z*q.z + w*q.w;
        }

        public double length2()
        {
            return abs*abs;
        }

        public double length()
        {
            return abs;
        }

        public double norm
        {
            get { return (x*x) + (y*y) + (z*z) + (w*w); }
        }

        public double abs {
            get { return Math.Sqrt(norm); }
        }

        public double arg {
            get { return Math.Acos(w/abs); }
        }

        #endregion

        public override string ToString()
        {
            return string.Format("quat=({0:F4},{1:F4},{2:F4},{3:F4})" /*, rpy={4}"*/, w, x, y, z /*, getRPY()*/);
        }

        public emVector3 getRPY()
        {
            emVector3 tmp = new emMatrix3x3(this).getYPR();
            return new emVector3(tmp.z, tmp.y, tmp.x);
        }

        public static emQuaternion FromRPY(emVector3 rpy)
        {
            double halfroll = rpy.x/2;
            double halfpitch = rpy.y/2;
            double halfyaw = rpy.z/2;

            double sin_r2 = Math.Sin(halfroll);
            double sin_p2 = Math.Sin(halfpitch);
            double sin_y2 = Math.Sin(halfyaw);

            double cos_r2 = Math.Cos(halfroll);
            double cos_p2 = Math.Cos(halfpitch);
            double cos_y2 = Math.Cos(halfyaw);

            return new emQuaternion(
                cos_r2*cos_p2*cos_y2 + sin_r2*sin_p2*sin_y2,
                sin_r2*cos_p2*cos_y2 - cos_r2*sin_p2*sin_y2,
                cos_r2*sin_p2*cos_y2 + sin_r2*cos_p2*sin_y2,
                cos_r2*cos_p2*sin_y2 - sin_r2*sin_p2*cos_y2
                );
        }

        public double angleShortestPath(emQuaternion q)
        {
            return (this - q).abs;
        }

        public emQuaternion slerp(emQuaternion q, double t)
        {
            double theta = angleShortestPath(q);
            if (theta != 0)
            {
                double d = 1.0/Math.Sin(theta);
                double s0 = Math.Sin(1.0 - t)*theta;
                double s1 = Math.Sin(t*theta);
                if (dot(q) < 0)
                {
                    return new emQuaternion(
                        (w*s0 + -1*q.w*s1)*d,
                        (x*s0 + -1*q.x*s1)*d,
                        (y*s0 + -1*q.y*s1)*d,
                        (z*s0 + -1*q.z*s1)*d);
                }
                return new emQuaternion((w*s0 + q.w*s1)*d,
                    (x*s0 + q.x*s1)*d,
                    (y*s0 + q.y*s1)*d,
                    (z*s0 + q.z*s1)*d);
            }
            return new emQuaternion(this);
        }
    }

#if !TRACE
    [DebuggerStepThrough]
#endif
    public class emVector3
    {
        public double x;
        public double y;
        public double z;

        public emVector3() : this(0, 0, 0)
        {
        }

        public emVector3(double X, double Y, double Z)
        {
            x = X;
            y = Y;
            z = Z;
        }

        public emVector3(emVector3 shallow) : this(shallow.x, shallow.y, shallow.z)
        {
        }

        public emVector3(gm.Vector3 shallow) : this(shallow.x, shallow.y, shallow.z)
        {
        }

#if FOR_UNITY
        public emVector3(UnityEngine.Vector3 v)
        {
            UnityPosition = v;
        }

        public UnityEngine.Vector3 UnityPosition
        {
            get { return new UnityEngine.Vector3((float)-y, (float)z, (float)x); }
            set { x = value.z; y = -value.x; z = value.y; }
        }
#endif

        public gm.Vector3 ToMsg()
        {
            return new gm.Vector3 {x = x, y = y, z = z};
        }

        #region add
        public static emVector3 operator +(emVector3 v1, emVector3 v2)
        {
            return new emVector3(v1.x+v2.x,v1.y+v2.y,v1.z+v2.z);
        }
        #endregion

        #region sub
        public static emVector3 operator -(emVector3 v1, emVector3 v2)
        {
            return new emVector3(v1.x - v2.x, v1.y - v2.y, v1.z - v2.z);
        }

        public static emVector3 operator -(emVector3 v1)
        {
            return new emVector3(-v1.x, -v1.y, -v1.z);
        }
        #endregion

        #region mult
        public static emVector3 operator *(emVector3 v1, float d)
        {
            return v1*((double) d);
        }

        public static emVector3 operator *(emVector3 v1, int d)
        {
            return v1*((double) d);
        }

        public static emVector3 operator *(emVector3 v1, double d)
        {
            return new emVector3(d*v1.x, d*v1.y, d*v1.z);
        }

        public static emVector3 operator *(float d, emVector3 v1)
        {
            return v1*((double) d);
        }

        public static emVector3 operator *(int d, emVector3 v1)
        {
            return v1 * ((double)d);
        }

        public static emVector3 operator *(double d, emVector3 v1)
        {
            return v1 * d;
        }
        #endregion

        public double dot(emVector3 v2)
        {
            return x*v2.x + y*v2.y + z*v2.z;
        }

        public override string ToString()
        {
            return string.Format("({0:F4},{1:F4},{2:F4})", x, y, z);
        }

        public void setInterpolate3(emVector3 v0, emVector3 v1, double rt)
        {
            double s = 1.0 - rt;
            x = s*v0.x + rt*v1.x;
            y = s*v0.y + rt*v1.y;
            z = s*v0.z + rt*v1.z;
        }
    }
}