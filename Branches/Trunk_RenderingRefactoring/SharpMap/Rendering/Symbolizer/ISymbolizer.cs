using System;
using SharpMap.Layers;

namespace SharpMap.Rendering.Symbolizer
{
    /// <summary>
    /// Basic interface for all symbolizers
    /// </summary>
    public interface ISymbolizer : ICloneable
    {
        /// <summary>
        /// Gets or sets a value indicating which <see cref="SmoothingMode"/> is to be used for rendering
        /// </summary>
        Smoothing SmoothingMode { get; set; }

        /// <summary>
        /// Gets or sets a value indicating which <see cref="PixelOffsetMode"/> is to be used for rendering
        /// </summary>
        PixelOffset PixelOffsetMode { get; set; }        

        /// <summary>
        /// Method to indicate that the symbolizer has to be prepared.
        /// </summary>
        /// <param name="g">The graphics object</param>
        /// <param name="map">The map</param>
        /// <param name="aproximateNumberOfGeometries">The approximate number of geometries</param>
        void Begin(IGraphics g, Map map, int aproximateNumberOfGeometries);

        /// <summary>
        /// Method to indicate that the symbolizer should do its symbolizer work.
        /// </summary>
        /// <param name="g">The graphics object</param>
        /// <param name="map">The map</param>
        void Symbolize(IGraphics g, Map map);

        /// <summary>
        /// Method to indicate that the symbolizers work is done and it can clean up.
        /// </summary>
        /// <param name="g">The graphics object</param>
        /// <param name="map">The map</param>
        void End(IGraphics g, Map map);

        /*
        /// <summary>
        /// Gets the icon for the symbolizer
        /// </summary>
        Image Icon { get; } 
         */
    }

    /// <summary>
    /// Generic interface for symbolizers that render symbolize specific geometries
    /// </summary>
    /// <typeparam name="TGeometry">The allowed type of geometries to symbolize</typeparam>
    public interface ISymbolizer<TGeometry> : ISymbolizer
        where TGeometry : class
    {
        /// <summary>
        /// Function to render the geometry
        /// </summary>
        /// <param name="map">The map object, mainly needed for transformation purposes.</param>
        /// <param name="geometry">The geometry to symbolize.</param>
        /// <param name="graphics">The graphics object to use.</param>
        void Render(Map map, TGeometry geometry, IGraphics graphics);

    }
}