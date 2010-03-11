using System;
using System.Threading;
using System.IO;
using System.IO.Pipes;
using System.Xml.Serialization;

namespace SyncButler
{
    public class SingleInstance
    {
        // Fields required for named pipes
        private static string pipeName = "SynButlerIPC"; // Eventually load this from settings?
        private static NamedPipeServerStream pipeServer = null;
        private static IAsyncResult listeningSession = null;
        private static bool closingPipe = false;

        // Fields for mutexes -- used for instance detection
        private static Mutex m_Mutex = null;
        public delegate void ReceiveDelegate(string[] args); //acts as a storage for incoming data

        // Stores the delegate which will be called when data arrives from a new instance of SyncButler
        static private ReceiveDelegate m_Receive = null;
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
            string m_UniqueIdentifier;
            string assemblyName = System.Reflection.Assembly.GetExecutingAssembly().GetName(false).CodeBase;
            m_UniqueIdentifier = assemblyName.Replace("\\", "_");
            
            m_Mutex = new Mutex(false, m_UniqueIdentifier);

            if (m_Mutex.WaitOne(1, true))
            {
                //Managed to lock. This is the first instance.
                CreateInstanceChannel();
                return true;
            }
            else
            {
                //Not the first instance!!!
                m_Mutex.Close();
                m_Mutex = null;
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
            MemoryStream mbuf = new MemoryStream();

            try
            {
                // This may happen if the pipe is closing
                if (pipeServer == null) return;
                
                pipeServer.EndWaitForConnection(result);

                // Read one message off the pipe
                byte[] buf = new byte[256];
                do mbuf.Write(buf, 0, pipeServer.Read(buf, 0, buf.Length));
                while (!pipeServer.IsMessageComplete);

                if (mbuf.Length == 0) throw new IOException();
                // Reset the position so that we can reuse this stream for reading
                mbuf.Position = 0;
            }
            catch (IOException e)
            {
                // The pipe was broken
                CreateInstanceChannel();
                return;
            }
            catch (ObjectDisposedException e)
            {
                // The pipe was closed
                CreateInstanceChannel();
                return;
            }

            CreateInstanceChannel();

            // Now it's time to unserialize...
            XmlSerializer serializer = new XmlSerializer(typeof(string[]));
            string[] data;

            try
            {
                data = (string[]) serializer.Deserialize(mbuf);
            }
            catch (Exception e)
            {
                Controller.LogMessage("Message received over named pipe, but deserialization failed: " + e.Message);
                return;
            }

            if (m_Receive != null)
            {
                m_Receive(data);
            }
        }

        /// <summary>
        /// Cleans up the mutex and pipes
        /// </summary>
        public static void Cleanup()
        {
            if (m_Mutex != null)
            {
                m_Mutex.Close();
            }

            if (pipeServer != null) 
            {
                closingPipe = true;

                try
                {
                    pipeServer.Close();
                }
                catch (IOException e)
                {
                    // The pipe connection got broken. Can ignore.
                }
                catch (ObjectDisposedException e)
                {
                    // The pipe is closed. Can ignore
                }
            }

            pipeServer = null;
            m_Mutex = null;
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
            catch (TimeoutException e)
            {
                Controller.LogMessage("Timeout trying to connect to the named pipe!");
            }
            catch (IOException e)
            {
                // broken pipe?
            }
            catch (ObjectDisposedException e)
            {
                // object closed at other end?
            }
            finally
            {
                pipeClient.Close();
            }
        }
    }
}
