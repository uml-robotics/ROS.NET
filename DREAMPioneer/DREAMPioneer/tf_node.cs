using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Threading;
using System.ComponentModel;
using System.Windows.Data;
using System.IO;


using Messages;
using Messages.custom_msgs;
using Ros_CSharp;
using XmlRpc_Wrapper;


using tf = Messages.tf;
using gm = Messages.geometry_msgs;
using String = Messages.std_msgs.String;

namespace DREAMPioneer
{
    // Listenes to the /tf topic, need subscriber
    // for each Transform in /tf, create a new frame. Frame has a (frame)child and (frame)id
    // provide translation from 2 frames, user requests from /map to /base_link for example, must identify route
    // base_link.child = odom, odom.child = map
    // map-> odom + odom->base_link
    class tf_node
    {
        Dictionary<string, tf_frame> frames;
        List<tf_frame> currFrames;
        tf.tfMessage msg;
        Thread mythread;
        bool firsttime = true;

        private static NodeHandle tfhandle;
        private Subscriber<TypedMessage<tf.tfMessage>> tfsub;

       /*private void waitfunc()
        {
            while (!ROS.initialized)
            {
                Thread.Sleep(100);
            }
            Dispatcher.BeginInvoke(new Action(SetupTopic));
        }
       
              private void SetupTopic()
              {

                  if (tfhandle == null)
                      tfhandle = new NodeHandle();
                  if (tfsub != null)
                      tfsub.shutdown();

                  tfsub = tfhandle.subscribe<tf.tfMessage>("/tf", 1, (n) =>
                      Dispatcher.BeginInvoke(new Action(() =>
                      {
                          int j = 0; 
                          while (j < msg.transforms.Length)
                          {
                              addFrame(msg.transforms[j]);
                          }
                      })), "*");
              }*/

        private void init()
        {
            if (tfhandle == null)
                      tfhandle = new NodeHandle();
                  if (tfsub != null)
                      tfsub.shutdown();

                  tfsub = tfhandle.subscribe<tf.tfMessage>("/tf", 1, tfCallback);
        }

        private void tfCallback(TypedMessage<tf.tfMessage> msg)
        {

            //if (msg.data.transforms.Length > frames.Count)
            //{
            if (frames ==null)
                frames = new Dictionary<string,tf_frame>();

                foreach (Messages.geometry_msgs.TransformStamped t in msg.data.transforms)
                {
                    addFrame(t);
                }
        }

        public tf_node()
        {
            frames = new Dictionary<string,tf_frame>();
            //subscribes
            msg = new tf.tfMessage();
            init();
            mythread = new Thread( new ThreadStart( init ));
        }

        public void addFrame(gm.TransformStamped t)
        {
            if (!frames.ContainsKey(t.header.frame_id.data))
            {
                frames.Add(t.header.frame_id.data, new tf_frame(t));
                //Console.WriteLine(frames.Count + " " + frames[t.header.frame_id.data].frame_id.data);
            }
            else
            {
                frames[t.header.frame_id.data].transform = t.transform;
                //Console.WriteLine(frames.Count + " " + frames[t.header.frame_id.data].frame_id.data);
            }
        }


        public tf_frame transformFrame(String source, String target)
        {
            if (!frames.ContainsKey(source.data))
                throw new Exception("Arrg! Source key does not exist!");
            if (!frames.ContainsKey(target.data))
                throw new Exception("Arrg! Target key does not exist!");

            currFrames = new List<tf_frame>();
            
            link(source, target);

            foreach(tf_frame k in currFrames)
            {
                gm.Transform trans = new gm.Transform();
                trans.rotation.w += k.transform.rotation.w;
                trans.rotation.x += k.transform.rotation.x;
                trans.rotation.y += k.transform.rotation.y;
                trans.rotation.z += k.transform.rotation.z;
                trans.translation.x += k.transform.translation.x;
                trans.translation.y += k.transform.translation.y;
                trans.translation.z += k.transform.translation.z;
            }
            return new tf_frame();
        }

        public void link(String source, String target)
        {
            if (source != target)
                link(source, target);
            currFrames.Add( frames[source.data] );
        }

    }


    class tf_frame
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
