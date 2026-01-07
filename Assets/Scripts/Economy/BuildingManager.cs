using UnityEngine;
using CastleWars.Data;
using CastleWars.Buildings;
using CastleWars.Core;
using System.Collections.Generic;

namespace CastleWars.Economy
{
    /// <summary>
    /// 建筑管理器 - 处理建筑的建造和管理（纯本地模式）
    /// </summary>
    public class BuildingManager : MonoBehaviour
    {
        [Header("建筑槽位")]
        public Transform[] buildingSlots;

        [Header("所属玩家")]
        public int playerId;

        private PlayerEconomy economy;
        private List<BuildingBase> ownedBuildings = new List<BuildingBase>();

        private void Awake()
        {
            economy = GetComponent<PlayerEconomy>();
        }

        /// <summary>
        /// 尝试建造建筑
        /// </summary>
        public void TryBuildBuilding(BuildingData buildingData, int slotIndex)
        {
            // 检查槽位是否有效
            if (buildingSlots == null || slotIndex < 0 || slotIndex >= buildingSlots.Length)
            {
                Debug.LogWarning("[BuildingManager] Invalid building slot index");
                return;
            }

            // 检查槽位是否已占用
            if (IsSlotOccupied(slotIndex))
            {
                Debug.LogWarning("[BuildingManager] Building slot already occupied");
                return;
            }

            // 检查前置条件
            if (!CheckPrerequisites(buildingData))
            {
                Debug.LogWarning("[BuildingManager] Prerequisites not met");
                return;
            }

            // 检查金币
            if (economy != null && !economy.HasEnoughGold(buildingData.goldCost))
            {
                Debug.LogWarning("[BuildingManager] Not enough gold");
                return;
            }

            BuildBuilding(buildingData, slotIndex);
        }

        /// <summary>
        /// 建造建筑
        /// </summary>
        private void BuildBuilding(BuildingData buildingData, int slotIndex)
        {
            if (buildingData == null) return;

            // 消耗金币
            if (economy != null && !economy.SpendGold(buildingData.goldCost))
            {
                return;
            }

            // 生成建筑
            Vector3 spawnPos = buildingSlots[slotIndex].position;
            GameObject buildingObj;

            if (buildingData.prefab != null)
            {
                buildingObj = Instantiate(buildingData.prefab, spawnPos, Quaternion.identity);
            }
            else
            {
                // 创建默认建筑对象
                buildingObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                buildingObj.transform.position = spawnPos;
                buildingObj.transform.localScale = new Vector3(3, 3, 3);
                buildingObj.name = buildingData.buildingName;
            }

            // 设置建筑
            BuildingBase building = buildingObj.GetComponent<BuildingBase>();
            if (building == null)
            {
                building = buildingObj.AddComponent<BuildingBase>();
            }

            building.buildingData = buildingData;
            building.Initialize(playerId);
            ownedBuildings.Add(building);

            Debug.Log($"[BuildingManager] Building constructed at slot {slotIndex}");
        }

        /// <summary>
        /// 检查槽位是否已被占用
        /// </summary>
        private bool IsSlotOccupied(int slotIndex)
        {
            if (buildingSlots == null || slotIndex >= buildingSlots.Length) return true;

            Collider[] colliders = Physics.OverlapSphere(buildingSlots[slotIndex].position, 1f);
            foreach (Collider col in colliders)
            {
                if (col.GetComponent<BuildingBase>() != null)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 检查前置条件
        /// </summary>
        private bool CheckPrerequisites(BuildingData buildingData)
        {
            if (buildingData.requiredBuildings != null && buildingData.requiredBuildings.Length > 0)
            {
                foreach (BuildingData required in buildingData.requiredBuildings)
                {
                    bool hasRequired = false;
                    foreach (BuildingBase building in ownedBuildings)
                    {
                        if (building.buildingData == required)
                        {
                            hasRequired = true;
                            break;
                        }
                    }

                    if (!hasRequired)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// 获取所有已建造的建筑
        /// </summary>
        public List<BuildingBase> GetOwnedBuildings()
        {
            return new List<BuildingBase>(ownedBuildings);
        }
    }
}
