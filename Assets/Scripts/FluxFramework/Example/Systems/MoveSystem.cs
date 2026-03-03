using UnityEngine;

namespace FluxFramework.Example
{
    /// <summary>
    /// 移动系统 - 处理移动逻辑
    /// </summary>
    public class MoveSystem : NodeSystem
    {
        public override void OnAttach(Node node)
        {
            node.On<MoveRequestEvent>(e => OnMoveRequest(e, node));
            node.On<TickEventArgs>(e => OnAutoMove(e, node));
        }

        private void OnMoveRequest(MoveRequestEvent e, Node node)
        {
            // 逻辑在System里：直接操作逻辑节点的Position
            if (e.Target is IMovable movable && node is PlayerNode playerNode)
            {
                playerNode.Position += e.Direction * e.Speed * Time.deltaTime;
            }
        }

        private void OnAutoMove(TickEventArgs e, Node node)
        {
            // 自动移动逻辑（如Enemy的往复移动）
            if (node is IAutoMovable autoMovable && node is EnemyNode enemyNode)
            {
                // 循环往复移动逻辑在System里
                var pos = enemyNode.Position;
                pos.z += autoMovable.MoveSpeed * autoMovable.MoveDirection * e.DeltaTime;

                // 到达边界则反向
                float minZ = autoMovable.StartPosition.z - autoMovable.MoveRange;
                float maxZ = autoMovable.StartPosition.z + autoMovable.MoveRange;
                
                if (pos.z > maxZ)
                {
                    pos.z = maxZ;
                    autoMovable.MoveDirection = -1f;
                }
                else if (pos.z < minZ)
                {
                    pos.z = minZ;
                    autoMovable.MoveDirection = 1f;
                }

                enemyNode.Position = pos;
            }
        }
    }

    /// <summary>
    /// 可移动接口 - 纯数据，无逻辑
    /// </summary>
    public interface IMovable
    {
        // 只提供数据访问，不包含逻辑方法
    }

    /// <summary>
    /// 自动移动接口 - 纯数据，无逻辑
    /// </summary>
    public interface IAutoMovable
    {
        float MoveSpeed { get; set; }
        float MoveRange { get; set; }
        float MoveDirection { get; set; }
        Vector3 StartPosition { get; }
    }
}
