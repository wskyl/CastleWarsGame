using UnityEngine;
using CastleWars.Data;

namespace CastleWars.Core
{
    /// <summary>
    /// 本地单位状态枚举
    /// </summary>
    public enum LocalUnitState
    {
        Moving,
        Fighting,
        Dead
    }

    /// <summary>
    /// 本地单位 - 在本地模式下管理单位状态
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class LocalUnit : MonoBehaviour
    {
        [Header("单位配置")]
        public UnitData unitData;

        [Header("所属玩家")]
        public int OwnerId { get; private set; }

        [Header("状态")]
        private float currentHealth;
        private LocalUnitState currentState = LocalUnitState.Moving;
        private Transform currentTarget;

        // 组件
        private Rigidbody rb;

        // 属性
        public float CurrentHealth => currentHealth;
        public float MaxHealth => unitData != null ? unitData.maxHealth : 100f;
        public float AttackDamage => unitData != null ? unitData.attackDamage : 10f;
        public float AttackRange => unitData != null ? unitData.attackRange : 2f;
        public float MoveSpeed => unitData != null ? unitData.moveSpeed : 5f;
        public bool IsDead => currentHealth <= 0;

        // 攻击冷却
        private float attackCooldown = 0f;
        private float attackInterval => unitData != null ? unitData.attackSpeed : 1f;

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
            currentState = LocalUnitState.Moving;

            SetTeamColor();

            Debug.Log($"Unit spawned for player {ownerId}");
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
            if (!LocalGameMode.IsLocalMode) return;
            if (LocalGameMode.Instance == null || LocalGameMode.Instance.CurrentState != GameState.Playing) return;
            if (IsDead) return;

            if (attackCooldown > 0)
            {
                attackCooldown -= Time.deltaTime;
            }

            switch (currentState)
            {
                case LocalUnitState.Moving:
                    MoveForward();
                    SearchForTarget();
                    break;

                case LocalUnitState.Fighting:
                    if (currentTarget == null || !IsTargetValid())
                    {
                        currentState = LocalUnitState.Moving;
                        currentTarget = null;
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
            float direction = OwnerId == 1 ? 1f : -1f;
            Vector3 movement = new Vector3(direction * MoveSpeed * Time.deltaTime, 0, 0);
            transform.position += movement;
        }

        private void SearchForTarget()
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, AttackRange * 2f);

            float nearestDistance = float.MaxValue;
            Transform nearestTarget = null;

            foreach (Collider col in colliders)
            {
                LocalUnit enemyUnit = col.GetComponent<LocalUnit>();
                if (enemyUnit != null && enemyUnit.OwnerId != OwnerId && !enemyUnit.IsDead)
                {
                    float distance = Vector3.Distance(transform.position, col.transform.position);
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestTarget = col.transform;
                    }
                }

                LocalCastle enemyCastle = col.GetComponent<LocalCastle>();
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
                currentState = LocalUnitState.Fighting;
            }
        }

        private bool IsTargetValid()
        {
            if (currentTarget == null) return false;

            float distance = Vector3.Distance(transform.position, currentTarget.position);
            if (distance > AttackRange) return false;

            LocalUnit targetUnit = currentTarget.GetComponent<LocalUnit>();
            if (targetUnit != null) return !targetUnit.IsDead;

            LocalCastle targetCastle = currentTarget.GetComponent<LocalCastle>();
            if (targetCastle != null) return !targetCastle.IsDestroyed;

            return false;
        }

        private void AttackTarget()
        {
            if (attackCooldown > 0) return;

            LocalUnit targetUnit = currentTarget.GetComponent<LocalUnit>();
            if (targetUnit != null)
            {
                targetUnit.TakeDamage(AttackDamage, OwnerId);
                attackCooldown = attackInterval;
                return;
            }

            LocalCastle targetCastle = currentTarget.GetComponent<LocalCastle>();
            if (targetCastle != null)
            {
                targetCastle.TakeDamage(AttackDamage);
                attackCooldown = attackInterval;
            }
        }

        /// <summary>
        /// 受到伤害
        /// </summary>
        public void TakeDamage(float damage, int attackerId = 0)
        {
            if (IsDead) return;

            currentHealth = Mathf.Max(0, currentHealth - damage);

            PlayHitEffect();

            if (currentHealth <= 0)
            {
                Die(attackerId);
            }
        }

        private void PlayHitEffect()
        {
            // 受击效果
        }

        /// <summary>
        /// 单位死亡
        /// </summary>
        public void Die(int killerId = 0)
        {
            if (currentState == LocalUnitState.Dead) return;

            currentState = LocalUnitState.Dead;
            Debug.Log($"Unit (Player {OwnerId}) died!");

            PlayDeathEffect();

            Destroy(gameObject, 0.5f);
        }

        private void PlayDeathEffect()
        {
            // 死亡效果
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, AttackRange);
        }
    }
}
