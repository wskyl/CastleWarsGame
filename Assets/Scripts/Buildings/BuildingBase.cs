using UnityEngine;
using CastleWars.Data;
using CastleWars.Core;
using System.Collections.Generic;

namespace CastleWars.Buildings
{
    /// <summary>
    /// 建筑基类 - 负责自动生产单位（纯本地模式）
    /// </summary>
    public class BuildingBase : MonoBehaviour
    {
        [Header("建筑配置")]
        public BuildingData buildingData;

        [Header("所属玩家")]
        public int OwnerId { get; private set; }

        [Header("生产设置")]
        [SerializeField] private Transform spawnPoint;

        // 生产状态
        private float productionTimer = 0f;
        private bool isProducing = true;

        // 属性
        public bool IsProducing => isProducing;
        public float ProductionProgress => buildingData != null ? productionTimer / buildingData.productionInterval : 0f;

        /// <summary>
        /// 初始化建筑
        /// </summary>
        public void Initialize(int ownerId)
        {
            OwnerId = ownerId;
            productionTimer = 0f;
            isProducing = true;

            SetTeamColor();

            Debug.Log($"[BuildingBase] Building initialized for player {ownerId}");
        }

        private void SetTeamColor()
        {
            Renderer renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = OwnerId == 1 ? new Color(0.3f, 0.5f, 0.9f) : new Color(0.9f, 0.3f, 0.3f);
            }
        }

        private void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Playing)
                return;

            if (!isProducing || buildingData == null || buildingData.producedUnit == null)
                return;

            productionTimer += Time.deltaTime;

            if (productionTimer >= buildingData.productionInterval)
            {
                ProduceUnit();
                productionTimer = 0f;
            }
        }

        /// <summary>
        /// 生产单位
        /// </summary>
        private void ProduceUnit()
        {
            if (buildingData == null || buildingData.producedUnit == null) return;

            if (UnitSpawner.Instance != null)
            {
                UnitSpawner.Instance.SpawnUnit(OwnerId, buildingData.producedUnit);
                PlayProductionEffect();
            }

            Debug.Log($"[BuildingBase] Produced unit for player {OwnerId}");
        }

        /// <summary>
        /// 播放生产特效
        /// </summary>
        private void PlayProductionEffect()
        {
            // TODO: 实现生产特效
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
    /// 简单的对象池
    /// </summary>
    public class ObjectPool
    {
        private GameObject prefab;
        private Queue<GameObject> pool;

        public ObjectPool(GameObject prefab, int initialSize)
        {
            this.prefab = prefab;
            this.pool = new Queue<GameObject>();

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
