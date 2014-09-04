using System;

namespace Cross
{
    public class ExceptionBase : Exception
    {
        public static bool SilentError = false;
        public static bool LogError = true;
        private DateTime datetime = DateTime.Now;
        public DateTime Time
        {
            get
            {
                return datetime;
            }
        }
        public ExceptionBase()
        {
        }
        public ExceptionBase(string message)
            : base(message)
        {
        }
        public ExceptionBase(Exception original)
            : base(original.Message, original.InnerException)
        {
        }
        public ExceptionBase(string message, Exception innerError)
            : base(message, innerError)
        {
        }
    }
}
