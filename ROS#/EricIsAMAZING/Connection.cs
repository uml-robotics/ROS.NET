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

        }

        public void writeHeader(IDictionary key_vals, WriteFinishedFunc finished_func)
        {

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
                transport.Close();
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
            
        }

        void transport_ReadCallback(TcpTransport trans, byte[] data, bool success)
        {
            
        }

        private void onReadable(TcpTransport trans)
        {

        }

        private void onWriteable(TcpTransport trans)
        {

        }

        private void onDisconnect(TcpTransport trans)
        {

        }
        private void onHeaderWritten(Connection conn)
        {

        }
        private void onErrorHeaderWritten(Connection con)
        {

        }
        private void onHeaderLengthRead(Connection connection, byte[] data, bool success)
        {

        }
        private void onHeaderRead(Connection con, byte[] buffer, bool success)
        {
        }
        private void readTransport()
        {

        }
        private void writeTransport()
        {

        }
    }
    public delegate void DisconnectFunc(Connection connection, Connection.DropReason reason);
    public delegate void HeaderReceivedFunc(Connection connection, Header header);
    public delegate void WriteFinishedFunc(Connection connection);
    public delegate void ReadFinishedFunc(Connection connection, byte[] data, bool success);
}
