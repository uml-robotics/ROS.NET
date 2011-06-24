#region USINGZ

using Messages;
using Messages.geometry_msgs;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace TestProject
{
    [TestClass]
    public class MessagesTest
    {
        [TestMethod]
        public void SerializationTest()
        {
            DummyMsgThing dmt = new DummyMsgThing(new DummyMsgThing.Data
                           {
                               leftnipple = new Twist.Data
                                                {
                                                    angular =
                                                        new Vector3.Data {x = 1, y = 2, z = 3}
                                                },
                               rightnipple =
                                   new Twist.Data {angular = new Vector3.Data {x = 4, y = 5, z = 6}}
                           });


            byte[] guts = dmt.Serialize();
            DummyMsgThing otherside = new DummyMsgThing(guts);

            Assert.AreEqual(dmt.data.leftnipple.angular.x, otherside.data.leftnipple.angular.x);
            Assert.AreEqual(dmt.data.leftnipple.angular.y, otherside.data.leftnipple.angular.y);
            Assert.AreEqual(dmt.data.leftnipple.angular.z, otherside.data.leftnipple.angular.z);
            Assert.AreEqual(dmt.data.leftnipple.linear.x, otherside.data.leftnipple.linear.x);
            Assert.AreEqual(dmt.data.leftnipple.linear.y, otherside.data.leftnipple.linear.y);
            Assert.AreEqual(dmt.data.leftnipple.linear.z, otherside.data.leftnipple.linear.z);
            Assert.AreEqual(dmt.data.rightnipple.angular.x, otherside.data.rightnipple.angular.x);
            Assert.AreEqual(dmt.data.rightnipple.angular.y, otherside.data.rightnipple.angular.y);
            Assert.AreEqual(dmt.data.rightnipple.angular.z, otherside.data.rightnipple.angular.z);
            Assert.AreEqual(dmt.data.rightnipple.linear.x, otherside.data.rightnipple.linear.x);
            Assert.AreEqual(dmt.data.rightnipple.linear.y, otherside.data.rightnipple.linear.y);
            Assert.AreEqual(dmt.data.rightnipple.linear.z, otherside.data.rightnipple.linear.z);
        }
    }
}