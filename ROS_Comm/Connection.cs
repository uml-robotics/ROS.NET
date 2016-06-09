// File: Connection.cs
// Project: ROS_C-Sharp
// 
// ROS.NET
// Eric McCann <emccann@cs.uml.edu>
// UMass Lowell Robotics Laboratory
// 
// Reimplementation of the ROS (ros.org) ros_cpp client in C#.
// 
// Created: 04/28/2015
// Updated: 02/10/2016

#define BEGIN_INVOKE

#region USINGZ

using System;
using System.Collections;
using System.Threading;

#endregion

namespace Ros_CSharp
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

        public string RemoteString;
        public object drop_mutex = new object();
        public bool dropped;
        public Header header = new Header();
        public HeaderReceivedFunc header_func;
        public WriteFinishedFunc header_written_callback;
        public bool is_server;
        private byte[] length_buffer = new byte[4];
        public byte[] read_buffer;
        public ReadFinishedFunc read_callback;
        private object read_callback_mutex = new object();
        public int read_filled;
        public int read_size;
        private byte[] real_read_buffer;
        public bool sendingHeaderError;
        public TcpTransport transport;
        public byte[] write_buffer;
        public WriteFinishedFunc write_callback;
        public object write_callback_mutex = new object();
        public int write_sent, write_size;
        private object reading = new object(), writing = new object();

        /// <summary>
        ///     Returns the ID of the connection
        /// </summary>
        public string CallerID
        {
            get
            {
                if (header != null && header.Values.Contains("callerid"))
                    return (string) header.Values["callerid"];
                return "";
            }
        }

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
            int msg_len = (int) len + 4;
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
            write(full_msg, msg_len, onHeaderWritten, true);
        }

        public void read(int size, ReadFinishedFunc finished_func)
        {
            if (dropped || sendingHeaderError) return;
            lock (read_callback_mutex)
            {
                if (read_callback != null)
                    throw new Exception("NOYOUBLO");
                read_callback = finished_func;
            }
            if (size == 4)
                read_buffer = length_buffer;
            else
            {
                if (real_read_buffer == null || real_read_buffer.Length != size)
                    real_read_buffer = new byte[size];
                read_buffer = real_read_buffer;
            }
            read_size = size;
            read_filled = 0;
            transport.enableRead();
            readTransport();
        }

        public void write(byte[] data, int size, WriteFinishedFunc finished_func)
        {
            write(data, size, finished_func, true);
        }

        public void write(byte[] data, int size, WriteFinishedFunc finished_func, bool immediate)
        {
            if (dropped || sendingHeaderError) return;
            lock (write_callback_mutex)
            {
                if (write_callback != null)
                    writeTransport();
                if (write_callback != null)
                    throw new Exception("Not finished writing previous data on this connection");
                write_callback = finished_func;
                write_buffer = data;
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

            if (this.header_func != null)
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

        private bool onHeaderRead(Connection conn, byte[] data, int size, bool success)
        {
            if (conn != this) throw new Exception("THAT EVENT IS NOT FOR MEEE!");
            if (!success)
            {
                return false;
            }
            string error_msg = "";
            if (!header.Parse(data, (int) size, ref error_msg))
            {
                drop(DropReason.HeaderError);
                return false;
            }
            else
            {
                string error_val = "";
                if (header.Values.Contains("error"))
                {
                    error_val = (string) header.Values["error"];
                    EDB.WriteLine("Received error message in header for connection to [{0}]: [{1}]",
                        "TCPROS connection to [" + transport.cached_remote_host + "]", error_val);
                    drop(DropReason.HeaderError);
                    return false;
                }
                else
                {
                    if (header_func == null) throw new Exception("AMG YOUR HEADERFUNC SUCKS");
                    transport.parseHeader(header);
                    header_func(conn, header);
                }
            }
            return true;
        }

        private bool onHeaderWritten(Connection conn)
        {
            if (conn != this) throw new Exception("THAT EVENT IS NOT FOR MEEE!");
            if (header_written_callback == null)
                throw new Exception(
                    "NOBODY CARES ABOUT YOU, YOUR CHILDREN (neither present nor future), NOR YOUR GRANDCHILDREN (neither present nor future)");
            header_written_callback(conn);
            header_written_callback = null;
            return true;
        }

        private bool onErrorHeaderWritten(Connection conn)
        {
            drop(DropReason.HeaderError);
            return false;
        }

        public void setHeaderReceivedCallback(HeaderReceivedFunc func)
        {
            header_func = func;
            if (transport.getRequiresHeader())
                read(4, onHeaderLengthRead);
        }

        private bool onHeaderLengthRead(Connection conn, byte[] data, int size, bool success)
        {
            if (conn != this) throw new Exception("THAT EVENT IS NOT FOR MEEE!");
            if (size != 4) throw new Exception("THAT SIZE ISN'T 4! SDKJSDLKJHSDLKJSHD");
            if (!success)
            {
                return false;
            }
            int len = BitConverter.ToInt32(data, 0);
            if (len > 1000000000)
            {
                conn.drop(DropReason.HeaderError);
                return false;
            }
            read(len, onHeaderRead);
            return true;
        }

        private void readTransport()
        {
            lock (reading)
            {
                //EDB.WriteLine("READ - "+transport.poll_set);
                if (dropped) return;
                ScopedTimer.Ping();
                ReadFinishedFunc callback;
                lock (read_callback_mutex)
                    callback = read_callback;
                int size;
                while (!dropped && callback != null)
                {
                    ScopedTimer.Ping();
                    int to_read = read_size - read_filled;
                    if (to_read > 0 && read_buffer == null)
                        throw new Exception("Trying to read "+to_read+" bytes with a null read_buffer!");
                    if (callback == null)
                        lock (read_callback_mutex)
                            callback = read_callback;
                    if (callback == null)
                        throw new Exception("Cannot determine which read_callback to invoke?!");
                    if (to_read > 0)
                    {
                        ScopedTimer.Ping();
                        int bytes_read = transport.read(read_buffer, read_filled, to_read);
                        if (dropped)
                        {
                            if (read_callback == null)
                                transport.disableRead();
                            break;
                        }
                        if (bytes_read < 0)
                        {
                            ScopedTimer.Ping();
                            read_callback = null;
                            byte[] buffer = read_buffer;
                            read_buffer = null;
                            size = read_size;
                            read_size = 0;
                            read_filled = 0;
#if BEGIN_INVOKE
                            callback.BeginInvoke(this, buffer, size, false, readTransportComplete, callback);
#else
                        if (!callback(this, buffer, size, false))
                        {
                            Console.WriteLine("Callbacks invoked by connection errored");
                        }
                        callback = null;
                        lock(read_callback_mutex)
                            if (read_callback==null)
                                transport.disableRead();
#endif
                            break;
                        }
                        lock (read_callback_mutex)
                            callback = read_callback;
                        read_filled += bytes_read;
                    }
                    else
                    {
                        lock (read_callback_mutex)
                            if (read_callback == null)
                                transport.disableRead();
                        break;
                    }
                    if (read_filled == read_size && !dropped)
                    {
                        size = read_size;
                        byte[] buffer = read_buffer;
                        read_buffer = null;
                        lock (read_callback_mutex)
                            read_callback = null;
                        read_size = 0;
#if BEGIN_INVOKE
                        callback.BeginInvoke(this, buffer, size, true, readTransportComplete, callback);
#else
                    if (!callback(this, buffer, size, true))
                    {
                        Console.WriteLine("Callbacks invoked by connection errored");
                    }
                    lock (read_callback_mutex)
                        if (read_callback == null)
                            transport.disableRead();
#endif
                        callback = null;
                    }
                    else
                    {
                        lock (read_callback_mutex)
                            if (read_callback == null)
                                transport.disableRead();
                        break;
                    }
                }
            }
        }

#if BEGIN_INVOKE
        private void readTransportComplete(IAsyncResult iar)
        {
            lock (read_callback_mutex)
            {
                if (!((ReadFinishedFunc)iar.AsyncState).EndInvoke(iar))
                    Console.WriteLine(((ReadFinishedFunc)iar.AsyncState).Method.Name + " FAILED");
                if (read_callback == null)
                    transport.disableRead();
            }
        }
#endif

        private void writeTransport()
        {
            lock (writing)
            {
                if (dropped) return;
                ScopedTimer.Ping();
                bool can_write_more = true;
                while (write_callback != null && can_write_more && !dropped)
                {
                    int to_write = write_size - write_sent;
                    int bytes_sent = transport.write(write_buffer, write_sent, to_write);
                    if (bytes_sent <= 0)
                    {
                        return;
                    }
                    write_sent += (int) bytes_sent;
                    if (bytes_sent < write_size - write_sent)
                        can_write_more = false;
                    if (write_sent == write_size && !dropped)
                    {
                        lock (write_callback_mutex)
                        {
                            WriteFinishedFunc callback = write_callback;
                            write_callback = null;
                            write_buffer = null;
                            write_sent = 0;
                            write_size = 0;
                            if (!callback(this))
                                Console.WriteLine("Failed to invoke " + callback.Method.Name);
                        }
                    }
                }
            }
        }
    }

    public delegate void ConnectFunc(Connection connection);

    public delegate void DisconnectFunc(Connection connection, Connection.DropReason reason);

    public delegate bool HeaderReceivedFunc(Connection connection, Header header);

    public delegate bool WriteFinishedFunc(Connection connection);

    public delegate bool ReadFinishedFunc(Connection connection, byte[] data, int size, bool success);
}