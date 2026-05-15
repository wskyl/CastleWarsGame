using UnityEngine;
using CastleWars.Data;
using CastleWars.Combat;
using CastleWars.Core;

namespace CastleWars.Units
{
    /// <summary>
    /// 单位战斗控制器
    /// 管理敌人搜索、攻击执行、AOE伤害
    /// </summary>
    [RequireComponent(typeof(UnitBase))]
    public class UnitCombat : MonoBehaviour
    {
        [Header("战斗设置")]
        [Tooltip("敌人检测层级")]
        [SerializeField] private LayerMask enemyLayer;

        [Tooltip("攻击发射点")]
        [SerializeField] private Transform attackPoint;

        [Header("投射物")]
        [Tooltip("投射物预制体")]
        [SerializeField] private GameObject projectilePrefab;

        // 组件引用
        private UnitBase _unitBase;
        private UnitMovement _movement;
        private Animator _animator;

        // 攻击计时
        private float _lastAttackTime = -999f;

        // 属性
        public bool CanAttackNow => Time.time >= _lastAttackTime + GetAttackInterval();

        private void Awake()
        {
            _unitBase = GetComponent<UnitBase>();
            _movement = GetComponent<UnitMovement>();
            _animator = GetComponent<Animator>();
        }

        private void Start()
        {
            // 如果没有设置攻击点，使用单位中心偏上
            if (attackPoint == null)
            {
                attackPoint = transform;
            }

            // 如果有配置投射物预制体，使用单位数据中的
            if (_unitBase?.unitData?.projectilePrefab != null && projectilePrefab == null)
            {
                projectilePrefab = _unitBase.unitData.projectilePrefab;
            }
        }

        /// <summary>
        /// 获取攻击间隔
        /// </summary>
        private float GetAttackInterval()
        {
            if (_unitBase?.unitData == null) return 1f;
            return 1f / _unitBase.unitData.attackSpeed;
        }

        #region 敌人搜索

        /// <summary>
        /// 查找最近的敌人
        /// </summary>
        /// <param name="myOwnerId">自己的所有者ID</param>
        /// <returns>最近敌人的Transform</returns>
        public Transform FindNearestEnemy(int myOwnerId)
        {
            if (_unitBase?.unitData == null) return null;

            float searchRadius = _unitBase.unitData.attackRange * 1.5f;
            Collider[] colliders = Physics.OverlapSphere(transform.position, searchRadius, enemyLayer);

            Transform nearestTarget = null;
            float nearestDistance = float.MaxValue;

            foreach (Collider col in colliders)
            {
                // 检查敌方单位
                UnitBase enemyUnit = col.GetComponent<UnitBase>();
                if (enemyUnit != null && enemyUnit.OwnerId != myOwnerId && !enemyUnit.IsDead)
                {
                    // 检查是否可以攻击该类型单位
                    if (!_unitBase.CanAttack(enemyUnit)) continue;

                    float distance = Vector3.Distance(transform.position, col.transform.position);
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestTarget = col.transform;
                    }
                    continue;
                }

                // 检查敌方城堡
                CastleController enemyCastle = col.GetComponent<CastleController>();
                if (enemyCastle != null && enemyCastle.OwnerId != myOwnerId && !enemyCastle.IsDestroyed)
                {
                    float distance = Vector3.Distance(transform.position, col.transform.position);
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestTarget = col.transform;
                    }
                }
            }

            return nearestTarget;
        }

        /// <summary>
        /// 获取攻击范围内的所有敌人
        /// </summary>
        public UnitBase[] GetEnemiesInRange(int myOwnerId)
        {
            if (_unitBase?.unitData == null) return new UnitBase[0];

            Collider[] colliders = Physics.OverlapSphere(
                transform.position,
                _unitBase.unitData.attackRange,
                enemyLayer
            );

            System.Collections.Generic.List<UnitBase> enemies = new System.Collections.Generic.List<UnitBase>();

            foreach (Collider col in colliders)
            {
                UnitBase enemyUnit = col.GetComponent<UnitBase>();
                if (enemyUnit != null && enemyUnit.OwnerId != myOwnerId && !enemyUnit.IsDead)
                {
                    if (_unitBase.CanAttack(enemyUnit))
                    {
                        enemies.Add(enemyUnit);
                    }
                }
            }

            return enemies.ToArray();
        }

        #endregion

        #region 攻击执行

        /// <summary>
        /// 攻击目标
        /// </summary>
        public void Attack(Transform target)
        {
            if (target == null || _unitBase == null || _unitBase.unitData == null) return;
            if (!CanAttackNow) return;

            _lastAttackTime = Time.time;

            // 面向目标
            _movement?.FaceTarget(target);

            // 播放攻击动画
            PlayAttackAnimation();

            // 根据攻击类型执行攻击
            if (_unitBase.unitData.attackType == AttackType.Melee)
            {
                PerformMeleeAttack(target);
            }
            else
            {
                PerformRangedAttack(target);
            }
        }

        /// <summary>
        /// 执行近战攻击
        /// </summary>
        private void PerformMeleeAttack(Transform target)
        {
            float damage = _unitBase.GetAttackDamage();
            int myOwnerId = _unitBase.OwnerId;

            // 检查是否是克制关系
            UnitBase targetUnit = target.GetComponent<UnitBase>();
            if (targetUnit != null && _unitBase.unitData.IsCounterTo(targetUnit.unitData))
            {
                damage *= 2f; // 克制伤害翻倍
            }

            // 对主目标造成伤害
            DealDamageToTarget(target, damage, myOwnerId);

            // 处理AOE
            if (_unitBase.unitData.hasAOE && _unitBase.unitData.aoeRadius > 0)
            {
                PerformAOEDamage(target.position, damage * 0.5f, myOwnerId);
            }

            // 处理击晕
            if (_unitBase.unitData.stunChance > 0)
            {
                TryApplyStun(target);
            }
        }

        /// <summary>
        /// 执行远程攻击
        /// </summary>
        private void PerformRangedAttack(Transform target)
        {
            if (projectilePrefab == null)
            {
                // 没有投射物，直接造成伤害
                PerformMeleeAttack(target);
                return;
            }

            // 生成投射物
            Vector3 spawnPos = attackPoint != null ? attackPoint.position : transform.position + Vector3.up;
            GameObject projectileObj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);

            ProjectileController projectile = projectileObj.GetComponent<ProjectileController>();
            if (projectile != null)
            {
                float damage = _unitBase.GetAttackDamage();

                // 检查克制关系
                UnitBase targetUnit = target.GetComponent<UnitBase>();
                if (targetUnit != null && _unitBase.unitData.IsCounterTo(targetUnit.unitData))
                {
                    damage *= 2f;
                }

                projectile.Initialize(
                    target,
                    damage,
                    _unitBase.OwnerId,
                    _unitBase.unitData.hasAOE,
                    _unitBase.unitData.aoeRadius,
                    _unitBase.unitData.projectileSpeed
                );
            }
        }

        /// <summary>
        /// 对目标造成伤害
        /// </summary>
        private void DealDamageToTarget(Transform target, float damage, int attackerId)
        {
            // 单位
            UnitBase targetUnit = target.GetComponent<UnitBase>();
            if (targetUnit != null)
            {
                targetUnit.TakeDamage(damage, attackerId);
                return;
            }

            // 城堡
            CastleController targetCastle = target.GetComponent<CastleController>();
            if (targetCastle != null)
            {
                // 使用对建筑的伤害
                float buildingDamage = _unitBase.GetBuildingDamage();
                targetCastle.TakeDamage(buildingDamage);
            }
        }

        /// <summary>
        /// AOE范围伤害
        /// </summary>
        private void PerformAOEDamage(Vector3 center, float damage, int attackerId)
        {
            Collider[] hits = Physics.OverlapSphere(center, _unitBase.unitData.aoeRadius, enemyLayer);

            foreach (Collider hit in hits)
            {
                UnitBase enemy = hit.GetComponent<UnitBase>();
                if (enemy != null && enemy.OwnerId != attackerId && !enemy.IsDead)
                {
                    // AOE伤害减半
                    enemy.TakeDamage(damage, attackerId);
                }
            }
        }

        /// <summary>
        /// 尝试施加击晕
        /// </summary>
        private void TryApplyStun(Transform target)
        {
            if (Random.value <= _unitBase.unitData.stunChance)
            {
                UnitBase targetUnit = target.GetComponent<UnitBase>();
                if (targetUnit != null)
                {
                    targetUnit.ApplyStun(_unitBase.unitData.stunDuration);
                }
            }
        }

        #endregion

        #region 动画

        /// <summary>
        /// 播放攻击动画
        /// </summary>
        private void PlayAttackAnimation()
        {
            if (_animator != null)
            {
                _animator.SetTrigger("Attack");
            }
        }

        #endregion

        #region 工具方法

        /// <summary>
        /// 获取到目标的距离
        /// </summary>
        public float GetDistanceToTarget(Transform target)
        {
            if (target == null) return float.MaxValue;
            return Vector3.Distance(transform.position, target.position);
        }

        /// <summary>
        /// 设置敌人检测层级
        /// </summary>
        public void SetEnemyLayer(LayerMask layer)
        {
            enemyLayer = layer;
        }

        /// <summary>
        /// 重置攻击计时器
        /// </summary>
        public void ResetAttackTimer()
        {
            _lastAttackTime = -999f;
        }

        #endregion

        private void OnDrawGizmosSelected()
        {
            if (_unitBase?.unitData != null)
            {
                // 攻击范围
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position, _unitBase.unitData.attackRange);

                // AOE范围
                if (_unitBase.unitData.hasAOE)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawWireSphere(transform.position, _unitBase.unitData.aoeRadius);
                }
            }
        }
    }
}
