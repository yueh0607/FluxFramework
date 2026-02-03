using UnityEngine;

namespace FluxFramework.Example
{
    /// <summary>
    /// 玩家节点 - 使用系统架构
    /// </summary>
    public class PlayerNode : ViewNode, IInputHandler, IMovable
    {
        public float MoveSpeed { get; set; } = 3f;
        public float ShootCooldown = 0.3f;
        
        private float _lastShootTime;

        public override void OnSpawn()
        {
            base.OnSpawn();
            
            // 附加系统
            AttachSystem<InputSystem>();
            AttachSystem<MoveSystem>();
            AttachSystem<ShootSystem>();
        }

        public void Initialize()
        {
            // 创建玩家外观
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "Player";
            go.GetComponent<Renderer>().material.color = Color.blue;
            go.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            
            // 移除默认碰撞体
            var collider = go.GetComponent<Collider>();
            if (collider != null) Object.Destroy(collider);
            
            Bind(go);
            Position = new Vector3(-3, 0, 0);
        }

        public void Move(Vector3 delta)
        {
            Position += delta;
        }

        public void TryShoot(Node node)
        {
            if (Time.time - _lastShootTime < ShootCooldown) return;
            _lastShootTime = Time.time;

            // 发出射击请求事件
            OwnerThread.Broadcast(new ShootRequestEvent
            {
                Shooter = this,
                Position = Position + Vector3.right * 0.5f,
                Direction = Vector3.right
            });
        }
    }
}
