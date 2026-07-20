using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlyCounter
{
    public abstract class SpectrumInfo
    {
        public int ScanNumber { get; set; }
        public double RetentionTime { get; set; }
        public int MsLevel { get; set; }
        public int PrecursorScanNumber { get; set; }
        public double PrecursorMz { get; set; }
        public int Charge { get; set; }
        public Fragmentation.Type DissociationMethod { get; set; }
        public double CollisionEnergy { get; set; }
        public double TotalIonCurrent { get; set; }
        public double BasePeakIntensity { get; set; }
        public double ScanInjTime { get; set; }
        public double[] Mz { get; set; } = Array.Empty<double>();
        public double[] Intensity { get; set; } = Array.Empty<double>();
        public double[] Noise { get; set; } = Array.Empty<double>();
        public bool HasNoise { get; set; }
        public bool Tims { get; set; }


        public virtual IEnumerable<string> GetOutputHeaders()
        {
            yield return "ScanNumber";
            yield return "RetentionTime";
            yield return "MSLevel";
            yield return "PrecursorScanNumber";
            yield return "PrecursorMz";
            yield return "Charge";
            yield return "DissociationMethod";
            yield return "NCE";
            yield return "ScanTIC";
            yield return "BasePeakIntensity";
            yield return "ScanInjTime";
            yield return "NumOxonium";
            yield return "TotalOxoSignal";
        }

        public sealed class RawMzmlSpectrumInfo : SpectrumInfo
        {
            //no extra headers
        }

        public sealed class TimsSpectrumInfo : SpectrumInfo
        {
            public double IonMobility { get; set; }
            public override IEnumerable<string> GetOutputHeaders()
            {
                foreach (var h in base.GetOutputHeaders())
                    yield return h;
                yield return "IonMobility";
            }
        }
    }
}
