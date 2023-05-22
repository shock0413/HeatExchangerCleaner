using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortableCleaner.Struct
{
    public class StructHole
    {
        public int Index { get; set; }
        public double X { get; set; }
        public double Y { get; set; }

        public double StartDistance { get { return Math.Sqrt(Math.Pow(X, 2) + Math.Pow(Y, 2)); } }

        public double VisionX { get; set; }
        public double VisionY { get; set; }

        public int Row { get; set; }
        public int Column { get; set; }

        public bool IsTarget { get; set; }

        public StructHole AfterPoint { get; set; }
        public StructHole BeforePoint { get; set; }
        public int GroupIndex { get; set; }

        public bool IsSortStartPoint { get; set; }
        public double AfterDistance { get; set; }
 
        public bool IsCleaningFinish { get; set; }
        public bool IsOK { get; set; }
    }
}
