using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlyCounter
{
    public static class FileHeaders
    {
        public const string PeriscopeHeader = "ScanNumber\tOxoniumIons\tMassError\t";

        //Raw and MzML
        public const string OxoSignalHeader =
            "ScanNumber\tRetentionTime\tMSLevel\tPrecursorMZ\tCharge\tNCE\tScanTIC\tTotalOxoSignal\tScanInjTime\tDissociationType\tPrecursorScan\tNumOxonium\tTotalOxoSignal\t";
        public const string LikelyGlycoHeader = "OxoInPeakDepthThresh\tOxoRequired\tOxoTICfraction\tLikelyGlycoSpectrum";

        //tims
        public const string OxoSignalHeader_tims =
            "ScanNumber\tRetentionTime\tIonMobility\tMSLevel\tPrecursorMZ\tCharge\tNCE\tScanTIC\tTotalOxoSignal\tScanInjTime\tDissociationType\tPrecursorScan\tNumOxonium\tTotalOxoSignal\t";
        public const string LikelyGlycoHeader_tims = "OxoInPeakDepthThresh\tOxoRequired\tOxoTICfraction\tLikelyGlycoSpectrum";


        //Summary
        public const string SummaryDissociationHeader = "\tTotal\tHCD\tETD\tUVPD\t%Total\t%HCD\t%ETD\t%UVPD";
        public const string SummarySeparator1 = @"\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\" + "\t" +
                                                @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" +
                                                @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" +
                                                @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\";

        public const string SummarySeparator2 = @" \\\\\\\\\\\\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" +
                                                @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" +
                                                @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" +
                                                @"\\\\\\\\\\";

        public const string SummarySeparator3 = @"\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\" + "\t" +
                                                @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" +
                                                @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" +
                                                @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\";
    }
}