using System;
using System.Collections.Generic;

namespace FluxFramework
{
    /// <summary>
    /// 节点对象池
    /// 负责分配唯一 ID 和管理节点的生命周期
    /// 内部使用 PoolContainerNode 实现节点化的池管理
    /// </summary>
    public static class NodePool
    {
        #region 字段

        private static uint _nextId = 1;
        
        /// <summary>
        /// 活跃节点表（ID -> Node）
        /// </summary>
        private static readonly Dictionary<uint, Node> _activeNodes = new Dictionary<uint, Node>();

        /// <summary>
        /// 池容器节点
        /// </summary>
        private static PoolContainerNode _poolContainer;

        #endregion

        #region 池容器

        /// <summary>
        /// 获取池容器节点
        /// </summary>
        public static PoolContainerNode PoolContainer => _poolContainer;

        /// <summary>
        /// 设置池容器（由 FluxRoot 调用）
        /// </summary>
        internal static void SetPoolContainer(PoolContainerNode container)
        {
            _poolContainer = container;
        }

        #endregion

        #region Spawn

        /// <summary>
        /// 从池中获取节点
        /// </summary>
        public static T Spawn<T>() where T : Node, new()
        {
            var node = SpawnWithoutInit<T>();
            node.OnSpawn();
            return node;
        }

        /// <summary>
        /// 从池中获取节点，但不调用 OnSpawn
        /// 用于 AddChild，在设置 OwnerThread 后再调用 OnSpawn
        /// </summary>
        internal static T SpawnWithoutInit<T>() where T : Node, new()
        {
            var type = typeof(T);
            T node;

            // 尝试从节点化的池中获取
            if (_poolContainer != null)
            {
                var typePool = _poolContainer.GetTypePool(type);
                if (typePool != null && typePool.PooledCount > 0)
                {
                    node = (T)typePool.Pop();
                    // 分配新的唯一 ID
                    node.Id = _nextId++;
                    _activeNodes[node.Id] = node;
                    return node;
                }
            }

            // 池中没有，创建新节点
            node = new T();
            node.Id = _nextId++;
            _activeNodes[node.Id] = node;

            return node;
        }

        #endregion

        #region Despawn

        /// <summary>
        /// 将节点放回池中
        /// </summary>
        public static void Despawn(Node node)
        {
            if (node == null) return;

            // 从活跃节点表移除
            _activeNodes.Remove(node.Id);

            // 调用生命周期回调
            node.OnDespawn();

            // 放入节点化的池中
            if (_poolContainer != null)
            {
                var type = node.GetType();
                var typePool = _poolContainer.GetOrCreateTypePool(type);
                typePool.Push(node);
            }

            // 重置 ID（防止误用）
            node.Id = 0;
        }

        #endregion

        #region 查找

        /// <summary>
        /// 通过 ID 查找活跃节点
        /// </summary>
        public static Node FindById(uint id)
        {
            _activeNodes.TryGetValue(id, out var node);
            return node;
        }

        /// <summary>
        /// 通过 ID 查找指定类型的活跃节点
        /// </summary>
        public static T FindById<T>(uint id) where T : Node
        {
            if (_activeNodes.TryGetValue(id, out var node))
            {
                return node as T;
            }
            return null;
        }

        #endregion

        #region 统计

        /// <summary>
        /// 活跃节点数量
        /// </summary>
        public static int ActiveCount => _activeNodes.Count;

        /// <summary>
        /// 指定类型的池中节点数量
        /// </summary>
        public static int PoolCount<T>() where T : Node
        {
            if (_poolContainer == null) return 0;
            var typePool = _poolContainer.GetTypePool<T>();
            return typePool?.PooledCount ?? 0;
        }

        /// <summary>
        /// 所有池中节点总数
        /// </summary>
        public static int TotalPooledCount => _poolContainer?.TotalPooledCount ?? 0;

        #endregion

        #region 预热

        /// <summary>
        /// 预热指定类型的节点池
        /// </summary>
        public static void Prewarm<T>(int count) where T : Node, new()
        {
            if (_poolContainer == null)
            {
                UnityEngine.Debug.LogWarning("NodePool: PoolContainer is not initialized. Call FluxRoot.Initialize() first.");
                return;
            }

            var typePool = _poolContainer.GetOrCreateTypePool<T>();
            for (int i = 0; i < count; i++)
            {
                var node = new T();
                typePool.Push(node);
            }
        }

        #endregion

        #region 清理

        /// <summary>
        /// 清空所有池和活跃节点
        /// </summary>
        public static void Clear()
        {
            _activeNodes.Clear();
            _poolContainer?.ClearAll();
            _nextId = 1;
        }

        #endregion
    }
}
