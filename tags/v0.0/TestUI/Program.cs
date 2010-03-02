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
                MainMenu();
                
                controller.AddPartnership("Test Name",@"C:\test", @"D:\test");
                PrintMRU();
                
                Console.Out.WriteLine("Before");
                PrintPartnerships();
                controller.AddPartnership("Test Name",@"C:\test", @"D:\test\test");

                Console.Out.WriteLine("After");
                controller.DeletePartnership(0);
                PrintPartnerships();

                Console.WriteLine("Had the program ran before? : " + controller.programRanBefore());

                //Thread updateThread = new Thread(new ThreadStart(updateMonitor));
                //updateThread.Start();

                //Write code for shutting down
                controller.Shutdown();
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex);
            }
        }

        private static void MainMenu()
        {
            Console.Out.WriteLine("1) Create a Partnership");
            Console.Out.WriteLine("2) View Partnerships");
            Console.Out.WriteLine("3) Show Recent Files From Registry Entries");
            Console.Out.WriteLine("------------------------------------------");
            Console.Out.WriteLine("Select an action: ");

            bool validSelection = false;
            while (!validSelection)
            {
                String action = Console.In.ReadLine().Trim();
                switch (action)
                {
                    case "1":
                        CreatePartnershipMenu();
                        validSelection = true;
                        break;
                    case "2":
                        PrintPartnerships();
                        validSelection = true;
                        break;
                    case "3":
                        PrintMRU();
                        validSelection = true;
                        break;
                    default:
                        Console.Out.WriteLine("Invalid Command");
                        break;
                }

            }
        }

        private static void CreatePartnershipMenu()
        {
            Console.Out.WriteLine("Creating a Partnership");
            Console.Out.WriteLine("Enter a friendly name for this partnership");
            String name = Console.In.ReadLine();
            Console.Out.WriteLine("Enter Path to 1st Partner:");
            String leftPath = Console.In.ReadLine();
            Console.Out.WriteLine("Enter Path to 2nd Partner:");
            String rightPath = Console.In.ReadLine();
            controller.AddPartnership(name,leftPath, rightPath);
            Console.Out.WriteLine("Partnership Created!");

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
            foreach (Partnership pt in controller.GetPartnershipList().Values)
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
