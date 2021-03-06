using System;
using DelftTools.Functions;
using GisSharpBlog.NetTopologySuite.Geometries;

namespace NetTopologySuite.Extensions.Coverages
{
    public class PointTypeConverter : TypeConverterBase<Point>
    {
        public override Type[] StoreTypes
        {
            get { return new [] {typeof(double), typeof(double)}; }
        }

        public override string[] VariableNames
        {
            get { return new [] {"x", "y"}; }
        }

        public override string[] VariableStandardNames
        {
            get { return new[] { FunctionAttributes.StandardNames.Longitude_X, FunctionAttributes.StandardNames.Latitude_Y }; }
        }

        public override string[] VariableUnits
        {
            get { return new[] { FunctionAttributes.StandardUnits.Long_X_Degr, FunctionAttributes.StandardUnits.Lat_Y_Degr }; }
        }

        public override Point ConvertFromStore(object source)
        {
            var sourceTuple = (object[])source;

            var x = Convert.ToDouble(sourceTuple[0]);
            var y = Convert.ToDouble(sourceTuple[1]);

            return new Point(x, y);
        }

        public override object[] ConvertToStore(Point source)
        {
            return new object[] { source.X, source.Y };
        }
    }
}