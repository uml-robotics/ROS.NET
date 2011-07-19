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
        public object drop_mutex = new object();
        public bool dropped;
        public Header header = new Header();
        public HeaderReceivedFunc header_func;
        public WriteFinishedFunc header_written_callback;
        public bool is_server;
        public byte[] read_buffer;
        public ReadFinishedFunc read_callback;
        public int read_filled;
        public object read_mutex = new object();
        public int read_size;
        public bool reading;
        public bool sendingHeaderError;
        public TcpTransport transport;
        public byte[] write_buffer;
        public WriteFinishedFunc write_callback;
        public object write_callback_mutex = new object();
        public object write_mutex = new object();
        public int write_sent, write_size;
        public bool writing;
        public event DisconnectFunc DroppedEvent;

        public void sendHeaderError(ref string error_message)
        {
            IDictionary m = new Hashtable();
            m["error"] = error_message;
            writeHeader(m, onErrorHeaderWritten);
            sendingHeaderError = true;
        }

        public void writeHeader(IDictionary key_vals, WriteFinishedFunc finished_func)
        {
            header_written_callback = finished_func;
            if (!transport.getRequiresHeader())
            {
                onHeaderWritten(this);
                return;
            }
            int len = 0;
            byte[] buffer = null;
            header.Write(key_vals, ref buffer, ref len);
            int msg_len = len + 4;
            byte[] full_msg = new byte[msg_len];
            int j = 0;
            byte[] blen = Header.ByteLength(len);
            for (; j < 4; j++)
                full_msg[j] = blen[j];
            for (int i = 0; j < msg_len; j++)
            {
                i = j - 4;
                full_msg[j] = buffer[i];
            }
            write(full_msg, msg_len, onHeaderWritten, false);
        }

        public void read(int size, ReadFinishedFunc finished_func)
        {
            if (dropped || sendingHeaderError) return;
            lock (read_mutex)
            {
                if (read_callback != null)
                    throw new Exception("NOYOUBLO");
                read_callback = finished_func;
                read_buffer = new byte[size];
                read_size = size;
                read_filled = 0;
                readTransport();
            }
        }

        public void write(byte[] data, int size, WriteFinishedFunc finished_func, bool immediate = true)
        {
            if (dropped || sendingHeaderError) return;
            lock (write_callback_mutex)
            {
                if (write_callback != null)
                    throw new Exception("NOYOUBLO");
                write_callback = finished_func;
                write_buffer = new byte[data.Length];
                Array.Copy(data, write_buffer, data.Length);
                write_size = size;
                transport.enableWrite();
                if (immediate)
                    writeTransport();
            }
        }

        public void drop(DropReason reason)
        {
            bool did_drop = false;
            if (!dropped)
            {
                dropped = true;
                did_drop = true;
                if (DroppedEvent != null)
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

            transport.read_cb += onReadable;
            transport.write_cb += onWriteable;
            transport.disconnect_cb += onDisconnect;

            if (header_func != null)
            {
                read(4, onHeaderLengthRead);
            }
        }

        private void onReadable(TcpTransport trans)
        {
            if (trans != transport) throw new Exception("THAT EVENT IS NOT FOR MEEE!");
            readTransport();
        }

        private void onWriteable(TcpTransport trans)
        {
            if (trans != transport) throw new Exception("THAT EVENT IS NOT FOR MEEE!");
            writeTransport();
        }

        private void onDisconnect(TcpTransport trans)
        {
            if (trans != transport) throw new Exception("THAT EVENT IS NOT FOR MEEE!");
            drop(DropReason.TransportDisconnect);
        }

        private void onHeaderRead(Connection conn, byte[] data, int size, bool success)
        {
            if (conn != this) throw new Exception("THAT EVENT IS NOT FOR MEEE!");
            if (!success)
                return;
            string error_msg = "";
            if (!header.Parse(data, size, ref error_msg))
            {
                drop(DropReason.HeaderError);
            }
            else
            {
                string error_val = "";
                if (header.Values.Contains("error"))
                {
                    error_val = (string) header.Values["error"];
                    Console.WriteLine("Received error message in header for connection to [{0}]: [{1}]", "TCPROS connection to [" + transport.cached_remote_host + "]", error_val);
                    drop(DropReason.HeaderError);
                }
                else
                {
                    if (header_func == null) throw new Exception("AMG YOUR HEADERFUNC SUCKS");
                    transport.parseHeader(header);
                    Console.WriteLine("GOT HEADER!");
                    foreach (object k in header.Values)
                    {
                        string key = (string) k;
                        Console.WriteLine("" + key + " = " + ((string) header.Values[k]));
                    }
                    header_func(conn, header);
                }
            }
        }

        private void onHeaderWritten(Connection conn)
        {
            if (conn != this) throw new Exception("THAT EVENT IS NOT FOR MEEE!");
            if (header_written_callback == null) throw new Exception("NOBODY CARES ABOUT YOU, YOUR CHILDREN (neither present nor future), NOR YOUR GRANDCHILDREN (neither present nor future)");
            header_written_callback(conn);
            header_written_callback = null;
        }

        private void onErrorHeaderWritten(Connection conn)
        {
            drop(DropReason.HeaderError);
        }

        public void setHeaderReceivedCallback(HeaderReceivedFunc func)
        {
            header_func = func;
            if (transport.getRequiresHeader())
                read(4, onHeaderLengthRead);
        }

        private void onHeaderLengthRead(Connection conn, byte[] data, int size, bool success)
        {
            if (conn != this) throw new Exception("THAT EVENT IS NOT FOR MEEE!");
            if (size != 4) throw new Exception("THAT SIZE ISN'T 4! SDKJSDLKJHSDLKJSHD");
            if (!success) return;
            int len = BitConverter.ToInt32(data, 0);
            if (len > 1000000000)
            {
                conn.drop(DropReason.HeaderError);
            }
            read(len, onHeaderRead);
        }

        private void readTransport()
        {
            if (dropped || reading) return;
            lock (read_mutex)
            {
                reading = true;
                while (!dropped && read_callback != null)
                {
                    if (read_buffer == null || read_callback == null)
                        throw new Exception("YOU SUCK!");
                    int to_read = read_size - read_filled;
                    if (to_read > 0)
                    {
                        int bytes_read = transport.read(ref read_buffer, read_filled, to_read);
                        if (dropped)
                            return;
                        else if (bytes_read < 0)
                        {
                            ReadFinishedFunc callback = read_callback;
                            read_callback = null;
                            read_buffer = null;
                            int size = read_size;
                            read_size = 0;
                            read_filled = 0;
                            callback(this, read_buffer, size, false);
                            break;
                        }
                        read_filled += bytes_read;
                    }
                    else
                        break;
                    if (read_filled == read_size && !dropped)
                    {
                        ReadFinishedFunc callback = read_callback;
                        int size = read_size;
                        byte[] buffer = new byte[read_buffer.Length];
                        Array.Copy(read_buffer, buffer, buffer.Length);
                        read_buffer = null;
                        read_callback = null;
                        read_size = 0;
                        callback(this, buffer, size, true);
                    }
                    else break;
                }
                if (read_callback == null)
                    transport.disableRead();
                reading = false;
            }
        }

        private void writeTransport()
        {
            if (dropped || writing) return;
            lock (write_mutex)
            {
                writing = true;
                bool can_write_more = true;
                while (write_callback != null && can_write_more && !dropped)
                {
                    int to_write = write_size - write_sent;
                    int bytes_sent = transport.write(write_buffer, write_sent, to_write);
                    if (bytes_sent <= 0)
                    {
                        writing = false;
                        return;
                    }
                    write_sent += bytes_sent;
                    if (bytes_sent < write_size - write_sent)
                        can_write_more = false;
                    if (write_sent == write_size && !dropped)
                    {
                        WriteFinishedFunc callback;
                        lock (write_callback_mutex)
                        {
                            callback = write_callback;
                            write_callback = null;
                            write_buffer = null;
                            write_sent = 0;
                            write_size = 0;
                        }
                        callback(this);
                    }
                }
                writing = false;
            }
        }
    }

    public delegate void ConnectFunc(Connection connection);

    public delegate void DisconnectFunc(Connection connection, Connection.DropReason reason);

    public delegate bool HeaderReceivedFunc(Connection connection, Header header);

    public delegate void WriteFinishedFunc(Connection connection);

    public delegate void ReadFinishedFunc(Connection connection, byte[] data, int size, bool success);
}