using System;
using System.Collections.Generic;

namespace FluxFramework
{
    /// <summary>
    /// 消息对象池
    /// 用于复用消息对象，避免频繁 GC
    /// </summary>
    public static class MessagePool
    {
        private static readonly Dictionary<Type, Stack<Message>> _pools = new Dictionary<Type, Stack<Message>>();

        /// <summary>
        /// 从池中获取消息
        /// </summary>
        public static T Spawn<T>() where T : Message, new()
        {
            var type = typeof(T);
            T msg;

            if (_pools.TryGetValue(type, out var pool) && pool.Count > 0)
            {
                msg = (T)pool.Pop();
            }
            else
            {
                msg = new T();
            }

            msg.OnSpawn();
            return msg;
        }

        /// <summary>
        /// 将消息放回池中
        /// </summary>
        public static void Despawn(Message msg)
        {
            if (msg == null) return;

            msg.OnDespawn();

            var type = msg.GetType();
            if (!_pools.TryGetValue(type, out var pool))
            {
                pool = new Stack<Message>();
                _pools[type] = pool;
            }
            pool.Push(msg);
        }

        /// <summary>
        /// 清空所有池
        /// </summary>
        public static void Clear()
        {
            _pools.Clear();
        }

        /// <summary>
        /// 预热指定类型的消息池
        /// </summary>
        public static void Prewarm<T>(int count) where T : Message, new()
        {
            var type = typeof(T);
            if (!_pools.TryGetValue(type, out var pool))
            {
                pool = new Stack<Message>();
                _pools[type] = pool;
            }

            for (int i = 0; i < count; i++)
            {
                pool.Push(new T());
            }
        }
    }
}
