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

        /// <summary>
        /// Recursively randomized the contents of a message
        /// </summary>
        /// <param name="T">The type of the message or field</param>
        /// <param name="target">The message or field to set randomly</param>
        private void Randomize(Type T, ref object target, int hardcodedarraylength=-1)
        {
            if (!T.IsArray)
            {
                if (T != typeof (TimeData) && T.Namespace.Contains("Message"))
                {
                    IRosMessage msg = null;
                    FieldInfo[] infos = SerializationHelper.GetFields(T, ref target, out msg);
                    if (msg == null)
                    {
                        SerializationHelper.GetFields(T, ref target, out msg);
                        throw new Exception("SOMETHING AIN'T RIGHT");
                    }
                    foreach (FieldInfo info in infos)
                    {
                        if (info.Name.Contains("(")) continue;
                        if (msg.Fields[info.Name].IsConst) continue;
                        if (info.GetValue(target) == null)
                        {
                            if (info.FieldType == typeof (string))
                                info.SetValue(target, "");
                            else if (info.FieldType.IsArray)
                                info.SetValue(target, Array.CreateInstance(info.FieldType.GetElementType(), 0));
                            else if (info.FieldType.FullName != null && !info.FieldType.FullName.Contains("Messages."))
                                info.SetValue(target, 0);
                            else
                                info.SetValue(target, Activator.CreateInstance(info.FieldType));
                        }
                        object field = info.GetValue(target);
                        Randomize(info.FieldType, ref field, msg.Fields[info.Name].Length);
                        info.SetValue(target, field);
                    }
                }
                else if (target is byte || T == typeof (byte))
                {
                    target = (byte) r.Next(255);
                }
                else if (target is string || T == typeof (string))
                {
                    //create a string of random length [1,100], composed of random chars
                    int length = r.Next(100)+1;
                    byte[] buf = new byte[length];
                    r.NextBytes(buf);  //fill the whole buffer with random bytes
                    for(int i=0;i<length;i++)
                        if (buf[i] == 0) //replace null chars with non-null random ones
                            buf[i] = (byte)(r.Next(254) + 1);
                    buf[length - 1] = 0; //null terminate
                    target = Encoding.ASCII.GetString(buf);
                }
                else if (target is bool || T == typeof (bool))
                {
                    target = r.Next(2) == 1;
                }
                else if (target is int || T == typeof (int))
                {
                    target = r.Next();
                }
                else if (target is uint || T == typeof (int))
                {
                    target = (uint) r.Next();
                }
                else if (target is double || T == typeof (double))
                {
                    target = r.NextDouble();
                }
                else if (target is TimeData || T == typeof (TimeData))
                {
                    target = new TimeData((uint) r.Next(), (uint) r.Next());
                }
                else if (target is float || T == typeof (float))
                {
                    target = (float)r.NextDouble();
                }
                else if (target is Int16 || T == typeof (Int16))
                {
                    target = (Int16) r.Next(Int16.MaxValue + 1);
                }
                else if (target is UInt16 || T == typeof(UInt16))
                {
                    target = (UInt16)r.Next(UInt16.MaxValue + 1);
                }
                else if (target is SByte || T == typeof (SByte))
                {
                    target = (SByte) (r.Next(255)-127);
                }
                else if (target is UInt64 || T == typeof (UInt64))
                {
                    target = (UInt64)((uint)(r.Next() << 32)) | (uint)r.Next();
                }
                else if (target is Int64 || T == typeof (Int64))
                {
                    target = (Int64) (r.Next() << 32) | r.Next();
                }
                else if (target is char || T == typeof (char))
                {
                    target = (char) (byte) (r.Next(254)+1);
                }
                else
                {
                    throw new Exception("Unhandled randomization: " + T);
                }
            }
            else
            {
                int length = hardcodedarraylength != -1 ? hardcodedarraylength : r.Next(10);
                Type elem = T.GetElementType();
                Array field = Array.CreateInstance(elem, new int[] {length}, new int[] {0});
                for (int i = 0; i < length; i++)
                {
                    object val = field.GetValue(i);
                    Randomize(elem, ref val);
                    field.SetValue(val, i);
                }
                target = field;
            }
        }

        /// <summary>
        /// Compares all fields in 2 messages of a given type, recursing into non-literals in both messages
        /// </summary>
        /// <param name="T">The type of A and B</param>
        /// <param name="A">The first message to compare</param>
        /// <param name="B">The second message to compare</param>
        /// <returns>Whether or A and B contain the same values</returns>
        private bool Compare(Type T, ref object A, ref object B)
        {
            bool res = true;
            if (!T.IsArray)
            {
                if (T != typeof(TimeData) && T.Namespace.Contains("Message"))
                {
                    IRosMessage msgA = null, msgB = null;
                    FieldInfo[] infosA = SerializationHelper.GetFields(T, ref A, out msgA);
                    FieldInfo[] infosB = SerializationHelper.GetFields(T, ref B, out msgB);
                    res &= infosA.Length == infosB.Length;
                    if (res)
                        for(int i=0;i<infosA.Length;i++)
                        {
                            FieldInfo infoA = infosA[i];
                            FieldInfo infoB = infosB[i];
                            if (infoA.Name.Contains("(")) continue;
                            if (infoA.GetValue(A) == null)
                            {
                                if (msgA.Fields[infoA.Name].IsConst) Assert.Fail();
                                if (infoA.FieldType == typeof(string))
                                    infoA.SetValue(A, "");
                                else if (infoA.FieldType.IsArray)
                                    infoA.SetValue(A, Array.CreateInstance(infoA.FieldType.GetElementType(), 0));
                                else if (infoA.FieldType.FullName != null && !infoA.FieldType.FullName.Contains("Messages."))
                                    infoA.SetValue(A, 0);
                                else
                                    infoA.SetValue(A, Activator.CreateInstance(infoA.FieldType));
                            }
                            if (infoB.Name.Contains("(")) continue;
                            if (infoB.GetValue(B) == null)
                            {
                                if (msgB.Fields[infoB.Name].IsConst) Assert.Fail();
                                if (infoB.FieldType == typeof(string))
                                    infoB.SetValue(B, "");
                                else if (infoB.FieldType.IsArray)
                                    infoB.SetValue(B, Array.CreateInstance(infoB.FieldType.GetElementType(), 0));
                                else if (infoB.FieldType.FullName != null && !infoB.FieldType.FullName.Contains("Messages."))
                                    infoB.SetValue(B, 0);
                                else
                                    infoB.SetValue(B, Activator.CreateInstance(infoB.FieldType));
                            }
                            object fieldA = infoA.GetValue(A);
                            object fieldB = infoB.GetValue(B);
                            res &= Compare(infoA.FieldType, ref fieldA, ref fieldB);
                            if (!res)
                                Debug.WriteLine(infoB.Name + " DID NOT MATCH");
                            //Assert.IsTrue(res);
                        }
                }
                else if ((A is TimeData && B is TimeData) || T == typeof(TimeData))
                {
                    TimeData ta = (TimeData) A;
                    TimeData tb = (TimeData) B;
                    res &= ((ta.sec == tb.sec) && (ta.nsec == tb.nsec));
                    if (!res)
                        Debug.WriteLine("" + ta.sec + "." + ta.nsec + " != " + tb.sec + "." + tb.nsec);
                    //Assert.IsTrue(res);
                }
                else if ((A is string && B is string) || T == typeof (string))
                {
                    res &= string.Equals(A, B);
                    if (!res)
                        Debug.WriteLine("\""+A+"\" != \""+B+"\"");
                    //Assert.IsTrue(res);
                }
                else if ((A is char && B is char) || T == typeof(char))
                {
                    res &= ("" + ((char)A)).Equals("" + ((char)B));
                    if (!res)
                        Debug.WriteLine("" + A + "(" + A.GetType() + ") != " + B + "(" + B.GetType() + ")");
                }
                else
                {
                    res &= A.Equals(B);
                    if (!res)
                        Debug.WriteLine("" + A + "("+A.GetType()+") != " + B+"("+B.GetType()+")");
                    //Assert.IsTrue(res);
                }
            }
            else
            {
                Array fielda = (Array) A;
                Array fieldb = (Array) B;
                res &= fielda.GetLength(0).Equals(fieldb.GetLength(0));
                if (res)
                {
                    Type elem = T.GetElementType();
                    for (int i = 0; i < fielda.GetLength(0); i++)
                    {
                        object elemA = fielda.GetValue(i);
                        object elemB = fieldb.GetValue(i);
                        res &= Compare(elem, ref elemA, ref elemB);
                        if (!res)
                        {
                            Debug.WriteLine("ARRAY[" + i + "] DID NOT MATCH");
                        }
                        //Assert.IsTrue(res);
                    }
                }
            }
            return res;
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
                object omsg = IRosMessage.generate(m);
                Assert.IsTrue(omsg != null);
                try
                {
                    Randomize(omsg.GetType(), ref omsg);
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Failed to randomize: "+omsg.GetType());
                    Assert.Fail();
                }
                IRosMessage original = (IRosMessage)omsg;
                original.Serialized = original.Serialize();
                Debug.WriteLine("Randomized " + m + " = " + dumphex(original.Serialized));
                IRosMessage msg = IRosMessage.generate(m);
                Assert.IsTrue(msg != null);

                //strip off the length we send with the message to subscribers
                byte[] data = new byte[original.Serialized.Length - 4];
                for (int i = 0; i < data.Length; i++)
                    data[i] = original.Serialized[i+4];
                msg.Deserialize(data);
                object oo = original, om = msg;
                Assert.IsTrue(Compare(msg.GetType(), ref oo, ref om));
                Debug.WriteLine("PASS: " + m);
            }
        }
    }
}
