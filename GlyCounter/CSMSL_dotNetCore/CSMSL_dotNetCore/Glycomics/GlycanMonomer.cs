// Copyright 2019 Dain R. Brademan
// 
// This file (Nucleotide.cs) is part of CSMSL.Transcriptomics.
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
using System;
using System.Collections.Generic;
using System.Globalization;

namespace CSMSL.Glycomics
{
    public class GlycanMonomer : IGlycanMonomer
    {
        #region The Generic Forms of 5- and 6-Member Glycans

        public static GlycanMonomer Hex { get; private set; }

        public static GlycanMonomer HexN { get; private set; }

        public static GlycanMonomer HexA { get; private set; }

        public static GlycanMonomer HexNAc { get; private set; }

        public static GlycanMonomer Pent { get; private set; }

        #endregion The Generic Forms of 5- and 6-Member Glycans

        private static readonly Dictionary<string, GlycanMonomer> Residues;

        private static readonly GlycanMonomer[] ResiduesByString;

        public static GlycanMonomer AddResidue(string name, string glycanShorthandAbbreviation, string CharmmResidueName, string chemicalFormula)
        {
            var residue = new GlycanMonomer(name, glycanShorthandAbbreviation, CharmmResidueName, chemicalFormula);
            AddResidueToDictionary(residue);
            return residue;
        }

        /// <summary>
        /// Get the residue based on the residues's symbol
        /// </summary>
        /// <param name="symbol"></param>
        /// <returns></returns>
        public static GlycanMonomer GetResidue(string symbol)
        {
            return Residues[symbol];
        }

        public static bool TryGetResidue(string symbol, out GlycanMonomer residue)
        {
            return Residues.TryGetValue(symbol, out residue);
        }

        /// <summary>
        /// Construct the actual glycan monomers
        /// </summary>
        static GlycanMonomer()
        {
            Residues = new Dictionary<string, GlycanMonomer>(100);
            ResiduesByString = new GlycanMonomer[100];
            Hex = AddResidue("Hexose", "Hex", "HEX", "C6H12O6");
            HexN = AddResidue("Hexosamine", "HexN", "HEXN", "C6H13NO5");
            HexA = AddResidue("n-acetyl-hexosamine", "HexNAc", "HEXNAC", "C8H15NO6");
            HexNAc = AddResidue("Hexuronic Acid", "HexA", "HEXA", "C6H10O7");
            Pent = AddResidue("Pentose", "Pent", "PENT", "C5H10O5");
        }

        private static void AddResidueToDictionary(GlycanMonomer residue)
        {
            Residues.Add(residue.ShorthandSymbol.ToString(CultureInfo.InvariantCulture), residue);
            Residues.Add(residue.Name, residue);
            Residues.Add(residue.CharmmSymbol, residue);
        }

        internal GlycanMonomer(string name, string shorthandSymbol, string charmmSymbol, ChemicalFormula chemicalFormula)
        {
            Name = name;
            ShorthandSymbol = shorthandSymbol;
            CharmmSymbol = charmmSymbol;
            ChemicalFormula = chemicalFormula;
            MonoisotopicMass = ChemicalFormula.MonoisotopicMass;
        }

        public ChemicalFormula ChemicalFormula { get; private set; }

        public string ShorthandSymbol { get; private set; }

        public ModificationSites Site { get; private set; }

        public double MonoisotopicMass { get; private set; }

        public string Name { get; private set; }

        public string CharmmSymbol { get; private set; }

        public override string ToString()
        {
            return string.Format("{0} {1} ({2})", ShorthandSymbol, CharmmSymbol, Name);
        }
    }
}
