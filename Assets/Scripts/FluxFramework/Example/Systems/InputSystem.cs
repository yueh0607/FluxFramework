using UnityEngine;

namespace FluxFramework.Example
{
    /// <summary>
    /// 输入系统 - 处理玩家输入并发出事件
    /// </summary>
    public class InputSystem : NodeSystem
    {
        public override void OnAttach(Node node)
        {
            if (node is IInputHandler handler)
            {
                node.On<TickEventArgs>(e => OnTick(e, handler, node));
            }
        }

        private void OnTick(TickEventArgs e, IInputHandler handler, Node node)
        {
            // 移动输入
            var move = Vector3.zero;
            if (Input.GetKey(KeyCode.W)) move.z += 1;
            if (Input.GetKey(KeyCode.S)) move.z -= 1;
            if (Input.GetKey(KeyCode.A)) move.x -= 1;
            if (Input.GetKey(KeyCode.D)) move.x += 1;

            if (move != Vector3.zero)
            {
                node.OwnerThread.Broadcast(new MoveRequestEvent
                {
                    Target = node,
                    Direction = move.normalized,
                    Speed = handler.MoveSpeed
                });
            }

            // 射击输入
            if (Input.GetKey(KeyCode.Space))
            {
                handler.TryShoot(node);
            }
        }
    }

    /// <summary>
    /// 输入处理接口
    /// </summary>
    public interface IInputHandler
    {
        float MoveSpeed { get; }
        void TryShoot(Node node);
    }
}
