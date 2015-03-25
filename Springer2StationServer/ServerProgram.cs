//
/*   Server Program    */

using System;
using System.Text;
using System.Net;
using System.Net.Sockets;

public class serv
{
    public static void Main()
    {
        try
        {
            IPAddress ipAd = IPAddress.Parse("141.163.186.217");
            // use local m/c IP address, and 
            // use the same in the client

            /* Initializes the Listener */
            TcpListener myList = new TcpListener(ipAd, 23);

            /* Start Listeneting at the specified port */
            myList.Start();

            Console.WriteLine("The server is running at port 8001...");
            Console.WriteLine("The local End point is  :" +
                              myList.LocalEndpoint);
            Console.WriteLine("Waiting for a connection.....");

            Socket clientConnection = null;

            do
            {
                while (!Console.KeyAvailable)
                {
                    if (myList.Pending())
                    {
                        clientConnection = myList.AcceptSocket();
                        Console.WriteLine("Connection accepted from " + clientConnection.RemoteEndPoint);

                        byte[] b = new byte[100];
                        int k = clientConnection.Receive(b);
                        Console.WriteLine("Received a connection request...");
                        for (int i = 0; i < k; i++)
                            Console.Write(Convert.ToChar(b[i]));

                        ASCIIEncoding asen = new ASCIIEncoding();
                        clientConnection.Send(asen.GetBytes("The string was recieved by the server."));
                        Console.WriteLine("\nSent Acknowledgement");

                    }
                }
            } while (Console.ReadKey(true).Key != ConsoleKey.Escape);
                    
            /* clean up */
            clientConnection.Close();
            myList.Stop();

        }
        catch (Exception e)
        {
            Console.WriteLine("Error..... " + e.StackTrace);
        }
    }

}