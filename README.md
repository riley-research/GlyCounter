# GlyCounter
## Download Instructions
Each time a commit is pushed to master, the Builds folder will update with the new GlyCounter.exe package. The GlyCounter solution can also be cloned to Visual Studio and run.
## GlyCounter Basics
The Pre-ID tab is heart of GlyCounter. Here you can pick common oxonium ions seen in glycopeptide MS/MS spectra, and GlyCounter will find them in your raw data. You can also upload csv file with additional or custom ions to be considered, and scan settings are customizable per dissociation method (see below for more details).  This allows you to understand what your glycoproteomics data is telling you before you ever have to decided what search algorithm to use. GlyCounter can be useful for many steps in a glycoproteomics experiment, including quick evaluations of sample prep or instrument conditions,  what glycan database you might want to use for searching your data, or how to better understand what identifications your search algorithm produces. GlyCounter is designed to provide flexibility, so there are several settings you can control as the user to best understand your data. 

### Selecting Files
GlyCounter accepts .raw or .mzML files. The top browse box allows you to navigate to folders that contain your data. Start by selecting one file. GlyCounter can process single files individually, or you can check the "All .raw and .mzML files in folder" box to perform batch processing on all raw data files in that folder location. GlyCounter will generate output files (see below) individually for each file regardless of choosing batch processing or not. Note, if you select the check box for all files in the folder, GlyCounter will process all .raw AND .mzML files, so be sure to move any files you would not want processed in batch. Each time GlyCounter is run for a give file, previous output files stored in that same location are overwritten, so be sure to remove or rename files if you want to keep outputs from different settings/iterations of GlyCounter.

one or more .raw or .mzML file(s) and extracts information about the oxonium ions in your spectra. 
### Variables
**ppm Tolerance**: tolerance for determining if the oxonium ion is present

**Signal-to-Noise Requirement**: S/N ratio needed for oxonium ions

**Intensity Threshold**: If S/N is not available in the file, an intensity threshold is used instead. This will always be the case for ion trap .raw files and all .mzML files.

**Scan Settings - Peak Depth (Must be within N most intense peaks)**: number of peaks which are checked for the Oxonium Count Requirement

**Scan Settings - TIC Fraction**: fraction of total TIC which needs to be oxonium ions for LikelyGlycoSpectrum

**Scan Settings - Oxonium Count Requirement**: Number of oxonium ions that need to be in the peak depth for LikelyGlycoSpectrum. When this is set to 0 the default amount is used. The default depends on the number of oxonium ions checked. For HCD and UVPD the defaults are: 

| Number of Oxonium Ions Checked | Default Oxonium Count Requirement| 
|:------------------------------:|:--------------------------------:|
| less than 6                    | 4                                |
| between 6 and 15               | half of number of checked ions   |
| greater than 15                | 8                                |

These defaults are halved for ETD spectra. The Check Common Ions button checks 17 ions, so if used with the default setting the count requirement would be 8 for HCD/UVPD and 4 for ETD.

**Ouput IPSA Annotations**: Check if you want to output annotations compatable with IPSA 2.0. This creates a text file with the found oxonium ions per scan and their mass errors.

### LikelyGlycoSpectrum
A spectrum is considered likely to be a glycopeptide if it meets the requirements set by the user before the run. This is based on the Oxonium Count Requirement (minimum amount of oxonium ions needed to be observed in the N most intense peaks set by the Peak Depth option) and the chosen TIC fraction (minimum percentage of TIC that needs to be oxonium ions).
If the HexNAc (204.0867 m/z) oxonium ion is selected, it must show up in the set peak depth for a spectrum to be considered LikelyGlyco.
If the settings are not changed by the user or an unrecognizable input is entered, the default values will be used.

### Output Files
#### GlyCounter_OxoPeakDepth.txt
Shows the peak depth for each selected oxonium ion in each spectrum

#### GlyCounter_OxoSignal.txt
Shows the intensity for each selected oxonium ion in each spectrum

#### GlyCounter_Summary.txt
Shows the settings used and a summary of the results per scan type

## Ynaught
Calculates and extracts Y-ions and/or glycan neutral losses from database searched data. Accepts a formatted .txt PSMs file with the headers "Spectrum", "Charge", "Peptide", "Total Glycan Composition", "Observed M/Z", and "Assigned Modifications"; a glycan masses .txt with headers "Glycan" and "Mass"; and a .raw file. Ynaught currently does not support .mzML files. 
Additional csv files can be uploaded with custom Y-ions or neutral losses (Headers "Mass" and "Description").

### Output Files
#### GlyCounter_YionPeakDepth.txt
Shows the peak depth for each selected Y-ion in each spectrum

#### GlyCounter_YionSignal.txt
Shows the intensity for each selected Y-ion in each spectrum

#### GlyCounter_YionSummary.txt
Shows the settings used and a summary of the results per scan type

### Variables
**ppm tolerance**: tolerance for determining if the ion is present

**Signal-to-Noise Requirement**: S/N ratio needed for ions

**Isotope Options**: Choose if you want to look for C13 isotopes

**Charge State Options**: Larger glycopeptide fragments have the potential to be at any charge state between +1 and the precursor charge state. The charge state limits are determined based on z-X and z-Y where z is the precursor charge, z-X is the highest considered charge state, and z-Y is the lowest considered charge state.
For example: if the precursor charge is 4 and I want to consider anything with a charge +2 to +4, I would enter 0 for X and 2 for Y. If I only wanted to consider the precursor charge then X and Y should be 0.

## Example Files
**Glycounter_Custom_Ion_Upload.csv**: Oxonium ions taken from the supplementary table of *Experimentally Determined Diagnostic Ions for Identification of Peptide Glycotopes* published by DeBono, Moh, and Packer in JPR (2024).

**Human or Mouse glycan masses text file**: YNaught glycan masses upload

**Sample_Ynaught_GlycopeptideIDs.txt**: YNaught Glycopeptide IDs sample file upload

