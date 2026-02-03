using System.Collections.Generic;
using UnityEngine;

namespace FluxFramework.Example
{
    /// <summary>
    /// 敌人管理器节点
    /// 作为节点树的一部分，管理所有敌人
    /// </summary>
    public class EnemyManagerNode : ViewNode
    {
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

        public override void OnSpawn()
        {
            base.OnSpawn();
            CreateEmpty("EnemyManager");
        }
    }
}
