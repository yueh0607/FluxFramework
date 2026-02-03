using System;

namespace FluxFramework
{
    /// <summary>
    /// 类型池节点
    /// 存放特定类型的回收节点，体现"一切皆节点"的设计
    /// </summary>
    /// <remarks>
    /// 池内的节点作为 TypePoolNode 的子节点存在
    /// 可以通过树结构查看池内有哪些节点
    /// </remarks>
    public class TypePoolNode : Node
    {
        #region 属性

        /// <summary>
        /// 池化的节点类型
        /// </summary>
        public Type PooledType { get; }

        /// <summary>
        /// 池中节点数量
        /// </summary>
        public int PooledCount => Children.Count;

        #endregion

        #region 构造

        public TypePoolNode(Type type)
        {
            PooledType = type;
        }

        #endregion

        #region 池操作

        /// <summary>
        /// 将节点放入池中
        /// 节点成为 TypePoolNode 的子节点
        /// </summary>
        public void Push(Node node)
        {
            if (node == null) return;

            // 设置为池节点的子节点（不触发正常的生命周期回调）
            node.Parent = this;
            node.Depth = this.Depth + 1;
            node.OwnerThread = null; // 池中节点没有线程归属
            Children.Add(node);
        }

        /// <summary>
        /// 从池中取出节点
        /// </summary>
        public Node Pop()
        {
            if (Children.Count == 0) return null;

            var lastIndex = Children.Count - 1;
            var node = Children[lastIndex];
            Children.RemoveAt(lastIndex);
            node.Parent = null;
            return node;
        }

        /// <summary>
        /// 查看池中的节点（不取出）
        /// </summary>
        public Node Peek()
        {
            if (Children.Count == 0) return null;
            return Children[Children.Count - 1];
        }

        /// <summary>
        /// 清空池
        /// </summary>
        public void Clear()
        {
            foreach (var child in Children)
            {
                child.Parent = null;
            }
            Children.Clear();
        }

        #endregion

        #region 显示

        internal override string GetNodeDisplayName(bool includeDetails)
        {
            var typeName = PooledType?.Name ?? "Unknown";
            if (!includeDetails)
                return $"Pool<{typeName}>";

            return $"Pool<{typeName}> [Count:{PooledCount}]";
        }

        #endregion
    }
}
