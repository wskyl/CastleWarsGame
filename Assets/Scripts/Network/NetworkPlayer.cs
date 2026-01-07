using UnityEngine;
using System;
using Unity.Netcode;
using CastleWars.Economy;

namespace CastleWars.Core
{
    /// <summary>
    /// 网络玩家控制器
    /// 管理玩家信息、经济系统、建筑系统的初始化
    /// </summary>
    public class NetworkPlayer : NetworkBehaviour
    {
        [Header("玩家信息")]
        private NetworkVariable<int> _playerId = new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        private NetworkVariable<FixedString64Bytes> _playerName = new NetworkVariable<FixedString64Bytes>(
            default,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        private NetworkVariable<bool> _isReady = new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        // 组件引用
        private PlayerEconomy _economy;
        private BuildingManager _buildingManager;

        // 事件
        public event Action<int> OnPlayerIdChanged;
        public event Action<string> OnPlayerNameChanged;
        public event Action<bool> OnReadyStateChanged;

        // 属性
        public int PlayerId => _playerId.Value;
        public string PlayerName => _playerName.Value.ToString();
        public bool IsReady => _isReady.Value;

        private void Awake()
        {
            _economy = GetComponent<PlayerEconomy>();
            _buildingManager = GetComponent<BuildingManager>();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            // 订阅网络变量变化
            _playerId.OnValueChanged += HandlePlayerIdChanged;
            _playerName.OnValueChanged += HandlePlayerNameChanged;
            _isReady.OnValueChanged += HandleReadyStateChanged;

            if (IsOwner)
            {
                InitializeLocalPlayer();
            }

            // 初始化组件
            if (_buildingManager != null)
            {
                _buildingManager.Initialize(_playerId.Value);
            }

            if (_economy != null)
            {
                _economy.Initialize(_playerId.Value);
            }

            Debug.Log($"[NetworkPlayer] 玩家 {_playerId.Value} 网络生成完成");
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            _playerId.OnValueChanged -= HandlePlayerIdChanged;
            _playerName.OnValueChanged -= HandlePlayerNameChanged;
            _isReady.OnValueChanged -= HandleReadyStateChanged;

            // 清理事件订阅
            if (_economy != null)
            {
                _economy.OnGoldChanged -= OnGoldChanged;
                _economy.OnIncomeChanged -= OnIncomeChanged;
            }
        }

        /// <summary>
        /// 初始化玩家（服务器调用）
        /// </summary>
        public void Initialize(int playerId)
        {
            if (!IsServer) return;

            _playerId.Value = playerId;
            _playerName.Value = $"Player {playerId}";

            Debug.Log($"[NetworkPlayer] 初始化玩家: {playerId}");
        }

        /// <summary>
        /// 设置玩家名称
        /// </summary>
        [ServerRpc]
        public void SetPlayerNameServerRpc(string name)
        {
            _playerName.Value = name;
        }

        /// <summary>
        /// 设置准备状态
        /// </summary>
        [ServerRpc]
        public void SetReadyServerRpc(bool ready)
        {
            _isReady.Value = ready;
        }

        #region 本地玩家初始化

        private void InitializeLocalPlayer()
        {
            Debug.Log($"[NetworkPlayer] 本地玩家初始化: Player {_playerId.Value}");

            // 订阅经济系统事件
            if (_economy != null)
            {
                _economy.OnGoldChanged += OnGoldChanged;
                _economy.OnIncomeChanged += OnIncomeChanged;
            }

            // 设置相机跟随等本地操作
            SetupLocalCamera();
        }

        private void SetupLocalCamera()
        {
            // 根据玩家ID设置相机位置
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                Vector3 cameraPos = _playerId.Value == 1
                    ? new Vector3(-10, 10, -10)
                    : new Vector3(10, 10, 10);

                mainCamera.transform.position = cameraPos;
                mainCamera.transform.LookAt(Vector3.zero);
            }
        }

        #endregion

        #region 经济事件处理

        private void OnGoldChanged(int newGold)
        {
            // 可以在这里更新UI或触发其他逻辑
            Debug.Log($"[NetworkPlayer] Player {_playerId.Value} 金币: {newGold}");
        }

        private void OnIncomeChanged(int newIncome)
        {
            Debug.Log($"[NetworkPlayer] Player {_playerId.Value} 收入: {newIncome}/秒");
        }

        #endregion

        #region 网络变量变化处理

        private void HandlePlayerIdChanged(int previousValue, int newValue)
        {
            OnPlayerIdChanged?.Invoke(newValue);

            // 重新初始化依赖玩家ID的组件
            if (_buildingManager != null)
            {
                _buildingManager.Initialize(newValue);
            }

            if (_economy != null)
            {
                _economy.Initialize(newValue);
            }
        }

        private void HandlePlayerNameChanged(FixedString64Bytes previousValue, FixedString64Bytes newValue)
        {
            OnPlayerNameChanged?.Invoke(newValue.ToString());
        }

        private void HandleReadyStateChanged(bool previousValue, bool newValue)
        {
            OnReadyStateChanged?.Invoke(newValue);
        }

        #endregion

        #region 工具方法

        /// <summary>
        /// 获取玩家经济组件
        /// </summary>
        public PlayerEconomy GetEconomy()
        {
            return _economy;
        }

        /// <summary>
        /// 获取建筑管理器
        /// </summary>
        public BuildingManager GetBuildingManager()
        {
            return _buildingManager;
        }

        /// <summary>
        /// 检查是否是本地玩家
        /// </summary>
        public bool IsLocalPlayer()
        {
            return IsOwner;
        }

        /// <summary>
        /// 获取敌方玩家ID
        /// </summary>
        public int GetEnemyPlayerId()
        {
            return _playerId.Value == 1 ? 2 : 1;
        }

        #endregion
    }
}
