using UnityEngine;
using CastleWars.Data;
using CastleWars.Economy;
using CastleWars.Core;
using System.Collections.Generic;

namespace CastleWars.UI
{
    /// <summary>
    /// 建筑UI - 显示可建造的建筑列表
    /// 支持本地模式和网络模式
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
        private PlayerEconomy economy;
        private LocalPlayer localPlayer;
        private List<BuildingButtonSimple> buildingButtons = new List<BuildingButtonSimple>();

        private int selectedSlot = -1;

        private void Start()
        {
            // 获取本地玩家的组件
            FindPlayerComponents();

            // 创建建筑按钮
            CreateBuildingButtons();

            // 隐藏面板
            if (buildingPanel != null)
            {
                buildingPanel.SetActive(false);
            }
        }

        private void FindPlayerComponents()
        {
            // 优先使用本地模式
            if (LocalGameMode.IsLocalMode && LocalGameMode.Instance != null)
            {
                localPlayer = LocalGameMode.Instance.GetPlayer(1);
                return;
            }

            // 网络模式
            var playerObj = FindLocalPlayer();
            if (playerObj != null)
            {
                buildingManager = playerObj.GetComponent<BuildingManager>();
                economy = playerObj.GetComponent<PlayerEconomy>();
            }
        }

        private GameObject FindLocalPlayer()
        {
            var networkPlayers = FindObjectsOfType<NetworkPlayer>();
            foreach (var player in networkPlayers)
            {
                return player.gameObject;
            }
            return null;
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

            if (LocalGameMode.IsLocalMode && localPlayer != null)
            {
                currentGold = localPlayer.Gold;
            }
            else if (economy != null)
            {
                currentGold = economy.Gold;
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
    /// 简化的建筑按钮组件 - 不依赖UI模块
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

        /// <summary>
        /// 由外部调用触发点击
        /// </summary>
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
