// Copyright 2023 Dain R. Brademan
// 
// This file (NucleicAcid.cs) is part of CSMSL.Glycomics.
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
using CSMSL.Chemistry;

namespace CSMSL.Glycomics
{
    /// <summary>
    /// A linear representation of glycans. Implementation should really be tree-based, but eh.
    /// </summary>
    public abstract class GlycanPolymer : IEquatable<GlycanPolymer>, IMass, IGlycanPolymer
    {
        #region Static Properties

        /// <summary>
        /// The default chemical formula of the formula loss when a bond is made to a glycan (typically water loss)
        /// </summary>
        public static readonly ChemicalFormula DefaultGlycanBondLoss = new ChemicalFormula("O-1H-2");

        /// <summary>
        /// Defines if newly generated Glycan will store the glycan sequence as a string
        /// or generate the string dynamically. If true, certain operations will be quicker at the cost of
        /// increased memory consumption. Default value is True.
        /// </summary>
        public static bool StoreSequenceString { get; set; }

        #endregion Static Properties

        #region Instance Variables

        /// <summary>
        /// The chemical formula loss that occurs  cap. This is different from the 5-prime terminus modification.
        /// </summary>
        private IChemicalFormula _GlycanBondLoss;

        /// <summary>
        /// TODO: Need to convert this to a tree-based structure.
        /// All of the modifications indexed by position
        /// </summary>
        private IMass[] _modifications;

        /// <summary>
        /// TODO: Convert this to a tree-based structure
        /// All of the glycan monomers indexed by position.
        /// </summary>
        private GlycanMonomer[] _glycanMonomers;

        /// <summary>
        /// The glycan sequence with modification names interspersed. Is ignored if 'StoreSequenceString' is false
        /// </summary>
        private string _sequenceWithMods;

        /// <summary>
        /// The glycan sequence. Is ignored if 'StoreSequenceString' is false
        /// </summary>
        private string _sequence;

        /// <summary>
        /// The internal flag to represent that the sequence with modifications have been changed and need to be updated
        /// </summary>
        internal bool IsDirty { get; set; }

        #endregion Instance Variables

        #region Constructors

        /// <summary>
        /// Static constructor, sets the default parameters for all glycans
        /// </summary>
        static GlycanPolymer()
        {
            StoreSequenceString = true;
        }

        protected GlycanPolymer()
            : this(string.Empty, DefaultGlycanBondLoss)
        {
        }

        protected GlycanPolymer(string sequence)
            : this(sequence, DefaultGlycanBondLoss)
        {
        }

        protected GlycanPolymer(string sequence, IChemicalFormula glycanBondLoss)
        {
            MonoisotopicMass = 0;
            Length = sequence.Count(character => character == '-') + 1;
            _glycanMonomers = new GlycanMonomer[Length];
            GlycanBondLossModifier = glycanBondLoss;
            ParseSequence(sequence);
        }

        protected GlycanPolymer(GlycanPolymer nucleicAcidPolymer, bool includeModifications = true)
            : this(nucleicAcidPolymer, 0, nucleicAcidPolymer.Length, includeModifications)
        {
        }

        protected GlycanPolymer(GlycanPolymer glycanPolymer, int firstResidue, int length, bool includeModifications = true)
        {
            if (firstResidue < 0 || firstResidue > glycanPolymer.Length)
                throw new IndexOutOfRangeException(string.Format("The first residue index is outside the valid range [{0}-{1}]", 0, glycanPolymer.Length));
            if (length + firstResidue > glycanPolymer.Length)
                throw new ArgumentOutOfRangeException("length", "The length + firstResidue value is too large");

            Length = length;
            _glycanMonomers = new GlycanMonomer[length];

            _GlycanBondLoss = glycanPolymer.GlycanBondLossModifier;

            double monoMass = 0;

            GlycanMonomer[] otherGlycanMonomers = glycanPolymer._glycanMonomers;

            if (includeModifications && glycanPolymer.ContainsModifications())
            {
                _modifications = new IMass[length];
                for (int i = 0; i < length; i++)
                {
                    var gm = otherGlycanMonomers[i + firstResidue];
                    _glycanMonomers[i] = gm;
                    monoMass += gm.MonoisotopicMass;

                    IMass mod = glycanPolymer._modifications[i + firstResidue + 1];
                    if (mod == null)
                        continue;

                    _modifications[i + 1] = mod;
                    monoMass += mod.MonoisotopicMass;
                }
            }
            else
            {
                for (int i = 0, j = firstResidue; i < length; i++, j++)
                {
                    var gm = _glycanMonomers[i] = otherGlycanMonomers[j];
                    monoMass += gm.MonoisotopicMass;
                }
            }

            MonoisotopicMass = monoMass;

            IsDirty = true;
        }

        #endregion Constructors

        #region Public Properties

        /// <summary>
        /// Gets or sets the default bond loss modifier for a GlycanPolymer
        /// </summary>
        public IChemicalFormula GlycanBondLossModifier
        {
            get { return _GlycanBondLoss; }
            set { ReplaceBondLossModifier(ref _GlycanBondLoss, value); }
        }

        /// <summary>
        /// Gets the number of nucleic acids in this nucleic acid polymer
        /// </summary>
        public int Length { get; private set; }

        /// <summary>
        /// The total monoisotopic mass of this peptide and all of its modifications
        /// </summary>
        public double MonoisotopicMass { get; private set; }

        #endregion Public Properties

        #region Internal Properties

        /// <summary>
        /// The internal data store for the modifications (same size as number of glycan monomers)
        /// </summary>
        internal IMass[] Modifications
        {
            get { return _modifications; }
        }

        /// <summary>
        /// The internal data store for the glycan monomers
        /// </summary>
        internal GlycanMonomer[] GlycanMonomers
        {
            get { return _glycanMonomers; }
        }

        #endregion Internal Properties

        #region Nucleic Acid Sequence

        public GlycanMonomer GetResidue(int position)
        {
            if (position < 0 || position >= Length)
                return null;
            return _glycanMonomers[position];
        }

        /// <summary>
        /// Checks if an glycan polymer residue with the value of 'residue' is contained in this polymer
        /// </summary>
        /// <param name="residue">The string represenation for the nucleic acid residue</param>
        /// <returns>True if any glycan polymer residue is the same as the specified string</returns>
        public bool Contains(string residue)
        {
            return _glycanMonomers.Any(gm => gm.ShorthandSymbol.Equals(residue));
        }

        /// <summary>
        /// Checks if the nucleic acid residue is contained in this polymer
        /// </summary>
        /// <param name="residue">The residue to check for</param>
        /// <returns>True if the polymer contains the specified residue, False otherwise</returns>
        public bool Contains(IGlycanMonomer residue)
        {
            return _glycanMonomers.Contains(residue);
        }

        /// <summary>
        /// Gets the base Glycan Polymer sequence
        /// </summary>
        public string Sequence
        {
            get
            {
                // Don't store the string if we don't have too, just recreate it on the fly
                if (!StoreSequenceString)
                {
                    return string.Join("-", _glycanMonomers.Select(na => na.ShorthandSymbol).ToArray());
                }

                // Generate the sequence if the stored version is null or empty
                if (string.IsNullOrEmpty(_sequence))
                {
                    _sequence = string.Join("-", _glycanMonomers.Select(na => na.ShorthandSymbol).ToArray());
                }

                return _sequence;
            }
        }

        /// <summary>
        /// Gets the nucleic acid sequence with modifications
        /// </summary>
        public string SequenceWithModifications
        {
            get
            {
                // Don't store the string if we don't have too, just recreate it on the fly
                if (!StoreSequenceString)
                    return GetSequenceWithModifications();

                if (!IsDirty && !string.IsNullOrEmpty(_sequenceWithMods))
                    return _sequenceWithMods;

                _sequenceWithMods = GetSequenceWithModifications();
                IsDirty = false;
                return _sequenceWithMods;
            }
        }

        public string GetSequenceWithModifications()
        {
            if (_modifications == null)
                return Sequence;

            StringBuilder modSeqSb = new StringBuilder(Length);

            IMass mod;

            // Handle Glycan Polymer Residues
            for (int i = 0; i < Length; i++)
            {
                modSeqSb.Append(_glycanMonomers[i].ShorthandSymbol);

                // Handle Glycan Monomer Modification (0-based)
                if ((mod = _modifications[i]) != null && !Modification.Empty.Equals(mod) && !mod.MassEquals(0))
                {
                    modSeqSb.Append('[');
                    modSeqSb.Append(mod);
                    modSeqSb.Append(']');
                }

                modSeqSb.Append("-");
            }

            return modSeqSb.ToString();
        }

        /// <summary>
        /// Gets the total number of glycan monomers in this glycan polymer
        /// </summary>
        /// <returns>The number of glycan monomer residues</returns>
        public int ResidueCount()
        {
            return Length;
        }

        /// <summary>
        /// Gets the total number of a specific glycan monomer in this glycan polymer
        /// </summary>
        /// <returns>The number of a specific glycan monomer residues</returns>
        public int ResidueCount(IGlycanPolymer glycanPolymer)
        {
            return glycanPolymer == null ? 0 : _glycanMonomers.Count(gmr => gmr.Equals(glycanPolymer));
        }

        /// <summary>
        /// Gets the number of glycan monomer residues in this glycan polymer that
        /// has the specified residue string
        /// </summary>
        /// <param name="residueString">The residue string to search for</param>
        /// <returns>The number of nucleic acid residues that have the same letter in this polymer</returns>
        public int ResidueCount(string residueString)
        {
            return _glycanMonomers.Count(gmr => gmr.ShorthandSymbol.Equals(residueString));
        }

        public int ResidueCount(string residueString, int index, int length)
        {
            return _glycanMonomers.SubArray(index, length).Count(gmr => gmr.ShorthandSymbol.Equals(residueString));
        }

        public int ResidueCount(IGlycanMonomer glycanMonomer, int index, int length)
        {
            return _glycanMonomers.SubArray(index, length).Count(gmr => gmr.Equals(glycanMonomer));
        }

        public int ElementCount(string element)
        {
            // Residues count
            int count = _glycanMonomers.Sum(gmr => gmr.ChemicalFormula.Count(element));
            // Modifications count (if the mod is a IChemicalFormula)
            if (_modifications != null)
                count += _modifications.Where(mod => mod is IChemicalFormula).Cast<IChemicalFormula>().Sum(mod => mod.ChemicalFormula.Count(element));
            return count;
        }

        public int ElementCount(Isotope isotope)
        {
            // Residues count
            int count = _glycanMonomers.Sum(gmr => gmr.ChemicalFormula.Count(isotope));
            // Modifications count (if the mod is a IChemicalFormula)
            if (_modifications != null)
                count += _modifications.Where(mod => mod is IChemicalFormula).Cast<IChemicalFormula>().Sum(mod => mod.ChemicalFormula.Count(isotope));
            return count;
        }

        public bool ContainsMonomer(IGlycanPolymer item)
        {
            return ContainsMonomer(item.Sequence);
        }

        public bool ContainsMonomer(string sequence)
        {
            return Sequence.Contains(sequence);
        }

        #endregion Nucleic Acid Sequence

        #region Fragmentation
        /// <summary>
        /// Calculates the fragments that are different between this and another nucleic acid polymer
        /// </summary>
        /// <param name="other"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public IEnumerable<Fragment> GetSiteDeterminingFragments(GlycanPolymer other, FragmentTypes type)
        {
            return GetSiteDeterminingFragments(this, other, type);
        }

        /// <summary>
        /// Calculates all the fragments of the types you specify
        /// </summary>
        /// <param name="types"></param>
        /// <param name="calculateChemicalFormula"></param>
        /// <returns></returns>
        public IEnumerable<Fragment> Fragment(FragmentTypes types, bool calculateChemicalFormula = false)
        {
            return Fragment(types, 1, Length - 1, calculateChemicalFormula);
        }

        public IEnumerable<Fragment> Fragment(FragmentTypes types, int number, bool calculateChemicalFormula = false)
        {
            return Fragment(types, number, number, calculateChemicalFormula);
        }

        public IEnumerable<Fragment> Fragment(FragmentTypes types, int min, int max, bool calculateChemicalFormula = false)
        {
            if (min > max)
                throw new ArgumentOutOfRangeException();

            if (min < 1 || max > Length - 1)
                throw new IndexOutOfRangeException();

            foreach (FragmentTypes type in types.GetIndividualFragmentTypes())
            {
                bool isChemicalFormula = calculateChemicalFormula;
                ChemicalFormula capFormula = type.GetIonCap();
                bool isThreePrimeTerminal = type.GetTerminus() == Terminus.ThreePrime;

                double monoMass = capFormula.MonoisotopicMass;
                ChemicalFormula formula = new ChemicalFormula(capFormula);

                IChemicalFormula terminus = new ChemicalFormula("");
                monoMass += terminus.MonoisotopicMass;

                if (isChemicalFormula)
                {
                    formula += terminus;
                }

                bool first = true;
                bool hasMod = _modifications != null;

                for (int i = 0; i <= max; i++)
                {
                    int naIndex = isThreePrimeTerminal ? Length - i : i - 1;

                    // Handle the terminus mods first in a special case
                    IMass mod;
                    if (first)
                    {
                        first = false;
                        if (hasMod)
                        {
                            mod = _modifications[naIndex + 1];
                            if (mod != null)
                            {
                                monoMass += mod.MonoisotopicMass;

                                if (isChemicalFormula)
                                {
                                    IChemicalFormula modFormula = mod as IChemicalFormula;
                                    if (modFormula != null)
                                    {
                                        formula.Add(modFormula);
                                    }
                                    else
                                    {
                                        isChemicalFormula = false;
                                    }
                                }
                            }
                        }
                        continue;
                    }

                    monoMass += _glycanMonomers[naIndex].MonoisotopicMass;
                    formula.Add(_glycanMonomers[naIndex]);

                    if (hasMod)
                    {
                        mod = _modifications[naIndex + 1];

                        if (mod != null)
                        {
                            monoMass += mod.MonoisotopicMass;

                            if (isChemicalFormula)
                            {
                                IChemicalFormula modFormula = mod as IChemicalFormula;
                                if (modFormula != null)
                                {
                                    formula.Add(modFormula);
                                }
                                else
                                {
                                    isChemicalFormula = false;
                                }
                            }
                        }
                    }

                    if (i < min)
                        continue;

                    if (isChemicalFormula)
                    {
                        yield return new ChemicalFormulaFragment(type, i, formula, this);
                    }
                    else
                    {
                        yield return new Fragment(type, i, monoMass, this);
                    }
                }
            }
        }

        #endregion Fragmentation

        #region Modifications

        public bool ContainsModifications()
        {
            return _modifications != null && _modifications.Any(m => m != null);
        }

        public IMass[] GetModifications()
        {
            IMass[] mods = new IMass[Length + 2];
            if (_modifications != null)
                Array.Copy(_modifications, mods, _modifications.Length);
            return mods;
        }

        public ISet<T> GetUniqueModifications<T>() where T : IMass
        {
            HashSet<T> uniqueMods = new HashSet<T>();

            if (_modifications == null)
                return uniqueMods;

            foreach (IMass mod in _modifications)
            {
                if (mod is T)
                    uniqueMods.Add((T)mod);
            }
            return uniqueMods;
        }

        /// <summary>
        /// Gets or sets the modification of the 3' terminus on this nucleic acid polymer
        /// </summary>
        public IMass ThreePrimeTerminusModification
        {
            get { return GetModification(Length + 1); }
            set { ReplaceMod(Length + 1, value); }
        }

        /// <summary>
        /// Gets or sets the modification of the 3' terminus on this nucleic acid polymer
        /// </summary>
        public IMass FivePrimeTerminusModification
        {
            get { return GetModification(0); }
            set { ReplaceMod(0, value); }
        }

        /// <summary>
        /// Counts the total number of modifications on this polymer that are not null
        /// </summary>
        /// <returns>The number of modifications</returns>
        public int ModificationCount()
        {
            return _modifications == null ? 0 : _modifications.Count(mod => mod != null);
        }

        /// <summary>
        /// Counts the total number of the specified modification on this polymer
        /// </summary>
        /// <param name="modification">The modification to count</param>
        /// <returns>The number of modifications</returns>
        public int ModificationCount(IMass modification)
        {
            if (modification == null || _modifications == null)
                return 0;

            return _modifications.Count(modification.Equals);
        }

        /// <summary>
        /// Determines if the specified modification exists in this polymer
        /// </summary>
        /// <param name="modification">The modification to look for</param>
        /// <returns>True if the modification is found, false otherwise</returns>
        public bool Contains(IMass modification)
        {
            if (modification == null || _modifications == null)
                return false;

            return _modifications.Contains(modification);
        }

        /// <summary>
        /// Get the modification at the given residue number
        /// </summary>
        /// <param name="residueNumber">The nucleic acid residue number</param>
        /// <returns>The modification at the site, null if there isn't any modification present</returns>
        public IMass GetModification(int residueNumber)
        {
            return _modifications == null ? null : _modifications[residueNumber];
        }

        public bool TryGetModification(int residueNumber, out IMass mod)
        {
            if (residueNumber > Length || residueNumber < 1 || _modifications == null)
            {
                mod = null;
                return false;
            }
            mod = _modifications[residueNumber];
            return mod != null;
        }

        public bool TryGetModification<T>(int residueNumber, out T mod) where T : class, IMass
        {
            IMass outMod;
            if (TryGetModification(residueNumber, out outMod))
            {
                mod = outMod as T;
                return mod != null;
            }
            mod = default(T);
            return false;
        }

        /// <summary>
        /// Sets the modification at the terminus of this nucleic acid polymer
        /// </summary>
        /// <param name="mod">The modification to set</param>
        /// <param name="terminus">The termini to set the mod at</param>
        public virtual void SetModification(IMass mod, Terminus terminus)
        {
            if ((terminus & Terminus.FivePrime) == Terminus.FivePrime)
                FivePrimeTerminusModification = mod;

            if ((terminus & Terminus.ThreePrime) == Terminus.ThreePrime)
                ThreePrimeTerminusModification = mod;
        }

        /// <summary>
        /// Sets the modification at specific sites on this glycan polymer
        /// </summary>
        /// <param name="mod">The modification to set</param>
        /// <param name="sites">The sites to set the modification at</param>
        /// <returns>The number of modifications added to this glycan polymer</returns>
        public virtual int SetModification(IMass mod, ModificationSites sites)
        {
            int count = 0;

            for (int i = 0; i < Length; i++)
            {
                ModificationSites site = _glycanMonomers[i].Site;
                if ((sites & site) == site)
                {
                    ReplaceMod(i + 1, mod);
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Sets the modification at specific sites on this glycan polymer
        /// </summary>
        /// <param name="mod">The modification to set</param>
        /// <param name="shorthandSymbol">The residue character to set the modification at</param>
        /// <returns>The number of modifications added to this glycan polymer</returns>
        public virtual int SetModification(IMass mod, string shorthandSymbol)
        {
            int count = 0;
            for (int i = 0; i < Length; i++)
            {
                if (!shorthandSymbol.Equals(_glycanMonomers[i].ShorthandSymbol))
                    continue;

                ReplaceMod(i + 1, mod);
                count++;
            }

            return count;
        }

        /// <summary>
        /// Sets the modification at specific sites on this glycan polymer
        /// </summary>
        /// <param name="mod">The modification to set</param>
        /// <param name="residue">The residue to set the modification at</param>
        /// <returns>The number of modifications added to this glycan polymer</returns>
        public virtual int SetModification(IMass mod, IGlycanMonomer residue)
        {
            int count = 0;
            for (int i = 0; i < Length; i++)
            {
                if (!residue.Equals(_glycanMonomers[i]))
                    continue;

                ReplaceMod(i + 1, mod);
                count++;
            }
            return count;
        }

        /// <summary>
        /// Sets the modification at specific sites on this glycan polymer
        /// </summary>
        /// <param name="mod">The modification to set</param>
        /// <param name="residueNumber">The residue number to set the modification at</param>
        public virtual void SetModification(IMass mod, int residueNumber)
        {
            if (residueNumber > Length || residueNumber < 1)
                throw new IndexOutOfRangeException(string.Format("Residue number not in the correct range: [{0}-{1}] you specified: {2}", 1, Length, residueNumber));

            ReplaceMod(residueNumber, mod);
        }

        public void SetModifications(IEnumerable<Modification> modifications)
        {
            if (modifications == null)
                return;
            foreach (Modification mod in modifications)
            {
                SetModification(mod, mod.Sites);
            }
        }

        public void SetModification(Modification mod)
        {
            SetModification(mod, mod.Sites);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="mod"></param>
        /// <param name="residueNumbers">(1-based) residue number</param>
        public void SetModification(IMass mod, params int[] residueNumbers)
        {
            foreach (int residueNumber in residueNumbers)
            {
                SetModification(mod, residueNumber);
            }
        }

        /// <summary>
        /// Replaces all instances of the old modification with the new modification in this polymer
        /// </summary>
        /// <param name="oldMod">The modification to remove</param>
        /// <param name="newMod">The modification to replace it with</param>
        /// <returns>The number of modifications added to this nucleic acid polymer</returns>
        public virtual int ReplaceModification(IMass oldMod, IMass newMod)
        {
            if (oldMod == null)
                throw new ArgumentException("Cannot replace a null modification");

            // No need to replace identical mods
            if (oldMod.Equals(newMod))
                return 0;

            int count = 0;
            for (int i = 0; i < Length; i++)
            {
                IMass mod = GetModification(i);
                if (mod == null || !oldMod.Equals(mod))
                    continue;

                ReplaceMod(i, newMod);
                count++;
            }
            return count;
        }

        public virtual int AddModification(Modification modification)
        {
            return AddModification(modification, modification.Sites);
        }

        public virtual int AddModification(IMass modification, ModificationSites sites)
        {
            if (_modifications == null)
                _modifications = new IMass[Length + 2];

            int count = 0;
            IMass currentMod;

            for (int i = 0; i < Length; i++)
            {
                ModificationSites site = _glycanMonomers[i].Site;
                if ((sites & site) == site)
                {
                    currentMod = _modifications[i + 1];
                    ReplaceMod(i + 1, currentMod == null ? modification : new ModificationCollection(currentMod, modification));
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Adds the modification at specific sites on this nucleic acid polymer, combining modifications if a modification is already present
        /// </summary>
        /// <param name="modification">The modification to set</param>
        /// <param name="residueNumber">The residue number to set the modification at</param>
        public virtual void AddModification(IMass modification, int residueNumber)
        {
            if (residueNumber > Length || residueNumber < 1)
                throw new IndexOutOfRangeException(string.Format("Residue number not in the correct range: [{0}-{1}] you specified: {2}", 1, Length, residueNumber));

            IMass currentMod = GetModification(residueNumber);
            ReplaceMod(residueNumber, currentMod == null ? modification : new ModificationCollection(currentMod, modification));
        }

        /// <summary>
        /// Clear the modifications from the specified sites(s)
        /// </summary>
        /// <param name="sites">The sites to remove modifications from</param>
        public void ClearModifications(ModificationSites sites)
        {
            if (_modifications == null)
                return;

            for (int i = 0; i < Length; i++)
            {
                int modIndex = i;

                if (_modifications[modIndex] == null)
                    continue;

                ModificationSites curSite = _glycanMonomers[i].Site;

                if ((curSite & sites) == curSite)
                {
                    ReplaceMod(modIndex, null);
                }
            }
        }

        /// <summary>
        /// Clear all modifications from this nucleic acid polymer.
        /// Includes 5' and 3' terminus modifications.
        /// </summary>
        public void ClearModifications()
        {
            if (!ContainsModifications())
                return;

            for (int i = 0; i <= Length + 1; i++)
            {
                if (_modifications[i] == null)
                    continue;

                MonoisotopicMass -= _modifications[i].MonoisotopicMass;
                _modifications[i] = null;
                IsDirty = true;
            }
        }

        /// <summary>
        /// Removes the specified mod from all locations on this polymer
        /// </summary>
        /// <param name="mod">The modification to remove from this polymer</param>
        public void ClearModifications(IMass mod)
        {
            if (mod == null || _modifications == null)
                return;

            for (int i = 0; i <= Length + 1; i++)
            {
                if (!mod.Equals(_modifications[i]))
                    continue;

                MonoisotopicMass -= mod.MonoisotopicMass;
                _modifications[i] = null;
                IsDirty = true;
            }
        }

        #endregion Modifications

        #region ChemicalFormula

        /// <summary>
        /// Gets the chemical formula of this glycan polymer.
        /// If a modification attached to this polymer does not
        /// have a chemical formula, it is not included in the output,
        /// thus the return chemical formula may not be accurate.
        /// See <see cref="TryGetChemicalFormula"/> for more details
        /// </summary>
        /// <returns></returns>
        public ChemicalFormula GetChemicalFormula()
        {
            var formula = new ChemicalFormula();

            // Handle Modifications
            if (ContainsModifications())
            {
                for (int i = 0; i < Length; i++)
                {
                    IChemicalFormula chemMod = _modifications[i] as IChemicalFormula;

                    if (chemMod == null)
                        continue;

                    formula.Add(chemMod.ChemicalFormula);
                }
            }

            // Handle Glycan Monomer Residues
            for (int i = 0; i < Length; i++)
            {
                formula.Add(_glycanMonomers[i].ChemicalFormula);
            }

            return formula;
        }

        /// <summary>
        /// Try and get the chemical formula for the whole glycan polymer. Modifications
        /// may not always be of IChemicalFormula and this method will return false if any
        /// modification is not a chemical formula
        /// </summary>
        /// <param name="formula"></param>
        /// <returns></returns>
        public bool TryGetChemicalFormula(out ChemicalFormula formula)
        {
            formula = new ChemicalFormula();

            // Handle Modifications
            if (ContainsModifications())
            {
                for (int i = 0; i < Length; i++)
                {
                    IMass mod;
                    if ((mod = _modifications[i]) == null)
                        continue;

                    IChemicalFormula chemMod = mod as IChemicalFormula;
                    if (chemMod == null)
                        return false;

                    formula.Add(chemMod.ChemicalFormula);
                }
            }

            // Handle Nucleic Acid Residues
            for (int i = 0; i < Length; i++)
            {
                formula.Add(_glycanMonomers[i].ChemicalFormula);
            }

            return true;
        }

        #endregion ChemicalFormula

        #region Object

        public override string ToString()
        {
            return SequenceWithModifications;
        }

        public override int GetHashCode()
        {
            return Sequence.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            GlycanPolymer gp = obj as GlycanPolymer;
            return gp != null && Equals(gp);
        }

        #endregion Object

        #region IEquatable

        public bool Equals(GlycanPolymer other)
        {
            if (other == null ||
                Length != other.Length ||
                !GlycanBondLossModifier.Equals(other.GlycanBondLossModifier))
                return false;

            bool containsMod = ContainsModifications();

            if (containsMod != other.ContainsModifications())
                return false;

            for (int i = 0; i <= Length; i++)
            {
                if (containsMod && !Equals(_modifications[i], other._modifications[i]))
                    return false;

                if (i == 0 || i == Length + 1)
                    continue; // uneven arrays, so skip these two conditions

                if (!_glycanMonomers[i - 1].Equals(other._glycanMonomers[i - 1]))
                    return false;
            }
            return true;
        }

        #endregion IEquatable

        #region Private Methods

        private bool ReplaceBondLossModifier(ref IChemicalFormula bondLossModifier, IChemicalFormula value)
        {
            if (Equals(value, bondLossModifier))
                return false;

            if (bondLossModifier != null)
                MonoisotopicMass -= bondLossModifier.MonoisotopicMass;

            bondLossModifier = value;

            if (value != null)
                MonoisotopicMass += value.MonoisotopicMass;

            return true;
        }

        /// <summary>
        /// Replaces a modification (if present) at the specific index in the residue (0-based for 5' and 3' termini)
        /// </summary>
        /// <param name="index">The residue index to replace at</param>
        /// <param name="mod">The modification to replace with</param>
        private bool ReplaceMod(int index, IMass mod)
        {
            // No error checking here as all validation will occur before this method is call. This is to prevent
            // unneeded bounds checking

            if (_modifications == null)
            {
                _modifications = new IMass[Length + 2];
            }

            IMass oldMod = _modifications[index]; // Get the mod at the index, if present

            if (Equals(mod, oldMod))
                return false; // Same modifications, no change is required

            IsDirty = true;

            if (oldMod != null)
                MonoisotopicMass -= oldMod.MonoisotopicMass; // remove the old mod mass

            _modifications[index] = mod;

            if (mod != null)
                MonoisotopicMass += mod.MonoisotopicMass; // add the new mod mass

            return true;
        }

        /// <summary>
        /// Parses a string sequence of glycan shorthand symbols into a glycan object
        /// </summary>
        /// <param name="sequence"></param>
        /// <returns></returns>
        private bool ParseSequence(string sequence)
        {
            if (string.IsNullOrEmpty(sequence))
                return false;

            bool inMod = false;
            int index = 0;

            double monoMass = 0;

            StringBuilder sb = null;
            bool storeSequenceString = StoreSequenceString;
            if (storeSequenceString)
                sb = new StringBuilder(sequence.Length);

            StringBuilder modSb = new StringBuilder(10);

            foreach (string shorthandSymbol in sequence.Split('-'))
            {
                if (inMod)
                {
                    if (shorthandSymbol.Contains(']'))
                    {
                        inMod = false; // end the modification phase

                        string modString = modSb.ToString();
                        modSb.Clear();
                        IMass modification;
                        switch (modString)
                        {
                            default:
                                double mass;
                                Modification mod;
                                if (ModificationDictionary.TryGetModification(modString, out mod))
                                {
                                    modification = mod;
                                }
                                else if (ChemicalFormula.IsValidChemicalFormula(modString))
                                {
                                    modification = new ChemicalFormula(modString);
                                }
                                else if (double.TryParse(modString, out mass))
                                {
                                    modification = new Mass(mass);
                                }
                                else
                                {
                                    throw new ArgumentException("Unable to correctly parse the following modification: " + modString);
                                }
                                break;
                        }

                        monoMass += modification.MonoisotopicMass;

                        if (_modifications == null)
                            _modifications = new IMass[Length];
                        else
                        {
                            _modifications[index] = modification;
                        }
                    }
                    else
                    {
                        modSb.Append(shorthandSymbol);
                    }
                }
                else
                {
                    GlycanMonomer residue;
                    //char upperletter = char.ToUpper(letter); // moved to nucleic acid dictionary
                    if (GlycanMonomer.TryGetResidue(shorthandSymbol, out residue))
                    {
                        _glycanMonomers[index++] = residue;
                        if (storeSequenceString)
                            sb.Append(residue.ShorthandSymbol);
                        monoMass += residue.MonoisotopicMass;
                    }
                    else
                    {
                        switch (shorthandSymbol)
                        {
                            /*
                            case '[': // start of a modification
                                inMod = true;
                                break;

                            case '-': // start of a 3'-terminal modification
                                threePrimeTerminalMod = (index > 0);
                                break;

                            case ' ': // ignore spaces
                                break;

                            case '*': // ignore *
                                break;
                            */
                            default:
                                throw new ArgumentException(string.Format("Glycan Shorthand Symbol {0} does not exist in the Glycan Monomer Dictionary.", shorthandSymbol));
                        }
                    }
                }
            }

            if (inMod)
            {
                throw new ArgumentException("Couldn't find the closing ] for a modification in this sequence: " + sequence);
            }

            if (storeSequenceString)
                _sequence = sb.ToString();

            Length = index;
            MonoisotopicMass += monoMass;
            Array.Resize(ref _glycanMonomers, Length);
            if (_modifications != null)
                Array.Resize(ref _modifications, Length + 2);
            IsDirty = true;

            return true;
        }

        #endregion Private Methods

        #region Static Methods

        #region Fragmentation

        public static IEnumerable<Fragment> GetSiteDeterminingFragments(GlycanPolymer peptideA, GlycanPolymer peptideB, FragmentTypes types)
        {
            if (peptideA == null)
            {
                // Only b is not null, return all of its fragments
                if (peptideB != null)
                {
                    return peptideB.Fragment(types);
                }
                throw new ArgumentNullException("peptideA", "Cannot be null");
            }

            if (peptideB == null)
            {
                return peptideA.Fragment(types);
            }
            HashSet<Fragment> aFrags = new HashSet<Fragment>(peptideA.Fragment(types));
            HashSet<Fragment> bfrags = new HashSet<Fragment>(peptideB.Fragment(types));

            aFrags.SymmetricExceptWith(bfrags);
            return aFrags;
        }

        #endregion Fragmentation

        public static double GetMass(string sequence)
        {
            double mass = Constants.Water;
            foreach (string shorthandSymbol in sequence.Split('-'))
            {
                GlycanMonomer residue;
                if (GlycanMonomer.TryGetResidue(shorthandSymbol, out residue))
                {
                    mass += residue.MonoisotopicMass;
                }
            }
            return mass;
        }

        #endregion Static Methods
    }
}
