using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;

namespace GlyCounter
{
    public class OxoniumIon : IEquatable<OxoniumIon>
    {
        public double theoMZ { get; set; }
        public double measuredMZ { get; set; }
        public string description { get; set; }
        public double intensity { get; set; }
        public string glycanSource { get; set; }
        public int peakDepth { get; set; }
        public int hcdCount { get; set; }
        public int etdCount { get; set; }
        public int uvpdCount { get; set; }

        public bool Equals(OxoniumIon other)
        {
            if (other == null) return false;

            return theoMZ == other.theoMZ || description == other.description;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType()) return false;
            return Equals(obj as OxoniumIon);
        }

        public override int GetHashCode()
        {
            return theoMZ.GetHashCode() * 397 ^ (description?.GetHashCode() ?? 0);
        }

        public static OxoniumIon ProcessOxoIon(object item, string glycanSource, GlyCounterSettings glySettings, bool check204 = false)
        {
            string[] oxoniumIonArray = item.ToString().Split(',');
            OxoniumIon oxoIon = new OxoniumIon();
            oxoIon.theoMZ = Convert.ToDouble(oxoniumIonArray[0], CultureInfo.InvariantCulture);
            oxoIon.description = item.ToString();
            oxoIon.glycanSource = glycanSource;
            oxoIon.hcdCount = 0;
            oxoIon.etdCount = 0;
            oxoIon.uvpdCount = 0;
            oxoIon.peakDepth = glySettings.arbitraryPeakDepthIfNotFound;
            //only need to check for 204 in hexnac ions and custom ions
            if (check204)
                if (Convert.ToDouble(oxoniumIonArray[0], CultureInfo.InvariantCulture) == 204.0867)
                    glySettings.using204 = true;
            return oxoIon;
        }
    }
}
