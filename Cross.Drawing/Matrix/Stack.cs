using System.Collections.Generic;

namespace System.Collections
{
    public interface IStack
    {
        void Push(Object e);
        Object Pop();
        Object Top();
        void EnsureCapacity();
    }

    public class Stack : Stack<object>
    {
        public Stack()
            : base()
        {

        }

        public Stack(Stack stack)
            : base(stack)
        {

        }
    }
}