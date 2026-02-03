using System;

namespace FluxFramework
{
    /// <summary>
    /// 系统基类
    /// 系统是可复用的逻辑单元，多个节点共享同一个系统实例
    /// 系统之间通过事件通信，完全解耦
    /// </summary>
    /// <remarks>
    /// 系统设计原则：
    /// 1. 系统不定义具体事件，由用户自己定义
    /// 2. 系统通过接口和类型转换访问节点数据
    /// 3. 系统之间完全解耦，不直接调用
    /// </remarks>
    public abstract class NodeSystem : Node
    {
        /// <summary>
        /// 系统优先级（影响执行顺序，数字越大越先执行）
        /// </summary>
        public virtual int Priority => 0;

        /// <summary>
        /// 系统是否启用
        /// </summary>
        public bool Enabled { get; set; } = true;

        #region 生命周期回调

        /// <summary>
        /// 当系统被附加到节点时调用
        /// 通常在这里订阅事件
        /// </summary>
        public virtual void OnAttach(Node owner) { }

        /// <summary>
        /// 当系统从节点移除时调用
        /// </summary>
        public virtual void OnDetach(Node owner) { }

        /// <summary>
        /// 系统每帧更新
        /// </summary>
        /// <param name="owner">拥有此系统的节点</param>
        /// <param name="args">Tick 参数</param>
        public virtual void OnSystemTick(Node owner, TickEventArgs args) { }

        #endregion

        #region 显示

        internal override string GetNodeDisplayName(bool includeDetails)
        {
            var typeName = GetType().Name;
            
            if (!includeDetails)
                return typeName;

            return $"{typeName} [ID:{Id}]";
        }

        #endregion
    }
}
