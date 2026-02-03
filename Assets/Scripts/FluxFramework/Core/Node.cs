using System;
using System.Collections.Generic;

namespace FluxFramework
{
    /// <summary>
    /// 通用节点基类
    /// 可继承扩展任意功能，使用对象池管理避免 GC
    /// </summary>
    public class Node : IPoolable
    {
        #region 唯一标识

        /// <summary>
        /// 全局唯一 ID，由 NodePool 分配
        /// </summary>
        public uint Id { get; internal set; }

        #endregion

        #region 树结构

        /// <summary>
        /// 父节点
        /// </summary>
        public Node Parent { get; internal set; }

        /// <summary>
        /// 子节点列表
        /// </summary>
        public List<Node> Children { get; private set; }

        /// <summary>
        /// 树深度，AddChild 时自动计算
        /// 用于事件派发时的排序（父先于子）
        /// </summary>
        public int Depth { get; internal set; }

        #endregion

        #region 线程归属

        /// <summary>
        /// 所属的线程节点
        /// AddChild 时自动设置，缓存避免每次向上查找
        /// </summary>
        public ThreadNode OwnerThread { get; internal set; }

        #endregion

        #region 事件句柄

        /// <summary>
        /// 持有所有订阅的句柄，OnDespawn 时自动清理
        /// </summary>
        private List<EventHandle> _eventHandles;

        #endregion

        #region 系统管理

        /// <summary>
        /// 附加的系统列表
        /// </summary>
        private List<NodeSystem> _attachedSystems;

        #endregion

        #region 构造函数

        public Node()
        {
            Children = new List<Node>();
            _eventHandles = new List<EventHandle>();
        }

        #endregion

        #region 生命周期

        /// <summary>
        /// 从池中取出时调用
        /// </summary>
        public virtual void OnSpawn()
        {
        }

        /// <summary>
        /// 放回池中时调用
        /// 自动取消所有事件订阅
        /// </summary>
        public virtual void OnDespawn()
        {
            // 清理系统
            if (_attachedSystems != null)
            {
                foreach (var system in _attachedSystems)
                {
                    system.OnDetach(this);
                }
                _attachedSystems.Clear();
            }

            // 自动取消所有事件订阅
            for (int i = 0; i < _eventHandles.Count; i++)
            {
                _eventHandles[i].Dispose();
            }
            _eventHandles.Clear();

            // 清理树结构
            Parent = null;
            OwnerThread = null;
            Depth = 0;
        }

        #endregion

        #region 子节点管理

        /// <summary>
        /// 添加子节点
        /// </summary>
        public T AddChild<T>() where T : Node, new()
        {
            var child = NodePool.SpawnWithoutInit<T>();
            AddChildInternal(child);
            // OnSpawn 在 OwnerThread 设置后调用，这样用户可以在 OnSpawn 中订阅事件
            child.OnSpawn();
            return child;
        }

        /// <summary>
        /// 添加已有节点作为子节点
        /// </summary>
        public void AddChild(Node child)
        {
            if (child == null) return;
            AddChildInternal(child);
        }

        private void AddChildInternal(Node child)
        {
            // 如果已有父节点，先从原父节点移除
            child.Parent?.RemoveChild(child);

            child.Parent = this;
            child.Depth = this.Depth + 1;

            // 继承线程归属
            // 如果当前节点是 ThreadNode，子节点归属于当前节点
            // 否则继承父节点的 OwnerThread
            if (this is ThreadNode threadNode)
            {
                child.OwnerThread = threadNode;
            }
            else
            {
                child.OwnerThread = this.OwnerThread;
            }

            Children.Add(child);

            // 递归更新子树的 OwnerThread 和 Depth
            UpdateSubtreeOwnership(child);

            OnChildAdded(child);
            child.OnAddedToParent();
        }

        /// <summary>
        /// 递归更新子树的线程归属和深度
        /// </summary>
        private void UpdateSubtreeOwnership(Node node)
        {
            foreach (var child in node.Children)
            {
                child.Depth = node.Depth + 1;

                // 如果子节点不是 ThreadNode，继承父节点的 OwnerThread
                if (!(child is ThreadNode))
                {
                    child.OwnerThread = node.OwnerThread;
                }

                UpdateSubtreeOwnership(child);
            }
        }

        /// <summary>
        /// 移除子节点
        /// </summary>
        public void RemoveChild(Node child)
        {
            if (child == null || child.Parent != this) return;

            Children.Remove(child);
            child.Parent = null;
            child.OnRemovedFromParent();
            OnChildRemoved(child);
        }

        /// <summary>
        /// 移除并销毁子节点
        /// </summary>
        public void RemoveAndDespawnChild(Node child)
        {
            if (child == null || child.Parent != this) return;

            // 先递归销毁所有子孙节点
            DespawnSubtree(child);

            Children.Remove(child);
            NodePool.Despawn(child);
        }

        /// <summary>
        /// 递归销毁子树
        /// </summary>
        private void DespawnSubtree(Node node)
        {
            // 先销毁所有子节点
            for (int i = node.Children.Count - 1; i >= 0; i--)
            {
                var child = node.Children[i];
                DespawnSubtree(child);
                NodePool.Despawn(child);
            }
            node.Children.Clear();
        }

        /// <summary>
        /// 子节点添加时回调
        /// </summary>
        protected virtual void OnChildAdded(Node child) { }

        /// <summary>
        /// 子节点移除时回调
        /// </summary>
        protected virtual void OnChildRemoved(Node child) { }

        /// <summary>
        /// 被添加到父节点时回调
        /// </summary>
        protected virtual void OnAddedToParent() { }

        /// <summary>
        /// 从父节点移除时回调
        /// </summary>
        protected virtual void OnRemovedFromParent() { }

        #endregion

        #region 事件订阅

        /// <summary>
        /// 订阅事件（泛型，零 GC）
        /// 返回句柄，用于 O(1) 取消
        /// 句柄会自动存入 _eventHandles，OnDespawn 时自动清理
        /// </summary>
        public EventHandle On<T>(Action<T> handler)
        {
            if (OwnerThread == null)
            {
                throw new InvalidOperationException($"Node {Id} has no OwnerThread, cannot subscribe events");
            }

            var handle = OwnerThread.Register(this, handler);
            _eventHandles.Add(handle);
            return handle;
        }

        /// <summary>
        /// 手动取消订阅（通常不需要，OnDespawn 会自动清理）
        /// </summary>
        public void Off(EventHandle handle)
        {
            handle.Dispose();
            _eventHandles.Remove(handle);
        }

        #endregion

        #region 消息处理

        /// <summary>
        /// 处理跨线程消息（子类重写）
        /// </summary>
        protected internal virtual void OnMessage(Message msg) { }

        #endregion

        #region 树结构可视化

        /// <summary>
        /// 将节点及其子树转为格式化字符串
        /// </summary>
        public string ToTreeString(bool includeDetails = true)
        {
            var sb = new System.Text.StringBuilder();
            BuildTreeString(sb, "", true, includeDetails);
            return sb.ToString();
        }

        /// <summary>
        /// 递归构建树字符串
        /// </summary>
        private void BuildTreeString(System.Text.StringBuilder sb, string indent, bool isLast, bool includeDetails)
        {
            // 绘制当前节点
            sb.Append(indent);
            sb.Append(isLast ? "└── " : "├── ");
            sb.Append(GetNodeDisplayName(includeDetails));
            sb.AppendLine();

            // 计算子节点的缩进
            var childIndent = indent + (isLast ? "    " : "│   ");

            // 递归绘制子节点
            for (int i = 0; i < Children.Count; i++)
            {
                Children[i].BuildTreeString(sb, childIndent, i == Children.Count - 1, includeDetails);
            }
        }

        /// <summary>
        /// 获取节点显示名称（子类可重写）
        /// internal 以便 PoolContainerNode 等同命名空间类访问
        /// </summary>
        internal virtual string GetNodeDisplayName(bool includeDetails)
        {
            var typeName = GetType().Name;
            
            if (!includeDetails)
                return typeName;

            // 详细模式：显示类型、ID
            var info = $"{typeName} [ID:{Id}]";
            
            // 如果是 ThreadNode，显示线程名
            if (this is ThreadNode tn)
            {
                info += $" ({tn.ThreadName ?? "unnamed"})";
            }
            
            return info;
        }

        /// <summary>
        /// 获取整棵树的格式化字符串（从根节点开始）
        /// </summary>
        /// <param name="includeDetails">是否包含详细信息</param>
        /// <param name="includePool">是否包含对象池信息</param>
        public static string GetFullTreeString(bool includeDetails = true, bool includePool = false)
        {
            if (!Tree.IsInitialized)
                return "[Tree not initialized]";

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("=== FluxFramework Tree Structure ===");

            // 从 FluxRoot 开始显示完整树
            if (Tree.FluxRoot != null)
            {
                BuildFullTree(sb, Tree.FluxRoot, includeDetails, includePool);
            }
            else
            {
                // 兼容旧版本：显示 UserRoot
                sb.Append("UserRoot");
                if (includeDetails && Tree.Root != null)
                    sb.Append($" [ID:{Tree.Root.Id}]");
                sb.AppendLine();

                if (Tree.Root != null)
                {
                    for (int i = 0; i < Tree.Root.Children.Count; i++)
                    {
                        Tree.Root.Children[i].BuildTreeString(sb, "", i == Tree.Root.Children.Count - 1, includeDetails);
                    }
                }
            }

            sb.AppendLine($"=== Active: {NodePool.ActiveCount}, Pooled: {NodePool.TotalPooledCount} ===");
            return sb.ToString();
        }

        /// <summary>
        /// 构建完整的框架树（包含 FluxRoot、PoolContainer、UserRoot）
        /// </summary>
        private static void BuildFullTree(System.Text.StringBuilder sb, FluxRoot fluxRoot, bool includeDetails, bool includePool)
        {
            // 显示 FluxRoot
            sb.AppendLine(fluxRoot.GetNodeDisplayName(includeDetails));

            var childCount = fluxRoot.Children.Count;
            var showPoolIndex = includePool ? childCount : -1;

            for (int i = 0; i < childCount; i++)
            {
                var child = fluxRoot.Children[i];
                var isPoolContainer = child is PoolContainerNode;
                
                // 如果不显示池且当前是池容器，跳过
                if (!includePool && isPoolContainer)
                    continue;

                var isLast = (i == childCount - 1);
                var prefix = isLast ? "└── " : "├── ";
                var childIndent = isLast ? "    " : "│   ";

                // 显示子节点名称
                sb.Append(prefix);
                sb.AppendLine(child.GetNodeDisplayName(includeDetails));

                // 递归显示子节点
                if (isPoolContainer && includePool)
                {
                    BuildPoolTree(sb, (PoolContainerNode)child, childIndent, includeDetails);
                }
                else
                {
                    // 显示 UserRoot 的子树
                    for (int j = 0; j < child.Children.Count; j++)
                    {
                        child.Children[j].BuildTreeString(sb, childIndent, j == child.Children.Count - 1, includeDetails);
                    }
                }
            }
        }

        /// <summary>
        /// 构建对象池树
        /// </summary>
        private static void BuildPoolTree(System.Text.StringBuilder sb, PoolContainerNode poolContainer, string indent, bool includeDetails)
        {
            var pools = poolContainer.GetAllTypePools();
            var poolList = new List<TypePoolNode>(pools.Values);

            for (int i = 0; i < poolList.Count; i++)
            {
                var typePool = poolList[i];
                var isLast = i == poolList.Count - 1;
                var prefix = isLast ? "└── " : "├── ";
                var childPrefix = isLast ? "    " : "│   ";

                sb.Append(indent);
                sb.Append(prefix);
                sb.AppendLine(typePool.GetNodeDisplayName(includeDetails));

                // 显示池中的所有节点（全部展开）
                if (includeDetails && typePool.PooledCount > 0)
                {
                    for (int j = 0; j < typePool.Children.Count; j++)
                    {
                        var nodeIsLast = j == typePool.Children.Count - 1;
                        sb.Append(indent);
                        sb.Append(childPrefix);
                        sb.Append(nodeIsLast ? "└── " : "├── ");
                        sb.AppendLine($"[pooled] {typePool.Children[j].GetType().Name}");
                    }
                }
            }
        }

        #endregion

        #region 查找

        /// <summary>
        /// 向上查找指定类型的祖先节点
        /// </summary>
        public T FindAncestor<T>() where T : Node
        {
            var current = Parent;
            while (current != null)
            {
                if (current is T result)
                    return result;
                current = current.Parent;
            }
            return null;
        }

        /// <summary>
        /// 在子节点中查找指定类型的节点
        /// </summary>
        public T FindChild<T>() where T : Node
        {
            foreach (var child in Children)
            {
                if (child is T result)
                    return result;
            }
            return null;
        }

        /// <summary>
        /// 递归查找指定类型的后代节点
        /// </summary>
        public T FindDescendant<T>() where T : Node
        {
            foreach (var child in Children)
            {
                if (child is T result)
                    return result;

                var found = child.FindDescendant<T>();
                if (found != null)
                    return found;
            }
            return null;
        }

        #endregion

        #region 系统管理

        /// <summary>
        /// 附加系统
        /// </summary>
        public void AttachSystem<T>() where T : NodeSystem
        {
            if (_attachedSystems == null)
                _attachedSystems = new List<NodeSystem>();

            var container = Tree.FluxRoot?.SystemContainer;
            if (container == null)
            {
                UnityEngine.Debug.LogError("SystemContainer not initialized");
                return;
            }

            var system = container.GetSystem<T>();
            if (system == null)
            {
                UnityEngine.Debug.LogError($"System '{typeof(T).Name}' not found. Did you register it?");
                return;
            }

            if (_attachedSystems.Contains(system))
            {
                UnityEngine.Debug.LogWarning($"System '{typeof(T).Name}' already attached to Node {Id}");
                return;
            }

            _attachedSystems.Add(system);
            system.OnAttach(this);
        }

        /// <summary>
        /// 移除系统
        /// </summary>
        public void DetachSystem<T>() where T : NodeSystem
        {
            if (_attachedSystems == null) return;

            var system = _attachedSystems.Find(s => s.GetType() == typeof(T));
            if (system != null)
            {
                system.OnDetach(this);
                _attachedSystems.Remove(system);
            }
        }

        /// <summary>
        /// 检查是否有某个系统
        /// </summary>
        public bool HasSystem<T>() where T : NodeSystem
        {
            if (_attachedSystems == null) return false;
            return _attachedSystems.Exists(s => s.GetType() == typeof(T));
        }

        /// <summary>
        /// 执行所有系统的 Tick
        /// </summary>
        protected void TickSystems(TickEventArgs args)
        {
            if (_attachedSystems == null || _attachedSystems.Count == 0)
                return;

            // 按优先级排序
            _attachedSystems.Sort((a, b) => b.Priority.CompareTo(a.Priority));

            foreach (var system in _attachedSystems)
            {
                if (system.Enabled)
                {
                    system.OnSystemTick(this, args);
                }
            }
        }

        #endregion
    }
}
