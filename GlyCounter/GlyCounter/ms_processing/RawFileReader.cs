using CSMSL.Spectral;
using Newtonsoft.Json.Linq;
using Nova.Data;
using Nova.Io.Read;
using NuGet;
using PSI_Interface.MSData;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThermoFisher.CommonCore.Data.Business;

namespace GlyCounter
{
    public interface IRawFileReader<T> where T : SpectrumInfo
    {
        IAsyncEnumerable<T> ReadSpectraAsync(string rawFilePath, CancellationToken cancellationToken);
    }

    public sealed class RawMzMLReader : IRawFileReader<SpectrumInfo.RawMzmlSpectrumInfo>
    {
        public static IEnumerable<string> GetOutputHeaders()
        {
            return new SpectrumInfo.RawMzmlSpectrumInfo().GetOutputHeaders();
        }

        public async IAsyncEnumerable<SpectrumInfo.RawMzmlSpectrumInfo> ReadSpectraAsync(
            string filePath, CancellationToken cancellationToken = default)
        {
            FileReader rawFile = new(filePath);

            for (int i = 1; i <= rawFile.LastScan; i++)
            {
                SpectrumEx scan = rawFile.ReadSpectrumEx(scanNumber: i);

                FileReader typeCheck = new FileReader();
                string fileType = typeCheck.CheckFileFormat(filePath).ToString(); //either "ThermoRaw" or "MzML"
                bool thermo = true;
                if (fileType == "MzML")
                    thermo = false;

                (Fragmentation.Type dissociationMethod, double nce) = Fragmentation.GetFragmentationType(thermo, scan);

                var precursorScanNumber = 0;
                var precursorMz = 0.0;
                var charge = 0;

                switch (scan.MsLevel)
                {
                    case > 1:
                        precursorScanNumber = scan.PrecursorMasterScanNumber;
                        precursorMz = scan.Precursors[0].IsolationMz;
                        charge = scan.Precursors[0].Charge;
                        break;
                }


                var spectrum = new SpectrumInfo.RawMzmlSpectrumInfo()
                {
                    ScanNumber = scan.ScanNumber,
                    RetentionTime = scan.RetentionTime,
                    MsLevel = scan.MsLevel,
                    PrecursorScanNumber = precursorScanNumber,
                    PrecursorMz = precursorMz,
                    Charge = charge,
                    DissociationMethod = dissociationMethod,
                    CollisionEnergy = nce,
                    TotalIonCurrent = scan.TotalIonCurrent,
                    BasePeakIntensity = scan.BasePeakIntensity,
                    ScanInjTime = scan.IonInjectionTime,
                    Mz = scan.DataPoints.Select(x => x.Mz).ToArray(),
                    Intensity = scan.DataPoints.Select(x => x.Intensity).ToArray(),
                    Noise = scan.DataPoints.Select(x => x.Noise).ToArray(),
                    HasNoise = (scan.DataPoints.Select(x => x.Noise).ToArray().Max() > 0),
                };

                yield return spectrum;
            }

            await Task.CompletedTask;
        }
    }

    public sealed class TimsFileReader : IRawFileReader<SpectrumInfo.TimsSpectrumInfo>
    {
        public static IEnumerable<string> GetOutputHeaders()
        {
            return new SpectrumInfo.TimsSpectrumInfo().GetOutputHeaders();
        }

        public async IAsyncEnumerable<SpectrumInfo.TimsSpectrumInfo> ReadSpectraAsync(
            string filePath, CancellationToken cancellationToken = default)
        {
            foreach (RawSpectrum rawSpectrum in Native.ReadMsnSpectraLazy(filePath))
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Skip MS1 spectra - they have no precursors and shouldn't be processed
                if (rawSpectrum.ms_level <= 1)
                {
                    continue;
                }

                if (rawSpectrum.intensity == null || rawSpectrum.intensity.Length == 0 ||
                    rawSpectrum.mz == null || rawSpectrum.mz.Length == 0 ||
                    rawSpectrum.precursors == null || rawSpectrum.precursors.Length == 0)
                {
                    try
                    {
                        var logPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "glycounter_bad_spectra.log");
                        System.IO.File.AppendAllText(logPath, $"{DateTime.Now:O}\tSkipped spectrum id={rawSpectrum?.id ?? "null"}\n");
                    }
                    catch { /* never let logging throw */ }
                    continue;
                }

                var precursorScanNumber = 0;
                var precursorMz = 0.0;
                var charge = 0;
                var ionMobility = 0.0;

                precursorScanNumber = Convert.ToInt32(rawSpectrum.precursors[0].spectrum_ref);
                precursorMz = rawSpectrum.precursors[0].mz;
                charge = rawSpectrum.precursors[0].charge ?? 0;
                ionMobility = rawSpectrum.precursors[0].ion_mobility ?? 0;
                var spectrum = new SpectrumInfo.TimsSpectrumInfo();
                try
                {
                    spectrum = new SpectrumInfo.TimsSpectrumInfo
                    {
                        ScanNumber = int.TryParse(rawSpectrum.id, out var scanNum) ? scanNum : 0,
                        RetentionTime = rawSpectrum.scan_start_time ?? 0,
                        MsLevel = rawSpectrum.ms_level,
                        PrecursorScanNumber = precursorScanNumber,
                        PrecursorMz = precursorMz,
                        Charge = charge,
                        DissociationMethod = Fragmentation.Type.HCD, // TIMS assumes HCD/PASEF
                        CollisionEnergy = Convert.ToDouble(rawSpectrum.collision_energy ?? 0),
                        TotalIonCurrent = rawSpectrum.intensity.Sum(),
                        BasePeakIntensity = rawSpectrum.intensity.Length > 0 ? rawSpectrum.intensity.Max() : 0,
                        ScanInjTime = 0, // Not available in TIMS
                        Mz = Array.ConvertAll(rawSpectrum.mz, x => (double)x),
                        Intensity = Array.ConvertAll(rawSpectrum.intensity, x => (double)x),
                        Noise = Array.Empty<double>(), //no noise values available
                        HasNoise = false,
                        IonMobility = ionMobility,
                    };


                }
                catch (System.IndexOutOfRangeException)
                {
                    Debug.WriteLine("Exception here");
                }

                yield return spectrum;



            }

            await Task.CompletedTask;
        }
    }

}
