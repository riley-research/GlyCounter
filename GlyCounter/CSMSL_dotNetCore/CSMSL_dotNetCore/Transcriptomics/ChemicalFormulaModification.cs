// Copyright 2019 Dain R. Brademan
// 
// This file (ChemicalFormulamodification.cs) is part of CSMSL.Transcriptomics.
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

using CSMSL.Chemistry;
using System.Xml.Linq;

namespace CSMSL.Transcriptomics
{
    public class ChemicalFormulaModification : Modification, IChemicalFormula
    {
        /// <summary>
        /// The Chemical Formula of this modifications
        /// </summary>
        public ChemicalFormula ChemicalFormula { get; private set; }

        public ChemicalFormulaModification(string chemicalFormula, ModificationSites sites = ModificationSites.Any)
            : this(new ChemicalFormula(chemicalFormula), "", sites)
        {
            Name = ChemicalFormula.ToString();
        }

        public ChemicalFormulaModification(string chemicalFormula, string name, ModificationSites sites = ModificationSites.Any)
            : this(new ChemicalFormula(chemicalFormula), name, sites)
        {
        }

        public ChemicalFormulaModification(ChemicalFormula chemicalFormula, string name, ModificationSites sites = ModificationSites.Any)
            : base(chemicalFormula.MonoisotopicMass, name, sites)
        {
            ChemicalFormula = chemicalFormula;
        }

        public ChemicalFormulaModification(ChemicalFormulaModification other)
            : this(new ChemicalFormula(other.ChemicalFormula), other.Name, other.Sites)
        {
        }
    }
}
