namespace FluxFramework
{
    /// <summary>
    /// 跨线程事件扩展方法
    /// 让 Node 可以方便地发送跨线程事件
    /// </summary>
    public static class CrossThreadEventExtensions
    {
        /// <summary>
        /// 发送跨线程广播事件
        /// 使用方式：node.EmitTo(targetThread, event);
        /// </summary>
        public static void EmitTo<T>(this Node node, ThreadNode targetThread, T args)
        {
            node.OwnerThread?.EmitTo(targetThread, args);
        }

        /// <summary>
        /// 发送跨线程定向事件
        /// 使用方式：node.EmitTo(targetThread, event, targetId);
        /// </summary>
        public static void EmitTo<T>(this Node node, ThreadNode targetThread, T args, int targetId)
        {
            node.OwnerThread?.EmitTo(targetThread, args, targetId);
        }
    }
}
