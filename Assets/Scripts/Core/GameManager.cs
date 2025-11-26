using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace CastleWars.Core
{
    /// <summary>
    /// 游戏管理器 - 管理游戏状态和流程
    /// </summary>
    public class GameManager : NetworkBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("游戏设置")]
        [SerializeField] private float gameDuration = 600f; // 10分钟

        [Header("玩家设置")]
        [SerializeField] private Transform player1SpawnPoint;
        [SerializeField] private Transform player2SpawnPoint;
        [SerializeField] private GameObject playerPrefab;

        // 游戏状态
        private NetworkVariable<GameState> currentState = new NetworkVariable<GameState>(GameState.Waiting);
        private NetworkVariable<float> gameTime = new NetworkVariable<float>(0f);

        // 玩家
        private Dictionary<int, NetworkPlayer> players = new Dictionary<int, NetworkPlayer>();

        public GameState CurrentState => currentState.Value;
        public float GameTime => gameTime.Value;

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

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsServer)
            {
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
            }

            currentState.OnValueChanged += OnGameStateChanged;
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            if (IsServer)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
            }

            currentState.OnValueChanged -= OnGameStateChanged;
        }

        private void Update()
        {
            if (!IsServer) return;

            if (currentState.Value == GameState.Playing)
            {
                gameTime.Value += Time.deltaTime;

                // 检查时间限制
                if (gameTime.Value >= gameDuration)
                {
                    EndGame(GameEndReason.TimeLimit);
                }
            }
        }

        private void OnClientConnected(ulong clientId)
        {
            Debug.Log($"Client connected: {clientId}");

            // 当有两个玩家时开始游戏
            if (NetworkManager.Singleton.ConnectedClients.Count == 2 && currentState.Value == GameState.Waiting)
            {
                StartGame();
            }
        }

        private void OnClientDisconnected(ulong clientId)
        {
            Debug.Log($"Client disconnected: {clientId}");

            if (currentState.Value == GameState.Playing)
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

            currentState.Value = GameState.Playing;
            gameTime.Value = 0f;

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
            if (!IsServer) return;

            currentState.Value = GameState.Ended;

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
