using System;

namespace Cross
{
    public class UnsupportedException
    {
        internal static void Publish(string p1, string p2)
        {
            throw new NotImplementedException();
        }

        internal static void Publish(Type type)
        {
            throw new NotImplementedException();
        }
    }
}
