using UnityEngine;
using CastleWars.Data;

namespace CastleWars.Units
{
    /// <summary>
    /// 单位移动控制
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class UnitMovement : MonoBehaviour
    {
        private UnitBase unitBase;
        private Rigidbody rb;
        private bool isStopped = false;

        private void Awake()
        {
            unitBase = GetComponent<UnitBase>();
            rb = GetComponent<Rigidbody>();
        }

        private void Start()
        {
            // 配置刚体
            if (unitBase != null && unitBase.unitData != null)
            {
                if (unitBase.unitData.canFly)
                {
                    rb.useGravity = false;
                    rb.constraints = RigidbodyConstraints.FreezeRotation;
                }
                else
                {
                    rb.constraints = RigidbodyConstraints.FreezeRotationX |
                                    RigidbodyConstraints.FreezeRotationZ;
                }
            }
        }

        /// <summary>
        /// 朝指定方向移动
        /// </summary>
        public void MoveInDirection(float directionX)
        {
            if (isStopped) return;
            if (unitBase == null || unitBase.IsDead) return;
            if (unitBase.unitData == null) return;

            Vector3 moveDirection = new Vector3(directionX, 0, 0).normalized;

            if (unitBase.unitData.canFly)
            {
                float targetHeight = 5f;
                float currentHeight = transform.position.y;

                if (Mathf.Abs(currentHeight - targetHeight) > 0.5f)
                {
                    moveDirection.y = (targetHeight - currentHeight) * 0.1f;
                }
            }

            Vector3 velocity = moveDirection * unitBase.unitData.moveSpeed;
            rb.velocity = velocity;

            if (directionX != 0)
            {
                transform.rotation = Quaternion.LookRotation(new Vector3(directionX, 0, 0));
            }
        }

        /// <summary>
        /// 停止移动
        /// </summary>
        public void Stop()
        {
            isStopped = true;
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
            }
        }

        /// <summary>
        /// 恢复移动
        /// </summary>
        public void Resume()
        {
            isStopped = false;
        }
    }
}
