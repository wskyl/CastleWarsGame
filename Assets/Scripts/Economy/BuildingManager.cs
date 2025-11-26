using UnityEngine;
using Unity.Netcode;
using CastleWars.Data;
using CastleWars.Buildings;
using System.Collections.Generic;

namespace CastleWars.Economy
{
    /// <summary>
    /// 建筑管理器 - 处理建筑的建造和管理
    /// </summary>
    public class BuildingManager : NetworkBehaviour
    {
        [Header("建筑槽位")]
        public Transform[] buildingSlots; // 可建造建筑的位置

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
            if (slotIndex < 0 || slotIndex >= buildingSlots.Length)
            {
                Debug.LogWarning("Invalid building slot index");
                return;
            }

            // 检查槽位是否已占用
            if (IsSlotOccupied(slotIndex))
            {
                Debug.LogWarning("Building slot already occupied");
                return;
            }

            // 检查前置条件
            if (!CheckPrerequisites(buildingData))
            {
                Debug.LogWarning("Prerequisites not met");
                return;
            }

            // 检查金币
            if (!economy.HasEnoughGold(buildingData.goldCost))
            {
                Debug.LogWarning("Not enough gold");
                return;
            }

            // 请求服务器建造
            BuildBuildingServerRpc(buildingData.name, slotIndex);
        }

        [ServerRpc(RequireOwnership = false)]
        private void BuildBuildingServerRpc(string buildingDataName, int slotIndex, ServerRpcParams rpcParams = default)
        {
            // ��载建筑数据（实际项目中需要从资源管理器获取）
            BuildingData buildingData = Resources.Load<BuildingData>($"Buildings/{buildingDataName}");
            if (buildingData == null)
            {
                Debug.LogError($"Building data not found: {buildingDataName}");
                return;
            }

            // 再次验证（防止作弊）
            if (!economy.SpendGold(buildingData.goldCost))
            {
                return;
            }

            // 生成建筑
            Vector3 spawnPos = buildingSlots[slotIndex].position;
            GameObject buildingObj = Instantiate(buildingData.prefab, spawnPos, Quaternion.identity);

            // 设置建筑
            BuildingBase building = buildingObj.GetComponent<BuildingBase>();
            if (building != null)
            {
                building.buildingData = buildingData;
                building.ownerId.Value = playerId;
                ownedBuildings.Add(building);
            }

            // 生成网络对象
            NetworkObject netObj = buildingObj.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                netObj.Spawn();
            }

            // 通知所有客户端
            OnBuildingBuiltClientRpc(slotIndex);
        }

        [ClientRpc]
        private void OnBuildingBuiltClientRpc(int slotIndex)
        {
            // 播放建造音效
            // 播放建造特效
            Debug.Log($"Building constructed at slot {slotIndex}");
        }

        private bool IsSlotOccupied(int slotIndex)
        {
            // 检查槽位附近是否已有建筑
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

        private bool CheckPrerequisites(BuildingData buildingData)
        {
            // 检查是否已建造前置建筑
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
