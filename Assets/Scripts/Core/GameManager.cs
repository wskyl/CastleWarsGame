using UnityEngine;
using System.Collections.Generic;

#if UNITY_NETCODE
using Unity.Netcode;
#endif

namespace CastleWars.Core
{
    /// <summary>
    /// 游戏管理器 - 管理游戏状态和流程
    /// 支持网络模式和本地模式
    /// </summary>
#if UNITY_NETCODE
    public class GameManager : NetworkBehaviour
#else
    public class GameManager : MonoBehaviour
#endif
    {
        public static GameManager Instance { get; private set; }

        [Header("游戏设置")]
        [SerializeField] private float gameDuration = 600f;
        [SerializeField] private bool forceLocalMode = true;

        [Header("玩家设置")]
        [SerializeField] private Transform player1SpawnPoint;
        [SerializeField] private Transform player2SpawnPoint;
        [SerializeField] private GameObject playerPrefab;

#if UNITY_NETCODE
        // 网络模式的游戏状态
        private NetworkVariable<GameState> networkState = new NetworkVariable<GameState>(GameState.Waiting);
        private NetworkVariable<float> networkGameTime = new NetworkVariable<float>(0f);
#endif

        // 本地模式的游戏状态
        private GameState localState = GameState.Waiting;
        private float localGameTime = 0f;

        // 玩家
        private Dictionary<int, NetworkPlayer> players = new Dictionary<int, NetworkPlayer>();

        // 判断是否为本地模式
        public bool IsLocalMode => forceLocalMode || LocalGameMode.IsLocalMode || !IsNetworkActive();

#if UNITY_NETCODE
        public GameState CurrentState => IsLocalMode ? localState : networkState.Value;
        public float GameTime => IsLocalMode ? localGameTime : networkGameTime.Value;
#else
        public GameState CurrentState => localState;
        public float GameTime => localGameTime;
#endif

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
#if UNITY_NETCODE
            return NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
#else
            return false;
#endif
        }

        /// <summary>
        /// 初始化本地模式
        /// </summary>
        private void InitializeLocalMode()
        {
            Debug.Log("Initializing local game mode...");
            LocalGameMode.EnableLocalMode();

            if (LocalGameMode.Instance == null)
            {
                GameObject localModeObj = new GameObject("LocalGameMode");
                localModeObj.AddComponent<LocalGameMode>();
            }
        }

#if UNITY_NETCODE
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

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

        private void OnClientConnected(ulong clientId)
        {
            Debug.Log($"Client connected: {clientId}");

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
                EndGame(GameEndReason.PlayerDisconnected);
            }
        }

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

            SpawnPlayers();
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
        }

        [ClientRpc]
        private void OnGameEndedClientRpc(GameEndReason reason, int winnerId)
        {
            Debug.Log($"Game Ended! Winner: Player {winnerId}, Reason: {reason}");
        }

        private void OnGameStateChanged(GameState oldState, GameState newState)
        {
            Debug.Log($"Game state changed: {oldState} -> {newState}");
        }
#endif

        private void Update()
        {
            if (IsLocalMode) return;

#if UNITY_NETCODE
            if (!IsServer) return;

            if (networkState.Value == GameState.Playing)
            {
                networkGameTime.Value += Time.deltaTime;

                if (networkGameTime.Value >= gameDuration)
                {
                    EndGame(GameEndReason.TimeLimit);
                }
            }
#endif
        }

        /// <summary>
        /// 结束游戏
        /// </summary>
        public void EndGame(GameEndReason reason, int winnerId = 0)
        {
            if (IsLocalMode)
            {
                if (LocalGameMode.Instance != null)
                {
                    LocalGameMode.Instance.EndGame(reason, winnerId);
                }
                return;
            }

#if UNITY_NETCODE
            if (!IsServer) return;

            networkState.Value = GameState.Ended;

            if (reason == GameEndReason.TimeLimit)
            {
                winnerId = DetermineWinnerByHealth();
            }

            OnGameEndedClientRpc(reason, winnerId);
#endif
        }

        private int DetermineWinnerByHealth()
        {
            return 1;
        }

        /// <summary>
        /// 城堡被摧毁时调用
        /// </summary>
        public void OnCastleDestroyed(int ownerId)
        {
            if (IsLocalMode)
            {
                if (LocalGameMode.Instance != null)
                {
                    LocalGameMode.Instance.OnCastleDestroyed(ownerId);
                }
                return;
            }

#if UNITY_NETCODE
            if (!IsServer) return;

            int winnerId = ownerId == 1 ? 2 : 1;
            EndGame(GameEndReason.CastleDestroyed, winnerId);
#endif
        }
    }

    public enum GameState
    {
        Waiting,
        Playing,
        Paused,
        Ended
    }

    public enum GameEndReason
    {
        CastleDestroyed,
        TimeLimit,
        PlayerDisconnected,
        Surrender
    }
}
