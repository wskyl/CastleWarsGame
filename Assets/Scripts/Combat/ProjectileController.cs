using UnityEngine;
using CastleWars.Units;
using CastleWars.Core;

namespace CastleWars.Combat
{
    /// <summary>
    /// 投射物控制器 - 处理远程攻击的子弹/箭矢（纯本地模式）
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
            // 检查 UnitController
            UnitController unitController = other.GetComponent<UnitController>();
            if (unitController != null)
            {
                if (unitController.OwnerId == attackerId)
                {
                    return;
                }

                unitController.TakeDamage(damage, attackerId);

                if (hasAOE)
                {
                    PerformAOEDamage(transform.position);
                }

                SpawnHitEffect();
                Destroy(gameObject);
                return;
            }

            // 检查 UnitBase
            UnitBase unit = other.GetComponent<UnitBase>();
            if (unit != null)
            {
                if (unit.OwnerId == attackerId)
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
                return;
            }

            // 检查城堡
            CastleController castle = other.GetComponent<CastleController>();
            if (castle != null && castle.OwnerId != attackerId)
            {
                castle.TakeDamage(damage);
                SpawnHitEffect();
                Destroy(gameObject);
            }
        }

        private void PerformAOEDamage(Vector3 center)
        {
            Collider[] hits = Physics.OverlapSphere(center, aoeRadius);

            foreach (Collider hit in hits)
            {
                UnitController enemyController = hit.GetComponent<UnitController>();
                if (enemyController != null && enemyController.OwnerId != attackerId)
                {
                    enemyController.TakeDamage(damage * 0.5f, attackerId);
                    continue;
                }

                UnitBase enemy = hit.GetComponent<UnitBase>();
                if (enemy != null && enemy.OwnerId != attackerId)
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
