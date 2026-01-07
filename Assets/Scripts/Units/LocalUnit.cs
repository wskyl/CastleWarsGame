using UnityEngine;
using CastleWars.Data;

namespace CastleWars.Core
{
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
        private UnitState currentState = UnitState.Moving;
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
            currentState = UnitState.Moving;

            // 设置颜色以区分阵营
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
            if (LocalGameMode.Instance.CurrentState != GameState.Playing) return;
            if (IsDead) return;

            // 更新攻击冷却
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

        private void MoveForward()
        {
            // 根据所属玩家决定移动方向
            float direction = OwnerId == 1 ? 1f : -1f;
            Vector3 movement = new Vector3(direction * MoveSpeed * Time.deltaTime, 0, 0);
            transform.position += movement;
        }

        private void SearchForTarget()
        {
            // 查找范围内的敌方单位
            Collider[] colliders = Physics.OverlapSphere(transform.position, AttackRange * 2f);

            float nearestDistance = float.MaxValue;
            Transform nearestTarget = null;

            foreach (Collider col in colliders)
            {
                // 检查敌方单位
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

                // 检查敌方城堡
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
                currentState = UnitState.Fighting;
            }
        }

        private bool IsTargetValid()
        {
            if (currentTarget == null) return false;

            // 检查距离
            float distance = Vector3.Distance(transform.position, currentTarget.position);
            if (distance > AttackRange) return false;

            // 检查目标是否还活着
            LocalUnit targetUnit = currentTarget.GetComponent<LocalUnit>();
            if (targetUnit != null) return !targetUnit.IsDead;

            LocalCastle targetCastle = currentTarget.GetComponent<LocalCastle>();
            if (targetCastle != null) return !targetCastle.IsDestroyed;

            return false;
        }

        private void AttackTarget()
        {
            if (attackCooldown > 0) return;

            // 攻击敌方单位
            LocalUnit targetUnit = currentTarget.GetComponent<LocalUnit>();
            if (targetUnit != null)
            {
                targetUnit.TakeDamage(AttackDamage, OwnerId);
                attackCooldown = attackInterval;
                return;
            }

            // 攻击敌方城堡
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

            // 播放受击效果
            PlayHitEffect();

            if (currentHealth <= 0)
            {
                Die(attackerId);
            }
        }

        private void PlayHitEffect()
        {
            // 简单的受击效果
        }

        /// <summary>
        /// 单位死亡
        /// </summary>
        public void Die(int killerId = 0)
        {
            if (currentState == UnitState.Dead) return;

            currentState = UnitState.Dead;
            Debug.Log($"Unit (Player {OwnerId}) died!");

            // 播放死亡效果
            PlayDeathEffect();

            // 延迟销毁
            Destroy(gameObject, 0.5f);
        }

        private void PlayDeathEffect()
        {
            // 死亡效果 - 可以添加粒子特效等
        }

        private void OnDrawGizmosSelected()
        {
            // 绘制攻击范围
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, AttackRange);
        }
    }
}
