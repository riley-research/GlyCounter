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
        public static SpecDataPointEx GetPeak(SpectrumInfo spectrum, double mz, bool usingDa, double tolerance)
        {
            DoubleRange rangeOxonium = usingDa
                ? DoubleRange.FromDa(mz, tolerance)
                : DoubleRange.FromPPM(mz, tolerance);

            List<SpecDataPointEx> peaks = [];

            // Ensure all arrays have the same length; use 0 for missing noise values
            int length = spectrum.Mz.Length;
            for (int i = 0; i < length; i++)
            {
                double noise = i < spectrum.Noise.Length ? spectrum.Noise[i] : 0.0;

                SpecDataPointEx peak = new()
                {
                    Mz = spectrum.Mz[i],
                    Intensity = spectrum.Intensity[i],
                    Noise = noise
                };
                peaks.Add(peak);
            }

            List<SpecDataPointEx> peakList = peaks.Where(peak => rangeOxonium.Contains(peak.Mz)).ToList();

            peakList = spectrum.HasNoise
                ? peakList.OrderByDescending(peak => (peak.Intensity / peak.Noise)).ToList()
                : peakList.OrderByDescending(peak => (peak.Intensity)).ToList();

            return peakList.FirstOrDefault();
        }

        public static SpecDataPointEx GetPeak(SpectrumEx spectrum, double mz, bool usingDa, double tolerance, bool thermo)
        {
            DoubleRange rangeOxonium = usingDa
                ? DoubleRange.FromDa(mz, tolerance)
                : DoubleRange.FromPPM(mz, tolerance);

            var mzList = spectrum.DataPoints.Select(x => x.Mz).ToList();
            var intensityList = spectrum.DataPoints.Select(x => x.Intensity).ToList();
            var noiseList = spectrum.DataPoints.Select(x => x.Noise).ToList();

            List<SpecDataPointEx> peaks = [];
            for (int i = 0; i < mzList.Count(); i++)
            {
                SpecDataPointEx peak = new() { Mz = mzList[i], Intensity = intensityList[i], Noise = noiseList[i] };
                peaks.Add(peak);
            }

            List<SpecDataPointEx> peakList = peaks.Where(peak => rangeOxonium.Contains(peak.Mz)).ToList();


            peakList = thermo
                ? peakList.OrderByDescending(peak => (peak.Intensity / peak.Noise)).ToList()
                : peakList.OrderByDescending(peak => (peak.Intensity)).ToList();

            return peakList.FirstOrDefault();
        }

        public static Dictionary<double, int> RankOrderPeaks(Dictionary<double, int> dictionary, SpectrumInfo spectrum)
        {
            List<double> intensities = spectrum.Intensity.ToList();

            var sortedPeakIntensities = intensities.OrderByDescending(x => x);

            int i = 1;
            foreach (double value in sortedPeakIntensities)
            {
                if (dictionary.TryAdd(value, i))
                    i++;
            }
            return dictionary;
        }

        public static Dictionary<double, int> RankOrderPeaks(Dictionary<double, int> dictionary, SpectrumEx spectrum)
        {
            List<double> intensities = spectrum.DataPoints.Select(x => x.Intensity).ToList();

            var sortedPeakIntensities = intensities.OrderByDescending(x => x);

            int i = 1;
            foreach (double value in sortedPeakIntensities)
            {
                if (dictionary.TryAdd(value, i))
                    i++;
            }
            return dictionary;
        }
    }
}
