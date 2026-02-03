using System.Collections.Concurrent;

namespace FluxFramework
{
    /// <summary>
    /// 邮箱
    /// 线程安全的消息队列，用于跨线程通信
    /// </summary>
    public class MailBox
    {
        private readonly ConcurrentQueue<Message> _queue = new ConcurrentQueue<Message>();

        /// <summary>
        /// 投递消息到邮箱
        /// </summary>
        public void Post(Message msg)
        {
            _queue.Enqueue(msg);
        }

        /// <summary>
        /// 尝试从邮箱取出一条消息
        /// </summary>
        public bool TryReceive(out Message msg)
        {
            return _queue.TryDequeue(out msg);
        }

        /// <summary>
        /// 邮箱中的消息数量
        /// </summary>
        public int Count => _queue.Count;

        /// <summary>
        /// 邮箱是否为空
        /// </summary>
        public bool IsEmpty => _queue.IsEmpty;

        /// <summary>
        /// 清空邮箱（回收所有消息）
        /// </summary>
        public void Clear()
        {
            while (_queue.TryDequeue(out var msg))
            {
                MessagePool.Despawn(msg);
            }
        }
    }

    /// <summary>
    /// 节点消息扩展方法
    /// </summary>
    public static class NodeMailExtensions
    {
        /// <summary>
        /// 发送消息给目标节点
        /// 自动判断是否跨线程：同线程直接派发，跨线程投递到邮箱
        /// </summary>
        public static void Send<T>(this Node sender, Node target, System.Action<T> setup = null)
            where T : Message, new()
        {
            if (target == null) return;

            var msg = MessagePool.Spawn<T>();
            msg.SenderId = sender.Id;
            msg.TargetId = target.Id;
            setup?.Invoke(msg);

            if (sender.OwnerThread == target.OwnerThread)
            {
                // 同线程，直接派发
                target.OnMessage(msg);
                MessagePool.Despawn(msg);
            }
            else
            {
                // 跨线程，投递到目标的 OwnerThread 邮箱
                target.OwnerThread?.InBox.Post(msg);
            }
        }

        /// <summary>
        /// 发送消息给指定 ID 的节点
        /// </summary>
        public static void SendTo<T>(this Node sender, uint targetId, System.Action<T> setup = null)
            where T : Message, new()
        {
            var target = NodePool.FindById(targetId);
            if (target != null)
            {
                sender.Send(target, setup);
            }
        }
    }
}
