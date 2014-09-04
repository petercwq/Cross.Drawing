#region Using directives
using System;
#endregion

namespace Cross.Drawing
{
    /// <summary>
    /// 
    /// </summary>
    public class DrawingExceptionBase : Cross.ExceptionBase
    {
        #region OPTIONAL (if not inherited from Cross.Application.ExceptionBase)
        /*
        /// <summary>
        /// When true, static exception publishers will write exception to logs
        /// </summary>
        public static bool LogError;
        
        /// <summary>
        /// When true, static exception publishers will not throw exception
        /// </summary>
        public static bool SilentError;
        */
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a new empty instance
        /// </summary>
        public DrawingExceptionBase() { }

        /// <summary>
        /// Creates a new instance with provided message
        /// </summary>
        public DrawingExceptionBase(string message) : base(message) { }

        /// <summary>
        /// Creates a new instance with message & inner as provided by the original error
        /// </summary>
        public DrawingExceptionBase(Exception original) : base(original.Message, original.InnerException) { }

        /// <summary>
        /// Creates a new instance that stacks above an inner exception
        /// </summary>
        /// <param name="message">The message for this exception</param>
        /// <param name="innerError">Inner stack exception</param>
        public DrawingExceptionBase(string message, Exception innerError) : base(message, innerError) { }
        #endregion
    }
}
