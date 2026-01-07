using UnityEngine;
using System;
using System.Collections.Generic;
using Unity.Netcode;
using CastleWars.Data;
using CastleWars.Units;

namespace CastleWars.Buildings
{
    /// <summary>
    /// 建筑基类
    /// 负责自动生产单位、对象池管理
    /// </summary>
    public class BuildingBase : NetworkBehaviour
    {
        [Header("建筑配置")]
        [Tooltip("建筑数据")]
        public BuildingData buildingData;

        [Header("生产设置")]
        [Tooltip("单位生成位置")]
        [SerializeField] private Transform spawnPoint;

        [Tooltip("生成位置偏移")]
        [SerializeField] private Vector3 spawnOffset = Vector3.forward * 2f;

        // 网络同步变量
        private NetworkVariable<int> _ownerId = new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        private NetworkVariable<bool> _isProducing = new NetworkVariable<bool>(
            true,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        private NetworkVariable<float> _productionProgress = new NetworkVariable<float>(
            0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        // 对象池
        private UnitPool _unitPool;

        // 生产计时
        private float _productionTimer = 0f;

        // 事件
        public event Action<UnitBase> OnUnitProduced;
        public event Action<float> OnProductionProgress; // 0-1 进度

        // 属性
        public int OwnerId => _ownerId.Value;
        public bool IsProducing => _isProducing.Value;
        public float ProductionProgress => _productionProgress.Value;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsServer)
            {
                InitializePool();
            }

            _productionProgress.OnValueChanged += HandleProgressChanged;
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            _productionProgress.OnValueChanged -= HandleProgressChanged;

            // 清理对象池
            _unitPool?.Clear();
        }

        /// <summary>
        /// 初始化建筑
        /// </summary>
        public void Initialize(int ownerId, BuildingData data = null)
        {
            if (!IsServer) return;

            _ownerId.Value = ownerId;

            if (data != null)
            {
                buildingData = data;
            }

            InitializePool();

            Debug.Log($"[BuildingBase] 建筑初始化 - 所有者: Player {ownerId}, 建筑: {buildingData?.buildingName ?? "Unknown"}");
        }

        /// <summary>
        /// 初始化对象池
        /// </summary>
        private void InitializePool()
        {
            if (buildingData?.producedUnit?.prefab != null)
            {
                _unitPool = new UnitPool(buildingData.producedUnit.prefab, 5);
            }
        }

        private void Update()
        {
            if (!IsServer || !_isProducing.Value) return;
            if (buildingData == null || buildingData.producedUnit == null) return;

            _productionTimer += Time.deltaTime;

            // 更新生产进度
            _productionProgress.Value = Mathf.Clamp01(_productionTimer / buildingData.productionInterval);

            // 检查是否可以生产
            if (_productionTimer >= buildingData.productionInterval)
            {
                ProduceUnit();
                _productionTimer = 0f;
            }
        }

        #region 单位生产

        /// <summary>
        /// 生产单位
        /// </summary>
        private void ProduceUnit()
        {
            if (buildingData?.producedUnit?.prefab == null) return;

            Vector3 spawnPos = GetSpawnPosition();

            for (int i = 0; i < buildingData.productionCount; i++)
            {
                SpawnSingleUnit(spawnPos + Vector3.right * i);
            }

            // 通知客户端播放特效
            OnUnitProducedClientRpc();
        }

        /// <summary>
        /// 生成单个单位
        /// </summary>
        private void SpawnSingleUnit(Vector3 position)
        {
            GameObject unitObj;

            // 优先使用对象池
            if (_unitPool != null)
            {
                unitObj = _unitPool.Get();
                unitObj.transform.position = position;
            }
            else
            {
                unitObj = Instantiate(buildingData.producedUnit.prefab, position, Quaternion.identity);
            }

            // 初始化单位
            UnitBase unit = unitObj.GetComponent<UnitBase>();
            if (unit != null)
            {
                unit.Initialize(_ownerId.Value, buildingData.producedUnit);
            }

            // 生成网络对象
            NetworkObject netObj = unitObj.GetComponent<NetworkObject>();
            if (netObj != null && !netObj.IsSpawned)
            {
                netObj.Spawn();
            }

            OnUnitProduced?.Invoke(unit);

            Debug.Log($"[BuildingBase] 生产单位: {buildingData.producedUnit.unitName}");
        }

        /// <summary>
        /// 获取生成位置
        /// </summary>
        private Vector3 GetSpawnPosition()
        {
            if (spawnPoint != null)
            {
                return spawnPoint.position;
            }

            // 根据所有者方向确定生成位置
            float direction = _ownerId.Value == 1 ? 1f : -1f;
            return transform.position + new Vector3(direction * spawnOffset.z, spawnOffset.y, 0);
        }

        #endregion

        #region 生产控制

        /// <summary>
        /// 暂停生产
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void PauseProductionServerRpc()
        {
            _isProducing.Value = false;
        }

        /// <summary>
        /// 恢复生产
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void ResumeProductionServerRpc()
        {
            _isProducing.Value = true;
        }

        /// <summary>
        /// 直接暂停（服务器调用）
        /// </summary>
        public void PauseProduction()
        {
            if (!IsServer) return;
            _isProducing.Value = false;
        }

        /// <summary>
        /// 直接恢复（服务器调用）
        /// </summary>
        public void ResumeProduction()
        {
            if (!IsServer) return;
            _isProducing.Value = true;
        }

        #endregion

        #region 网络回调

        [ClientRpc]
        private void OnUnitProducedClientRpc()
        {
            PlayProductionEffect();
            PlayProductionSound();
        }

        private void HandleProgressChanged(float previousValue, float newValue)
        {
            OnProductionProgress?.Invoke(newValue);
        }

        #endregion

        #region 特效和音效

        private void PlayProductionEffect()
        {
            // 播放生产特效
        }

        private void PlayProductionSound()
        {
            // 播放生产音效
            AudioSource audioSource = GetComponent<AudioSource>();
            if (audioSource != null)
            {
                audioSource.Play();
            }
        }

        #endregion

        #region 工具方法

        /// <summary>
        /// 获取建筑数据
        /// </summary>
        public BuildingData GetBuildingData()
        {
            return buildingData;
        }

        /// <summary>
        /// 检查建筑是否属于指定玩家
        /// </summary>
        public bool BelongsTo(int playerId)
        {
            return _ownerId.Value == playerId;
        }

        /// <summary>
        /// 获取剩余生产时间
        /// </summary>
        public float GetRemainingProductionTime()
        {
            if (buildingData == null) return 0f;
            return buildingData.productionInterval - _productionTimer;
        }

        #endregion

        private void OnDrawGizmosSelected()
        {
            // 绘制生成点
            Vector3 spawnPos = spawnPoint != null ? spawnPoint.position : transform.position + spawnOffset;

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(spawnPos, 0.5f);
            Gizmos.DrawLine(transform.position, spawnPos);
        }
    }

    /// <summary>
    /// 单位对象池
    /// 优化单位生成性能
    /// </summary>
    public class UnitPool
    {
        private GameObject _prefab;
        private Queue<GameObject> _pool;
        private Transform _poolParent;

        public UnitPool(GameObject prefab, int initialSize)
        {
            _prefab = prefab;
            _pool = new Queue<GameObject>();

            // 创建对象池父对象
            _poolParent = new GameObject($"UnitPool_{prefab.name}").transform;

            // 预创建对象
            for (int i = 0; i < initialSize; i++)
            {
                CreateNew();
            }
        }

        /// <summary>
        /// 创建新对象
        /// </summary>
        private GameObject CreateNew()
        {
            GameObject obj = Object.Instantiate(_prefab, _poolParent);
            obj.SetActive(false);
            _pool.Enqueue(obj);
            return obj;
        }

        /// <summary>
        /// 从池中获取对象
        /// </summary>
        public GameObject Get()
        {
            if (_pool.Count == 0)
            {
                CreateNew();
            }

            GameObject obj = _pool.Dequeue();
            obj.SetActive(true);
            obj.transform.SetParent(null);
            return obj;
        }

        /// <summary>
        /// 归还对象到池中
        /// </summary>
        public void Return(GameObject obj)
        {
            if (obj == null) return;

            obj.SetActive(false);
            obj.transform.SetParent(_poolParent);
            _pool.Enqueue(obj);
        }

        /// <summary>
        /// 清空对象池
        /// </summary>
        public void Clear()
        {
            while (_pool.Count > 0)
            {
                GameObject obj = _pool.Dequeue();
                if (obj != null)
                {
                    Object.Destroy(obj);
                }
            }

            if (_poolParent != null)
            {
                Object.Destroy(_poolParent.gameObject);
            }
        }

        /// <summary>
        /// 获取池中对象数量
        /// </summary>
        public int Count => _pool.Count;
    }
}
