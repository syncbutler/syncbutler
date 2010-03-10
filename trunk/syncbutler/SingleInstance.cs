// Adapted from http://www.codeproject.com/KB/threads/SingletonApp.aspx

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Net.Sockets;

namespace SyncButler
{
    [Serializable]
    class SingleInstance : MarshalByRefObject
    {
        private static TcpChannel m_TCPChannel = null;
        private static Mutex m_Mutex = null;
        private static int portNumber = 1231; //Get from settings
        public delegate void ReceiveDelegate(string[] args); //acts as a storage for incoming data

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

        private static void CreateInstanceChannel()
        {
            int[] portNumbers = new int[10] {1231,1232,1233,1234,1235,1236,1237,1238,1239,1240}; // may be replaced by a function to generate port numbers
            for (int i = 0; i < 10; i++)
            {
                try
                {
                    portNumber = portNumbers[i]; //need to store this into settings file
                    m_TCPChannel = new TcpChannel(portNumber);
                }
                catch (SocketException se)
                {
                    Console.Out.WriteLine(se + " exception while trying to bind to port " + portNumbers[i]);
                    Console.Out.WriteLine("Trying next port...");
                }
            }
            ChannelServices.RegisterChannel(m_TCPChannel, false);
            RemotingConfiguration.RegisterWellKnownServiceType(
                Type.GetType("SyncButler.SingleInstance"),
                "SingleInstance",
                WellKnownObjectMode.SingleCall);
        }

        public static void Cleanup()
        {
            if (m_Mutex != null)
            {
                m_Mutex.Close();
            }

            if (m_TCPChannel != null)
            {
                m_TCPChannel.StopListening(null);
            }

            m_Mutex = null;
            m_TCPChannel = null;
        }

        public static void Send(string[] s)
        {
            SingleInstance ctrl;
            TcpChannel channel = new TcpChannel();
            ChannelServices.RegisterChannel(channel, false);
            try
            {
                ctrl = (SingleInstance)Activator.GetObject(typeof(SingleInstance), "tcp://localhost:" + portNumber + "/SingleInstance");
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: " + e.Message);
                throw;
            }
            ctrl.Receive(s);
        }

        public void Receive(string[] s)
        {
            if (m_Receive != null)
            {
                m_Receive(s);
            }
        }
    }
}
