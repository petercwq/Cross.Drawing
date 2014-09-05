using System;

namespace Cross.Drawing
{
    public class NotMatchPaintTypeException : Cross.Drawing.DrawingExceptionBase
    {
        /// <summary>
        /// Fully loaded message builder
        /// </summary>
        static string GetMessage(Type expectedType, Type currentType)
        {
            return string.Format("{0} paint is not expected. Rasterizer is expecting {1} ", currentType, expectedType);
        }

        /// <summary>
        /// Publish exception
        /// </summary>
        /// <param name="sender">the subsystem that raises this error</param>
        /// <param name="control">the control that is not supported</param>
        [System.Diagnostics.DebuggerHidden]
        public static void Publish(Type expectedType, Type currentType)
        {
            NotMatchPaintTypeException error = new NotMatchPaintTypeException(expectedType, currentType);
            BasePublish(error);
        }

        /// <summary>
        /// Full constructor
        /// </summary>
        /// <param name="sender">the subsystem that raises this error</param>
        /// <param name="control">the control that is not supported</param>
        public NotMatchPaintTypeException(Type expectedType, Type currentType)
            : base(GetMessage(expectedType, currentType))
        {
        }
    }
}
