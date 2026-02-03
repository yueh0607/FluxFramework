using UnityEngine;
using System.Collections.Generic;

namespace FluxFramework.Example
{
    /// <summary>
    /// 碰撞系统 - 处理碰撞检测
    /// </summary>
    public class CollisionSystem : NodeSystem
    {
        public override void OnAttach(Node node)
        {
            node.On<TickEventArgs>(e => CheckCollisions(node));
        }

        private void CheckCollisions(Node node)
        {
            if (node is ICollidable collidable)
            {
                // 查找潜在的碰撞目标
                var targets = collidable.GetCollisionTargets();
                if (targets == null) return;

                foreach (var target in targets)
                {
                    if (target is ICollidable otherCollidable)
                    {
                        float dist = Vector3.Distance(collidable.Position, otherCollidable.Position);
                        if (dist < collidable.CollisionRadius + otherCollidable.CollisionRadius)
                        {
                            // 发出碰撞事件
                            node.OwnerThread.Broadcast(new CollisionEvent
                            {
                                CollidingNode = node,
                                OtherNode = target
                            });
                            break;
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// 可碰撞接口
    /// </summary>
    public interface ICollidable
    {
        Vector3 Position { get; }
        float CollisionRadius { get; }
        List<Node> GetCollisionTargets();
    }
}
