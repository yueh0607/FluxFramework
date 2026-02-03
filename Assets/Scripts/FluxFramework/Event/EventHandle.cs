using System;
using System.Collections.Generic;

namespace FluxFramework
{
    internal struct EventSubscriber
    {
        public Node Node;
        public Delegate Handler;
    }

    public struct EventHandle : IDisposable
    {
        internal LinkedListNode<EventSubscriber> ListNode;
        internal LinkedList<EventSubscriber> List;

        public bool IsValid => ListNode != null && List != null;

        public void Dispose()
        {
            if (List != null && ListNode != null)
            {
                List.Remove(ListNode);
            }
            ListNode = null;
            List = null;
        }
    }
}
