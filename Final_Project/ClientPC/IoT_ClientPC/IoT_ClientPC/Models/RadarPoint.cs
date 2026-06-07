using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoT_ClientPC.Models
{
    public class RadarPoint
    {
        public int Angle { get; set; }
        public int Distance { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
