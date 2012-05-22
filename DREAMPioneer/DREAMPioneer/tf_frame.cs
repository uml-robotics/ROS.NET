using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using tf = Messages.tf;
using gm = Messages.geometry_msgs;
using String = Messages.std_msgs.String;
namespace DREAMPioneer
{
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
            get { return msg.child_frame_id;}
            set { msg.child_frame_id = value;}
        }
        public gm.Transform transform
        {
            get { return msg.transform; }
            set { msg.transform = value; }
        }
        #endregion
    }
}
