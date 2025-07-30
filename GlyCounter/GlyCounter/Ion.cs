﻿namespace GlyCounter
{
    public class Ion : IEquatable<Ion>
    {
        public required double theoMZ { get; set; }
        public double measuredMZ { get; set; }
        public string description { get; set; }
        public double intensity { get; set; }
        public string ionSource { get; set; }
        public int peakDepth { get; set; }
        public int hcdCount { get; set; }
        public int etdCount { get; set; }
        public int uvpdCount { get; set; }


        public bool Equals(Ion? other)
        {
            if (other == null) return false;
            return theoMZ == other.theoMZ;
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as Ion);
        }

        public override int GetHashCode()
        {
            return theoMZ.GetHashCode();
        }
    }
}
