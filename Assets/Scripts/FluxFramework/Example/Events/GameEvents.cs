using UnityEngine;

namespace FluxFramework.Example
{
    /// <summary>
    /// 移动请求事件
    /// </summary>
    public class MoveRequestEvent : EventArgs
    {
        public Node Target;
        public Vector3 Direction;
        public float Speed;
    }

    /// <summary>
    /// 射击请求事件
    /// </summary>
    public class ShootRequestEvent : EventArgs
    {
        public Node Shooter;
        public Vector3 Position;
        public Vector3 Direction;
    }

    /// <summary>
    /// 碰撞事件
    /// </summary>
    public class CollisionEvent : EventArgs
    {
        public Node CollidingNode;
        public Node OtherNode;
    }

    /// <summary>
    /// 伤害事件
    /// </summary>
    public class DamageEvent : EventArgs
    {
        public Node Target;
        public int Damage;
        public Node Source;
    }

    /// <summary>
    /// 死亡事件
    /// </summary>
    public class DeathEvent : EventArgs
    {
        public Node Target;
    }

    /// <summary>
    /// 生成子弹事件
    /// </summary>
    public class SpawnBulletEvent : EventArgs
    {
        public Vector3 Position;
        public Vector3 Direction;
    }
}
