// Copyright 2019 Dain R. Brademan
// 
// This file (ChemicalFormulaFragment.cs) is part of CSMSL.Transcriptomics.
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

namespace CSMSL.Transcriptomics
{
    public class ChemicalFormulaFragment : Fragment, IChemicalFormula
    {
        public ChemicalFormula ChemicalFormula { get; private set; }

        public ChemicalFormulaFragment(FragmentTypes type, int number, string chemicalFormula, NucleicAcid parent)
            : this(type, number, new ChemicalFormula(chemicalFormula), parent)
        {
        }

        public ChemicalFormulaFragment(FragmentTypes type, int number, ChemicalFormula formula, NucleicAcid parent)
            : base(type, number, formula.MonoisotopicMass, parent)
        {
            ChemicalFormula = new ChemicalFormula(formula);
        }
    }
}
