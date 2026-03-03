using System;
using System.Collections.Generic;
using System.Threading;

namespace FluxFramework
{
    /// <summary>
    /// 线程节点
    /// 通用的线程边界 + 事件中心
    /// 可配置为外部驱动（如主线程）或独立线程运行
    /// 其子树在此线程上下文运行，拥有独立的邮箱和事件注册表
    /// </summary>
    public class ThreadNode : Node
    {
        #region 线程属性

        /// <summary>
        /// 线程名称
        /// </summary>
        public string ThreadName { get; private set; }

        /// <summary>
        /// 是否运行在独立线程
        /// false = 外部驱动（如主线程手动调用 Tick）
        /// true = 内部自动循环
        /// </summary>
        public bool RunsOnDedicatedThread { get; private set; }

        /// <summary>
        /// 邮箱，用于接收跨线程消息
        /// </summary>
        public MailBox InBox { get; private set; }

        private Thread _thread;
        private volatile bool _running;
        private float _targetFrameTime = 1f / 60f; // 默认 60 FPS

        #endregion

        #region 事件注册表（深度分桶 + 链表）

        /// <summary>
        /// 事件类型 -> 深度桶数组，每个桶是一个链表（广播事件）
        /// </summary>
        private readonly Dictionary<Type, List<LinkedList<EventSubscriber>>> _eventRegistry 
            = new Dictionary<Type, List<LinkedList<EventSubscriber>>>();

        /// <summary>
        /// 定向事件注册表：事件类型 -> (目标ID -> 订阅者链表)
        /// 用于支持按ID定向发送事件
        /// </summary>
        private readonly Dictionary<Type, Dictionary<int, LinkedList<EventSubscriber>>> _targetedEventRegistry
            = new Dictionary<Type, Dictionary<int, LinkedList<EventSubscriber>>>();

        /// <summary>
        /// 复用的 Tick 事件参数（struct，零 GC）
        /// </summary>
        private TickEventArgs _tickArgs;

        #endregion

        #region 构造函数

        public ThreadNode()
        {
            InBox = new MailBox();
            // ThreadNode 的 OwnerThread 是自己
            OwnerThread = this;
        }

        #endregion

        #region 生命周期

        public override void OnSpawn()
        {
            base.OnSpawn();
            OwnerThread = this; // 确保 OwnerThread 是自己
        }

        public override void OnDespawn()
        {
            Stop();
            InBox.Clear();
            ClearEventRegistry();
            Tree.UnregisterThreadNode(this);
            base.OnDespawn();
        }

        #endregion

        #region 线程配置

        /// <summary>
        /// 配置线程节点
        /// </summary>
        /// <param name="name">线程名称</param>
        /// <param name="runOnDedicatedThread">是否在独立线程运行（false=外部驱动）</param>
        public void Configure(string name, bool runOnDedicatedThread = false)
        {
            ThreadName = name;
            RunsOnDedicatedThread = runOnDedicatedThread;
            Tree.RegisterThreadNode(this);
        }

        /// <summary>
        /// 设置目标帧率（仅对独立线程有效）
        /// </summary>
        public void SetTargetFrameRate(int fps)
        {
            _targetFrameTime = 1f / fps;
        }

        #endregion

        #region 线程管理

        /// <summary>
        /// 启动线程（仅对 RunsOnDedicatedThread=true 有效）
        /// </summary>
        public void Start()
        {
            if (!RunsOnDedicatedThread)
            {
                UnityEngine.Debug.LogWarning($"ThreadNode '{ThreadName}' is configured for external driving, no need to Start()");
                return;
            }

            if (_running) return;

            _running = true;
            _thread = new Thread(ThreadLoop)
            {
                Name = ThreadName,
                IsBackground = true
            };
            _thread.Start();
        }

        /// <summary>
        /// 停止线程
        /// </summary>
        public void Stop()
        {
            if (!_running) return;

            _running = false;
            _thread?.Join(1000); // 等待最多 1 秒
            _thread = null;
        }

        /// <summary>
        /// 线程循环
        /// </summary>
        private void ThreadLoop()
        {
            var lastTime = DateTime.UtcNow;

            while (_running)
            {
                var now = DateTime.UtcNow;
                var deltaTime = (float)(now - lastTime).TotalSeconds;
                lastTime = now;

                try
                {
                    Tick(deltaTime);
                }
                catch (Exception ex)
                {
                    // 记录异常但继续运行
                    UnityEngine.Debug.LogException(ex);
                }

                // 控制帧率
                var elapsed = (DateTime.UtcNow - now).TotalSeconds;
                var sleepTime = _targetFrameTime - elapsed;
                if (sleepTime > 0)
                {
                    Thread.Sleep((int)(sleepTime * 1000));
                }
            }
        }

        #endregion

        #region Tick 驱动

        /// <summary>
        /// 驱动子树更新
        /// - 外部驱动模式：由外部调用（如 MonoBehaviour.Update）
        /// - 独立线程模式：由内部循环自动调用
        /// </summary>
        public void Tick(float deltaTime)
        {
            // 1. 处理邮件
            ProcessMails();

            // 2. 派发 Tick 事件（struct，零 GC）
            _tickArgs.DeltaTime = deltaTime;
            _tickArgs.Handled = false;
            Emit(_tickArgs);
        }

        /// <summary>
        /// 处理邮箱中的所有消息
        /// </summary>
        private void ProcessMails()
        {
            while (InBox.TryReceive(out var msg))
            {
                try
                {
                    // 检查是否是事件消息
                    if (msg is EventMessage eventMsg)
                    {
                        // 自动在当前线程分发事件
                        eventMsg.DispatchOn(this);
                    }
                    else
                    {
                        // 普通消息
                        var target = NodePool.FindById(msg.TargetId);
                        if (target != null && target.OwnerThread == this)
                        {
                            target.OnMessage(msg);
                        }
                    }
                }
                finally
                {
                    MessagePool.Despawn(msg);
                }
            }
        }

        #endregion

        #region 跨线程事件

        /// <summary>
        /// 跨线程发送广播事件
        /// 使用方式：logicThread.EmitTo(viewThread, new NodeSpawnedEvent {...});
        /// </summary>
        public void EmitTo<T>(ThreadNode targetThread, T args)
        {
            if (targetThread == null) return;

            if (targetThread == this)
            {
                // 同线程，直接 Emit
                Emit(args);
            }
            else
            {
                // 跨线程，包装成消息投递到目标邮箱
                var msg = MessagePool.Spawn<EventMessage<T>>();
                msg.EventData = args;
                msg.TargetNodeId = null;  // 广播
                targetThread.InBox.Post(msg);
            }
        }

        /// <summary>
        /// 跨线程发送定向事件
        /// 使用方式：logicThread.EmitTo(viewThread, new TransformSyncEvent {...}, targetId);
        /// </summary>
        public void EmitTo<T>(ThreadNode targetThread, T args, int targetId)
        {
            if (targetThread == null) return;

            if (targetThread == this)
            {
                // 同线程，直接 Emit
                Emit(args, targetId);
            }
            else
            {
                // 跨线程，包装成消息投递到目标邮箱
                var msg = MessagePool.Spawn<EventMessage<T>>();
                msg.EventData = args;
                msg.TargetNodeId = targetId;  // 定向
                targetThread.InBox.Post(msg);
            }
        }

        #endregion

        #region 事件注册表

        /// <summary>
        /// 注册事件 - O(1)，泛型方法，零 GC
        /// </summary>
        internal EventHandle Register<T>(Node node, Action<T> handler)
        {
            var type = typeof(T);

            if (!_eventRegistry.TryGetValue(type, out var buckets))
            {
                buckets = new List<LinkedList<EventSubscriber>>();
                _eventRegistry[type] = buckets;
            }

            // 确保深度桶存在
            while (buckets.Count <= node.Depth)
            {
                buckets.Add(new LinkedList<EventSubscriber>());
            }

            var list = buckets[node.Depth];
            var subscriber = new EventSubscriber
            {
                Node = node,
                Handler = handler
            };
            var listNode = list.AddLast(subscriber);

            // 返回句柄，持有链表节点引用，取消时 O(1)
            return new EventHandle
            {
                ListNode = listNode,
                List = list
            };
        }

        /// <summary>
        /// 派发事件 - O(H + K)，H=最大深度，K=订阅者数
        /// 按深度从小到大遍历，保证父节点先于子节点收到事件
        /// 泛型方法，零装箱，零 GC
        /// </summary>
        public void Emit<T>(T args)
        {
            var type = typeof(T);

            if (!_eventRegistry.TryGetValue(type, out var buckets))
                return;

            // 按深度从小到大遍历（父先于子）
            foreach (var list in buckets)
            {
                if (list == null) continue;

                var current = list.First;
                while (current != null)
                {
                    var next = current.Next; // 缓存下一个，防止迭代时删除
                    var sub = current.Value;

                    try
                    {
                        ((Action<T>)sub.Handler)(args);
                    }
                    catch (Exception ex)
                    {
                        UnityEngine.Debug.LogException(ex);
                    }

                    // 检查 Handled（特殊处理不同类型）
                    if (CheckHandled(args))
                        return; // 中断传播

                    current = next;
                }
            }
        }

        /// <summary>
        /// 广播事件（Emit的别名，语义更清晰）
        /// </summary>
        public void Broadcast<T>(T args)
        {
            Emit(args);
        }

        /// <summary>
        /// 注册定向事件 - O(1)
        /// 只接收指定 targetId 的事件
        /// </summary>
        internal EventHandle RegisterTargeted<T>(int targetId, Node node, Action<T> handler)
        {
            var type = typeof(T);

            if (!_targetedEventRegistry.TryGetValue(type, out var idMap))
            {
                idMap = new Dictionary<int, LinkedList<EventSubscriber>>();
                _targetedEventRegistry[type] = idMap;
            }

            if (!idMap.TryGetValue(targetId, out var list))
            {
                list = new LinkedList<EventSubscriber>();
                idMap[targetId] = list;
            }

            var subscriber = new EventSubscriber
            {
                Node = node,
                Handler = handler
            };
            var listNode = list.AddLast(subscriber);

            return new EventHandle
            {
                ListNode = listNode,
                List = list
            };
        }

        /// <summary>
        /// 定向发送事件 - O(1) 查找 + O(K) 分发，K=该ID的订阅者数
        /// 只触发订阅了指定 targetId 的处理器
        /// </summary>
        public void Emit<T>(T args, int targetId)
        {
            var type = typeof(T);

            if (!_targetedEventRegistry.TryGetValue(type, out var idMap))
                return;

            if (!idMap.TryGetValue(targetId, out var list))
                return;

            var current = list.First;
            while (current != null)
            {
                var next = current.Next;
                var sub = current.Value;

                try
                {
                    ((Action<T>)sub.Handler)(args);
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogException(ex);
                }

                // 检查 Handled
                if (CheckHandled(args))
                    return;

                current = next;
            }
        }

        /// <summary>
        /// 检查事件是否已被处理（支持 struct 和 class）
        /// </summary>
        private bool CheckHandled<T>(T args)
        {
            // struct TickEventArgs
            if (args is TickEventArgs tick)
                return tick.Handled;
            
            // class EventArgs
            if (args is EventArgs evt)
                return evt.Handled;
            
            return false;
        }

        /// <summary>
        /// 派发事件（自动从池中获取和回收 EventArgs）
        /// 仅用于 class 类型的 EventArgs
        /// 使用方式：EmitPooled<DamageEventArgs>(args => { args.Damage = 10; });
        /// </summary>
        public void EmitPooled<T>(System.Action<T> setup = null) where T : EventArgs, new()
        {
            var args = Pool<T>.Spawn();
            try
            {
                setup?.Invoke(args);
                Emit(args);
            }
            finally
            {
                Pool<T>.Despawn(args);
            }
        }

        /// <summary>
        /// 清空事件注册表
        /// </summary>
        private void ClearEventRegistry()
        {
            // 清空广播事件
            foreach (var buckets in _eventRegistry.Values)
            {
                foreach (var list in buckets)
                {
                    list?.Clear();
                }
                buckets.Clear();
            }
            _eventRegistry.Clear();

            // 清空定向事件
            foreach (var idMap in _targetedEventRegistry.Values)
            {
                foreach (var list in idMap.Values)
                {
                    list?.Clear();
                }
                idMap.Clear();
            }
            _targetedEventRegistry.Clear();
        }

        #endregion

        #region 子节点管理

        protected override void OnChildAdded(Node child)
        {
            base.OnChildAdded(child);
            // 子节点的 OwnerThread 在 Node.AddChildInternal 中已设置
        }

        #endregion
    }
}
