using UnityEngine;
using Unity.Netcode;

namespace CastleWars.Core
{
    /// <summary>
    /// 城堡控制器 - 游戏的胜利目标
    /// 支持网络模式和本地模式
    /// </summary>
    public class CastleController : NetworkBehaviour
    {
        [Header("城堡属性")]
        [SerializeField] private float maxHealth = 5000f;

        [Header("所属玩家")]
        public int ownerId;

        // 网络模式血量
        private NetworkVariable<float> networkHealth = new NetworkVariable<float>();

        // 本地模式血量
        private float localHealth;

        // 判断是否为本地模式
        private bool IsLocalMode => LocalGameMode.IsLocalMode;

        public float CurrentHealth => IsLocalMode ? localHealth : networkHealth.Value;
        public float MaxHealth => maxHealth;
        public bool IsDestroyed => CurrentHealth <= 0;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsServer)
            {
                networkHealth.Value = maxHealth;
            }

            networkHealth.OnValueChanged += OnHealthChanged;
        }

        private void Start()
        {
            // 本地模式初始化
            if (IsLocalMode)
            {
                localHealth = maxHealth;
                Debug.Log($"Castle {ownerId} initialized in local mode with {localHealth} HP");
            }
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            networkHealth.OnValueChanged -= OnHealthChanged;
        }

        /// <summary>
        /// 受到伤害（本地模式直接调用）
        /// </summary>
        public void TakeDamage(float damage)
        {
            if (IsDestroyed) return;

            if (IsLocalMode)
            {
                // 本地模式
                float oldHealth = localHealth;
                localHealth = Mathf.Max(0, localHealth - damage);
                OnHealthChanged(oldHealth, localHealth);

                if (localHealth <= 0)
                {
                    OnCastleDestroyed();
                }
            }
            else
            {
                // 网络模式
                TakeDamageServerRpc(damage);
            }
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

        private void OnHealthChanged(float oldHealth, float newHealth)
        {
            // 更新UI显示血量
            Debug.Log($"Castle {ownerId} Health: {newHealth}/{maxHealth}");

            // 播放受击效果
            if (newHealth < oldHealth)
            {
                PlayHitEffect();
            }
        }

        private void OnCastleDestroyed()
        {
            Debug.Log($"Castle {ownerId} destroyed!");

            // 通知游戏管理器
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnCastleDestroyed(ownerId);
            }

            // 播放摧毁特效
            PlayDestroyEffectClientRpc();
        }

        [ClientRpc]
        private void PlayDestroyEffectClientRpc()
        {
            // 播放城堡摧毁动画
            // 播放摧毁音效
            // 可以添加爆炸特效
        }

        private void PlayHitEffect()
        {
            // 播放城堡受击特效
        }

        private void OnTriggerEnter(Collider other)
        {
            if (IsLocalMode)
            {
                // 本地模式：检测本地单位
                var localUnit = other.GetComponent<LocalUnit>();
                if (localUnit != null && localUnit.OwnerId != ownerId)
                {
                    TakeDamage(localUnit.AttackDamage);
                    localUnit.Die();
                    return;
                }
            }

            // 网络模式：敌方单位到达城堡，对城堡造成伤害
            var unit = other.GetComponent<Units.UnitBase>();
            if (unit != null && unit.ownerId.Value != ownerId)
            {
                if (IsServer)
                {
                    // 单位对城堡造成伤害
                    TakeDamageServerRpc(unit.unitData.attackDamage);

                    // 销毁单位
                    Destroy(unit.gameObject);
                }
            }
        }
    }
}
