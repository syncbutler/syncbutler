using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SyncButler;
using System.Collections;
using System.Threading;

namespace TestUI
{
    class Program
    {
        static Controller controller;
        public static void Main(String[] args)
        {
            try
            {
                controller = new Controller();
                controller.AddPartnership(@"C:\test", @"D:\test");
                PrintMRU();
                controller.AddPartnership(@"C:\test", @"D:\test\tesets");
                Console.Out.WriteLine("Before");
                PrintPartnerships();
                Console.Out.WriteLine("After");
                controller.DeletePartnership(0);
                PrintPartnerships();
                Thread updateThread = new Thread(new ThreadStart(updateMonitor));
                updateThread.Start();
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex);
            }
        }
        private static void PrintMRU()
        {
            foreach (KeyValuePair<String,String> kvp in controller.GetMonitoredFiles())
            {
                Console.Out.WriteLine(kvp.Key + " : " + kvp.Value);
            }
        }
        
        private static void PrintPartnerships()
        {
            foreach (Partnership pt in controller.GetPartnershipList())
            {
                Console.Out.WriteLine(pt.ToString());
            }
        }

        private static void updateMonitor()
        {
            while (true)
            {
                PrintMRU();
                Thread.Sleep(5000);
            }
        }
    }
}
