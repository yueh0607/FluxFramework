using System;
using System.Collections.Generic;

namespace FluxFramework
{
    /// <summary>
    /// 池容器节点
    /// 作为所有类型池的父节点，体现"一切皆节点"的设计宗旨
    /// </summary>
    /// <remarks>
    /// 结构：
    /// PoolContainerNode
    /// ├── TypePoolNode&lt;BulletViewNode&gt;
    /// │   ├── (池内的 BulletViewNode 实例)
    /// │   └── ...
    /// └── TypePoolNode&lt;EnemyViewNode&gt;
    ///     └── ...
    /// </remarks>
    public class PoolContainerNode : Node
    {
        #region 字段

        /// <summary>
        /// 类型 -> 类型池节点的映射（快速查找）
        /// </summary>
        private readonly Dictionary<Type, TypePoolNode> _typePools = new Dictionary<Type, TypePoolNode>();

        #endregion

        #region 类型池管理

        /// <summary>
        /// 获取或创建指定类型的池节点
        /// </summary>
        public TypePoolNode GetOrCreateTypePool(Type type)
        {
            if (!_typePools.TryGetValue(type, out var poolNode))
            {
                // 创建新的类型池节点（直接 new，不走池）
                poolNode = new TypePoolNode(type);
                
                // 建立父子关系
                poolNode.Parent = this;
                poolNode.Depth = this.Depth + 1;
                Children.Add(poolNode);
                
                _typePools[type] = poolNode;
            }
            return poolNode;
        }

        /// <summary>
        /// 获取或创建指定类型的池节点（泛型版本）
        /// </summary>
        public TypePoolNode GetOrCreateTypePool<T>() where T : Node
        {
            return GetOrCreateTypePool(typeof(T));
        }

        /// <summary>
        /// 获取指定类型的池节点（可能为 null）
        /// </summary>
        public TypePoolNode GetTypePool(Type type)
        {
            _typePools.TryGetValue(type, out var poolNode);
            return poolNode;
        }

        /// <summary>
        /// 获取指定类型的池节点（泛型版本）
        /// </summary>
        public TypePoolNode GetTypePool<T>() where T : Node
        {
            return GetTypePool(typeof(T));
        }

        /// <summary>
        /// 获取所有类型池
        /// </summary>
        public IReadOnlyDictionary<Type, TypePoolNode> GetAllTypePools()
        {
            return _typePools;
        }

        #endregion

        #region 统计

        /// <summary>
        /// 类型池数量
        /// </summary>
        public int TypeCount => _typePools.Count;

        /// <summary>
        /// 所有池中节点总数
        /// </summary>
        public int TotalPooledCount
        {
            get
            {
                int total = 0;
                foreach (var pool in _typePools.Values)
                {
                    total += pool.PooledCount;
                }
                return total;
            }
        }

        #endregion

        #region 清理

        /// <summary>
        /// 清空所有池
        /// </summary>
        public void ClearAll()
        {
            foreach (var poolNode in _typePools.Values)
            {
                poolNode.Clear();
            }
            _typePools.Clear();
            Children.Clear();
        }

        /// <summary>
        /// 清空指定类型的池
        /// </summary>
        public void ClearTypePool(Type type)
        {
            if (_typePools.TryGetValue(type, out var poolNode))
            {
                poolNode.Clear();
            }
        }

        /// <summary>
        /// 清空指定类型的池（泛型版本）
        /// </summary>
        public void ClearTypePool<T>() where T : Node
        {
            ClearTypePool(typeof(T));
        }

        #endregion

        #region 显示

        internal override string GetNodeDisplayName(bool includeDetails)
        {
            if (!includeDetails)
                return "PoolContainer";

            return $"PoolContainer [Types:{TypeCount}, Pooled:{TotalPooledCount}]";
        }

        #endregion
    }
}
