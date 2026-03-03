namespace FluxFramework.Example
{
    /// <summary>
    /// 逻辑分支根节点
    /// 作为 LogicThread 的业务层入口，提供跨线程事件便捷方法
    /// </summary>
    public class LogicRoot : Node
    {
        /// <summary>
        /// 视图线程引用（用于跨线程发送事件）
        /// </summary>
        public ThreadNode ViewThread { get; private set; }

        /// <summary>
        /// 初始化，设置视图线程引用
        /// </summary>
        public void Initialize(ThreadNode viewThread)
        {
            ViewThread = viewThread;
        }

        /// <summary>
        /// 发送广播事件到视图线程
        /// 便捷方法，子节点可以直接调用
        /// </summary>
        public void EmitToView<T>(T args)
        {
            if (ViewThread != null)
            {
                OwnerThread?.EmitTo(ViewThread, args);
            }
        }

        /// <summary>
        /// 发送定向事件到视图线程
        /// </summary>
        public void EmitToView<T>(T args, int targetId)
        {
            if (ViewThread != null)
            {
                OwnerThread?.EmitTo(ViewThread, args, targetId);
            }
        }

        /// <summary>
        /// 获取 LogicRoot（方便子节点获取）
        /// </summary>
        public static LogicRoot GetLogicRoot(Node node)
        {
            var current = node;
            while (current != null)
            {
                if (current is LogicRoot root)
                    return root;
                current = current.Parent;
            }
            return null;
        }

        public override void OnSpawn()
        {
            base.OnSpawn();
        }
    }
}

