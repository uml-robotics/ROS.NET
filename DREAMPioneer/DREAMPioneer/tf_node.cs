using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.ComponentModel;
using System.Windows.Data;

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
        Dictionary<String, tf_frame> frames;
        List<tf_frame> currFrames;
        tf.tfMessage msg;

        private static NodeHandle tfhandle;
        private Subscriber<TypedMessage<tf.tfMessage>> tfsub;

 /*       private void waitfunc()
        {
            while (!ROS.initialized)
            {
                Thread.Sleep(100);
            }
            Dispatcher.BeginInvoke(new Action(SetupTopic));
        }

        private void SetupTopic()
        {

            if (imagehandle == null)
                imagehandle = new NodeHandle();
            if (robotsub != null)
                robotsub.shutdown();

            robotsub = imagehandle.subscribe<gm.PolygonStamped>(TopicName, 1, (i) =>
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    //Console.WriteLine(i.data.polygon.points[0].y);
                    float x = i.data.polygon.points[0].x - 0.19f;
                    float y = i.data.polygon.points[0].y - 0.19f;
                    updatePOS(x, y);
                })), "*");
        }
        */
        public tf_node()
        {
            frames = new Dictionary<String,tf_frame>();
            //subscribes
            msg = new tf.tfMessage();

            int i = 0;

            while( i < msg.transforms.Length )
            {
                addFrame( msg.transforms[i] );
            }
        }

        public void addFrame(gm.TransformStamped t)
        {
            frames.Add(t.header.frame_id, new tf_frame(t));
        }

        public tf_frame transformFrame(String source, String target)
        {
            if (!frames.ContainsKey(source))
                throw new Exception("Arrg! Source key does not exist!");
            if (!frames.ContainsKey(target))
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
            currFrames.Add( frames[source] );
        }

    }
}
