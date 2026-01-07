using UnityEngine;
using CastleWars.Data;

namespace CastleWars.Core
{
    /// <summary>
    /// 单位状态枚举
    /// </summary>
    public enum UnitState
    {
        Moving,
        Fighting,
        Dead
    }

    /// <summary>
    /// 单位控制器 - 管理单位行为（纯本地模式）
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class UnitController : MonoBehaviour
    {
        [Header("单位配置")]
        public UnitData unitData;

        [Header("所属玩家")]
        public int OwnerId { get; private set; }

        // 状态
        private float currentHealth;
        private UnitState currentState = UnitState.Moving;
        private Transform currentTarget;

        // 组件
        private Rigidbody rb;

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

        // 事件
        public System.Action<float, float> OnHealthChanged;
        public System.Action OnDeath;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.useGravity = false;
                rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
            }
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

            SetTeamColor();

            Debug.Log($"[UnitController] Unit spawned for player {ownerId}");
        }

        private void SetTeamColor()
        {
            Renderer renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = OwnerId == 1 ? Color.blue : Color.red;
            }
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
                    if (currentTarget == null || !IsTargetValid())
                    {
                        currentState = UnitState.Moving;
                        currentTarget = null;
                    }
                    else
                    {
                        AttackTarget();
                    }
                    break;
            }
        }

        /// <summary>
        /// 向前移动
        /// </summary>
        private void MoveForward()
        {
            float direction = OwnerId == 1 ? 1f : -1f;
            Vector3 movement = new Vector3(direction * MoveSpeed * Time.deltaTime, 0, 0);
            transform.position += movement;
        }

        /// <summary>
        /// 搜索目标
        /// </summary>
        private void SearchForTarget()
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, AttackRange * 2f);

            float nearestDistance = float.MaxValue;
            Transform nearestTarget = null;

            foreach (Collider col in colliders)
            {
                // 检查敌方单位
                UnitController enemyUnit = col.GetComponent<UnitController>();
                if (enemyUnit != null && enemyUnit.OwnerId != OwnerId && !enemyUnit.IsDead)
                {
                    float distance = Vector3.Distance(transform.position, col.transform.position);
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestTarget = col.transform;
                    }
                }

                // 检查敌方城堡
                CastleController enemyCastle = col.GetComponent<CastleController>();
                if (enemyCastle != null && enemyCastle.OwnerId != OwnerId && !enemyCastle.IsDestroyed)
                {
                    float distance = Vector3.Distance(transform.position, col.transform.position);
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestTarget = col.transform;
                    }
                }
            }

            if (nearestTarget != null && nearestDistance <= AttackRange)
            {
                currentTarget = nearestTarget;
                currentState = UnitState.Fighting;
            }
        }

        /// <summary>
        /// 检查目标是否有效
        /// </summary>
        private bool IsTargetValid()
        {
            if (currentTarget == null) return false;

            float distance = Vector3.Distance(transform.position, currentTarget.position);
            if (distance > AttackRange) return false;

            UnitController targetUnit = currentTarget.GetComponent<UnitController>();
            if (targetUnit != null) return !targetUnit.IsDead;

            CastleController targetCastle = currentTarget.GetComponent<CastleController>();
            if (targetCastle != null) return !targetCastle.IsDestroyed;

            return false;
        }

        /// <summary>
        /// 攻击目标
        /// </summary>
        private void AttackTarget()
        {
            if (attackCooldown > 0) return;

            UnitController targetUnit = currentTarget.GetComponent<UnitController>();
            if (targetUnit != null)
            {
                targetUnit.TakeDamage(AttackDamage, OwnerId);
                attackCooldown = AttackSpeed;
                return;
            }

            CastleController targetCastle = currentTarget.GetComponent<CastleController>();
            if (targetCastle != null)
            {
                targetCastle.TakeDamage(AttackDamage);
                attackCooldown = AttackSpeed;
            }
        }

        /// <summary>
        /// 受到伤害
        /// </summary>
        public void TakeDamage(float damage, int attackerId = 0)
        {
            if (IsDead) return;

            float oldHealth = currentHealth;
            currentHealth = Mathf.Max(0, currentHealth - damage);

            OnHealthChanged?.Invoke(oldHealth, currentHealth);
            PlayHitEffect();

            if (currentHealth <= 0)
            {
                Die(attackerId);
            }
        }

        /// <summary>
        /// 播放受击特效
        /// </summary>
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
            OnDeath?.Invoke();

            Debug.Log($"[UnitController] Unit (Player {OwnerId}) died!");

            PlayDeathEffect();
            Destroy(gameObject, 0.5f);
        }

        /// <summary>
        /// 播放死亡特效
        /// </summary>
        private void PlayDeathEffect()
        {
            // TODO: 实现死亡特效
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, AttackRange);
        }
    }
}
