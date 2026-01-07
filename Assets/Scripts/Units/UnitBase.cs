using UnityEngine;
using CastleWars.Data;
using CastleWars.Core;

namespace CastleWars.Units
{
    /// <summary>
    /// 单位基类 - 所有游戏单位的核心组件（纯本地模式）
    /// 注意：推荐使用 CastleWars.Core.UnitController 作为主要单位控制器
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class UnitBase : MonoBehaviour
    {
        [Header("单位配置")]
        public UnitData unitData;

        [Header("所属玩家")]
        public int OwnerId { get; private set; }

        // 状态
        private float currentHealth;
        private UnitState currentState = UnitState.Moving;

        // 组件引用
        private Rigidbody rb;
        private UnitMovement movement;
        private UnitCombat combat;

        // 目标
        private Transform currentTarget;

        // 攻击冷却
        private float attackCooldown = 0f;

        // 属性
        public float CurrentHealth => currentHealth;
        public float MaxHealth => unitData != null ? unitData.maxHealth : 100f;
        public float AttackDamage => unitData != null ? unitData.attackDamage : 10f;
        public float AttackRange => unitData != null ? unitData.attackRange : 2f;
        public float MoveSpeed => unitData != null ? unitData.moveSpeed : 5f;
        public float AttackSpeed => unitData != null ? unitData.attackSpeed : 1f;
        public bool IsDead => currentHealth <= 0;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            movement = GetComponent<UnitMovement>();
            combat = GetComponent<UnitCombat>();
        }

        /// <summary>
        /// 初始化单位
        /// </summary>
        public void Initialize(int ownerId, UnitData data = null)
        {
            OwnerId = ownerId;
            if (data != null)
            {
                unitData = data;
            }
            currentHealth = MaxHealth;
            currentState = UnitState.Moving;

            Debug.Log($"[UnitBase] Unit initialized for player {ownerId}");
        }

        private void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Playing)
                return;

            if (IsDead) return;

            if (attackCooldown > 0)
            {
                attackCooldown -= Time.deltaTime;
            }

            switch (currentState)
            {
                case UnitState.Moving:
                    MoveForward();
                    SearchForTarget();
                    break;

                case UnitState.Fighting:
                    if (currentTarget == null || !IsTargetInRange())
                    {
                        currentState = UnitState.Moving;
                    }
                    else
                    {
                        AttackTarget();
                    }
                    break;
            }
        }

        private void MoveForward()
        {
            if (unitData == null) return;

            float direction = OwnerId == 1 ? 1f : -1f;

            if (movement != null)
            {
                movement.MoveInDirection(direction);
            }
            else
            {
                transform.position += new Vector3(direction * MoveSpeed * Time.deltaTime, 0, 0);
            }
        }

        private void SearchForTarget()
        {
            if (combat != null)
            {
                currentTarget = combat.FindNearestEnemy(OwnerId);
                if (currentTarget != null && IsTargetInRange())
                {
                    currentState = UnitState.Fighting;
                    movement?.Stop();
                }
            }
        }

        private bool IsTargetInRange()
        {
            if (currentTarget == null || unitData == null) return false;
            float distance = Vector3.Distance(transform.position, currentTarget.position);
            return distance <= unitData.attackRange;
        }

        private void AttackTarget()
        {
            if (attackCooldown > 0) return;

            if (combat != null)
            {
                combat.Attack(currentTarget);
                attackCooldown = AttackSpeed;
            }
        }

        /// <summary>
        /// 受到伤害
        /// </summary>
        public void TakeDamage(float damage, int attackerId = 0)
        {
            if (IsDead) return;

            float actualDamage = CalculateDamage(damage);
            currentHealth = Mathf.Max(0, currentHealth - actualDamage);
            PlayHitEffect();

            if (currentHealth <= 0)
            {
                Die(attackerId);
            }
        }

        private float CalculateDamage(float baseDamage)
        {
            return baseDamage;
        }

        private void PlayHitEffect()
        {
            // TODO: 实现受击特效
        }

        /// <summary>
        /// 单位死亡
        /// </summary>
        public void Die(int killerId = 0)
        {
            if (currentState == UnitState.Dead) return;

            currentState = UnitState.Dead;
            Debug.Log($"[UnitBase] Unit (Player {OwnerId}) died!");

            Destroy(gameObject, 0.5f);
        }

        private void OnDrawGizmosSelected()
        {
            if (unitData != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position, unitData.attackRange);
            }
        }
    }

    /// <summary>
    /// 单位状态枚举
    /// </summary>
    public enum UnitState
    {
        Moving,
        Fighting,
        Dead
    }
}
