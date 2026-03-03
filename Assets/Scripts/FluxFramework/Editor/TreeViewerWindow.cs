#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace FluxFramework.Editor
{
    /// <summary>
    /// 树结构查看器编辑器窗口
    /// </summary>
    public class TreeViewerWindow : EditorWindow
    {
        private Vector2 _scrollPosition;
        private bool _autoRefresh = true;
        private float _refreshInterval = 0.5f;
        private double _lastRefreshTime;
        private string _treeString = "";
        private bool _includeDetails = true;
        private bool _includePool = true;
        private GUIStyle _treeStyle;

        [MenuItem("Window/Tree Framework/Tree Viewer")]
        public static void ShowWindow()
        {
            var window = GetWindow<TreeViewerWindow>("Tree Viewer");
            window.minSize = new Vector2(400, 300);
        }

        private void OnEnable()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        }

        private void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                _treeString = "";
            }
        }

        private void OnGUI()
        {
            InitStyles();

            DrawToolbar();
            DrawTreeView();
        }

        private void InitStyles()
        {
            if (_treeStyle == null)
            {
                _treeStyle = new GUIStyle(EditorStyles.label)
                {
                    font = Font.CreateDynamicFontFromOSFont("Consolas", 12),
                    richText = true,
                    wordWrap = false
                };
            }
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                RefreshTree();
            }

            GUILayout.Space(10);

            _autoRefresh = GUILayout.Toggle(_autoRefresh, "Auto", EditorStyles.toolbarButton, GUILayout.Width(50));

            if (_autoRefresh)
            {
                EditorGUILayout.LabelField("Interval:", GUILayout.Width(50));
                _refreshInterval = EditorGUILayout.Slider(_refreshInterval, 0.1f, 2f, GUILayout.Width(100));
            }

            GUILayout.Space(10);

            _includeDetails = GUILayout.Toggle(_includeDetails, "Details", EditorStyles.toolbarButton, GUILayout.Width(50));

            GUILayout.Space(5);

            _includePool = GUILayout.Toggle(_includePool, "Pool", EditorStyles.toolbarButton, GUILayout.Width(40));

            GUILayout.Space(10);

            // EnableView 开关
            if (Application.isPlaying)
            {
                bool enableView = Tree.EnableView;
                bool newEnableView = GUILayout.Toggle(enableView, "View", EditorStyles.toolbarButton, GUILayout.Width(50));
                if (newEnableView != enableView)
                {
                    Tree.EnableView = newEnableView;
                    Debug.Log($"[TreeViewer] EnableView set to: {newEnableView}");
                }
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Copy", EditorStyles.toolbarButton, GUILayout.Width(50)))
            {
                EditorGUIUtility.systemCopyBuffer = _treeString;
                Debug.Log("Tree structure copied to clipboard");
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawTreeView()
        {
            // 自动刷新
            if (_autoRefresh && Application.isPlaying)
            {
                if (EditorApplication.timeSinceStartup - _lastRefreshTime > _refreshInterval)
                {
                    RefreshTree();
                    _lastRefreshTime = EditorApplication.timeSinceStartup;
                    Repaint();
                }
            }

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to view the tree structure.", MessageType.Info);
            }
            else if (!Tree.IsInitialized)
            {
                EditorGUILayout.HelpBox("Tree is not initialized. Call Tree.Initialize() first.", MessageType.Warning);
            }
            else if (string.IsNullOrEmpty(_treeString))
            {
                RefreshTree();
            }

            if (!string.IsNullOrEmpty(_treeString))
            {
                EditorGUILayout.TextArea(_treeString, _treeStyle, GUILayout.ExpandHeight(true));
            }

            EditorGUILayout.EndScrollView();
        }

        private void RefreshTree()
        {
            if (Application.isPlaying && Tree.IsInitialized)
            {
                _treeString = Node.GetFullTreeString(_includeDetails, _includePool);
            }
            else
            {
                _treeString = "";
            }
        }

        private void Update()
        {
            // 强制重绘以实现自动刷新
            if (_autoRefresh && Application.isPlaying)
            {
                Repaint();
            }
        }
    }
}
#endif
