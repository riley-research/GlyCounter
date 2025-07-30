// Copyright 2019 Dain R. Brademan
// 
// This file (RNA.cs) is part of CSMSL.Transcriptomics.
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

namespace CSMSL.Transcriptomics
{
    public class RNA : NucleicAcid
    {
        /// <summary>
        /// The nucleic acid number this peptide is located in its parent
        /// </summary>
        public int StartResidue { get; set; }

        /// <summary>
        /// The nucleic acid number this peptide is located in its parent
        /// </summary>
        public int EndResidue { get; set; }

        /// <summary>
        /// The nucleic acid polymer this peptide came from
        /// </summary>
        public NucleicAcid Parent { get; set; }

        /// <summary>
        /// The preceding nucleic acid in its parent
        /// </summary>
        public Nucleotide PreviousNucleicAcid { get; set; }

        /// <summary>
        /// The next nucleic acid in its parent
        /// </summary>
        public Nucleotide NextNucleicAcid { get; set; }

        public RNA()
        {
        }

        /// <summary>
        /// Create a new peptide based on another nucleic acid polymer
        /// </summary>
        /// <param name="nucleicAcidPolymer">The other nucleic acid polymer to copy</param>
        /// <param name="includeModifications">Whether to copy the modifications to the new peptide</param>
        public RNA(NucleicAcid nucleicAcidPolymer, bool includeModifications = true)
            : base(nucleicAcidPolymer, includeModifications)
        {
            Parent = nucleicAcidPolymer;
            StartResidue = 0;
            EndResidue = Length - 1;
        }

        public RNA(NucleicAcid nucleicAcidPolymer, int firstResidue, int length, bool includeModifications = true)
            : base(nucleicAcidPolymer, firstResidue, length, includeModifications)
        {
            Parent = nucleicAcidPolymer;
            StartResidue = firstResidue;
            EndResidue = firstResidue + length - 1;
            PreviousNucleicAcid = nucleicAcidPolymer.GetResidue(StartResidue - 1);
            NextNucleicAcid = nucleicAcidPolymer.GetResidue(EndResidue + 1);
        }

        public RNA(string sequence)
            : this(sequence, null, 0)
        {
        }

        public RNA(string sequence, NucleicAcid parent)
            : this(sequence, parent, 0)
        {
        }

        public RNA(string sequence, NucleicAcid parent, int startResidue)
            : base(sequence)
        {
            Parent = parent;
            StartResidue = startResidue;
            EndResidue = startResidue + Length - 1;

            if (parent != null)
            {
                if (StartResidue > 0)
                    PreviousNucleicAcid = parent.NucleicAcids[StartResidue - 1];

                if (EndResidue < parent.Length - 1)
                    NextNucleicAcid = parent.NucleicAcids[EndResidue + 1];
            }
        }

        public RNA GetSubPeptide(int firstResidue, int length)
        {
            return new RNA(this, firstResidue, length);
        }

        public new bool Equals(NucleicAcid other)
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
