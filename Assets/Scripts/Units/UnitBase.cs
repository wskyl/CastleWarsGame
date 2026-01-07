using UnityEngine;
using CastleWars.Data;
using CastleWars.Core;

#if UNITY_NETCODE
using Unity.Netcode;
#endif

namespace CastleWars.Units
{
    /// <summary>
    /// 单位基类 - 所有游戏单位的核心组件
    /// 支持本地模式和网络模式
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
#if UNITY_NETCODE
    public class UnitBase : NetworkBehaviour
#else
    public class UnitBase : MonoBehaviour
#endif
    {
        [Header("单位配置")]
        public UnitData unitData;

        [Header("所属玩家")]
#if UNITY_NETCODE
        public NetworkVariable<int> ownerId = new NetworkVariable<int>();
#else
        private int _ownerId;
        public int OwnerIdValue
        {
            get => _ownerId;
            set => _ownerId = value;
        }
#endif

        [Header("当前状态")]
#if UNITY_NETCODE
        private NetworkVariable<float> networkHealth = new NetworkVariable<float>();
#endif
        private float localHealth;
        private UnitState currentState = UnitState.Moving;

        // 组件引用
        private Rigidbody rb;
        private UnitMovement movement;
        private UnitCombat combat;

        // 目标
        private Transform currentTarget;

        // 本地模式判断
        private bool IsLocalMode => LocalGameMode.IsLocalMode;

#if UNITY_NETCODE
        public float CurrentHealth => IsLocalMode ? localHealth : networkHealth.Value;
#else
        public float CurrentHealth => localHealth;
#endif
        public float MaxHealth => unitData != null ? unitData.maxHealth : 100f;
        public bool IsDead => CurrentHealth <= 0;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            movement = GetComponent<UnitMovement>();
            combat = GetComponent<UnitCombat>();
        }

#if UNITY_NETCODE
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsServer && unitData != null)
            {
                networkHealth.Value = unitData.maxHealth;
            }
        }
#endif

        private void Start()
        {
            // 本地模式初始化
            if (IsLocalMode && unitData != null)
            {
                localHealth = unitData.maxHealth;
            }
        }

        private void Update()
        {
            if (IsLocalMode)
            {
                UpdateLocalMode();
            }
#if UNITY_NETCODE
            else
            {
                UpdateNetworkMode();
            }
#endif
        }

        private void UpdateLocalMode()
        {
            if (IsDead) return;

            switch (currentState)
            {
                case UnitState.Moving:
                    MoveForwardLocal();
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
                    break;
            }
        }

#if UNITY_NETCODE
        private void UpdateNetworkMode()
        {
            if (!IsOwner && !IsServer) return;

            switch (currentState)
            {
                case UnitState.Moving:
                    MoveForwardNetwork();
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
                    break;
            }
        }
#endif

        private void MoveForwardLocal()
        {
            if (unitData == null) return;

#if UNITY_NETCODE
            float direction = ownerId.Value == 1 ? 1f : -1f;
#else
            float direction = _ownerId == 1 ? 1f : -1f;
#endif

            if (movement != null)
            {
                movement.MoveInDirection(direction);
            }
            else
            {
                // 简单移动
                transform.position += new Vector3(direction * unitData.moveSpeed * Time.deltaTime, 0, 0);
            }
        }

#if UNITY_NETCODE
        private void MoveForwardNetwork()
        {
            if (movement != null && unitData != null)
            {
                float direction = ownerId.Value == 1 ? 1f : -1f;
                movement.MoveInDirection(direction);
            }
        }
#endif

        private void SearchForTarget()
        {
            if (combat != null)
            {
#if UNITY_NETCODE
                currentTarget = combat.FindNearestEnemy(ownerId.Value);
#else
                currentTarget = combat.FindNearestEnemy(_ownerId);
#endif
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
            if (combat != null)
            {
                combat.Attack(currentTarget);
            }
        }

        /// <summary>
        /// 受到伤害（本地模式直接调用）
        /// </summary>
        public void TakeDamage(float damage, int attackerId = 0)
        {
            if (IsDead) return;

            if (IsLocalMode)
            {
                float actualDamage = CalculateDamage(damage);
                localHealth = Mathf.Max(0, localHealth - actualDamage);
                PlayHitEffect();

                if (localHealth <= 0)
                {
                    Die(attackerId);
                }
            }
#if UNITY_NETCODE
            else
            {
                TakeDamageServerRpc(damage, attackerId);
            }
#endif
        }

#if UNITY_NETCODE
        /// <summary>
        /// 受到伤害（服务器权威）
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void TakeDamageServerRpc(float damage, int attackerId)
        {
            if (IsDead) return;

            float actualDamage = CalculateDamage(damage);
            networkHealth.Value = Mathf.Max(0, networkHealth.Value - actualDamage);

            PlayHitEffectClientRpc();

            if (networkHealth.Value <= 0)
            {
                Die(attackerId);
            }
        }

        [ClientRpc]
        private void PlayHitEffectClientRpc()
        {
            PlayHitEffect();
        }

        [ClientRpc]
        private void OnUnitDiedClientRpc(int killerId)
        {
            // 播放死亡动画和音效
        }
#endif

        private float CalculateDamage(float baseDamage)
        {
            return baseDamage;
        }

        private void PlayHitEffect()
        {
            // 播放受击特效、音效
        }

        private void Die(int killerId)
        {
            currentState = UnitState.Dead;

#if UNITY_NETCODE
            if (!IsLocalMode)
            {
                OnUnitDiedClientRpc(killerId);
            }
#endif

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

    public enum UnitState
    {
        Moving,
        Fighting,
        Dead
    }
}
