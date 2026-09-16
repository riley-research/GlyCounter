using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using Nova.Data;
using System.Diagnostics;
using System.Text;

namespace GlyCounter
{
    internal static unsafe partial class NativeMethods
    {

        [DllImport("timsrust_ffi", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]

        public static extern nuint open_reader([MarshalAs(UnmanagedType.LPUTF8Str)] string path);

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
                        Debug.WriteLine($"Reading spectrum {i}/{count}");

                    var spectrum = ReadSpectrumAt(handle, i);
                        yield return spectrum;
                }

                Debug.WriteLine("Finished reading all spectra");
            }
            finally
            {
                if (handle != UIntPtr.Zero)
                    close_reader(handle);
            }
        }

        private static RawSpectrum ReadSpectrumAt(UIntPtr handle, int index)
        {
            byte* spectrumByte = null;
            try
            {
                spectrumByte = get_spectrum(handle, (UIntPtr)index);
                if (spectrumByte == null)
                    return null;

                string json = Marshal.PtrToStringAnsi((IntPtr)spectrumByte);
                if (string.IsNullOrEmpty(json))
                    return null;

                return JsonConvert.DeserializeObject<RawSpectrum>(json);
            }
            finally
            {
                if (spectrumByte != null)
                    free_string(spectrumByte);
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

        [JsonProperty("ion_mobility")]
        public float? ion_mobility { get; set; }

        [JsonProperty("retention_time")]
        public float? retention_time { get; set; }
    }

    public class RawSpectrum
    {
        [JsonProperty("precursor")]
        public Precursor precursor { get; set; }

        [JsonProperty("mz")]
        public float[] mz { get; set; }

        [JsonProperty("index")]
        public string id { get; set; }

        [JsonProperty("intensities")]
        public float[] intensity { get; set; }

        [JsonProperty("collision_energy")]
        public float? collision_energy { get; set; }
    }
}