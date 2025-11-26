using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CastleWars.Data;
using CastleWars.Economy;
using System.Collections.Generic;

namespace CastleWars.UI
{
    /// <summary>
    /// 建筑UI - 显示可建造的建筑列表
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
        private List<BuildingButton> buildingButtons = new List<BuildingButton>();

        private int selectedSlot = -1;

        private void Start()
        {
            // 获取本地玩家的组件
            var localPlayer = FindLocalPlayer();
            if (localPlayer != null)
            {
                buildingManager = localPlayer.GetComponent<BuildingManager>();
                economy = localPlayer.GetComponent<PlayerEconomy>();
            }

            // 创建建筑按钮
            CreateBuildingButtons();

            // 隐藏面板
            buildingPanel.SetActive(false);
        }

        private GameObject FindLocalPlayer()
        {
            var networkPlayers = FindObjectsOfType<CastleWars.Core.NetworkPlayer>();
            foreach (var player in networkPlayers)
            {
                if (player.IsOwner)
                {
                    return player.gameObject;
                }
            }
            return null;
        }

        private void CreateBuildingButtons()
        {
            foreach (BuildingData building in availableBuildings)
            {
                GameObject buttonObj = Instantiate(buildingButtonPrefab, buildingListContainer);
                BuildingButton button = buttonObj.GetComponent<BuildingButton>();

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
            buildingPanel.SetActive(true);

            // 更新按钮状态
            UpdateButtonStates();
        }

        /// <summary>
        /// 隐藏建筑面板
        /// </summary>
        public void HideBuildingPanel()
        {
            buildingPanel.SetActive(false);
            selectedSlot = -1;
        }

        private void OnBuildingSelected(BuildingData buildingData)
        {
            if (selectedSlot < 0 || buildingManager == null) return;

            // 尝试建造
            buildingManager.TryBuildBuilding(buildingData, selectedSlot);

            // 关闭面板
            HideBuildingPanel();
        }

        private void UpdateButtonStates()
        {
            if (economy == null) return;

            foreach (var button in buildingButtons)
            {
                button.UpdateState(economy.Gold);
            }
        }

        private void Update()
        {
            // 实时更新按钮状态
            if (buildingPanel.activeSelf)
            {
                UpdateButtonStates();
            }
        }
    }

    /// <summary>
    /// 建筑按钮组件
    /// </summary>
    public class BuildingButton : MonoBehaviour
    {
        [Header("UI组件")]
        public Image iconImage;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI costText;
        public Button button;

        private BuildingData buildingData;
        private System.Action<BuildingData> onClickCallback;

        public void Initialize(BuildingData data, System.Action<BuildingData> onClick)
        {
            buildingData = data;
            onClickCallback = onClick;

            // 设置UI
            if (iconImage != null) iconImage.sprite = data.icon;
            if (nameText != null) nameText.text = data.buildingName;
            if (costText != null) costText.text = $"{data.goldCost} Gold";

            // 绑定点击事件
            if (button != null)
            {
                button.onClick.AddListener(OnButtonClicked);
            }
        }

        public void UpdateState(int currentGold)
        {
            // 根据金币数量启用/禁用按钮
            bool canAfford = currentGold >= buildingData.goldCost;

            if (button != null)
            {
                button.interactable = canAfford;
            }

            // 改变颜色提示
            if (costText != null)
            {
                costText.color = canAfford ? Color.green : Color.red;
            }
        }

        private void OnButtonClicked()
        {
            onClickCallback?.Invoke(buildingData);
        }
    }
}
