using UnityEngine;

namespace CastleWars.Core
{
    /// <summary>
    /// 城堡控制器 - 游戏的胜利目标（纯本地模式）
    /// </summary>
    public class CastleController : MonoBehaviour
    {
        [Header("城堡属性")]
        [SerializeField] private float maxHealth = 5000f;

        [Header("所属玩家")]
        public int OwnerId { get; private set; }

        // 当前血量
        private float currentHealth;

        // 属性
        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public bool IsDestroyed => currentHealth <= 0;

        // 事件
        public System.Action<float, float> OnHealthChanged;
        public System.Action OnDestroyed;

        /// <summary>
        /// 初始化城堡
        /// </summary>
        public void Initialize(int ownerId)
        {
            OwnerId = ownerId;
            currentHealth = maxHealth;
            Debug.Log($"[CastleController] Castle {ownerId} initialized with {currentHealth} HP");
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
            Debug.Log($"[CastleController] Castle {OwnerId} HP: {currentHealth:F0}/{maxHealth}");

            PlayHitEffect();

            if (currentHealth <= 0)
            {
                OnCastleDestroyed();
            }
        }

        /// <summary>
        /// 城堡被摧毁
        /// </summary>
        private void OnCastleDestroyed()
        {
            Debug.Log($"[CastleController] Castle {OwnerId} destroyed!");

            OnDestroyed?.Invoke();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnCastleDestroyed(OwnerId);
            }

            PlayDestroyEffect();
        }

        /// <summary>
        /// 播放受击特效
        /// </summary>
        private void PlayHitEffect()
        {
            // TODO: 实现受击特效
        }

        /// <summary>
        /// 播放摧毁特效
        /// </summary>
        private void PlayDestroyEffect()
        {
            // TODO: 实现摧毁特效
        }

        /// <summary>
        /// 单位触碰城堡时的处理
        /// </summary>
        private void OnTriggerEnter(Collider other)
        {
            UnitController unit = other.GetComponent<UnitController>();
            if (unit != null && unit.OwnerId != OwnerId && !unit.IsDead)
            {
                // 敌方单位攻击城堡
                TakeDamage(unit.AttackDamage);
                unit.Die();
            }
        }
    }
}
