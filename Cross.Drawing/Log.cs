using System;

namespace Cross
{
    public interface ITextWriter
    {
        void WriteLine(string message);
    }

    internal class DefaultTextWriter : ITextWriter
    {
        public void WriteLine(string message)
        {
            System.Diagnostics.Debug.WriteLine(message);
        }
    }

    public enum LogLevel
    {
        Debug = 0,
        Info,
        Warning,
        Error
    }

    public class Log
    {

        static Log()
        {
            Level = LogLevel.Debug;
            Writer = new DefaultTextWriter();
        }

        public static ITextWriter Writer { get; set; }
        public static LogLevel Level { get; set; }

        private static void Write(string message, LogLevel level)
        {
            if (level >= Level && Writer != null)
                Writer.WriteLine(message/* + Environment.NewLine*/);
        }

        internal static void Warning(string p)
        {
            Write(p, LogLevel.Warning);
        }

        internal static void Debug(string p, int x)
        {
            Write(p, LogLevel.Debug);
        }

        internal static void Debug(string p)
        {
            Write(p, LogLevel.Debug);
        }

        internal static object Start(string scope)
        {
            throw new NotImplementedException();
        }

        internal static void End(object scope)
        {
            throw new NotImplementedException();
        }

        internal static void Error(Exception error)
        {
            Write(error.ToString(), LogLevel.Error);
        }
    }
}
