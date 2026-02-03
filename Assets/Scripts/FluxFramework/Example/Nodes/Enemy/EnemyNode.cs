using UnityEngine;
using System.Collections.Generic;

namespace FluxFramework.Example
{
    /// <summary>
    /// 敌人节点 - 使用系统架构
    /// </summary>
    public class EnemyNode : ViewNode, IAutoMovable, IDamageable
    {
        public int CurrentHp { get; private set; }
        public int MaxHp { get; private set; } = 100;
        
        public float MoveSpeed = 2f;
        public float MoveRange = 3f;
        
        private Vector3 _startPos;
        private float _moveDirection = 1f;
        private Renderer _renderer;

        public override void OnSpawn()
        {
            base.OnSpawn();
            CurrentHp = MaxHp;
            
            // 附加系统
            AttachSystem<MoveSystem>();
            AttachSystem<HealthSystem>();
            
            // 监听死亡事件
            On<DeathEvent>(OnDeath);
        }

        public void Initialize(Vector3 startPosition)
        {
            // 创建敌人外观
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "Enemy";
            _renderer = go.GetComponent<Renderer>();
            _renderer.material.color = Color.red;
            go.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);

            MoveSpeed = Random.Range(1, 3f);
            
            // 移除默认碰撞体
            var collider = go.GetComponent<Collider>();
            if (collider != null) Object.Destroy(collider);
            
            Bind(go);
            Position = startPosition;
            _startPos = startPosition;
        }

        public void AutoMove(float deltaTime)
        {
            if (GameObject == null) return;

            // 循环往复移动
            var pos = Position;
            pos.z += MoveSpeed * _moveDirection * deltaTime;

            // 到达边界则反向
            float minZ = _startPos.z - MoveRange;
            float maxZ = _startPos.z + MoveRange;
            
            if (pos.z > maxZ)
            {
                pos.z = maxZ;
                _moveDirection = -1f;
            }
            else if (pos.z < minZ)
            {
                pos.z = minZ;
                _moveDirection = 1f;
            }

            Position = pos;
        }

        public void TakeDamage(int damage)
        {
            CurrentHp -= damage;
            
            // 闪烁效果
            if (_renderer != null)
            {
                _renderer.material.color = Color.white;
                DelayCall(0.1f, () => {
                    if (_renderer != null)
                        _renderer.material.color = Color.red;
                });
            }
        }

        private void OnDeath(DeathEvent e)
        {
            if (e.Target != this) return;
            
            Debug.Log("Enemy died! Respawning...");
            // 重置
            CurrentHp = MaxHp;
            Position = _startPos;
        }

        private void DelayCall(float delay, System.Action action)
        {
            if (GameObject != null)
            {
                var helper = GameObject.GetComponent<DelayHelper>();
                if (helper == null)
                    helper = GameObject.AddComponent<DelayHelper>();
                helper.StartCoroutine(DelayCoroutine(delay, action));
            }
        }

        private System.Collections.IEnumerator DelayCoroutine(float delay, System.Action action)
        {
            yield return new WaitForSeconds(delay);
            action?.Invoke();
        }
    }
}
