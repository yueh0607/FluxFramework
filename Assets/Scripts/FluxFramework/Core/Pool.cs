using System.Collections.Generic;

namespace FluxFramework
{
    public static class Pool<T> where T : class, IPoolable, new()
    {
        private static readonly Stack<T> _pool = new Stack<T>();

        public static T Spawn()
        {
            T obj = _pool.Count > 0 ? _pool.Pop() : new T();
            obj.OnSpawn();
            return obj;
        }

        public static void Despawn(T obj)
        {
            if (obj == null) return;
            obj.OnDespawn();
            _pool.Push(obj);
        }

        public static void Clear()
        {
            _pool.Clear();
        }

        public static void Prewarm(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var obj = new T();
                _pool.Push(obj);
            }
        }

        public static int Count => _pool.Count;
    }
}
