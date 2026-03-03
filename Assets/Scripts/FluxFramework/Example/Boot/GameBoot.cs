using UnityEngine;

namespace FluxFramework.Example
{
    /// <summary>
    /// 游戏启动器 - 双线程架构
    /// 
    /// 架构：
    /// - LogicThread: 独立线程，30fps，运行游戏逻辑
    /// - ViewThread: 主线程，Unity 帧率，渲染视图
    /// - 通过 MailBox + EventMessage 跨线程通信
    /// </summary>
    public class GameBoot : MonoBehaviour
    {
        [Header("游戏设置")]
        [SerializeField] private int _enemyCount = 3;
        [SerializeField] private float _enemySpacing = 3f;
        [SerializeField] private bool _useMultiThread = true;  // 是否启用多线程
        [SerializeField] private int _logicFrameRate = 30;

        // 双线程
        private ThreadNode _logicThread;
        private ThreadNode _viewThread;
        
        // 业务根节点
        private LogicRoot _logicRoot;
        private ViewRoot _viewRoot;
        
        // 逻辑节点
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
            if (_useMultiThread)
            {
                // 多线程模式：只驱动视图线程（逻辑线程自己运行）
                Tree.Tick(_viewThread, Time.deltaTime);
            }
            else
            {
                // 单线程模式：手动驱动两个线程
                Tree.Tick(_logicThread, Time.deltaTime);
                Tree.Tick(_viewThread, Time.deltaTime);
            }

            // 调试按键
            if (Input.GetKeyDown(KeyCode.T))
            {
                PrintTreeStructure(false);
            }

            if (Input.GetKeyDown(KeyCode.P))
            {
                PrintTreeStructure(true);
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                RestartGame();
            }
        }

        void OnDestroy()
        {
            if (_useMultiThread && _logicThread != null)
            {
                _logicThread.Stop();
            }
            Tree.Shutdown();
        }

        /// <summary>
        /// 初始化双线程树结构
        /// 
        /// FluxRoot
        /// ├── LogicThread (独立线程, 30fps)
        /// │   ├── LogicSystemRoot (SystemContainerNode)
        /// │   │   ├── MoveSystem
        /// │   │   ├── ShootSystem
        /// │   │   ├── CollisionSystem
        /// │   │   └── HealthSystem
        /// │   └── LogicRoot (业务逻辑)
        /// │       ├── PlayerNode
        /// │       ├── BulletManagerNode
        /// │       └── EnemyManagerNode
        /// │
        /// └── ViewThread (主线程, Unity帧率)
        ///     ├── ViewSystemRoot (SystemContainerNode)
        ///     │   └── InputSystem
        ///     └── ViewRoot (业务视图)
        ///         ├── PlayerViewNode
        ///         ├── EnemyViewNode
        ///         └── BulletViewNode
        /// </summary>
        private void InitializeTree()
        {
            // 1. 初始化全局树
            Tree.Initialize();

            // 2. 创建逻辑线程
            _logicThread = Tree.Root.AddChild<ThreadNode>();
            _logicThread.Configure("LogicThread", runOnDedicatedThread: _useMultiThread);
            _logicThread.SetTargetFrameRate(_logicFrameRate);

            // 3. 创建逻辑系统根节点（自动注册逻辑系统）
            var logicSystemRoot = _logicThread.AddChild<LogicSystemRoot>();

            // 4. 创建视图线程
            _viewThread = Tree.Root.AddChild<ThreadNode>();
            _viewThread.Configure("ViewThread", runOnDedicatedThread: false);

            // 5. 创建视图系统根节点（自动注册视图系统）
            var viewSystemRoot = _viewThread.AddChild<ViewSystemRoot>();
            viewSystemRoot.ConfigureSystems(_logicThread);  // 配置 InputSystem 的逻辑线程引用

            // 6. 创建视图分支（先创建，以便监听逻辑事件）
            _viewRoot = _viewThread.AddChild<ViewRoot>();
            _viewRoot.Initialize(_logicThread);

            // 7. 创建逻辑分支
            _logicRoot = _logicThread.AddChild<LogicRoot>();
            _logicRoot.Initialize(_viewThread);

            // 8. 创建游戏逻辑节点
            _bulletManager = _logicRoot.AddChild<BulletManagerNode>();
            _enemyManager = _logicRoot.AddChild<EnemyManagerNode>();
            
            _player = _logicRoot.AddChild<PlayerNode>();
            _player.Initialize(new Vector3(-3, 0, 0));

            // 9. 创建敌人
            for (int i = 0; i < _enemyCount; i++)
            {
                var pos = new Vector3(3 + i * _enemySpacing, 0, 0);
                _enemyManager.SpawnEnemy(pos);
            }

            // 10. 启动逻辑线程（如果是多线程模式）
            if (_useMultiThread)
            {
                _logicThread.Start();
                Debug.Log($"=== Multi-Thread Mode: Logic={_logicFrameRate}fps, View=Unity ===");
            }
            else
            {
                Debug.Log("=== Single-Thread Mode ===");
            }

            Debug.Log("=== Game Initialized ===");
            Debug.Log("Controls: WASD to move, SPACE to shoot");
            Debug.Log("Press T to view tree structure (P to include pool)");
            Debug.Log("Press R to restart");
            
            PrintTreeStructure();
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
            if (_useMultiThread && _logicThread != null)
            {
                _logicThread.Stop();
            }
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

            GUILayout.BeginArea(new Rect(10, 10, 350, 320));
            
            GUILayout.Label($"Active Nodes: {NodePool.ActiveCount}", style);
            GUILayout.Label($"Pooled Nodes: {NodePool.TotalPooledCount}", style);
            GUILayout.Space(10);
            GUILayout.Label("=== Architecture ===", style);
            GUILayout.Label($"Mode: {(_useMultiThread ? "Multi-Thread" : "Single-Thread")}", style);
            if (_useMultiThread)
            {
                GUILayout.Label($"Logic: {_logicFrameRate}fps (独立线程)", style);
                GUILayout.Label($"View: Unity帧率 (主线程)", style);
            }
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
