
namespace Cross.Drawing
{
    /// <summary>
    /// Contains information for a single stop in a <see cref="ColorRamp"/>
    /// </summary>
    public class ColorStop
    {
        /// <summary>
        /// Gets/Sets the color for this stop
        /// </summary>
        public Color Color = Colors.Transparent;

        /// <summary>
        /// HuyHM change to internal field
        /// This internal use for better performance
        /// </summary>
        internal double mStop = 0;
        /// <summary>
        /// Gets/Sets stop value (must be within range [0..1] )
        /// </summary>
        public double Stop
        {
            get { return mStop; }
            set
            {
                if (mStop != value)
                {
                    mStop = value;
                    if (mStop < 0) mStop = 0;
                    else if (mStop > 1) mStop = 1;
                }
            }
        }

        /// <summary>
        /// Converts to display text
        /// </summary>
        public override string ToString()
        {
            return string.Format("Stop: {0} - Color: {1}", mStop, Color);
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public ColorStop()
        { }

        /// <summary>
        /// Create a new instance
        /// </summary>
        /// <param name="color">Color value of stop</param>
        /// <param name="stop">Position of stop (must be in range [0..1]</param>
        public ColorStop(Color color, double stop)
        {
            this.Color = color;
            if (stop < 0) mStop = 0;
            else if (stop > 1) mStop = 1;
            else this.mStop = stop;
        }
    }
}


