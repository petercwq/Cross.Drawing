using System;

namespace Cross.Drawing
{
    public class IncompatibleTypeException : DrawingExceptionBase
    {
        static string GetMessage(object obj, Type type)
        {
            return string.Format("Instance {0} is not compatible with type {1}", obj, type);
        }

        [System.Diagnostics.DebuggerHidden]
        public static void Publish(object obj, Type type)
        {
            var error = new IncompatibleTypeException(obj, type);
            BasePublish(error);
        }

        public IncompatibleTypeException(object obj, Type type)
            : base(GetMessage(obj, type))
        {
        }
    }
}
