using UnityEngine;
using System;
using System.Collections.Generic;
using Unity.Netcode;
using CastleWars.Data;
using CastleWars.Buildings;

namespace CastleWars.Economy
{
    /// <summary>
    /// 建筑管理器
    /// 处理建筑建造、前置条件检查、槽位管理
    /// </summary>
    public class BuildingManager : NetworkBehaviour
    {
        [Header("建筑槽位")]
        [Tooltip("建筑槽位列表")]
        [SerializeField] private Transform[] buildingSlots;

        [Header("可建造建筑")]
        [Tooltip("所有可建造的建筑数据")]
        [SerializeField] private BuildingData[] availableBuildings;

        // 玩家ID
        private int _playerId;

        // 经济组件引用
        private PlayerEconomy _economy;

        // 已建造的建筑
        private List<BuildingBase> _ownedBuildings = new List<BuildingBase>();

        // 槽位占用状态
        private Dictionary<int, BuildingBase> _slotOccupancy = new Dictionary<int, BuildingBase>();

        // 事件
        public event Action<BuildingBase, int> OnBuildingBuilt; // 建筑, 槽位索引
        public event Action<BuildingData, string> OnBuildingFailed; // 建筑数据, 失败原因

        // 属性
        public int PlayerId => _playerId;
        public int OwnedBuildingsCount => _ownedBuildings.Count;
        public int AvailableSlotsCount => buildingSlots != null ? buildingSlots.Length : 0;

        private void Awake()
        {
            _economy = GetComponent<PlayerEconomy>();
        }

        /// <summary>
        /// 初始化建筑管理器
        /// </summary>
        public void Initialize(int playerId)
        {
            _playerId = playerId;
            _ownedBuildings.Clear();
            _slotOccupancy.Clear();

            Debug.Log($"[BuildingManager] 初始化 - Player {playerId}, 槽位数: {AvailableSlotsCount}");
        }

        #region 建筑建造

        /// <summary>
        /// 尝试建造建筑
        /// </summary>
        /// <param name="buildingData">建筑数据</param>
        /// <param name="slotIndex">槽位索引</param>
        public void TryBuildBuilding(BuildingData buildingData, int slotIndex)
        {
            // 验证参数
            if (buildingData == null)
            {
                OnBuildingFailed?.Invoke(null, "建筑数据为空");
                return;
            }

            // 检查槽位有效性
            if (!IsValidSlot(slotIndex))
            {
                OnBuildingFailed?.Invoke(buildingData, "无效的建筑槽位");
                return;
            }

            // 检查槽位占用
            if (IsSlotOccupied(slotIndex))
            {
                OnBuildingFailed?.Invoke(buildingData, "该槽位已被占用");
                return;
            }

            // 检查前置条件
            if (!CheckPrerequisites(buildingData))
            {
                OnBuildingFailed?.Invoke(buildingData, $"需要先建造: {buildingData.GetPrerequisiteNames()}");
                return;
            }

            // 检查金币
            if (_economy != null && !_economy.HasEnoughGold(buildingData.goldCost))
            {
                OnBuildingFailed?.Invoke(buildingData, $"金币不足 (需要 {buildingData.goldCost})");
                return;
            }

            // 发送建造请求
            BuildBuildingServerRpc(GetBuildingIndex(buildingData), slotIndex);
        }

        /// <summary>
        /// 服务器端建造建筑
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        private void BuildBuildingServerRpc(int buildingIndex, int slotIndex, ServerRpcParams rpcParams = default)
        {
            if (buildingIndex < 0 || buildingIndex >= availableBuildings.Length)
            {
                Debug.LogError($"[BuildingManager] 无效的建筑索引: {buildingIndex}");
                return;
            }

            BuildingData buildingData = availableBuildings[buildingIndex];

            // 再次验证（服务器权威）
            if (IsSlotOccupied(slotIndex))
            {
                BuildingFailedClientRpc(buildingIndex, "槽位已被占用");
                return;
            }

            if (_economy != null && !_economy.SpendGold(buildingData.goldCost))
            {
                BuildingFailedClientRpc(buildingIndex, "金币不足");
                return;
            }

            // 执行建造
            BuildBuilding(buildingData, slotIndex);
        }

        /// <summary>
        /// 执行建造
        /// </summary>
        private void BuildBuilding(BuildingData buildingData, int slotIndex)
        {
            Vector3 spawnPos = buildingSlots[slotIndex].position;

            // 根据玩家方向设置建筑朝向
            Quaternion rotation = _playerId == 1 ? Quaternion.identity : Quaternion.Euler(0, 180, 0);

            // 生成建筑
            GameObject buildingObj;
            if (buildingData.prefab != null)
            {
                buildingObj = Instantiate(buildingData.prefab, spawnPos, rotation);
            }
            else
            {
                // 创建默认建筑
                buildingObj = CreateDefaultBuilding(buildingData, spawnPos, rotation);
            }

            // 初始化建筑组件
            BuildingBase building = buildingObj.GetComponent<BuildingBase>();
            if (building == null)
            {
                building = buildingObj.AddComponent<BuildingBase>();
            }

            building.Initialize(_playerId, buildingData);

            // 生成网络对象
            NetworkObject netObj = buildingObj.GetComponent<NetworkObject>();
            if (netObj == null)
            {
                netObj = buildingObj.AddComponent<NetworkObject>();
            }

            if (!netObj.IsSpawned)
            {
                netObj.Spawn();
            }

            // 记录建筑
            _ownedBuildings.Add(building);
            _slotOccupancy[slotIndex] = building;

            Debug.Log($"[BuildingManager] 建造成功: {buildingData.buildingName} 在槽位 {slotIndex}");

            // 通知客户端
            BuildingBuiltClientRpc(slotIndex, GetBuildingIndex(buildingData));
        }

        /// <summary>
        /// 创建默认建筑对象
        /// </summary>
        private GameObject CreateDefaultBuilding(BuildingData data, Vector3 position, Quaternion rotation)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.transform.position = position;
            obj.transform.rotation = rotation;
            obj.transform.localScale = new Vector3(2, 3, 2);
            obj.name = data.buildingName;

            // 设置颜色
            Renderer renderer = obj.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = _playerId == 1 ? Color.blue : Color.red;
                renderer.material = mat;
            }

            return obj;
        }

        #endregion

        #region 前置条件检查

        /// <summary>
        /// 检查前置建筑条件
        /// </summary>
        private bool CheckPrerequisites(BuildingData buildingData)
        {
            if (buildingData.requiredBuildings == null || buildingData.requiredBuildings.Length == 0)
                return true;

            // 获取已建造建筑的数据列表
            BuildingData[] builtBuildingDatas = new BuildingData[_ownedBuildings.Count];
            for (int i = 0; i < _ownedBuildings.Count; i++)
            {
                builtBuildingDatas[i] = _ownedBuildings[i].buildingData;
            }

            return buildingData.CheckPrerequisites(builtBuildingDatas);
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
                    if (owned.buildingData == required)
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

        #endregion

        #region 槽位管理

        /// <summary>
        /// 检查槽位是否有效
        /// </summary>
        private bool IsValidSlot(int slotIndex)
        {
            return buildingSlots != null && slotIndex >= 0 && slotIndex < buildingSlots.Length;
        }

        /// <summary>
        /// 检查槽位是否被占用
        /// </summary>
        public bool IsSlotOccupied(int slotIndex)
        {
            if (_slotOccupancy.ContainsKey(slotIndex) && _slotOccupancy[slotIndex] != null)
            {
                return true;
            }

            // 物理检测备用
            if (buildingSlots != null && slotIndex < buildingSlots.Length)
            {
                Collider[] colliders = Physics.OverlapSphere(buildingSlots[slotIndex].position, 1f);
                foreach (Collider col in colliders)
                {
                    if (col.GetComponent<BuildingBase>() != null)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// 获取空闲槽位数量
        /// </summary>
        public int GetFreeSlotCount()
        {
            int count = 0;
            for (int i = 0; i < buildingSlots.Length; i++)
            {
                if (!IsSlotOccupied(i))
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// 获取第一个空闲槽位
        /// </summary>
        public int GetFirstFreeSlot()
        {
            for (int i = 0; i < buildingSlots.Length; i++)
            {
                if (!IsSlotOccupied(i))
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// 获取槽位位置
        /// </summary>
        public Vector3 GetSlotPosition(int slotIndex)
        {
            if (IsValidSlot(slotIndex))
            {
                return buildingSlots[slotIndex].position;
            }
            return Vector3.zero;
        }

        #endregion

        #region 网络回调

        [ClientRpc]
        private void BuildingBuiltClientRpc(int slotIndex, int buildingIndex)
        {
            Debug.Log($"[BuildingManager] 建筑建造完成 - 槽位: {slotIndex}");

            // 触发事件
            if (buildingIndex >= 0 && buildingIndex < availableBuildings.Length)
            {
                // 查找对应的建筑实例
                if (_slotOccupancy.TryGetValue(slotIndex, out BuildingBase building))
                {
                    OnBuildingBuilt?.Invoke(building, slotIndex);
                }
            }
        }

        [ClientRpc]
        private void BuildingFailedClientRpc(int buildingIndex, string reason)
        {
            if (buildingIndex >= 0 && buildingIndex < availableBuildings.Length)
            {
                OnBuildingFailed?.Invoke(availableBuildings[buildingIndex], reason);
            }
        }

        #endregion

        #region 工具方法

        /// <summary>
        /// 获取建筑在可用建筑列表中的索引
        /// </summary>
        private int GetBuildingIndex(BuildingData data)
        {
            for (int i = 0; i < availableBuildings.Length; i++)
            {
                if (availableBuildings[i] == data)
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// 获取所有已建造的建筑
        /// </summary>
        public List<BuildingBase> GetOwnedBuildings()
        {
            return new List<BuildingBase>(_ownedBuildings);
        }

        /// <summary>
        /// 获取所有可建造的建筑
        /// </summary>
        public BuildingData[] GetAvailableBuildings()
        {
            return availableBuildings;
        }

        /// <summary>
        /// 获取可负担的建筑列表
        /// </summary>
        public List<BuildingData> GetAffordableBuildings()
        {
            List<BuildingData> affordable = new List<BuildingData>();

            foreach (var building in availableBuildings)
            {
                if (_economy != null && _economy.HasEnoughGold(building.goldCost) && CheckPrerequisites(building))
                {
                    affordable.Add(building);
                }
            }

            return affordable;
        }

        /// <summary>
        /// 检查是否拥有特定建筑
        /// </summary>
        public bool HasBuilding(BuildingData buildingData)
        {
            foreach (var building in _ownedBuildings)
            {
                if (building.buildingData == buildingData)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 统计特定建筑的数量
        /// </summary>
        public int CountBuilding(BuildingData buildingData)
        {
            int count = 0;
            foreach (var building in _ownedBuildings)
            {
                if (building.buildingData == buildingData)
                {
                    count++;
                }
            }
            return count;
        }

        #endregion
    }
}
