using UnityEngine;
using CastleWars.Data;

namespace CastleWars.Core
{
    /// <summary>
    /// 单位生成器 - 管理单位的生成（纯本地模式）
    /// </summary>
    public class UnitSpawner : MonoBehaviour
    {
        public static UnitSpawner Instance { get; private set; }

        [Header("默认单位预制体")]
        [SerializeField] private GameObject defaultUnitPrefab;

        [Header("默认单位数据")]
        [SerializeField] private UnitData defaultUnitData;

        [Header("生成设置")]
        [SerializeField] private Vector3 player1SpawnOffset = new Vector3(-15, 0, 0);
        [SerializeField] private Vector3 player2SpawnOffset = new Vector3(15, 0, 0);

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
        public UnitController SpawnUnit(int ownerId, UnitData unitData = null)
        {
            if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Playing)
            {
                return null;
            }

            Vector3 spawnPos = GetSpawnPosition(ownerId);
            UnitData data = unitData ?? defaultUnitData;

            GameObject unitObj = CreateUnitObject(spawnPos, data);

            UnitController controller = unitObj.GetComponent<UnitController>();
            if (controller == null)
            {
                controller = unitObj.AddComponent<UnitController>();
            }

            controller.Initialize(ownerId, data);

            Debug.Log($"[UnitSpawner] Spawned unit for player {ownerId} at {spawnPos}");

            return controller;
        }

        /// <summary>
        /// 获取生成位置
        /// </summary>
        private Vector3 GetSpawnPosition(int ownerId)
        {
            CastleController castle = GameManager.Instance?.GetOwnCastle(ownerId);

            if (castle != null)
            {
                Vector3 offset = ownerId == 1 ? new Vector3(3, 0, 0) : new Vector3(-3, 0, 0);
                return castle.transform.position + offset;
            }

            return ownerId == 1 ? player1SpawnOffset : player2SpawnOffset;
        }

        /// <summary>
        /// 创建单位对象
        /// </summary>
        private GameObject CreateUnitObject(Vector3 position, UnitData data)
        {
            GameObject unitObj;

            // 优先使用数据中的预制体
            if (data != null && data.prefab != null)
            {
                unitObj = Instantiate(data.prefab, position, Quaternion.identity);
            }
            // 使用默认预制体
            else if (defaultUnitPrefab != null)
            {
                unitObj = Instantiate(defaultUnitPrefab, position, Quaternion.identity);
            }
            // 创建简单的立方体单位
            else
            {
                unitObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                unitObj.transform.position = position;
                unitObj.transform.localScale = new Vector3(1, 1, 1);

                // 添加 Rigidbody
                Rigidbody rb = unitObj.GetComponent<Rigidbody>();
                if (rb == null)
                {
                    rb = unitObj.AddComponent<Rigidbody>();
                }
                rb.useGravity = false;
                rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;

                // 设置碰撞器
                CapsuleCollider col = unitObj.GetComponent<CapsuleCollider>();
                if (col != null)
                {
                    col.isTrigger = false;
                }
            }

            unitObj.name = "Unit";
            unitObj.tag = "Unit";

            return unitObj;
        }
    }
}
