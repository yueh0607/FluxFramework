namespace FluxFramework.Example
{
    /// <summary>
    /// 逻辑系统根节点
    /// 管理所有逻辑线程系统
    /// </summary>
    public class LogicSystemRoot : SystemContainerNode
    {
        public override void OnSpawn()
        {
            base.OnSpawn();
            
            // 注册逻辑线程系统（不能调用 Unity 主线程 API）
            RegisterSystem<MoveSystem>();
            RegisterSystem<ShootSystem>();
            RegisterSystem<CollisionSystem>();
            RegisterSystem<HealthSystem>();
            
            UnityEngine.Debug.Log("[LogicSystemRoot] Logic systems registered");
        }
    }
}
