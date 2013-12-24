// File: tf_node.cs
// Project: ROS_C-Sharp
// 
// ROS#
// Eric McCann <emccann@cs.uml.edu>
// UMass Lowell Robotics Laboratory
// 
// Reimplementation of the ROS (ros.org) ros_cpp client in C#.
// 
// Created: 07/01/2013
// Updated: 07/26/2013

using System;
using System.Collections.Generic;
using System.Threading;
using Messages;
using Messages.std_msgs;
using Messages.tf;
using gm = Messages.geometry_msgs;
using String = Messages.std_msgs.String;

namespace Ros_CSharp
{
    // Listenes to the /tf topic, need subscriber
    // for each Transform in /tf, create a new frame. Frame has a (frame)child and (frame)id
    // provide translation from 2 frames, user requests from /map to /base_link for example, must identify route
    // base_link.child = odom, odom.child = map
    // map-> odom + odom->base_link
    public class tf_node
    {
        private static tf_node _instance;

        private Queue<tfMessage> additions;
        public object addlock = new object();
        public object frameslock = new object();
        private NodeHandle tfhandle;
        public Transformer transformer = new Transformer();
        public Thread updateThread;

        public tf_node()
        {
            if (additions == null)
                additions = new Queue<tfMessage>();
            if (tfhandle == null)
            {
                tfhandle = new NodeHandle();
            }
            if (updateThread == null)
            {
                updateThread = new Thread(() =>
                {
                    while (ROS.ok)
                    {
                        Queue<tfMessage> local;
                        lock (addlock)
                        {
                            local = new Queue<tfMessage>(additions);
                            additions.Clear();
                        }
                        if (local.Count > 0)
                        {
                            while (local.Count > 0)
                            {
                                tfMessage msg = local.Dequeue();
                                foreach (gm.TransformStamped t in msg.transforms)
                                {
                                    transformer.setTransform(new emTransform(t));
                                }
                            }
                        }
                        Thread.Sleep(100);
                    }
                });
                updateThread.Start();
            }
            tfhandle.subscribe<tfMessage>("/tf", 1, tfCallback);
        }

        public static tf_node instance
        {
            get
            {
                if (_instance == null)
                    _instance = new tf_node();
                return _instance;
            }
        }

        private void tfCallback(tfMessage msg)
        {
            lock (addlock)
                additions.Enqueue(msg);
        }

        public emTransform transformFrame(string source, string target, out gm.Vector3 vec, out gm.Quaternion quat)
        {
            emTransform trans = new emTransform();
            transformer.lookupTransform(source, target, new Time(new TimeData()), out trans);
            vec = trans != null ? trans.translation.ToMsg() : new emVector3().ToMsg();
            quat = trans != null ? trans.rotation.ToMsg() : new emQuaternion().ToMsg();
            return trans;
        }

        public List<emTransform> link(string source, string target)
        {
            lock (frameslock)
            {
                return link_unlocked(source, target);
            }
        }

        private List<emTransform> link_unlocked(string source, string target)
        {
            return null;
        }
    }

    public class Transformer
    {
        //autobots, roll out
        private object framemutex = new object();
        private Dictionary<string, emTransform> frames = new Dictionary<string, emTransform> {{"/", null}};

        public void lookupTransform(String t, String s, Time time, out emTransform transform)
        {
            lookupTransform(t.data, s.data, time, out transform);
        }

        public void lookupTransform(string target_frame, string source_frame, Time time, out emTransform transform)
        {
            lock (framemutex)
            {
                TransformAccum accum = new TransformAccum();
                if (!walkToTopParent(accum, time, target_frame, source_frame))
                {
                    transform = new emTransform();
                    return;
                }
                transform = new emTransform(accum.result_quat, accum.result_vec, accum.time, source_frame, target_frame);
            }
        }

        public bool walkToTopParent(TransformAccum f, Time time, string target_frame, string source_frame)
        {
            if (target_frame == source_frame)
            {
                f.finalize(TransformAccum.WalkEnding.Identity, time);
                return true;
            }
            string frame = source_frame;
            string top_parent = frame;
            if (!frames.ContainsKey(frame)) return false;
            while (frames[frame] != null)
            {
                emTransform cache = frames[frame];
                if (cache == null)
                    break;
                string parent = cache.parent_frame_id;
                if (parent == null)
                {
                    top_parent = frame;
                    break;
                }

                if (frame == target_frame)
                {
                    f.finalize(TransformAccum.WalkEnding.TargetParentOfSource, time);
                    return true;
                }
                f.accum(cache, true);
                top_parent = frame;
                frame = parent;
            }

            frame = target_frame;
            while (frame != top_parent)
            {
                emTransform cache = frames[frame];
                if (cache == null)
                {
                    break;
                }

                string parent = cache.parent_frame_id;
                if (parent == null)
                {
                    return false;
                }
                if (frame == source_frame)
                {
                    f.finalize(TransformAccum.WalkEnding.SourceParentOfTarget, time);
                    return true;
                }
                f.accum(cache, false);
                frame = parent;
            }

            if (frame != top_parent) return false;

            f.finalize(TransformAccum.WalkEnding.FullPath, time);

            return true;
        }

        public bool setTransform(emTransform transform)
        {
            if (transform.child_frame_id == transform.frame_id)
                return false;
            if (transform.child_frame_id == "/")
                return false;
            if (transform.frame_id == "/")
                return false;
            lock (frames)
            {
                if (!frames.ContainsKey(transform.frame_id)) frames.Add(transform.frame_id, transform);
                else frames[transform.frame_id] = transform;
            }
            return true;
        }
    }

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
    }

    public class TransformAccum
    {
        public enum WalkEnding
        {
            Identity,
            TargetParentOfSource,
            SourceParentOfTarget,
            FullPath
        }

        public emQuaternion result_quat;
        public emVector3 result_vec;
        public emQuaternion source_to_top_quat = new emQuaternion();
        public emVector3 source_to_top_vec = new emVector3();
        public emQuaternion target_to_top_quat = new emQuaternion();
        public emVector3 target_to_top_vec = new emVector3();
        public Time time;

        public void finalize(WalkEnding end, Time _time)
        {
            switch (end)
            {
                case WalkEnding.Identity:
                    break;
                case WalkEnding.TargetParentOfSource:
                    result_vec = source_to_top_vec;
                    result_quat = source_to_top_quat;
                    break;
                case WalkEnding.SourceParentOfTarget:
                    result_quat = target_to_top_quat.inverse();
                    result_vec = quatRotate(result_quat, new emVector3(-target_to_top_vec.x, -target_to_top_vec.y, -target_to_top_vec.z));
                    break;
                case WalkEnding.FullPath:
                    emQuaternion inv_target_quat = target_to_top_quat.inverse();
                    emVector3 inv_target_vec = quatRotate(inv_target_quat, new emVector3(-target_to_top_vec.x, -target_to_top_vec.y, -target_to_top_vec.z));
                    result_vec = quatRotate(inv_target_quat, source_to_top_vec) + inv_target_vec;
                    result_quat = inv_target_quat*source_to_top_quat;
                    break;
            }
            time = _time;
        }

        public void accum(emTransform st, bool source)
        {
            if (source)
            {
                source_to_top_vec = quatRotate(st.rotation, source_to_top_vec) + st.translation;
                source_to_top_quat = st.rotation*source_to_top_quat;
            }
            else
            {
                target_to_top_vec = quatRotate(st.rotation, target_to_top_vec) + st.translation;
                target_to_top_quat = st.rotation*target_to_top_quat;
            }
        }

        public emVector3 quatRotate(emQuaternion rotation, emVector3 v)
        {
            emQuaternion q = rotation*v;
            q = q*rotation.inverse();
            return new emVector3(q.x, q.y, q.z);
        }
    }

    public class emTransform
    {
        private static Dictionary<string, string> paternityTest;
        public string child_frame_id;
        public string frame_id;

        public emQuaternion rotation;
        public Time stamp;
        public emVector3 translation;

        public emTransform() : this(new emQuaternion(), new emVector3(), new Time(new TimeData()), "", "")
        {
        }

        public emTransform(gm.TransformStamped msg) : this(new emQuaternion(msg.transform.rotation), new emVector3(msg.transform.translation), msg.header.stamp, msg.header.frame_id.data, msg.child_frame_id.data)
        {
        }

        public emTransform(emQuaternion q, emVector3 v, Time t, string fid, string cfi)
        {
            if (paternityTest == null) paternityTest = new Dictionary<string, string>();
            rotation = q;
            translation = v;
            stamp = t;

            frame_id = fid;
            child_frame_id = cfi;
            if (cfi.Length > 0 && fid.Length > 0)
            {
                if (!paternityTest.ContainsKey(cfi))
                    paternityTest.Add(cfi, fid);
                else
                    paternityTest[cfi] = fid;
            }
        }

        public string parent_frame_id
        {
            get
            {
                if (paternityTest == null || !paternityTest.ContainsKey(frame_id))
                    return "/";
                return paternityTest[frame_id];
            }
        }
    }

    public class emQuaternion
    {
        public double w;
        public double x, y, z;

        public emQuaternion() : this(0, 0, 0, 1)
        {
        }

        public emQuaternion(double X, double Y, double Z, double W)
        {
            x = X;
            y = Y;
            z = Z;
            w = W;
        }

        public emQuaternion(emQuaternion shallow) : this(shallow.x, shallow.y, shallow.z, shallow.w)
        {
        }

        public emQuaternion(gm.Quaternion shallow) : this(shallow.x, shallow.y, shallow.z, shallow.w)
        {
        }

        public gm.Quaternion ToMsg()
        {
            return new gm.Quaternion {w = w, x = x, y = y, z = z};
        }

        public static emQuaternion operator +(emQuaternion v1, emQuaternion v2)
        {
            return new emQuaternion(v1.w + v2.w, v1.x + v2.x, v1.y + v2.y, v1.z + v2.z);
        }

        public static emQuaternion operator *(emQuaternion v1, float d)
        {
            return v1*((double) d);
        }

        public static emQuaternion operator *(emQuaternion v1, int d)
        {
            return v1*((double) d);
        }

        public static emQuaternion operator *(emQuaternion v1, double d)
        {
            return new emQuaternion(v1.x*d, v1.y*d, v1.z*d, v1.w*d);
        }

        public static emQuaternion operator *(emQuaternion v1, emQuaternion v2)
        {
            return new emQuaternion(v1.w*v2.x + v1.x*v2.x + v1.y*v2.z - v1.z*v2.y,
                v1.w*v2.y + v1.y*v2.w + v1.z*v2.x - v1.x*v2.z,
                v1.w*v2.z + v1.z*v2.w + v1.x*v2.y - v1.y*v2.x,
                v1.w*v2.w - v1.x*v2.x - v1.y*v2.y - v1.z*v2.z);
        }

        public static emQuaternion operator *(emQuaternion q, emVector3 w)
        {
            return new emQuaternion(w.x*q.w + w.y*q.z - w.z*q.y,
                w.y*q.w + w.z*q.x - w.x*q.z,
                w.z*q.w + w.x*q.y - w.y*q.x,
                -w.x*q.x - w.y*q.y - w.z*q.z);
        }

        public static emQuaternion operator *(emVector3 w, emQuaternion q)
        {
            return new emQuaternion(w.x*q.w + w.y*q.z - w.z*q.y,
                w.y*q.w + w.z*q.x - w.x*q.z,
                w.z*q.w + w.x*q.y - w.y*q.x,
                -w.x*q.x - w.y*q.y - w.z*q.z);
        }

        public static emQuaternion operator /(emQuaternion v1, float s)
        {
            return v1/((double) s);
        }

        public static emQuaternion operator /(emQuaternion v1, int s)
        {
            return v1/((double) s);
        }

        public static emQuaternion operator /(emQuaternion v1, double s)
        {
            return v1*(1.0/s);
        }

        public emQuaternion inverse()
        {
            return new emQuaternion(-x, -y, -z, w);
        }

        public double dot(emQuaternion q)
        {
            return x*q.x + y*q.y + z*q.z + w*q.w;
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
            return string.Format("({0},{1},{2},{3})", x, y, z, w);
        }

        public emVector3 getRPY()
        {
            emVector3 ret = new emVector3();
            double w2 = w * w;
            double x2 = x * x;
            double y2 = y * y;
            double z2 = z * z;
            double unitLength = length();    // Normalized == 1, otherwise correction divisor.
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
    }

    public class emVector3
    {
        public double x, y, z;

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

        public gm.Vector3 ToMsg()
        {
            return new gm.Vector3 {x = x, y = y, z = z};
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
            return v1*((double) d);
        }

        public static emVector3 operator *(emVector3 v1, int d)
        {
            return v1*((double) d);
        }

        public static emVector3 operator *(emVector3 v1, double d)
        {
            return new emVector3(v1.x*d, v1.y*d, v1.z*d);
        }

        public override string ToString()
        {
            return string.Format("({0},{1},{2})", x, y, z);
        }
    }
}