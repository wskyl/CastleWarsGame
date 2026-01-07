using UnityEngine;
using CastleWars.Data;
using CastleWars.Combat;
using CastleWars.Core;

namespace CastleWars.Units
{
    /// <summary>
    /// 单位战斗控制
    /// 支持本地模式和网络模式
    /// </summary>
    public class UnitCombat : MonoBehaviour
    {
        private UnitBase unitBase;
        private float lastAttackTime;

        [Header("战斗设置")]
        public LayerMask enemyLayer;
        public Transform attackPoint;

        [Header("投射物")]
        public GameObject projectilePrefab;

        private void Awake()
        {
            unitBase = GetComponent<UnitBase>();
        }

        /// <summary>
        /// 查找最近的敌人
        /// </summary>
        public Transform FindNearestEnemy(int myOwnerId)
        {
            if (unitBase == null || unitBase.unitData == null) return null;

            Collider[] colliders = Physics.OverlapSphere(
                transform.position,
                unitBase.unitData.attackRange * 2,
                enemyLayer
            );

            Transform nearest = null;
            float nearestDistance = float.MaxValue;

            foreach (Collider col in colliders)
            {
                // 本地模式检查LocalUnit
                if (LocalGameMode.IsLocalMode)
                {
                    LocalUnit localEnemy = col.GetComponent<LocalUnit>();
                    if (localEnemy != null && localEnemy.OwnerId != myOwnerId && !localEnemy.IsDead)
                    {
                        float distance = Vector3.Distance(transform.position, col.transform.position);
                        if (distance < nearestDistance)
                        {
                            nearestDistance = distance;
                            nearest = col.transform;
                        }
                    }
                    continue;
                }

                // 网络模式检查UnitBase
                UnitBase enemy = col.GetComponent<UnitBase>();
                if (enemy == null) continue;

                int enemyOwnerId = GetUnitOwnerId(enemy);
                if (enemyOwnerId == myOwnerId) continue;

                if (!CanAttackTarget(enemy)) continue;

                float dist = Vector3.Distance(transform.position, col.transform.position);
                if (dist < nearestDistance)
                {
                    nearestDistance = dist;
                    nearest = col.transform;
                }
            }

            return nearest;
        }

        private int GetUnitOwnerId(UnitBase unit)
        {
#if UNITY_NETCODE
            return unit.ownerId.Value;
#else
            return unit.OwnerIdValue;
#endif
        }

        private int GetMyOwnerId()
        {
#if UNITY_NETCODE
            return unitBase.ownerId.Value;
#else
            return unitBase.OwnerIdValue;
#endif
        }

        private bool CanAttackTarget(UnitBase target)
        {
            if (target.unitData.unitType == UnitType.Air)
            {
                return unitBase.unitData.canAttackAir;
            }
            return true;
        }

        /// <summary>
        /// 攻击目标
        /// </summary>
        public void Attack(Transform target)
        {
            if (target == null || unitBase == null || unitBase.unitData == null) return;

            if (Time.time < lastAttackTime + (1f / unitBase.unitData.attackSpeed))
            {
                return;
            }

            lastAttackTime = Time.time;

            Vector3 direction = (target.position - transform.position).normalized;
            transform.rotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));

            // 本地模式攻击LocalUnit
            if (LocalGameMode.IsLocalMode)
            {
                LocalUnit localTarget = target.GetComponent<LocalUnit>();
                if (localTarget != null)
                {
                    PerformAttackLocal(localTarget);
                    return;
                }
            }

            // 网络模式攻击UnitBase
            UnitBase targetUnit = target.GetComponent<UnitBase>();
            if (targetUnit != null)
            {
                if (unitBase.unitData.attackType == AttackType.Melee)
                {
                    PerformMeleeAttack(targetUnit);
                }
                else
                {
                    PerformRangedAttack(target);
                }
            }
        }

        private void PerformAttackLocal(LocalUnit target)
        {
            PlayAttackAnimation();

            if (unitBase.unitData.attackType == AttackType.Melee)
            {
                int myOwnerId = GetMyOwnerId();
                target.TakeDamage(unitBase.unitData.attackDamage, myOwnerId);

                if (unitBase.unitData.hasAOE)
                {
                    PerformAOEDamageLocal(target.transform.position);
                }
            }
            else
            {
                PerformRangedAttack(target.transform);
            }
        }

        private void PerformMeleeAttack(UnitBase target)
        {
            PlayAttackAnimation();

            int myOwnerId = GetMyOwnerId();
            target.TakeDamage(unitBase.unitData.attackDamage, myOwnerId);

            if (unitBase.unitData.hasAOE)
            {
                PerformAOEDamage(target.transform.position);
            }
        }

        private void PerformRangedAttack(Transform target)
        {
            PlayAttackAnimation();

            if (projectilePrefab != null)
            {
                Vector3 spawnPos = attackPoint != null ? attackPoint.position : transform.position + Vector3.up;
                GameObject projectile = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);

                ProjectileController projController = projectile.GetComponent<ProjectileController>();
                if (projController != null)
                {
                    int myOwnerId = GetMyOwnerId();
                    projController.Initialize(
                        target,
                        unitBase.unitData.attackDamage,
                        myOwnerId,
                        unitBase.unitData.hasAOE,
                        unitBase.unitData.aoeRadius
                    );
                }
            }
        }

        private void PerformAOEDamageLocal(Vector3 center)
        {
            Collider[] hits = Physics.OverlapSphere(center, unitBase.unitData.aoeRadius, enemyLayer);
            int myOwnerId = GetMyOwnerId();

            foreach (Collider hit in hits)
            {
                LocalUnit enemy = hit.GetComponent<LocalUnit>();
                if (enemy != null && enemy.OwnerId != myOwnerId)
                {
                    enemy.TakeDamage(unitBase.unitData.attackDamage * 0.5f, myOwnerId);
                }
            }
        }

        private void PerformAOEDamage(Vector3 center)
        {
            Collider[] hits = Physics.OverlapSphere(center, unitBase.unitData.aoeRadius, enemyLayer);
            int myOwnerId = GetMyOwnerId();

            foreach (Collider hit in hits)
            {
                UnitBase enemy = hit.GetComponent<UnitBase>();
                if (enemy != null && GetUnitOwnerId(enemy) != myOwnerId)
                {
                    enemy.TakeDamage(unitBase.unitData.attackDamage * 0.5f, myOwnerId);
                }
            }
        }

        private void PlayAttackAnimation()
        {
            Animator animator = GetComponent<Animator>();
            if (animator != null)
            {
                animator.SetTrigger("Attack");
            }
        }
    }
}
