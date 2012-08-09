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
    public class tf_node
    {
        private static tf_node _instance;
        public static tf_node instance
        {
            get
            {
                if (_instance == null)
                    _instance = new tf_node();
                return _instance;
            }
        }
        static Dictionary<string, tf_frame> frames;

        private NodeHandle tfhandle;
        private Subscriber<tf.tfMessage> tfsub;
        private Queue<tf.tfMessage> additions;
        public object addlock = new object();
        public object frameslock = new object();
        public Thread updateThread;

        public tf_node()
        {
            if (additions == null)
                additions = new Queue<tf.tfMessage>();
            if (frames == null)
                frames = new Dictionary<string, tf_frame>();
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
                            Queue<tf.tfMessage> local;
                            lock (addlock)
                            {
                                local = new Queue<Messages.tf.tfMessage>(additions);
                                additions.Clear();
                            }
                            if (local.Count > 0)
                            {
                                Dictionary<string, tf_frame> localframes;
                                lock (frameslock)
                                {
                                    localframes = new Dictionary<string, tf_frame>(frames);
                                }
                                while (local.Count > 0)
                                {
                                    tf.tfMessage msg = local.Dequeue();
                                    foreach (Messages.geometry_msgs.TransformStamped t in msg.transforms)
                                    {
                                        //Console.WriteLine("TF UPDATE: " + t.header.frame_id.data + " --> " + t.child_frame_id.data);
                                        if (!localframes.ContainsKey(t.header.frame_id.data))
                                        {
                                            localframes.Add(t.header.frame_id.data, new tf_frame(t));
                                        }
                                        else
                                        {
                                            localframes[t.header.frame_id.data].transform = t.transform;
                                        }
                                    }
                                }
                                lock (frameslock)
                                    frames = localframes;
                            }
                            Thread.Sleep(1);
                        }
                    });
                updateThread.Start();
            }
            tfsub = tfhandle.subscribe<tf.tfMessage>("/tf", 1, tfCallback);
        }

        private void tfCallback(tf.tfMessage msg)
        {
            //if (frames.Count < 16) Console.WriteLine("OH NOZ NOT ENUF TRANZFURMS (("+frames.Count+"))");
            lock(addlock)
                additions.Enqueue(msg);
        }

        public tf_frame transformFrame(string source, string target, out gm.Vector3 vec, out gm.Quaternion quat)
        {
            try
            {
                List<tf_frame> currFrames = link(source, target);
                gm.Transform trans;
                trans = new gm.Transform();                
                vec = new gm.Vector3();
                vec.x = vec.y = vec.z = 0;
                quat = new gm.Quaternion();
                quat.w = quat.x = quat.y = quat.z = 0;
                //Console.WriteLine("Tapdancing through " + currFrames.Count + "FRAMES");
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
            catch (Exception e) { Console.WriteLine(e); vec = new Messages.geometry_msgs.Vector3(); quat = new Messages.geometry_msgs.Quaternion(); }
            return new tf_frame();
        }

        private Dictionary<string, Dictionary<string, List<tf_frame>>> memo;
        private Dictionary<string, Dictionary<string, DateTime>> updated; 
        public List<tf_frame> link(string source, string target)
        {
            lock (frameslock)
            {                
                return link_unlocked(source, target);
            }
        }
        private List<tf_frame> link_unlocked(string source, string target)
        {
            bool doit = false;
            if (memo == null)
                memo = new Dictionary<string, Dictionary<string, List<tf_frame>>>(); 
            if (updated == null)
                updated = new Dictionary<string, Dictionary<string, DateTime>>();
            if (!memo.ContainsKey(source))
            {
                doit = true;
                memo.Add(source, new Dictionary<string, List<tf_frame>>());
                updated.Add(source, new Dictionary<string, DateTime>());
            }
            if (!memo[source].ContainsKey(target))
            {
                doit = true;
                memo[source].Add(target, new List<tf_frame>());
                updated[source].Add(target, DateTime.Now);
            }
            double dift = DateTime.Now.Subtract(updated[source][target]).TotalMilliseconds;
            //Console.WriteLine(dift);
            doit |=  dift > 10;
            if (doit)
            {
                if (memo.ContainsKey(source) && memo[source].ContainsKey(target))
                    memo[source][target].Clear();
                if (frames.ContainsKey(target))
                {
                    if (source != target)
                    {
                        List<tf_frame> res = link_unlocked(source, frames[target].child_id.data);
                        memo[source][target].AddRange(res);                        
                    }
                    memo[source][target].Add(frames[target]);
                }
                updated[source][target] = DateTime.Now;
            }

            return memo[source][target];
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
