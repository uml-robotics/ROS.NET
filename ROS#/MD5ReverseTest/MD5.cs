#region USINGZ

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

#endregion

namespace EricIsAMAZING
{
    public static class MD5
    {
        public static string Sum(string str)
        {
            return Sum(Encoding.UTF8.GetBytes(str));
        }

        public static string Sum(byte[] data)
        {
            string s = "";
            byte[] sum = System.Security.Cryptography.MD5.Create().ComputeHash(data);
            foreach (byte b in sum)
            {
                s += b.ToString("x");
            }
            return s.TrimEnd(' ','\t','\n');
        }

        public static List<char> alphanum = new List<char>();
        static object numlock = new object();
        static int numcount = 0;
        static bool abort;
        public static string Reverse(string md5)
        {
            return Reverse(md5, new List<string>());
        }

        static void startprinting()
        {
            if (printer == null)
            {
                printer = new Thread(() =>
                                         {
                                             string offset = "";
                                             while (!abort)
                                             {
                                                 Thread.Sleep(500);
                                                 lock (numlock)
                                                 {
                                                     offset = "";
                                                     for (int i = thissum1.Length; i < longest; i++)
                                                         offset += " ";
                                                     Console.WriteLine(numcount + "\t" + thissum1 + offset + "\t" + thissummaker);
                                                 }
                                             }
                                         });
                printer.IsBackground = true;
                printer.Start();
            }
        }
        static Thread printer = null;

        public static string Reverse(string md5, List<string> frontier)
        {
            startprinting();
            abort = false;
            numcount = 0;
            if (alphanum.Count == 0)
            {
                for (int i = 0; i < 10; i++)
                    alphanum.Add((""+i)[0]);
                for (int i = 65; i < 91; i++)
                {
                    frontier.Add("" + ((char)i));
                    alphanum.Add((char) i);
                }

                frontier.Add("" + '/');
                alphanum.Add('/');
            }
            if (frontier.Count == 0)
            {
                return "";
            }
            string res = "";
            bool spawned = false;
            while (!abort && frontier.Count > 0)
            {
                frontier = new List<string>(TestAndExpand(md5, frontier));
                if (frontier.Count < 35000)
                {
                    if (frontier.Count == 1)
                    {
                        res = frontier[0];
                        abort = true;
                        break;
                    }
                }
                else
                {
                    spawned = true;
                    bool GOTIT = false;
                    int count = frontier.Count;
                    for (int i = 0; i < 34; i++)
                    {
                        List<string> chunk = frontier.Take((count/35)).ToList();
                        frontier = frontier.Except(chunk).ToList();
                        new Thread((L) =>
                                       {
                                               List<string> f = (List<string>) L;
                                           while (!abort && f.Count > 0)
                                           {
                                               f  = new List<string>(TestAndExpand(md5, f));
                                               if (f.Count == 1)
                                               {
                                                   res = f[0];
                                                   abort = true;
                                               }
                                           }
                                       }).Start(chunk);
                    }
                    if (GOTIT || abort) break;
                }
            }
            Console.WriteLine(res);
            return res;
        }
        static int longest = 0;
        static string thissummaker = "";
        static string thissum1 = "";
        public static List<string> TestAndExpand(string seeking, List<string> old)
        {
            List<string> res = new List<string>();
            foreach (string s in old)
            {
                foreach (char ch in alphanum)
                {
                    if (abort) break;
                    string news = s + ch;
                    res.Add(news);
                    //lock (numlock)
                    //{
                        numcount++;
                    //}
                    thissummaker = news;
                        thissum1 = Sum(news);
                        if (thissum1.Length > longest)
                            longest = thissum1.Length;
                    
                    byte startcount =  (byte)news.Count((c)=>char.IsUpper(c));
                    byte curr = startcount;
                    while (curr > 0)
                    {
                        foreach(List<byte> ind in CombinationsBS.Combinations((byte)(startcount), (byte)(curr)))
                        {
                            foreach (byte b in ind)
                            {
                                if (b >= news.Length) continue;
                                if (char.IsUpper(news[(int) b]))
                                    news = news.Replace(news[(int) b], char.ToLower(news[(int) b]));
                            }
                            //lock (numlock)
                            
                                numcount++;
                                /*thissummaker = news;
                                thissum1 = Sum(news);
                                if (thissum1.Length > longest)
                                    longest = thissum1.Length;
                            */
                                if (Sum(news) == seeking)
                            {
                                abort = true;
                                return new List<string> {news};
                            }
                            news = news.ToUpper();
                        }

                        curr--;
                    }
                }
                if (abort) break;
            }
            //= (from s in old from c in alphanum select s + c).ToList();
            return res;
        }

        /// <summary>
        ///   The combinations bs.
        /// </summary>
        internal static class CombinationsBS
        {
            /// <summary>
            ///   The _combinations.
            /// </summary>
            private static List<List<List<byte>>>[] _combinations = new List<List<List<byte>>>[31];

            /// <summary>
            ///   The combinations.
            /// </summary>
            /// <param name = "count">
            ///   The count.
            /// </param>
            /// <returns>
            /// </returns>
            internal static List<List<byte>> Combinations(byte n, byte k)
            {
                //Console.WriteLine("(" + n + ", " + k + ")");
                while (_combinations[n] == null)
                    _combinations[n] = new List<List<List<byte>>>();

                while (_combinations[n].Count <= k)
                    _combinations[n].Add(new List<List<byte>>());
                
                if (_combinations[n][k].Count == 0)
                    _combinations[n][k] = Combine(n,k);

                return _combinations[n][k];
            }

            /// <summary>
            ///   The combine.
            /// </summary>
            /// <param name = "COUNT">
            ///   The count.
            /// </param>
            /// <returns>
            /// </returns>
            private static List<List<byte>> Combine(byte n, byte k)
            {
                if (n == 1)
                {
                    Console.WriteLine("0");
                    return new List<List<byte>> {new List<byte> {0}};
                }
                if (k == 0)
                    return new List<List<byte>>();
                if (k == 1)
                {
                    List<List<byte>> singles = new List<List<byte>>();
                    while (n > 0)
                    {
                        singles.Add(new List<byte> { n-- });
                    }
                    singles.Add(new List<byte> { n });
                    foreach(List<byte> B in singles)
                    {
                        foreach (byte b in B)
                            Console.Write("" + b.ToString("x") + " ");
                        Console.Write("\n");
                    }
                    return singles;
                }
                List<byte> WHOLESET = new List<byte>();
                for (byte i = (byte)(n - 0x1); i > 0; i--)
                    WHOLESET.Add(i);
                WHOLESET.Add(0);
                List<List<byte>> ret= Combine(WHOLESET, Math.Min(n, k));

                foreach (List<byte> B in ret)
                {
                    foreach(byte l in B)
                        Console.Write("" + l.ToString("x") + "  ");
                    Console.Write("\n");
                }
                return ret;
            }

            /// <summary>
            ///   The combine.
            /// </summary>
            /// <param name = "WHOLESET">
            ///   The wholeset.
            /// </param>
            /// <param name = "COUNT">
            ///   The count.
            /// </param>
            /// <returns>
            /// </returns>
            private static List<List<byte>> Combine(IEnumerable<byte> WHOLESET, byte COUNT)
            {
                IEnumerable<byte> wholeset = WHOLESET.ToList();
                List<List<byte>> ret = new List<List<byte>>();
                foreach (byte i in wholeset)
                {
                    if (COUNT == 0)
                        ret.Add(new List<byte>());
                    else if (COUNT == 1)
                        ret.Add(new List<byte>(new[] { i }));
                    else
                    {
                        List<List<byte>> afterthisone = Combine(wholeset.Except(new[] { i }), (byte)(COUNT - 0x1));
                        foreach (List<byte> l in afterthisone.Select(list => new List<byte>(list) {i}).Where(l => !ret.Any(beentheredonethat => beentheredonethat.Intersect(l).Count() == beentheredonethat.Count)))
                        {
                            ret.Add(l);
                        }
                    }
                }

                return ret;
            }
        }
    }
}