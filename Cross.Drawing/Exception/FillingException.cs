#region Using directives
using System;
#endregion

namespace Cross.Drawing
{
    /// <summary>
    /// 
    /// </summary>
    public class FillingException : Cross.ExceptionBase
    {
        #region Get Message
        /// <summary>
        /// Fully loaded message builder
        /// </summary>
        static string GetMessage(Type fillingType, string message)
        {
            string result = "";
            //if (Cross.Application.Language.Language.Current == Cross.Application.Language.Vietnamese.Vietnamese.Instance)
            //    result = string.Format("Filling exception in {0}. Details : {1} ",fillingType,message);
            //else
            result = string.Format("Filling exception in {0}. Details : {1} ", fillingType, message);
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
        public static void Publish(Type fillingType, string message)
        {
            FillingException error = new FillingException(fillingType, message);
            //write error to log            
            if (Cross.ExceptionBase.LogError) Cross.Log.Error(error);
            //throw error
            if (!Cross.ExceptionBase.SilentError) throw error;
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Full constructor
        /// </summary>
        /// <param name="sender">the subsystem that raises this error</param>
        /// <param name="control">the control that is not supported</param>
        public FillingException(Type fillingType, string message)
            : base(GetMessage(fillingType, message)) { }
        #endregion
    }
}
