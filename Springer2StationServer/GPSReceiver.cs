using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Springer2StationServer
{
    class GPSReceiver : NMEAReceiver
    {
        
        // the degrees are signed to indicate N-orth (+) / S-outh (-) hemisphere - E-ast (+) / W-est (-)
        public int LongDegrees;     // 3 digits
        public int LatDegrees;      // 2 digits
        public int LongMinutes;     // 2 digits
        public int LatMinutes;      // 2 digits
        public int LongDeciminutes; // 4 digits
        public int LatDeciminutes;  // 4 digits


        // UTC time as transmitted by last GPS reception
        public int LastUTCHour;       // 2 digits
        public int LastUTCMinute;     // 2 digits
        public int LastUTCSecond;     // 2 digits
        public int LasUTCMillisecond; // 3 digits
        
        // UTC Date and Time in Datetime format
        public DateTime UTCDateTime;

        // Data valid (3d argument field in GPRMC sentence)
        public Boolean MRCStatus;
        // Speed over Ground
        public double SpeedOverGround;
        // Course over ground
        public double CourseOverGround;

        // Magnetic Variation
        public double MagVariation;

        // constructor
        public GPSReceiver()
            : base()
        {

            // an approximate current location (somewhere in Plymouth)
            LongDegrees = +50; // 50 degrees North
            LongMinutes = 35;  // 22 minutes
            LongDeciminutes = 0; 

            LatDegrees = -4;  // 4 degrees West
            LatMinutes = 07;  // 7 minutes
            LatDeciminutes = 0;

            // Date and time
            UTCDateTime = DateTime.Now;
            LastUTCHour = UTCDateTime.Hour;
            LastUTCMinute = UTCDateTime.Minute;
            LastUTCSecond = UTCDateTime.Second;
            LasUTCMillisecond = UTCDateTime.Millisecond;

            MRCStatus = false;
            SpeedOverGround = CourseOverGround = -1;
            
        }

        public GPSReceiver(string commport, int baudrate, Boolean checksum)
            : base(commport, baudrate, checksum)
        {
            // an approximate current location (somewhere in Plymouth)
            LongDegrees = +50; // 50 degrees North
            LongMinutes = 35;  // 22 minutes
            LongDeciminutes = 0; 

            LatDegrees = -4;  // 4 degrees West
            LatMinutes = 07;  // 7 minutes
            LatDeciminutes = 0;

            // Date and time
            UTCDateTime = DateTime.Now;
            LastUTCHour = UTCDateTime.Hour;
            LastUTCMinute = UTCDateTime.Minute;
            LastUTCSecond = UTCDateTime.Second;
            LasUTCMillisecond = UTCDateTime.Millisecond;

            MRCStatus = false;

            SpeedOverGround = CourseOverGround = -1;
        }
        
        public override void processEvents()
        {
            // removing a sentence possibly store in the queue
            int qlen = SentenceQueue.Count();
            if (qlen > 0)
            {
                string sentence = SentenceQueue[qlen - 1];
                SentenceQueue.RemoveAt(qlen - 1);
                //Console.WriteLine(sentence);
                List<string> args = parseNMEASentence(string2Bytes(sentence));
                if (args[0] == "GPRMC")
                { // GPRMC sentence
                    // Field 1: UTC Time
                    string thetime = args[1]; // first argument should be the time in hhmmss.sss format
                    if (thetime.Length >=6)
                    {
                        LastUTCHour = (thetime[0] - '0') * 10 + thetime[1] - '0';
                        LastUTCMinute = (thetime[2] - '0') * 10 + thetime[3] - '0';
                        LastUTCSecond = (thetime[4] - '0') * 10 + thetime[5] - '0';
                        if (thetime.Length >=8) LasUTCMillisecond = (thetime[7] - '0') * 10 + thetime[8] - '0';
                    }
                    // Field 2: Valid Data (A - valid, V - invalid)
                    
                    MRCStatus = args[2][0] == 'A' ? true : false;
                    if (MRCStatus)
                    {
                        // Field 3-4: Latitude in format ddmm.mmmm and latitude hemisphere (N/S)
                        string latitude = args[3];
                        LatDegrees = (latitude[0] - '0') * 10 + latitude[1] - '0';
                        LatMinutes = (latitude[2] - '0') * 10 + latitude[3] - '0';
                        LatDeciminutes = (latitude[5] - '0') * 1000 + (latitude[6] - '0') * 100 + (latitude[7] - '0') * 10 + latitude[8] - '0';

                        LatDegrees *= (args[4][0] == 'N') ? 1 : -1;

                        // Field 5 - 6: Longitude in format dddmm.mmmm and longitude hemisphere (E/W)
                        string longitude = args[5];
                        LongDegrees = (longitude[0] - '0') * 100 + (longitude[1] - '0') * 10 + longitude[2] - '0';
                        LongMinutes = (longitude[3] - '0') * 10 + longitude[4] - '0';
                        LongDeciminutes = (longitude[6] - '0') * 1000 + (longitude[7] - '0') * 100 + (longitude[8] - '0') * 10 + longitude[9] - '0';

                        LongDegrees *= args[6][0] == 'E' ? 1 : -1;
                    }
                    else
                    {
                        LatDegrees = LatMinutes = 0;
                        LongDegrees = LongMinutes = LongDeciminutes = 0;
                    }

                    //Field 7: Speed over ground
                    if (MRCStatus)
                    {
                        string speed = args[7];
                        if (speed.Length > 0)
                            SpeedOverGround = Convert.ToDouble(speed);
                        else
                            SpeedOverGround = -1;
                        // Field 8: course over ground
                        string course = args[8];
                        if (course.Length > 0)
                            CourseOverGround = Convert.ToDouble(course);
                        else
                            CourseOverGround = -1;

                        // Field 9: Date
                        string thedate = args[9];
                        int day, month, year;

                        day = (thedate[0] - '0') * 10 + thedate[1] - '0';
                        month = (thedate[2] - '0') * 10 + thedate[3] - '0';
                        year = (thedate[4] - '0') * 10 + thedate[5] - '0';

                        UTCDateTime = new DateTime(2000 + year, month, day, LastUTCHour, LastUTCMinute, LastUTCSecond);
                    }
                    else
                    {
                        SpeedOverGround = -1;
                        CourseOverGround = -1;
                        UTCDateTime = DateTime.Today;
                    }

                    // Field 10 - 11: Magnetic Variation (E/W)
                    /* if (MRCStatus)
                    {
                        string magvar = args[10];
                        if (magvar.Length > 0)
                        {
                            MagVariation = Convert.ToDouble(magvar);

                            MagVariation *= args[11][0] == 'E' ? 1 : -1;
                        } els
                    }
                    else MagVariation = -1; */

                }
                    
                    // ************** time retrieved and stored **********
            }
        }



        // a function that returns the latitude as string
        public String getLatitudeAsString()
        {
            String latitude = "";
            // Latitude Degrees (2 digits)
            latitude += Convert.ToString(LatDegrees / 10);
            latitude += Convert.ToString(LatDegrees % 10);

            // Latitude minutes (2 digits)        
            latitude += Convert.ToString(LatMinutes / 10);
            latitude += Convert.ToString(LatMinutes % 10);


            // Latitude Deciminutes (4 digits)
            latitude += Convert.ToString(LatDeciminutes / 1000);
            latitude += Convert.ToString(LatDeciminutes / 100);
            latitude += Convert.ToString(LatDeciminutes / 10);

            latitude += Convert.ToString(LatDeciminutes % 10);
            return latitude;
        }

        // a function that returns the longitude as string
        public String getLongitudeAsString()
        {
            String longitude = "";
            // Longitude Degrees (3 digits)
            longitude += Convert.ToString(LongDegrees / 100);
            
            longitude += Convert.ToString(LongDegrees / 10);
            longitude += Convert.ToString(LongDegrees % 10);

            // Latitude minutes (2 digits)        
            longitude += Convert.ToString(LongMinutes / 10);
            longitude += Convert.ToString(LongMinutes % 10);


            // Latitude Deciminutes (4 digits)
            longitude += Convert.ToString(LongDeciminutes / 1000);
            longitude += Convert.ToString(LongDeciminutes / 100);
            longitude += Convert.ToString(LongDeciminutes / 10);

            longitude += Convert.ToString(LongDeciminutes % 10);
            
            return longitude;
        }

    }
}
