using UnityEngine;
using CastleWars.Data;

namespace CastleWars.Units
{
    /// <summary>
    /// 单位移动控制器
    /// 控制单位的移动、飞行、停止/恢复
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(UnitBase))]
    public class UnitMovement : MonoBehaviour
    {
        [Header("飞行设置")]
        [Tooltip("飞行单位的目标高度")]
        [SerializeField] private float flyHeight = 5f;

        [Tooltip("高度调整速度")]
        [SerializeField] private float heightAdjustSpeed = 2f;

        // 组件引用
        private UnitBase _unitBase;
        private Rigidbody _rigidbody;

        // 移动状态
        private bool _isStopped = false;
        private float _currentDirection = 0f;

        // 属性
        public bool IsStopped => _isStopped;
        public float CurrentSpeed => _rigidbody != null ? _rigidbody.velocity.magnitude : 0f;

        private void Awake()
        {
            _unitBase = GetComponent<UnitBase>();
            _rigidbody = GetComponent<Rigidbody>();
        }

        private void Start()
        {
            ConfigureRigidbody();
        }

        /// <summary>
        /// 配置刚体属性
        /// </summary>
        private void ConfigureRigidbody()
        {
            if (_rigidbody == null) return;

            if (_unitBase?.unitData != null && _unitBase.unitData.unitType == UnitType.Air)
            {
                // 空中单位配置
                _rigidbody.useGravity = false;
                _rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
            }
            else
            {
                // 地面单位配置
                _rigidbody.useGravity = true;
                _rigidbody.constraints = RigidbodyConstraints.FreezeRotationX |
                                          RigidbodyConstraints.FreezeRotationZ;
            }

            // 设置刚体为运动学模式以获得更好的网络同步
            _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        }

        private void FixedUpdate()
        {
            if (_isStopped || _unitBase == null || _unitBase.IsDead) return;

            // 处理飞行单位的高度
            if (_unitBase.unitData != null && _unitBase.unitData.unitType == UnitType.Air)
            {
                AdjustFlightHeight();
            }
        }

        /// <summary>
        /// 朝指定方向移动
        /// </summary>
        /// <param name="directionX">移动方向（1为正方向，-1为负方向）</param>
        public void MoveInDirection(float directionX)
        {
            if (_isStopped || _unitBase == null || _unitBase.IsDead) return;
            if (_unitBase.unitData == null || _rigidbody == null) return;

            _currentDirection = directionX;

            // 计算移动速度
            Vector3 moveDirection = new Vector3(directionX, 0, 0).normalized;
            Vector3 velocity = moveDirection * _unitBase.unitData.moveSpeed;

            // 保持Y轴速度（用于飞行单位）
            if (_unitBase.unitData.unitType == UnitType.Air)
            {
                velocity.y = _rigidbody.velocity.y;
            }

            _rigidbody.velocity = velocity;

            // 更新朝向
            UpdateFacing(directionX);
        }

        /// <summary>
        /// 移动到指定位置
        /// </summary>
        public void MoveToPosition(Vector3 targetPosition)
        {
            if (_isStopped || _unitBase == null || _unitBase.IsDead) return;
            if (_unitBase.unitData == null || _rigidbody == null) return;

            Vector3 direction = (targetPosition - transform.position).normalized;

            // 对于地面单位，只在XZ平面移动
            if (_unitBase.unitData.unitType == UnitType.Ground)
            {
                direction.y = 0;
                direction = direction.normalized;
            }

            Vector3 velocity = direction * _unitBase.unitData.moveSpeed;
            _rigidbody.velocity = velocity;

            // 更新朝向
            if (direction.x != 0)
            {
                UpdateFacing(direction.x > 0 ? 1f : -1f);
            }
        }

        /// <summary>
        /// 调整飞行高度
        /// </summary>
        private void AdjustFlightHeight()
        {
            float currentHeight = transform.position.y;
            float heightDiff = flyHeight - currentHeight;

            if (Mathf.Abs(heightDiff) > 0.1f)
            {
                float verticalSpeed = Mathf.Sign(heightDiff) * heightAdjustSpeed;
                Vector3 velocity = _rigidbody.velocity;
                velocity.y = verticalSpeed;
                _rigidbody.velocity = velocity;
            }
            else
            {
                // 保持在目标高度
                Vector3 velocity = _rigidbody.velocity;
                velocity.y = 0;
                _rigidbody.velocity = velocity;

                Vector3 position = transform.position;
                position.y = flyHeight;
                transform.position = position;
            }
        }

        /// <summary>
        /// 更新单位朝向
        /// </summary>
        private void UpdateFacing(float directionX)
        {
            if (directionX == 0) return;

            // 使用LookRotation设置朝向
            Vector3 lookDirection = new Vector3(directionX, 0, 0);
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
        }

        /// <summary>
        /// 停止移动
        /// </summary>
        public void Stop()
        {
            _isStopped = true;

            if (_rigidbody != null)
            {
                // 保留Y轴速度（用于飞行单位悬停）
                if (_unitBase?.unitData != null && _unitBase.unitData.unitType == UnitType.Air)
                {
                    Vector3 velocity = _rigidbody.velocity;
                    velocity.x = 0;
                    velocity.z = 0;
                    _rigidbody.velocity = velocity;
                }
                else
                {
                    _rigidbody.velocity = Vector3.zero;
                }
            }
        }

        /// <summary>
        /// 恢复移动
        /// </summary>
        public void Resume()
        {
            _isStopped = false;
        }

        /// <summary>
        /// 面向目标
        /// </summary>
        public void FaceTarget(Transform target)
        {
            if (target == null) return;

            Vector3 direction = (target.position - transform.position).normalized;
            direction.y = 0; // 只在水平面旋转

            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
            }
        }

        /// <summary>
        /// 设置飞行高度
        /// </summary>
        public void SetFlyHeight(float height)
        {
            flyHeight = height;
        }

        /// <summary>
        /// 获取当前移动方向
        /// </summary>
        public float GetCurrentDirection()
        {
            return _currentDirection;
        }

        /// <summary>
        /// 检查是否在移动
        /// </summary>
        public bool IsMoving()
        {
            return !_isStopped && _rigidbody != null && _rigidbody.velocity.magnitude > 0.1f;
        }

        /// <summary>
        /// 施加击退效果
        /// </summary>
        public void ApplyKnockback(Vector3 direction, float force)
        {
            if (_rigidbody == null) return;

            _rigidbody.AddForce(direction * force, ForceMode.Impulse);
        }
    }
}
