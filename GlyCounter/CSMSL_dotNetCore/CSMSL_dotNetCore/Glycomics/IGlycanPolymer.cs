// Copyright 2023 Dain R. Brademan
// 
// This file (IGlycan.cs) is part of CSMSL.Glycomics.
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

namespace CSMSL.Glycomics
{
    public interface IGlycanPolymer
    {
        /// <summary>
        /// The glycan monomer sequence
        /// </summary>
        string Sequence { get; }

        /// <summary>
        /// The total number of glycan monomers in the glycan tree
        /// </summary>
        int Length { get; }
    }
}
