namespace DelftTools.Functions.Generic
{
    public enum InterpolationType
    {
        /// <summary>
        /// Performs a piece-wise interpolation. On extrapolation nearest defined value is used.
        /// </summary>
        Constant,
        /// <summary>
        /// Provider linear inter- and extra-polation
        /// </summary>
        Linear,

        None
    }
}