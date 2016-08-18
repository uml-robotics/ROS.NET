using System;
using System.Diagnostics;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MD5SumTest
{
    [TestClass]
    public class MD5Test
    {
        #region TEST DICTIONARY UTILITY
        private Dictionary<string,string> _msgSums = null, _srvSums = null;
        private Dictionary<string,string> PopulateSums(ref Dictionary<string,string> target, string resource)
        {
            if (target != null) return target;
            target = new Dictionary<string, string>();
            string[] lines = test_resources.msg_sums.Replace("\r", "").Split('\n');
            foreach (string s in lines)
            {
                string[] pieces = s.Split(' ');
                if (pieces.Length == 2)
                {
                    target[pieces[0]] = pieces[1];
                }
                else
                    throw new Exception("Each line of the test resources must be \"{MSG|SRV}NAME MD5\"");
            }
            return target;
        }

        private Dictionary<string, string> msgSums { get { return _msgSums ?? PopulateSums(ref _msgSums, test_resources.msg_sums); } }
        private Dictionary<string, string> srvSums { get { return _srvSums ?? PopulateSums(ref _srvSums, test_resources.srv_sums); } }
        #endregion

        public MD5Test()
        {
        }

        [TestMethod]
        public void TestMethod1()
        {
            //test all generated msg md5s vs. dump of known ones on kinetic
            List<MsgTypes> msg_failures = new List<MsgTypes>();
            foreach (MsgTypes m in Enum.GetValues(typeof(MsgTypes)))
            {
                if (m == MsgTypes.Unknown) continue;
                IRosMessage msg = IRosMessage.generate(m);
                string type = msg.GetType().FullName.Replace("Messages.", "").Replace(".", "/");
                if (!msgSums.ContainsKey(type)) continue;
                string desiredSum = msgSums[type];
                string actualSum = msg.MD5Sum();
                bool eq = String.Equals(desiredSum,actualSum);
                Debug.WriteLine("{0}\t{1}", type, eq?"OK":"FAIL");
                if (!eq)
                    msg_failures.Add(m);
            }
            Assert.IsFalse(msg_failures.Any());

            //test all generated srv md5s vs. dump of known ones on kinetic
            List<SrvTypes> srv_failures = new List<SrvTypes>();
            foreach (SrvTypes m in Enum.GetValues(typeof(SrvTypes)))
            {
                if (m == SrvTypes.Unknown) continue;
                IRosService srv = IRosService.generate(m);
                string type = srv.GetType().FullName.Replace("Messages.", "").Replace(".", "/");
                if (!srvSums.ContainsKey(type)) continue;
                string desiredSum = srvSums[type];
                string actualSum = srv.MD5Sum();
                bool eq = String.Equals(desiredSum, actualSum);
                Debug.WriteLine("{0}\t{1}", type, eq ? "OK" : "FAIL");
                if (!eq)
                    srv_failures.Add(m);
            }
            Assert.IsFalse(srv_failures.Any());
        }
    }
}
