using UnityEngine;

namespace FluxFramework.Example
{
    /// <summary>
    /// 生命值系统 - 处理伤害和死亡
    /// </summary>
    public class HealthSystem : NodeSystem
    {
        public override void OnAttach(Node node)
        {
            node.On<DamageEvent>(e => OnDamage(e, node));
            node.On<CollisionEvent>(e => OnCollision(e, node));
        }

        private void OnDamage(DamageEvent e, Node node)
        {
            if (e.Target != node) return;

            if (node is IDamageable damageable)
            {
                // 逻辑在System里：直接扣血
                damageable.CurrentHp -= e.Damage;
                Debug.Log($"{node.GetType().Name} took {e.Damage} damage! HP: {damageable.CurrentHp}/{damageable.MaxHp}");

                if (damageable.CurrentHp <= 0)
                {
                    node.OwnerThread.Broadcast(new DeathEvent { Target = node });
                }
            }
        }

        private void OnCollision(CollisionEvent e, Node node)
        {
            // 子弹击中敌人
            if (e.CollidingNode is IBullet bullet && e.OtherNode is IDamageable)
            {
                node.OwnerThread.Broadcast(new DamageEvent
                {
                    Target = e.OtherNode,
                    Damage = bullet.Damage,
                    Source = e.CollidingNode
                });

                // 销毁子弹
                bullet.DestroySelf();
            }
        }
    }

    /// <summary>
    /// 可受伤接口 - 纯数据，无逻辑
    /// </summary>
    public interface IDamageable
    {
        int CurrentHp { get; set; }
        int MaxHp { get; }
        Color OriginalColor { get; }
    }

    /// <summary>
    /// 子弹接口
    /// </summary>
    public interface IBullet
    {
        int Damage { get; }
        void DestroySelf();
    }
}
