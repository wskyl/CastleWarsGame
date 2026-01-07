using UnityEngine;
using System;

namespace CastleWars.UI
{
    /// <summary>
    /// 建筑槽位
    /// 可放置建筑的位置，处理点击交互
    /// </summary>
    public class BuildingSlot : MonoBehaviour
    {
        [Header("槽位设置")]
        [Tooltip("槽位索引")]
        public int slotIndex;

        [Tooltip("是否被占用")]
        [SerializeField] private bool isOccupied = false;

        [Tooltip("所属玩家ID")]
        [SerializeField] private int ownerId = 0;

        [Header("视觉效果")]
        [Tooltip("高亮特效")]
        [SerializeField] private GameObject highlightEffect;

        [Tooltip("正常材质")]
        [SerializeField] private Material normalMaterial;

        [Tooltip("高亮材质")]
        [SerializeField] private Material highlightMaterial;

        [Tooltip("占用材质")]
        [SerializeField] private Material occupiedMaterial;

        [Tooltip("不可用材质")]
        [SerializeField] private Material disabledMaterial;

        // 组件引用
        private MeshRenderer _meshRenderer;
        private BuildingUI _buildingUI;
        private Collider _collider;

        // 事件
        public event Action<int> OnSlotClicked; // 槽位索引

        // 属性
        public bool IsOccupied => isOccupied;
        public int OwnerId => ownerId;

        private void Awake()
        {
            _meshRenderer = GetComponent<MeshRenderer>();
            _collider = GetComponent<Collider>();
            _buildingUI = FindObjectOfType<BuildingUI>();
        }

        private void Start()
        {
            // 初始化高亮效果
            if (highlightEffect != null)
            {
                highlightEffect.SetActive(false);
            }

            // 应用初始材质
            ApplyMaterial();
        }

        /// <summary>
        /// 初始化槽位
        /// </summary>
        public void Initialize(int index, int owner)
        {
            slotIndex = index;
            ownerId = owner;
            isOccupied = false;

            ApplyMaterial();
        }

        /// <summary>
        /// 槽位被点击
        /// </summary>
        public void OnSlotClicked_Handler()
        {
            if (isOccupied)
            {
                Debug.Log($"[BuildingSlot] 槽位 {slotIndex} 已被占用");
                return;
            }

            Debug.Log($"[BuildingSlot] 槽位 {slotIndex} 被点击");

            // 显示建筑选择UI
            if (_buildingUI != null)
            {
                _buildingUI.ShowBuildingPanel(slotIndex);
            }

            // 高亮显示
            SetHighlight(true);

            // 触发事件
            OnSlotClicked?.Invoke(slotIndex);
        }

        /// <summary>
        /// 鼠标/触摸点击处理
        /// </summary>
        private void OnMouseDown()
        {
            OnSlotClicked_Handler();
        }

        #region 视觉效果

        /// <summary>
        /// 设置高亮状态
        /// </summary>
        public void SetHighlight(bool enabled)
        {
            if (isOccupied) return;

            // 高亮特效
            if (highlightEffect != null)
            {
                highlightEffect.SetActive(enabled);
            }

            // 高亮材质
            if (_meshRenderer != null)
            {
                if (enabled && highlightMaterial != null)
                {
                    _meshRenderer.material = highlightMaterial;
                }
                else
                {
                    ApplyMaterial();
                }
            }
        }

        /// <summary>
        /// 应用当前状态对应的材质
        /// </summary>
        private void ApplyMaterial()
        {
            if (_meshRenderer == null) return;

            if (isOccupied)
            {
                if (occupiedMaterial != null)
                {
                    _meshRenderer.material = occupiedMaterial;
                }
                else
                {
                    _meshRenderer.enabled = false;
                }
            }
            else
            {
                _meshRenderer.enabled = true;
                if (normalMaterial != null)
                {
                    _meshRenderer.material = normalMaterial;
                }
            }
        }

        #endregion

        #region 占用管理

        /// <summary>
        /// 标记为已占用
        /// </summary>
        public void SetOccupied(bool occupied)
        {
            isOccupied = occupied;

            // 更新视觉效果
            ApplyMaterial();

            // 禁用高亮
            if (highlightEffect != null)
            {
                highlightEffect.SetActive(false);
            }

            // 禁用碰撞（可选）
            // if (_collider != null)
            // {
            //     _collider.enabled = !occupied;
            // }

            Debug.Log($"[BuildingSlot] 槽位 {slotIndex} 占用状态: {occupied}");
        }

        /// <summary>
        /// 检查槽位是否可用
        /// </summary>
        public bool IsAvailable()
        {
            return !isOccupied;
        }

        /// <summary>
        /// 重置槽位
        /// </summary>
        public void Reset()
        {
            isOccupied = false;
            SetHighlight(false);
            ApplyMaterial();
        }

        #endregion

        #region 所有者管理

        /// <summary>
        /// 设置所有者
        /// </summary>
        public void SetOwner(int owner)
        {
            ownerId = owner;
        }

        /// <summary>
        /// 检查是否属于指定玩家
        /// </summary>
        public bool BelongsTo(int playerId)
        {
            return ownerId == playerId || ownerId == 0; // 0表示公共槽位
        }

        #endregion

        private void OnDrawGizmos()
        {
            // 绘制槽位范围
            Gizmos.color = isOccupied ? Color.red : Color.green;
            Gizmos.DrawWireCube(transform.position, new Vector3(2, 0.1f, 2));

            // 绘制槽位索引
#if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up, $"Slot {slotIndex}");
#endif
        }
    }
}
