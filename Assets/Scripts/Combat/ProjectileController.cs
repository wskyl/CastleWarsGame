using UnityEngine;
using CastleWars.Units;

namespace CastleWars.Combat
{
    /// <summary>
    /// 投射物控制器 - 处理远程攻击的子弹/箭矢
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class ProjectileController : MonoBehaviour
    {
        [Header("投射物设置")]
        [SerializeField] private float speed = 10f;
        [SerializeField] private float lifetime = 5f;
        [SerializeField] private GameObject hitEffectPrefab;

        private Transform target;
        private float damage;
        private int attackerId;
        private bool hasAOE;
        private float aoeRadius;
        private Rigidbody rb;

        private bool isInitialized = false;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
        }

        /// <summary>
        /// 初始化投射物
        /// </summary>
        public void Initialize(Transform target, float damage, int attackerId, bool hasAOE = false, float aoeRadius = 0f)
        {
            this.target = target;
            this.damage = damage;
            this.attackerId = attackerId;
            this.hasAOE = hasAOE;
            this.aoeRadius = aoeRadius;
            this.isInitialized = true;

            // 设置初始方向
            if (target != null)
            {
                Vector3 direction = (target.position - transform.position).normalized;
                rb.velocity = direction * speed;
                transform.rotation = Quaternion.LookRotation(direction);
            }

            // 自动销毁
            Destroy(gameObject, lifetime);
        }

        private void FixedUpdate()
        {
            if (!isInitialized) return;

            // 追踪目标（导弹效果）
            if (target != null)
            {
                Vector3 direction = (target.position - transform.position).normalized;
                rb.velocity = direction * speed;
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // 检查是否击中单位
            UnitBase unit = other.GetComponent<UnitBase>();
            if (unit != null)
            {
                // 确保不会击中友军
                if (unit.ownerId.Value == attackerId)
                {
                    return;
                }

                // 造成伤害
                unit.TakeDamageServerRpc(damage, attackerId);

                // AOE伤害
                if (hasAOE)
                {
                    PerformAOEDamage(transform.position);
                }

                // 播放击中效果
                SpawnHitEffect();

                // 销毁投射物
                Destroy(gameObject);
            }
        }

        private void PerformAOEDamage(Vector3 center)
        {
            Collider[] hits = Physics.OverlapSphere(center, aoeRadius);

            foreach (Collider hit in hits)
            {
                UnitBase enemy = hit.GetComponent<UnitBase>();
                if (enemy != null && enemy.ownerId.Value != attackerId)
                {
                    // AOE伤害是主伤害的50%
                    enemy.TakeDamageServerRpc(damage * 0.5f, attackerId);
                }
            }
        }

        private void SpawnHitEffect()
        {
            if (hitEffectPrefab != null)
            {
                GameObject effect = Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
                Destroy(effect, 2f);
            }
        }
    }
}
