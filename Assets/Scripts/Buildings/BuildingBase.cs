using UnityEngine;
using Unity.Netcode;
using CastleWars.Data;
using CastleWars.Units;

namespace CastleWars.Buildings
{
    /// <summary>
    /// 建筑基类 - 负责生产单位
    /// </summary>
    public class BuildingBase : NetworkBehaviour
    {
        [Header("建筑配置")]
        public BuildingData buildingData;

        [Header("所属玩家")]
        public NetworkVariable<int> ownerId = new NetworkVariable<int>();

        [Header("生产设置")]
        public Transform spawnPoint; // 单位生成位置

        // 生产状态
        private float productionTimer = 0f;
        private bool isProducing = true;

        // 对象池
        private UnitPool unitPool;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsServer)
            {
                // 初始化对象池
                if (buildingData.producedUnit != null)
                {
                    unitPool = new UnitPool(buildingData.producedUnit.prefab, 5);
                }
            }
        }

        private void Update()
        {
            // 只在服务器上执行生产逻辑
            if (!IsServer) return;

            if (isProducing && buildingData.producedUnit != null)
            {
                productionTimer += Time.deltaTime;

                if (productionTimer >= buildingData.productionInterval)
                {
                    ProduceUnit();
                    productionTimer = 0f;
                }
            }
        }

        /// <summary>
        /// 生产单位
        /// </summary>
        private void ProduceUnit()
        {
            if (buildingData.producedUnit == null) return;

            Vector3 spawnPos = spawnPoint != null ? spawnPoint.position : transform.position + Vector3.forward;

            // 从对象池获取单位
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
            // 播放生产特效
            PlayProductionEffect();
            // 播放生产音效
            PlayProductionSound();
        }

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
