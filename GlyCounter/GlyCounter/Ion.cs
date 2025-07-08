using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlyCounter
{
    public class Ion : IEquatable<Ion>
    {
        public double theoMZ { get; set; }
        public double measuredMZ { get; set; }
        public string description { get; set; }
        public double intensity { get; set; }
        public string ionSource { get; set; }
        public int peakDepth { get; set; }
        public int hcdCount { get; set; }
        public int etdCount { get; set; }
        public int uvpdCount { get; set; }

        public bool Equals(Ion other)
        {
            if (other == null) return false;

            return this.theoMZ == other.theoMZ || this.description == other.description;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType()) return false;
            return Equals(obj as Ion);
        }

        public override int GetHashCode()
        {
            return (theoMZ.GetHashCode() * 397) ^ (description?.GetHashCode() ?? 0);
        }
    }
}
