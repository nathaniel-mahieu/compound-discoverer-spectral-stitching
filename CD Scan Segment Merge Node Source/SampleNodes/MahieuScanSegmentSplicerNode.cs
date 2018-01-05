﻿using System.Collections.Generic;
using System.Linq;
using Thermo.Magellan.BL.Data;
using Thermo.Magellan.BL.Processing;
using Thermo.Magellan.BL.Processing.Interfaces;
using Thermo.Magellan.MassSpec;

namespace Mahieu.RawData.Nodes
{

	/// <summary>
	/// This node takes data in which sequential spectra have different scan ranges, repeating throughout the experiment and merges (truncates/concatenates) each n spectra into a single spectrum which can be handled by downstream workflows.
	/// </summary>
	[ProcessingNode("6A6D5214-BC2A-493B-B291-331B5B746AA9",
		Category = ProcessingNodeCategories.DataProcessing,
		DisplayName = "Mahieu: Scan Segment Merging Node",
		Description = "Truncates and merges every n sequential spectra",
		MainVersion = 0,
		MinorVersion = 1)]
	[PublisherInformation(Publisher = "Nathaniel Mahieu")]
	[ProcessingNodeAppearance(
		ImageSmallSource = "IMG_CubeBlue_16x16.png",
		ImageLargeSource = "IMG_CubeBlue_32x32.png")]
	[ProcessingNodeConstraints(UsageConstraint = UsageConstraint.Unrestricted)]
	[ConnectionPoint(
		"IncomingSpectra", 
		ConnectionDirection = ConnectionDirection.Incoming,
		ConnectionMultiplicity = ConnectionMultiplicity.Single,
		ConnectionMode = ConnectionMode.Manual,
		ConnectionRequirement = ConnectionRequirement.RequiredAtDesignTime,
		ConnectionDisplayName = ProcessingNodeCategories.SpectrumAndFeatureRetrieval,
		ConnectionDataHandlingType = ConnectionDataHandlingType.InMemory)]
	[ConnectionPointDataContract(
		"IncomingSpectra",
		MassSpecDataTypes.MSnSpectra)]
	[ConnectionPoint(
		"OutgoingSpectra", 
		ConnectionDirection = ConnectionDirection.Outgoing,
		ConnectionMultiplicity = ConnectionMultiplicity.Multiple,
		ConnectionMode = ConnectionMode.Manual,
		ConnectionRequirement = ConnectionRequirement.Optional,
		ConnectionDataHandlingType = ConnectionDataHandlingType.InMemory)]
	[ConnectionPointDataContract(
		"OutgoingSpectra",
		MassSpecDataTypes.MSnSpectra,
		DataTypeAttributes = new[] { MassSpecDataTypeAttributes.Filtered })]	
	public class MahieuSpectrumMergeNode
		: SpectrumProcessingNode
	{
		[IntegerParameter(
			Category = "Parameters",
			DisplayName = "Number of Scan Segments",
			DefaultValue = "2",
			MinimumValue = "2")]
		public IntegerParameter nSegs; /// Could autodetect but keep it simple for now.

        [DoubleParameter(
                Category = "Parameters",
                DisplayName = "Segment 1 Lower Limit",
                DefaultValue = "0.0",
                MinimumValue = "0.0")]
        public DoubleParameter seg1Low;

        [DoubleParameter(
                Category = "Parameters",
                DisplayName = "Segment 1 Upper Limit",
                DefaultValue = "3000.0",
                MinimumValue = "0.0")]
        public DoubleParameter seg1Upp;

        [DoubleParameter(
                Category = "Parameters",
                DisplayName = "Segment 2 Lower Limit",
                DefaultValue = "0.0",
                MinimumValue = "0.0")]
        public DoubleParameter seg2Low;

        [DoubleParameter(
                Category = "Parameters",
                DisplayName = "Segment 2 Upper Limit",
                DefaultValue = "3000.0",
                MinimumValue = "0.0")]
        public DoubleParameter seg2Upp;

        [DoubleParameter(
                Category = "Parameters",
                DisplayName = "Segment 3 Lower Limit",
                DefaultValue = "0.0",
                MinimumValue = "0.0")]
        public DoubleParameter seg3Low;

        [DoubleParameter(
                Category = "Parameters",
                DisplayName = "Segment 3 Upper Limit",
                DefaultValue = "3000.0",
                MinimumValue = "0.0")]
        public DoubleParameter seg3Upp;



        /// <summary>
        /// REPLACE THIS
        /// 
        /// Assumes scan segment order does not vary.  Not sure if this is a fair assumption.
        /// </returns>
        protected override MassSpectrumCollection ProcessSpectra(MassSpectrumCollection spectra)
		{
			 return NMerge(spectra);			
		}

		private MassSpectrumCollection NMerge(IEnumerable<MassSpectrum> spectra)
		{
            List<double> lowers = new List<double> { this.seg1Low.Value, this.seg2Low.Value, this.seg3Low.Value };
            List<double> uppers = new List<double> { this.seg1Upp.Value, this.seg2Upp.Value, this.seg3Upp.Value };

            int nSpecs = spectra.Aggregate(0, (x, y) => x + 1);

            double count = 0;
            double segs = (double)this.nSegs.Value;

            MassSpectrumCollection massSpectrumCollection = new MassSpectrumCollection((int)(nSpecs / segs));

            int nCentsY = 0;
            int nCentsN = 0;

            int nCentsYtot = 0;
            int nCentsNtot = 0;
            int nSpecStart = nSpecs;

            base.WriteLogMessage(MessageLevel.Info, "Mahieu.NMerge - Starting - Number of Specs: {0}", nSpecs);
            MassSpectrum buildingSpec = null;
            foreach (MassSpectrum spec in spectra)
            {
                // base.WriteLogMessage(MessageLevel.Info, "Mahieu.NMerge - Header. {0} - {1} - {2} - {3}", spec.Header.ScanRange, spec.Header.MasterScanRange, spec.Header.ScanNumbers, spec.Header.MasterScanNumbers);
                // base.WriteLogMessage(MessageLevel.Info, "Mahieu.NMerge - {0} - {1} - {2} - {3}", spec.ScanEvent.IsolationOffset, spec.Header.MasterScanRange, spec.Header.ScanNumbers, spec.Header.MasterScanNumbers);


                int currentSeg = (int)((int)(count) % (int)segs);

                if (count > 0 & currentSeg == 0)
                {
                    //buildingSpec.Header.HighPosition = buildingSpec.PeakCentroids.FindClosestPeak(10000).Position;
                    //buildingSpec.Header.LowPosition = buildingSpec.PeakCentroids.FindClosestPeak(0).Position;

                    buildingSpec.Header.HighPosition = uppers.Max();
                    buildingSpec.Header.LowPosition = lowers.Min();

                    buildingSpec.Header.BasePeakIntensity = buildingSpec.PeakCentroids.FindMostIntensePeak().Intensity;
                    buildingSpec.Header.BasePeakPosition = buildingSpec.PeakCentroids.FindMostIntensePeak().Position;
                    buildingSpec.Header.TotalIntensity = buildingSpec.PeakCentroids.Aggregate(0.0, (x, y) => x + y.Intensity);
                    
                    // fake it. important?
                    buildingSpec.ScanEvent.IsolationMass = (uppers.Max() + lowers.Min()) / 2;
                    buildingSpec.ScanEvent.IsolationWidth = (uppers.Max() - lowers.Min());


                    if (!buildingSpec.IsValid)
                    {
                        base.WriteLogMessage(MessageLevel.Info, "Building Spec Invalid");
                    }

                    // spec.Precursor.IsolationMass, spec.Precursor.IonInjectTime, spec.Precursor.IsolationWindow = 0; spec.Precursor.MeasuredMonoisotopicPeakCentroids is empty.
                    // spec.Traits mostly mass accuracy, should be invariant
                    // spec.ScanEvent Isolation mass and such could be important
                    // spec.Header - appears to be important.

                    // Every nSegs specs add the merged spec to the collection and start a new one.
                    massSpectrumCollection.Add(buildingSpec);

                    nCentsY = buildingSpec.PeakCentroids.Aggregate(0, (x, y) => x + 1);
                    nCentsYtot = nCentsYtot + nCentsY;

                    if (nCentsY < nCentsN) {
                        base.WriteLogMessage(MessageLevel.Warn, "Mahieu.NMerge - Number of centroids kept ({0}) is larger than number of centroids discarded ({1}). Are scan segments defined appropriately?", nCentsY, nCentsN);
                    }

                    if (count % (nSpecs/4) == 0)
                    {
                        base.WriteLogMessage(MessageLevel.Info, "Mahieu.NMerge - Spot Check - Centroids kept: {0}; Centroids discarded {1}.", nCentsY, nCentsN);
                    }
                    nCentsNtot = nCentsNtot + nCentsN;
                    nCentsY = 0;
                    nCentsN = 0;
                }
                if (currentSeg == 0)
                {
                    buildingSpec = new MassSpectrum();
                    buildingSpec.Header = spec.Header;
                    buildingSpec.ScanEvent = spec.ScanEvent;
                    buildingSpec.Precursor = spec.Precursor;

                    int ncents = buildingSpec.PeakCentroids.Aggregate(0, (x, y) => x + 1);
                    int nprofs = buildingSpec.ProfilePoints.Aggregate(0, (x, y) => x + 1);

                    buildingSpec.PeakCentroids = new MassCentroidCollection(ncents * 4);
                    buildingSpec.ProfilePoints = new SpectrumPointCollection(nprofs * 4);

                    // buildingSpec = spec;
                }

                // Merge current scan with buildingSpec
                //base.WriteLogMessage(MessageLevel.Info, "Mahieu.NMerge - Current Segment {0}.", currentSeg);

                // base.WriteLogMessage(MessageLevel.Info, "Mahieu.NMerge - Precursor Info. {0}", spec.Precursor.MeasuredMonoisotopicPeakCentroids.Max(t => t.Position));
                // base.WriteLogMessage(MessageLevel.Info, "Mahieu.NMerge - Precursor Info. {0} - {1} - {2}", spec.ScanEvent.IsolationWidth, spec.ScanEvent.IsolationMass, spec.ScanEvent.IsolationWindow);

                foreach (MassCentroid m in spec.PeakCentroids)
                {
                    if ((m.Position >= lowers[currentSeg]) && (m.Position < uppers[currentSeg]))
                    {
                        buildingSpec.PeakCentroids.Add(m);
                    }
                    else
                    {
                        nCentsN = nCentsN + 1;
                        // base.WriteLogMessage(MessageLevel.Info, "Mahieu.NMerge - Discarded Centroid.");
                    }
                }

                foreach (SpectrumPoint m in spec.ProfilePoints)
                {
                    if ((m.Position >= lowers[currentSeg]) && (m.Position < uppers[currentSeg]))
                    {
                        buildingSpec.ProfilePoints.Add(m);
                    }
                    else
                    {
                        // counter
                    }
                }

                count += 1;
            }



            int nSpecs2 = massSpectrumCollection.Aggregate(0, (x, y) => x + 1);
            SendAndLogMessage("Mahieu NMerge Completed - Specs Remaining: {0}/{1} - Centroids remaining: {3}/{4} - Fraction: {2}", nSpecs2, nSpecStart, (double)nCentsYtot/((double)nCentsNtot + (double)nCentsYtot), nCentsNtot, nCentsYtot);

            if ((double)nCentsYtot / ((double)nCentsNtot + (double)nCentsYtot) < 0.9) {
                SendAndLogWarningMessage("Mahieu NMerge Reported >10% centroids discarded. Are scan segments set appropriately?");
            }

            return massSpectrumCollection;
		}
	}
}

