using UnityEngine;
using CastleWars.Data;
using System.Collections.Generic;

namespace CastleWars.Core
{
    /// <summary>
    /// 本地单位生成器 - 在本地模式下处理单位生成
    /// </summary>
    public class LocalUnitSpawner : MonoBehaviour
    {
        public static LocalUnitSpawner Instance { get; private set; }

        [Header("生成设置")]
        [SerializeField] private Transform player1SpawnPoint;
        [SerializeField] private Transform player2SpawnPoint;

        [Header("单位预制体")]
        [SerializeField] private GameObject defaultUnitPrefab;
        [SerializeField] private List<UnitData> availableUnits = new List<UnitData>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// 生成单位
        /// </summary>
        public LocalUnit SpawnUnit(int playerId, UnitData unitData = null)
        {
            if (!LocalGameMode.IsLocalMode)
            {
                Debug.LogWarning("LocalUnitSpawner only works in local mode");
                return null;
            }

            // 确定生成位置
            Vector3 spawnPos = GetSpawnPosition(playerId);

            // 创建单位对象
            GameObject unitObj;
            if (unitData != null && unitData.prefab != null)
            {
                unitObj = Instantiate(unitData.prefab, spawnPos, Quaternion.identity);
            }
            else if (defaultUnitPrefab != null)
            {
                unitObj = Instantiate(defaultUnitPrefab, spawnPos, Quaternion.identity);
            }
            else
            {
                unitObj = CreateDefaultUnit(spawnPos);
            }

            // 添加本地单位组件
            LocalUnit localUnit = unitObj.GetComponent<LocalUnit>();
            if (localUnit == null)
            {
                localUnit = unitObj.AddComponent<LocalUnit>();
            }

            localUnit.Initialize(playerId, unitData);

            return localUnit;
        }

        private Vector3 GetSpawnPosition(int playerId)
        {
            if (playerId == 1 && player1SpawnPoint != null)
            {
                return player1SpawnPoint.position + GetRandomOffset();
            }
            else if (playerId == 2 && player2SpawnPoint != null)
            {
                return player2SpawnPoint.position + GetRandomOffset();
            }

            // 默认位置
            float x = playerId == 1 ? -18f : 18f;
            return new Vector3(x, 0, 0) + GetRandomOffset();
        }

        private Vector3 GetRandomOffset()
        {
            return new Vector3(0, 0, Random.Range(-2f, 2f));
        }

        private GameObject CreateDefaultUnit(Vector3 position)
        {
            GameObject unitObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            unitObj.transform.position = position;
            unitObj.transform.localScale = new Vector3(1f, 1f, 1f);
            unitObj.name = "Unit";

            // 添加刚体
            Rigidbody rb = unitObj.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;

            return unitObj;
        }

        /// <summary>
        /// 获取可用单位列表
        /// </summary>
        public List<UnitData> GetAvailableUnits()
        {
            return new List<UnitData>(availableUnits);
        }

        /// <summary>
        /// 根据索引获取单位数据
        /// </summary>
        public UnitData GetUnitData(int index)
        {
            if (index >= 0 && index < availableUnits.Count)
            {
                return availableUnits[index];
            }
            return null;
        }
    }
}
