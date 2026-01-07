using UnityEngine;
using System;

namespace CastleWars.Core
{
    /// <summary>
    /// 本地城堡 - 在本地模式下管理城堡状态
    /// </summary>
    public class LocalCastle : MonoBehaviour
    {
        [Header("城堡属性")]
        [SerializeField] private float maxHealth = 5000f;

        [Header("所属玩家")]
        public int OwnerId { get; private set; }

        private float currentHealth;

        // 事件
        public event Action<float, float> OnHealthChanged; // (oldHealth, newHealth)
        public event Action OnDestroyed;

        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public float HealthPercentage => currentHealth / maxHealth;
        public bool IsDestroyed => currentHealth <= 0;

        /// <summary>
        /// 初始化城堡
        /// </summary>
        public void Initialize(int ownerId, float customMaxHealth = 0)
        {
            OwnerId = ownerId;

            if (customMaxHealth > 0)
            {
                maxHealth = customMaxHealth;
            }

            currentHealth = maxHealth;

            // 设置标签便于识别
            gameObject.tag = "Castle";
            gameObject.name = $"Castle_Player{ownerId}";

            Debug.Log($"Castle initialized for player {ownerId}, HP: {currentHealth}/{maxHealth}");
        }

        /// <summary>
        /// 受到伤害
        /// </summary>
        public void TakeDamage(float damage)
        {
            if (IsDestroyed) return;

            float oldHealth = currentHealth;
            currentHealth = Mathf.Max(0, currentHealth - damage);

            OnHealthChanged?.Invoke(oldHealth, currentHealth);
            Debug.Log($"Castle {OwnerId} took {damage} damage. HP: {currentHealth}/{maxHealth}");

            // 播放受击效果
            PlayHitEffect();

            if (currentHealth <= 0)
            {
                OnCastleDestroyed();
            }
        }

        /// <summary>
        /// 治疗城堡
        /// </summary>
        public void Heal(float amount)
        {
            if (IsDestroyed) return;

            float oldHealth = currentHealth;
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);

            OnHealthChanged?.Invoke(oldHealth, currentHealth);
            Debug.Log($"Castle {OwnerId} healed {amount}. HP: {currentHealth}/{maxHealth}");
        }

        private void OnCastleDestroyed()
        {
            Debug.Log($"Castle {OwnerId} destroyed!");

            OnDestroyed?.Invoke();

            // 通知游戏管理器
            if (LocalGameMode.Instance != null)
            {
                LocalGameMode.Instance.OnCastleDestroyed(OwnerId);
            }

            // 播放摧毁效果
            PlayDestroyEffect();
        }

        private void PlayHitEffect()
        {
            // 简单的受击效果 - 可以扩展为粒子特效等
            StartCoroutine(FlashEffect());
        }

        private System.Collections.IEnumerator FlashEffect()
        {
            Renderer renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                Color originalColor = renderer.material.color;
                renderer.material.color = Color.red;
                yield return new WaitForSeconds(0.1f);
                renderer.material.color = originalColor;
            }
        }

        private void PlayDestroyEffect()
        {
            // 摧毁效果 - 实际项目中可添加粒子特效、音效等
            // 可以选择隐藏或销毁城堡对象
        }

        private void OnTriggerEnter(Collider other)
        {
            // 检测敌方单位进入
            LocalUnit unit = other.GetComponent<LocalUnit>();
            if (unit != null && unit.OwnerId != OwnerId)
            {
                // 单位对城堡造成伤害
                TakeDamage(unit.AttackDamage);

                // 销毁单位
                unit.Die();
            }
        }
    }
}
