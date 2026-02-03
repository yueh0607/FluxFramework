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
            node.On<MoveRequestEvent>(OnMoveRequest);
            node.On<TickEventArgs>(e => OnAutoMove(e, node));
        }

        private void OnMoveRequest(MoveRequestEvent e)
        {
            if (e.Target is IMovable movable)
            {
                movable.Move(e.Direction * e.Speed * Time.deltaTime);
            }
        }

        private void OnAutoMove(TickEventArgs e, Node node)
        {
            // 自动移动逻辑（如Enemy的往复移动）
            if (node is IAutoMovable autoMovable)
            {
                autoMovable.AutoMove(e.DeltaTime);
            }
        }
    }

    /// <summary>
    /// 可移动接口
    /// </summary>
    public interface IMovable
    {
        void Move(Vector3 delta);
    }

    /// <summary>
    /// 自动移动接口
    /// </summary>
    public interface IAutoMovable
    {
        void AutoMove(float deltaTime);
    }
}
