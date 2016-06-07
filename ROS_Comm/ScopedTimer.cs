using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Ros_CSharp
{
    internal static class ScopedTimer
    {
        internal class ScopedTimerRecord
        {
            internal ScopedTimerRecord(string file, int line)
            {
                this.file = file;
                this.line = line;
            }
            private string file;
            private int line;
            private long count = -1;
            private DateTime lastreport = DateTime.Now;

            internal void update()
            {
                if (count <= 0 || DateTime.Now.Subtract(lastreport).TotalMilliseconds > 1000)
                {
                    if (count <= 0)
                        count = 1;
                    Console.WriteLine(string.Format("{0}@{1}: last={2}", file, line, count));
                    lastreport = DateTime.Now;
                    count = 0;
                }
                count++;
            }
        }

        internal static Dictionary<string, Dictionary<int, ScopedTimerRecord>> records = new Dictionary<string, Dictionary<int, ScopedTimerRecord>>();
        internal static void Ping()
        {
            return; //NO-OP
            StackFrame sf = new StackTrace(new StackFrame(1, true)).GetFrame(0);
            string file = sf.GetFileName();
            int line = sf.GetFileLineNumber();
            Dictionary<int, ScopedTimerRecord> filedict;
            ScopedTimerRecord linerecord;
            lock (records)
            {
                if (!records.ContainsKey(file))
                {
                    records.Add(file, new Dictionary<int, ScopedTimerRecord>());
                }
                filedict = records[file];
            }
            lock(filedict)
            { 
                if (!filedict.ContainsKey(line))
                {
                    filedict.Add(line, new ScopedTimerRecord(file, line));
                }
                linerecord = filedict[line];
            }
            linerecord.update();
        }
    }
}
