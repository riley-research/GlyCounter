# GlyCounter

[![Build and Release](https://github.com/riley-research/GlyCounter/actions/workflows/build.yml/badge.svg)](https://github.com/riley-research/GlyCounter/actions/workflows/build.yml)
[![Latest Release](https://img.shields.io/github/v/release/riley-research/GlyCounter)](https://github.com/riley-research/GlyCounter/releases/latest)
[![Issues](https://img.shields.io/github/issues/riley-research/GlyCounter)](https://github.com/riley-research/GlyCounter/issues)

## Table of Contents

- [Download Instructions](#download-instructions)
- [GlyCounter Basics](#glycounter-basics)
  - [Selecting Files](#selecting-files)
  - [Variables](#variables)
  - [LikelyGlycoSpectrum](#likelyglycopectrum)
  - [Output Files](#output-files)
- [Ynaught](#ynaught)
  - [Output Files](#output-files-1)
  - [Variables](#variables-1)
- [Example Files](#example-files)
- [Release Process](#release-process)

## Download Instructions

A stand-alone GlyCounter executable is available in the [Releases](https://github.com/riley-research/GlyCounter/releases) section of this repository. Download the latest `GlyCounter-win-Setup.exe` to install GlyCounter on your Windows system. When there's an update available, users will be automatically prompted to update.

The GlyCounter solution can also be cloned to Visual Studio and run.

## GlyCounter Basics

The Pre-ID tab is heart of GlyCounter. Here you can pick common oxonium ions seen in glycopeptide MS/MS spectra, and GlyCounter will find them in your raw data. You can also upload csv file with additional or custom ions to be considered, and scan settings are customizable per dissociation method (see below for more details). This allows you to understand what your glycoproteomics data is telling you before you ever have to decided what search algorithm to use. GlyCounter can be useful for many steps in a glycoproteomics experiment, including quick evaluations of sample prep or instrument conditions, what glycan database you might want to use for searching your data, or how to better understand what identifications your search algorithm produces. GlyCounter is designed to provide flexibility, so there are several settings you can control as the user to best understand your data.

### Selecting Files

GlyCounter accepts .raw or .mzML files. The top browse box allows you to navigate to folders that contain your data. Choose one or more raw/mzml files that you'd like to process, and the text box should update to show how many files you've chosen. GlyCounter creates individual outputs for each file, and will overwrite files if a different output directory is not chosen. The bottom browse box allows you to set your output directory, which is the folder where the GlyCounter results will be stored. If you do not select a valid output directory the program will error.

### Variables

**ppm Tolerance**: tolerance for determining if the oxonium ion is present

**Signal-to-Noise Requirement**: S/N ratio needed for oxonium ions

**Intensity Threshold**: If S/N is not available in the file, an intensity threshold is used instead. This will always be the case for ion trap .raw files and all .mzML files.

**Scan Settings - Peak Depth (Must be within N most intense peaks)**: number of peaks which are checked for the Oxonium Count Requirement

**Scan Settings - TIC Fraction**: fraction of total TIC which needs to be oxonium ions for LikelyGlycoSpectrum

**Scan Settings - Oxonium Count Requirement**: Number of oxonium ions that need to be in the peak depth for LikelyGlycoSpectrum. When this is set to 0 the default amount is used. The default depends on the number of oxonium ions checked. For HCD and UVPD the defaults are:

| Number of Oxonium Ions Checked | Default Oxonium Count Requirement |
| :----------------------------: | :-------------------------------: |
|          less than 6           |                 4                 |
|        between 6 and 15        |  half of number of checked ions   |
|        greater than 15         |                 8                 |

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

Calculates and extracts Y-ions and/or glycan neutral losses from database searched data. Accepts a formatted .txt or .tsv PSMs file with the headers "Spectrum", "Charge", "Peptide", "Total Glycan Composition", "Observed M/Z", and "Assigned Modifications"; a glycan masses .txt with headers "Glycan" and "Mass"; and a .raw or .mzml file.
Additional csv files can be uploaded with custom Y-ions or neutral losses (Headers "Mass" and "Description").

### Output Files

#### GlyCounter_YionSignal.txt

Shows the intensity for each selected Y-ion in each spectrum. The IonsFound column contains a list of Y-ions formatted as "m/z value, description:charge states;".

#### GlyCounter_YionSummary.txt

Shows the settings used and a summary of the results per scan type

### Variables

**ppm tolerance**: tolerance for determining if the ion is present

**Signal-to-Noise Requirement**: S/N ratio needed for ions

**Isotope Options**: Choose if you want to look for C13 isotopes

**Ouput IPSA Annotations**: Check if you want to output annotations compatable with IPSA 2.0. This creates a text file with the found oxonium ions per scan and their mass errors.

**Charge State Options**: Larger glycopeptide fragments have the potential to be at any charge state between +1 and the precursor charge state. The charge state limits are determined based on the precursor charge "P".
For example: if the precursor charge is 4 and I want to consider anything with a charge +2 to +4, I would enter either P-2 or 2 in the lower bound (depending on if I wanted to always be two charge states below my precursor charge or if I wanted to always start at charge state 2) and P or 4 in the upper bound.

**Combining vs Splitting Columns for Charge States**: There are two options for how to format columns in the YionSignal output. First, the intensities for all charge states of a given Y-ion can be summed and placed in a column with that Y-ion name as a header. Additional information about which charge states were found is available in the "ChargeStatesFound" column, but the individual intensities will not be separable. Second, the columns can be expanded so that there is one column for every charge checked for each Y-ion. The column headers would contain both the Y-ion name and the charge state. This creates an output file with a lot of columns, but allows for individual peak intensities to be reported.

## Example Files

**Glycounter_Custom_Ion_Upload.csv**: Oxonium ions taken from the supplementary table of _Experimentally Determined Diagnostic Ions for Identification of Peptide Glycotopes_ published by DeBono, Moh, and Packer in JPR (2024).

**Human or Mouse glycan masses text file**: YNaught glycan masses upload

**Sample_Ynaught_GlycopeptideIDs.txt**: YNaught Glycopeptide IDs sample file upload

**Phospho.txt**: R file used to create a figure using GlyCounter data

**ListofGivenOxoIons.txt**: List of ions and m/z values provided by default in GlyCounter

## Release Process

To create a new release of GlyCounter:

1. From the master branch, create a Git tag with a version number higher than the last release:
   ```bash
   git tag v1.0.0
   ```
2. Push the tag to GitHub:

   ```bash
   git push origin v1.0.0
   ```

3. The GitHub Actions workflow will automatically build and publish the release artifacts.
