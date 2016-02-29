using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Messages.std_msgs;
using Ros_CSharp;

namespace tf.net
{
    public enum TF_STATUS
    {
        NO_ERROR,
        LOOKUP_ERROR,
        CONNECTIVITY_ERROR,
        EXTRAPOLATION_ERROR
    }


    public enum WalkEnding
    {
        Identity,
        TargetParentOfSource,
        SourceParentOfTarget,
        FullPath
    }

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
}
