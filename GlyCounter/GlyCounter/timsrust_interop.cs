using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using Nova.Data;
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
        [JsonProperty("mz")]
        public float mz { get; set; }

        [JsonProperty("charge")]
        public byte? charge { get; set; }

        [JsonProperty("intensity")]
        public float? intensity { get; set; }

        [JsonProperty("spectrum_ref")]
        public string spectrum_ref { get; set; }

        [JsonProperty("inverse_ion_mobility")]
        public float? ion_mobility { get; set; }

        [JsonProperty("isolation_window")]
        public float[] isolation_window { get; set; }
    }

    public class RawSpectrum
    {
        [JsonProperty("precursors")]
        public Precursor[] precursors { get; set; }

        [JsonProperty("scan_start_time")]
        public float? scan_start_time { get; set; }

        [JsonProperty("mz")]
        public float[] mz { get; set; }

        [JsonProperty("ms_level")]
        public byte ms_level { get; set; }

        [JsonProperty("id")]
        public string id { get; set; }

        [JsonProperty("intensity")]
        public float[] intensity { get; set; }

        [JsonProperty("collision_energy")]
        public float? collision_energy { get; set; }

        [JsonIgnore]
        public List<SpecDataPointEx> peaks { get; set; } = new List<SpecDataPointEx>();
    }
}