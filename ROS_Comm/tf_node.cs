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
// Updated: 10/07/2015

#region USINGZ

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using MathNet.Numerics.LinearAlgebra.Complex;
using MathNet.Spatial;
using Messages;
using Messages.std_msgs;
using Messages.tf;
using gm = Messages.geometry_msgs;
using Int64 = System.Int64;
using String = Messages.std_msgs.String;
using Vector3 = MathNet.Spatial.Vector3D;

#endregion

namespace Ros_CSharp
{
    public class Stamped<T>
    {
        public T data;
        public string frame_id;
        public Time stamp;

        public Stamped()
        {
        }

        public Stamped(Time t, string f, T d)
        {
            stamp = t;
            frame_id = f;
            data = d;
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
        private NodeHandle nh;

        private ConcurrentDictionary<string, uint> frameIDs = new ConcurrentDictionary<string, uint>();
        private ConcurrentDictionary<uint, string> frameids_reverse = new ConcurrentDictionary<uint, string>();
        private ConcurrentDictionary<uint, TimeCache> frames = new ConcurrentDictionary<uint, TimeCache>();

        private bool interpolating;

        public Transformer(bool interpolating = true, ulong ct = (ulong) DEFAULT_CACHE_TIME)
        {
            frameIDs["NO_PARENT"] = 0;
            frameids_reverse[0] = "NO_PARENT";
            nh = new NodeHandle();
            nh.subscribe<tfMessage>("/tf", 0, Update);
            this.interpolating = interpolating;
            cache_time = ct;
        }

        private void Update(tfMessage msg)
        {
            foreach (gm.TransformStamped tf in msg.transforms)
                if (!setTransform(new emTransform(tf)))
                    ROS.Warn("Failed to setTransform in transformer update function");
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
                if (prefix[0] == '/' && prefix.Length == 1)
                    return "/" + frame_name;
                if (prefix[0] == '/')
                    return prefix + "/" + frame_name;
                return "/" + prefix + "/" + frame_name;
            }
            return "/" + frame_name;
        }

        public void clear()
        {
            foreach (TimeCache tc in frames.Values)
                tc.clearList();
            frameIDs.Clear();
            frameids_reverse.Clear();
        }

        public void lookupTransform(String target_frame, String source_frame, Time time, out emTransform transform)
        {
            try
            {
                lookupTransform(target_frame.data, source_frame.data, time, out transform);
            }
            catch (Exception e)
            {
                transform = null;
                ROS.Error(e);
                throw e;
            }
        }

        private uint getFrameIDInternal(string frame)
        {
            uint value;
            if (frameIDs.TryGetValue(frame, out value))
            {
                return value;
            }
            return 0;
        }

        public uint getFrameID(string frame)
        {
            return getFrameIDInternal(frame);
        }

        public bool frameExists(string frame)
        {
            return getFrameID(frame) != 0;
        }

        private bool frameExistsInternal(string frame)
        {
            return getFrameIDInternal(frame) != 0;
        }

        public bool lookupTransform(string target_frame, string source_frame, Time time, out emTransform transform)
        {
            string error_string = null;
            bool result = lookupTransform(target_frame, source_frame, time, out transform, ref error_string);
            /*if (!result && error_string != null)
                ROS.Error(error_string);*/
            return result;
        }

        public bool lookupTransform(string target_frame, string source_frame, Time time, out emTransform transform, ref string error_string)
        {
            transform = null;

            string mapped_tgt = resolve(tf_prefix, target_frame);
            string mapped_src = resolve(tf_prefix, source_frame);

            if (mapped_tgt == mapped_src)
            {
                transform = new emTransform();
                transform.origin = new emVector3();
                transform.basis = new emQuaternion();
                transform.child_frame_id = mapped_src;
                transform.frame_id = mapped_tgt;
                transform.stamp = ROS.GetTime(DateTime.Now);
                return true;
            }

            TF_STATUS retval;
            uint target_id = getFrameIDInternal(mapped_tgt);
            uint source_id = getFrameIDInternal(mapped_src);

            TransformAccum accum = new TransformAccum();

            retval = walkToTopParent(accum, TimeCache.toLong(time.data), target_id, source_id, ref error_string);
            if (retval != TF_STATUS.NO_ERROR)
            {
                error_string = error_string ?? "UNSPECIFIED";
                switch (retval)
                {
                    case TF_STATUS.CONNECTIVITY_ERROR:
                        error_string = "NO CONNECTIONSZSZ: " + error_string;
                        break;
                    case TF_STATUS.EXTRAPOLATION_ERROR:
                        error_string = "EXTRAPOLATION: " + error_string;
                        break;
                    case TF_STATUS.LOOKUP_ERROR:
                        error_string = "LOOKUP: " + error_string;
                        break;
                    default:
                        if (accum.result_quat == null || accum.result_vec == null)
                        {
                            error_string = "ACCUM WALK FAIL!";
                        }
                        break;
                }
            }
            if (accum.result_vec != null && accum.result_quat != null)
            {
                transform = new emTransform();
                transform.origin = accum.result_vec;
                transform.basis = accum.result_quat;
                transform.child_frame_id = mapped_src;
                transform.frame_id = mapped_tgt;
                transform.stamp = new Time(ROS.ticksToData((long) accum.time));
            }
            return retval == TF_STATUS.NO_ERROR;
        }

        public void transformQuaternion(string target_frame, Stamped<emQuaternion> stamped_in, ref Stamped<emQuaternion> stamped_out)
        {
            emTransform trans = new emTransform();
            lookupTransform(target_frame, stamped_in.frame_id, stamped_in.stamp, out trans);
            stamped_out.data = trans*stamped_in.data;
            stamped_out.stamp = trans.stamp;
            stamped_out.frame_id = target_frame;
        }

        public void transformQuaternion(string target_frame, Stamped<gm.Quaternion> stamped_in, ref Stamped<gm.Quaternion> stamped_out)
        {
            Stamped<emQuaternion> quatin = new Stamped<emQuaternion>(stamped_in.stamp, stamped_in.frame_id, new emQuaternion(stamped_in.data));
            Stamped<emQuaternion> quatout = new Stamped<emQuaternion>(stamped_out.stamp, stamped_out.frame_id, new emQuaternion(stamped_out.data));
            transformQuaternion(target_frame, quatin, ref quatout);
            stamped_out.stamp = quatout.stamp;
            stamped_out.data = quatout.data.ToMsg();
            stamped_out.frame_id = quatout.frame_id;
        }

        public void transformVector(string target_frame, Stamped<emVector3> stamped_in, ref Stamped<emVector3> stamped_out)
        {
            emTransform trans = new emTransform();
            lookupTransform(target_frame, stamped_in.frame_id, stamped_in.stamp, out trans);
            emVector3 end = stamped_in.data;
            emVector3 origin = new emVector3(0, 0, 0);
            emVector3 output = (trans*end) - (trans*origin);
            stamped_out.data = output;
            stamped_out.stamp = trans.stamp;
            stamped_out.frame_id = target_frame;
        }

        public void transformVector(string target_frame, Stamped<gm.Vector3> stamped_in, ref Stamped<gm.Vector3> stamped_out)
        {
            Stamped<emVector3> vecin = new Stamped<emVector3>(stamped_in.stamp, stamped_in.frame_id, new emVector3(stamped_in.data));
            Stamped<emVector3> vecout = new Stamped<emVector3>(stamped_out.stamp, stamped_out.frame_id, new emVector3(stamped_out.data));
            transformVector(target_frame, vecin, ref vecout);
            stamped_out.stamp = vecout.stamp;
            stamped_out.data = vecout.data.ToMsg();
            stamped_out.frame_id = vecout.frame_id;
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
                frameIDs[frame] = (uint) (frameIDs.Count + 1);
                frameids_reverse[(uint) frameids_reverse.Count + 1] = frame;
            }
            return frameIDs[frame];
        }

        public bool setTransform(emTransform transform)
        {
            emTransform mapped_transform = new emTransform(transform.basis, transform.origin, transform.stamp, transform.frame_id, transform.child_frame_id);
            mapped_transform.child_frame_id = resolve(tf_prefix, transform.child_frame_id);
            mapped_transform.frame_id = resolve(tf_prefix, transform.frame_id);

            if (mapped_transform.child_frame_id == mapped_transform.frame_id)
                return false;
            if (mapped_transform.child_frame_id == "/")
                return false;
            if (mapped_transform.frame_id == "/")
                return false;
            uint frame_number = lookupOrInsertFrameNumber(mapped_transform.frame_id);
            uint child_frame_number = lookupOrInsertFrameNumber(mapped_transform.child_frame_id);
            TimeCache parent_frame=null,frame = null;
            if (!frames.ContainsKey(frame_number))
            {
                parent_frame = frames[frame_number] = new TimeCache(cache_time);
                parent_frame.insertData(new TransformStorage(mapped_transform, 0, frame_number));
            }
            if (!frames.ContainsKey(child_frame_number))
            {
                frame = frames[child_frame_number] = new TimeCache(cache_time);
            }
            else
            {
                //if we're revising a frame, that was previously labelled as having no parent, clear that knowledge from the time cache
                frame = frames[child_frame_number];
            }
            uint before = frame.getListLength();
            if (frame.insertData(new TransformStorage(mapped_transform, frame_number, child_frame_number)))
            {
                parent_frame = frames[frame_number] = new TimeCache(cache_time);
                parent_frame.insertData(new TransformStorage(mapped_transform, 0, frame_number));
            }
            else
                return false;
            return true;
        }

        public bool waitForTransform(string target_frame, string source_frame, Time time, Duration timeout, ref string error_msg)
        {
            return waitForTransform(target_frame, source_frame, time, timeout, null, ref error_msg);
        }

        public bool waitForTransform(string target_frame, Time target_time, string source_frame, Time source_time, Duration timeout, ref string error_msg)
        {
            return waitForTransform(target_frame, target_time, source_frame, source_time, timeout, null, ref error_msg);
        }

        public bool waitForTransform(string target_frame, Time target_time, string source_frame, Time source_time, Duration timeout, Duration pollingSleepDuration, ref string error_msg)
        {
            return waitForTransform(target_frame, source_frame, target_time, timeout, pollingSleepDuration, ref error_msg) &&
                   waitForTransform(target_frame, source_frame, source_time, timeout, pollingSleepDuration, ref error_msg);
        }

        public bool waitForTransform(string target_frame, string source_frame, Time time, Duration timeout, Duration pollingSleepDuration, ref string error_msg)
        {
            if (pollingSleepDuration == null)
                pollingSleepDuration = ROS.GetTime<Duration>(new TimeSpan(0, 0, 0, 0, 100));
            return waitForTransform(target_frame, source_frame, time, ROS.GetTime(timeout), ROS.GetTime(pollingSleepDuration), ref error_msg);
        }

        private bool waitForTransform(string target_frame, string source_frame, Time time, TimeSpan timeout, TimeSpan pollingSleepDuration, ref string error_msg)
        {
            DateTime start_time = DateTime.Now;
            string mapped_target = resolve(tf_prefix, target_frame);
            string mapped_source = resolve(tf_prefix, source_frame);

            do
            {
                if (canTransform(mapped_target, mapped_source, time, ref error_msg))
                    return true;
                if (!ROS.ok || !(DateTime.Now.Subtract(start_time).TotalMilliseconds < timeout.TotalMilliseconds))
                    break;
                Thread.Sleep(pollingSleepDuration);
            } while (ROS.ok && (DateTime.Now.Subtract(start_time).TotalMilliseconds < timeout.TotalMilliseconds));
            return false;
        }

        public bool waitForTransform(string target_frame, string source_frame, Time time, Duration timeout, Duration pollingSleepDuration = null)
        {
            string error_msg = null;
            return waitForTransform(target_frame, source_frame, time, timeout, pollingSleepDuration, ref error_msg);
        }

        public bool waitForTransform(string target_frame, Time target_time, string source_frame, Time source_time, Duration timeout, Duration pollingSleepDuration=null)
        {
            string error_msg = null;
            return waitForTransform(target_frame, target_time, source_frame, source_time, timeout, pollingSleepDuration, ref error_msg);
        }

        private bool waitForTransform(string target_frame, string source_frame, Time time, TimeSpan timeout, TimeSpan pollingSleepDuration)
        {
            string error_msg = null;
            return waitForTransform(target_frame, source_frame, time, timeout, pollingSleepDuration, ref error_msg);
        }

        private bool canTransform(string target_frame, Time target_time, string source_frame, Time source_time, ref string error_msg)
        {
            return canTransform(target_frame, source_frame, target_time, ref error_msg) && canTransform(target_frame, source_frame, source_time, ref error_msg);
        }

        private bool canTransform(string target_frame, string source_frame, Time time, ref string error_msg)
        {
            string mapped_target = resolve(tf_prefix, target_frame);
            string mapped_source = resolve(tf_prefix, source_frame);
            if (mapped_target == mapped_source) return true;
            if (!frameExistsInternal(mapped_target) || !frameExistsInternal(mapped_source)) return false;
            uint target_id = getFrameIDInternal(mapped_target);
            uint source_id = getFrameIDInternal(mapped_source);
            return canTransformNoLock(target_id, source_id, time, ref error_msg);
        }

        private bool canTransformNoLock(uint target_id, uint source_id, Time time, ref string error_msg)
        {
            if (target_id == 0 || source_id == 0) return false;
            CanTransformAccum accum = new CanTransformAccum();
            if (walkToTopParent(accum, TimeCache.toLong(time.data), target_id, source_id, ref error_msg) == TF_STATUS.NO_ERROR)
            {
                return true;
            }
            return false;
        }

        private bool canTransformInternal(uint target_id, uint source_id, Time time, ref string error_msg)
        {
            return canTransformNoLock(target_id, source_id, time, ref error_msg);
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
            return (ulong)ROS.ticksFromData(td);
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
            while (storage.Count > 0 && storage.Front.stamp + max_storage_time < latest_time)
                storage.popFront();
        }

        public bool getData(TimeData time_, ref TransformStorage data_out, ref string error_str)
        {
            return getData(toLong(time_), ref data_out, ref error_str);
        }

        public bool getData(ulong time_, ref TransformStorage data_out, ref string error_str)
        {
            TransformStorage temp1 = null, temp2 = null;
            int num_nodes = findClosest(ref temp1, ref temp2, time_, ref error_str);
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
                if (SimTime.instance.IsTimeSimulated)
                {
                    storage.Clear();
                }
                else
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

            int num_nodes = findClosest(ref temp1, ref temp2, time, ref error_str);
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

#if !TRACE
    [DebuggerStepThrough]
#endif
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
            rotation = data.basis;
            translation = data.origin;
            stamp = TimeCache.toLong(data.stamp.data);
            this.frame_id = frame_id;
            this.child_frame_id = child_frame_id;
        }
    }

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
            public double yaw;
            public double pitch;
            public double roll;
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
                    emVector3 inv_target_vec = quatRotate(inv_target_quat, -1*target_to_top_vec);
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

        public emVector3 quatRotate(emQuaternion rotation, emVector3 v)
        {
            emQuaternion q = rotation*v;
            q*=rotation.inverse();
            return new emVector3(q.x, q.y, q.z);
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

        public emTransform() : this(new emQuaternion(), new emVector3(), new Time(new TimeData()), "", "")
        {
        }

        public emTransform(gm.TransformStamped msg) : this(new emQuaternion(msg.transform.rotation), new emVector3(msg.transform.translation), msg.header.stamp, msg.header.frame_id.data, msg.child_frame_id.data)
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
            return new emVector3(mat.m_el[0].vec.DotProduct(v.vec) + t.origin.x,
                mat.m_el[1].vec.DotProduct(v.vec) + t.origin.y,
                mat.m_el[2].vec.DotProduct(v.vec) + t.origin.z);
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
        internal Quaternion quat;

        public double w
        {
            set { quat = new Quaternion(value, x, y, z); }
            get { return quat.Real; }
        }

        public double x
        {
            set { quat = new Quaternion(w, value, y, z); }
            get { return quat.ImagX; }
        }

        public double y
        {
            set { quat = new Quaternion(w, x, value, z); }
            get { return quat.ImagY; }
        }

        public double z
        {
            set { quat = new Quaternion(w, x, y, value); }
            get { return quat.ImagZ; }
        }

        public emQuaternion() : this(new Quaternion(1, 0, 0, 0))
        {
        }

        public emQuaternion(Quaternion q)
        {
            quat = q;
        }

        public emQuaternion(double X, double Y, double Z, double W)
        {
            quat = new Quaternion(W, X, Y, Z);
        }

        public emQuaternion(emQuaternion shallow)
            : this(shallow.x, shallow.y, shallow.z, shallow.w)
        {
        }

        public emQuaternion(gm.Quaternion shallow)
            : this(shallow.x, shallow.y, shallow.z, shallow.w)
        {
        }

        public gm.Quaternion ToMsg()
        {
            return new gm.Quaternion {w = w, x = x, y = y, z = z};
        }

        public static emQuaternion operator +(emQuaternion v1, emQuaternion v2)
        {
            return new emQuaternion(v1.quat + v2.quat);
        }

        public static emQuaternion operator *(emQuaternion v1, float d)
        {
            return new emQuaternion(v1.quat*d);
        }

        public static emQuaternion operator *(emQuaternion v1, int d)
        {
            return new emQuaternion(v1.quat*d);
        }

        public static emQuaternion operator *(emQuaternion v1, double d)
        {
            return new emQuaternion(v1.quat*d);
        }

        public static emQuaternion operator *(float d, emQuaternion v1)
        {
            return new emQuaternion(d*v1.quat);
        }

        public static emQuaternion operator *(int d, emQuaternion v1)
        {
            return new emQuaternion(d*v1.quat);
        }

        public static emQuaternion operator *(double d, emQuaternion v1)
        {
            return new emQuaternion(d*v1.quat);
        }

        public static emQuaternion operator *(emQuaternion v1, emQuaternion v2)
        {
            return new emQuaternion(v1.quat*v2.quat);
        }

        public static emQuaternion operator *(emQuaternion q, emVector3 w)
        {
            return new emQuaternion( q.w * w.x + q.y * w.z - q.z * w.y,
                 q.w * w.y + q.z * w.x - q.x * w.z,
                 q.w * w.z + q.x * w.y - q.y * w.x,
                 -q.x * w.x - q.y * w.y - q.z * w.z);
        }

        public static emQuaternion operator /(emQuaternion v1, float s)
        {
            return new emQuaternion(v1.quat/s);
        }

        public static emQuaternion operator /(emQuaternion v1, int s)
        {
            return new emQuaternion(v1.quat/s);
        }

        public static emQuaternion operator /(emQuaternion v1, double s)
        {
            return new emQuaternion(v1.quat/s);
        }

        public emQuaternion inverse()
        {
            return new emQuaternion(quat.Inverse());
        }

        public double dot(emQuaternion q)
        {
            return x*q.x + y*q.y + z*q.z + w*q.w;
        }

        public double length2()
        {
            return quat.Abs*quat.Abs;
        }

        public double length()
        {
            return quat.Abs;
        }

        public override string ToString()
        {
            return string.Format("quat=({0:F4},{1:F4},{2:F4},{3:F4})"/*, rpy={4}"*/, w, x, y, z/*, getRPY()*/);
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
            return Quaternion.Distance(quat, q.quat);
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
        internal Vector3 vec;

        public double x
        {
            get { return vec.X; }
            set { vec = new Vector3(value, y, z); }
        }

        public double y
        {
            get { return vec.Y; }
            set { vec = new Vector3(x, value, z); }
        }

        public double z
        {
            get { return vec.Z; }
            set { vec = new Vector3(x, y, value); }
        }

        public emVector3() : this(0, 0, 0)
        {
        }

        public emVector3(Vector3 v)
        {
            vec = v;
        }

        public emVector3(double X, double Y, double Z) : this(new Vector3(X, Y, Z))
        {
        }

        public emVector3(emVector3 shallow) : this(shallow.vec)
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
            return new emVector3(v1.vec + v2.vec);
        }

        public static emVector3 operator -(emVector3 v1, emVector3 v2)
        {
            return new emVector3(v1.vec - v2.vec);
        }

        public static emVector3 operator *(emVector3 v1, float d)
        {
            return new emVector3(d*v1.vec);
        }

        public static emVector3 operator *(emVector3 v1, int d)
        {
            return new emVector3(d*v1.vec);
        }

        public static emVector3 operator *(emVector3 v1, double d)
        {
            return new emVector3(d*v1.vec);
        }

        public static emVector3 operator *(float d, emVector3 v1)
        {
            return new emVector3(d*v1.vec);
        }

        public static emVector3 operator *(int d, emVector3 v1)
        {
            return new emVector3(d*v1.vec);
        }

        public static emVector3 operator *(double d, emVector3 v1)
        {
            return new emVector3(d*v1.vec);
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