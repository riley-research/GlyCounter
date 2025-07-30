// Copyright 2023 Dain R. Brademan
// 
// This file (RNA.cs) is part of CSMSL.Glycomics.
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

using System.Collections.Generic;
using System.Linq;

namespace CSMSL.Glycomics
{
    public class Glycan : GlycanPolymer
    {
        /// <summary>
        /// The glycan number this peptide is located in its parent
        /// </summary>
        public int StartResidue { get; set; }

        /// <summary>
        /// The glycan number this peptide is located in its parent
        /// </summary>
        public int EndResidue { get; set; }

        /// <summary>
        /// The glycan polymer this peptide came from
        /// </summary>
        public GlycanPolymer Parent { get; set; }

        /// <summary>
        /// The preceding glycan monomer in its parent
        /// </summary>
        public GlycanMonomer PreviousGlycanMonomer { get; set; }

        /// <summary>
        /// The next nucleic acid in its parent
        /// </summary>
        public GlycanMonomer NextGlycanMonomer { get; set; }

        public Glycan()
        {
        }

        /// <summary>
        /// Create a new peptide based on another nucleic acid polymer
        /// </summary>
        /// <param name="glycanPolymer">The other nucleic acid polymer to copy</param>
        /// <param name="includeModifications">Whether to copy the modifications to the new peptide</param>
        public Glycan(GlycanPolymer glycanPolymer, bool includeModifications = true)
            : base(glycanPolymer, includeModifications)
        {
            Parent = glycanPolymer;
            StartResidue = 0;
            EndResidue = Length - 1;
        }

        public Glycan(GlycanPolymer glycanPolymer, int firstResidue, int length, bool includeModifications = true)
            : base(glycanPolymer, firstResidue, length, includeModifications)
        {
            Parent = glycanPolymer;
            StartResidue = firstResidue;
            EndResidue = firstResidue + length - 1;
            PreviousGlycanMonomer = glycanPolymer.GetResidue(StartResidue - 1);
            NextGlycanMonomer = glycanPolymer.GetResidue(EndResidue + 1);
        }

        public Glycan(string sequence)
            : this(sequence, null, 0)
        {
        }

        public Glycan(string sequence, GlycanPolymer parent)
            : this(sequence, parent, 0)
        {
        }

        public Glycan(string sequence, GlycanPolymer parent, int startResidue)
            : base(sequence)
        {
            Parent = parent;
            StartResidue = startResidue;
            EndResidue = startResidue + Length - 1;

            if (parent != null)
            {
                if (StartResidue > 0)
                    PreviousGlycanMonomer = parent.GlycanMonomers[StartResidue - 1];

                if (EndResidue < parent.Length - 1)
                    NextGlycanMonomer = parent.GlycanMonomers[EndResidue + 1];
            }
        }

        public Glycan GetSubGlycan(int firstResidue, int length)
        {
            return new Glycan(this, firstResidue, length);
        }

        public new bool Equals(GlycanPolymer other)
        {
            return base.Equals(other);
        }
    }

    internal class ModificationArrayComparer : IEqualityComparer<Modification[]>
    {
        public bool Equals(Modification[] x, Modification[] y)
        {
            int length = x.Length;
            if (length != y.Length)
                return false;
            for (int i = 0; i < length; i++)
            {
                Modification a = x[i];
                Modification b = y[i];
                if (a == null)
                {
                    if (b != null)
                        return false;
                }
                else
                {
                    if (!a.Equals(b))
                        return false;
                }
            }
            return true;
        }

        public int GetHashCode(Modification[] obj)
        {
            unchecked
            {
                const int p = 16777619;
                int hash = obj.Where(t => t != null).Aggregate((int)2166136261, (current, t) => (current ^ t.GetHashCode()) * p);
                hash += hash << 13;
                hash ^= hash >> 7;
                hash += hash << 3;
                hash ^= hash >> 17;
                hash += hash << 5;
                return hash;
            }
        }
    }
}
