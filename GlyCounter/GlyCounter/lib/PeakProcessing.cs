using CSMSL;
using Nova.Data;
using NuGet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlyCounter
{
    public class PeakProcessing
    {
        public static SpecDataPointEx GetPeak(SpectrumEx spectrum, double mz, bool usingda, double tolerance, bool thermo, bool IT = false)
        {
            DoubleRange rangeOxonium = usingda
                ? DoubleRange.FromDa(mz, tolerance)
                : DoubleRange.FromPPM(mz, tolerance);

            List<SpecDataPointEx> peaks = spectrum.DataPoints.ToList();

            List<SpecDataPointEx> peakList = peaks.Where(peak => rangeOxonium.Contains(peak.Mz)).ToList();


            if (!IT && thermo)
                peakList = peakList.OrderByDescending(peak => (peak.Intensity / peak.Noise)).ToList();
            else
                peakList = peakList.OrderByDescending(peak => (peak.Intensity)).ToList();

            return peakList.FirstOrDefault();
        }

        public static SpecDataPointEx GetPeak(RawSpectrum spectrum, double mz, bool usingda, double tolerance)
        {
            DoubleRange rangeOxonium = usingda
                ? DoubleRange.FromDa(mz, tolerance)
                : DoubleRange.FromPPM(mz, tolerance);

            List<SpecDataPointEx> peakList = spectrum.peaks.Where(peak => rangeOxonium.Contains(peak.Mz)).ToList();


            peakList = peakList.OrderByDescending(peak => (peak.Intensity)).ToList();

            return peakList.FirstOrDefault();
        }

        public static Dictionary<double, int> RankOrderPeaks(Dictionary<double, int> dictionary, SpectrumEx spectrum)
        {
            var peaks = spectrum.DataPoints;
            List<double> intensities = new List<double>();
            foreach (var peak in peaks)
                intensities.Add(peak.Intensity);

            var sortedpeakIntensities = intensities.OrderByDescending(x => x);

            int i = 1;
            foreach (double value in sortedpeakIntensities)
            {
                if (!dictionary.ContainsKey(value))
                {
                    dictionary.Add(value, i);
                    i++;
                }
            }
            return dictionary;
        }

        public static Dictionary<double, int> RankOrderPeaks(Dictionary<double, int> dictionary, RawSpectrum spectrum)
        {
            var sortedpeakIntensities = spectrum.intensity.OrderByDescending(x => x);

            int i = 1;
            foreach (double value in sortedpeakIntensities)
            {
                if (!dictionary.ContainsKey(value))
                {
                    dictionary.Add(value, i);
                    i++;
                }
            }
            return dictionary;
        }

        public static List<SpecDataPointEx> ListsToPeaks(List<float> mzs, List<float> intensities)
        {
            List<SpecDataPointEx> peaks = [];
            for (int i = 0; i < mzs.Count; i++)
            {
                SpecDataPointEx peak = new SpecDataPointEx
                {
                    Mz = mzs[i],
                    Intensity = intensities[i]
                };
                peaks.Add(peak);
            }

            return peaks;
        }
    }
}
