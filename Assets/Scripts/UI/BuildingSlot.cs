using UnityEngine;

namespace CastleWars.UI
{
    /// <summary>
    /// 建筑槽位 - 可放置建筑的位置
    /// </summary>
    public class BuildingSlot : MonoBehaviour
    {
        [Header("槽位设置")]
        public int slotIndex;
        public bool isOccupied = false;

        [Header("视觉效果")]
        [SerializeField] private GameObject highlightEffect;
        [SerializeField] private Material normalMaterial;
        [SerializeField] private Material highlightMaterial;

        private MeshRenderer meshRenderer;
        private BuildingUI buildingUI;

        private void Awake()
        {
            meshRenderer = GetComponent<MeshRenderer>();
            buildingUI = FindObjectOfType<BuildingUI>();
        }

        private void Start()
        {
            if (highlightEffect != null)
            {
                highlightEffect.SetActive(false);
            }
        }

        /// <summary>
        /// 槽位被点击
        /// </summary>
        public void OnSlotClicked()
        {
            if (isOccupied)
            {
                Debug.Log("Slot already occupied");
                return;
            }

            // 显示建筑选择UI
            if (buildingUI != null)
            {
                buildingUI.ShowBuildingPanel(slotIndex);
            }

            // 高亮显示
            Highlight(true);
        }

        /// <summary>
        /// 高亮显示
        /// </summary>
        public void Highlight(bool enabled)
        {
            if (highlightEffect != null)
            {
                highlightEffect.SetActive(enabled);
            }

            if (meshRenderer != null && highlightMaterial != null && normalMaterial != null)
            {
                meshRenderer.material = enabled ? highlightMaterial : normalMaterial;
            }
        }

        /// <summary>
        /// 标记为已占用
        /// </summary>
        public void SetOccupied(bool occupied)
        {
            isOccupied = occupied;

            if (occupied)
            {
                // 可以改变颜色或添加特效表示已占用
                if (meshRenderer != null)
                {
                    meshRenderer.enabled = false; // 隐藏槽位
                }
            }
        }
    }
}
