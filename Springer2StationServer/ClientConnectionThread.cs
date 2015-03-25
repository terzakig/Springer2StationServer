// **************************************************************
// A client thread connection to the SLAM server
//
//              George Terzakis
//               Plymouth University 2012-2013


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net.Sockets;


namespace Springer2StationServer
{
    class ClientConnectionThread
    {
        //////////////////////////// message character constants //////////////////////////////////////
        public const char MSG_STARTER = '@';     // message starter
        public const char MSG_TERMINATOR = '#';  // message terminator



        public Socket ClientSocket;

        public Boolean Active, InThreadLoop;

        public Thread thread;

        // some global variables for data reception 
        private Boolean Data_RemainingBytes; // to be used as global by the processData function
        private string Command_Str;          // the command string currently being received
        private int ByteCounter;             // a byte counter for overflow cases (i.e., terminator has not been found)
        private const int Data_Sequence_Reset_Limit = 30; // following 30 characters, a reset is enforced in the command reception autiomaton

        // a rolling buffer for the socket's incoming data
        private RollingBuffer SocketBuffer;


        // a rolling queue for the incoming commands
        public CommandQueue Commands;

        // a GPS receiver for this client connection
        public GPSReceiver GpsReceiver;

        // client connection constructor
        public ClientConnectionThread(Socket sock, GPSReceiver gps)
        {
            // let the thread be inactive for the moment...
            Active = false;

            ClientSocket = sock;

            SocketBuffer = new RollingBuffer();

            Commands = new CommandQueue();

            GpsReceiver = gps;
        }

        // client connection constructor #2
        public ClientConnectionThread(Socket sock)
        {
            // let the thread be inactive for the moment...
            Active = false;

            ClientSocket = sock;

            SocketBuffer = new RollingBuffer();

            Commands = new CommandQueue();

            GpsReceiver = null;
        }

        public void start()
        {
            // initializing data related variables
            Data_RemainingBytes = false;
            SocketBuffer.clear();
            Commands.clear();

            // raising the active flag before starting the thread
            Active = true;

            // creating and starting the thread
            thread = new Thread(mainLoop);

            thread.Start();
        }


        // stop the thread
        public void stop()
        {
            Active = false;
            // wait until the main loop has exited
            while (InThreadLoop) ;

            // kill the thread
            thread.Abort();
        }


        // The main loop of the client connection
        public void mainLoop()
        {
            InThreadLoop = true;
            while (Active)
            {
                updateBuffer();
                processData();
                processCommands();

            }
            // signify that code gas exited main loop 
            InThreadLoop = false;
        }




        // a function to update the rolling buffer of incoming data from the socket
        public void updateBuffer()
        {
            int bytesAvailable = ClientSocket.Available;
            if (bytesAvailable > 0)
            {
                byte[] buf = new byte[bytesAvailable];

                ClientSocket.Receive(buf, bytesAvailable, SocketFlags.None);

                SocketBuffer.addRange(buf);
            }

        }


        // the following wraps the incoming data into messages
        private void processData()
        {
            int bytesAvailable = SocketBuffer.bytesAvailable();

            do
            {
                if (!Data_RemainingBytes)
                {// i.e., the previous message is complete (or we are commencing reception now)
                    if (bytesAvailable >= 1)
                    { // we have -at least-the message starter in the buffer. 

                        byte ch = (byte)SocketBuffer.removeByte();

                        if (ch == MSG_STARTER)
                        {
                            Data_RemainingBytes = true; // found a starter character. expecting to see a terminator now
                            ByteCounter = 0;            // resetting the byte counter
                            Command_Str = "";           // clearing the command string being received
                        }

                    }
                }
                else
                {
                    // starting tempstring             
                    byte ch = 0;
                    while ((!SocketBuffer.isEmpty()) && (ch != (byte)'#'))
                    {
                        ch = (byte)SocketBuffer.removeByte();
                        if (ch == (byte)'#')
                        {
                            Data_RemainingBytes = false;
                            // adding the command in the queue
                            Commands.addCommand(Command_Str);
                        }
                        else
                        {
                            ByteCounter++;
                            Command_Str += (char)ch;

                        }
                        if (ByteCounter == Data_Sequence_Reset_Limit)
                        {
                            Data_RemainingBytes = false;
                            break;
                        }
                    }

                }

                // updating the number of bytes left in the buffer
                bytesAvailable = SocketBuffer.bytesAvailable();

            } while ((bytesAvailable > 0) && (Data_RemainingBytes));

        }


        // processing commands
        private void processCommands()
        {
            string cmd = null;
            if (!Commands.isEmpty())
            {
                cmd = Commands.removeCommand();

                ASCIIEncoding asen = new ASCIIEncoding();

                switch (cmd)
                {
                    case "STARTREF": // use the current camera pose as reference frame
                        Console.WriteLine("Reference frame requested");
                        string ack = "@ACK#";
                        ClientSocket.Send(asen.GetBytes(ack));
                        break;
                    case "RIGID":    // returns the rigid transformation of the current camera pose to the reference frame
                        /////////////////////////////// sending virtual data for testing ///////////////////
                        string trans = "@1,2,3,0.1,0.8,0,-0.8,0.1,0,0,0,1#";
                        // transmitting the reply

                        ClientSocket.Send(asen.GetBytes(trans));
                        break;
                    case "GPSSOG":   // GPS Speed over ground

                        string spov = "@NULL#";
                        if (GpsReceiver != null) 
                            if (GpsReceiver.CommPort.IsOpen)
                                if (GpsReceiver.MRCStatus) spov = "@V" + Convert.ToString(GpsReceiver.SpeedOverGround) + "#";
                                else 
                                    spov = Convert.ToString("@I"+GpsReceiver.SpeedOverGround)+"#";
                        // returning Spurious data again
                        // string spov = "@-3.145#";
                        ClientSocket.Send(asen.GetBytes(spov));
                        break;
                    case "GPSLONG":  // GPS Longitude
                        string Longitude = "@NULL#";
                        if (GpsReceiver != null)
                            if (GpsReceiver.CommPort.IsOpen)
                                if (GpsReceiver.MRCStatus) Longitude = "@V" + GpsReceiver.getLongitudeAsString() + "#";
                                else
                                    Longitude = "@I" + GpsReceiver.getLongitudeAsString() + "#";
                        
                        //string Longitude = "@+50.17#";
                        ClientSocket.Send(asen.GetBytes(Longitude));
                        break;
                    case "GPSLAT":  // GPS Latitude
                        string Latitude = "@NULL#";
                        if (GpsReceiver != null)
                            if (GpsReceiver.CommPort.IsOpen)
                                if (GpsReceiver.MRCStatus) Latitude = "@V" + GpsReceiver.getLatitudeAsString()+"#";
                                else
                                    Latitude = "@I" + GpsReceiver.getLatitudeAsString()+"#";
                        
                        //string Latitude = "@-2.13#";
                        ClientSocket.Send(asen.GetBytes(Latitude));
                        break;
                    default:        // unknown command
                        string reply = "@UNKNOWN:" + cmd + "#";
                        ClientSocket.Send(asen.GetBytes(reply));
                        break;
                }
            }
        }


        // a function to close the connection. Essentially destroy the entire class, since socket will not be resuable
        public void disconnect()
        {
            if (ClientSocket.Connected) ClientSocket.Close();
        }

    }

   }
