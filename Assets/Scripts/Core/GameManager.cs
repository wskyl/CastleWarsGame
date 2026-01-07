using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace CastleWars.Core
{
    /// <summary>
    /// 游戏管理器 - 管理游戏状态和流程
    /// 支持网络模式和本地模式
    /// </summary>
    public class GameManager : NetworkBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("游戏设置")]
        [SerializeField] private float gameDuration = 600f; // 10分钟
        [SerializeField] private bool forceLocalMode = true; // 强制使用本地模式

        [Header("玩家设置")]
        [SerializeField] private Transform player1SpawnPoint;
        [SerializeField] private Transform player2SpawnPoint;
        [SerializeField] private GameObject playerPrefab;

        // 网络模式的游戏状态
        private NetworkVariable<GameState> networkState = new NetworkVariable<GameState>(GameState.Waiting);
        private NetworkVariable<float> networkGameTime = new NetworkVariable<float>(0f);

        // 本地模式的游戏状态
        private GameState localState = GameState.Waiting;
        private float localGameTime = 0f;

        // 玩家
        private Dictionary<int, NetworkPlayer> players = new Dictionary<int, NetworkPlayer>();

        // 判断是否为本地模式
        public bool IsLocalMode => forceLocalMode || LocalGameMode.IsLocalMode || !IsNetworkActive();

        public GameState CurrentState => IsLocalMode ? localState : networkState.Value;
        public float GameTime => IsLocalMode ? localGameTime : networkGameTime.Value;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            // 如果是本地模式，自动开始游戏
            if (IsLocalMode)
            {
                InitializeLocalMode();
            }
        }

        /// <summary>
        /// 检查网络是否激活
        /// </summary>
        private bool IsNetworkActive()
        {
            return NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
        }

        /// <summary>
        /// 初始化本地模式
        /// </summary>
        private void InitializeLocalMode()
        {
            Debug.Log("Initializing local game mode...");
            LocalGameMode.EnableLocalMode();

            // 检查是否已有LocalGameMode实例
            if (LocalGameMode.Instance == null)
            {
                GameObject localModeObj = new GameObject("LocalGameMode");
                localModeObj.AddComponent<LocalGameMode>();
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            // 网络模式下禁用本地模式
            if (IsNetworkActive())
            {
                LocalGameMode.DisableLocalMode();
            }

            if (IsServer)
            {
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
            }

            networkState.OnValueChanged += OnGameStateChanged;
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            if (IsServer && NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
            }

            networkState.OnValueChanged -= OnGameStateChanged;
        }

        private void Update()
        {
            // 本地模式由LocalGameMode处理
            if (IsLocalMode) return;

            if (!IsServer) return;

            if (networkState.Value == GameState.Playing)
            {
                networkGameTime.Value += Time.deltaTime;

                // 检查时间限制
                if (networkGameTime.Value >= gameDuration)
                {
                    EndGame(GameEndReason.TimeLimit);
                }
            }
        }

        private void OnClientConnected(ulong clientId)
        {
            Debug.Log($"Client connected: {clientId}");

            // 当有两个玩家时开始游戏
            if (NetworkManager.Singleton.ConnectedClients.Count == 2 && networkState.Value == GameState.Waiting)
            {
                StartGame();
            }
        }

        private void OnClientDisconnected(ulong clientId)
        {
            Debug.Log($"Client disconnected: {clientId}");

            if (networkState.Value == GameState.Playing)
            {
                // 玩家断线，结束游戏
                EndGame(GameEndReason.PlayerDisconnected);
            }
        }

        /// <summary>
        /// 开始游戏
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void StartGameServerRpc()
        {
            StartGame();
        }

        private void StartGame()
        {
            if (!IsServer) return;

            networkState.Value = GameState.Playing;
            networkGameTime.Value = 0f;

            // 生成玩家
            SpawnPlayers();

            // 通知所有客户端
            OnGameStartedClientRpc();
        }

        private void SpawnPlayers()
        {
            int playerIndex = 1;
            foreach (var client in NetworkManager.Singleton.ConnectedClients)
            {
                Vector3 spawnPos = playerIndex == 1 ? player1SpawnPoint.position : player2SpawnPoint.position;

                GameObject playerObj = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
                NetworkObject netObj = playerObj.GetComponent<NetworkObject>();

                if (netObj != null)
                {
                    netObj.SpawnAsPlayerObject(client.Key);
                }

                NetworkPlayer networkPlayer = playerObj.GetComponent<NetworkPlayer>();
                if (networkPlayer != null)
                {
                    networkPlayer.playerId.Value = playerIndex;
                    players[playerIndex] = networkPlayer;
                }

                playerIndex++;
            }
        }

        [ClientRpc]
        private void OnGameStartedClientRpc()
        {
            Debug.Log("Game Started!");
            // 显示游戏开始UI
        }

        /// <summary>
        /// 结束游戏
        /// </summary>
        public void EndGame(GameEndReason reason, int winnerId = 0)
        {
            // 本地模式由LocalGameMode处理
            if (IsLocalMode)
            {
                if (LocalGameMode.Instance != null)
                {
                    LocalGameMode.Instance.EndGame(reason, winnerId);
                }
                return;
            }

            if (!IsServer) return;

            networkState.Value = GameState.Ended;

            // 确定胜利者
            if (reason == GameEndReason.CastleDestroyed)
            {
                // winnerId已传入
            }
            else if (reason == GameEndReason.TimeLimit)
            {
                // 根据剩余城堡血量判断
                winnerId = DetermineWinnerByHealth();
            }

            OnGameEndedClientRpc(reason, winnerId);
        }

        private int DetermineWinnerByHealth()
        {
            // 比较双方城堡血量
            // TODO: 实现城堡系统后完善
            return 1;
        }

        [ClientRpc]
        private void OnGameEndedClientRpc(GameEndReason reason, int winnerId)
        {
            Debug.Log($"Game Ended! Winner: Player {winnerId}, Reason: {reason}");
            // 显示游戏结束UI
        }

        private void OnGameStateChanged(GameState oldState, GameState newState)
        {
            Debug.Log($"Game state changed: {oldState} -> {newState}");
        }

        /// <summary>
        /// 城堡被摧毁时调用
        /// </summary>
        public void OnCastleDestroyed(int ownerId)
        {
            // 本地模式由LocalGameMode处理
            if (IsLocalMode)
            {
                if (LocalGameMode.Instance != null)
                {
                    LocalGameMode.Instance.OnCastleDestroyed(ownerId);
                }
                return;
            }

            if (!IsServer) return;

            // 对方获胜
            int winnerId = ownerId == 1 ? 2 : 1;
            EndGame(GameEndReason.CastleDestroyed, winnerId);
        }
    }

    public enum GameState
    {
        Waiting,    // 等待玩家
        Playing,    // 游戏中
        Paused,     // 暂停
        Ended       // 已结束
    }

    public enum GameEndReason
    {
        CastleDestroyed,    // 城堡被摧毁
        TimeLimit,          // 时间到
        PlayerDisconnected, // 玩家断线
        Surrender           // 投降
    }
}
