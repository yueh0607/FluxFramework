using UnityEngine;

namespace FluxFramework.Example
{
    /// <summary>
    /// 玩家逻辑节点（纯逻辑，无视图）
    /// 通过 LogicRoot 与视图层通信
    /// </summary>
    public class PlayerNode : Node, IInputHandler, IMovable
    {
        // 逻辑数据
        public Vector3 Position { get; set; }
        public float MoveSpeed { get; set; } = 3f;
        public float ShootCooldown = 0.3f;
        
        private float _lastShootTime;

        public override void OnSpawn()
        {
            base.OnSpawn();
            
            // 附加移动和射击系统（不再附加输入系统）
            AttachSystem<MoveSystem>();
            AttachSystem<ShootSystem>();
            
            // 监听跨线程输入事件
            On<InputEvent>(OnInput);
            
            // 监听 Tick，发送同步事件
            On<TickEventArgs>(OnTick);
        }

        /// <summary>
        /// 收到跨线程输入事件
        /// </summary>
        private void OnInput(InputEvent e)
        {
            // 处理移动输入
            if (e.MoveInput != Vector2.zero)
            {
                var move = new Vector3(e.MoveInput.x, 0, e.MoveInput.y);
                OwnerThread.Broadcast(new MoveRequestEvent
                {
                    Target = this,
                    Direction = move.normalized,
                    Speed = MoveSpeed
                });
            }

            // 处理射击输入
            if (e.ShootPressed)
            {
                TryShoot(this);
            }
        }

        public void Initialize(Vector3 position)
        {
            Position = position;
            
            // 通过 LogicRoot 发送创建事件到视图线程
            var logicRoot = LogicRoot.GetLogicRoot(this);
            logicRoot?.EmitToView(new NodeSpawnedEvent
            {
                NodeId = (int)Id,
                NodeType = "Player",
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
                NodeType = "Player"
            });
            
            base.OnDespawn();
        }

        private void OnTick(TickEventArgs e)
        {
            if (OwnerThread == null) return;
            
            // 通过 LogicRoot 发送同步事件到视图线程
            var logicRoot = LogicRoot.GetLogicRoot(this);
            logicRoot?.EmitToView(new TransformSyncEvent
            {
                Position = Position,
                Rotation = Quaternion.identity,
                Scale = Vector3.one
            }, targetId: (int)Id);
        }

        public void TryShoot(Node node)
        {
            if (Time.time - _lastShootTime < ShootCooldown) return;
            _lastShootTime = Time.time;

            // 发出射击请求事件（逻辑线程内广播）
            OwnerThread.Broadcast(new ShootRequestEvent
            {
                Shooter = this,
                Position = Position + Vector3.right * 0.5f,
                Direction = Vector3.right
            });
        }
    }

    /// <summary>
    /// 玩家视图节点（纯视图，只负责渲染）
    /// 通过定向事件接收逻辑数据
    /// </summary>
    public class PlayerViewNode : ViewNode
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
            
            // 订阅定向同步事件（只接收该ID的事件）
            On<TransformSyncEvent>(_logicNodeId, OnTransformSync);
            
            // 创建外观
            InitializeView(position);
        }

        private void InitializeView(Vector3 position)
        {
            // 创建玩家外观
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = $"Player_{_logicNodeId}";
            go.GetComponent<Renderer>().material.color = Color.blue;
            go.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            go.transform.position = position;
            
            // 移除默认碰撞体
            var collider = go.GetComponent<Collider>();
            if (collider != null) Object.Destroy(collider);
            
            Bind(go);
        }

        /// <summary>
        /// 收到同步事件，更新目标位置
        /// </summary>
        private void OnTransformSync(TransformSyncEvent e)
        {
            _targetPosition = e.Position;
            
            // 直接更新位置（或可以插值）
            if (GameObject != null)
            {
                GameObject.transform.position = _targetPosition;
            }
        }
    }
}
