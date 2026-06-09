using UnityEngine;
using CastleWars.Units;
using CastleWars.Core;

namespace CastleWars.Combat
{
    /// <summary>
    /// 投射物控制器
    /// 处理远程攻击的投射物飞行、命中、AOE伤害
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    public class ProjectileController : MonoBehaviour
    {
        [Header("投射物设置")]
        [Tooltip("默认飞行速度")]
        [SerializeField] private float defaultSpeed = 10f;

        [Tooltip("最大存活时间")]
        [SerializeField] private float maxLifetime = 5f;

        [Tooltip("追踪目标")]
        [SerializeField] private bool isHoming = true;

        [Tooltip("追踪灵敏度")]
        [SerializeField] private float homingSensitivity = 5f;

        [Header("特效")]
        [Tooltip("命中特效")]
        [SerializeField] private GameObject hitEffectPrefab;

        [Tooltip("飞行轨迹特效")]
        [SerializeField] private TrailRenderer trailRenderer;

        // 投射物参数
        private Transform _target;
        private float _damage;
        private int _attackerId;
        private bool _hasAOE;
        private float _aoeRadius;
        private float _speed;

        // 组件引用
        private Rigidbody _rigidbody;

        // 状态
        private bool _isInitialized = false;
        private Vector3 _lastTargetPosition;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();

            // 配置刚体
            _rigidbody.useGravity = false;
            _rigidbody.isKinematic = false;

            // 配置碰撞体为触发器
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }
        }

        /// <summary>
        /// 初始化投射物
        /// </summary>
        /// <param name="target">目标Transform</param>
        /// <param name="damage">伤害值</param>
        /// <param name="attackerId">攻击者ID</param>
        /// <param name="hasAOE">是否有AOE</param>
        /// <param name="aoeRadius">AOE半径</param>
        /// <param name="speed">飞行速度（可选）</param>
        public void Initialize(Transform target, float damage, int attackerId, bool hasAOE = false, float aoeRadius = 0f, float speed = 0f)
        {
            _target = target;
            _damage = damage;
            _attackerId = attackerId;
            _hasAOE = hasAOE;
            _aoeRadius = aoeRadius;
            _speed = speed > 0 ? speed : defaultSpeed;
            _isInitialized = true;

            // 记录目标位置（用于目标死亡后继续飞行）
            if (target != null)
            {
                _lastTargetPosition = target.position;

                // 初始朝向
                Vector3 direction = (_lastTargetPosition - transform.position).normalized;
                _rigidbody.velocity = direction * _speed;
                transform.rotation = Quaternion.LookRotation(direction);
            }

            // 设置最大存活时间
            Destroy(gameObject, maxLifetime);

            Debug.Log($"[Projectile] 初始化 - 伤害: {damage}, 攻击者: Player {attackerId}, AOE: {hasAOE}");
        }

        private void FixedUpdate()
        {
            if (!_isInitialized) return;

            // 更新目标位置
            if (_target != null)
            {
                _lastTargetPosition = _target.position;
            }

            if (isHoming && _lastTargetPosition != Vector3.zero)
            {
                // 追踪飞行
                Vector3 direction = (_lastTargetPosition - transform.position).normalized;

                // 平滑转向
                Vector3 newVelocity = Vector3.Lerp(
                    _rigidbody.velocity.normalized,
                    direction,
                    Time.fixedDeltaTime * homingSensitivity
                ) * _speed;

                _rigidbody.velocity = newVelocity;

                // 更新朝向
                if (newVelocity != Vector3.zero)
                {
                    transform.rotation = Quaternion.LookRotation(newVelocity);
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!_isInitialized) return;

            // 检查敌方单位
            UnitBase unit = other.GetComponent<UnitBase>();
            if (unit != null)
            {
                // 忽略友军
                if (unit.OwnerId == _attackerId)
                {
                    return;
                }

                // 造成伤害
                unit.TakeDamage(_damage, _attackerId);

                // AOE伤害
                if (_hasAOE && _aoeRadius > 0)
                {
                    PerformAOEDamage(transform.position);
                }

                // 命中效果
                OnHit();
                return;
            }

            // 检查敌方城堡
            CastleController castle = other.GetComponent<CastleController>();
            if (castle != null && castle.OwnerId != _attackerId)
            {
                castle.TakeDamage(_damage);

                if (_hasAOE && _aoeRadius > 0)
                {
                    PerformAOEDamage(transform.position);
                }

                OnHit();
                return;
            }

            // 检查是否碰到地面或障碍物
            if (other.gameObject.layer == LayerMask.NameToLayer("Ground") ||
                other.gameObject.layer == LayerMask.NameToLayer("Obstacle"))
            {
                if (_hasAOE && _aoeRadius > 0)
                {
                    PerformAOEDamage(transform.position);
                }

                OnHit();
            }
        }

        /// <summary>
        /// AOE范围伤害
        /// </summary>
        private void PerformAOEDamage(Vector3 center)
        {
            Collider[] hits = Physics.OverlapSphere(center, _aoeRadius);

            foreach (Collider hit in hits)
            {
                // 跳过自己
                if (hit.gameObject == gameObject) continue;

                UnitBase enemy = hit.GetComponent<UnitBase>();
                if (enemy != null && enemy.OwnerId != _attackerId && !enemy.IsDead)
                {
                    // AOE伤害减半
                    float aoeDamage = _damage * 0.5f;
                    enemy.TakeDamage(aoeDamage, _attackerId);
                }
            }

            Debug.Log($"[Projectile] AOE伤害 - 中心: {center}, 半径: {_aoeRadius}");
        }

        /// <summary>
        /// 命中处理
        /// </summary>
        private void OnHit()
        {
            // 播放命中特效
            SpawnHitEffect();

            // 禁用轨迹
            if (trailRenderer != null)
            {
                trailRenderer.enabled = false;
            }

            // 销毁投射物
            Destroy(gameObject);
        }

        /// <summary>
        /// 生成命中特效
        /// </summary>
        private void SpawnHitEffect()
        {
            if (hitEffectPrefab != null)
            {
                GameObject effect = Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
                Destroy(effect, 2f);
            }
        }

        /// <summary>
        /// 设置投射物速度
        /// </summary>
        public void SetSpeed(float speed)
        {
            _speed = speed;

            if (_rigidbody != null && _rigidbody.velocity != Vector3.zero)
            {
                _rigidbody.velocity = _rigidbody.velocity.normalized * _speed;
            }
        }

        /// <summary>
        /// 设置追踪目标
        /// </summary>
        public void SetTarget(Transform target)
        {
            _target = target;
            if (target != null)
            {
                _lastTargetPosition = target.position;
            }
        }

        /// <summary>
        /// 禁用追踪
        /// </summary>
        public void DisableHoming()
        {
            isHoming = false;
        }

        private void OnDrawGizmosSelected()
        {
            if (_hasAOE && _aoeRadius > 0)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(transform.position, _aoeRadius);
            }
        }
    }
}
