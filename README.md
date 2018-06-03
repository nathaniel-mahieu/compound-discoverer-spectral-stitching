# Compound Discoverer Scan Segment Merging Node
## Introduction
Compound Discoverer is a framework for processing and interpreting untargeted metabolomic data.  The software performs peak detection, common adduct annotation, and isotopic composition analysis.  Further nodes can be applied to do peak statistics, search peaks in databases, and annotate peaks with MSMS data.

The data we generate uses a scan splicing approach similar to that applied here: http://pubs.acs.org/doi/abs/10.1021/ac062446p. Since our HILIC data peaks are wide, and we don’t need the extra resolution, we use the extra duty cycle to scan different mass ranges in alternating scans.  This spreads the observed charge over more “space” (multiple trap volumes) and drastically increases dynamic range and sensitivity.  This is especially notable when a large ion is present in one mass range, but the second mass range is not crowded out by it.

This data is not natively compatible with Compound Discoverer.  There is a custom node written by Nate to make this compatible.
## Installation
 - Copy “Mahieu.RawData.Nodes.dll” to “C:\Program Files\Thermo\Compound Discoverer 3.0\bin”
 - In “C:\Program Files\Thermo\Compound Discoverer 3.0\bin” run the command (Figure 2):
    - ./Thermo.CompoundDiscoverer.Server.exe -Install
 - You can check that my node was found and registered in the output (Figure 2)
Start CD with the command: (this command shows a window in which you can view the progress of the analysis.  Otherwise you can start CD normally.) (Figure 1)
    - ./Thermo.CompoundDiscoverer.exe -startServer -showServerWindow
## Usage
 - Insert the scan splicing node between select spectra and the rest of your workflow (there may be unpictured steps here.)
 - Open a raw data file in qual browser and verify
 - The number of scan segments
 - The scan ranges which to use 
 - Note in the case of overlapping scan ranges use a non-overlapping portion of the ranges
 - In the Mahieu Scan Segment Merging Node options
    - Specify the number of scan segments
    - The scan ranges to take from each segment
 - Fill in the rest of your workflow as desired.
## Notes
 - Make sure the first scan range specified (Segment 1 Lower/Upper) corresponds to the first scan of the file
 - Make sure the scan ranges specified are non overlapping
 - Check for errors and warnings in the output.  The software will raise a warning if more than 10% of the mass peaks are discarded.
 - Tested on CD 3.0.0.260