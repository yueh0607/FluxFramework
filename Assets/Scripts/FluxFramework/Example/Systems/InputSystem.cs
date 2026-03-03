using UnityEngine;

namespace FluxFramework.Example
{
    /// <summary>
    /// 输入系统（必须在主线程/ViewThread 运行）
    /// 采集 Unity 输入后，通过跨线程事件发送到逻辑线程
    /// </summary>
    public class InputSystem : NodeSystem
    {
        private ThreadNode _logicThread;  // 逻辑线程引用

        public override int Priority => 1000;  // 最高优先级

        /// <summary>
        /// 设置逻辑线程引用（在 GameBoot 中调用）
        /// </summary>
        public void SetLogicThread(ThreadNode logicThread)
        {
            _logicThread = logicThread;
        }

        public override void OnAttach(Node node)
        {
            // 只在 ViewRoot 上附加，采集全局输入
            if (node is ViewRoot)
            {
                node.On<TickEventArgs>(OnTick);
            }
        }

        public override void OnDetach(Node node)
        {
        }

        private void OnTick(TickEventArgs e)
        {
            // 采集 Unity 输入（必须在主线程）
            var moveInput = Vector3.zero;
            bool shootPressed = false;

            if (Input.GetKey(KeyCode.W)) moveInput.z += 1;
            if (Input.GetKey(KeyCode.S)) moveInput.z -= 1;
            if (Input.GetKey(KeyCode.A)) moveInput.x -= 1;
            if (Input.GetKey(KeyCode.D)) moveInput.x += 1;
            if (Input.GetKeyDown(KeyCode.Space)) shootPressed = true;

            // 如果有输入，发送跨线程事件到逻辑线程
            if (moveInput != Vector3.zero || shootPressed)
            {
                if (_logicThread != null && OwnerThread != null)
                {
                    OwnerThread.EmitTo(_logicThread, new InputEvent
                    {
                        MoveInput = new Vector2(moveInput.x, moveInput.z),
                        ShootPressed = shootPressed
                    });
                }
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

