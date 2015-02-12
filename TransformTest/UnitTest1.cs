using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Messages;
using Messages.std_msgs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ros_CSharp;

namespace TransformTest
{
    /// <summary>
    /// Summary description for UnitTest1
    /// </summary>
    [TestClass]
    public class UnitTest1
    {
        private Transformer transformer;
        private Time when;
        public UnitTest1()
        {
            when = ROS.GetTime();
            //
            // TODO: Add constructor logic here
            //
            transformer = new Transformer(false);
        }

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

        [TestMethod]
        public void TestMethod1()
        {
            //
            // TODO: Add test logic here
            //
            emTransform a2b = new emTransform(
                new emQuaternion(),
                new emVector3(1.0, 0.0, 0.5),
                when,
                "a",
                "b");
            emTransform b2c = new emTransform(
                new emQuaternion(),
                new emVector3(-1.0, -0.5, 1.0),
                when,
                "b",
                "c");
            emTransform c2d = new emTransform(
                new emQuaternion(0.14644660940672619, 0.35355339059327373, 0.35355339059327373, 0.8535533905932738),
                new emVector3(1.0,0.0,0.0),
                when,
                "c",
                "d");
            Assert.IsTrue(transformer.setTransform(a2b));
            Assert.IsTrue(transformer.setTransform(b2c));
            Assert.IsTrue(transformer.setTransform(c2d));
            emTransform forward = new emTransform();
            emTransform backward = new emTransform();
            transformer.lookupTransform("c", "a", when, out forward);
            transformer.lookupTransform("a", "c", when, out backward);
            backward.rotation = backward.rotation.inverse();
            backward.translation = backward.translation * -1;
            Assert.AreEqual(forward.translation.ToString(), backward.translation.ToString());
            Assert.AreEqual(forward.rotation.ToString(), backward.rotation.ToString());
            forward = new emTransform();
            backward = new emTransform();
            transformer.lookupTransform("d", "a", when, out forward);
            transformer.lookupTransform("a", "d", when, out backward);
            backward.rotation = backward.rotation.inverse();
            backward.translation = backward.translation * -1;
            Assert.AreEqual(forward.rotation.ToString(), backward.rotation.ToString());
            Assert.AreEqual(forward.translation.ToString(), backward.translation.ToString());
        }
    }
}
