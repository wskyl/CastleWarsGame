using UnityEngine;
using Unity.Netcode;
using CastleWars.Data;

namespace CastleWars.Units
{
    /// <summary>
    /// 单位基类 - 所有游戏单位的核心组件
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class UnitBase : NetworkBehaviour
    {
        [Header("单位配置")]
        public UnitData unitData;

        [Header("所属玩家")]
        public NetworkVariable<int> ownerId = new NetworkVariable<int>();

        [Header("当前状态")]
        private NetworkVariable<float> currentHealth = new NetworkVariable<float>();
        private UnitState currentState = UnitState.Moving;

        // 组件引用
        private Rigidbody rb;
        private UnitMovement movement;
        private UnitCombat combat;

        // 目标
        private Transform currentTarget;

        public float CurrentHealth => currentHealth.Value;
        public float MaxHealth => unitData.maxHealth;
        public bool IsDead => currentHealth.Value <= 0;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            movement = GetComponent<UnitMovement>();
            combat = GetComponent<UnitCombat>();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsServer)
            {
                currentHealth.Value = unitData.maxHealth;
            }
        }

        private void Update()
        {
            if (!IsOwner && !IsServer) return;

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

                case UnitState.Dead:
                    // 等待销毁
                    break;
            }
        }

        private void MoveForward()
        {
            if (movement != null)
            {
                // 根据所属玩家决定移动方向
                float direction = ownerId.Value == 1 ? 1f : -1f;
                movement.MoveInDirection(direction);
            }
        }

        private void SearchForTarget()
        {
            if (combat != null)
            {
                currentTarget = combat.FindNearestEnemy(ownerId.Value);
                if (currentTarget != null && IsTargetInRange())
                {
                    currentState = UnitState.Fighting;
                    movement?.Stop();
                }
            }
        }

        private bool IsTargetInRange()
        {
            if (currentTarget == null) return false;
            float distance = Vector3.Distance(transform.position, currentTarget.position);
            return distance <= unitData.attackRange;
        }

        private void AttackTarget()
        {
            if (combat != null)
            {
                combat.Attack(currentTarget);
            }
        }

        /// <summary>
        /// 受到伤害（服务器权威）
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void TakeDamageServerRpc(float damage, int attackerId)
        {
            if (IsDead) return;

            // 计算实际伤害（可以加入护甲减伤等）
            float actualDamage = CalculateDamage(damage);
            currentHealth.Value = Mathf.Max(0, currentHealth.Value - actualDamage);

            // 通知所有客户端播放受击效果
            PlayHitEffectClientRpc();

            if (currentHealth.Value <= 0)
            {
                Die(attackerId);
            }
        }

        private float CalculateDamage(float baseDamage)
        {
            // 这里可以添加护甲减伤、克制关系等计算
            return baseDamage;
        }

        [ClientRpc]
        private void PlayHitEffectClientRpc()
        {
            // 播放受击特效、音效
        }

        private void Die(int killerId)
        {
            currentState = UnitState.Dead;

            // 通知游戏管理器
            OnUnitDiedClientRpc(killerId);

            // 延迟销毁
            Destroy(gameObject, 2f);
        }

        [ClientRpc]
        private void OnUnitDiedClientRpc(int killerId)
        {
            // 播放死亡动画
            // 播放死亡音效
            // 更新击杀统计
        }

        private void OnDrawGizmosSelected()
        {
            if (unitData != null)
            {
                // 绘制攻击范围
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position, unitData.attackRange);
            }
        }
    }

    public enum UnitState
    {
        Moving,     // 移动中
        Fighting,   // 战斗中
        Dead        // 已死亡
    }
}
