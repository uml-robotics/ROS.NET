using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EricIsAMAZING
{


    public class Connection
    {
        public TcpTransport transport;
        public event DisconnectFunc DroppedEvent;
        public event HeaderReceivedFunc HeaderReceivedEvent;
        public event WriteFinishedFunc header_written_callback;
        public ReadFinishedFunc read_callback;
        public WriteFinishedFunc write_callback;
        public HeaderReceivedFunc header_func;
        public bool dropped;
        public bool is_server;
        public bool sendingHeaderError;
        public string CallerID;
        public string RemoteString;
        public Header header;
        
        public void sendHeaderError(string error_message)
        {
            throw new NotImplementedException();
        }

        public void writeHeader(IDictionary key_vals, WriteFinishedFunc finished_func)
        {
            throw new NotImplementedException();
        }

        public void read(uint size, ReadFinishedFunc finished_func)
        {
            read_callback = finished_func;
            readTransport();
        }

        public void write(byte[] data, WriteFinishedFunc finished_func, bool immediate = true)
        {
            if (dropped || sendingHeaderError) return;
            write_callback = finished_func;
            transport.enableWrite();
            if (immediate)
                writeTransport();
        }

        public enum DropReason
        {
            TransportDisconnect, HeaderError, Destructing
        }

        public void drop(DropReason reason)
        {
            bool did_drop = false;
            if (!dropped)
            {
                dropped = true;
                did_drop = true;
                DroppedEvent(this, reason);
            }

            if (did_drop)
            {
                transport.close();
            }
        }

        public void initialize(TcpTransport trans, bool is_server, HeaderReceivedFunc header_func)
        {
            if (trans == null) throw new Exception("Connection innitialized with null transport");
            transport = trans;
            this.header_func = header_func;
            this.is_server = is_server;

            transport.ReadCallback += new TcpTransport.ReadFinishedFunc(transport_ReadCallback);
            transport.WriteCallback += new TcpTransport.WriteFinishedFunc(transport_WriteCallback);
            transport.DisconnectCallback += new TcpTransport.DisconnectFunc(transport_DisconnectCallback);

            if (header_func != null)
            {
                read(4, onHeaderLengthRead);
            }
        }

        void transport_DisconnectCallback(TcpTransport trans, Connection.DropReason reason)
        {
            drop(DropReason.TransportDisconnect);
        }

        void transport_WriteCallback(TcpTransport trans)
        {
            throw new NotImplementedException();
        }

        void transport_ReadCallback(TcpTransport trans)
        {
            throw new NotImplementedException();
        }

        private void onReadable(TcpTransport trans)
        {
            throw new NotImplementedException();
        }

        private void onWriteable(TcpTransport trans)
        {
            throw new NotImplementedException();
        }

        private void onDisconnect(TcpTransport trans)
        {
            throw new NotImplementedException();
        }
        private void onHeaderWritten(Connection conn)
        {
            throw new NotImplementedException();
        }
        private void onErrorHeaderWritten(Connection con)
        {
            throw new NotImplementedException();
        }
        private void onHeaderLengthRead(Connection connection, byte[] data, int size, bool success)
        {
            throw new NotImplementedException();
        }
        private void readTransport()
        {
            throw new NotImplementedException();
        }
        private void writeTransport()
        {
            throw new NotImplementedException();
        }
    }
    public delegate void ConnectFunc(Connection connection);
    public delegate void DisconnectFunc(Connection connection, Connection.DropReason reason);
    public delegate void HeaderReceivedFunc(Connection connection, Header header);
    public delegate void WriteFinishedFunc(Connection connection);
    public delegate void ReadFinishedFunc(Connection connection, byte[] data, int size, bool success);
}
