using UnityEngine;
using System;
using Unity.Netcode;
using CastleWars.Units;

namespace CastleWars.Core
{
    /// <summary>
    /// 城堡控制器
    /// 管理城堡血量、受击判定、摧毁检测
    /// </summary>
    public class CastleController : NetworkBehaviour
    {
        [Header("城堡属性")]
        [SerializeField] private float maxHealth = 5000f;

        [Header("特效")]
        [SerializeField] private GameObject hitEffectPrefab;
        [SerializeField] private GameObject destroyEffectPrefab;

        [Header("音效")]
        [SerializeField] private AudioClip hitSound;
        [SerializeField] private AudioClip destroySound;

        // 网络同步变量
        private NetworkVariable<float> _currentHealth = new NetworkVariable<float>(
            5000f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        private NetworkVariable<int> _ownerId = new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        // 事件
        public event Action<float, float> OnHealthChanged; // currentHealth, maxHealth
        public event Action OnCastleDestroyed;

        // 属性
        public float CurrentHealth => _currentHealth.Value;
        public float MaxHealth => maxHealth;
        public int OwnerId => _ownerId.Value;
        public bool IsDestroyed => _currentHealth.Value <= 0;
        public float HealthPercentage => _currentHealth.Value / maxHealth;

        // 组件
        private AudioSource _audioSource;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            _currentHealth.OnValueChanged += HandleHealthChanged;
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            _currentHealth.OnValueChanged -= HandleHealthChanged;
        }

        /// <summary>
        /// 初始化城堡
        /// </summary>
        public void Initialize(int ownerId, float health)
        {
            if (!IsServer) return;

            _ownerId.Value = ownerId;
            maxHealth = health;
            _currentHealth.Value = health;

            Debug.Log($"[Castle] 城堡初始化 - 所有者: Player {ownerId}, 血量: {health}");
        }

        #region 伤害处理

        /// <summary>
        /// 受到伤害（ServerRpc）
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void TakeDamageServerRpc(float damage, ServerRpcParams rpcParams = default)
        {
            ApplyDamage(damage);
        }

        /// <summary>
        /// 直接受到伤害（仅服务器调用）
        /// </summary>
        public void TakeDamage(float damage)
        {
            if (!IsServer) return;
            ApplyDamage(damage);
        }

        private void ApplyDamage(float damage)
        {
            if (IsDestroyed || damage <= 0) return;

            float newHealth = Mathf.Max(0, _currentHealth.Value - damage);
            _currentHealth.Value = newHealth;

            Debug.Log($"[Castle] Player {_ownerId.Value} 城堡受到 {damage} 伤害, 剩余血量: {newHealth}");

            // 播放受击特效
            PlayHitEffectClientRpc();

            // 检查是否被摧毁
            if (newHealth <= 0)
            {
                HandleDestruction();
            }
        }

        private void HandleDestruction()
        {
            Debug.Log($"[Castle] Player {_ownerId.Value} 城堡被摧毁!");

            // 通知GameManager
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnCastleDestroyed(_ownerId.Value);
            }

            // 播放摧毁特效
            PlayDestroyEffectClientRpc();
        }

        #endregion

        #region 特效和音效

        [ClientRpc]
        private void PlayHitEffectClientRpc()
        {
            // 播放受击特效
            if (hitEffectPrefab != null)
            {
                Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
            }

            // 播放受击音效
            if (hitSound != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(hitSound);
            }

            OnHealthChanged?.Invoke(_currentHealth.Value, maxHealth);
        }

        [ClientRpc]
        private void PlayDestroyEffectClientRpc()
        {
            // 播放摧毁特效
            if (destroyEffectPrefab != null)
            {
                Instantiate(destroyEffectPrefab, transform.position, Quaternion.identity);
            }

            // 播放摧毁音效
            if (destroySound != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(destroySound);
            }

            OnCastleDestroyed?.Invoke();
        }

        #endregion

        #region 事件处理

        private void HandleHealthChanged(float previousValue, float newValue)
        {
            OnHealthChanged?.Invoke(newValue, maxHealth);

            // 血量警告
            if (newValue <= maxHealth * 0.25f && previousValue > maxHealth * 0.25f)
            {
                Debug.LogWarning($"[Castle] Player {_ownerId.Value} 城堡血量低于25%!");
            }
        }

        #endregion

        #region 碰撞检测

        private void OnTriggerEnter(Collider other)
        {
            if (!IsServer) return;

            // 检测敌方单位
            UnitBase unit = other.GetComponent<UnitBase>();
            if (unit != null && unit.OwnerId != _ownerId.Value)
            {
                // 单位对城堡造成伤害
                float damage = unit.GetBuildingDamage();
                TakeDamage(damage);

                // 单位自毁（撞击城堡后死亡）
                unit.Die();
            }
        }

        #endregion

        #region 工具方法

        /// <summary>
        /// 治疗城堡
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void HealServerRpc(float amount)
        {
            if (IsDestroyed || amount <= 0) return;

            float newHealth = Mathf.Min(maxHealth, _currentHealth.Value + amount);
            _currentHealth.Value = newHealth;

            Debug.Log($"[Castle] Player {_ownerId.Value} 城堡恢复 {amount} 血量, 当前血量: {newHealth}");
        }

        /// <summary>
        /// 获取血量比例
        /// </summary>
        public float GetHealthRatio()
        {
            return _currentHealth.Value / maxHealth;
        }

        /// <summary>
        /// 检查城堡是否属于指定玩家
        /// </summary>
        public bool BelongsTo(int playerId)
        {
            return _ownerId.Value == playerId;
        }

        #endregion
    }
}
