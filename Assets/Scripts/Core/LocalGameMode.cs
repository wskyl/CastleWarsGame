using UnityEngine;
using System.Collections.Generic;

namespace CastleWars.Core
{
    /// <summary>
    /// 本地游戏模式管理器 - 在没有网络的情况下支持本地单机/双人对战
    /// </summary>
    public class LocalGameMode : MonoBehaviour
    {
        public static LocalGameMode Instance { get; private set; }
        public static bool IsLocalMode { get; private set; } = true;

        [Header("游戏设置")]
        [SerializeField] private float gameDuration = 600f;
        [SerializeField] private bool enableAI = true;

        [Header("玩家设置")]
        [SerializeField] private Transform player1SpawnPoint;
        [SerializeField] private Transform player2SpawnPoint;
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private GameObject castlePrefab;

        // 游戏状态
        private GameState currentState = GameState.Waiting;
        private float gameTime = 0f;

        // 玩家和城堡
        private Dictionary<int, LocalPlayer> players = new Dictionary<int, LocalPlayer>();
        private Dictionary<int, LocalCastle> castles = new Dictionary<int, LocalCastle>();

        public GameState CurrentState => currentState;
        public float GameTime => gameTime;
        public float GameDuration => gameDuration;

        // 事件
        public System.Action<GameState, GameState> OnGameStateChanged;
        public System.Action OnGameStarted;
        public System.Action<GameEndReason, int> OnGameEnded;

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
            // 自动开始本地游戏
            StartLocalGame();
        }

        private void Update()
        {
            if (currentState == GameState.Playing)
            {
                gameTime += Time.deltaTime;

                // 检查时间限制
                if (gameTime >= gameDuration)
                {
                    EndGame(GameEndReason.TimeLimit);
                }
            }
        }

        /// <summary>
        /// 启动本地游戏模式
        /// </summary>
        public static void EnableLocalMode()
        {
            IsLocalMode = true;
            Debug.Log("Local game mode enabled");
        }

        /// <summary>
        /// 禁用本地游戏模式（使用网络）
        /// </summary>
        public static void DisableLocalMode()
        {
            IsLocalMode = false;
            Debug.Log("Network mode enabled");
        }

        /// <summary>
        /// 开始本地游戏
        /// </summary>
        public void StartLocalGame()
        {
            if (!IsLocalMode) return;

            SetGameState(GameState.Playing);
            gameTime = 0f;

            // 创建玩家
            SpawnLocalPlayers();

            // 创建城堡
            SpawnCastles();

            OnGameStarted?.Invoke();
            Debug.Log("Local game started!");
        }

        private void SpawnLocalPlayers()
        {
            // 创建玩家1（人类控制）
            CreateLocalPlayer(1, player1SpawnPoint != null ? player1SpawnPoint.position : new Vector3(-15, 0, 0), false);

            // 创建玩家2（AI或人类）
            CreateLocalPlayer(2, player2SpawnPoint != null ? player2SpawnPoint.position : new Vector3(15, 0, 0), enableAI);
        }

        private void CreateLocalPlayer(int playerId, Vector3 position, bool isAI)
        {
            GameObject playerObj;

            if (playerPrefab != null)
            {
                playerObj = Instantiate(playerPrefab, position, Quaternion.identity);
            }
            else
            {
                playerObj = new GameObject($"Player_{playerId}");
                playerObj.transform.position = position;
            }

            // 添加本地玩家组件
            LocalPlayer localPlayer = playerObj.GetComponent<LocalPlayer>();
            if (localPlayer == null)
            {
                localPlayer = playerObj.AddComponent<LocalPlayer>();
            }

            localPlayer.Initialize(playerId, isAI);
            players[playerId] = localPlayer;

            Debug.Log($"Created local player {playerId}, IsAI: {isAI}");
        }

        private void SpawnCastles()
        {
            // 创建玩家1城堡 - 优先使用SpawnPoint
            Vector3 p1CastlePos = player1SpawnPoint != null ? player1SpawnPoint.position : new Vector3(-20, 0, 0);
            CreateLocalCastle(1, p1CastlePos);

            // 创建玩家2城堡 - 优先使用SpawnPoint
            Vector3 p2CastlePos = player2SpawnPoint != null ? player2SpawnPoint.position : new Vector3(20, 0, 0);
            CreateLocalCastle(2, p2CastlePos);
        }

        private void CreateLocalCastle(int ownerId, Vector3 position)
        {
            GameObject castleObj;

            if (castlePrefab != null)
            {
                castleObj = Instantiate(castlePrefab, position, Quaternion.identity);
            }
            else
            {
                castleObj = new GameObject($"Castle_{ownerId}");
                castleObj.transform.position = position;

                // 添加基本碰撞器
                BoxCollider collider = castleObj.AddComponent<BoxCollider>();
                collider.size = new Vector3(5, 10, 5);
                collider.isTrigger = true;
            }

            // 添加本地城堡组件
            LocalCastle localCastle = castleObj.GetComponent<LocalCastle>();
            if (localCastle == null)
            {
                localCastle = castleObj.AddComponent<LocalCastle>();
            }

            localCastle.Initialize(ownerId);
            castles[ownerId] = localCastle;

            Debug.Log($"Created local castle for player {ownerId}");
        }

        /// <summary>
        /// 结束游戏
        /// </summary>
        public void EndGame(GameEndReason reason, int winnerId = 0)
        {
            SetGameState(GameState.Ended);

            // 确定胜利者
            if (reason == GameEndReason.TimeLimit)
            {
                winnerId = DetermineWinnerByHealth();
            }

            OnGameEnded?.Invoke(reason, winnerId);
            Debug.Log($"Game ended! Winner: Player {winnerId}, Reason: {reason}");
        }

        private int DetermineWinnerByHealth()
        {
            if (castles.TryGetValue(1, out LocalCastle castle1) &&
                castles.TryGetValue(2, out LocalCastle castle2))
            {
                return castle1.CurrentHealth >= castle2.CurrentHealth ? 1 : 2;
            }
            return 1;
        }

        /// <summary>
        /// 城堡被摧毁
        /// </summary>
        public void OnCastleDestroyed(int ownerId)
        {
            int winnerId = ownerId == 1 ? 2 : 1;
            EndGame(GameEndReason.CastleDestroyed, winnerId);
        }

        private void SetGameState(GameState newState)
        {
            GameState oldState = currentState;
            currentState = newState;
            OnGameStateChanged?.Invoke(oldState, newState);
            Debug.Log($"Game state changed: {oldState} -> {newState}");
        }

        /// <summary>
        /// 获取玩家
        /// </summary>
        public LocalPlayer GetPlayer(int playerId)
        {
            return players.TryGetValue(playerId, out LocalPlayer player) ? player : null;
        }

        /// <summary>
        /// 获取城堡
        /// </summary>
        public LocalCastle GetCastle(int ownerId)
        {
            return castles.TryGetValue(ownerId, out LocalCastle castle) ? castle : null;
        }

        /// <summary>
        /// 重新开始游戏
        /// </summary>
        public void RestartGame()
        {
            // 清理所有残留单位
            LocalUnit[] allUnits = FindObjectsOfType<LocalUnit>();
            foreach (var unit in allUnits)
            {
                if (unit != null) Destroy(unit.gameObject);
            }

            // 清理现有玩家和城堡对象
            foreach (var player in players.Values)
            {
                if (player != null) Destroy(player.gameObject);
            }
            foreach (var castle in castles.Values)
            {
                if (castle != null) Destroy(castle.gameObject);
            }
            players.Clear();
            castles.Clear();

            // 重新开始
            StartLocalGame();
        }
    }
}
