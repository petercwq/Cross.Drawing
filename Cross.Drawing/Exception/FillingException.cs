using System;

namespace Cross.Drawing
{
    public class FillingException : DrawingExceptionBase
    {
        /// <summary>
        /// Fully loaded message builder
        /// </summary>
        static string GetMessage(Type fillingType, string message)
        {
            return string.Format("Filling exception in {0}. Details : {1} ", fillingType, message);
        }

        /// <summary>
        /// Publish exception
        /// </summary>
        /// <param name="sender">the subsystem that raises this error</param>
        /// <param name="control">the control that is not supported</param>
        [System.Diagnostics.DebuggerHidden]
        public static void Publish(Type fillingType, string message)
        {
            FillingException error = new FillingException(fillingType, message);
            BasePublish(error);
        }

        /// <summary>
        /// Full constructor
        /// </summary>
        /// <param name="sender">the subsystem that raises this error</param>
        /// <param name="control">the control that is not supported</param>
        public FillingException(Type fillingType, string message)
            : base(GetMessage(fillingType, message))
        {
        }
    }
}