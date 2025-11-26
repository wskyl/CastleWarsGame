using UnityEngine;
using CastleWars.Data;
using CastleWars.Combat;

namespace CastleWars.Units
{
    /// <summary>
    /// 单位战斗控制
    /// </summary>
    public class UnitCombat : MonoBehaviour
    {
        private UnitBase unitBase;
        private float lastAttackTime;

        [Header("战斗设置")]
        public LayerMask enemyLayer;
        public Transform attackPoint; // 攻击点（发射子弹位置）

        [Header("投射物")]
        public GameObject projectilePrefab; // 远程单位的投射物

        private void Awake()
        {
            unitBase = GetComponent<UnitBase>();
        }

        /// <summary>
        /// 查找最近的敌人
        /// </summary>
        public Transform FindNearestEnemy(int myOwnerId)
        {
            Collider[] colliders = Physics.OverlapSphere(
                transform.position,
                unitBase.unitData.attackRange * 2, // 搜索范围稍大于攻击范围
                enemyLayer
            );

            Transform nearest = null;
            float nearestDistance = float.MaxValue;

            foreach (Collider col in colliders)
            {
                UnitBase enemy = col.GetComponent<UnitBase>();
                if (enemy == null) continue;

                // 检查是否是敌人
                if (enemy.ownerId.Value == myOwnerId) continue;

                // 检查类型匹配（地面单位不能攻击空中）
                if (!CanAttackTarget(enemy)) continue;

                float distance = Vector3.Distance(transform.position, col.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = col.transform;
                }
            }

            return nearest;
        }

        private bool CanAttackTarget(UnitBase target)
        {
            // 如果目标是空中单位，检查是否能攻击空中
            if (target.unitData.unitType == UnitType.Air)
            {
                return unitBase.unitData.canAttackAir;
            }

            return true; // 所有单位都能攻击地面单位
        }

        /// <summary>
        /// 攻击目标
        /// </summary>
        public void Attack(Transform target)
        {
            if (target == null) return;

            // 检查攻击冷却
            if (Time.time < lastAttackTime + (1f / unitBase.unitData.attackSpeed))
            {
                return;
            }

            lastAttackTime = Time.time;

            // 转向目标
            Vector3 direction = (target.position - transform.position).normalized;
            transform.rotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));

            UnitBase targetUnit = target.GetComponent<UnitBase>();
            if (targetUnit == null) return;

            if (unitBase.unitData.attackType == AttackType.Melee)
            {
                // 近战直接造成伤害
                PerformMeleeAttack(targetUnit);
            }
            else
            {
                // 远程发射投射物
                PerformRangedAttack(target);
            }
        }

        private void PerformMeleeAttack(UnitBase target)
        {
            // 播放攻击动画
            PlayAttackAnimation();

            // 对目标造成伤害
            target.TakeDamageServerRpc(unitBase.unitData.attackDamage, (int)unitBase.ownerId.Value);

            // AOE伤害
            if (unitBase.unitData.hasAOE)
            {
                PerformAOEDamage(target.transform.position);
            }
        }

        private void PerformRangedAttack(Transform target)
        {
            // 播放攻击动画
            PlayAttackAnimation();

            // 生成投射物
            if (projectilePrefab != null)
            {
                Vector3 spawnPos = attackPoint != null ? attackPoint.position : transform.position + Vector3.up;
                GameObject projectile = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);

                ProjectileController projController = projectile.GetComponent<ProjectileController>();
                if (projController != null)
                {
                    projController.Initialize(
                        target,
                        unitBase.unitData.attackDamage,
                        (int)unitBase.ownerId.Value,
                        unitBase.unitData.hasAOE,
                        unitBase.unitData.aoeRadius
                    );
                }
            }
        }

        private void PerformAOEDamage(Vector3 center)
        {
            Collider[] hits = Physics.OverlapSphere(center, unitBase.unitData.aoeRadius, enemyLayer);

            foreach (Collider hit in hits)
            {
                UnitBase enemy = hit.GetComponent<UnitBase>();
                if (enemy != null && enemy.ownerId.Value != unitBase.ownerId.Value)
                {
                    // AOE伤害通常是主伤害的一部分
                    enemy.TakeDamageServerRpc(
                        unitBase.unitData.attackDamage * 0.5f,
                        (int)unitBase.ownerId.Value
                    );
                }
            }
        }

        private void PlayAttackAnimation()
        {
            // 触发攻击动画
            Animator animator = GetComponent<Animator>();
            if (animator != null)
            {
                animator.SetTrigger("Attack");
            }
        }
    }
}
