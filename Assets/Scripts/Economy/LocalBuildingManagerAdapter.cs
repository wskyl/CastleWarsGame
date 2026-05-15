using UnityEngine;
using System;
using System.Collections.Generic;
using CastleWars.Data;
using CastleWars.Core;

namespace CastleWars.Economy
{
    /// <summary>
    /// 本地建筑管理器适配器
    /// 在本地模式下提供BuildingManager的核心功能
    /// 使得BuildingUI在本地模式下可以正常工作
    /// </summary>
    public class LocalBuildingManagerAdapter : MonoBehaviour
    {
        private LocalPlayer _localPlayer;
        private IEconomyProvider _economy;
        private List<BuildingData> _ownedBuildings = new List<BuildingData>();

        // 事件
        public event Action<int, int> OnBuildingBuilt; // slotIndex, buildingIndex

        public int PlayerId => _localPlayer != null ? _localPlayer.PlayerId : 0;

        /// <summary>
        /// 初始化适配器
        /// </summary>
        public void Initialize(LocalPlayer localPlayer)
        {
            _localPlayer = localPlayer;
            _economy = localPlayer?.GetComponent<LocalPlayerEconomyAdapter>();
        }

        /// <summary>
        /// 尝试建造建筑
        /// </summary>
        public void TryBuildBuilding(BuildingData buildingData, int slotIndex)
        {
            if (buildingData == null)
            {
                Debug.LogWarning("[LocalBuildingManager] Building data is null");
                return;
            }

            // 检查金币
            if (_economy != null && !_economy.HasEnoughGold(buildingData.goldCost))
            {
                Debug.LogWarning($"[LocalBuildingManager] Not enough gold. Need {buildingData.goldCost}, have {_economy.Gold}");
                return;
            }

            // 扣除金币
            if (_economy != null && _economy.SpendGold(buildingData.goldCost))
            {
                _ownedBuildings.Add(buildingData);
                OnBuildingBuilt?.Invoke(slotIndex, _ownedBuildings.Count - 1);

                // 如果建筑提供收入，增加收入
                if (buildingData.providesIncome && buildingData.incomePerSecond > 0)
                {
                    _localPlayer?.AddIncome(buildingData.incomePerSecond);
                }

                Debug.Log($"[LocalBuildingManager] Built {buildingData.buildingName} at slot {slotIndex}");
            }
        }

        /// <summary>
        /// 检查前置建筑条件
        /// </summary>
        public bool CheckPrerequisites(BuildingData buildingData)
        {
            if (buildingData.requiredBuildings == null || buildingData.requiredBuildings.Length == 0)
                return true;

            return buildingData.CheckPrerequisites(_ownedBuildings.ToArray());
        }

        /// <summary>
        /// 获取缺少的前置建筑
        /// </summary>
        public List<BuildingData> GetMissingPrerequisites(BuildingData buildingData)
        {
            List<BuildingData> missing = new List<BuildingData>();

            if (buildingData.requiredBuildings == null)
                return missing;

            foreach (var required in buildingData.requiredBuildings)
            {
                bool hasBuilding = false;
                foreach (var owned in _ownedBuildings)
                {
                    if (owned == required)
                    {
                        hasBuilding = true;
                        break;
                    }
                }

                if (!hasBuilding)
                {
                    missing.Add(required);
                }
            }

            return missing;
        }

        /// <summary>
        /// 获取已建造建筑列表
        /// </summary>
        public List<BuildingData> GetOwnedBuildings()
        {
            return new List<BuildingData>(_ownedBuildings);
        }
    }
}
