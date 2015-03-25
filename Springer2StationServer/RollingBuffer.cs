// **************************************************************
// A rolling buffer for stuff that's received over the network
//
//              George Terzakis
//               Plymouth University 2011


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Springer2StationServer
{
    class RollingBuffer
    {
        // constants
        public int BUFFER_SIZE = 10000;


        // members
        private byte[] buffer;
        private int FirstByteIndex, LastByteIndex;
        private int BufferLength;


        // constructor
        public RollingBuffer()
        {
            BufferLength = BUFFER_SIZE;

            buffer = new byte[BufferLength];

            clear();
        }

        // constructor #2
        public RollingBuffer(int maxlen)
        {
            BufferLength = maxlen;
            buffer = new byte[BufferLength];

            clear();
        }


        public int bytesAvailable()
        {
            int bytesAvail = 0;
            if ((FirstByteIndex == -1) || (LastByteIndex == -1)) bytesAvail = 0;
            else if (FirstByteIndex < LastByteIndex)
                bytesAvail = LastByteIndex - FirstByteIndex + 1;
            else if (FirstByteIndex > LastByteIndex)
                bytesAvail = BufferLength - FirstByteIndex + LastByteIndex + 1;
            else if (FirstByteIndex == LastByteIndex)
                bytesAvail = 1;

            return bytesAvail;
        }


        
        public Boolean isEmpty()
        {
            return (bytesAvailable() == 0);
        }

        public Boolean isFull()
        {
            return (bytesAvailable() == BufferLength);
        }

        
        // advance last character index
        public void advanceLastIndex()
        {
            LastByteIndex = (LastByteIndex + 1) % BufferLength;
        }

        // advance first character index
        public void advanceFirstIndex()
        {
            FirstByteIndex = (FirstByteIndex + 1) % BufferLength;
        }

        // clear the buffer
        public void clear()
        {
            FirstByteIndex = LastByteIndex = -1;
        }



        // add byte to the buffer
        public void addByte(byte ch)
        {

            if (isEmpty())
            {
                FirstByteIndex = LastByteIndex = 0;

            }
            else if (isFull())
            {
                advanceFirstIndex();
                advanceLastIndex();

            }
            else
                advanceLastIndex();

            buffer[LastByteIndex] = ch;

        }

        // add a range of bytes
        public void addRange(byte[] data)
        {
            int i;
            int len = data.Length;
            for (i = 0; i < len; i++) 
                addByte(data[i]);
        }

        // remove a byte from the buffer
        public int removeByte()
        {
            int ch = -1;
            if (!isEmpty())
            {
                ch = buffer[FirstByteIndex];
                if (FirstByteIndex == LastByteIndex) 
                    FirstByteIndex = LastByteIndex = -1;
                else 
                    advanceFirstIndex();
            }

            return ch;
        }
                


        


    }
}
