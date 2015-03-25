// **************************************************************
// The SLAM Server - FOR TESTING PURPOSES ONLY!!!
//
//              George Terzakis
//               Plymouth University 2014

/*   Server Program    */
using System.Collections.Generic;
using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Linq;

namespace Springer2StationServer
{

    class Program
    {
        [STAThread]
        public static void Main(String[] args)
        {
            StationServer srv = new StationServer(null, 23);
            srv.startServer();
        }
    }

}