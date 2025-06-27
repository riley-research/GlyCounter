using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlyCounter
{
    internal class Yion
    {
        public double theoMass { get; set; }
        public string description { get; set; }
        public List<double> intensities { get; set; } = [];
        public List<double> mz { get; set; } = [];
        public string glycanSource { get; set; }
        public int hcdCount { get; set; }
        public int etdCount { get; set; }
        public List<int> chargeStates { get; set; } = [];
    }
}
