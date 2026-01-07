using UnityEngine;
using CastleWars.Data;
using CastleWars.Combat;
using CastleWars.Core;

namespace CastleWars.Units
{
    /// <summary>
    /// 单位战斗控制（纯本地模式）
    /// </summary>
    public class UnitCombat : MonoBehaviour
    {
        private UnitBase unitBase;
        private UnitController unitController;
        private float lastAttackTime;

        [Header("战斗设置")]
        public LayerMask enemyLayer;
        public Transform attackPoint;

        [Header("投射物")]
        public GameObject projectilePrefab;

        private void Awake()
        {
            unitBase = GetComponent<UnitBase>();
            unitController = GetComponent<UnitController>();
        }

        /// <summary>
        /// 获取所属玩家ID
        /// </summary>
        private int GetOwnerId()
        {
            if (unitController != null) return unitController.OwnerId;
            if (unitBase != null) return unitBase.OwnerId;
            return 0;
        }

        /// <summary>
        /// 获取攻击范围
        /// </summary>
        private float GetAttackRange()
        {
            if (unitController != null) return unitController.AttackRange;
            if (unitBase != null && unitBase.unitData != null) return unitBase.unitData.attackRange;
            return 2f;
        }

        /// <summary>
        /// 获取攻击伤害
        /// </summary>
        private float GetAttackDamage()
        {
            if (unitController != null) return unitController.AttackDamage;
            if (unitBase != null && unitBase.unitData != null) return unitBase.unitData.attackDamage;
            return 10f;
        }

        /// <summary>
        /// 获取攻击速度
        /// </summary>
        private float GetAttackSpeed()
        {
            if (unitController != null) return unitController.AttackSpeed;
            if (unitBase != null && unitBase.unitData != null) return unitBase.unitData.attackSpeed;
            return 1f;
        }

        /// <summary>
        /// 查找最近的敌人
        /// </summary>
        public Transform FindNearestEnemy(int myOwnerId)
        {
            float attackRange = GetAttackRange();

            Collider[] colliders = Physics.OverlapSphere(transform.position, attackRange * 2, enemyLayer);

            Transform nearest = null;
            float nearestDistance = float.MaxValue;

            foreach (Collider col in colliders)
            {
                // 检查 UnitController
                UnitController enemyController = col.GetComponent<UnitController>();
                if (enemyController != null && enemyController.OwnerId != myOwnerId && !enemyController.IsDead)
                {
                    float distance = Vector3.Distance(transform.position, col.transform.position);
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearest = col.transform;
                    }
                    continue;
                }

                // 检查 UnitBase
                UnitBase enemy = col.GetComponent<UnitBase>();
                if (enemy != null && enemy.OwnerId != myOwnerId && !enemy.IsDead)
                {
                    if (!CanAttackTarget(enemy)) continue;

                    float dist = Vector3.Distance(transform.position, col.transform.position);
                    if (dist < nearestDistance)
                    {
                        nearestDistance = dist;
                        nearest = col.transform;
                    }
                }
            }

            return nearest;
        }

        private bool CanAttackTarget(UnitBase target)
        {
            if (target.unitData == null) return true;
            if (unitBase != null && unitBase.unitData != null)
            {
                if (target.unitData.unitType == UnitType.Air)
                {
                    return unitBase.unitData.canAttackAir;
                }
            }
            return true;
        }

        /// <summary>
        /// 攻击目标
        /// </summary>
        public void Attack(Transform target)
        {
            if (target == null) return;

            float attackSpeed = GetAttackSpeed();
            if (Time.time < lastAttackTime + (1f / attackSpeed))
            {
                return;
            }

            lastAttackTime = Time.time;

            Vector3 direction = (target.position - transform.position).normalized;
            transform.rotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));

            float attackDamage = GetAttackDamage();
            int myOwnerId = GetOwnerId();

            // 攻击 UnitController
            UnitController targetController = target.GetComponent<UnitController>();
            if (targetController != null)
            {
                PerformAttack(targetController, attackDamage, myOwnerId);
                return;
            }

            // 攻击 UnitBase
            UnitBase targetUnit = target.GetComponent<UnitBase>();
            if (targetUnit != null)
            {
                PerformAttack(targetUnit, attackDamage, myOwnerId);
                return;
            }

            // 攻击城堡
            CastleController targetCastle = target.GetComponent<CastleController>();
            if (targetCastle != null)
            {
                targetCastle.TakeDamage(attackDamage);
            }
        }

        private void PerformAttack(UnitController target, float damage, int attackerId)
        {
            PlayAttackAnimation();

            bool isRanged = unitBase != null && unitBase.unitData != null &&
                           unitBase.unitData.attackType == AttackType.Ranged;

            if (isRanged && projectilePrefab != null)
            {
                SpawnProjectile(target.transform, damage, attackerId);
            }
            else
            {
                target.TakeDamage(damage, attackerId);

                if (unitBase != null && unitBase.unitData != null && unitBase.unitData.hasAOE)
                {
                    PerformAOEDamage(target.transform.position, damage, attackerId);
                }
            }
        }

        private void PerformAttack(UnitBase target, float damage, int attackerId)
        {
            PlayAttackAnimation();

            bool isRanged = unitBase != null && unitBase.unitData != null &&
                           unitBase.unitData.attackType == AttackType.Ranged;

            if (isRanged && projectilePrefab != null)
            {
                SpawnProjectile(target.transform, damage, attackerId);
            }
            else
            {
                target.TakeDamage(damage, attackerId);

                if (unitBase != null && unitBase.unitData != null && unitBase.unitData.hasAOE)
                {
                    PerformAOEDamage(target.transform.position, damage, attackerId);
                }
            }
        }

        private void SpawnProjectile(Transform target, float damage, int attackerId)
        {
            Vector3 spawnPos = attackPoint != null ? attackPoint.position : transform.position + Vector3.up;
            GameObject projectile = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);

            ProjectileController projController = projectile.GetComponent<ProjectileController>();
            if (projController != null)
            {
                float aoeRadius = unitBase != null && unitBase.unitData != null ? unitBase.unitData.aoeRadius : 0f;
                bool hasAOE = unitBase != null && unitBase.unitData != null && unitBase.unitData.hasAOE;

                projController.Initialize(target, damage, attackerId, hasAOE, aoeRadius);
            }
        }

        private void PerformAOEDamage(Vector3 center, float damage, int attackerId)
        {
            float aoeRadius = unitBase != null && unitBase.unitData != null ? unitBase.unitData.aoeRadius : 2f;

            Collider[] hits = Physics.OverlapSphere(center, aoeRadius, enemyLayer);

            foreach (Collider hit in hits)
            {
                UnitController enemyController = hit.GetComponent<UnitController>();
                if (enemyController != null && enemyController.OwnerId != attackerId)
                {
                    enemyController.TakeDamage(damage * 0.5f, attackerId);
                    continue;
                }

                UnitBase enemy = hit.GetComponent<UnitBase>();
                if (enemy != null && enemy.OwnerId != attackerId)
                {
                    enemy.TakeDamage(damage * 0.5f, attackerId);
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
