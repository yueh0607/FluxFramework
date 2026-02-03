using UnityEngine;

namespace FluxFramework
{
    /// <summary>
    /// 视图节点
    /// 可绑定 GameObject 的节点，必须在主线程的 ThreadNode 下
    /// Unity GameObject 树是 Node 树的子集
    /// </summary>
    public class ViewNode : Node
    {
        #region GameObject 绑定

        /// <summary>
        /// 绑定的 GameObject
        /// </summary>
        public GameObject GameObject { get; private set; }

        /// <summary>
        /// 便捷访问 Transform
        /// </summary>
        public Transform Transform => GameObject?.transform;

        /// <summary>
        /// Prefab 路径（如果是从 Prefab 创建的）
        /// </summary>
        public string PrefabPath { get; private set; }

        #endregion

        #region 生命周期

        public override void OnSpawn()
        {
            base.OnSpawn();
        }

        public override void OnDespawn()
        {
            // 销毁 GameObject
            if (GameObject != null)
            {
                Object.Destroy(GameObject);
                GameObject = null;
            }
            PrefabPath = null;
            base.OnDespawn();
        }

        #endregion

        #region GameObject 操作

        /// <summary>
        /// 绑定已有的 GameObject
        /// </summary>
        public void Bind(GameObject go)
        {
            if (go == null) return;

            // 如果已有 GameObject，先销毁
            if (GameObject != null && GameObject != go)
            {
                Object.Destroy(GameObject);
            }

            GameObject = go;

            // 同步到父节点的 Transform 下
            SyncToParentTransform();
        }

        /// <summary>
        /// 从 Resources 路径创建 Prefab
        /// </summary>
        public void CreateFromPrefab(string path)
        {
            var prefab = Resources.Load<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogError($"Failed to load prefab from path: {path}");
                return;
            }

            CreateFromPrefab(prefab);
            PrefabPath = path;
        }

        /// <summary>
        /// 从 Prefab 实例化
        /// </summary>
        public void CreateFromPrefab(GameObject prefab)
        {
            if (prefab == null) return;

            // 如果已有 GameObject，先销毁
            if (GameObject != null)
            {
                Object.Destroy(GameObject);
            }

            GameObject = Object.Instantiate(prefab);

            // 同步到父节点的 Transform 下
            SyncToParentTransform();
        }

        /// <summary>
        /// 创建空的 GameObject
        /// </summary>
        public void CreateEmpty(string name = null)
        {
            // 如果已有 GameObject，先销毁
            if (GameObject != null)
            {
                Object.Destroy(GameObject);
            }

            GameObject = new GameObject(name ?? GetType().Name);

            // 同步到父节点的 Transform 下
            SyncToParentTransform();
        }

        /// <summary>
        /// 同步到父节点的 Transform 下
        /// </summary>
        private void SyncToParentTransform()
        {
            if (GameObject == null) return;

            // 查找父 ViewNode
            var parentView = FindAncestor<ViewNode>();
            if (parentView?.Transform != null)
            {
                Transform.SetParent(parentView.Transform, false);
            }
        }

        #endregion

        #region 子节点管理

        protected override void OnChildAdded(Node child)
        {
            base.OnChildAdded(child);

            // 如果子节点是 ViewNode 且有 GameObject，同步 Transform 层级
            if (child is ViewNode viewChild && viewChild.GameObject != null)
            {
                if (Transform != null)
                {
                    viewChild.Transform.SetParent(Transform, false);
                }
            }
        }

        protected override void OnChildRemoved(Node child)
        {
            base.OnChildRemoved(child);

            // 子节点移除时，解除 Transform 父子关系
            if (child is ViewNode viewChild && viewChild.GameObject != null)
            {
                viewChild.Transform.SetParent(null, false);
            }
        }

        #endregion

        #region 组件访问

        /// <summary>
        /// 获取组件
        /// </summary>
        public T GetComponent<T>() where T : Component
        {
            return GameObject?.GetComponent<T>();
        }

        /// <summary>
        /// 获取或添加组件
        /// </summary>
        public T GetOrAddComponent<T>() where T : Component
        {
            if (GameObject == null) return null;
            var comp = GameObject.GetComponent<T>();
            if (comp == null)
            {
                comp = GameObject.AddComponent<T>();
            }
            return comp;
        }

        /// <summary>
        /// 获取子物体上的组件
        /// </summary>
        public T GetComponentInChildren<T>() where T : Component
        {
            return GameObject?.GetComponentInChildren<T>();
        }

        /// <summary>
        /// 获取子物体上的所有组件
        /// </summary>
        public T[] GetComponentsInChildren<T>() where T : Component
        {
            return GameObject?.GetComponentsInChildren<T>();
        }

        #endregion

        #region 常用属性

        /// <summary>
        /// 激活/隐藏 GameObject
        /// </summary>
        public bool Active
        {
            get => GameObject?.activeSelf ?? false;
            set => GameObject?.SetActive(value);
        }

        /// <summary>
        /// 本地坐标
        /// </summary>
        public Vector3 LocalPosition
        {
            get => Transform?.localPosition ?? Vector3.zero;
            set { if (Transform != null) Transform.localPosition = value; }
        }

        /// <summary>
        /// 世界坐标
        /// </summary>
        public Vector3 Position
        {
            get => Transform?.position ?? Vector3.zero;
            set { if (Transform != null) Transform.position = value; }
        }

        /// <summary>
        /// 本地旋转
        /// </summary>
        public Quaternion LocalRotation
        {
            get => Transform?.localRotation ?? Quaternion.identity;
            set { if (Transform != null) Transform.localRotation = value; }
        }

        /// <summary>
        /// 本地缩放
        /// </summary>
        public Vector3 LocalScale
        {
            get => Transform?.localScale ?? Vector3.one;
            set { if (Transform != null) Transform.localScale = value; }
        }

        #endregion

        #region 树结构可视化

        /// <summary>
        /// 获取节点显示名称，包含 GameObject 信息
        /// </summary>
        internal override string GetNodeDisplayName(bool includeDetails)
        {
            var baseName = base.GetNodeDisplayName(includeDetails);
            
            if (includeDetails && GameObject != null)
            {
                return $"{baseName} → {GameObject.name}";
            }
            
            return baseName;
        }

        #endregion
    }
}
