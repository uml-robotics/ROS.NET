#region Using

using System;
using System.Collections;
using Messages;

#endregion

namespace Ros_CSharp
{
    public class IServiceServerLink
    {
        public bool IsValid;
        public Connection connection;
        private IDictionary header_values;
        private string md5sum;
        private string md5sum_2;
        private string name;
        private bool persistent;

        public IServiceServerLink(string name, bool persistent, string md5sum, string md5sum_2,
                                  IDictionary header_values)
        {
            // TODO: Complete member initialization
            this.name = name;
            this.persistent = persistent;
            this.md5sum = md5sum;
            this.md5sum_2 = md5sum_2;
            this.header_values = header_values;
        }

        public IServiceServerLink()
        {
            throw new NotImplementedException();
        }

        public static object create(string request, string response)
        {
            Type gen = Type.GetType("ServiceServerLink").MakeGenericType(ROS.GetDataType(request),
                                                                         ROS.GetDataType(response));
            return gen.GetConstructor(null).Invoke(null);
        }

        internal void initialize(Connection connection)
        {
            this.connection = connection;
        }
    }

    public class ServiceServerLink<MReq, MRes> : IServiceServerLink
        where MReq : IRosMessage, new()
        where MRes : IRosMessage, new()
    {
        internal bool call<MReq, MRes>(MReq request, ref MRes response)
        {
            //THIS IS WRONG!!!
            /*TransportSubscriberLink bisexual = new TransportSubscriberLink(connection);
            bisexual.initialize(connection);
            bisexual.enqueueMessage(request as IRosMessage, true, true);*/
            Console.WriteLine("FINISH ME!");
            return true;
        }

        internal void reset()
        {
            
        }
    }

    internal struct CallInfo<MReq, MRes>
    {
    }
}