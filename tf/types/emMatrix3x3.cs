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
    public class emMatrix3x3
    {
        public emVector3[] m_el = new emVector3[3];

        public emMatrix3x3()
            : this(0, 0, 0, 0, 0, 0, 0, 0, 0)
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
            double s = 2.0 / d;
            double xs = q.x * s, ys = q.y * s, zs = q.z * s;
            double wx = q.w * xs, wy = q.w * ys, wz = q.w * zs;
            double xx = q.x * xs, xy = q.x * ys, xz = q.x * zs;
            double yy = q.y * ys, yz = q.y * zs, zz = q.z * zs;
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
                    euler_out.pitch = Math.PI / 2.0d;
                    euler_out2.pitch = Math.PI / 2.0d;
                    euler_out.roll = delta;
                    euler_out2.roll = delta;
                }
                else // gimbal locked up
                {
                    euler_out.pitch = -Math.PI / 2.0d;
                    euler_out2.pitch = -Math.PI / 2.0d;
                    euler_out.roll = delta;
                    euler_out2.roll = delta;
                }
            }
            else
            {
                euler_out.pitch = -Math.Asin(m_el[2].x);
                euler_out2.pitch = Math.PI - euler_out.pitch;

                euler_out.roll = Math.Atan2(m_el[2].y / Math.Cos(euler_out.pitch),
                    m_el[2].z / Math.Cos(euler_out.pitch));
                euler_out2.roll = Math.Atan2(m_el[2].y / Math.Cos(euler_out2.pitch),
                    m_el[2].z / Math.Cos(euler_out2.pitch));

                euler_out.yaw = Math.Atan2(m_el[1].x / Math.Cos(euler_out.pitch),
                    m_el[0].x / Math.Cos(euler_out.pitch));
                euler_out2.yaw = Math.Atan2(m_el[1].x / Math.Cos(euler_out2.pitch),
                    m_el[0].x / Math.Cos(euler_out2.pitch));
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
}