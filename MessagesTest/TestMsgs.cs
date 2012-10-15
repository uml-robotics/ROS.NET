using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Messages;

namespace MessagesTest
{
    /// <summary>
    /// Summary description for TestMsgs
    /// </summary>
    [TestClass]
    public class TestMsgs
    {
        List<IRosMessage> msgs = new List<IRosMessage>();

        public TestMsgs()
        {        
            Array arr = Enum.GetValues(typeof(MsgTypes));
            MsgTypes[] all = arr as MsgTypes[];
            foreach (MsgTypes s in all)
            {
                if (s == MsgTypes.Unknown) continue;
                msgs.Add(IRosMessage.generate(s));
            }
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
            bool pass;
            foreach (IRosMessage m in msgs)
            {
                byte [] res = m.Serialize();
                IRosMessage deserialized = IRosMessage.generate(m.msgtype);
                deserialized = deserialized.Deserialize(res);
                deserialized.Serialized=null;
                byte[] dres = deserialized.Serialize();
                pass = TestEqual(res, dres);
                if (!pass)
                { 
                    Console.Error.WriteLine("\nTestEqual Failed: " + m.GetType().ToString() + " != " + deserialized.GetType().ToString());
                }
                else
                    Console.Error.WriteLine("\nTestEqual Succeded: " + m.GetType().ToString() + " == " + deserialized.GetType().ToString());
            }
        }
        bool TestEqual(byte[] original, byte[] copy)
        {
            if (original.Count() != copy.Count())
            {
                Console.Error.WriteLine("\nSize Mismatch:");
                return false;
            }
                for (int i = 0; i < original.Count(); i++)
                {
                    if (original[i] != copy[i])
                    {
                        return false;
                    }

                }
                return true;
        }
    }
}
