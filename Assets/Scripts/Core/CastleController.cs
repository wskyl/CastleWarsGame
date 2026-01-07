using UnityEngine;

#if UNITY_NETCODE
using Unity.Netcode;
#endif

namespace CastleWars.Core
{
    /// <summary>
    /// 城堡控制器 - 游戏的胜利目标
    /// 支持网络模式和本地模式
    /// </summary>
#if UNITY_NETCODE
    public class CastleController : NetworkBehaviour
#else
    public class CastleController : MonoBehaviour
#endif
    {
        [Header("城堡属性")]
        [SerializeField] private float maxHealth = 5000f;

        [Header("所属玩家")]
        public int ownerId;

#if UNITY_NETCODE
        // 网络模式血量
        private NetworkVariable<float> networkHealth = new NetworkVariable<float>();
#endif

        // 本地模式血量
        private float localHealth;

        // 判断是否为本地模式
        private bool IsLocalMode => LocalGameMode.IsLocalMode;

#if UNITY_NETCODE
        public float CurrentHealth => IsLocalMode ? localHealth : networkHealth.Value;
#else
        public float CurrentHealth => localHealth;
#endif
        public float MaxHealth => maxHealth;
        public bool IsDestroyed => CurrentHealth <= 0;

#if UNITY_NETCODE
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsServer)
            {
                networkHealth.Value = maxHealth;
            }

            networkHealth.OnValueChanged += OnHealthChanged;
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            networkHealth.OnValueChanged -= OnHealthChanged;
        }

        /// <summary>
        /// 受到伤害（网络模式）
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void TakeDamageServerRpc(float damage)
        {
            if (IsDestroyed) return;

            networkHealth.Value = Mathf.Max(0, networkHealth.Value - damage);

            if (networkHealth.Value <= 0)
            {
                OnCastleDestroyed();
            }
        }

        [ClientRpc]
        private void PlayDestroyEffectClientRpc()
        {
            // 播放城堡摧毁动画和音效
        }
#endif

        private void Start()
        {
            // 本地模式初始化
            if (IsLocalMode)
            {
                localHealth = maxHealth;
                Debug.Log($"Castle {ownerId} initialized in local mode with {localHealth} HP");
            }
        }

        /// <summary>
        /// 受到伤害（本地模式直接调用）
        /// </summary>
        public void TakeDamage(float damage)
        {
            if (IsDestroyed) return;

            if (IsLocalMode)
            {
                float oldHealth = localHealth;
                localHealth = Mathf.Max(0, localHealth - damage);
                OnHealthChanged(oldHealth, localHealth);

                if (localHealth <= 0)
                {
                    OnCastleDestroyed();
                }
            }
#if UNITY_NETCODE
            else
            {
                TakeDamageServerRpc(damage);
            }
#endif
        }

        private void OnHealthChanged(float oldHealth, float newHealth)
        {
            Debug.Log($"Castle {ownerId} Health: {newHealth}/{maxHealth}");

            if (newHealth < oldHealth)
            {
                PlayHitEffect();
            }
        }

        private void OnCastleDestroyed()
        {
            Debug.Log($"Castle {ownerId} destroyed!");

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnCastleDestroyed(ownerId);
            }

#if UNITY_NETCODE
            if (!IsLocalMode)
            {
                PlayDestroyEffectClientRpc();
            }
#endif
        }

        private void PlayHitEffect()
        {
            // 播放城堡受击特效
        }

        private void OnTriggerEnter(Collider other)
        {
            if (IsLocalMode)
            {
                var localUnit = other.GetComponent<LocalUnit>();
                if (localUnit != null && localUnit.OwnerId != ownerId)
                {
                    TakeDamage(localUnit.AttackDamage);
                    localUnit.Die();
                    return;
                }
            }

#if UNITY_NETCODE
            var unit = other.GetComponent<Units.UnitBase>();
            if (unit != null && unit.ownerId.Value != ownerId)
            {
                if (IsServer)
                {
                    TakeDamageServerRpc(unit.unitData.attackDamage);
                    Destroy(unit.gameObject);
                }
            }
#endif
        }
    }
}
