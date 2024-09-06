// Copyright 2012, 2013, 2014 Derek J. Bailey
// 
// This file (ThermoRawFile.cs) is part of CSMSL.
// 
// CSMSL is free software: you can redistribute it and/or modify it
// under the terms of the GNU Lesser General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// CSMSL is distributed in the hope that it will be useful, but WITHOUT
// ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
// FITNESS FOR A PARTICULAR PURPOSE. See the GNU Lesser General Public
// License for more details.
// 
// You should have received a copy of the GNU Lesser General Public
// License along with CSMSL. If not, see <http://www.gnu.org/licenses/>.

using System.Linq;
using CSMSL.Proteomics;
using CSMSL.Spectral;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

using ThermoFisher.CommonCore.Data;
using ThermoFisher.CommonCore.Data.Business;
using ThermoFisher.CommonCore.Data.FilterEnums;
using ThermoFisher.CommonCore.Data.Interfaces;
using ThermoFisher.CommonCore.MassPrecisionEstimator;
using ThermoFisher.CommonCore.RawFileReader;
using System.Security.AccessControl;


namespace CSMSL.IO.Thermo
{
    public class ThermoRawFile : MSDataFile<ThermoSpectrum>
    {
        internal enum RawLabelDataColumn
        {
            MZ = 0,
            Intensity = 1,
            Resolution = 2,
            NoiseBaseline = 3,
            NoiseLevel = 4,
            Charge = 5
        }

        private enum ThermoMzAnalyzer
        {
            None = -1,
            ITMS = 0,
            TQMS = 1,
            SQMS = 2,
            TOFMS = 3,
            FTMS = 4,
            Sector = 5
        }

        public enum Smoothing
        {
            None = 0,
            Boxcar = 1,
            Gauusian = 2
        }

        public enum IntensityCutoffType
        {
            None = 0,
            Absolute = 1,
            Relative = 2
        };

        // DRB Edit to update CSMSL to Thermo's new Raw File Reader
        private IRawDataExtended _rawConnection;
        private IRunHeader _rawFileHeader;
        private IInstrumentDataAccess _rawInstrumentData;

        public ThermoRawFile(string filePath)
            : base(filePath, MSDataFileType.ThermoRawFile)
        {
        }

        public static bool AlwaysGetUnlabeledData = false;

        /// <summary>
        /// MS Tune Information
        /// </summary>
        protected bool mLoadMSTuneInfo = true;

        /// <summary>
        /// Opens the connection to the underlying data
        /// </summary>
        public override void Open()
        {
            if (IsOpen && _rawConnection != null)
                return;

            if (!File.Exists(FilePath) && !Directory.Exists(FilePath))
            {
                throw new IOException(string.Format("The MS data file {0} does not currently exist", FilePath));
            }

            // DRB edit: Try using the fileFactory settings here.
            _rawConnection = RawFileReaderAdapter.FileFactory(FilePath);
            _rawConnection.SelectInstrument(Device.MS, 1);

            _rawFileHeader = _rawConnection.RunHeaderEx;
            _rawInstrumentData = _rawConnection.GetInstrumentData();

            base.Open();
        }

        public void Close()
        {
            if (!File.Exists(FilePath) && !Directory.Exists(FilePath))
            {
                throw new IOException(string.Format("The MS data file {0} does not currently exist", FilePath));
            }

            if (IsOpen && _rawConnection != null)
            {

            }

        }

        public bool LoadMSTuneInfo
        {
            get { return mLoadMSTuneInfo; }
            set { mLoadMSTuneInfo = value; }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_rawConnection != null)
                {
                    _rawConnection.Dispose();
                    _rawConnection = null;
                }
            }
            base.Dispose(disposing);
        }

        protected override int GetFirstSpectrumNumber()
        {
            int spectrumNumber = 0;
            spectrumNumber = _rawFileHeader.FirstSpectrum;
            return spectrumNumber;
        }

        protected override int GetLastSpectrumNumber()
        {
            int spectrumNumber = 0;
            spectrumNumber = _rawFileHeader.LastSpectrum;
            return spectrumNumber;
        }

        public override double GetRetentionTime(int spectrumNumber)
        {
            double retentionTime = 0;
            retentionTime = _rawConnection.RetentionTimeFromScanNumber(spectrumNumber);
            return retentionTime;
        }

        public override int GetMsnOrder(int spectrumNumber)
        {
            int msnOrder = 0;
            msnOrder = ((int)_rawConnection.GetFilterForScanNumber(spectrumNumber).MSOrder);
            return msnOrder;
        }

        public override int GetParentSpectrumNumber(int spectrumNumber)
        {
            if (GetMsnOrder(spectrumNumber) == 1)
                return 0;

            object parentScanNumber = GetExtraValue(spectrumNumber, "Master Scan Number:");
            int scanNumber = Convert.ToInt32(parentScanNumber);

            if (scanNumber == 0)
            {
                int masterIndex = Convert.ToInt32(GetExtraValue(spectrumNumber, "Master Index:"));
                if (masterIndex == 0)
                    throw new ArgumentException("Scan Number " + spectrumNumber + " has no parent");
                int scanIndex = Convert.ToInt32(GetExtraValue(spectrumNumber, "Scan Event:"));
                scanNumber = spectrumNumber - scanIndex + masterIndex;
            }

            return scanNumber;
        }

        // DRB edit
        public double[,] GetChro(IRawDataPlus rawFile, int startScan, int endScan, TraceType traceType, bool outputData)
        {
            // Define the settings for getting the Base Peak chromatogram
            ChromatogramTraceSettings settings = new ChromatogramTraceSettings(traceType);

            // Get the chromatogram from the RAW file. 
            var chromaDataObject = rawFile.GetChromatogramData(new IChromatogramSettings[] { settings }, startScan, endScan);
            var chroDataArray = ChromatogramSignal.FromChromatogramData(chromaDataObject);

            var theActualChromatogram = chroDataArray[0];

            var retentionTimes = theActualChromatogram.SignalTimes;
            var intensities = theActualChromatogram.SignalIntensities;
            var arrayLength = theActualChromatogram.Length;

            var chro = new double[2, arrayLength];

            for (var i = 0; i <= arrayLength; i++)
            {
                chro[0, i] = retentionTimes[i];
                chro[1, i] = intensities[i];
            }

            return (double[,])chro;
        }

        private object GetExtraValue(int spectrumNumber, string filterText)
        {
            var returnLabels = new List<String>();
            var returnValues = new List<String>();

            GetAllTrailingHeaderData(spectrumNumber, out returnLabels, out returnValues);
            var returnObject = returnValues[returnLabels.FindIndex(a => a.Contains(filterText))];
            return returnObject;
        }

        private object GetExtraValues(int spectrumNumber)
        {
            object values = null;
            values = _rawConnection.GetTrailerExtraValues(spectrumNumber);
            return values;
        }

        // DRB edit
        public void GetAllTrailingHeaderData(int spectrumNumber, out List<String> returnLabels, out List<string> returnValues)
        {
            var trailingHeaderData = _rawConnection.GetTrailerExtraInformation(spectrumNumber);
            var test = _rawConnection.GetTrailerExtraHeaderInformation();
            returnLabels = ((string[])trailingHeaderData.Labels).ToList();
            returnValues = ((string[])trailingHeaderData.Values).ToList();
        }

        public string GetScanFilter(int spectrumNumber)
        {
            IScanFilter filter = null;

            filter = _rawConnection.GetFilterForScanNumber(spectrumNumber);

            return filter.ToString();
        }

        private static readonly Regex PolarityRegex = new Regex(@" \+ ", RegexOptions.Compiled);

        public override Polarity GetPolarity(int spectrumNumber)
        {
            string filter = GetScanFilter(spectrumNumber);
            return PolarityRegex.IsMatch(filter) ? Polarity.Positive : Polarity.Negative;
        }

        public override ThermoSpectrum GetSpectrum(int spectrumNumber)
        {
            return GetSpectrum(spectrumNumber);
        }

        public ThermoSpectrum GetSpectrum(int spectrumNumber, bool profileIfAvailable = false)
        {
            bool useProfile = profileIfAvailable;

            if (useProfile)
            {
                // check if this scan is profile. If it isn't, can't return profile data.
                var scanStats = _rawConnection.GetScanStatsForScanNumber(spectrumNumber);
                useProfile = !scanStats.IsCentroidScan; // Will set useProfile to true if scan is profile. Else scan is pre-centroided and won't have profile data (I think)
            }
            return new ThermoSpectrum(GetUnlabeledData(spectrumNumber, useProfile));
        }

        public MZSpectrum GetAveragedSpectrum(int firstSpectrumNumber, int lastSpectrumNumber, string scanFilter = "", IntensityCutoffType type = IntensityCutoffType.None, int intensityCutoff = 0)
        {
            double[,] _spectrum = null;
            int _arraySize = -1;

            var averageScan = _rawConnection.AverageScansInScanRange(firstSpectrumNumber, lastSpectrumNumber, scanFilter);

            if (averageScan.HasCentroidStream)
            {
                _arraySize = averageScan.CentroidScan.Length;
                _spectrum = new double[2, _arraySize];

                for (var i = 0; i < _arraySize; i++)
                {
                    _spectrum[0, i] = averageScan.CentroidScan.Masses[i];
                    _spectrum[i, i] = averageScan.CentroidScan.Intensities[i];
                }

            }

            return new MZSpectrum(_spectrum, _arraySize);
        }

        public ThermoSpectrum GetLabeledSpectrum(int spectrumNumber)
        {
            var labelData = GetLabeledData(spectrumNumber);
            return new ThermoSpectrum(labelData);
        }

        public MZSpectrum GetSNSpectrum(int spectrumNumber, double minSN = 3)
        {
            var labelData = GetLabeledData(spectrumNumber);
            int count = labelData.GetLength(1);
            double[] mz = new double[count];
            double[] sns = new double[count];
            int j = 0;
            for (int i = 0; i < count; i++)
            {
                double sn = labelData[1, i] / labelData[4, i];
                if (sn >= minSN)
                {
                    mz[j] = labelData[0, i];
                    sns[j] = sn;
                    j++;
                }
            }
            Array.Resize(ref mz, j);
            Array.Resize(ref sns, j);
            return new MZSpectrum(mz, sns, false);
        }

        private double[,] GetUnlabeledData(int spectrumNumber, bool useProfile)
        {
            double[,] massList = null;

            Scan scan = Scan.FromFile(_rawConnection, spectrumNumber);
            ScanStatistics scanStatistics = _rawConnection.GetScanStatsForScanNumber(spectrumNumber);

            // we want profile data and have profile data
            if (useProfile && !scanStatistics.IsCentroidScan)
            {
                scan.PreferCentroids = false;
                var arrayLength = scan.PreferredMasses.Length;

                massList = new double[2, arrayLength];

                for (var i = 0; i < arrayLength; i++)
                {
                    massList[0, i] = scan.PreferredMasses[i];
                    massList[1, i] = scan.PreferredIntensities[i];
                }

                return (double[,])(massList);
            }
            else
            {
                scan.PreferCentroids = true;
                var arrayLength = scan.PreferredMasses.Length;

                massList = new double[2, arrayLength];

                for (var i = 0; i < arrayLength; i++)
                {
                    massList[0, i] = scan.PreferredMasses[i];
                    massList[1, i] = scan.PreferredIntensities[i];
                }

                return (double[,])(massList);
            }
        }

        private double[,] GetLabeledData(int spectrumNumber, bool profileIfAvailable = false)
        {
            double[,] peakList = null;
            MZAnalyzerType scanAnalyzer = GetMzAnalyzer(spectrumNumber);

            /*
            if (profileIfAvailable || scanAnalyzer != MZAnalyzerType.Orbitrap)
            {
                // profile data actually doesn't have lables, just m/z and intensities
                // just use GetUnlabledData
                return null;
            }
            */
            if (profileIfAvailable)
            {
                // profile data actually doesn't have lables, just m/z and intensities
                // just use GetUnlabledData
                return null;
            }
            else
            {
                ScanStatistics scanStatistics = _rawConnection.GetScanStatsForScanNumber(spectrumNumber);

                var scan = Scan.FromFile(_rawConnection, spectrumNumber);
                scan.PreferCentroids = true;

                var centroidStream = scan.CentroidScan;

                peakList = new double[6, centroidStream.Masses.Length];

                for (var i = 0; i < scan.PreferredMasses.Length; i++)
                {
                    peakList[0, i] = centroidStream.Masses[i];
                    peakList[1, i] = centroidStream.Intensities[i];
                    peakList[2, i] = centroidStream.Resolutions[i];
                    peakList[3, i] = centroidStream.Baselines[i];
                    peakList[4, i] = centroidStream.Noises[i];
                    peakList[5, i] = centroidStream.Charges[i];
                }
                return peakList;
            }
        }

        public override MZAnalyzerType GetMzAnalyzer(int spectrumNumber)
        {
            var _filter = _rawConnection.GetFilterForScanNumber(spectrumNumber);

            var _analyzer = _filter.MassAnalyzer;

            switch ((ThermoMzAnalyzer)_analyzer)
            {
                case ThermoMzAnalyzer.FTMS:
                    return MZAnalyzerType.Orbitrap;
                case ThermoMzAnalyzer.ITMS:
                    return MZAnalyzerType.IonTrap2D;
                case ThermoMzAnalyzer.Sector:
                    return MZAnalyzerType.Sector;
                case ThermoMzAnalyzer.TOFMS:
                    return MZAnalyzerType.TOF;
                default:
                    return MZAnalyzerType.Unknown;
            }
        }
        public DateTime GetCreationDate()
        {
            DateTime pCreationDate = DateTime.MinValue;
            pCreationDate = _rawConnection.CreationDate;
            return pCreationDate;
        }

        // DRB edit: Should update MSn Order to something representative of # activations. 
        public override double GetPrecursorMz(int spectrumNumber, int msnOrder = 2)
        {
            double mz = double.NaN;

            var scanEvent = _rawConnection.GetScanEventForScanNumber(spectrumNumber);
            var reactionParams = scanEvent.GetReaction(msnOrder - 2); // index 0 = original precursor AKA intact MS1 m/z
            mz = reactionParams.PrecursorMass;

            return mz;
        }

        public double GetPrecusorMz(int spectrumNumber, double searchMZ, int msnOrder = 2)
        {
            int parentScanNumber = GetParentSpectrumNumber(spectrumNumber);
            var ms1Scan = GetSpectrum(parentScanNumber);
            MZPeak peak = ms1Scan.GetClosestPeak(MassRange.FromDa(searchMZ, 50));
            if (peak != null)
                return peak.MZ;
            return double.NaN;
        }

        public override double GetIsolationWidth(int spectrumNumber, int msnOrder = 2)
        {
            object width = GetExtraValue(spectrumNumber, string.Format("MS{0} Isolation Width:", msnOrder));
            return Convert.ToDouble(width);
        }

        public double GetElapsedScanTime(int spectrumNumber)
        {
            object elapsedScanTime = GetExtraValue(spectrumNumber, "Elapsed Scan Time (sec):");
            return Convert.ToDouble(elapsedScanTime);
        }

        public double GetTIC(int spectrumNumber)
        {
            var scanStatistics = _rawConnection.GetScanStatsForScanNumber(spectrumNumber);

            var tic = scanStatistics.TIC;

            return tic;
        }

        public string[,] GetScanHeaderData(int spectrumNumber)
        {
            var trailerExtraInformation = _rawConnection.GetTrailerExtraInformation(spectrumNumber);

            var pvarLabels = trailerExtraInformation.Labels;
            var pvarValues = trailerExtraInformation.Values;
            var pnArraySize = trailerExtraInformation.Length;

            string[,] returnArray = new string[2, pnArraySize];

            for (int i = 0; i < pnArraySize; i++)
            {
                returnArray[0, i] = ((string[])pvarLabels)[i];
                returnArray[1, i] = ((string[])pvarValues)[i];
            }

            return returnArray;
        }

        public string GetScanHeaderData(int spectrumNumber, string flag)
        {
            var trailerExtraInformation = _rawConnection.GetTrailerExtraInformation(spectrumNumber);

            var pvarLabels = trailerExtraInformation.Labels.ToList();

            var indexOfFlag = trailerExtraInformation.Labels.ToList().FindIndex(a => a.Contains(flag));
            var returnFlagValue = trailerExtraInformation.Values[indexOfFlag];

            return returnFlagValue;
        }

        public override DissociationType GetDissociationType(int spectrumNumber, int msnOrder = 2)
        {
            IScanFilter filter = _rawConnection.GetFilterForScanNumber(spectrumNumber);

            ActivationType activationType = filter.GetActivation(msnOrder - 2); // DRB Edit: index 0 is the original activated precursor. Should r

            return (DissociationType)activationType;
        }

        public override MzRange GetMzRange(int spectrumNumber)
        {
            ScanStatistics scanStats = _rawConnection.GetScanStatsForScanNumber(spectrumNumber);

            var _lowMass = scanStats.LowMass;
            var _highMass = scanStats.HighMass;

            return new MzRange(_lowMass, _highMass);
        }

        public override int GetPrecusorCharge(int spectrumNumber, int msnOrder = 2)
        {
            short charge = Convert.ToInt16(GetExtraValue(spectrumNumber, "Charge State:"));
            return charge * (int)GetPolarity(spectrumNumber);
        }

        public override int GetSpectrumNumber(double retentionTime)
        {
            int spectrumNumber = 0;
            spectrumNumber = _rawConnection.ScanNumberFromRetentionTime(retentionTime);
            return spectrumNumber;
        }

        public override double GetInjectionTime(int spectrumNumber)
        {
            object time = GetExtraValue(spectrumNumber, "Ion Injection Time (ms):");
            return Convert.ToDouble(time);
        }

        public override double GetResolution(int spectrumNumber)
        {
            var labels = new List<string>();
            var values = new List<string>();

            GetAllTrailingHeaderData(spectrumNumber, out labels, out values);

            MZAnalyzerType analyzer = GetMzAnalyzer(spectrumNumber);
            double resolution = 0;
            switch (analyzer)
            {
                case MZAnalyzerType.FTICR:
                case MZAnalyzerType.Orbitrap:
                    string name = GetInstrumentName();
                    if (name.Contains("Orbitrap") || name.Equals("Q Exactive"))
                    {
                        object obj = GetExtraValue(spectrumNumber, "Orbitrap Resolution:");
                        resolution = Convert.ToDouble(obj);
                        if (resolution <= 0)
                        {
                            // Find first peak with S/N greater than 3 to use for resolution calculation
                            double[,] data = GetLabeledData(spectrumNumber);
                            int totalPeaks = data.GetLength(1);
                            List<double> avgResolution = new List<double>();

                            for (int i = 0; i < totalPeaks; i++)
                            {
                                double signalToNoise = data[1, i] / data[4, i];
                                if (signalToNoise >= 5)
                                {
                                    double mz = data[0, i];
                                    double peakRes = data[2, i];
                                    double correctedResolution = peakRes * Math.Sqrt(mz / 200);
                                    avgResolution.Add(correctedResolution);
                                }
                            }

                            double meanResolution = avgResolution.Median();
                            if (meanResolution <= 25000)
                            {
                                return 15000;
                            }
                            if (meanResolution <= 45000)
                            {
                                return 30000;
                            }
                            if (meanResolution <= 100000)
                            {
                                return 60000;
                            }
                            if (meanResolution <= 200000)
                            {
                                return 120000;
                            }
                            if (meanResolution <= 400000)
                            {
                                return 240000;
                            }
                            return 450000;
                        }
                        return resolution;
                    }
                    else
                    {
                        object obj = GetExtraValue(spectrumNumber, "FT Resolution:");
                        resolution = Convert.ToDouble(obj);
                        if (resolution > 300000) return 480000;
                        return resolution;
                    }
            }
            return resolution;
        }

        public double ResolutionDefinedAtMZ()
        {
            string name = GetInstrumentName();
            switch (name)
            {
                case "Orbitrap Fusion":
                case "Q Exactive":
                    return 200;
                case "LTQ Orbitrap XL":
                case "LTQ Orbitrap Velos":
                case "Orbitrap Elite":
                    return 400;
                default:
                    return double.NaN;
            }
        }

        public string GetInstrumentName()
        {
            string name = null;
            name = _rawInstrumentData.Name;
            return name;
        }

        public string GetInstrumentModel()
        {
            string model = null;
            model = _rawInstrumentData.Model;
            return model;
        }

        private static Regex _etdReactTimeRegex = new Regex(@"@etd(\d+).(\d+)(\d+)", RegexOptions.Compiled);

        public double GetETDReactionTime(int spectrumNumber)
        {
            string scanheader = GetScanFilter(spectrumNumber);
            Match m = _etdReactTimeRegex.Match(scanheader);
            if (m.Success)
            {
                string etdTime = m.ToString();
                string Time = etdTime.Remove(0, 4);
                double reactTime = double.Parse(Time);
                return reactTime;
            }
            return double.NaN;
        }

        public Chromatogram GetTICChroma()
        {
            // Define the settings for getting the Base Peak chromatogram
            ChromatogramTraceSettings settings = new ChromatogramTraceSettings(TraceType.TIC);

            // Get the chromatogram from the RAW file. 
            var chromaDataObject = _rawConnection.GetChromatogramData(new IChromatogramSettings[] { settings }, _rawFileHeader.FirstSpectrum, _rawFileHeader.LastSpectrum);

            var chroDataArray = ChromatogramSignal.FromChromatogramData(chromaDataObject);

            var theActualChromatogram = chroDataArray[0];

            var arrayLength = theActualChromatogram.Length;
            var retentionTimes = theActualChromatogram.Times;
            var intensities = theActualChromatogram.Intensities;

            double[,] pvarArray = new double[2, arrayLength];

            for (var i = 0; i < arrayLength; i++)
            {
                pvarArray[0, i] = retentionTimes[i];
                pvarArray[1, i] = intensities[i];
            }

            return new Chromatogram(pvarArray);
        }

        private readonly static Regex _msxRegex = new Regex(@"([\d.]+)@", RegexOptions.Compiled);

        public List<double> GetMSXPrecursors(int spectrumNumber)
        {
            string scanheader = GetScanFilter(spectrumNumber);

            // I don't think this was ever actually used for anything, but adding it in just in case...
            var scanStats = _rawConnection.GetScanStatsForScanNumber(spectrumNumber);
            var scanEvent = _rawConnection.GetScanEventForScanNumber(spectrumNumber);
            var multiplexValue = scanEvent.Multiplex;

            var matches = _msxRegex.Matches(scanheader);

            return (from Match match in matches select double.Parse(match.Groups[1].Value)).ToList();
        }
    }
}