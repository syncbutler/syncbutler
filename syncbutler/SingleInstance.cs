/*****************************************************************************/
// Copyright 2010 Sync Butler and its original developers.
// This file is part of Sync Butler (http://www.syncbutler.org).
// 
// Sync Butler is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Sync Butler is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Sync Butler.  If not, see <http://www.gnu.org/licenses/>.
//
/*****************************************************************************/

// Adapted from http://www.codeproject.com/KB/threads/SingletonApp.aspx
using System;
using System.Threading;
using System.IO;
using System.IO.Pipes;
using System.Xml.Serialization;

namespace SyncButler
{
    /// <summary>
    /// Provides methods to set up a single instance and check for existing instances
    /// </summary>
    public class SingleInstance
    {
        // Fields required for named pipes
        private static string uniqueIdentifier;
        private static string pipeName = "SyncButlerIPC"; // Eventually load this from settings?
        private static NamedPipeServerStream pipeServer;
        private static IAsyncResult listeningSession;
        private static bool closingPipe;

        // Fields for mutexes -- used for instance detection
        private static Mutex mutex;
        public delegate void ReceiveDelegate(string[] args); //acts as a storage for incoming data

        // Stores the delegate which will be called when data arrives from a new instance of SyncButler
        static private ReceiveDelegate m_Receive;
        static public ReceiveDelegate Receiver
        {
            get
            {
                return m_Receive;
            }
            set
            {
                m_Receive = value;
            }
        }

        /// <summary>
        /// Indicates whether this is the first running instance, and sets the receive delegate
        /// if this is indeed the first running isntance.
        /// </summary>
        /// <param name="r">The delegate which will receive the parameters from new instances</param>
        /// <returns>A boolean value indication whether this is the first running instance of the program</returns>
        public static bool IsFirst(ReceiveDelegate r)
        {
            if (IsFirst())
            {
                Receiver += r;
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Indicates whether this is the first running instance
        /// </summary>
        /// <returns>A boolean value indication whether this is the first running instance of the program</returns>
        public static bool IsFirst()
        {
            string assemblyName = System.Reflection.Assembly.GetExecutingAssembly().GetName(false).CodeBase;
            uniqueIdentifier = assemblyName.Replace("/", "_");
            
            mutex = new Mutex(false, uniqueIdentifier);

            if (mutex.WaitOne(1, true))
            {
                //Managed to lock. This is the first instance.
                CreateInstanceChannel();
                return true;
            }
            else
            {
                //Not the first instance!!!
                mutex.Close();
                mutex = null;
                return false;
            }
        }

        /// <summary>
        /// Creates a named pipe and starts listening for information pushed by new instances
        /// </summary>
        private static void CreateInstanceChannel()
        {
            if (closingPipe) return;

            closingPipe = false;
            if (pipeServer != null) pipeServer.Close();
            pipeServer = new NamedPipeServerStream(pipeName, PipeDirection.In, 1, PipeTransmissionMode.Message, PipeOptions.Asynchronous);
            listeningSession = pipeServer.BeginWaitForConnection(NewConnection, null);
        }

        /// <summary>
        /// This method is called whenever there is a new connection, ie. whenever a new instance
        /// of the program is trying to send data to the first instance.
        /// </summary>
        /// <param name="result"></param>
        private static void NewConnection(IAsyncResult result)
        {
            using (MemoryStream mbuf = new MemoryStream())
            {
                try
                {
                    // This may happen if the pipe is closing
                    if (pipeServer == null) return;

                    pipeServer.EndWaitForConnection(result);

                    // Read one message off the pipe
                    byte[] buf = new byte[256];
                    do mbuf.Write(buf, 0, pipeServer.Read(buf, 0, buf.Length));
                    while (!pipeServer.IsMessageComplete);

                    if (mbuf.Length == 0)
                    {
                        Logging.Logger.GetInstance().WARNING("Attempted to read from pipe, but the pipe was empty");
                        throw new IOException();
                    }
                    // Reset the position so that we can reuse this stream for reading
                    mbuf.Position = 0;
                }
                catch (IOException)
                {
                    // The pipe was broken
                    Logging.Logger.GetInstance().WARNING("Attempted to read from pipe, but the pipe was broken");
                    CreateInstanceChannel();
                    return;
                }
                catch (ObjectDisposedException)
                {
                    // The pipe was closed
                    Logging.Logger.GetInstance().WARNING("Attempted to read from pipe, but the pipe was closed");
                    CreateInstanceChannel();
                    return;
                }

                CreateInstanceChannel();

                // Now it's time to unserialize...
                XmlSerializer serializer = new XmlSerializer(typeof(string[]));
                string[] data;

                try
                {
                    data = (string[])serializer.Deserialize(mbuf);
                }
                catch (Exception e)
                {
                    Logging.Logger.GetInstance().FATAL("Message received over named pipe, but deserialization failed: " + e.Message);
                    return;
                }

                if (m_Receive != null)
                {
                    m_Receive(data);
                }
            }
        }

        /// <summary>
        /// Cleans up the mutex and pipes
        /// </summary>
        public static void Cleanup()
        {
            if (mutex != null)
            {
                mutex.Close();
            }

            if (pipeServer != null) 
            {
                closingPipe = true;

                try
                {
                    pipeServer.Close();
                }
                catch (IOException)
                {
                    // The pipe connection got broken. Can ignore.
                }
                catch (ObjectDisposedException)
                {
                    // The pipe is closed. Can ignore
                }
            }

            pipeServer = null;
            mutex = null;
        }

        /// <summary>
        /// Sends a string array to the first instance of the program
        /// </summary>
        /// <param name="s">The string array to send</param>
        public static void Send(string[] s)
        {
            // Serialise the string array
            XmlSerializer serializer = new XmlSerializer(typeof(string[]));
            MemoryStream rawData = new MemoryStream();
            serializer.Serialize(rawData, s);
            byte[] buf = rawData.ToArray();

            NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", pipeName, PipeDirection.Out);

            try
            {
                pipeClient.Connect(5000);
                pipeClient.Write(buf, 0, buf.Length);
            }
            catch (TimeoutException)
            {
                Logging.Logger.GetInstance().FATAL("Timeout trying to connect to the named pipe!");
            }
            catch (IOException e)
            {
                Logging.Logger.GetInstance().FATAL("SingleInstance.Send() : " + e.Message);
            }
            catch (ObjectDisposedException e)
            {
                Logging.Logger.GetInstance().FATAL("SingleInstance.Send() : " + e.Message);
            }
            finally
            {
                pipeClient.Close();
            }
        }
    }
}
