#if UNITY
#define FOR_UNITY
#endif
#if ENABLE_MONO
#define FOR_UNITY
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Messages.std_msgs;
using Messages.tf;
using Ros_CSharp;
using gm=Messages.geometry_msgs;

namespace tf.net
{
    public class Transformer
    {
        private const string tf_prefix = "/";
        private const uint MAX_GRAPH_DEPTH = 100;
        private const double DEFAULT_CACHE_TIME = 1000000000;
        private const ulong DEFAULT_MAX_EXTRAPOLATION_DISTANCE = 0;
        private ulong cache_time;

        private Dictionary<string, uint> frameIDs = new Dictionary<string, uint>();
        private Dictionary<uint, string> frameids_reverse = new Dictionary<uint, string>();
        private Dictionary<uint, TimeCache> frames = new Dictionary<uint, TimeCache>();

        private bool interpolating;
        private NodeHandle nh;
#if FOR_UNITY
        private static List<Transformer> instances = new List<Transformer>();
        public static void LateInit()
        {
            lock(instances)
            {
                foreach(Transformer t in instances)
                    t.InitNH();
                instances.Clear();
            }
        }
#endif

        private void InitNH()
        {
            nh = new NodeHandle();
            nh.subscribe<tfMessage>("/tf", 0, Update);
        }

        public Transformer(bool interpolating = true, ulong ct = (ulong) DEFAULT_CACHE_TIME)
        {
            frameIDs["NO_PARENT"] = 0;
            frameids_reverse[0] = "NO_PARENT";
#if FOR_UNITY
#if UNITY_EDITOR
            if (UnityEditor.EditorApplication.isPlaying)
            {
#endif
                if (!ROS.isStarted())
                {
                    lock(instances)
                        instances.Add(this);
                }
                else
                {
#endif
                    InitNH();
#if FOR_UNITY
                }
#if UNITY_EDITOR
            }
#endif
#endif
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
            if (!result && error_string != null)
                ROS.Error(error_string);
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
                transform.stamp = new Time(ROS.ticksToData((long)accum.time));
            }
            return retval == TF_STATUS.NO_ERROR;
        }

        public void transformQuaternion(string target_frame, Stamped<emQuaternion> stamped_in, ref Stamped<emQuaternion> stamped_out)
        {
            emTransform trans = new emTransform();
            lookupTransform(target_frame, stamped_in.frame_id, stamped_in.stamp, out trans);
            if (stamped_out == null)
                stamped_out = new Stamped<emQuaternion>();
            stamped_out.data = trans * stamped_in.data;
            stamped_out.stamp = trans.stamp;
            stamped_out.frame_id = target_frame;
        }

        public void transformQuaternion(string target_frame, Stamped<gm.Quaternion> stamped_in, ref Stamped<gm.Quaternion> stamped_out)
        {
            Stamped<emQuaternion> quatin = new Stamped<emQuaternion>(stamped_in.stamp, stamped_in.frame_id, new emQuaternion(stamped_in.data));
            Stamped<emQuaternion> quatout = new Stamped<emQuaternion>(stamped_out.stamp, stamped_out.frame_id, new emQuaternion(stamped_out.data));
            transformQuaternion(target_frame, quatin, ref quatout);
            if (stamped_out == null)
                stamped_out = new Stamped<gm.Quaternion>();
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
            emVector3 output = (trans * end) - (trans * origin);
            if (stamped_out == null)
                stamped_out = new Stamped<emVector3>();
            stamped_out.data = output;
            stamped_out.stamp = trans.stamp;
            stamped_out.frame_id = target_frame;
        }

        public void transformVector(string target_frame, Stamped<gm.Vector3> stamped_in, ref Stamped<gm.Vector3> stamped_out)
        {
            Stamped<emVector3> vecin = new Stamped<emVector3>(stamped_in.stamp, stamped_in.frame_id, new emVector3(stamped_in.data));
            Stamped<emVector3> vecout = new Stamped<emVector3>(stamped_out.stamp, stamped_out.frame_id, new emVector3(stamped_out.data));
            transformVector(target_frame, vecin, ref vecout);
            if (stamped_out == null)
                stamped_out = new Stamped<gm.Vector3>();
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
                frameIDs[frame] = (uint)(frameIDs.Count + 1);
                frameids_reverse[(uint)frameids_reverse.Count + 1] = frame;
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
            TimeCache parent_frame = null, frame = null;
            if (!frames.ContainsKey(frame_number))
            {
                parent_frame = frames[frame_number] = new TimeCache(cache_time);
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
            return frame.insertData(new TransformStorage(mapped_transform, frame_number, child_frame_number));
        }

        public bool waitForTransform(string target_frame, string source_frame, Time time, Duration timeout, ref string error_msg)
        {
            return waitForTransform(target_frame, source_frame, time, timeout, ref error_msg, null);
        }

        public bool waitForTransform(string target_frame, Time target_time, string source_frame, Time source_time, Duration timeout, ref string error_msg)
        {
            return waitForTransform(target_frame, target_time, source_frame, source_time, timeout, ref error_msg, null);
        }

        public bool waitForTransform(string target_frame, Time target_time, string source_frame, Time source_time, Duration timeout, ref string error_msg, Duration pollingSleepDuration)
        {
            return waitForTransform(target_frame, source_frame, target_time, timeout, ref error_msg, pollingSleepDuration) &&
                   waitForTransform(target_frame, source_frame, source_time, timeout, ref error_msg, pollingSleepDuration);
        }

        public bool waitForTransform(string target_frame, string source_frame, Time time, Duration timeout, ref string error_msg, Duration pollingSleepDuration)
        {
            TimeSpan? ts = null;
            if (pollingSleepDuration != null)
                ts = ROS.GetTime(pollingSleepDuration);
            return waitForTransform(target_frame, source_frame, time, 
                ROS.GetTime(timeout),
                ref error_msg, ts);
        }

        private bool waitForTransform(string target_frame, string source_frame, Time time, TimeSpan timeout, ref string error_msg, TimeSpan? pollingSleepDuration)
        {
            if (pollingSleepDuration == null)
                pollingSleepDuration = new TimeSpan(0, 0, 0, 0, 100);
            DateTime start_time = DateTime.Now;
            string mapped_target = resolve(tf_prefix, target_frame);
            string mapped_source = resolve(tf_prefix, source_frame);

            do
            {
                if (canTransform(mapped_target, mapped_source, time, ref error_msg))
                    return true;
                if (!ROS.ok || !(DateTime.Now.Subtract(start_time).TotalMilliseconds < timeout.TotalMilliseconds))
                    break;
                Thread.Sleep(pollingSleepDuration.Value);
            } while (ROS.ok && (DateTime.Now.Subtract(start_time).TotalMilliseconds < timeout.TotalMilliseconds));
            return false;
        }

        public bool waitForTransform(string target_frame, string source_frame, Time time, Duration timeout, Duration pollingSleepDuration)
        {
            string error_msg = null;
            return waitForTransform(target_frame, source_frame, time, timeout, ref error_msg, pollingSleepDuration);
        }

        public bool waitForTransform(string target_frame, Time target_time, string source_frame, Time source_time, Duration timeout, Duration pollingSleepDuration)
        {
            string error_msg = null;
            return waitForTransform(target_frame, target_time, source_frame, source_time, timeout, ref error_msg, pollingSleepDuration);
        }

        private bool waitForTransform(string target_frame, string source_frame, Time time, TimeSpan timeout, TimeSpan? pollingSleepDuration)
        {
            string error_msg = null;
            return waitForTransform(target_frame, source_frame, time, timeout, ref error_msg, pollingSleepDuration);
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
}
