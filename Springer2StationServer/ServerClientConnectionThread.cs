using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net.Sockets;


namespace ServerTest
{
    class ClientConnectionThread 
    {
        public Socket ClientSocket;
        
        public Boolean Active, InThreadLoop;

        public Thread thread;

        public ClientConnectionThread(Socket sock)
        {
            // let the thread be inactive for the moment...
            Active = false;
           
            ClientSocket = sock;
        }


        public void start()
        {
            thread = new Thread(mainLoop);
        }

        public void mainLoop() {
            InThreadLoop = true;
            while (Active) {
                
            

    }
}
