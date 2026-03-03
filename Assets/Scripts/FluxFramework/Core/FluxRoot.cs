namespace FluxFramework
{
    /// <summary>
    /// 框架根节点
    /// 整个 FluxFramework 的入口，管理框架级别的特殊节点
    /// 设计宗旨：一切皆节点
    /// </summary>
    /// <remarks>
    /// 树结构：
    /// FluxRoot (框架根节点，ID=0)
    /// ├── PoolContainerNode (对象池容器)
    /// │   ├── TypePoolNode&lt;T1&gt; [池内节点...]
    /// │   └── TypePoolNode&lt;T2&gt; [池内节点...]
    /// └── UserRoot (用户根节点)
    ///     └── (用户业务节点树...)
    /// 
    /// 注意：系统容器 (SystemContainerNode) 不再由 FluxRoot 管理，
    /// 而是由各个 ThreadNode 的业务层创建（如 LogicSystemRoot、ViewSystemRoot）
    /// </remarks>
    public class FluxRoot : Node
    {
        #region 单例

        /// <summary>
        /// 全局唯一实例
        /// </summary>
        public static FluxRoot Instance { get; private set; }

        #endregion

        #region 子节点

        /// <summary>
        /// 对象池容器节点
        /// </summary>
        public PoolContainerNode PoolContainer { get; private set; }

        /// <summary>
        /// 用户根节点（业务节点从这里开始）
        /// </summary>
        public Node UserRoot { get; private set; }

        #endregion

        #region 初始化

        /// <summary>
        /// 初始化框架
        /// 创建 FluxRoot 及其子节点
        /// </summary>
        public static void Initialize()
        {
            if (Instance != null)
            {
                UnityEngine.Debug.LogWarning("FluxRoot is already initialized");
                return;
            }

            // 1. 创建框架根节点（直接 new，不走池）
            Instance = new FluxRoot();
            Instance.Id = 0; // 特殊 ID
            Instance.Depth = 0;

            // 2. 创建池容器节点（直接 new，不走池）
            Instance.PoolContainer = new PoolContainerNode();
            Instance.AddSpecialChild(Instance.PoolContainer);

            // 3. 设置 NodePool 的引用
            NodePool.SetPoolContainer(Instance.PoolContainer);

            // 4. 创建用户根节点（通过池创建，这样可以被正常管理）
            Instance.UserRoot = NodePool.Spawn<Node>();
            Instance.AddChildInternal(Instance.UserRoot);
        }

        /// <summary>
        /// 添加特殊子节点（不通过池，直接设置关系）
        /// </summary>
        private void AddSpecialChild(Node child)
        {
            child.Parent = this;
            child.Depth = this.Depth + 1;
            Children.Add(child);
        }

        /// <summary>
        /// 内部添加子节点（绕过 OnSpawn）
        /// </summary>
        private void AddChildInternal(Node child)
        {
            child.Parent = this;
            child.Depth = this.Depth + 1;
            Children.Add(child);
        }

        #endregion

        #region 关闭

        /// <summary>
        /// 关闭框架
        /// </summary>
        public static void Shutdown()
        {
            if (Instance == null) return;

            // 1. 销毁用户节点树
            if (Instance.UserRoot != null)
            {
                DestroySubtree(Instance.UserRoot);
                NodePool.Despawn(Instance.UserRoot);
                Instance.UserRoot = null;
            }

            // 2. 清空池容器
            Instance.PoolContainer?.ClearAll();
            Instance.PoolContainer = null;

            // 3. 清理引用
            NodePool.SetPoolContainer(null);
            Instance = null;
        }

        /// <summary>
        /// 递归销毁子树
        /// </summary>
        private static void DestroySubtree(Node node)
        {
            for (int i = node.Children.Count - 1; i >= 0; i--)
            {
                var child = node.Children[i];
                DestroySubtree(child);
                NodePool.Despawn(child);
            }
            node.Children.Clear();
        }

        #endregion

        #region 显示

        internal override string GetNodeDisplayName(bool includeDetails)
        {
            if (!includeDetails)
                return "FluxRoot";

            return $"FluxRoot [ID:{Id}]";
        }

        #endregion
    }
}
