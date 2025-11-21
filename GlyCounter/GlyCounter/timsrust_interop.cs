using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace GlyCounter
{
    static class Native
    {
        [DllImport("timsrust", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern IntPtr read_msn_spectra([MarshalAs(UnmanagedType.LPStr)] string path);

        [DllImport("timsrust", CallingConvention = CallingConvention.Cdecl)]
        public static extern void tr_free_cstring(IntPtr s);
    }

    public class Precursor
    {
        public float mz { get; set; }
        public byte? charge { get; set; }
        public float? intensity { get; set; }
        public string spectrum_ref { get; set; }
        public float? inverse_ion_mobility { get; set; }
        public float[] isolation_window { get; set; } // [low, high]
    }

    public class RawSpectrum
    {
        public Precursor[] precursors { get; set; }
        public float? scan_start_time { get; set; }
        public float? ion_injection_time { get; set; }
        public float total_ion_current { get; set; }
        public float[] mz { get; set; }
        public byte ms_level { get; set; }
        public string id { get; set; }
        public float[] intensity { get; set; }
    }
}
