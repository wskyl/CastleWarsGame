using UnityEngine;
using System;
using System.Collections.Generic;
using Unity.Netcode;

namespace CastleWars.Core
{
    /// <summary>
    /// 游戏状态枚举
    /// </summary>
    public enum GameState
    {
        Waiting,    // 等待玩家
        Playing,    // 游戏进行中
        Paused,     // 暂停
        Ended       // 游戏结束
    }

    /// <summary>
    /// 游戏结束原因枚举
    /// </summary>
    public enum GameEndReason
    {
        CastleDestroyed,    // 城堡被摧毁
        TimeLimit,          // 时间耗尽
        PlayerDisconnected, // 玩家断线
        Surrender           // 投降
    }

    /// <summary>
    /// 游戏管理器
    /// 负责游戏状态管理、玩家连接、胜负判定
    /// </summary>
    public class GameManager : NetworkBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("游戏设置")]
        [Tooltip("游戏时长（秒），默认10分钟")]
        [SerializeField] private float gameDuration = 600f;

        [Tooltip("城堡初始血量")]
        [SerializeField] private float castleMaxHealth = 5000f;

        [Header("玩家生成点")]
        [SerializeField] private Transform player1SpawnPoint;
        [SerializeField] private Transform player2SpawnPoint;

        [Header("预制体")]
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private GameObject castlePrefab;

        // 网络同步变量
        private NetworkVariable<GameState> _gameState = new NetworkVariable<GameState>(
            GameState.Waiting,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        private NetworkVariable<float> _gameTime = new NetworkVariable<float>(
            0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        private NetworkVariable<int> _player1CastleHealth = new NetworkVariable<int>();
        private NetworkVariable<int> _player2CastleHealth = new NetworkVariable<int>();

        // 玩家字典
        private Dictionary<ulong, NetworkPlayer> _players = new Dictionary<ulong, NetworkPlayer>();

        // 城堡引用
        private CastleController _player1Castle;
        private CastleController _player2Castle;

        // 事件
        public event Action<GameState> OnGameStateChanged;
        public event Action<int> OnGameEnded; // 参数为获胜玩家ID
        public event Action OnGameStarted;

        // 属性
        public GameState CurrentState => _gameState.Value;
        public float GameTime => _gameTime.Value;
        public float GameDuration => gameDuration;
        public float RemainingTime => Mathf.Max(0, gameDuration - _gameTime.Value);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            _gameState.OnValueChanged += HandleGameStateChanged;

            if (IsServer)
            {
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

                // 初始化城堡血量
                _player1CastleHealth.Value = (int)castleMaxHealth;
                _player2CastleHealth.Value = (int)castleMaxHealth;
            }
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            _gameState.OnValueChanged -= HandleGameStateChanged;

            if (IsServer && NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
            }
        }

        private void Update()
        {
            if (!IsServer) return;

            if (_gameState.Value == GameState.Playing)
            {
                _gameTime.Value += Time.deltaTime;

                // 检查时间限制
                if (_gameTime.Value >= gameDuration)
                {
                    EndGame(GameEndReason.TimeLimit);
                }
            }
        }

        #region 玩家连接管理

        private void OnClientConnected(ulong clientId)
        {
            Debug.Log($"[GameManager] 玩家连接: {clientId}");

            // 当两个玩家都连接时，自动开始游戏
            if (NetworkManager.Singleton.ConnectedClients.Count == 2 &&
                _gameState.Value == GameState.Waiting)
            {
                StartGame();
            }
        }

        private void OnClientDisconnected(ulong clientId)
        {
            Debug.Log($"[GameManager] 玩家断开: {clientId}");

            // 游戏进行中有玩家断线
            if (_gameState.Value == GameState.Playing)
            {
                EndGame(GameEndReason.PlayerDisconnected);
            }

            // 移除玩家
            if (_players.ContainsKey(clientId))
            {
                _players.Remove(clientId);
            }
        }

        #endregion

        #region 游戏流程控制

        /// <summary>
        /// 开始游戏
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void RequestStartGameServerRpc()
        {
            if (_gameState.Value == GameState.Waiting)
            {
                StartGame();
            }
        }

        private void StartGame()
        {
            if (!IsServer) return;

            Debug.Log("[GameManager] 游戏开始");

            // 生成玩家和城堡
            SpawnPlayersAndCastles();

            // 更新游戏状态
            _gameState.Value = GameState.Playing;
            _gameTime.Value = 0f;

            // 通知客户端
            OnGameStartedClientRpc();
        }

        private void SpawnPlayersAndCastles()
        {
            int playerIndex = 1;
            foreach (var client in NetworkManager.Singleton.ConnectedClients)
            {
                // 确定生成位置
                Vector3 spawnPos = playerIndex == 1
                    ? (player1SpawnPoint != null ? player1SpawnPoint.position : new Vector3(-15, 0, 0))
                    : (player2SpawnPoint != null ? player2SpawnPoint.position : new Vector3(15, 0, 0));

                // 生成玩家
                if (playerPrefab != null)
                {
                    GameObject playerObj = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
                    NetworkObject netObj = playerObj.GetComponent<NetworkObject>();

                    if (netObj != null)
                    {
                        netObj.SpawnAsPlayerObject(client.Key);
                    }

                    NetworkPlayer networkPlayer = playerObj.GetComponent<NetworkPlayer>();
                    if (networkPlayer != null)
                    {
                        networkPlayer.Initialize(playerIndex);
                        _players[client.Key] = networkPlayer;
                    }
                }

                // 生成城堡
                if (castlePrefab != null)
                {
                    Vector3 castlePos = playerIndex == 1
                        ? new Vector3(-20, 0, 0)
                        : new Vector3(20, 0, 0);

                    Quaternion castleRot = playerIndex == 1
                        ? Quaternion.identity
                        : Quaternion.Euler(0, 180, 0);

                    GameObject castleObj = Instantiate(castlePrefab, castlePos, castleRot);
                    NetworkObject castleNetObj = castleObj.GetComponent<NetworkObject>();

                    if (castleNetObj != null)
                    {
                        castleNetObj.Spawn();
                    }

                    CastleController castle = castleObj.GetComponent<CastleController>();
                    if (castle != null)
                    {
                        castle.Initialize(playerIndex, castleMaxHealth);

                        if (playerIndex == 1)
                            _player1Castle = castle;
                        else
                            _player2Castle = castle;
                    }
                }

                playerIndex++;
            }
        }

        /// <summary>
        /// 暂停游戏
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void PauseGameServerRpc()
        {
            if (_gameState.Value == GameState.Playing)
            {
                _gameState.Value = GameState.Paused;
            }
        }

        /// <summary>
        /// 恢复游戏
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void ResumeGameServerRpc()
        {
            if (_gameState.Value == GameState.Paused)
            {
                _gameState.Value = GameState.Playing;
            }
        }

        /// <summary>
        /// 投降
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void SurrenderServerRpc(int playerId)
        {
            int winnerId = playerId == 1 ? 2 : 1;
            EndGame(GameEndReason.Surrender, winnerId);
        }

        /// <summary>
        /// 结束游戏
        /// </summary>
        public void EndGame(GameEndReason reason, int winnerId = 0)
        {
            if (!IsServer || _gameState.Value == GameState.Ended) return;

            // 如果是时间耗尽，根据城堡血量判定胜负
            if (reason == GameEndReason.TimeLimit && winnerId == 0)
            {
                winnerId = DetermineWinnerByHealth();
            }

            Debug.Log($"[GameManager] 游戏结束 - 原因: {reason}, 获胜者: Player {winnerId}");

            _gameState.Value = GameState.Ended;

            // 通知客户端
            OnGameEndedClientRpc(reason, winnerId);
        }

        private int DetermineWinnerByHealth()
        {
            float health1 = _player1Castle != null ? _player1Castle.CurrentHealth : 0;
            float health2 = _player2Castle != null ? _player2Castle.CurrentHealth : 0;

            if (health1 > health2) return 1;
            if (health2 > health1) return 2;
            return 0; // 平局
        }

        #endregion

        #region 城堡管理

        /// <summary>
        /// 城堡被摧毁时调用
        /// </summary>
        public void OnCastleDestroyed(int ownerId)
        {
            if (!IsServer) return;

            int winnerId = ownerId == 1 ? 2 : 1;
            EndGame(GameEndReason.CastleDestroyed, winnerId);
        }

        /// <summary>
        /// 获取城堡控制器
        /// </summary>
        public CastleController GetCastle(int playerId)
        {
            return playerId == 1 ? _player1Castle : _player2Castle;
        }

        /// <summary>
        /// 获取敌方城堡位置
        /// </summary>
        public Vector3 GetEnemyCastlePosition(int playerId)
        {
            CastleController enemyCastle = playerId == 1 ? _player2Castle : _player1Castle;
            return enemyCastle != null ? enemyCastle.transform.position : Vector3.zero;
        }

        #endregion

        #region 网络回调

        [ClientRpc]
        private void OnGameStartedClientRpc()
        {
            Debug.Log("[GameManager] 游戏开始 (Client)");
            OnGameStarted?.Invoke();
        }

        [ClientRpc]
        private void OnGameEndedClientRpc(GameEndReason reason, int winnerId)
        {
            Debug.Log($"[GameManager] 游戏结束 (Client) - 原因: {reason}, 获胜者: Player {winnerId}");
            OnGameEnded?.Invoke(winnerId);
        }

        private void HandleGameStateChanged(GameState previousValue, GameState newValue)
        {
            Debug.Log($"[GameManager] 游戏状态变更: {previousValue} -> {newValue}");
            OnGameStateChanged?.Invoke(newValue);
        }

        #endregion

        #region 工具方法

        /// <summary>
        /// 获取玩家数量
        /// </summary>
        public int GetPlayerCount()
        {
            return _players.Count;
        }

        /// <summary>
        /// 检查游戏是否进行中
        /// </summary>
        public bool IsGamePlaying()
        {
            return _gameState.Value == GameState.Playing;
        }

        /// <summary>
        /// 获取游戏进度百分比
        /// </summary>
        public float GetGameProgress()
        {
            return Mathf.Clamp01(_gameTime.Value / gameDuration);
        }

        #endregion
    }
}
