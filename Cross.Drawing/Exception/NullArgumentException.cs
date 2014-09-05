using System;

namespace Cross.Drawing
{
    public class NullArgumentException : DrawingExceptionBase
    {
        /// <summary>
        /// Fully loaded message builder
        /// </summary>
        static string GetMessage(Type type, string parameter)
        {
            return string.Format("Null is not expected, expect a instance of {0} for {1}", type, parameter);
        }

        /// <summary>
        /// Publish exception
        /// </summary>
        /// <param name="sender">the subsystem that raises this error</param>
        /// <param name="control">the control that is not supported</param>
        [System.Diagnostics.DebuggerHidden]
        public static void Publish(Type type, string parameter)
        {
            NullArgumentException error = new NullArgumentException(type, parameter);
            BasePublish(error);
        }

        /// <summary>
        /// Full constructor
        /// </summary>
        /// <param name="sender">the subsystem that raises this error</param>
        /// <param name="control">the control that is not supported</param>
        public NullArgumentException(Type type, string parameter)
            : base(GetMessage(type, parameter))
        {
        }
    }
}
