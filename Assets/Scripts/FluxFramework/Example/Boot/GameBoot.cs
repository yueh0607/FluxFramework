using UnityEngine;

namespace FluxFramework.Example
{
    /// <summary>
    /// 游戏启动器
    /// 展示树框架的射击游戏示例
    /// </summary>
    public class GameBoot : MonoBehaviour
    {
        [Header("游戏设置")]
        [SerializeField] private int _enemyCount = 3;
        [SerializeField] private float _enemySpacing = 3f;

        private ThreadNode _mainThread;
        private GameRootNode _gameRoot;
        private PlayerNode _player;
        private BulletManagerNode _bulletManager;
        private EnemyManagerNode _enemyManager;

        void Start()
        {
            InitializeTree();
            SetupCamera();
        }

        void Update()
        {
            // 驱动主线程
            Tree.Tick(_mainThread, Time.deltaTime);

            // 按 T 打印树结构
            if (Input.GetKeyDown(KeyCode.T))
            {
                PrintTreeStructure(false);
            }

            // 按 P 打印完整树结构（包含对象池）
            if (Input.GetKeyDown(KeyCode.P))
            {
                PrintTreeStructure(true);
            }

            // 按 R 重启游戏
            if (Input.GetKeyDown(KeyCode.R))
            {
                RestartGame();
            }
        }

        void OnDestroy()
        {
            Tree.Shutdown();
        }

        /// <summary>
        /// 初始化树结构
        /// </summary>
        private void InitializeTree()
        {
            // 1. 初始化全局树（会创建 FluxRoot -> SystemContainer + PoolContainer + UserRoot）
            Tree.Initialize();

            // 2. 注册全局系统
            RegisterSystems();

            // 3. 在 UserRoot 下创建主线程节点（外部驱动模式）
            _mainThread = Tree.Root.AddChild<ThreadNode>();
            _mainThread.Configure("MainThread", runOnDedicatedThread: false);

            // 4. 创建游戏根节点
            _gameRoot = _mainThread.AddChild<GameRootNode>();

            // 5. 创建子弹管理器节点
            _bulletManager = _gameRoot.AddChild<BulletManagerNode>();

            // 6. 创建敌人管理器节点
            _enemyManager = _gameRoot.AddChild<EnemyManagerNode>();

            // 7. 创建玩家
            _player = _gameRoot.AddChild<PlayerNode>();
            _player.Initialize();

            // 8. 创建敌人
            for (int i = 0; i < _enemyCount; i++)
            {
                var pos = new Vector3(3 + i * _enemySpacing, 0, 0);
                _enemyManager.SpawnEnemy(pos);
            }

            Debug.Log("=== Game Initialized ===");
            Debug.Log("Controls: WASD to move, SPACE to shoot");
            Debug.Log("Press T to view tree structure (P to include pool)");
            Debug.Log("Press R to restart");
            
            PrintTreeStructure();
        }

        /// <summary>
        /// 注册全局系统
        /// </summary>
        private void RegisterSystems()
        {
            Tree.SystemContainer.RegisterSystem<InputSystem>();
            Tree.SystemContainer.RegisterSystem<MoveSystem>();
            Tree.SystemContainer.RegisterSystem<ShootSystem>();
            Tree.SystemContainer.RegisterSystem<CollisionSystem>();
            Tree.SystemContainer.RegisterSystem<HealthSystem>();
            
            Debug.Log("=== Systems Registered ===");
        }

        /// <summary>
        /// 设置摄像机
        /// </summary>
        private void SetupCamera()
        {
            var cam = Camera.main;
            if (cam != null)
            {
                cam.transform.position = new Vector3(5, 10, 0);
                cam.transform.rotation = Quaternion.Euler(90, 0, 0);
                cam.orthographic = true;
                cam.orthographicSize = 8;
            }
        }

        /// <summary>
        /// 重启游戏
        /// </summary>
        private void RestartGame()
        {
            Tree.Shutdown();
            InitializeTree();
        }

        /// <summary>
        /// 打印树结构
        /// </summary>
        private void PrintTreeStructure(bool includePool = false)
        {
            Debug.Log(Node.GetFullTreeString(true, includePool));
        }

        /// <summary>
        /// 在屏幕上显示信息
        /// </summary>
        void OnGUI()
        {
            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14
            };

            GUILayout.BeginArea(new Rect(10, 10, 300, 250));
            
            GUILayout.Label($"Active Nodes: {NodePool.ActiveCount}", style);
            GUILayout.Label($"Pooled Nodes: {NodePool.TotalPooledCount}", style);
            GUILayout.Space(10);
            GUILayout.Label("=== Controls ===", style);
            GUILayout.Label("WASD - Move", style);
            GUILayout.Label("SPACE - Shoot", style);
            GUILayout.Label("T - Print Tree", style);
            GUILayout.Label("P - Print Tree + Pool", style);
            GUILayout.Label("R - Restart", style);
            GUILayout.Space(10);
            GUILayout.Label("Window > Tree Framework > Tree Viewer", style);
            
            GUILayout.EndArea();
        }
    }
}
