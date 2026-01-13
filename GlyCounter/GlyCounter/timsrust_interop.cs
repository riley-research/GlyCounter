using Nova.Data;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using System.Diagnostics;

namespace GlyCounter
{
    static class Native
    {
        private const string DllName = "timsrust";

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern UIntPtr open_reader([MarshalAs(UnmanagedType.LPStr)] string path);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern UIntPtr get_spectrum_count(UIntPtr handle);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr get_spectrum(UIntPtr handle, UIntPtr index);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern void close_reader(UIntPtr handle);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool is_dia([MarshalAs(UnmanagedType.LPStr)] string path);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern void tr_free_cstring(IntPtr s);

        /// <summary>
        /// Check if a Bruker .d file is DIA acquisition
        /// </summary>
        public static bool IsDia(string path)
        {
            try
            {
                return is_dia(path);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking if file is DIA: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Read MS/MS spectra one at a time using IEnumerable
        /// </summary>
        public static IEnumerable<RawSpectrum> ReadMsnSpectraLazy(string path)
        {
            UIntPtr handle = UIntPtr.Zero;
            try
            {
                handle = open_reader(path);

                if (handle == UIntPtr.Zero)
                {
                    Debug.WriteLine("Failed to open reader");
                    yield break;
                }

                var count = (int)get_spectrum_count(handle);

                for (int i = 0; i < count; i++)
                {
                    if (i % 1000 == 0)
                    {
                        Console.WriteLine($"Reading spectrum {i}/{count}");
                    }

                    IntPtr spectrumPtr = IntPtr.Zero;
                    try
                    {
                        spectrumPtr = get_spectrum(handle, (UIntPtr)i);

                        if (spectrumPtr == IntPtr.Zero)
                        {
                            // Spectrum has no precursor, skip it
                            continue;
                        }

                        string json = Marshal.PtrToStringAnsi(spectrumPtr);
                        if (string.IsNullOrEmpty(json))
                        {
                            continue;
                        }

                        var spectrum = JsonConvert.DeserializeObject<RawSpectrum>(json);
                        if (spectrum != null)
                        {
                            yield return spectrum;
                        }
                    }
                    finally
                    {
                        if (spectrumPtr != IntPtr.Zero)
                        {
                            tr_free_cstring(spectrumPtr);
                        }
                    }
                }

                Console.WriteLine($"Finished reading all spectra");
            }
            finally
            {
                if (handle != UIntPtr.Zero)
                {
                    close_reader(handle);
                }
            }
        }

        /// <summary>
        /// Read all spectra into a list (for backwards compatibility)
        /// </summary>
        public static List<RawSpectrum> ReadMsnSpectra(string path)
        {
            return new List<RawSpectrum>(ReadMsnSpectraLazy(path));
        }
    }

    public class Precursor
    {
        public float mz { get; set; }
        public byte? charge { get; set; }
        public float? intensity { get; set; }
        public string spectrum_ref { get; set; }
        public float? inverse_ion_mobility { get; set; }
        public float[] isolation_window { get; set; }
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
        public List<SpecDataPointEx> peaks { get; set; } = new List<SpecDataPointEx>();
    }
}