using UnityEngine;

namespace FluxFramework.Example
{
    /// <summary>
    /// Transform 同步事件
    /// 逻辑节点发送，视图节点接收
    /// </summary>
    public struct TransformSyncEvent
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Scale;
    }

    /// <summary>
    /// 生命值同步事件
    /// </summary>
    public struct HealthSyncEvent
    {
        public float CurrentHp;
        public float MaxHp;
        public float Ratio => MaxHp > 0 ? CurrentHp / MaxHp : 0;
    }

    /// <summary>
    /// 节点创建事件（用于视图层自动创建视图节点）
    /// </summary>
    public struct NodeSpawnedEvent
    {
        public int NodeId;
        public string NodeType;  // "Player", "Enemy", "Bullet"
        public Vector3 Position;
    }

    /// <summary>
    /// 节点销毁事件（用于视图层自动销毁视图节点）
    /// </summary>
    public struct NodeDespawnedEvent
    {
        public int NodeId;
        public string NodeType;
    }

    /// <summary>
    /// 输入事件（从主线程发送到逻辑线程）
    /// </summary>
    public struct InputEvent
    {
        public Vector2 MoveInput;   // WASD 输入
        public bool ShootPressed;   // 空格键
    }
}
