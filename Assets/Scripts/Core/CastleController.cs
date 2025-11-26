using UnityEngine;
using Unity.Netcode;

namespace CastleWars.Core
{
    /// <summary>
    /// 城堡控制器 - 游戏的胜利目标
    /// </summary>
    public class CastleController : NetworkBehaviour
    {
        [Header("城堡属性")]
        [SerializeField] private float maxHealth = 5000f;

        [Header("所属玩家")]
        public int ownerId;

        private NetworkVariable<float> currentHealth = new NetworkVariable<float>();

        public float CurrentHealth => currentHealth.Value;
        public float MaxHealth => maxHealth;
        public bool IsDestroyed => currentHealth.Value <= 0;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsServer)
            {
                currentHealth.Value = maxHealth;
            }

            currentHealth.OnValueChanged += OnHealthChanged;
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            currentHealth.OnValueChanged -= OnHealthChanged;
        }

        /// <summary>
        /// 受到伤害
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void TakeDamageServerRpc(float damage)
        {
            if (IsDestroyed) return;

            currentHealth.Value = Mathf.Max(0, currentHealth.Value - damage);

            if (currentHealth.Value <= 0)
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
            // 敌方单位到达城堡，对城堡造成伤害
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
