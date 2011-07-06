#region USINGZ

using System;
using System.Collections;

#endregion

namespace EricIsAMAZING
{
    public class Connection
    {
        #region DropReason enum

        public enum DropReason
        {
            TransportDisconnect,
            HeaderError,
            Destructing
        }

        #endregion

        public string CallerID;
        public string RemoteString;
        public bool dropped;
        public Header header;
        public HeaderReceivedFunc header_func;
        public bool is_server;
        public ReadFinishedFunc read_callback;
        public bool sendingHeaderError;

        public TcpTransport transport;
        public WriteFinishedFunc write_callback;
        public event DisconnectFunc DroppedEvent;
        public event HeaderReceivedFunc HeaderReceivedEvent;
        public event WriteFinishedFunc header_written_callback;

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

            transport.ReadCallback += transport_ReadCallback;
            transport.WriteCallback += transport_WriteCallback;
            transport.DisconnectCallback += transport_DisconnectCallback;

            if (header_func != null)
            {
                read(4, onHeaderLengthRead);
            }
        }

        private void transport_DisconnectCallback(TcpTransport trans, DropReason reason)
        {
            drop(DropReason.TransportDisconnect);
        }

        private void transport_WriteCallback(TcpTransport trans)
        {
            throw new NotImplementedException();
        }

        private void transport_ReadCallback(TcpTransport trans)
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