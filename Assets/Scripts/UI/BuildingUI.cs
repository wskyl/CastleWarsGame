using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using CastleWars.Data;
using CastleWars.Economy;
using CastleWars.Core;

namespace CastleWars.UI
{
    /// <summary>
    /// 建筑选择UI
    /// 显示可建造的建筑列表，处理建筑选择
    /// </summary>
    public class BuildingUI : MonoBehaviour
    {
        [Header("UI引用")]
        [Tooltip("建筑按钮预制体")]
        [SerializeField] private GameObject buildingButtonPrefab;

        [Tooltip("建筑列表容器")]
        [SerializeField] private Transform buildingListContainer;

        [Tooltip("建筑面板")]
        [SerializeField] private GameObject buildingPanel;

        [Tooltip("关闭按钮")]
        [SerializeField] private Button closeButton;

        [Header("分类标签")]
        [SerializeField] private Transform categoryTabContainer;
        [SerializeField] private GameObject categoryTabPrefab;

        [Header("建筑数据")]
        [Tooltip("所有可建造的建筑")]
        [SerializeField] private List<BuildingData> availableBuildings = new List<BuildingData>();

        // 组件引用
        private BuildingManager _buildingManager;
        private LocalBuildingManagerAdapter _localBuildingManager;
        private IEconomyProvider _economy;

        // 按钮列表
        private List<BuildingButton> _buildingButtons = new List<BuildingButton>();

        // 当前选中的槽位
        private int _selectedSlot = -1;

        // 当前选中的分类
        private BuildingCategory _currentCategory = BuildingCategory.Infantry;

        // 事件
        public event Action<BuildingData, int> OnBuildingSelected; // 建筑数据, 槽位索引
        public event Action OnPanelClosed;

        // 属性
        public bool IsOpen => buildingPanel != null && buildingPanel.activeSelf;

        private void Start()
        {
            // 查找玩家组件
            FindPlayerComponents();

            // 创建建筑按钮
            CreateBuildingButtons();

            // 创建分类标签
            CreateCategoryTabs();

            // 设置关闭按钮
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(HideBuildingPanel);
            }

            // 初始隐藏面板
            if (buildingPanel != null)
            {
                buildingPanel.SetActive(false);
            }
        }

        /// <summary>
        /// 查找玩家组件
        /// </summary>
        private void FindPlayerComponents()
        {
            // 优先检查本地模式
            if (CastleWars.Core.LocalGameMode.IsLocalMode)
            {
                FindLocalPlayerComponents();
                return;
            }

            // 网络模式：查找NetworkPlayer
            NetworkPlayer[] players = FindObjectsOfType<NetworkPlayer>();
            foreach (var player in players)
            {
                if (player.IsLocalPlayer())
                {
                    _buildingManager = player.GetBuildingManager();
                    _economy = player.GetEconomy();
                    break;
                }
            }
        }

        /// <summary>
        /// 在本地模式下查找玩家组件
        /// </summary>
        private void FindLocalPlayerComponents()
        {
            CastleWars.Core.LocalPlayer localPlayer = null;

            if (CastleWars.Core.LocalGameMode.Instance != null)
            {
                localPlayer = CastleWars.Core.LocalGameMode.Instance.GetPlayer(1);
            }

            if (localPlayer != null)
            {
                // 本地模式下使用LocalPlayer适配器
                _economy = localPlayer.GetComponent<LocalPlayerEconomyAdapter>();
                if (_economy == null)
                {
                    _economy = localPlayer.gameObject.AddComponent<LocalPlayerEconomyAdapter>();
                    ((LocalPlayerEconomyAdapter)_economy).Initialize(localPlayer);
                }

                // 本地模式下使用LocalBuildingManagerAdapter
                _localBuildingManager = localPlayer.GetComponent<LocalBuildingManagerAdapter>();
                if (_localBuildingManager == null)
                {
                    _localBuildingManager = localPlayer.gameObject.AddComponent<LocalBuildingManagerAdapter>();
                    _localBuildingManager.Initialize(localPlayer);
                }
            }
        }

        /// <summary>
        /// 设置玩家组件引用
        /// </summary>
        public void SetPlayerComponents(BuildingManager manager, PlayerEconomy economy)
        {
            _buildingManager = manager;
            _economy = economy;

            // 订阅事件
            if (_economy != null)
            {
                _economy.OnGoldChanged += OnGoldChanged;
            }
        }

        #region 面板控制

        /// <summary>
        /// 显示建筑面板
        /// </summary>
        /// <param name="slotIndex">槽位索引</param>
        public void ShowBuildingPanel(int slotIndex)
        {
            _selectedSlot = slotIndex;

            if (buildingPanel != null)
            {
                buildingPanel.SetActive(true);
            }

            // 更新按钮状态
            UpdateButtonStates();

            Debug.Log($"[BuildingUI] 显示建筑面板 - 槽位: {slotIndex}");
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

            _selectedSlot = -1;
            OnPanelClosed?.Invoke();

            Debug.Log("[BuildingUI] 隐藏建筑面板");
        }

        /// <summary>
        /// 切换面板显示状态
        /// </summary>
        public void TogglePanel(int slotIndex)
        {
            if (IsOpen && _selectedSlot == slotIndex)
            {
                HideBuildingPanel();
            }
            else
            {
                ShowBuildingPanel(slotIndex);
            }
        }

        #endregion

        #region 建筑按钮

        /// <summary>
        /// 创建建筑按钮
        /// </summary>
        private void CreateBuildingButtons()
        {
            if (buildingButtonPrefab == null || buildingListContainer == null) return;

            // 清除现有按钮
            foreach (Transform child in buildingListContainer)
            {
                Destroy(child.gameObject);
            }
            _buildingButtons.Clear();

            // 创建新按钮
            foreach (BuildingData building in availableBuildings)
            {
                GameObject buttonObj = Instantiate(buildingButtonPrefab, buildingListContainer);
                BuildingButton button = buttonObj.GetComponent<BuildingButton>();

                if (button != null)
                {
                    button.Initialize(building, OnBuildingButtonClicked);
                    _buildingButtons.Add(button);
                }
            }
        }

        /// <summary>
        /// 创建分类标签
        /// </summary>
        private void CreateCategoryTabs()
        {
            if (categoryTabContainer == null || categoryTabPrefab == null) return;

            foreach (BuildingCategory category in Enum.GetValues(typeof(BuildingCategory)))
            {
                GameObject tabObj = Instantiate(categoryTabPrefab, categoryTabContainer);
                Button tabButton = tabObj.GetComponent<Button>();
                TextMeshProUGUI tabText = tabObj.GetComponentInChildren<TextMeshProUGUI>();

                if (tabText != null)
                {
                    tabText.text = category.ToString();
                }

                if (tabButton != null)
                {
                    BuildingCategory cat = category; // 闭包捕获
                    tabButton.onClick.AddListener(() => FilterByCategory(cat));
                }
            }
        }

        /// <summary>
        /// 按分类筛选
        /// </summary>
        private void FilterByCategory(BuildingCategory category)
        {
            _currentCategory = category;

            foreach (var button in _buildingButtons)
            {
                bool show = button.BuildingData.category == category;
                button.gameObject.SetActive(show);
            }
        }

        /// <summary>
        /// 建筑按钮点击回调
        /// </summary>
        private void OnBuildingButtonClicked(BuildingData buildingData)
        {
            if (_selectedSlot < 0) return;

            // 尝试建造 - 支持网络模式和本地模式
            if (_localBuildingManager != null)
            {
                _localBuildingManager.TryBuildBuilding(buildingData, _selectedSlot);
            }
            else if (_buildingManager != null)
            {
                _buildingManager.TryBuildBuilding(buildingData, _selectedSlot);
            }

            OnBuildingSelected?.Invoke(buildingData, _selectedSlot);

            // 隐藏面板
            HideBuildingPanel();
        }

        /// <summary>
        /// 更新按钮状态
        /// </summary>
        private void UpdateButtonStates()
        {
            int currentGold = _economy != null ? _economy.Gold : 0;

            foreach (var button in _buildingButtons)
            {
                // 检查金币
                bool canAfford = currentGold >= button.BuildingData.goldCost;

                // 检查前置条件 - 支持网络模式和本地模式
                bool hasPrerequisites = true;
                if (_localBuildingManager != null)
                {
                    var missing = _localBuildingManager.GetMissingPrerequisites(button.BuildingData);
                    hasPrerequisites = missing.Count == 0;
                }
                else if (_buildingManager != null)
                {
                    var missing = _buildingManager.GetMissingPrerequisites(button.BuildingData);
                    hasPrerequisites = missing.Count == 0;
                }

                button.UpdateState(canAfford, hasPrerequisites);
            }
        }

        private void OnGoldChanged(int newGold)
        {
            if (IsOpen)
            {
                UpdateButtonStates();
            }
        }

        #endregion

        private void Update()
        {
            // 持续更新按钮状态（如果面板打开）
            if (IsOpen)
            {
                UpdateButtonStates();
            }
        }

        private void OnDestroy()
        {
            if (_economy != null)
            {
                _economy.OnGoldChanged -= OnGoldChanged;
            }
        }
    }

    /// <summary>
    /// 建筑按钮组件
    /// </summary>
    public class BuildingButton : MonoBehaviour
    {
        [Header("UI组件")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI costText;
        [SerializeField] private TextMeshProUGUI tierText;
        [SerializeField] private Button button;
        [SerializeField] private Image lockOverlay;
        [SerializeField] private GameObject affordIndicator;

        // 建筑数据
        private BuildingData _buildingData;
        private Action<BuildingData> _onClickCallback;

        // 属性
        public BuildingData BuildingData => _buildingData;

        /// <summary>
        /// 初始化按钮
        /// </summary>
        public void Initialize(BuildingData data, Action<BuildingData> onClick)
        {
            _buildingData = data;
            _onClickCallback = onClick;

            // 设置UI
            if (iconImage != null && data.icon != null)
            {
                iconImage.sprite = data.icon;
            }

            if (nameText != null)
            {
                nameText.text = data.buildingName;
            }

            if (costText != null)
            {
                costText.text = $"{data.goldCost}";
            }

            if (tierText != null)
            {
                tierText.text = data.tier.ToString();
            }

            // 绑定点击事件
            if (button != null)
            {
                button.onClick.AddListener(OnButtonClicked);
            }
        }

        /// <summary>
        /// 更新按钮状态
        /// </summary>
        public void UpdateState(bool canAfford, bool hasPrerequisites)
        {
            bool canBuild = canAfford && hasPrerequisites;

            // 更新按钮可交互状态
            if (button != null)
            {
                button.interactable = canBuild;
            }

            // 更新金币颜色
            if (costText != null)
            {
                costText.color = canAfford ? Color.green : Color.red;
            }

            // 更新锁定遮罩
            if (lockOverlay != null)
            {
                lockOverlay.gameObject.SetActive(!hasPrerequisites);
            }

            // 更新可负担指示器
            if (affordIndicator != null)
            {
                affordIndicator.SetActive(canAfford);
            }
        }

        private void OnButtonClicked()
        {
            _onClickCallback?.Invoke(_buildingData);
        }
    }
}
