using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlyCounter
{
    public class GlyCounterSettings
    {
        public string outputPath { get; set; } = "";
        public ObservableCollection<string> fileList { get; set; } = [];
        public HashSet<OxoniumIon> oxoniumIonHashSet { get; set; } = new HashSet<OxoniumIon>();
        public string filePath { get; set; } = "";
        public string defaultOutput { get; set; } = @"C:\";
        public string csvCustomFile { get; set; } = "empty";
        public double daTolerance { get; set; } = 1;
        public double ppmTolerance { get; set; } = 15;
        public double SNthreshold { get; set; } = 3;
        public double peakDepthThreshold_hcd { get; set; } = 25;
        public double peakDepthThreshold_etd { get; set; } = 50;
        public double peakDepthThreshold_uvpd { get; set; } = 25;
        public int arbitraryPeakDepthIfNotFound { get; set; } = 10000;
        public double oxoTICfractionThreshold_hcd { get; set; } = 0.20;
        public double oxoTICfractionThreshold_etd { get; set; } = 0.05;
        public double oxoTICfractionThreshold_uvpd { get; set; } = 0.20;
        public double oxoCountRequirement_hcd_user { get; set; } = 0;
        public double oxoCountRequirement_etd_user { get; set; } = 0;
        public double oxoCountRequirement_uvpd_user { get; set; } = 0;
        public double intensityThreshold { get; set; } = 1000;
        public double tol { get; set; } = 0;
        public bool using204 { get; set; } = false;
        public bool ipsa { get; set; } = false;
        public bool usingda { get; set; } = false;
        public decimal msLevelLB { get; set; } = 2;
        public decimal msLevelUB { get; set; } = 2;
        public bool ignoreMSLevel { get; set; } = false;
    }
    public class YnaughtSettings 
    {
        public HashSet<Yion> yIonHashSet { get; set; } = new HashSet<Yion>();
        public string pepIDFilePath { get; set; } = "";
        public string rawFilePath { get; set; } = "";
        public double tol { get; set; } = 0;
        public double SNthreshold { get; set; } = 3;
        public double intensityThreshold { get; set; } = 1000;
        public double ppmTolerance { get; set; } = 0;
        public double daTolerance { get; set; } = 0;
        public string chargeLB { get; set; } = "1";
        public string chargeUB { get; set; } = "P";
        public string csvCustomAdditions { get; set; } = "empty";
        public string csvCustomSubtractions { get; set; } = "empty";
        public bool condenseChargeStates { get; set; } = true;
        public bool ipsa { get; set; } = false;
        public bool usingda { get; set; } = false;
        public bool firstIsotope { get; set; } = false;
        public bool secondIsotope { get; set; } = false;
    }
}
