using System;

namespace Cross
{
    public class IncompatibleTypeException : Cross.ExceptionBase
    {
        internal static void Publish(object state, Type type)
        {
            throw new NotImplementedException();
        }
    }
}
