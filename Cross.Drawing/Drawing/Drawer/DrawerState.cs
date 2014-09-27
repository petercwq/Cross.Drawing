
namespace Cross.Drawing
{
    /// <summary>
    /// Memento object to store state of <see cref="Drawer"/>
    /// </summary>
    public class DrawerState
    {
        /// <summary>
        /// Stack of transformation matrices
        /// </summary>
        public Matrix3x3Stack MatrixStack = null;
        /// <summary>
        /// Current transformation matrix
        /// </summary>
        public Matrix3x3 CurrentTransform = null;

        /// <summary>
        /// Default constructor
        /// </summary>
        public DrawerState()
        { }
    }
}

