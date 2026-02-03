using System.Collections.Generic;

namespace FluxFramework
{
    /// <summary>
    /// 全局树的 API 入口
    /// 提供便捷的访问方式，实际管理由 FluxRoot 负责
    /// </summary>
    public static class Tree
    {
        #region 属性

        /// <summary>
        /// 框架根节点
        /// </summary>
        public static FluxRoot FluxRoot => FluxRoot.Instance;

        /// <summary>
        /// 用户根节点（业务节点从这里开始）
        /// </summary>
        public static Node Root => FluxRoot?.UserRoot;

        /// <summary>
        /// 池容器节点
        /// </summary>
        public static PoolContainerNode PoolContainer => FluxRoot?.PoolContainer;

        /// <summary>
        /// 系统容器节点
        /// </summary>
        public static SystemContainerNode SystemContainer => FluxRoot?.SystemContainer;

        /// <summary>
        /// 是否已初始化
        /// </summary>
        public static bool IsInitialized => FluxRoot != null;

        #endregion

        #region ThreadNode 管理

        /// <summary>
        /// 所有 ThreadNode 列表
        /// </summary>
        private static readonly List<ThreadNode> _threadNodes = new List<ThreadNode>();

        /// <summary>
        /// ThreadNode 字典（按名称索引）
        /// </summary>
        private static readonly Dictionary<string, ThreadNode> _threadNodesByName = new Dictionary<string, ThreadNode>();

        /// <summary>
        /// 注册 ThreadNode
        /// </summary>
        internal static void RegisterThreadNode(ThreadNode node)
        {
            if (!_threadNodes.Contains(node))
            {
                _threadNodes.Add(node);

                if (!string.IsNullOrEmpty(node.ThreadName))
                {
                    _threadNodesByName[node.ThreadName] = node;
                }
            }
        }

        /// <summary>
        /// 注销 ThreadNode
        /// </summary>
        internal static void UnregisterThreadNode(ThreadNode node)
        {
            _threadNodes.Remove(node);

            if (!string.IsNullOrEmpty(node.ThreadName))
            {
                _threadNodesByName.Remove(node.ThreadName);
            }
        }

        /// <summary>
        /// 通过名称查找 ThreadNode
        /// </summary>
        public static ThreadNode FindThreadNode(string name)
        {
            _threadNodesByName.TryGetValue(name, out var node);
            return node;
        }

        /// <summary>
        /// 获取所有 ThreadNode
        /// </summary>
        public static IReadOnlyList<ThreadNode> GetAllThreadNodes()
        {
            return _threadNodes;
        }

        #endregion

        #region 初始化

        /// <summary>
        /// 初始化树
        /// </summary>
        public static void Initialize()
        {
            FluxFramework.FluxRoot.Initialize();
        }

        #endregion

        #region Tick

        /// <summary>
        /// 驱动指定 ThreadNode 的 Tick
        /// </summary>
        public static void Tick(string threadName, float deltaTime)
        {
            var threadNode = FindThreadNode(threadName);
            if (threadNode != null)
            {
                threadNode.Tick(deltaTime);
            }
            else
            {
                UnityEngine.Debug.LogWarning($"ThreadNode '{threadName}' not found");
            }
        }

        /// <summary>
        /// 驱动指定 ThreadNode 的 Tick
        /// </summary>
        public static void Tick(ThreadNode threadNode, float deltaTime)
        {
            threadNode?.Tick(deltaTime);
        }

        #endregion

        #region 查找

        /// <summary>
        /// 通过 ID 查找节点
        /// </summary>
        public static Node FindById(uint id)
        {
            return NodePool.FindById(id);
        }

        /// <summary>
        /// 通过 ID 查找指定类型的节点
        /// </summary>
        public static T FindById<T>(uint id) where T : Node
        {
            return NodePool.FindById<T>(id);
        }

        #endregion

        #region 关闭

        /// <summary>
        /// 关闭树
        /// </summary>
        public static void Shutdown()
        {
            // 停止所有独立线程
            foreach (var threadNode in _threadNodes)
            {
                if (threadNode.RunsOnDedicatedThread)
                {
                    threadNode.Stop();
                }
            }

            _threadNodes.Clear();
            _threadNodesByName.Clear();

            // 关闭框架
            FluxFramework.FluxRoot.Shutdown();

            // 清空消息池
            MessagePool.Clear();
        }

        #endregion

        #region 树结构可视化

        /// <summary>
        /// 获取完整的树结构字符串（包含框架节点）
        /// </summary>
        public static string GetFullTreeString(bool includeDetails = true)
        {
            if (FluxRoot == null)
                return "[Tree not initialized]";

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("=== FluxFramework Tree Structure ===");

            // 显示框架根
            BuildTreeString(sb, FluxRoot, "", true, includeDetails);

            sb.AppendLine($"=== Active Nodes: {NodePool.ActiveCount}, Pooled: {NodePool.TotalPooledCount} ===");
            return sb.ToString();
        }

        /// <summary>
        /// 获取用户树结构字符串（不包含框架节点）
        /// </summary>
        public static string GetUserTreeString(bool includeDetails = true)
        {
            if (Root == null)
                return "[Tree not initialized]";

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("=== User Tree Structure ===");

            sb.Append("UserRoot");
            if (includeDetails)
                sb.Append($" [ID:{Root.Id}]");
            sb.AppendLine();

            for (int i = 0; i < Root.Children.Count; i++)
            {
                BuildTreeString(sb, Root.Children[i], "", i == Root.Children.Count - 1, includeDetails);
            }

            sb.AppendLine($"=== Active Nodes: {NodePool.ActiveCount} ===");
            return sb.ToString();
        }

        /// <summary>
        /// 递归构建树字符串
        /// </summary>
        private static void BuildTreeString(System.Text.StringBuilder sb, Node node, string indent, bool isLast, bool includeDetails)
        {
            sb.Append(indent);
            sb.Append(isLast ? "└── " : "├── ");
            sb.Append(node.GetType().Name);

            if (includeDetails)
            {
                sb.Append($" [ID:{node.Id}]");

                if (node is ThreadNode tn)
                {
                    sb.Append($" ({tn.ThreadName ?? "unnamed"})");
                }
                else if (node is TypePoolNode tp)
                {
                    sb.Append($" <{tp.PooledType?.Name}> [Count:{tp.PooledCount}]");
                }
                else if (node is PoolContainerNode pc)
                {
                    sb.Append($" [Types:{pc.TypeCount}, Pooled:{pc.TotalPooledCount}]");
                }
            }

            sb.AppendLine();

            var childIndent = indent + (isLast ? "    " : "│   ");

            for (int i = 0; i < node.Children.Count; i++)
            {
                BuildTreeString(sb, node.Children[i], childIndent, i == node.Children.Count - 1, includeDetails);
            }
        }

        #endregion
    }
}
