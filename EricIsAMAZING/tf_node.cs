using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
//using System.Windows.Threading;
using System.ComponentModel;
//using System.Windows.Data;
using System.IO;


using Messages;
using Messages.custom_msgs;
using Ros_CSharp;
using XmlRpc_Wrapper;


using tf = Messages.tf;
using gm = Messages.geometry_msgs;
using String = Messages.std_msgs.String;

namespace Ros_CSharp
{
    // Listenes to the /tf topic, need subscriber
    // for each Transform in /tf, create a new frame. Frame has a (frame)child and (frame)id
    // provide translation from 2 frames, user requests from /map to /base_link for example, must identify route
    // base_link.child = odom, odom.child = map
    // map-> odom + odom->base_link
    public static class tf_node
    {
        static Dictionary<string, tf_frame> frames;
        static List<tf_frame> currFrames;
        static tf.tfMessage msg;

        private static NodeHandle tfhandle;
        private static Subscriber<tf.tfMessage> tfsub;

        public static void init()
        {

            frames = new Dictionary<string, tf_frame>();
            //subscribes
            msg = new tf.tfMessage();

            if (tfhandle == null)
                      tfhandle = new NodeHandle();
                  if (tfsub != null)
                      tfsub.shutdown();

                  tfsub = tfhandle.subscribe<tf.tfMessage>("/tf", 1, tfCallback);
        }

        private static void tfCallback(tf.tfMessage msg)
        {
            if (frames ==null)
                frames = new Dictionary<string,tf_frame>();

                foreach (Messages.geometry_msgs.TransformStamped t in msg.transforms)
                {
                    addFrame(t);
                }
        }

        public static void addFrame(gm.TransformStamped t)
        {
            if (!frames.ContainsKey(t.header.frame_id.data))
            {
                frames.Add(t.header.frame_id.data, new tf_frame(t));
            }
            else
            {
                frames[t.header.frame_id.data].transform = t.transform;
            }
        }

        public static tf_frame transformFrame(string source, string target, out gm.Vector3 vec, out gm.Quaternion quat)
        {
            try
            {
                currFrames = new List<tf_frame>();

                link(source, target);
                gm.Transform trans;
                trans = new gm.Transform();
                vec = new gm.Vector3();
                quat = new gm.Quaternion();

                foreach (tf_frame k in currFrames)
                {
                    quat.w += k.transform.rotation.w;
                    quat.x += k.transform.rotation.x;
                    quat.y += k.transform.rotation.y;
                    quat.z += k.transform.rotation.z;
                    vec.x += k.transform.translation.x;
                    vec.y += k.transform.translation.y;
                    vec.z += k.transform.translation.z;
                }
            }
            catch (Exception e) { vec = new gm.Vector3(); quat = new gm.Quaternion(); }
            return new tf_frame();
        }

        public static void link(string source, string target)
        {
            if ( frames.ContainsKey(target))
            {
                if(source != target)
                    link(source, frames[target].child_id.data);
                currFrames.Add(frames[target]);
            }
        }

    }


    public class tf_frame
    {
        gm.TransformStamped msg;
        static int numberofframes;

        public tf_frame()
        {

        }

        public tf_frame(gm.TransformStamped _msg)
        {
            numberofframes++;
            msg = _msg;

        }
        #region Variables and accessors
        public String frame_id
        {
            get { return msg.header.frame_id; }
            set { msg.header.frame_id = value; }
        }

        public String child_id
        {
            get { return msg.child_frame_id; }
            set { msg.child_frame_id = value; }
        }
        public gm.Transform transform
        {
            get { return msg.transform; }
            set { msg.transform = value; }
        }
        #endregion
    }
}
