using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Springer2StationServer
{
    class GPSDataInstance
    {
        
        // the degrees are signed to indicate N-orth (+) / S-outh (-) hemisphere - E-ast (+) / W-est (-)
        public int LongDegrees;     // 3 digits
        public int LatDegrees;      // 2 digits
        public int LongMinutes;     // 2 digits
        public int LatMinutes;      // 2 digits
        public int LongDeciminutes; // 4 digits
        public int LatDeciminutes;  // 4 digits

        // Speed over Ground
        public double SpeedOverGround; // float
        // Course over ground
        public double CourseOverGround; // float

        // Valid signal reading flag
        public Boolean Valid;


        // frame index
        public int FrameIndex;

        // constructor #1 (by numbers)
        public GPSDataInstance(int frameindex, int longdeg, int latdeg, int longmin, int latmin,
                               int longdecimin, int latdecimin, double speedovgr, double courseoverg, Boolean valid)
        {
            FrameIndex = frameindex;

            LongDegrees = longdeg;
            LatDegrees = latdeg;
            LongMinutes = latmin;
            LongDeciminutes = longdecimin;
            LatDeciminutes = latdecimin;

            SpeedOverGround = speedovgr;
            CourseOverGround = courseoverg;
            Valid = valid;
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



        public string getGpsDataAsCommaDelimetedLine()
        {
            string lineStr = "";
            lineStr += Convert.ToString(FrameIndex) + ",";
            lineStr += (Valid ? "V," : "I,");
            lineStr += getLongitudeAsString() + ",";
            lineStr += getLatitudeAsString() + ",";
            lineStr += Convert.ToString(SpeedOverGround) + ",";
            lineStr += Convert.ToString(CourseOverGround);


            return lineStr;
        }




    }
}
