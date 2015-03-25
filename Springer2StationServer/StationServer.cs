using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Net;
using System.Net.Sockets;
using System.Linq;

using System.Threading;


namespace Springer2StationServer
{
    class StationServer
    {
        // Ip address (TCPIP v4)
        public IPAddress IpAd;

        // the TCPIP port
        public int TCPIPPort;

        // the client connection socket
        public Socket ClientConnection;


        // the TCP listenenr
        public TcpListener MyList;

        // list opf client connections
        public List<ClientConnectionThread> Clients;

        // the GPS rteceiver of the server (provides GPS upon demand)
        public GPSReceiver GpsReceiver;


        // constructor #1 (Detects the IP address of the local host)
        public StationServer(GPSReceiver gpsreceiver, int port)
        {

            TCPIPPort = port;
            GpsReceiver = gpsreceiver;

            // retrieve the IP address automatically
            string hostName = Dns.GetHostName();
            IPAddress[] hostIPAddresses = Dns.GetHostAddresses(hostName);

            Boolean found = false;
            int i = 0;
            while (!found)
            {
                IpAd = hostIPAddresses[i];
                if (IpAd.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork) found = true;
                else i++;
            }


        }


        public void startServer()
        {
            int i;

            try
            {

                // start the GpsReceiver
                if (GpsReceiver != null) GpsReceiver.StartReceiver();

                // create the client copnnection list
                Clients = new List<ClientConnectionThread>();

                string hostName = Dns.GetHostName();


                /* Initializes the Listener */
                MyList = new TcpListener(IpAd, TCPIPPort);

                /* Start Listeneting at the specified port */
                MyList.Start();

                Console.WriteLine("The server is running at port " + Convert.ToString(TCPIPPort) + "...");
                Console.WriteLine("The local End point is  :" + MyList.LocalEndpoint);
                Console.WriteLine("Waiting for a connection.....");

                // the list of Client connecxtions
                List<ClientConnectionThread> clients = new List<ClientConnectionThread>();


                ClientConnection = null;

                do
                {
                    while (!Console.KeyAvailable)
                    {
                        if (MyList.Pending())
                        {
                            ClientConnection = MyList.AcceptSocket();
                            Console.WriteLine("Connection accepted from " + ClientConnection.RemoteEndPoint);

                            // creating ans starting a new client thread
                            ClientConnectionThread client = new ClientConnectionThread(ClientConnection, GpsReceiver);
                            // adding it to the list
                            clients.Add(client);
                            // starting the thread
                            client.start();

                        }
                    }
                } while (Console.ReadKey(true).Key != ConsoleKey.Escape);

                /* clean up */

                // killing all the client threads
                int numClients = clients.Count();
                for (i = 0; i < numClients; i++)
                    clients[i].stop();

                MyList.Stop();

                if (GpsReceiver != null) GpsReceiver.stopReceiver();
            }
            catch (Exception e)
            {
                Console.WriteLine("Error..... " + e.StackTrace);
                // kill the Gps receiver if active
                if (GpsReceiver != null) GpsReceiver.stopReceiver();
                Console.ReadKey();
                Environment.Exit(0);

            }
        }


    }

}
