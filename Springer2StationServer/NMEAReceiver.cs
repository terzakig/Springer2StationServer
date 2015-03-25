// **************************************************************
// A receiver class for NMEA format types of messages
//
//              George Terzakis
//               Plymouth University 2011

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Ports;
using System.Threading;

namespace Springer2StationServer
{
    class NMEAReceiver
    {

        // reception state constants
        public const int RS_IDLE = 0;
        public const int RS_SENTENCE_PENDING = 1;
        public const int RS_CHECKSUM_PENDING = 2;

        // some NMEA constants
        public const char NMEA_Starter = '$';
        public const char NMEA_Terminator = '*';
        // maximum sentence size is 82 characters BETWEEN $ and the line feed characters
        public const int MAX_NMEA_LENGTH = 82;
        public const int MAX_BUFFER_SIZE = 100; // 100 characters long sentence buffer
        


        // The process main loop thread
        public Thread NMEAThread; 
        
        // NMEA thread active flag
        public Boolean NMEAThreadActive;

        // The serial Port member
        public SerialPort CommPort;

        // use checksum bytes flag
        public Boolean UseChecksum;

        // sentence queue
        public List<string> SentenceQueue;

        // global variables
        // private Boolean SentencePending; // a flag used by processData indicating that a sentence is incomplete (at the moment)
        private int SentenceByteCount;  // a counter indicating the number of bytes in a sentence received up till now


        // serial receiver state
        private int state;

        // expected Checksum of a message
        private ushort ExpectedChecksum;


        // Checksum error flag and count
        public Boolean FlagChecksumError;
        public int ChecksumErrorCount;

        private byte[] SentenceBuffer;  // a global storage space for the curently pending sentence




        // default class constructor
        public NMEAReceiver()
        {
            CommPort = new SerialPort( "COM1", 4800);

            CommPort.Parity = System.IO.Ports.Parity.None;
            CommPort.StopBits = System.IO.Ports.StopBits.One;
            CommPort.Handshake = System.IO.Ports.Handshake.None;
            CommPort.ReceivedBytesThreshold = 1; // one byte in queue triggers event

            //CommPort.DataReceived += this.SerialDataReceived; // SerialDataReceived becomes the event handler for the case of bytes in queue


            // clear flags-globals
            //SentencePending = false;
            state = RS_IDLE;

            UseChecksum = false;

            FlagChecksumError = false;
            ChecksumErrorCount = 0;

            SentenceByteCount = 0;
            
            SentenceBuffer = new byte[MAX_BUFFER_SIZE];


            SentenceQueue = new List<string>();


            NMEAThreadActive = false;
        }
        
        // class constructor 2
        public NMEAReceiver(string commport, int baudrate, Boolean usechecksum)
        {
            CommPort = new SerialPort(commport, baudrate);

            CommPort.Parity = System.IO.Ports.Parity.None;
            CommPort.StopBits = System.IO.Ports.StopBits.One;
            CommPort.Handshake = System.IO.Ports.Handshake.None;
            CommPort.ReceivedBytesThreshold = 1; // one byte in queue triggers event (not really relevant; event handler is not used)

            // CommPort.DataReceived += this.SerialDataReceived; // SerialDataReceived becomes the event handler for the case of bytes in queue

            //SentencePending = false;
            state = RS_IDLE;

            // Checksum flags and erroer counter
            UseChecksum = usechecksum;
            FlagChecksumError = false;
            ChecksumErrorCount = 0;


            SentenceByteCount = 0;
            SentenceBuffer = new byte[MAX_BUFFER_SIZE];


            SentenceQueue = new List<string>();

            NMEAThreadActive = false;
        }


        // Cecksum calculation
        /*public static ushort calculateCRC(byte[] pData, int numBytes)
        {
            int index = 0;
            ushort crc = 0;

            while (index < numBytes)
            {
                crc = (ushort)((crc >> 8) | (crc << 8));
                crc ^= pData[index++];
                crc ^= (ushort)((crc & 0xFF) >> 4);
                crc ^= (ushort)((crc << 8) << 4);
                crc ^= (ushort)(((crc & 0xFF) << 4) << 1);
            }

            return crc;
        }*/

        public static ushort calcNMEAChecksum(byte[] data, int numBytes)
        {
            int i;
            // assumiong first character is '$' and the last is '*'
            ushort checkDigits = (ushort)data[1];
            for (i = 2; i < numBytes - 1; i++)
                checkDigits ^= data[i];

            return checkDigits;
        }


        public void StartReceiver()
        {
            CommPort.Open();
            NMEAThread = new Thread(this.mainLoop);

            
            // strarting
            NMEAThread.Start();

            NMEAThreadActive = true;
        }
    

        // reset counters, state machine and buffers of the receiver
        public void resetReceiver()
        {
            // clear flags-globals
            //SentencePending = false;
            state = RS_IDLE;
            
            SentenceByteCount = 0;
            
            FlagChecksumError = false;
            ChecksumErrorCount = 0;


            SentenceQueue = new List<string>();
        }

        // Kills the thread of receiver
        public void stopReceiver()
        {
            NMEAThread.Abort();

            NMEAThreadActive = false;
        }



        private void mainLoop() {
            while (true) {
                
                // extract messages from the serial port and place them in a queue
                processData();

                // process events (e.g,., possible new packets, raise/clear flags, etc)
                processEvents();
            }
        }

        // teh following parses the sentence buffer and returns tru if the terminator character is found
        private Boolean containTerminator()
        {
            Boolean found = false;
            int i;
            for (i = 0; i < SentenceByteCount; i++)
                if (SentenceBuffer[i] == NMEA_Terminator)
                {
                    found = true;
                    break;
                }
            return found;
        }


        // processing (potentially) pending characters in the serial port buffer
        private void processData()
        {
            int i;

            int numBytesToRead = CommPort.BytesToRead; // let's see what we 've got...
            
            if (numBytesToRead > 0)
                do
                {
                    if (state == RS_IDLE)
                    { // previous Sentence was fully read (or we just started listening)
                        if (numBytesToRead > 0)
                        { // nead at least the sentence starter
                            // reading first 1 byte 

                            byte firstByte;
                            firstByte = (byte)CommPort.ReadByte();

                            // now cheking for framestarter character mismatch
                            if (firstByte == NMEA_Starter)
                            {
                                SentenceByteCount = 1;
                                SentenceBuffer[0] = (byte)NMEA_Starter;
                                if (numBytesToRead >= 1 + MAX_NMEA_LENGTH + 2)
                                {
                                    // creating a temporary buffer to read the entire sentence
                                    byte[] tempBuffer = new byte[1 + MAX_NMEA_LENGTH + 2];
                                    tempBuffer[0] = (byte)NMEA_Starter;
                                    CommPort.Read(tempBuffer, 1, MAX_NMEA_LENGTH + 2);
                                    if (tempBuffer[MAX_NMEA_LENGTH - 2] == NMEA_Terminator)
                                    { // NMEA valid sentence. Copying to global buffer
                                        i = 0;
                                        do
                                        {
                                            SentenceBuffer[i] = tempBuffer[i];
                                            SentenceByteCount++;
                                            i++;
                                        } while (SentenceBuffer[i - 1] != NMEA_Terminator);
                                    }

                                    if (!UseChecksum) state = RS_IDLE;
                                    else
                                    {
                                        // calculate excpected Checksum
                                        ExpectedChecksum = NMEAReceiver.calcNMEAChecksum(SentenceBuffer, SentenceByteCount);
                                        state = RS_CHECKSUM_PENDING;
                                    }
                                    
                                }
                                else
                                { // reading all the bytes into the global buffer
                                    SentenceBuffer[0] = (byte)NMEA_Starter;
                                    

                                    CommPort.Read(SentenceBuffer, 1, numBytesToRead - 1);

                                    SentenceByteCount += numBytesToRead - 1;
                                    
                                    //SentencePending = true;
                                    state = RS_SENTENCE_PENDING;
                                }
                            }
                            else
                            {
                                // flushing the contents of the serial buffer will occur gradually, byte by byte until we find a starter (or the buffer becomes empty)
                                byte ch;
                                do
                                {
                                    ch = (byte)CommPort.ReadByte();
                                    numBytesToRead = CommPort.BytesToRead;
                                } while ((ch != NMEA_Starter) && (numBytesToRead > 0));

                                if (ch == NMEA_Starter)
                                {
                                    SentenceBuffer[0] = (byte)NMEA_Starter;
                                    SentenceByteCount = 1;
                                    state = RS_SENTENCE_PENDING;
                                }
                                else state = RS_IDLE;
                            }
                        }
                    } else if (state == RS_SENTENCE_PENDING)
                    { // we now read bytes until we find a terminator or the buffer is empty or the buffer is size is exceeded
                        byte ch;
                        do
                        {
                            ch = (byte)CommPort.ReadByte();
                            SentenceBuffer[SentenceByteCount] = ch;
                            SentenceByteCount++;
                            numBytesToRead = CommPort.BytesToRead;
                        } while ((ch != NMEA_Terminator) && (numBytesToRead > 0) && (SentenceByteCount > MAX_BUFFER_SIZE) && (ch!= NMEA_Starter));

                        if ((SentenceByteCount > 1 + MAX_NMEA_LENGTH + 2) || (SentenceByteCount > MAX_BUFFER_SIZE))
                        { // sentence exceeds typical length. error
                            SentenceByteCount = 0;
                            //SentencePending = false;
                            state = UseChecksum ? RS_CHECKSUM_PENDING : RS_IDLE;
                        }
                        else if (ch == NMEA_Terminator)
                        { // sentence is ready. raising flag
                            
                            
                            //SentencePending = false;
                            if (UseChecksum)
                            {
                                ExpectedChecksum = NMEAReceiver.calcNMEAChecksum(SentenceBuffer, SentenceByteCount);
                                state = RS_CHECKSUM_PENDING;
                            } else {
                                string str = "";
     
                                for (i = 0; i < SentenceByteCount; i++)
                                    str = str + (char)SentenceBuffer[i];
                            
                                SentenceQueue.Add(str);
                            
                                SentenceByteCount = 0;
                                state = RS_IDLE;
                            }
                        }
                        else if (ch == NMEA_Starter)
                        { // start again!
                            
                            //SentencePending = true;
                            state = RS_SENTENCE_PENDING;
                            SentenceByteCount = 1;
                        }




                    }
                    else if (state == RS_CHECKSUM_PENDING)
                    { // expecting two more bytes
                        if (numBytesToRead >= 2)
                        {
                            byte ChecksumHighByte = (byte)CommPort.ReadByte();
                            byte ChecksumLowByte = (byte)CommPort.ReadByte();

                            byte HighDigit = ChecksumHighByte >= (byte)'A' ? (byte)(10 + ChecksumHighByte - 'A') : (byte)(ChecksumHighByte - '0');
                            byte LowDigit = ChecksumLowByte >= (byte)'A' ? (byte)(10 + ChecksumLowByte - 'A') : (byte)(ChecksumLowByte - '0');

                            char high = (char)ChecksumHighByte;
                            char low = (char)ChecksumLowByte;

                            ushort ChecksumResult = (ushort)(16 * HighDigit + LowDigit);
                            if (ChecksumResult == ExpectedChecksum)
                            {
                                string str = "";

                                for (i = 0; i < SentenceByteCount; i++)
                                    str = str + (char)SentenceBuffer[i];

                                SentenceQueue.Add(str);

                                SentenceByteCount = 0;
                            }
                            else
                            { // checksum does not match
                                ChecksumErrorCount++; // increase error count
                                // raise the error flag
                                FlagChecksumError = true;
                            }
                            state = RS_IDLE;
                        }

                    }

                    numBytesToRead = CommPort.BytesToRead;
                }
                while (numBytesToRead > 0);

        }


        

        public virtual void processEvents()
        {
            // a lot more can happen!
            if (SentenceQueue.Count > 0)
            {
                Console.WriteLine(SentenceQueue[SentenceQueue.Count - 1]);
                List<string> args = parseNMEASentence(string2Bytes( SentenceQueue[SentenceQueue.Count - 1]));
                SentenceQueue.RemoveAt(SentenceQueue.Count - 1);
                
            }
            // printing checksum error count
            if (FlagChecksumError)
            {
                Console.WriteLine("Checksum Error!");
                Console.WriteLine("Total Checksum Errors so far :", ChecksumErrorCount);
                
                FlagChecksumError = false;
            }
            
        }



        // A couple of initialization messages
        public virtual void resetGPS()
        {
            string HexDigits = "0123456789ABCDEF";
            ushort check;
            string cmd = "$PGRMI,0001.000,N,00100.000,W,,,A*";
            //cmd = "$PGRMI,0100.0000,N,00100.0000,W,031111,,R*";
    
            int len = cmd.Length;
            byte[] msg = new byte[len + 4];

            char[] charCmd = cmd.ToCharArray();
            for (int i = 0; i < len; i++)
                msg[i] = (byte)charCmd[i];

            //cmd = "$PGRMIE";
                check = calcNMEAChecksum(msg, cmd.Length);

                char HighByteChar = HexDigits[(byte)((check >> 8) & 0x0F)];
                char LowByteChar = HexDigits[(byte)(check & 0x0F)];

                msg[len] = (byte)HighByteChar;
                msg[len + 1] = (byte)LowByteChar;
                msg[len + 2] = 10;
                msg[len + 3] = 13;

                CommPort.Write(msg, 0, len + 4);

        
        }


        public static List<string> parseNMEASentence(byte[] sentence)
        {
            string currentWord = "";
            char ch = '$';
            List<string> NMEAwords = new List<string>();
            int i = 1; // skipping the starter
            while ((ch = (char)sentence[i++]) != '*')
            {
                if (ch == ',')
                {
                    NMEAwords.Add(currentWord);
                    currentWord = "";
                }
                else currentWord += ch;
            }

            NMEAwords.Add(currentWord);

            return NMEAwords;
        }




        // convert a string to a series of bytes
        public static byte[] string2Bytes(string msg)
        {
            int len = msg.Length;
            int i;

            byte[] byteMsg = new byte[len];
            for (i = 0; i < len; i++)
                byteMsg[i] = (byte)msg[i];

            return byteMsg;
        }


        // convert a series of bytes to a string
        public static string bytes2String(byte[] byteMsg, int len)
        {
            StringBuilder sb = new StringBuilder("");
            int i;
            for (i = 0; i < len; i++)
                sb.Append(byteMsg[i]);

            string retStr = sb.ToString();

            return retStr;
        }


    }
}
































































/* George Terzakis 2011 */