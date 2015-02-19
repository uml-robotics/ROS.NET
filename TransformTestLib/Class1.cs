using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Messages;
using Messages.std_msgs;
using Ros_CSharp;
#if !CONSOLEMODE
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif

namespace TransformTestLib
{
    public class TransformTest
    {
        
        private Transformer transformer;
        private Time when;
        public TransformTest()
        {
            when = ROS.GetTime();
            //
            // TODO: Add constructor logic here
            //
            transformer = new Transformer();
        }

        public void TestMethod1(bool istest = false)
        {
            emTransform a2b = new emTransform(
                new emQuaternion(),
                new emVector3(0.0, 0.0, 1.0),//1.0, 0.0, 0.5),
                when,
                "a",
                "b");
            emTransform b2c = new emTransform(
                new emQuaternion(),
                new emVector3(0.0, 0.0, -0.5),//-1.0, -0.5, 1.0),
                when,
                "b",
                "c");
            emTransform c2d = new emTransform(
                emQuaternion.FromRPY(new emVector3(0.0, 0.0, Math.PI / 4.0)),
                new emVector3(1.0, 0.0, 0.0),
                when,
                "c",
                "d");
            bool setsuccess = transformer.setTransform(a2b) && transformer.setTransform(b2c) && transformer.setTransform(c2d);
#if !CONSOLEMODE
            Assert.IsTrue(setsuccess);
#else
            if (!setsuccess)
            {
                throw new Exception("Failed to set transforms");
            }
#endif
            Console.WriteLine("DO SOMETHING SMART WITH TRANSFORMPOINT");
        }
    }
}
