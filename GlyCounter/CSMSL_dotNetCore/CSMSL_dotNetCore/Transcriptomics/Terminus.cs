using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CSMSL.Transcriptomics
{
    /// <summary>
    /// The terminus of an amino acid polymer N-[Amino Acids]-C
    /// </summary>
    [Flags]
    public enum Terminus
    {
        /// <summary>
        /// The N-terminus (amino-terminus)
        /// </summary>
        FivePrime = 1,

        /// <summary>
        /// The C-terminus (carboxyl-terminus)
        /// </summary>
        ThreePrime = 2
    }
}
