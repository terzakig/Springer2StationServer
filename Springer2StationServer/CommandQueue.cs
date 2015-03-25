// **************************************************************
// A queue structures for receved commands
//
//              George Terzakis
//               Plymouth University 2012-2013

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Springer2StationServer
{
    class CommandQueue
    {
        // standard maximum length 
        public const int STD_MAX_LENGTH = 100; 
        // the commands
        public string[] Commands;
        // the maximum length of the queue
        public int MaxLength;

        // first and last item indexes
        public int FirstItemIndex, LastItemIndex;


        
        // constructor
        public CommandQueue()
        {
            MaxLength = STD_MAX_LENGTH;

            Commands = new string[MaxLength];

            clear();
        }

        // constructor #2
        public CommandQueue(int maxlen)
        {
            MaxLength = maxlen;
            Commands = new string[MaxLength];

            clear();
        }


        public int bytesAvailable()
        {
            int bytesAvail = 0;
            if ((FirstItemIndex == -1) || (LastItemIndex == -1)) bytesAvail = 0;
            else if (FirstItemIndex < LastItemIndex)
                bytesAvail = LastItemIndex - FirstItemIndex + 1;
            else if (FirstItemIndex > LastItemIndex)
                bytesAvail = MaxLength - FirstItemIndex + LastItemIndex + 1;
            else if (FirstItemIndex == LastItemIndex)
                bytesAvail = 1;

            return bytesAvail;
        }


        
        public Boolean isEmpty()
        {
            return (bytesAvailable() == 0);
        }

        public Boolean isFull()
        {
            return (bytesAvailable() == MaxLength);
        }

        
        // advance last character index
        public void advanceLastIndex()
        {
            LastItemIndex = (LastItemIndex + 1) % MaxLength;
        }

        // advance first character index
        public void advanceFirstIndex()
        {
            FirstItemIndex = (FirstItemIndex + 1) % MaxLength;
        }

        // clear the Commands
        public void clear()
        {
            FirstItemIndex = LastItemIndex = -1;
        }



        // add byte to the Commands
        public void addCommand(string cmd)
        {

            if (isEmpty())
            {
                FirstItemIndex = LastItemIndex = 0;

            }
            else if (isFull())
            {
                advanceFirstIndex();
                advanceLastIndex();

            }
            else
                advanceLastIndex();

            Commands[LastItemIndex] = cmd;

        }

        // add a range of bytes
        public void addRange(string[] cmds)
        {
            int i;
            int len = cmds.Length;
            for (i = 0; i < len; i++) 
                addCommand(cmds[i]);
        }

        // remove a byte from the Commands
        public string removeCommand()
        {
            string str = null;
            if (!isEmpty())
            {
                str = Commands[FirstItemIndex];
                if (FirstItemIndex == LastItemIndex) 
                    FirstItemIndex = LastItemIndex = -1;
                else 
                    advanceFirstIndex();
            }

            return str;
        }

    }
}
