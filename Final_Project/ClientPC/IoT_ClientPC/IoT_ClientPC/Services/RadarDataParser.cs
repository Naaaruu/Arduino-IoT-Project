using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IoT_ClientPC.Models;

namespace IoT_ClientPC.Services
{
    public class RadarDataParser
    {
        public bool TryParse(string message, out RadarData? data)
        {
            data = null;

            if (string.IsNullOrWhiteSpace(message))
                return false;

            string[] parts = message.Trim().Split(':');

            if (parts.Length != 3)
                return false;

            if (parts[0] != "RADAR")
                return false;

            if (!int.TryParse(parts[1], out int angle))
                return false;

            if (!int.TryParse(parts[2], out int distance))
                return false;

            data = new RadarData(angle, distance);
            return true;
        }
    }
}
