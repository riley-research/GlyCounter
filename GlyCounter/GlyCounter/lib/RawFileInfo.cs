using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace GlyCounter
{
    public class RawFileInfo
    {
        public int numberOfMS2scansWithOxo_1 { get; set; } = 0;
        public int numberOfMS2scansWithOxo_2 { get; set; } = 0;
        public int numberOfMS2scansWithOxo_3 { get; set; } = 0;
        public int numberOfMS2scansWithOxo_4 { get; set; } = 0;
        public int numberOfMS2scansWithOxo_5plus { get; set; } = 0;
        public int numberOfMS2scansWithOxo_1_hcd { get; set; } = 0;
        public int numberOfMS2scansWithOxo_2_hcd { get; set; } = 0;
        public int numberOfMS2scansWithOxo_3_hcd { get; set; } = 0;
        public int numberOfMS2scansWithOxo_4_hcd { get; set; } = 0;
        public int numberOfMS2scansWithOxo_5plus_hcd { get; set; } = 0;
        public int numberOfMS2scansWithOxo_1_etd { get; set; } = 0;
        public int numberOfMS2scansWithOxo_2_etd { get; set; } = 0;
        public int numberOfMS2scansWithOxo_3_etd { get; set; } = 0;
        public int numberOfMS2scansWithOxo_4_etd { get; set; } = 0;
        public int numberOfMS2scansWithOxo_5plus_etd { get; set; } = 0;
        public int numberOfMS2scansWithOxo_1_uvpd { get; set; } = 0;
        public int numberOfMS2scansWithOxo_2_uvpd { get; set; } = 0;
        public int numberOfMS2scansWithOxo_3_uvpd { get; set; } = 0;
        public int numberOfMS2scansWithOxo_4_uvpd { get; set; } = 0;
        public int numberOfMS2scansWithOxo_5plus_uvpd { get; set; } = 0;
        public int numberOfMS2scans { get; set; } = 0;
        public int numberOfHCDscans { get; set; } = 0;
        public int numberOfETDscans { get; set; } = 0;
        public int numberOfUVPDscans { get; set; } = 0;
        public int numberScansCountedLikelyGlyco_hcd { get; set; } = 0;
        public int numberScansCountedLikelyGlyco_etd { get; set; } = 0;
        public int numberScansCountedLikelyGlyco_uvpd { get; set; } = 0;
        public bool firstSpectrumInFile { get; set; } = true;
        public bool likelyGlycoSpectrum { get; set; } = false;
        public double nce { get; set; } = 0.0;
        public double halfTotalList { get; set; }

        //used for ynaught
        public int numberOfMS2scansWithYions { get; set; } = 0;
        public int numberOfMS2scansWithY0 { get; set; } = 0;
        public int numberOfMS2scansWithIntactGlycoPep { get; set; } = 0;
        public int numberOfMS2scansWithYions_hcd { get; set; } = 0;
        public int numberOfMS2scansWithY0_hcd { get; set; } = 0;
        public int numberOfMS2scansWithIntactGlycoPep_hcd { get; set; } = 0;
        public int numberOfMS2scansWithYions_etd { get; set; } = 0;
        public int numberOfMS2scansWithY0_etd { get; set; } = 0;
        public int numberOfMS2scansWithIntactGlycoPep_etd { get; set; } = 0;
    }

    public class CalculatedRawFileInfo
    {
        public int numberofMS2scansWithOxo { get; set; }
        public int numberofHCDscansWithOxo { get; set; }
        public int numberofETDscansWithOxo { get; set; }
        public int numberofUVPDscansWithOxo { get; set; }
        public double percentageHCD { get; set; }
        public double percentageETD { get; set; }
        public double percentageUVPD { get; set; }
        public double percentageSum { get; set; }
        public double percentageSum_hcd { get; set; }
        public double percentageSum_etd { get; set; }
        public double percentageSum_uvpd { get; set; }
        public int numberScansCountedLikelyGlyco_total { get; set; }
        public double percentageLikelyGlyco_total { get; set; }
        public double percentageLikelyGlyco_hcd { get; set; }
        public double percentageLikelyGlyco_etd { get; set; }
        public double percentageLikelyGlyco_uvpd { get; set; }
        public double percentage1ox { get; set; }
        public double percentage1ox_hcd { get; set; }
        public double percentage1ox_etd { get; set; }
        public double percentage1ox_uvpd { get; set; }
        public double percentage2ox { get; set; }
        public double percentage2ox_hcd { get; set; }
        public double percentage2ox_etd { get; set; }
        public double percentage2ox_uvpd { get; set; }
        public double percentage3ox { get; set; }
        public double percentage3ox_hcd { get; set; }
        public double percentage3ox_etd { get; set; }
        public double percentage3ox_uvpd { get; set; }
        public double percentage4ox { get; set; }
        public double percentage4ox_hcd { get; set; }
        public double percentage4ox_etd { get; set; }
        public double percentage4ox_uvpd { get; set; }
        public double percentage5plusox { get; set; }
        public double percentage5plusox_hcd { get; set; }
        public double percentage5plusox_etd { get; set; }
        public double percentage5plusox_uvpd { get; set; }



        public CalculatedRawFileInfo(RawFileInfo i)
        {
            numberofMS2scansWithOxo = i.numberOfMS2scansWithOxo_1 + i.numberOfMS2scansWithOxo_2 + i.numberOfMS2scansWithOxo_3 + i.numberOfMS2scansWithOxo_4 + i.numberOfMS2scansWithOxo_5plus;
            numberofHCDscansWithOxo = i.numberOfMS2scansWithOxo_1_hcd + i.numberOfMS2scansWithOxo_2_hcd + i.numberOfMS2scansWithOxo_3_hcd + i.numberOfMS2scansWithOxo_4_hcd + i.numberOfMS2scansWithOxo_5plus_hcd;
            numberofETDscansWithOxo = i.numberOfMS2scansWithOxo_1_etd + i.numberOfMS2scansWithOxo_2_etd + i.numberOfMS2scansWithOxo_3_etd + i.numberOfMS2scansWithOxo_4_etd + i.numberOfMS2scansWithOxo_5plus_etd;
            numberofUVPDscansWithOxo = i.numberOfMS2scansWithOxo_1_uvpd + i.numberOfMS2scansWithOxo_2_uvpd + i.numberOfMS2scansWithOxo_3_uvpd + i.numberOfMS2scansWithOxo_4_uvpd + i.numberOfMS2scansWithOxo_5plus_uvpd;

            percentageHCD = (double)i.numberOfHCDscans / (double)i.numberOfMS2scans * 100;
            percentageETD = (double)i.numberOfETDscans / (double)i.numberOfMS2scans * 100;
            percentageUVPD = (double)i.numberOfETDscans / (double)i.numberOfMS2scans * 100;

            percentageSum = numberofMS2scansWithOxo / (double)i.numberOfMS2scans * 100;
            percentageSum_hcd = numberofHCDscansWithOxo / (double)i.numberOfHCDscans * 100;
            percentageSum_etd = numberofETDscansWithOxo / (double)i.numberOfETDscans * 100;
            percentageSum_uvpd = numberofUVPDscansWithOxo / (double)i.numberOfUVPDscans * 100;

            numberScansCountedLikelyGlyco_total = i.numberScansCountedLikelyGlyco_hcd + i.numberScansCountedLikelyGlyco_etd + i.numberScansCountedLikelyGlyco_uvpd;
            percentageLikelyGlyco_total = (double)numberScansCountedLikelyGlyco_total / i.numberOfMS2scans * 100;
            percentageLikelyGlyco_hcd = (double)i.numberScansCountedLikelyGlyco_hcd / i.numberOfHCDscans * 100;
            percentageLikelyGlyco_etd = (double)i.numberScansCountedLikelyGlyco_etd / i.numberOfETDscans * 100;
            percentageLikelyGlyco_uvpd = (double)i.numberScansCountedLikelyGlyco_uvpd / i.numberOfUVPDscans * 100;

            percentage1ox = i.numberOfMS2scansWithOxo_1 / (double)i.numberOfMS2scans * 100;
            percentage1ox_hcd = i.numberOfMS2scansWithOxo_1_hcd / (double)i.numberOfHCDscans * 100;
            percentage1ox_etd = i.numberOfMS2scansWithOxo_1_etd / (double)i.numberOfETDscans * 100;
            percentage1ox_uvpd = i.numberOfMS2scansWithOxo_1_uvpd / (double)i.numberOfUVPDscans * 100;

            percentage2ox = i.numberOfMS2scansWithOxo_2 / (double)i.numberOfMS2scans * 100;
            percentage2ox_hcd = i.numberOfMS2scansWithOxo_2_hcd / (double)i.numberOfHCDscans * 100;
            percentage2ox_etd = i.numberOfMS2scansWithOxo_2_etd / (double)i.numberOfETDscans * 100;
            percentage2ox_uvpd = i.numberOfMS2scansWithOxo_2_uvpd / (double)i.numberOfUVPDscans * 100;

            percentage3ox = i.numberOfMS2scansWithOxo_3 / (double)i.numberOfMS2scans * 100;
            percentage3ox_hcd = i.numberOfMS2scansWithOxo_3_hcd / (double)i.numberOfHCDscans * 100;
            percentage3ox_etd = i.numberOfMS2scansWithOxo_3_etd / (double)i.numberOfETDscans * 100;
            percentage3ox_uvpd = i.numberOfMS2scansWithOxo_3_uvpd / (double)i.numberOfUVPDscans * 100;

            percentage4ox = i.numberOfMS2scansWithOxo_4 / (double)i.numberOfMS2scans * 100;
            percentage4ox_hcd = i.numberOfMS2scansWithOxo_4_hcd / (double)i.numberOfHCDscans * 100;
            percentage4ox_etd = i.numberOfMS2scansWithOxo_4_etd / (double)i.numberOfETDscans * 100;
            percentage4ox_uvpd = i.numberOfMS2scansWithOxo_4_uvpd / (double)i.numberOfUVPDscans * 100;

            percentage5plusox = i.numberOfMS2scansWithOxo_5plus / (double)i.numberOfMS2scans * 100;
            percentage5plusox_hcd = i.numberOfMS2scansWithOxo_5plus_hcd / (double)i.numberOfHCDscans * 100;
            percentage5plusox_etd = i.numberOfMS2scansWithOxo_5plus_etd / (double)i.numberOfETDscans * 100;
            percentage5plusox_uvpd = i.numberOfMS2scansWithOxo_5plus_uvpd / (double)i.numberOfUVPDscans * 100;
        }
    }
}
