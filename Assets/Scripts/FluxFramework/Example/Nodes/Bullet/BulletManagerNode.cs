using UnityEngine;

namespace FluxFramework.Example
{
    /// <summary>
    /// 子弹管理器节点
    /// 作为节点树的一部分，管理所有子弹的生成和销毁
    /// </summary>
    public class BulletManagerNode : ViewNode
    {
        public override void OnSpawn()
        {
            base.OnSpawn();
            CreateEmpty("BulletManager");
            
            // 监听生成子弹事件
            On<SpawnBulletEvent>(OnSpawnBullet);
        }

        private void OnSpawnBullet(SpawnBulletEvent e)
        {
            SpawnBullet(e.Position, e.Direction);
        }

        /// <summary>
        /// 生成子弹
        /// </summary>
        public void SpawnBullet(Vector3 position, Vector3 direction)
        {
            var bullet = AddChild<BulletNode>();
            bullet.Initialize(position, direction);
        }

        /// <summary>
        /// 销毁子弹
        /// </summary>
        public void DespawnBullet(BulletNode bullet)
        {
            if (bullet == null) return;
            RemoveAndDespawnChild(bullet);
        }

        /// <summary>
        /// 获取敌人管理器（通过树结构查找兄弟节点）
        /// </summary>
        public EnemyManagerNode GetEnemyManager()
        {
            return Parent?.FindChild<EnemyManagerNode>();
        }
    }
}
