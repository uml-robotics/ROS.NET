// File: tf_node.cs
// Project: ROS_C-Sharp
// 
// ROS.NET
// Eric McCann <emccann@cs.uml.edu>
// UMass Lowell Robotics Laboratory
// 
// Reimplementation of the ROS (ros.org) ros_cpp client in C#.
// 
// Created: 11/06/2013
// Updated: 07/23/2014

#region USINGZ

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Messages;
using Messages.std_msgs;
using Messages.tf;
using gm = Messages.geometry_msgs;
using Int64 = System.Int64;
using String = Messages.std_msgs.String;

#endregion

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
        private static object singleton_mutex = new object();

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
                        Thread.Sleep(10);
                    }
                });
                updateThread.Start();
            }
            tfhandle.subscribe<tfMessage>("/tf", 100, tfCallback);
        }

        public static tf_node instance
        {
            [DebuggerStepThrough]
            get
            {
                if (_instance == null)
                {
                    lock (singleton_mutex)
                    {
                        if (_instance == null)
                            _instance = new tf_node();
                    }
                }
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
            try
            {
                transformer.lookupTransform(target, source, new Time(new TimeData()), out trans);
            }
            catch (Exception e)
            {
                ROS.Error(e.ToString());
                trans = null;
            }
            if (trans != null)
            {
                vec = trans.translation != null ? trans.translation.ToMsg() : new emVector3().ToMsg();
                quat = trans.rotation != null ? trans.rotation.ToMsg() : new emQuaternion().ToMsg();
            }
            else
            {
                vec = null;
                quat = null;
            }
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

    public enum TF_STATUS
    {
        NO_ERROR,
        LOOKUP_ERROR,
        CONNECTIVITY_ERROR,
        EXTRAPOLATION_ERROR
    }

    public class Transformer
    {
        private const string tf_prefix = "/";
        private const uint MAX_GRAPH_DEPTH = 100;
        private const double DEFAULT_CACHE_TIME = 1000000000;
        private const ulong DEFAULT_MAX_EXTRAPOLATION_DISTANCE = 0;
        private ulong cache_time;

        private Dictionary<string, uint> frameIDs = new Dictionary<string, uint> {{"NO_PARENT", 0}};
        private Dictionary<uint, string> frameids_reverse = new Dictionary<uint, string> {{0, "NO_PARENT"}};
        private object framemutex = new object();
        private SortedList<uint, TimeCache> frames = new SortedList<uint, TimeCache>();

        private bool interpolating;

        public Transformer(bool i = true, ulong ct = (ulong) DEFAULT_CACHE_TIME)
        {
            interpolating = i;
            cache_time = ct;
        }

        public static string resolve(string prefix, string frame_name)
        {
            if (frame_name.Length > 0)
            {
                if (frame_name[0] == '/')
                    return frame_name;
            }
            if (prefix.Length > 0)
            {
                if (prefix[0] == '/')
                    return prefix + "/" + frame_name;
                return "/" + prefix + "/" + frame_name;
            }
            return "/" + frame_name;
        }

        public void clear()
        {
            lock (framemutex)
            {
                foreach (TimeCache tc in frames.Values)
                    tc.clearList();
            }
        }

        public void lookupTransform(String t, String s, Time time, out emTransform transform)
        {
            try
            {
                lookupTransform(t.data, s.data, time, out transform);
            }
            catch (Exception e)
            {
                transform = null;
                ROS.Error(e);
                throw e;
            }
        }

        public uint getFrameID(string frame)
        {
            if (frameIDs.ContainsKey(frame))
            {
                return frameIDs[frame];
            }
            return 0;
        }

        public void lookupTransform(string target_frame, string source_frame, Time time, out emTransform transform)
        {
            string error_string = null;
            lookupTransform(target_frame, source_frame, time, out transform, ref error_string);
        }

        public void lookupTransform(string target_frame, string source_frame, Time time, out emTransform transform, ref string error_string)
        {
            transform = new emTransform();

            string mapped_tgt = resolve(tf_prefix, target_frame);
            string mapped_src = resolve(tf_prefix, source_frame);

            if (mapped_tgt == mapped_src)
            {
                transform.translation = new emVector3();
                transform.rotation = new emQuaternion();
                transform.child_frame_id = mapped_src;
                transform.frame_id = mapped_tgt;
                transform.stamp = ROS.GetTime(DateTime.Now);
                return;
            }

            lock (framemutex)
            {
                uint target_id = getFrameID(mapped_tgt);
                uint source_id = getFrameID(mapped_src);

                TransformAccum accum = new TransformAccum();

                TF_STATUS retval = walkToTopParent(accum, TimeCache.toLong(time.data), target_id, source_id, ref error_string);
                if (error_string != null && retval != TF_STATUS.NO_ERROR)
                {
                    switch (retval)
                    {
                        case TF_STATUS.CONNECTIVITY_ERROR:
                            ROS.Error("NO CONNECTIONSZSZ: " + error_string);
                            break;
                        case TF_STATUS.EXTRAPOLATION_ERROR:
                            ROS.Error("EXTRAPOLATION: " + error_string);
                            break;
                        case TF_STATUS.LOOKUP_ERROR:
                            ROS.Error("LOOKUP: " + error_string);
                            break;
                    }
                }
                transform.translation = accum.result_vec;
                transform.rotation = accum.result_quat;
                transform.child_frame_id = mapped_src;
                transform.frame_id = mapped_tgt;
                transform.stamp = new Time {data = new TimeData {sec = (uint) (accum.time >> 32), nsec = (uint) (accum.time & 0xFFFFFFFF)}};
            }
        }

        public TF_STATUS walkToTopParent<F>(F f, ulong time, uint target_id, uint source_id, ref string error_str) where F : ATransformAccum
        {
            if (target_id == source_id)
            {
                f.finalize(WalkEnding.Identity, time);
                return TF_STATUS.NO_ERROR;
            }
            if (time == 0)
            {
                TF_STATUS retval = getLatestCommonTime(target_id, source_id, ref time, ref error_str);
                if (retval != TF_STATUS.NO_ERROR)
                    return retval;
            }
            uint frame = source_id;
            uint top_parent = frame;
            uint depth = 0;
            while (frame != 0)
            {
                if (!frames.ContainsKey(frame))
                {
                    top_parent = frame;
                    break;
                }
                TimeCache cache = frames[frame];
                uint parent = f.gather(cache, time, ref error_str);
                if (parent == 0)
                {
                    top_parent = frame;
                    break;
                }

                if (frame == target_id)
                {
                    f.finalize(WalkEnding.TargetParentOfSource, time);
                    return TF_STATUS.NO_ERROR;
                }

                f.accum(true);

                top_parent = frame;
                frame = parent;
                ++depth;
                if (depth > MAX_GRAPH_DEPTH)
                {
                    if (error_str != null)
                    {
                        error_str = "The tf tree is invalid because it contains a loop.";
                    }
                    return TF_STATUS.LOOKUP_ERROR;
                }
            }

            frame = target_id;
            depth = 0;
            while (frame != top_parent)
            {
                if (!frames.ContainsKey(frame))
                    break;
                TimeCache cache = frames[frame];

                uint parent = f.gather(cache, time, ref error_str);

                if (parent == 0)
                {
                    if (error_str != null)
                    {
                        error_str += ", when looking up transform from frame [" + frameids_reverse[source_id] + "] to [" + frameids_reverse[target_id] + "]";
                    }
                    return TF_STATUS.EXTRAPOLATION_ERROR;
                }

                if (frame == source_id)
                {
                    f.finalize(WalkEnding.SourceParentOfTarget, time);
                    return TF_STATUS.NO_ERROR;
                }

                f.accum(false);

                frame = parent;
                ++depth;
                if (depth > MAX_GRAPH_DEPTH)
                {
                    if (error_str != null)
                    {
                        error_str = "The tf tree is invalid because it contains a loop.";
                    }
                    return TF_STATUS.LOOKUP_ERROR;
                }
            }


            if (frame != top_parent)
            {
                if (error_str != null)
                    error_str = "" + frameids_reverse[source_id] + " DOES NOT CONNECT TO " + frameids_reverse[target_id];
                return TF_STATUS.CONNECTIVITY_ERROR;
            }

            f.finalize(WalkEnding.FullPath, time);

            return TF_STATUS.NO_ERROR;
        }

        private TF_STATUS getLatestCommonTime(uint target_id, uint source_id, ref ulong time, ref string error_str)
        {
            if (target_id == source_id)
            {
                time = TimeCache.toLong(ROS.GetTime(DateTime.Now).data);
                return TF_STATUS.NO_ERROR;
            }

            List<TimeAndFrameID> lct = new List<TimeAndFrameID>();

            uint frame = source_id;
            TimeAndFrameID temp;
            uint depth = 0;
            ulong common_time = ulong.MaxValue;
            while (frame != 0)
            {
                TimeCache cache;
                if (!frames.ContainsKey(frame)) break;
                cache = frames[frame];
                TimeAndFrameID latest = cache.getLatestTimeAndParent();
                if (latest.frame_id == 0)
                    break;
                if (latest.time != 0)
                    common_time = Math.Min(latest.time, common_time);
                lct.Add(latest);
                frame = latest.frame_id;
                if (frame == target_id)
                {
                    time = common_time;
                    if (time == ulong.MaxValue)
                        time = 0;
                    return TF_STATUS.NO_ERROR;
                }
                ++depth;
                if (depth > MAX_GRAPH_DEPTH)
                {
                    if (error_str != null)
                    {
                        error_str = "The tf tree is invalid because it contains a loop.";
                    }
                    return TF_STATUS.LOOKUP_ERROR;
                }
            }

            frame = target_id;
            depth = 0;
            common_time = ulong.MaxValue;
            uint common_parent = 0;
            while (true)
            {
                TimeCache cache;
                if (!frames.ContainsKey(frame))
                    break;
                cache = frames[frame];
                TimeAndFrameID latest = cache.getLatestTimeAndParent();
                if (latest.frame_id == 0)
                    break;
                if (latest.time != 0)
                    common_time = Math.Min(latest.time, common_time);

                foreach (TimeAndFrameID tf in lct)
                    if (tf.frame_id == latest.frame_id)
                    {
                        common_parent = tf.frame_id;
                        break;
                    }
                frame = latest.frame_id;

                if (frame == source_id)
                {
                    time = common_time;
                    if (time == uint.MaxValue)
                    {
                        time = 0;
                    }
                    return TF_STATUS.NO_ERROR;
                }
                ++depth;
                if (depth > MAX_GRAPH_DEPTH)
                {
                    if (error_str != null)
                    {
                        error_str = "The tf tree is invalid because it contains a loop.";
                    }
                    return TF_STATUS.LOOKUP_ERROR;
                }
            }
            if (common_parent == 0)
            {
                error_str = "" + frameids_reverse[source_id] + " DOES NOT CONNECT TO " + frameids_reverse[target_id];
                return TF_STATUS.CONNECTIVITY_ERROR;
            }
            for (int i = 0; i < lct.Count; i++)
            {
                if (lct[i].time != 0)
                    common_time = Math.Min(common_time, lct[i].time);
                if (lct[i].frame_id == common_parent)
                    break;
            }
            if (common_time == uint.MaxValue)
                common_time = 0;
            time = common_time;
            return TF_STATUS.NO_ERROR;
        }

        public uint lookupOrInsertFrameNumber(string frame)
        {
            if (!frameIDs.ContainsKey(frame))
            {
                frameIDs.Add(frame, (uint) (frameIDs.Count + 1));
                frameids_reverse.Add((uint) frameids_reverse.Count + 1, frame);
            }
            return frameIDs[frame];
        }

        public bool setTransform(emTransform transform)
        {
            emTransform mapped_transform = new emTransform(transform.rotation, transform.translation, transform.stamp, transform.frame_id, transform.child_frame_id);
            mapped_transform.child_frame_id = resolve(tf_prefix, transform.child_frame_id);
            mapped_transform.frame_id = resolve(tf_prefix, transform.frame_id);

            if (mapped_transform.child_frame_id == mapped_transform.frame_id)
                return false;
            if (mapped_transform.child_frame_id == "/")
                return false;
            if (mapped_transform.frame_id == "/")
                return false;
            lock (framemutex)
            {
                uint frame_number = lookupOrInsertFrameNumber(mapped_transform.child_frame_id);
                TimeCache frame = null;
                if (!frames.ContainsKey(frame_number))
                {
                    frames[frame_number] = new TimeCache(cache_time);
                    frame = frames[frame_number];
                }
                else
                    frame = frames[frame_number];
                if (frame.insertData(new TransformStorage(mapped_transform, lookupOrInsertFrameNumber(mapped_transform.frame_id), frame_number)))
                {
                    // authority? meh
                }
                else
                    return false;
            }
            return true;
        }
    }

    public struct TimeAndFrameID
    {
        public uint frame_id;
        public ulong time;

        public TimeAndFrameID(ulong t, uint f)
        {
            time = t;
            frame_id = f;
        }
    }

    public class TimeCache
    {
        private const int MIN_INTERPOLATION_DISTANCE = 5;
        private const uint MAX_LENGTH_LINKED_LIST = 10000000;
        private const Int64 DEFAULT_MAX_STORAGE_TIME = 1000000000;

        private ulong max_storage_time;
        private volatile DLL<TransformStorage> storage = new DLL<TransformStorage>();

        public TimeCache()
            : this(DEFAULT_MAX_STORAGE_TIME)
        {
        }

        public TimeCache(ulong max_storage_time)
        {
            this.max_storage_time = max_storage_time;
        }

        public static ulong toLong(TimeData td)
        {
            return (((ulong) td.sec) << 32) | td.nsec;
        }

        private byte findClosest(ref TransformStorage one, ref TransformStorage two, ulong target_time, ref string error_str)
        {
            if (storage.Count == 0)
            {
                createEmptyException(ref error_str);
                return 0;
            }

            if (target_time == 0)
            {
                one = storage.Back;
                return 1;
            }

            if (storage.Count == 1)
            {
                TransformStorage ts = storage.Front;
                if (ts.stamp == target_time)
                {
                    one = ts;
                    return 1;
                }
                createExtrapolationException1(target_time, ts.stamp, ref error_str);
                return 0;
            }

            ulong latest_time = storage.Back.stamp;
            ulong earliest_time = storage.Front.stamp;
            if (target_time == latest_time)
            {
                one = storage.Back;
                return 1;
            }
            if (target_time == earliest_time)
            {
                one = storage.Front;
                return 1;
            }
            if (target_time > latest_time)
            {
                createExtrapolationException2(target_time, latest_time, ref error_str);
                return 0;
            }
            if (target_time < earliest_time)
            {
                createExtrapolationException3(target_time, earliest_time, ref error_str);
                return 0;
            }

            ulong i;
            for (i = 0; i < storage.Count; i++)
            {
                if (storage[i].stamp <= target_time) break;
            }
            one = storage[i + 1];
            two = storage[i];
            return 2;
        }

        private void interpolate(TransformStorage one, TransformStorage two, ulong time, ref TransformStorage output)
        {
            if (one.stamp == two.stamp)
            {
                output = two;
                return;
            }

            if (output == null)
                output = new TransformStorage();

            double ratio = (time - one.stamp)/(two.stamp - one.stamp);
            output.translation.setInterpolate3(one.translation, two.translation, ratio);
            output.rotation = slerp(one.rotation, two.rotation, ratio);
            output.stamp = one.stamp;
            output.frame_id = one.frame_id;
            output.child_frame_id = one.child_frame_id;
        }

        private emQuaternion slerp(emQuaternion q1, emQuaternion q2, double rt)
        {
            return q1.slerp(q2, rt);
        }

        private void pruneList()
        {
            ulong latest_time = storage.Back.stamp;
            ulong preprune = storage.Count, postprune = 0;
            while ((postprune = storage.Count) > 0 && storage.Front.stamp + max_storage_time < latest_time)
                storage.popFront();
            //Console.WriteLine("Pruned " + (preprune - postprune) + " transforms. " + postprune + " remain");
        }

        public bool getData(TimeData time_, ref TransformStorage data_out, ref string error_str)
        {
            return getData(toLong(time_), ref data_out, ref error_str);
        }

        public bool getData(ulong time_, ref TransformStorage data_out, ref string error_str)
        {
            TransformStorage temp1 = null, temp2 = null;
            int num_nodes;
            num_nodes = findClosest(ref temp1, ref temp2, time_, ref error_str);
            switch (num_nodes)
            {
                case 0:
                    return false;
                case 1:
                    data_out = temp1;
                    break;
                case 2:
                    if (temp1.frame_id == temp2.frame_id)
                    {
                        interpolate(temp1, temp2, time_, ref data_out);
                    }
                    else
                    {
                        data_out = temp1;
                    }
                    break;
                default:
                    ROS.FREAKOUT();
                    break;
            }
            return true;
        }

        public bool insertData(TransformStorage new_data)
        {
            if (storage.Count > 0 && storage.Front.stamp > new_data.stamp + max_storage_time)
                return false;

            storage.insert(new_data, (a, b) => a.stamp > new_data.stamp);
            pruneList();
            return true;
        }

        public void clearList()
        {
            storage.Clear();
        }

        public uint getParent(ulong time, ref string error_str)
        {
            TransformStorage temp1 = null, temp2 = null;

            int num_nodes;
            num_nodes = findClosest(ref temp1, ref temp2, time, ref error_str);
            if (num_nodes == 0) return 0;
            return temp1.frame_id;
        }

        public uint getParent(TimeData time_, ref string error_str)
        {
            return getParent(toLong(time_), ref error_str);
        }

        public TimeAndFrameID getLatestTimeAndParent()
        {
            if (storage.Count == 0)
            {
                return new TimeAndFrameID(0, 0);
            }
            TransformStorage ts = storage.Back;
            return new TimeAndFrameID(ts.stamp, ts.frame_id);
        }

        public uint getListLength()
        {
            return (uint) storage.Count;
        }

        public ulong getLatestTimeStamp()
        {
            if (storage.Count == 0) return 0;
            return storage.Back.stamp;
        }

        public ulong getOldestTimestamp()
        {
            if (storage.Count == 0) return 0;
            return storage.Front.stamp;
        }

        #region ERROR THROWERS

        private void createEmptyException(ref string error_str)
        {
            if (error_str != null) error_str = "Cache is empty!";
        }

        private void createExtrapolationException1(ulong t0, ulong t1, ref string error_str)
        {
            if (error_str != null) error_str = "Lookup would require extrapolation at time \n" + t0 + ", but only time \n" + t1 + " is in the buffer";
        }

        private void createExtrapolationException2(ulong t0, ulong t1, ref string error_str)
        {
            if (error_str != null) error_str = "Lookup would require extrapolation into the future. Requested time \n" + t0 + " but the latest data is at the time \n" + t1;
        }

        private void createExtrapolationException3(ulong t0, ulong t1, ref string error_str)
        {
            if (error_str != null) error_str = "Lookup would require extrapolation into the past. Requested time \n" + t0 + " but the earliest data is at the time \n" + t1;
        }

        #endregion
    }

    public class TransformStorage
    {
        public uint child_frame_id;
        public uint frame_id;
        public emQuaternion rotation;
        public ulong stamp;
        public emVector3 translation;

        public TransformStorage()
        {
            rotation = new emQuaternion();

            translation = new emVector3();
        }

        public TransformStorage(emTransform data, uint frame_id, uint child_frame_id)
        {
            rotation = data.rotation;
            translation = data.translation;
            stamp = TimeCache.toLong(data.stamp.data);
            this.frame_id = frame_id;
            this.child_frame_id = child_frame_id;
        }
    }

    [DebuggerStepThrough]
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

    public enum WalkEnding
    {
        Identity,
        TargetParentOfSource,
        SourceParentOfTarget,
        FullPath
    }

    public abstract class ATransformAccum
    {
        public TransformStorage st;
        public abstract uint gather(TimeCache cache, ulong time, ref string error_str);
        public abstract void accum(bool source);
        public abstract void finalize(WalkEnding end, ulong time);
    }

    public class CanTransformAccum : ATransformAccum
    {
        public override uint gather(TimeCache cache, ulong time, ref string error_str)
        {
            return cache.getParent(time, ref error_str);
        }

        public override void accum(bool source)
        {
        }

        public override void finalize(WalkEnding end, ulong time)
        {
        }
    }

    public class TransformAccum : ATransformAccum
    {
        public emQuaternion result_quat;
        public emVector3 result_vec;
        public emQuaternion source_to_top_quat = new emQuaternion();
        public emVector3 source_to_top_vec = new emVector3();
        public emQuaternion target_to_top_quat = new emQuaternion();
        public emVector3 target_to_top_vec = new emVector3();
        public ulong time;

        public override uint gather(TimeCache cache, ulong time_, ref string error_str)
        {
            if (!cache.getData(time_, ref st, ref error_str))
                return 0;
            return st.frame_id;
        }

        public override void finalize(WalkEnding end, ulong _time)
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
                {
                    emQuaternion inv_target_quat = target_to_top_quat.inverse();
                    emVector3 inv_target_vec = quatRotate(inv_target_quat, -1*target_to_top_vec);
                    result_quat = inv_target_quat;
                    result_vec = inv_target_vec;
                }
                    break;
                case WalkEnding.FullPath:
                {
                    emQuaternion inv_target_quat = target_to_top_quat.inverse();
                    emVector3 inv_target_vec = quatRotate(inv_target_quat, new emVector3(-target_to_top_vec.x, -target_to_top_vec.y, -target_to_top_vec.z));
                    result_vec = quatRotate(inv_target_quat, source_to_top_vec) + inv_target_vec;
                    result_quat = inv_target_quat*source_to_top_quat;
                }
                    break;
            }
            time = _time;
        }

        public override void accum(bool source)
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

        [DebuggerStepThrough]
        public emVector3 quatRotate(emQuaternion rotation, emVector3 v)
        {
            emQuaternion q = rotation*v;
            q = q*rotation.inverse();
            return new emVector3(q.x, q.y, q.z);
        }
    }

    [DebuggerStepThrough]
    public class emTransform
    {
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
            rotation = q;
            translation = v;
            stamp = t;

            frame_id = fid;
            child_frame_id = cfi;
        }
    }

    [DebuggerStepThrough]
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

        public static emQuaternion operator *(float d, emQuaternion v1)
        {
            return v1*((double) d);
        }

        public static emQuaternion operator *(int d, emQuaternion v1)
        {
            return v1*((double) d);
        }

        public static emQuaternion operator *(double d, emQuaternion v1)
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
            emVector3 tmp = new emMatrix3x3(this).getYPR();
            return new emVector3(tmp.z, tmp.y, tmp.x);
            emVector3 ret = new emVector3();
            double w2 = w*w;
            double x2 = x*x;
            double y2 = y*y;
            double z2 = z*z;
            double unitLength = length(); // Normalized == 1, otherwise correction divisor.
            double abcd = w*x + y*z;
            double eps = Math.E;
            double pi = Math.PI;
            if (abcd > (0.5 - eps)*unitLength)
            {
                ret.z = 2*Math.Atan2(y, w);
                ret.y = pi;
                ret.x = 0;
            }
            else if (abcd < (-0.5 + eps)*unitLength)
            {
                ret.z = -2*Math.Atan2(y, w);
                ret.y = -pi;
                ret.x = 0;
            }
            else
            {
                double adbc = w*z - x*y;
                double acbd = w*y - x*z;
                ret.z = Math.Atan2(2*adbc, 1 - 2*(z2 + x2));
                ret.y = Math.Asin(2*abcd/unitLength);
                ret.x = Math.Atan2(2*acbd, 1 - 2*(y2 + x2));
            }
            return ret;
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
            double s = Math.Sqrt(length2()*q.length2());
            if (dot(q) < 0)
                return Math.Acos(dot(-1*q)/s)*2.0;
            return Math.Acos(dot(q)/s)*2.0;
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
                    return new emQuaternion((x*s0 + -1*q.x*s1)*d,
                        (y*s0 + -1*q.y*s1)*d,
                        (z*s0 + -1*q.z*s1)*d,
                        (w*s0 + -1*q.w*s1)*d);
                }
                return new emQuaternion((x*s0 + q.x*s1)*d,
                    (y*s0 + q.y*s1)*d,
                    (z*s0 + q.z*s1)*d,
                    (w*s0 + q.w*s1)*d);
            }
            return new emQuaternion(this);
        }
    }

    [DebuggerStepThrough]
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

        public static emVector3 operator *(float d, emVector3 v1)
        {
            return v1*((double) d);
        }

        public static emVector3 operator *(int d, emVector3 v1)
        {
            return v1*((double) d);
        }

        public static emVector3 operator *(double d, emVector3 v1)
        {
            return new emVector3(v1.x*d, v1.y*d, v1.z*d);
        }

        public override string ToString()
        {
            return string.Format("({0},{1},{2})", x, y, z);
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