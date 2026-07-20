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

        public const string LikelyGlycoHeader = "OxoInPeakDepthThresh\tOxoRequired\tOxoTICfraction\tLikelyGlycoSpectrum";


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