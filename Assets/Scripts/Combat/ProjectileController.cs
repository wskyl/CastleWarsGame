using UnityEngine;
using CastleWars.Units;
using CastleWars.Core;

namespace CastleWars.Combat
{
    /// <summary>
    /// 投射物控制器 - 处理远程攻击的子弹/箭矢
    /// 支持本地模式和网络模式
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

            if (target != null)
            {
                Vector3 direction = (target.position - transform.position).normalized;
                rb.velocity = direction * speed;
                transform.rotation = Quaternion.LookRotation(direction);
            }

            Destroy(gameObject, lifetime);
        }

        private void FixedUpdate()
        {
            if (!isInitialized) return;

            if (target != null)
            {
                Vector3 direction = (target.position - transform.position).normalized;
                rb.velocity = direction * speed;
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // 本地模式：检查LocalUnit
            if (LocalGameMode.IsLocalMode)
            {
                LocalUnit localUnit = other.GetComponent<LocalUnit>();
                if (localUnit != null)
                {
                    if (localUnit.OwnerId == attackerId)
                    {
                        return;
                    }

                    localUnit.TakeDamage(damage, attackerId);

                    if (hasAOE)
                    {
                        PerformAOEDamageLocal(transform.position);
                    }

                    SpawnHitEffect();
                    Destroy(gameObject);
                    return;
                }
            }

            // 网络模式或通用模式：检查UnitBase
            UnitBase unit = other.GetComponent<UnitBase>();
            if (unit != null)
            {
                int unitOwnerId = GetUnitOwnerId(unit);
                if (unitOwnerId == attackerId)
                {
                    return;
                }

                unit.TakeDamage(damage, attackerId);

                if (hasAOE)
                {
                    PerformAOEDamage(transform.position);
                }

                SpawnHitEffect();
                Destroy(gameObject);
            }
        }

        private int GetUnitOwnerId(UnitBase unit)
        {
#if UNITY_NETCODE
            return unit.ownerId.Value;
#else
            return unit.OwnerIdValue;
#endif
        }

        private void PerformAOEDamageLocal(Vector3 center)
        {
            Collider[] hits = Physics.OverlapSphere(center, aoeRadius);

            foreach (Collider hit in hits)
            {
                LocalUnit enemy = hit.GetComponent<LocalUnit>();
                if (enemy != null && enemy.OwnerId != attackerId)
                {
                    enemy.TakeDamage(damage * 0.5f, attackerId);
                }
            }
        }

        private void PerformAOEDamage(Vector3 center)
        {
            Collider[] hits = Physics.OverlapSphere(center, aoeRadius);

            foreach (Collider hit in hits)
            {
                UnitBase enemy = hit.GetComponent<UnitBase>();
                if (enemy != null && GetUnitOwnerId(enemy) != attackerId)
                {
                    enemy.TakeDamage(damage * 0.5f, attackerId);
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
