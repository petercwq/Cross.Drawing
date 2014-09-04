#region Using directives
using System;
#endregion

namespace Cross.Drawing
{
    /// <summary>
    /// 
    /// </summary>
    public class NotMatchPaintTypeException : Cross.Drawing.DrawingExceptionBase
    {
        #region Get Message
        /// <summary>
        /// Fully loaded message builder
        /// </summary>
        static string GetMessage(Type expectedType, Type currentType)
        {
            string result = "";
            //if (Cross.Application.Language.Language.Current == Cross.Application.Language.Vietnamese.Vietnamese.Instance)
            //    result = string.Format("{0} không phù hợp khi rasterizer mong muốn {1} ",currentType,expectedType);
            //else
            result = string.Format("{0} paint is not expected. Rasterizer is expecting {1} ", currentType, expectedType);
            return result;
        }
        #endregion

        #region Publish
        /// <summary>
        /// Publish exception
        /// </summary>
        /// <param name="sender">the subsystem that raises this error</param>
        /// <param name="control">the control that is not supported</param>
        [System.Diagnostics.DebuggerHidden]
        public static void Publish(Type expectedType, Type currentType)
        {
            NotMatchPaintTypeException error = new NotMatchPaintTypeException(expectedType, currentType);
            //write error to log            
            if (Cross.Drawing.DrawingExceptionBase.LogError) Cross.Log.Error(error);
            //throw error
            if (!Cross.Drawing.DrawingExceptionBase.SilentError) throw error;
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Full constructor
        /// </summary>
        /// <param name="sender">the subsystem that raises this error</param>
        /// <param name="control">the control that is not supported</param>
        public NotMatchPaintTypeException(Type expectedType, Type currentType)
            : base(GetMessage(expectedType, currentType)) { }
        #endregion
    }
}
