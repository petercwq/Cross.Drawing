using System;

namespace Cross.Drawing
{
    public class UnsupportedException : DrawingExceptionBase
    {
        static string GetMessage(Type type)
        {
            return string.Format("{0} is not supported", type);
        }

        static string GetMessage(string message, string region)
        {
            return string.Format("{0} is not supported in {1}", message, region);
        }

        [System.Diagnostics.DebuggerHidden]
        public static void Publish(Type type)
        {
            var error = new UnsupportedException(type);
            BasePublish(error);
        }

        [System.Diagnostics.DebuggerHidden]
        public static void Publish(string message, string region)
        {
            var error = new UnsupportedException(message, region);
            BasePublish(error);
        }

        public UnsupportedException(Type type)
            : base(GetMessage(type))
        {
        }

        public UnsupportedException(string message, string region)
            : base(GetMessage(message, region))
        {
        }
    }
}
