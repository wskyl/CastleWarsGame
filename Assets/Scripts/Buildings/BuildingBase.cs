using UnityEngine;
using CastleWars.Data;
using CastleWars.Units;
using CastleWars.Core;

#if UNITY_NETCODE
using Unity.Netcode;
#endif

namespace CastleWars.Buildings
{
    /// <summary>
    /// 建筑基类 - 负责生产单位
    /// 支持本地模式和网络模式
    /// </summary>
#if UNITY_NETCODE
    public class BuildingBase : NetworkBehaviour
#else
    public class BuildingBase : MonoBehaviour
#endif
    {
        [Header("建筑配置")]
        public BuildingData buildingData;

        [Header("所属玩家")]
#if UNITY_NETCODE
        public NetworkVariable<int> ownerId = new NetworkVariable<int>();
#else
        private int _ownerId;
        public int OwnerIdValue
        {
            get => _ownerId;
            set => _ownerId = value;
        }
#endif

        [Header("生产设置")]
        public Transform spawnPoint; // 单位生成位置

        // 生产状态
        private float productionTimer = 0f;
        private bool isProducing = true;

        // 对象池
        private UnitPool unitPool;

        // 本地模式判断
        private bool IsLocalMode => LocalGameMode.IsLocalMode;

#if UNITY_NETCODE
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsServer)
            {
                InitializePool();
            }
        }
#endif

        private void Start()
        {
            // 本地模式初始化
            if (IsLocalMode)
            {
                InitializePool();
            }
        }

        private void InitializePool()
        {
            if (buildingData != null && buildingData.producedUnit != null && buildingData.producedUnit.prefab != null)
            {
                unitPool = new UnitPool(buildingData.producedUnit.prefab, 5);
            }
        }

        private void Update()
        {
            if (IsLocalMode)
            {
                UpdateLocalMode();
            }
#if UNITY_NETCODE
            else
            {
                UpdateNetworkMode();
            }
#endif
        }

        private void UpdateLocalMode()
        {
            if (isProducing && buildingData != null && buildingData.producedUnit != null)
            {
                productionTimer += Time.deltaTime;

                if (productionTimer >= buildingData.productionInterval)
                {
                    ProduceUnitLocal();
                    productionTimer = 0f;
                }
            }
        }

#if UNITY_NETCODE
        private void UpdateNetworkMode()
        {
            // 只在服务器上执行生产逻辑
            if (!IsServer) return;

            if (isProducing && buildingData != null && buildingData.producedUnit != null)
            {
                productionTimer += Time.deltaTime;

                if (productionTimer >= buildingData.productionInterval)
                {
                    ProduceUnitNetwork();
                    productionTimer = 0f;
                }
            }
        }
#endif

        /// <summary>
        /// 本地模式生产单位
        /// </summary>
        private void ProduceUnitLocal()
        {
            if (buildingData == null || buildingData.producedUnit == null) return;

            Vector3 spawnPos = spawnPoint != null ? spawnPoint.position : transform.position + Vector3.forward;

            // 使用LocalUnitSpawner生成单位
            if (LocalUnitSpawner.Instance != null)
            {
#if UNITY_NETCODE
                LocalUnitSpawner.Instance.SpawnUnit(ownerId.Value, buildingData.producedUnit);
#else
                LocalUnitSpawner.Instance.SpawnUnit(_ownerId, buildingData.producedUnit);
#endif
            }
            else
            {
                // 备用方案：直接从对象池获取
                if (unitPool != null)
                {
                    GameObject unitObj = unitPool.Get();
                    unitObj.transform.position = spawnPos;

                    LocalUnit localUnit = unitObj.GetComponent<LocalUnit>();
                    if (localUnit == null)
                    {
                        localUnit = unitObj.AddComponent<LocalUnit>();
                    }
#if UNITY_NETCODE
                    localUnit.Initialize(ownerId.Value, buildingData.producedUnit);
#else
                    localUnit.Initialize(_ownerId, buildingData.producedUnit);
#endif
                }
            }

            // 播放生产特效
            PlayProductionEffect();
            PlayProductionSound();
        }

#if UNITY_NETCODE
        /// <summary>
        /// 网络模式生产单位
        /// </summary>
        private void ProduceUnitNetwork()
        {
            if (buildingData == null || buildingData.producedUnit == null) return;

            Vector3 spawnPos = spawnPoint != null ? spawnPoint.position : transform.position + Vector3.forward;

            // 从对象池获取单位
            if (unitPool == null) return;

            GameObject unitObj = unitPool.Get();
            unitObj.transform.position = spawnPos;

            // 设置单位所属
            UnitBase unit = unitObj.GetComponent<UnitBase>();
            if (unit != null)
            {
                unit.ownerId.Value = ownerId.Value;
                unit.unitData = buildingData.producedUnit;
            }

            // 生成网络对象
            NetworkObject netObj = unitObj.GetComponent<NetworkObject>();
            if (netObj != null && !netObj.IsSpawned)
            {
                netObj.Spawn();
            }

            // 通知客户端播放生产特效
            OnUnitProducedClientRpc();
        }

        [ClientRpc]
        private void OnUnitProducedClientRpc()
        {
            PlayProductionEffect();
            PlayProductionSound();
        }
#endif

        private void PlayProductionEffect()
        {
            // 实现生产特效
        }

        private void PlayProductionSound()
        {
            // 实现生产音效
        }

        /// <summary>
        /// 暂停生产
        /// </summary>
        public void PauseProduction()
        {
            isProducing = false;
        }

        /// <summary>
        /// 恢复生产
        /// </summary>
        public void ResumeProduction()
        {
            isProducing = true;
        }

        private void OnDrawGizmosSelected()
        {
            if (spawnPoint != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(spawnPoint.position, 0.5f);
                Gizmos.DrawLine(transform.position, spawnPoint.position);
            }
        }
    }

    /// <summary>
    /// 简单的单位对象池
    /// </summary>
    public class UnitPool
    {
        private GameObject prefab;
        private System.Collections.Generic.Queue<GameObject> pool;

        public UnitPool(GameObject prefab, int initialSize)
        {
            this.prefab = prefab;
            this.pool = new System.Collections.Generic.Queue<GameObject>();

            for (int i = 0; i < initialSize; i++)
            {
                CreateNew();
            }
        }

        private GameObject CreateNew()
        {
            GameObject obj = Object.Instantiate(prefab);
            obj.SetActive(false);
            pool.Enqueue(obj);
            return obj;
        }

        public GameObject Get()
        {
            if (pool.Count == 0)
            {
                CreateNew();
            }

            GameObject obj = pool.Dequeue();
            obj.SetActive(true);
            return obj;
        }

        public void Return(GameObject obj)
        {
            obj.SetActive(false);
            pool.Enqueue(obj);
        }
    }
}
