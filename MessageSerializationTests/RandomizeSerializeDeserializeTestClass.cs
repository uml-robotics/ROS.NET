#define REMOVE_LENGTH_PREFIX
using System;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MessageSerializationTests
{
    /// <summary>
    /// For each message type, create a new message with valid random data, then serialize it
    /// For each message type, create another message and deserialize the origina's serialized data, then
    ///     compare the literal contents of the messages, iterating+recursing over/into all of their fields in parallel
    /// </summary>
    [TestClass]
    public class SerializationTest
    {
        private Random r = new Random();

        /// <summary>
        /// Test constructor
        /// Contains test-critical initialization. seriously. it does.
        /// </summary>
        public SerializationTest()
        {
        }

        /// <summary>
        /// utility - dump hex representation of a byte array
        /// </summary>
        /// <param name="test"></param>
        /// <returns></returns>
        public static string dumphex(byte[] test)
        {
            if (test == null)
                return "dumphex(null)";
            StringBuilder sb = new StringBuilder().Append(Array.ConvertAll<byte, string>(test, (t) => string.Format("{0,3:X2}", t)).Aggregate("", (current, t) => current + t));
            return sb.ToString();
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

        private Array GetMsgTypes()
        {
            /*return new MsgTypes[] {
                Messages.MsgTypes.dynamic_reconfigure__BoolParameter,
                Messages.MsgTypes.dynamic_reconfigure__Config,
                Messages.MsgTypes.BrutalMsgs__ManyArrays
            };*/
            return Enum.GetValues(typeof (MsgTypes));
        }

        [TestMethod]
        public void DeserializeRandomizedMessagesAndCompareRecursivelyToOriginals()
        {
            //randomize messages of all known types, and serialize one of each of them
            foreach (MsgTypes m in GetMsgTypes())
            {
                if (m == MsgTypes.Unknown) continue;
                IRosMessage omsg = IRosMessage.generate(m);
                Assert.IsTrue(omsg != null);
                try
                {
                    omsg.Randomize();
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Failed to randomize: "+omsg.GetType());
                    Assert.Fail();
                }
                IRosMessage original = (IRosMessage)omsg;
                original.Serialized = original.Serialize();
                Debug.WriteLine("Randomized " + m + " = " + dumphex(original.Serialized));
                //Assert.AreNotEqual(original.Serialized.Length, 0);
                IRosMessage msg = IRosMessage.generate(m);
                Assert.IsTrue(msg != null);

                //strip off the length we send with the message to subscribers
                Debug.WriteLine("Trying to deserialize " + m);
                msg.Deserialize(original.Serialized);
                bool match = original.Equals(msg);
                if (!match)
                {
                    //Debug.WriteLine(">>>>>>>>FAILED<<<<<<<<");
                    Assert.Fail();
                }
                Debug.WriteLine("PASS: " + m);
            }
        }
    }
}
