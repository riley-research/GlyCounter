// Copyright 2023 Dain R. Brademan
// 
// This file (Modification.cs) is part of CSMSL.Glycomics.
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

using System;
using System.Collections.Generic;
using CSMSL.Chemistry;

namespace CSMSL.Glycomics
{
    public class Modification : IMass, IEquatable<Modification>
    {
        /// <summary>
        /// The default empty modification
        /// </summary>
        public static readonly Modification Empty = new Modification();

        /// <summary>
        /// The name of the modification
        /// </summary>
        public string Name { get; protected set; }

        /// <summary>
        /// The monoisotopic mass of the modification, commoningly known as the delta mass
        /// </summary>
        public double MonoisotopicMass { get; protected set; }

        /// <summary>
        /// The potentially modified sites of this modification
        /// </summary>
        public ModificationSites Sites { get; set; }

        /// <summary>
        /// Displays the name of the mod and the sites it modified in a formated string
        /// </summary>
        public string NameAndSites
        {
            get { return string.Format("{0} ({1})", Name, Sites); }
        }

        public Modification(Modification modification)
            : this(modification.MonoisotopicMass, modification.Name, modification.Sites)
        {
        }

        public Modification(double monoMass = 0.0, string name = "", ModificationSites sites = ModificationSites.Any)
        {
            MonoisotopicMass = monoMass;
            Name = name;
            Sites = sites;
        }

        public override string ToString()
        {
            return Name;
        }

        internal IEnumerable<int> GetModifiableSites(GlycanPolymer glycanPolymer)
        {
            if (Sites == ModificationSites.None || glycanPolymer == null)
                yield break;

            int i = 1;
            foreach (GlycanMonomer gm in glycanPolymer.GlycanMonomers)
            {
                if ((Sites & gm.Site) == gm.Site)
                    yield return i;
                i++;
            }
        }

        public override int GetHashCode()
        {
            return MonoisotopicMass.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            Modification modObj = obj as Modification;
            return modObj != null && Equals(modObj);
        }

        public bool Equals(Modification other)
        {
            if (other == null)
                return false;

            if (ReferenceEquals(this, other))
                return true;

            if (!this.MassEquals(other))
                return false;

            if (!Name.Equals(other.Name))
                return false;

            if (!Sites.Equals(other.Sites))
                return false;

            return true;
        }
    }
}
