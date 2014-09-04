#region Using directives

#endregion

namespace Cross.Drawing
{
    /// <summary>
    /// Cache of alpha values for alpha blending
    /// </summary>
    internal class AlphaCache
    {
        #region Cache
        private uint[] mCache;
        /// <summary>
        /// Gets alpha cache
        /// </summary>
        public static uint[] Cache
        {
            get { return Instance.mCache; }
        }
        #endregion

        #region Instance
        /// <summary>
        /// Static variable for thread locking
        /// </summary>
        static object SyncRoot = new object();

        /// <summary>
        /// Actual static variable for Instance
        /// </summary>
        static AlphaCache mInstance = null;

        /// <summary>
        /// Gets the static instance of this class
        /// </summary>
        static AlphaCache Instance
        {
            get
            {
                if (mInstance == null)
                {
                    //initialize new instance
                    AlphaCache tmp = new AlphaCache();

                    //assign to static variable. Lock root for thread-safe locking
                    lock (SyncRoot)
                    {
                        mInstance = tmp;
                    }
                }

                return mInstance;
            }
        }
        #endregion

        #region Constructor
        /// <summary>
        /// Default constructor
        /// </summary>
        AlphaCache()
        {
            mCache = new uint[256 * 256];
            for (int alpha = 0; alpha < 256; alpha++)
            {
                for (int beta = 0; beta < 256; beta++)
                {
                    int alphaIdx = alpha * 256 + beta;

                    // OLD CODE of HaiNM
                    //mCache[alphaIdx] = (uint)((alpha + beta) - ((beta * alpha + 255) >> 8));

                    //HUYHM CHANGE 26 Aug 2008
                    // all code using Alpha Cache will shift value to left 24,so this cache need to pre-built including shifting
                    mCache[alphaIdx] = (uint)((alpha + beta) - ((beta * alpha + 255) >> 8)) << 24;
                }
            }
        }
        #endregion
    }
}
