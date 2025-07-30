// Copyright 2023 Dain R. Brademan
// 
// This file (ModificationSites.cs) is part of CSMSL.Glycomics.
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
using System.Linq;
using System.Text;

namespace CSMSL.Glycomics
{
    [Flags]
    public enum ModificationSites
    {
        // bit shift operations to quickly flag relevant modifications
        // basically equivalent to a 32 bit array
        //      0/1 is used to indicate if AA is modified
        // this is unnecessarily optimized.
        None = 0,
        Hex = 1 << 0,
        HexN = 1 << 1,
        HexNAc = 1 << 2,
        HexA = 1 << 3,
        Pent = 1 << 4,
        Neu5Ac = 1 << 5,
        Neu5Gc = 1 << 6,
        Neu = 1 << 7,
        Kdn = 1 << 8,
        All = (1 << 9) - 1, // Handy way of setting all below the 9th bit
        Any = 1 << 31 // Acts like none, but is equal to all
    }

    public static class ModificationSiteExtensions
    {
        public static ModificationSites Set(this ModificationSites sites, string glycanMonomer)
        {
            GlycanMonomer na;
            if (GlycanMonomer.TryGetResidue(glycanMonomer, out na))
            {
                sites |= na.Site;
            }
            return sites;
        }

        public static ModificationSites Set(this ModificationSites sites, GlycanMonomer glycanMonomer)
        {
            if (glycanMonomer != null)
                sites |= glycanMonomer.Site;

            return sites;
        }

        public static IEnumerable<ModificationSites> GetActiveSites(this ModificationSites sites)
        {
            foreach (ModificationSites site in Enum.GetValues(typeof(ModificationSites)))
            {
                if (site == ModificationSites.None)
                {
                    continue;
                }
                if ((sites & site) == site)
                {
                    yield return site;
                }
            }
        }

        public static bool ContainsSite(this ModificationSites sites, ModificationSites otherSites)
        {
            // By convention, if the other site is 'Any', they are always equal
            if (otherSites == ModificationSites.Any)
                return true;

            if (otherSites == ModificationSites.None)
                return sites == ModificationSites.None;

            return sites == otherSites;
        }
    }
}
