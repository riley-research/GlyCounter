fn main() {
    csbindgen::Builder::default()
        .input_extern_file("src/lib.rs")
        .csharp_dll_name("timsrust_ffi")
        .csharp_namespace("GlyCounter")
        .generate_csharp_file("../GlyCounter/GlyCounter/NativeMethods.g.cs")
        .unwrap();
}