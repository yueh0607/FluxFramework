using System;
using System.Collections.Generic;

namespace FluxFramework
{
    /// <summary>
    /// 系统容器节点
    /// 管理所有全局共享的系统实例
    /// 体现"一切皆节点"的设计理念
    /// </summary>
    public class SystemContainerNode : Node
    {
        /// <summary>
        /// 系统类型 -> 系统实例的映射
        /// </summary>
        private readonly Dictionary<Type, NodeSystem> _systems = new Dictionary<Type, NodeSystem>();

        #region 系统管理

        /// <summary>
        /// 注册系统
        /// </summary>
        public T RegisterSystem<T>() where T : NodeSystem, new()
        {
            var type = typeof(T);

            if (_systems.ContainsKey(type))
            {
                UnityEngine.Debug.LogWarning($"System '{type.Name}' already registered");
                return _systems[type] as T;
            }

            var system = new T();
            
            // 系统作为子节点
            system.Parent = this;
            system.Depth = this.Depth + 1;
            system.Id = (uint)(1000 + Children.Count); // 特殊 ID 段
            Children.Add(system);

            _systems[type] = system;

            UnityEngine.Debug.Log($"System '{type.Name}' registered");
            return system;
        }

        /// <summary>
        /// 获取系统（泛型）
        /// </summary>
        public T GetSystem<T>() where T : NodeSystem
        {
            var type = typeof(T);
            _systems.TryGetValue(type, out var system);
            return system as T;
        }

        /// <summary>
        /// 获取所有系统
        /// </summary>
        public IReadOnlyDictionary<Type, NodeSystem> GetAllSystems()
        {
            return _systems;
        }

        #endregion

        #region 统计

        /// <summary>
        /// 系统数量
        /// </summary>
        public int SystemCount => _systems.Count;

        #endregion

        #region 显示

        internal override string GetNodeDisplayName(bool includeDetails)
        {
            if (!includeDetails)
                return "SystemContainer";

            return $"SystemContainer [Systems:{SystemCount}]";
        }

        #endregion
    }
}
