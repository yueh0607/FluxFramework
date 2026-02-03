using UnityEngine;

namespace FluxFramework.Example
{
    /// <summary>
    /// 射击系统 - 处理射击逻辑
    /// </summary>
    public class ShootSystem : NodeSystem
    {
        public override void OnAttach(Node node)
        {
            node.On<ShootRequestEvent>(OnShootRequest);
        }

        private void OnShootRequest(ShootRequestEvent e)
        {
            // 广播生成子弹事件
            e.Shooter.OwnerThread.Broadcast(new SpawnBulletEvent
            {
                Position = e.Position,
                Direction = e.Direction
            });
        }
    }
}
