using CSMSL.Chemistry;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace CSMSL.Transcriptomics
{
    public class RefactoredNucleotide : INucleotide
    {
        #region The Four Common RNA Bases

        public static RefactoredNucleotide Adenosine { get; private set; }

        public static RefactoredNucleotide Cytidine { get; private set; }

        public static RefactoredNucleotide Guanosine { get; private set; }

        public static RefactoredNucleotide Uridine { get; private set; }

        public static RefactoredNucleotide dThymine { get; private set; }

        #endregion The Four Common RNA Bases

        private static readonly Dictionary<string, RefactoredNucleotide> Residues;

        private static readonly RefactoredNucleotide[] ResiduesByLetter;

        public static RefactoredNucleotide AddResidue(string name, char oneLetterAbbreviation, string threeLetterAbbreviation, string chemicalFormula)
        {
            var residue = new RefactoredNucleotide(name, oneLetterAbbreviation, threeLetterAbbreviation, chemicalFormula);
            AddResidueToDictionary(residue);
            return residue;
        }

        /// <summary>
        /// Get the residue based on the residues's symbol
        /// </summary>
        /// <param name="symbol"></param>
        /// <returns></returns>
        public static RefactoredNucleotide GetResidue(string symbol)
        {
            return symbol.Length == 1 ? ResiduesByLetter[symbol[0]] : Residues[symbol];
        }

        /// <summary>
        /// Gets the resdiue based on the residue's one-character symbol
        /// </summary>
        /// <param name="letter"></param>
        /// <returns></returns>
        public static RefactoredNucleotide GetResidue(char letter)
        {
            return ResiduesByLetter[letter];
        }

        public static bool TryGetResidue(char letter, out RefactoredNucleotide residue)
        {
            residue = null;
            if (letter > 'z' || letter < 0)
                return false;
            residue = ResiduesByLetter[letter];
            return residue != null;
        }

        public static bool TryGetResidue(string symbol, out RefactoredNucleotide residue)
        {
            return Residues.TryGetValue(symbol, out residue);
        }

        /// <summary>
        /// Construct the actual nucleic acids
        /// </summary>
        static RefactoredNucleotide()
        {
            Residues = new Dictionary<string, RefactoredNucleotide>(66);
            ResiduesByLetter = new RefactoredNucleotide['z' + 1]; //Make it big enough for all the Upper and Lower characters
            Adenosine = AddResidue("Adenosine", 'A', "Ade", "C10H12N5O6P");
            Cytidine = AddResidue("Cytidine", 'C', "Cyt", "C9H12N3O7P");
            Guanosine = AddResidue("Guanosine", 'G', "Gua", "C10H12N5O7P");
            Uridine = AddResidue("Uridine", 'U', "Ura", "C9H11N2O8P");
            dThymine = AddResidue("sdaf;lkasjdf", 't', "dT", "");
        }

        private static void AddResidueToDictionary(RefactoredNucleotide residue)
        {
            Residues.Add(residue.Letter.ToString(CultureInfo.InvariantCulture), residue);
            Residues.Add(residue.Name, residue);
            Residues.Add(residue.Symbol, residue);
            ResiduesByLetter[residue.Letter] = residue;
            ResiduesByLetter[Char.ToLower(residue.Letter)] = residue;
        }

        internal RefactoredNucleotide(string name, char oneLetterAbbreviation, string threeLetterAbbreviation, string chemicalFormula)
            : this(name, oneLetterAbbreviation, threeLetterAbbreviation, new ChemicalFormula(chemicalFormula))
        {
        }

        internal RefactoredNucleotide(string name, char oneLetterAbbreviation, string threeLetterAbbreviation, ChemicalFormula chemicalFormula)
        {
            Name = name;
            Letter = oneLetterAbbreviation;
            Symbol = threeLetterAbbreviation;
            ChemicalFormula = chemicalFormula;
            MonoisotopicMass = ChemicalFormula.MonoisotopicMass;
        }

        public ChemicalFormula ChemicalFormula { get; private set; }

        public char Letter { get; private set; }

        public ModificationSites Site { get; private set; }

        public double MonoisotopicMass { get; private set; }

        public string Name { get; private set; }

        public string Symbol { get; private set; }

        public override string ToString()
        {
            return string.Format("{0} {1} ({2})", Letter, Symbol, Name);
        }
    }
}
