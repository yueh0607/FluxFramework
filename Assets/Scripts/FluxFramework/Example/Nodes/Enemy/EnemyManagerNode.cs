using System.Collections.Generic;
using UnityEngine;

namespace FluxFramework.Example
{
    /// <summary>
    /// 敌人管理器节点（纯逻辑）
    /// 管理所有敌人的生成和销毁
    /// 视图通过事件自动同步
    /// </summary>
    public class EnemyManagerNode : Node
    {
        public override void OnSpawn()
        {
            base.OnSpawn();
        }

        /// <summary>
        /// 生成敌人
        /// </summary>
        public EnemyNode SpawnEnemy(Vector3 startPosition)
        {
            var enemy = AddChild<EnemyNode>();
            enemy.Initialize(startPosition);
            return enemy;
        }

        /// <summary>
        /// 获取所有存活的敌人
        /// </summary>
        public List<EnemyNode> GetAllEnemies()
        {
            var enemies = new List<EnemyNode>();
            foreach (var child in Children)
            {
                if (child is EnemyNode enemy)
                {
                    enemies.Add(enemy);
                }
            }
            return enemies;
        }

        /// <summary>
        /// 销毁敌人
        /// </summary>
        public void DespawnEnemy(EnemyNode enemy)
        {
            if (enemy == null) return;
            RemoveAndDespawnChild(enemy);
        }
    }
}
