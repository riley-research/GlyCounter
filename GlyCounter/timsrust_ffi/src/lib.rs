use std::ffi::{CStr, CString};
use std::os::raw::{c_char};
use std::{ptr};
use std::sync::Mutex;
use std::collections::HashMap;
use timsrust::{TimsTofPath, SpectrumReader};
use serde::Serialize;
use serde_json;

struct ReaderHandle {
    spectrum_reader: SpectrumReader,
    is_tdf: bool,
}

lazy_static::lazy_static! {
    static ref READERS: Mutex<HashMap<usize, ReaderHandle>> = Mutex::new(HashMap::new());
    static ref NEXT_HANDLE: Mutex<usize> = Mutex::new(1);
}

#[derive(Serialize)]
struct PrecursorOut {
    spectrum_ref: usize,
    mz: f64,
    charge: Option<i32>,
    retention_time: f64,
    ion_mobility: f64,
}

#[derive(Serialize)]
struct SpectrumOut {
    index: usize,
    mz: Vec<f64>,
    intensities: Vec<f64>,
    precursor: Option<PrecursorOut>,
    collision_energy: Option<f64>,
}


#[unsafe(no_mangle)]
pub extern "C" fn open_reader(path: *const c_char) -> usize {
    let result = std::panic::catch_unwind(|| -> usize {
        if path.is_null() {
            return 0;
        }

        let path_str = match unsafe { CStr::from_ptr(path) }.to_str() {
            Ok(s) => s,
            Err(_) => return 0,
        };

        let tims_path = match TimsTofPath::new(path_str) {
            Ok(p) => p,
            Err(_) => return 0,
        };

        let is_tdf = std::path::Path::new(path_str).join("analysis.tdf").exists();

        let spectrum_reader = match tims_path.spectrum_reader() {
            Ok(r) => r,
            Err(_) => return 0,
        };

        let mut next_handle = NEXT_HANDLE.lock().unwrap();
        let handle = *next_handle;
        *next_handle += 1;

        READERS.lock().unwrap().insert(handle, ReaderHandle {
            spectrum_reader,
            is_tdf,
        });
        handle
    });

    result.unwrap_or(0)
}

fn precursor_out_from(p: &timsrust::core::Precursor) -> PrecursorOut {
    PrecursorOut {
        spectrum_ref: p.index(),
        mz: p.mz().into(),
        charge: p.charge().map(Into::into),
        retention_time: p.rt().into(),
        ion_mobility: p.im().into(),
    }
}

fn collision_energy_out<C>(spectrum: &timsrust::core::Spectrum<C>, is_tdf: bool) -> Option<f64> {
    if !is_tdf || spectrum.precursor().is_none() {
        return None;
    }
    Some(spectrum.isolation_window().collision_energy())
}

#[unsafe(no_mangle)]
pub extern "C" fn get_spectrum(handle: usize, index: usize) -> *mut c_char {
    let result = std::panic::catch_unwind(|| -> *mut c_char {
        let readers = READERS.lock().unwrap();
        let reader = match readers.get(&handle) {
            Some(r) => r,
            None => return ptr::null_mut(),
        };



        let spectrum = match reader.spectrum_reader.get(index) {
            Ok(s) => s,
            Err(_) => return ptr::null_mut(),
        };

        let precursor_out = spectrum.precursor().as_ref().map(|p| precursor_out_from(p));
        let collision_energy = collision_energy_out(&spectrum, reader.is_tdf);
        let mz_values: Vec<f64> = spectrum.mz_values().iter().map(|&mz| mz.into()).collect();

        let spectrum_out = SpectrumOut {
            index: spectrum.index(),
            mz: mz_values,
            intensities: spectrum.intensities().clone(),
            precursor: precursor_out,
            collision_energy,
        };

        match serde_json::to_string(&spectrum_out) {
            Ok(json) => match CString::new(json) {
                Ok(cstr) => cstr.into_raw(),
                Err(_) => ptr::null_mut(),
            },
            Err(_) => ptr::null_mut(),
        }
    });
    result.unwrap_or(ptr::null_mut())
}

#[unsafe(no_mangle)]
pub extern "C" fn get_spectrum_count(handle: usize) -> usize {
    let result = std::panic::catch_unwind(|| -> usize {
        let readers = READERS.lock().unwrap();
        match readers.get(&handle) {
            Some(r) => return r.spectrum_reader.len(),
            None => return 0,
        };
    });
    result.unwrap_or(0)
}

#[unsafe(no_mangle)]
pub extern "C" fn free_string(s: *mut c_char) {
    if s.is_null() {
        return;
    }
    unsafe {
        let _ = CString::from_raw(s);
    }
}

/// Closes and drops a reader handle.
#[unsafe(no_mangle)]
pub extern "C" fn close_reader(handle: usize) {
    let _ = std::panic::catch_unwind(|| {
        READERS.lock().unwrap().remove(&handle);
    });
}