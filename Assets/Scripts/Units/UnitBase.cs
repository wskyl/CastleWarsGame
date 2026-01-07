using UnityEngine;
using System;
using Unity.Netcode;
using CastleWars.Data;
using CastleWars.Core;

namespace CastleWars.Units
{
    /// <summary>
    /// 单位状态枚举
    /// </summary>
    public enum UnitState
    {
        Idle,       // 空闲
        Moving,     // 移动中
        Fighting,   // 战斗中
        Stunned,    // 被击晕
        Dead        // 死亡
    }

    /// <summary>
    /// 单位基类
    /// 管理单位生命值、状态机、网络同步
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    public class UnitBase : NetworkBehaviour
    {
        [Header("单位配置")]
        [Tooltip("单位数据配置")]
        public UnitData unitData;

        // 网络同步变量
        private NetworkVariable<float> _currentHealth = new NetworkVariable<float>(
            100f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        private NetworkVariable<int> _ownerId = new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        private NetworkVariable<UnitState> _currentState = new NetworkVariable<UnitState>(
            UnitState.Moving,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        // 组件引用
        private Rigidbody _rigidbody;
        private UnitMovement _movement;
        private UnitCombat _combat;
        private Animator _animator;

        // 当前目标
        private Transform _currentTarget;

        // 击晕计时器
        private float _stunEndTime;

        // 光环加成
        private float _attackBonus = 0f;

        // 事件
        public event Action<float, float> OnHealthChanged; // current, max
        public event Action<UnitState> OnStateChanged;
        public event Action OnUnitDied;

        // 属性
        public float CurrentHealth => _currentHealth.Value;
        public float MaxHealth => unitData != null ? unitData.maxHealth : 100f;
        public int OwnerId => _ownerId.Value;
        public UnitState CurrentState => _currentState.Value;
        public bool IsDead => _currentState.Value == UnitState.Dead;
        public bool IsStunned => _currentState.Value == UnitState.Stunned;
        public float HealthPercentage => _currentHealth.Value / MaxHealth;

        // 获取实际攻击力（含光环加成）
        public float GetAttackDamage()
        {
            if (unitData == null) return 0f;
            return unitData.attackDamage * (1f + _attackBonus);
        }

        // 获取对建筑的伤害
        public float GetBuildingDamage()
        {
            if (unitData == null) return 0f;
            return unitData.attackDamage * unitData.buildingDamageMultiplier * (1f + _attackBonus);
        }

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _movement = GetComponent<UnitMovement>();
            _combat = GetComponent<UnitCombat>();
            _animator = GetComponent<Animator>();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            _currentHealth.OnValueChanged += HandleHealthChanged;
            _currentState.OnValueChanged += HandleStateChanged;

            if (IsServer && unitData != null)
            {
                _currentHealth.Value = unitData.maxHealth;
            }

            Debug.Log($"[UnitBase] 单位生成 - 所有者: Player {_ownerId.Value}, 血量: {_currentHealth.Value}");
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            _currentHealth.OnValueChanged -= HandleHealthChanged;
            _currentState.OnValueChanged -= HandleStateChanged;
        }

        /// <summary>
        /// 初始化单位
        /// </summary>
        public void Initialize(int ownerId, UnitData data = null)
        {
            if (!IsServer) return;

            _ownerId.Value = ownerId;

            if (data != null)
            {
                unitData = data;
            }

            if (unitData != null)
            {
                _currentHealth.Value = unitData.maxHealth;
            }

            _currentState.Value = UnitState.Moving;
        }

        private void Update()
        {
            if (!IsServer) return;

            // 检查击晕状态
            if (IsStunned && Time.time >= _stunEndTime)
            {
                _currentState.Value = UnitState.Moving;
            }

            // 状态机更新
            UpdateStateMachine();

            // 更新光环效果
            UpdateAuraBonus();
        }

        #region 状态机

        private void UpdateStateMachine()
        {
            if (IsDead || IsStunned) return;

            switch (_currentState.Value)
            {
                case UnitState.Idle:
                    HandleIdleState();
                    break;

                case UnitState.Moving:
                    HandleMovingState();
                    break;

                case UnitState.Fighting:
                    HandleFightingState();
                    break;
            }
        }

        private void HandleIdleState()
        {
            // 搜索敌人
            SearchForTarget();

            // 如果没有敌人，继续移动
            if (_currentTarget == null)
            {
                _currentState.Value = UnitState.Moving;
            }
        }

        private void HandleMovingState()
        {
            // 移动
            if (_movement != null)
            {
                float direction = _ownerId.Value == 1 ? 1f : -1f;
                _movement.MoveInDirection(direction);
            }

            // 搜索敌人
            SearchForTarget();

            // 如果找到敌人并在攻击范围内
            if (_currentTarget != null && IsTargetInRange())
            {
                _currentState.Value = UnitState.Fighting;
                _movement?.Stop();
            }
        }

        private void HandleFightingState()
        {
            // 检查目标是否有效
            if (_currentTarget == null || !IsTargetValid(_currentTarget))
            {
                _currentTarget = null;
                _currentState.Value = UnitState.Moving;
                _movement?.Resume();
                return;
            }

            // 检查是否在攻击范围内
            if (!IsTargetInRange())
            {
                _currentState.Value = UnitState.Moving;
                _movement?.Resume();
                return;
            }

            // 攻击
            if (_combat != null)
            {
                _combat.Attack(_currentTarget);
            }
        }

        private void SearchForTarget()
        {
            if (_combat == null) return;

            _currentTarget = _combat.FindNearestEnemy(_ownerId.Value);
        }

        private bool IsTargetInRange()
        {
            if (_currentTarget == null || unitData == null) return false;

            float distance = Vector3.Distance(transform.position, _currentTarget.position);
            return distance <= unitData.attackRange;
        }

        private bool IsTargetValid(Transform target)
        {
            if (target == null) return false;

            UnitBase targetUnit = target.GetComponent<UnitBase>();
            if (targetUnit != null)
            {
                return !targetUnit.IsDead;
            }

            CastleController targetCastle = target.GetComponent<CastleController>();
            if (targetCastle != null)
            {
                return !targetCastle.IsDestroyed;
            }

            return false;
        }

        #endregion

        #region 伤害处理

        /// <summary>
        /// 受到伤害（ServerRpc）
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void TakeDamageServerRpc(float damage, int attackerId, ServerRpcParams rpcParams = default)
        {
            ApplyDamage(damage, attackerId);
        }

        /// <summary>
        /// 直接受到伤害（仅服务器调用）
        /// </summary>
        public void TakeDamage(float damage, int attackerId = 0)
        {
            if (!IsServer) return;
            ApplyDamage(damage, attackerId);
        }

        private void ApplyDamage(float damage, int attackerId)
        {
            if (IsDead || damage <= 0) return;

            // 计算实际伤害（考虑护甲）
            float actualDamage = CalculateActualDamage(damage);

            float newHealth = Mathf.Max(0, _currentHealth.Value - actualDamage);
            _currentHealth.Value = newHealth;

            Debug.Log($"[UnitBase] {unitData?.unitName ?? "Unit"} 受到 {actualDamage} 伤害, 剩余: {newHealth}");

            // 播放受击特效
            PlayHitEffectClientRpc();

            // 检查是否死亡
            if (newHealth <= 0)
            {
                Die(attackerId);
            }
        }

        private float CalculateActualDamage(float baseDamage)
        {
            if (unitData == null) return baseDamage;

            // 根据护甲类型减伤
            float reduction = unitData.armorType switch
            {
                ArmorType.None => 0f,
                ArmorType.Light => 0.1f,
                ArmorType.Medium => 0.2f,
                ArmorType.Heavy => 0.3f,
                ArmorType.Dragon => 0.3f,
                _ => 0f
            };

            return baseDamage * (1f - reduction);
        }

        /// <summary>
        /// 对单位施加击晕
        /// </summary>
        public void ApplyStun(float duration)
        {
            if (!IsServer || IsDead) return;

            _currentState.Value = UnitState.Stunned;
            _stunEndTime = Time.time + duration;
            _movement?.Stop();

            PlayStunEffectClientRpc();
        }

        #endregion

        #region 死亡处理

        /// <summary>
        /// 单位死亡
        /// </summary>
        public void Die(int killerId = 0)
        {
            if (!IsServer || IsDead) return;

            Debug.Log($"[UnitBase] {unitData?.unitName ?? "Unit"} 死亡, 击杀者: Player {killerId}");

            _currentState.Value = UnitState.Dead;
            _movement?.Stop();

            // 播放死亡特效
            PlayDeathEffectClientRpc();

            // 延迟销毁
            Invoke(nameof(DespawnUnit), 1f);
        }

        private void DespawnUnit()
        {
            if (IsServer && NetworkObject != null && NetworkObject.IsSpawned)
            {
                NetworkObject.Despawn();
            }
        }

        #endregion

        #region 光环系统

        private void UpdateAuraBonus()
        {
            if (unitData == null) return;

            _attackBonus = 0f;

            // 搜索周围友军的光环
            Collider[] nearbyUnits = Physics.OverlapSphere(transform.position, 10f);
            foreach (var col in nearbyUnits)
            {
                UnitBase otherUnit = col.GetComponent<UnitBase>();
                if (otherUnit != null &&
                    otherUnit != this &&
                    otherUnit.OwnerId == _ownerId.Value &&
                    otherUnit.unitData != null &&
                    otherUnit.unitData.auraAttackBonus > 0)
                {
                    float distance = Vector3.Distance(transform.position, otherUnit.transform.position);
                    if (distance <= otherUnit.unitData.auraRange)
                    {
                        _attackBonus += otherUnit.unitData.auraAttackBonus;
                    }
                }
            }
        }

        #endregion

        #region 网络回调和特效

        [ClientRpc]
        private void PlayHitEffectClientRpc()
        {
            // 播放受击动画
            if (_animator != null)
            {
                _animator.SetTrigger("Hit");
            }

            OnHealthChanged?.Invoke(_currentHealth.Value, MaxHealth);
        }

        [ClientRpc]
        private void PlayDeathEffectClientRpc()
        {
            // 播放死亡动画
            if (_animator != null)
            {
                _animator.SetTrigger("Die");
            }

            OnUnitDied?.Invoke();
        }

        [ClientRpc]
        private void PlayStunEffectClientRpc()
        {
            // 播放击晕特效
            if (_animator != null)
            {
                _animator.SetTrigger("Stun");
            }
        }

        private void HandleHealthChanged(float previousValue, float newValue)
        {
            OnHealthChanged?.Invoke(newValue, MaxHealth);
        }

        private void HandleStateChanged(UnitState previousValue, UnitState newValue)
        {
            OnStateChanged?.Invoke(newValue);
        }

        #endregion

        #region 工具方法

        /// <summary>
        /// 获取单位数据
        /// </summary>
        public UnitData GetUnitData()
        {
            return unitData;
        }

        /// <summary>
        /// 检查是否可以攻击指定目标
        /// </summary>
        public bool CanAttack(UnitBase target)
        {
            if (target == null || unitData == null || target.unitData == null)
                return false;

            return unitData.CanAttack(target.unitData.unitType);
        }

        /// <summary>
        /// 获取到目标的距离
        /// </summary>
        public float GetDistanceTo(Transform target)
        {
            if (target == null) return float.MaxValue;
            return Vector3.Distance(transform.position, target.position);
        }

        /// <summary>
        /// 设置当前目标
        /// </summary>
        public void SetTarget(Transform target)
        {
            _currentTarget = target;
        }

        /// <summary>
        /// 获取当前目标
        /// </summary>
        public Transform GetCurrentTarget()
        {
            return _currentTarget;
        }

        #endregion

        private void OnDrawGizmosSelected()
        {
            if (unitData != null)
            {
                // 攻击范围
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position, unitData.attackRange);

                // 光环范围
                if (unitData.auraRange > 0)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawWireSphere(transform.position, unitData.auraRange);
                }
            }
        }
    }
}
