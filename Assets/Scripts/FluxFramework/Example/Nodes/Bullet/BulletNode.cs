using UnityEngine;
using System.Collections.Generic;

namespace FluxFramework.Example
{
    /// <summary>
    /// 子弹逻辑节点（纯逻辑）
    /// 通过事件与视图层通信
    /// </summary>
    public class BulletNode : Node, ICollidable, IBullet
    {
        // 逻辑数据
        public Vector3 Position { get; set; }
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
            Position = position;
            _direction = direction.normalized;
            
            // 通过 LogicRoot 发送创建事件到视图线程
            var logicRoot = LogicRoot.GetLogicRoot(this);
            logicRoot?.EmitToView(new NodeSpawnedEvent
            {
                NodeId = (int)Id,
                NodeType = "Bullet",
                Position = position
            });
        }

        public override void OnDespawn()
        {
            // 通过 LogicRoot 发送销毁事件到视图线程
            var logicRoot = LogicRoot.GetLogicRoot(this);
            logicRoot?.EmitToView(new NodeDespawnedEvent
            {
                NodeId = (int)Id,
                NodeType = "Bullet"
            });
            
            base.OnDespawn();
        }

        private void OnTick(TickEventArgs e)
        {
            // 移动
            Position += _direction * Speed * e.DeltaTime;
            
            // 通过 LogicRoot 发送同步事件到视图线程
            if (OwnerThread != null)
            {
                var logicRoot = LogicRoot.GetLogicRoot(this);
                logicRoot?.EmitToView(new TransformSyncEvent
                {
                    Position = Position,
                    Rotation = Quaternion.identity,
                    Scale = Vector3.one
                }, targetId: (int)Id);
            }

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

    /// <summary>
    /// 子弹视图节点（纯视图）
    /// 通过定向事件接收逻辑数据
    /// </summary>
    public class BulletViewNode : ViewNode
    {
        private int _logicNodeId;
        private Vector3 _targetPosition;

        public override void OnSpawn()
        {
            base.OnSpawn();
        }

        /// <summary>
        /// 初始化视图，绑定逻辑节点ID
        /// </summary>
        public void Initialize(int logicNodeId, Vector3 position)
        {
            _logicNodeId = logicNodeId;
            _targetPosition = position;
            
            // 订阅定向同步事件
            On<TransformSyncEvent>(_logicNodeId, OnTransformSync);
            
            // 创建外观
            InitializeView(position);
        }

        private void InitializeView(Vector3 position)
        {
            // 创建子弹外观
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = $"Bullet_{_logicNodeId}";
            go.GetComponent<Renderer>().material.color = Color.yellow;
            go.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
            go.transform.position = position;
            
            // 移除默认碰撞体
            var collider = go.GetComponent<Collider>();
            if (collider != null) Object.Destroy(collider);
            
            Bind(go);
        }

        private void OnTransformSync(TransformSyncEvent e)
        {
            _targetPosition = e.Position;
            
            if (GameObject != null)
            {
                GameObject.transform.position = _targetPosition;
            }
        }
    }
}
