using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Messages;
using Ros_CSharp;

namespace tf.net
{
    public class TimeCache
    {
        private const int MIN_INTERPOLATION_DISTANCE = 5;
        private const uint MAX_LENGTH_LINKED_LIST = 10000000;
        private const Int64 DEFAULT_MAX_STORAGE_TIME = 1000000000;

        private ulong max_storage_time;
        private volatile SortedList<ulong, TransformStorage> storage = new SortedList<ulong, TransformStorage>();

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
            lock (storage)
            {
                if (storage.Count == 0)
                {
                    createEmptyException(ref error_str);
                    return 0;
                }

                if (target_time == 0)
                {
                    one = storage.Last().Value;
                    return 1;
                }

                if (storage.Count == 1)
                {
                    TransformStorage ts = storage.First().Value;
                    if (ts.stamp == target_time)
                    {
                        one = ts;
                        return 1;
                    }
                    createExtrapolationException1(target_time, ts.stamp, ref error_str);
                    return 0;
                }

                ulong latest_time = storage.Last().Key;
                ulong earliest_time = storage.First().Key;
                if (target_time == latest_time)
                {
                    one = storage.Last().Value;
                    return 1;
                }
                if (target_time == earliest_time)
                {
                    one = storage.First().Value;
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

                ulong i = 0;
                ulong j = storage.Last((kvp) =>
                                           {
                                               //look for the first keyvaluepair in the sorted list with a key greater than our target.
                                               //i is the last keyvaluepair's key, aka, the highest stamp 
                                               if (kvp.Key <= target_time)
                                               {
                                                   i = kvp.Key;
                                                   return false;
                                               }
                                               return true;
                                           }).Key;
                one = storage[i];
                two = storage[j];
            }
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

            double ratio = (time - one.stamp) / (two.stamp - one.stamp);
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
            ulong latest_time = storage.Last().Key;
            while (storage.Count > 0 && storage.First().Key + max_storage_time < latest_time || storage.Count > MAX_LENGTH_LINKED_LIST)
                storage.RemoveAt(0);
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
            lock (storage)
            {
                if (storage.Count > 0 && storage.First().Key > new_data.stamp + max_storage_time)
                    if (SimTime.instance.IsTimeSimulated)
                    {
                        storage.Clear();
                    }
                    else
                        return false;
                storage[new_data.stamp] = new_data;
                pruneList();
            }
            return true;
        }

        public void clearList()
        {
            lock(storage)
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
            lock (storage)
            {
                if (storage.Count == 0)
                {
                    return new TimeAndFrameID(0, 0);
                }
                TransformStorage ts = storage.Last().Value;
                return new TimeAndFrameID(ts.stamp, ts.frame_id);
            }
        }

        public uint getListLength()
        {
            lock(storage)
                return (uint)storage.Count;
        }

        public ulong getLatestTimeStamp()
        {
            lock (storage)
            {
                if (storage.Count == 0) return 0;
                return storage.Last().Key;
            }
        }

        public ulong getOldestTimestamp()
        {
            lock (storage)
            {
                if (storage.Count == 0) return 0;
                return storage.First().Key;
            }
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
}
