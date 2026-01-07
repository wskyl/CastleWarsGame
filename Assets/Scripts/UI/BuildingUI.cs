using UnityEngine;
using CastleWars.Data;
using CastleWars.Economy;
using CastleWars.Core;
using System.Collections.Generic;

namespace CastleWars.UI
{
    /// <summary>
    /// 建筑UI - 显示可建造的建筑列表（纯本地模式）
    /// </summary>
    public class BuildingUI : MonoBehaviour
    {
        [Header("UI引用")]
        [SerializeField] private GameObject buildingButtonPrefab;
        [SerializeField] private Transform buildingListContainer;
        [SerializeField] private GameObject buildingPanel;

        [Header("建筑数据")]
        [SerializeField] private List<BuildingData> availableBuildings = new List<BuildingData>();

        private BuildingManager buildingManager;
        private PlayerController playerController;
        private List<BuildingButtonSimple> buildingButtons = new List<BuildingButtonSimple>();

        private int selectedSlot = -1;

        private void Start()
        {
            FindPlayerComponents();
            CreateBuildingButtons();

            if (buildingPanel != null)
            {
                buildingPanel.SetActive(false);
            }
        }

        private void FindPlayerComponents()
        {
            if (GameManager.Instance != null)
            {
                playerController = GameManager.Instance.GetPlayer(1);
            }
        }

        private void CreateBuildingButtons()
        {
            if (buildingButtonPrefab == null || buildingListContainer == null) return;

            foreach (BuildingData building in availableBuildings)
            {
                GameObject buttonObj = Instantiate(buildingButtonPrefab, buildingListContainer);
                BuildingButtonSimple button = buttonObj.GetComponent<BuildingButtonSimple>();

                if (button != null)
                {
                    button.Initialize(building, OnBuildingSelected);
                    buildingButtons.Add(button);
                }
            }
        }

        /// <summary>
        /// 显示建筑面板
        /// </summary>
        public void ShowBuildingPanel(int slotIndex)
        {
            selectedSlot = slotIndex;
            if (buildingPanel != null)
            {
                buildingPanel.SetActive(true);
            }

            UpdateButtonStates();
        }

        /// <summary>
        /// 隐藏建筑面板
        /// </summary>
        public void HideBuildingPanel()
        {
            if (buildingPanel != null)
            {
                buildingPanel.SetActive(false);
            }
            selectedSlot = -1;
        }

        private void OnBuildingSelected(BuildingData buildingData)
        {
            if (selectedSlot < 0) return;

            if (buildingManager != null)
            {
                buildingManager.TryBuildBuilding(buildingData, selectedSlot);
            }

            HideBuildingPanel();
        }

        private void UpdateButtonStates()
        {
            int currentGold = 0;

            if (playerController != null)
            {
                currentGold = playerController.Gold;
            }

            foreach (var button in buildingButtons)
            {
                button.UpdateState(currentGold);
            }
        }

        private void Update()
        {
            if (buildingPanel != null && buildingPanel.activeSelf)
            {
                UpdateButtonStates();
            }
        }
    }

    /// <summary>
    /// 简化的建筑按钮组件
    /// </summary>
    public class BuildingButtonSimple : MonoBehaviour
    {
        private BuildingData buildingData;
        private System.Action<BuildingData> onClickCallback;
        private bool canAfford = true;

        public void Initialize(BuildingData data, System.Action<BuildingData> onClick)
        {
            buildingData = data;
            onClickCallback = onClick;
        }

        public void UpdateState(int currentGold)
        {
            if (buildingData == null) return;
            canAfford = currentGold >= buildingData.goldCost;
        }

        public void OnClick()
        {
            if (canAfford)
            {
                onClickCallback?.Invoke(buildingData);
            }
        }

        private void OnMouseDown()
        {
            OnClick();
        }
    }
}
