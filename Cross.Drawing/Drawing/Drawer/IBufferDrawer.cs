


namespace Cross.Drawing
{
    /// <summary>
    /// This interface allows interaction with an <see cref="IDrawer"/> that uses <see cref="PixelsBuffer"/> as their renderation target
    /// </summary>
    public interface IBufferDrawer : IDrawer
    {
        /// <summary>
        /// Gets/Sets the buffer where results will be rendered to
        /// </summary>
        PixelsBuffer Buffer
        { get; set; }
    }
}
