using UnityEngine;
using System.Collections.Generic;

namespace FluxFramework.Example
{
    /// <summary>
    /// 子弹节点 - 使用系统架构
    /// </summary>
    public class BulletNode : ViewNode, ICollidable, IBullet
    {
        public float Speed = 15f;
        public float MaxLifeTime = 3f;
        public int Damage { get; set; } = 20;
        public float CollisionRadius => 0.25f;
        
        private Vector3 _direction;
        private float _spawnTime;

        public override void OnSpawn()
        {
            base.OnSpawn();
            _spawnTime = Time.time;
            
            // 附加系统
            AttachSystem<CollisionSystem>();
            
            // 监听Tick事件
            On<TickEventArgs>(OnTick);
        }

        public void Initialize(Vector3 position, Vector3 direction)
        {
            _direction = direction.normalized;

            // 创建子弹外观
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Bullet";
            go.GetComponent<Renderer>().material.color = Color.yellow;
            go.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
            
            // 移除默认碰撞体
            var collider = go.GetComponent<Collider>();
            if (collider != null) Object.Destroy(collider);
            
            Bind(go);
            Position = position;
        }

        private void OnTick(TickEventArgs e)
        {
            if (GameObject == null) return;

            // 移动
            Position += _direction * Speed * e.DeltaTime;

            // 超时销毁
            if (Time.time - _spawnTime > MaxLifeTime)
            {
                DestroySelf();
            }
        }

        public List<Node> GetCollisionTargets()
        {
            // 获取所有敌人作为碰撞目标
            var bulletManager = Parent as BulletManagerNode;
            var enemyManager = bulletManager?.GetEnemyManager();
            var enemies = enemyManager?.GetAllEnemies();
            
            if (enemies == null) return null;
            
            return new List<Node>(enemies);
        }

        public void DestroySelf()
        {
            var bulletManager = Parent as BulletManagerNode;
            bulletManager?.DespawnBullet(this);
        }
    }
}
