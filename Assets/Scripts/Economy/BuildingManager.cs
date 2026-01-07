using UnityEngine;
using CastleWars.Data;
using CastleWars.Buildings;
using CastleWars.Core;
using System.Collections.Generic;

#if UNITY_NETCODE
using Unity.Netcode;
#endif

namespace CastleWars.Economy
{
    /// <summary>
    /// 建筑管理器 - 处理建筑的建造和管理
    /// 支持本地模式和网络模式
    /// </summary>
#if UNITY_NETCODE
    public class BuildingManager : NetworkBehaviour
#else
    public class BuildingManager : MonoBehaviour
#endif
    {
        [Header("建筑槽位")]
        public Transform[] buildingSlots;

        [Header("所属玩家")]
        public int playerId;

        private PlayerEconomy economy;
        private List<BuildingBase> ownedBuildings = new List<BuildingBase>();

        // 本地模式判断
        private bool IsLocalMode => LocalGameMode.IsLocalMode;

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
            if (economy != null && !economy.HasEnoughGold(buildingData.goldCost))
            {
                Debug.LogWarning("Not enough gold");
                return;
            }

            if (IsLocalMode)
            {
                // 本地模式直接建造
                BuildBuildingLocal(buildingData, slotIndex);
            }
#if UNITY_NETCODE
            else
            {
                // 网络模式请求服务器建造
                BuildBuildingServerRpc(buildingData.name, slotIndex);
            }
#endif
        }

        /// <summary>
        /// 本地模式建造建筑
        /// </summary>
        private void BuildBuildingLocal(BuildingData buildingData, int slotIndex)
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
                buildingObj.name = buildingData.name;
            }

            // 设置建筑
            BuildingBase building = buildingObj.GetComponent<BuildingBase>();
            if (building == null)
            {
                building = buildingObj.AddComponent<BuildingBase>();
            }

            building.buildingData = buildingData;
#if UNITY_NETCODE
            building.ownerId.Value = playerId;
#else
            building.OwnerIdValue = playerId;
#endif
            ownedBuildings.Add(building);

            OnBuildingBuiltLocal(slotIndex);
        }

        private void OnBuildingBuiltLocal(int slotIndex)
        {
            Debug.Log($"Building constructed at slot {slotIndex}");
            // 播放建造音效和特效
        }

#if UNITY_NETCODE
        [ServerRpc(RequireOwnership = false)]
        private void BuildBuildingServerRpc(string buildingDataName, int slotIndex, ServerRpcParams rpcParams = default)
        {
            BuildingData buildingData = Resources.Load<BuildingData>($"Buildings/{buildingDataName}");
            if (buildingData == null)
            {
                Debug.LogError($"Building data not found: {buildingDataName}");
                return;
            }

            if (economy != null && !economy.SpendGold(buildingData.goldCost))
            {
                return;
            }

            Vector3 spawnPos = buildingSlots[slotIndex].position;
            GameObject buildingObj = Instantiate(buildingData.prefab, spawnPos, Quaternion.identity);

            BuildingBase building = buildingObj.GetComponent<BuildingBase>();
            if (building != null)
            {
                building.buildingData = buildingData;
                building.ownerId.Value = playerId;
                ownedBuildings.Add(building);
            }

            NetworkObject netObj = buildingObj.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                netObj.Spawn();
            }

            OnBuildingBuiltClientRpc(slotIndex);
        }

        [ClientRpc]
        private void OnBuildingBuiltClientRpc(int slotIndex)
        {
            Debug.Log($"Building constructed at slot {slotIndex}");
        }
#endif

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
