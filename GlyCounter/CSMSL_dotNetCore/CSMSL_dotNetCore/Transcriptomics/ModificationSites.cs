// Copyright 2019 Dain R. Brademan
// 
// This file (ModificationSites.cs) is part of CSMSL.Transcriptomics.
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

namespace CSMSL.Transcriptomics
{
    [Flags]
    public enum ModificationSites
    {
        // bit shift operations to quickly flag relevant modifications
        None = 0,
        A = 1 << 0,
        C = 1 << 1,
        G = 1 << 2,
        U = 1 << 3,
        FivePrimeTerminus = 1 << 4,
        ThreePrimeTerminus = 1 << 5,
        All = (1 << 6) - 1, // Handy way of setting all below the 5th bit
        Any = 1 << 7 // Acts like none, but is equal to all
    }

    public static class ModificationSiteExtensions
    {
        public static ModificationSites Set(this ModificationSites sites, char nucleicacid)
        {
            Nucleotide na;
            if (Nucleotide.TryGetResidue(nucleicacid, out na))
            {
                sites |= na.Site;
            }
            return sites;
        }

        public static ModificationSites Set(this ModificationSites sites, Nucleotide nucleicAcid)
        {
            if (nucleicAcid != null)
                sites |= nucleicAcid.Site;

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
