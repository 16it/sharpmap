// WFS provider by Peter Robineau (peter.robineau@gmx.at)
// This file can be redistributed and/or modified under the terms of the GNU Lesser General Public License.

namespace SharpMap.Utilities.Wfs
{
    //ReSharper disable InconsistentNaming
    /// <summary>
    /// This class provides text resources specific for WFS 1.1.0.
    /// </summary>
    public class WFS_1_1_0_XPathTextResources : WFS_XPathTextResourcesBase
    {
        #region Fields and Properties

        ////////////////////////////////////////////////////////////////////////
        // XPath                                                              //                      
        // GetCapabilities WFS 1.1.0                                          //
        ////////////////////////////////////////////////////////////////////////

        private static string _XPATH_BBOX =
            "/wfs:WFS_Capabilities/wfs:FeatureTypeList/wfs:FeatureType[_PARAMCOMP_(wfs:Name, $_param1)]/ows:WGS84BoundingBox";

        private static string _XPATH_BOUNDINGBOXMAXX = "ows:UpperCorner/text()";
        private static string _XPATH_BOUNDINGBOXMAXY = "ows:UpperCorner/text()";
        private static string _XPATH_BOUNDINGBOXMINX = "ows:LowerCorner/text()";
        private static string _XPATH_BOUNDINGBOXMINY = "ows:LowerCorner/text()";

        private static string _XPATH_DESCRIBEFEATURETYPERESOURCE =
            "/wfs:WFS_Capabilities/ows:OperationsMetadata/ows:Operation[@name='DescribeFeatureType']/ows:DCP/ows:HTTP/ows:Post/@xlink:href";

        private static string _XPATH_GETFEATURERESOURCE =
            "/wfs:WFS_Capabilities/ows:OperationsMetadata/ows:Operation[@name='GetFeatureByOid']/ows:DCP/ows:HTTP/ows:Post/@xlink:href";

        private static string _XPATH_SRS =
            "/wfs:WFS_Capabilities/wfs:FeatureTypeList/wfs:FeatureType[_PARAMCOMP_(wfs:Name, $_param1)]/wfs:DefaultSRS";

        /// <summary>
        /// Gets an XPath string addressing the SRID of a featuretype in 'GetCapabilities'.
        /// </summary>
        public string XPATH_SRS
        {
            get { return _XPATH_SRS; }
        }

        /// <summary>
        /// Gets an XPath string addressing the bounding box of a featuretype in 'GetCapabilities'.
        /// </summary>
        public string XPATH_BBOX
        {
            get { return _XPATH_BBOX; }
        }

        /// <summary>
        /// Gets an XPath string addressing the URI of 'GetFeatureByOid'in 'GetCapabilities'.
        /// </summary>
        public string XPATH_GETFEATURERESOURCE
        {
            get { return _XPATH_GETFEATURERESOURCE; }
        }

        /// <summary>
        /// Gets an XPath string addressing the URI of 'DescribeFeatureType'in 'GetCapabilities'.
        /// </summary>
        public string XPATH_DESCRIBEFEATURETYPERESOURCE
        {
            get { return _XPATH_DESCRIBEFEATURETYPERESOURCE; }
        }

        /// <summary>
        /// Gets an XPath string addressing the lower corner of a featuretype's bounding box in 'GetCapabilities'
        /// for extracting 'minx'.
        /// </summary>
        public string XPATH_BOUNDINGBOXMINX
        {
            get { return _XPATH_BOUNDINGBOXMINX; }
        }

        /// <summary>
        /// Gets an XPath string addressing the lower corner of a featuretype's bounding box in 'GetCapabilities'
        /// for extracting 'miny'.
        /// </summary>
        public string XPATH_BOUNDINGBOXMINY
        {
            get { return _XPATH_BOUNDINGBOXMINY; }
        }

        /// <summary>
        /// Gets an XPath string addressing the upper corner of a featuretype's bounding box in 'GetCapabilities'
        /// for extracting 'maxx'.
        /// </summary>
        public string XPATH_BOUNDINGBOXMAXX
        {
            get { return _XPATH_BOUNDINGBOXMAXX; }
        }

        /// <summary>
        /// Gets an XPath string addressing the upper corner of a featuretype's bounding box in 'GetCapabilities'
        /// for extracting 'maxy'.
        /// </summary>
        public string XPATH_BOUNDINGBOXMAXY
        {
            get { return _XPATH_BOUNDINGBOXMAXY; }
        }

        #endregion

        #region Constructors

        #endregion
    }
}