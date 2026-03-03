using System;

namespace FluxFramework
{
    /// <summary>
    /// 事件消息（用于跨线程传递事件）
    /// 泛型版本，每种事件类型一个具体类
    /// </summary>
    public abstract class EventMessage : Message
    {
        /// <summary>
        /// 目标节点ID（null=广播，非null=定向）
        /// </summary>
        public int? TargetNodeId { get; set; }

        /// <summary>
        /// 在目标线程分发事件
        /// </summary>
        public abstract void DispatchOn(ThreadNode thread);

        public override void OnSpawn()
        {
            base.OnSpawn();
            TargetNodeId = null;
        }

        public override void OnDespawn()
        {
            TargetNodeId = null;
            base.OnDespawn();
        }
    }

    /// <summary>
    /// 泛型事件消息
    /// </summary>
    public class EventMessage<T> : EventMessage
    {
        public T EventData { get; set; }

        public override void DispatchOn(ThreadNode thread)
        {
            if (TargetNodeId.HasValue)
            {
                // 定向事件
                thread.Emit(EventData, TargetNodeId.Value);
            }
            else
            {
                // 广播事件
                thread.Emit(EventData);
            }
        }

        public override void OnSpawn()
        {
            base.OnSpawn();
            EventData = default;
        }

        public override void OnDespawn()
        {
            EventData = default;
            base.OnDespawn();
        }
    }
}
