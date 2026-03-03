namespace FluxFramework.Example
{
    /// <summary>
    /// 视图系统根节点
    /// 管理所有视图线程系统
    /// </summary>
    public class ViewSystemRoot : SystemContainerNode
    {
        private ThreadNode _logicThread;

        /// <summary>
        /// 设置逻辑线程引用（用于 InputSystem 跨线程发送事件）
        /// </summary>
        public void SetLogicThread(ThreadNode logicThread)
        {
            _logicThread = logicThread;
        }

        public override void OnSpawn()
        {
            base.OnSpawn();
            
            // 注册视图线程系统（可以调用 Unity 主线程 API）
            var inputSystem = RegisterSystem<InputSystem>();
            
            UnityEngine.Debug.Log("[ViewSystemRoot] View systems registered");
        }

        /// <summary>
        /// 配置系统的逻辑线程引用
        /// </summary>
        public void ConfigureSystems(ThreadNode logicThread)
        {
            var inputSystem = GetSystem<InputSystem>();
            if (inputSystem != null)
            {
                inputSystem.SetLogicThread(logicThread);
            }
        }
    }
}
