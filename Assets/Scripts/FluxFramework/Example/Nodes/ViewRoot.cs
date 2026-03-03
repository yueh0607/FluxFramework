using System.Collections.Generic;
using UnityEngine;

namespace FluxFramework.Example
{
    /// <summary>
    /// 视图分支根节点
    /// 作为 ViewThread 的业务层入口
    /// 监听逻辑线程的生命周期事件，自动创建/销毁视图节点
    /// </summary>
    public class ViewRoot : ViewNode
    {
        // 视图节点映射：逻辑节点ID -> 视图节点
        private Dictionary<int, ViewNode> _viewNodes = new Dictionary<int, ViewNode>();
        
        private ThreadNode _logicThread;  // 逻辑线程引用

        /// <summary>
        /// 初始化，设置逻辑线程引用
        /// </summary>
        public void Initialize(ThreadNode logicThread)
        {
            _logicThread = logicThread;
        }

        public override void OnSpawn()
        {
            base.OnSpawn();
            CreateEmpty("ViewRoot");

            // 监听来自逻辑线程的节点创建事件
            On<NodeSpawnedEvent>(OnNodeSpawned);
            
            // 监听来自逻辑线程的节点销毁事件
            On<NodeDespawnedEvent>(OnNodeDespawned);
            
            Debug.Log("[ViewRoot] Initialized, listening for logic events...");
        }

        /// <summary>
        /// 收到逻辑节点创建事件，自动创建对应视图
        /// </summary>
        private void OnNodeSpawned(NodeSpawnedEvent e)
        {
            Debug.Log($"[ViewRoot] OnNodeSpawned: {e.NodeType} (ID={e.NodeId})");
            
            ViewNode viewNode = null;

            switch (e.NodeType)
            {
                case "Player":
                    var playerView = AddChild<PlayerViewNode>();
                    playerView.Initialize(e.NodeId, e.Position);
                    viewNode = playerView;
                    break;

                case "Enemy":
                    var enemyView = AddChild<EnemyViewNode>();
                    enemyView.Initialize(e.NodeId, e.Position);
                    viewNode = enemyView;
                    break;

                case "Bullet":
                    var bulletView = AddChild<BulletViewNode>();
                    bulletView.Initialize(e.NodeId, e.Position);
                    viewNode = bulletView;
                    break;
            }

            if (viewNode != null)
            {
                _viewNodes[e.NodeId] = viewNode;
            }
        }

        /// <summary>
        /// 收到逻辑节点销毁事件，自动销毁对应视图
        /// </summary>
        private void OnNodeDespawned(NodeDespawnedEvent e)
        {
            Debug.Log($"[ViewRoot] OnNodeDespawned: {e.NodeType} (ID={e.NodeId})");
            
            if (_viewNodes.TryGetValue(e.NodeId, out var viewNode))
            {
                _viewNodes.Remove(e.NodeId);
                RemoveAndDespawnChild(viewNode);
            }
        }

        public override void OnDespawn()
        {
            _viewNodes.Clear();
            base.OnDespawn();
        }

        /// <summary>
        /// 获取逻辑线程引用
        /// </summary>
        public ThreadNode GetLogicThread() => _logicThread;
    }
}

