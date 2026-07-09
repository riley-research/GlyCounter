using CommandLine;
using Nova.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlyCounter
{
    public class Fragmentation
    {
        public enum Type
        {
            HCD,
            ETD,
            UVPD,
            Unknown
        }

        public static (Type, double) GetFragmentationType(bool thermo, SpectrumEx spectrum)
        {
            var dissociationMethod = Fragmentation.Type.Unknown;
            var nce = 0.0;

            if (thermo)
            {
                var scanFilter = spectrum.ScanFilter ?? string.Empty;
                var scanFilterParts = scanFilter.Split('@', 2);
                if (scanFilterParts.Length > 1)
                {
                    // safe parse of the part after '@'
                    var postAt = scanFilterParts[1];
                    var splitHCDheader = postAt.Split('d', 2);
                    if (splitHCDheader.Length > 1)
                    {
                        var collisionEnergyArray = splitHCDheader[1].Split('.', 2);
                        if (collisionEnergyArray.Length > 0 &&
                            double.TryParse(collisionEnergyArray[0],
                                System.Globalization.NumberStyles.Any,
                                System.Globalization.CultureInfo.InvariantCulture, out var nceVal))
                        {
                            nce = nceVal;
                        }
                    }
                }

                var filterLower = scanFilter.ToLowerInvariant();
                if (filterLower.Contains("etd"))
                    dissociationMethod = Fragmentation.Type.ETD;
                else if (filterLower.Contains("hcd"))
                    dissociationMethod = Fragmentation.Type.HCD;
                else if (filterLower.Contains("uvpd") || filterLower.Contains("ci"))
                    dissociationMethod = Fragmentation.Type.UVPD;
            }
            else
            {
                if (spectrum.Precursors != null && spectrum.Precursors.Count > 0 &&
                    spectrum.Precursors[0] != null)
                {
                    var firstPre = spectrum.Precursors[0];
                    string dt = firstPre.FramentationMethod.ToString() ?? string.Empty;

                    if (dt.Equals("HCD", StringComparison.OrdinalIgnoreCase))
                        dissociationMethod = Fragmentation.Type.HCD;

                    if (dt.Equals("ETD", StringComparison.OrdinalIgnoreCase))
                        dissociationMethod = Fragmentation.Type.ETD;

                    if (dt.Equals("CI", StringComparison.OrdinalIgnoreCase) ||
                        dt.Equals("UVPD", StringComparison.OrdinalIgnoreCase))
                        dissociationMethod = Fragmentation.Type.UVPD;

                    if (firstPre.CollisionEnergy != 0)
                        nce = firstPre.CollisionEnergy;
                }
            }
            return (dissociationMethod, nce);
        }
    }
}
