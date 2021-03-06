using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DelftTools.Functions;
using DelftTools.Functions.Generic;
using DelftTools.Utils.Collections;
using GeoAPI.Extensions.Networks;
using GeoAPI.Geometries;

namespace GeoAPI.Extensions.Coverages
{
    /// <summary>
    /// Network coverage is a function defined on the network in a linear way.
    /// TODO: introduce IDiscreteCurveCoverage (see ISO/OGC) and use it as a base interface for INetworkCoverage
    /// </summary>
    public interface INetworkCoverage : ICoverage
    {
        INetwork Network { get; set; }

        IVariable<INetworkLocation> Locations { get; }

        IVariable<INetworkSegment> Segments { get; }

        SegmentGenerationMethod SegmentGenerationMethod { get; set; }

        // TODO: remove it, use default value from component

        double DefaultValue { get; set; }

        /// <summary>
        /// Returns the value for the networkLocation. If there is no data available at the location
        /// the value will be interpolated.
        /// </summary>
        /// <param name="networkLocation"></param>
        /// <returns></returns>
        double Evaluate(INetworkLocation networkLocation);

        double Evaluate(IBranchFeature branchFeature);

        /// <summary>
        /// Relatively fast evaluation for chainage-sorted locations on a single branch.
        /// </summary>
        /// <param name="branch">The branch on which to evaluate the coverage</param>
        /// <param name="chainageValues">Sorted list with chainage values from low to high on the branch to evaluate</param>
        /// <param name="knownBranchLocations">Coverage locations for <param name="branch"/></param>
        /// <returns>List with values</returns>
        IList<double> EvaluateWithinBranch(IBranch branch, IOrderedEnumerable<double> chainageValues, IList<INetworkLocation> knownBranchLocations);

        double Evaluate(DateTime dateTime, INetworkLocation networkLocation);

        double Evaluate(DateTime time, IBranchFeature branchFeature);

        IFunction GetTimeSeries(IBranchFeature branchFeature);

        INetworkLocation GetLocationOnBranch(double x, double y);

        INetworkCoverage AddTimeFilter(DateTime time);

        void SetLocations(IEnumerable<INetworkLocation> locations);

        void AddValuesForTime(IEnumerable values, DateTime time);

        double Evaluate(INetworkSegment segment);

        IList<INetworkLocation> GetLocationsForBranch(IBranch branch);

        /// <summary>
        /// Flag to indicate the coverage is being sorted. Remove actions will be followed be add
        /// </summary>
        bool IsSorting { get; }

        /// <summary>
        /// Occurs when a change in the network occurs. No bubbling because there is not a clear parent/child relationship betweeen coverage and network
        /// </summary>
        event EventHandler<NotifyCollectionChangingEventArgs> NetworkCollectionChanged;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="coordinate"></param>
        /// <returns></returns>
        INetworkLocation GetNearestNetworkLocation(ICoordinate coordinate);
    }
}