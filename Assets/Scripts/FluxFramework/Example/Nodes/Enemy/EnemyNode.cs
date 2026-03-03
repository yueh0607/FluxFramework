using UnityEngine;
using System.Collections.Generic;

namespace FluxFramework.Example
{
    /// <summary>
    /// 敌人逻辑节点（纯逻辑，无视图）
    /// 通过事件与视图层通信
    /// </summary>
    public class EnemyNode : Node, IAutoMovable, IDamageable, ICollidable
    {
        // 逻辑数据
        public Vector3 Position { get; set; }
        public int CurrentHp { get; set; }
        public int MaxHp { get; private set; } = 100;
        public Color OriginalColor => Color.red;
        
        public float MoveSpeed { get; set; } = 2f;
        public float MoveRange { get; set; } = 3f;
        public float MoveDirection { get; set; } = 1f;
        public Vector3 StartPosition { get; private set; }
        public float CollisionRadius => 0.3f;

        public override void OnSpawn()
        {
            base.OnSpawn();
            CurrentHp = MaxHp;
            
            // 附加系统
            AttachSystem<MoveSystem>();
            AttachSystem<HealthSystem>();
            
            // 监听死亡事件
            On<DeathEvent>(OnDeath);
            
            // 监听 Tick，发送同步事件
            On<TickEventArgs>(OnTick);
        }

        public void Initialize(Vector3 startPosition)
        {
            Position = startPosition;
            StartPosition = startPosition;
            MoveSpeed = Random.Range(1, 3f);
            
            // 通过 LogicRoot 发送创建事件到视图线程
            var logicRoot = LogicRoot.GetLogicRoot(this);
            logicRoot?.EmitToView(new NodeSpawnedEvent
            {
                NodeId = (int)Id,
                NodeType = "Enemy",
                Position = startPosition
            });
        }

        public override void OnDespawn()
        {
            // 通过 LogicRoot 发送销毁事件到视图线程
            var logicRoot = LogicRoot.GetLogicRoot(this);
            logicRoot?.EmitToView(new NodeDespawnedEvent
            {
                NodeId = (int)Id,
                NodeType = "Enemy"
            });
            
            base.OnDespawn();
        }

        private void OnTick(TickEventArgs e)
        {
            if (OwnerThread == null) return;
            
            // 通过 LogicRoot 发送同步事件到视图线程
            var logicRoot = LogicRoot.GetLogicRoot(this);
            if (logicRoot != null)
            {
                logicRoot.EmitToView(new TransformSyncEvent
                {
                    Position = Position,
                    Rotation = Quaternion.identity,
                    Scale = Vector3.one
                }, targetId: (int)Id);
                
                logicRoot.EmitToView(new HealthSyncEvent
                {
                    CurrentHp = CurrentHp,
                    MaxHp = MaxHp
                }, targetId: (int)Id);
            }
        }

        private void OnDeath(DeathEvent e)
        {
            if (e.Target != this) return;
            
            Debug.Log("Enemy died! Respawning...");
            // 重置
            CurrentHp = MaxHp;
            Position = StartPosition;
        }

        public List<Node> GetCollisionTargets()
        {
            // 敌人不需要主动检测碰撞，由子弹检测
            return null;
        }
    }

    /// <summary>
    /// 敌人视图节点（纯视图，只负责渲染）
    /// 通过定向事件接收逻辑数据
    /// </summary>
    public class EnemyViewNode : ViewNode
    {
        private int _logicNodeId;
        private Renderer _renderer;
        private Vector3 _targetPosition;
        private Color _originalColor = Color.red;

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
            On<HealthSyncEvent>(_logicNodeId, OnHealthSync);
            
            // 监听伤害事件（广播），用于播放特效
            On<DamageEvent>(OnDamageEffect);
            
            // 创建外观
            InitializeView(position);
        }

        private void InitializeView(Vector3 position)
        {
            // 创建敌人外观
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = $"Enemy_{_logicNodeId}";
            _renderer = go.GetComponent<Renderer>();
            _renderer.material.color = _originalColor;
            go.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
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

        private void OnHealthSync(HealthSyncEvent e)
        {
            // 可用于更新血条等UI
        }

        private void OnDamageEffect(DamageEvent e)
        {
            // 通过ID判断是否是自己的逻辑节点
            if (e.Target is Node targetNode && (int)targetNode.Id != _logicNodeId) return;
            
            // 播放受击闪烁特效
            if (_renderer != null && GameObject != null)
            {
                _renderer.material.color = Color.white;
                
                var helper = GameObject.GetComponent<DelayHelper>();
                if (helper == null)
                    helper = GameObject.AddComponent<DelayHelper>();
                helper.StartCoroutine(DelayCoroutine(0.1f, () => {
                    if (_renderer != null)
                        _renderer.material.color = _originalColor;
                }));
            }
        }

        private System.Collections.IEnumerator DelayCoroutine(float delay, System.Action action)
        {
            yield return new WaitForSeconds(delay);
            action?.Invoke();
        }
    }
}
