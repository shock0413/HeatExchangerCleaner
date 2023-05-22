using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GocatorLib
{
    public class SurfaceResult
    {
        public class DataContext 
        {
            public double xResolution;
            public double yResolution;
            public double zResolution;
            public double xOffset;
            public double yOffset;
            public double zOffset;
            public uint serialNumber;
        }

        public class SurfacePoint
        {
            public double x;
            public double y;
            public double z;
            byte intensity;
        }
    }
}
