using UnityEngine;
using System.Collections.Generic;

namespace CastleWars.Core
{
    /// <summary>
    /// 游戏状态枚举
    /// </summary>
    public enum GameState
    {
        Waiting,
        Playing,
        Paused,
        Ended
    }

    /// <summary>
    /// 游戏结束原因
    /// </summary>
    public enum GameEndReason
    {
        CastleDestroyed,
        TimeLimit,
        Surrender
    }

    /// <summary>
    /// 游戏管理器 - 管理游戏状态和流程（纯本地模式）
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("游戏设置")]
        [SerializeField] private float gameDuration = 600f;

        [Header("生成点")]
        [SerializeField] private Transform player1SpawnPoint;
        [SerializeField] private Transform player2SpawnPoint;

        [Header("预制体")]
        [SerializeField] private GameObject castlePrefab;

        [Header("AI设置")]
        [SerializeField] private bool enableAI = true;

        // 游戏状态
        private GameState currentState = GameState.Waiting;
        private float gameTime = 0f;

        // 城堡
        private CastleController player1Castle;
        private CastleController player2Castle;

        // 玩家
        private Dictionary<int, PlayerController> players = new Dictionary<int, PlayerController>();

        // 事件
        public System.Action<GameState> OnGameStateChanged;
        public System.Action OnGameStarted;
        public System.Action<GameEndReason, int> OnGameEnded;

        // 公开属性
        public GameState CurrentState => currentState;
        public float GameTime => gameTime;
        public float GameDuration => gameDuration;
        public CastleController Player1Castle => player1Castle;
        public CastleController Player2Castle => player2Castle;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            StartGame();
        }

        private void Update()
        {
            if (currentState != GameState.Playing) return;

            gameTime += Time.deltaTime;

            if (gameTime >= gameDuration)
            {
                EndGame(GameEndReason.TimeLimit);
            }
        }

        /// <summary>
        /// 开始游戏
        /// </summary>
        public void StartGame()
        {
            Debug.Log("[GameManager] Starting game...");

            gameTime = 0f;
            SetGameState(GameState.Playing);

            SpawnCastles();
            SpawnPlayers();

            OnGameStarted?.Invoke();
            Debug.Log("[GameManager] Game started!");
        }

        private void SpawnCastles()
        {
            Vector3 pos1 = player1SpawnPoint != null ? player1SpawnPoint.position : new Vector3(-20, 0, 0);
            Vector3 pos2 = player2SpawnPoint != null ? player2SpawnPoint.position : new Vector3(20, 0, 0);

            player1Castle = CreateCastle(pos1, 1);
            player2Castle = CreateCastle(pos2, 2);
        }

        private CastleController CreateCastle(Vector3 position, int ownerId)
        {
            GameObject castleObj;

            if (castlePrefab != null)
            {
                castleObj = Instantiate(castlePrefab, position, Quaternion.identity);
            }
            else
            {
                castleObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                castleObj.transform.position = position;
                castleObj.transform.localScale = new Vector3(5, 10, 5);

                BoxCollider col = castleObj.GetComponent<BoxCollider>();
                col.isTrigger = true;

                Renderer rend = castleObj.GetComponent<Renderer>();
                rend.material.color = ownerId == 1 ? new Color(0.2f, 0.4f, 0.8f) : new Color(0.8f, 0.2f, 0.2f);
            }

            castleObj.name = $"Castle_P{ownerId}";
            castleObj.tag = "Castle";

            CastleController controller = castleObj.GetComponent<CastleController>();
            if (controller == null)
            {
                controller = castleObj.AddComponent<CastleController>();
            }
            controller.Initialize(ownerId);

            return controller;
        }

        private void SpawnPlayers()
        {
            CreatePlayer(1, false);
            CreatePlayer(2, enableAI);
        }

        private void CreatePlayer(int playerId, bool isAI)
        {
            GameObject playerObj = new GameObject($"Player_{playerId}");
            PlayerController controller = playerObj.AddComponent<PlayerController>();
            controller.Initialize(playerId, isAI);
            players[playerId] = controller;

            Debug.Log($"[GameManager] Created Player {playerId}, AI: {isAI}");
        }

        /// <summary>
        /// 结束游戏
        /// </summary>
        public void EndGame(GameEndReason reason, int winnerId = 0)
        {
            if (currentState == GameState.Ended) return;

            if (reason == GameEndReason.TimeLimit && winnerId == 0)
            {
                winnerId = DetermineWinnerByHealth();
            }

            SetGameState(GameState.Ended);
            OnGameEnded?.Invoke(reason, winnerId);

            string winnerName = winnerId == 1 ? "Player 1" : "Player 2 (AI)";
            Debug.Log($"[GameManager] Game Over! {winnerName} wins! Reason: {reason}");
        }

        private int DetermineWinnerByHealth()
        {
            if (player1Castle != null && player2Castle != null)
            {
                return player1Castle.CurrentHealth >= player2Castle.CurrentHealth ? 1 : 2;
            }
            return 1;
        }

        /// <summary>
        /// 城堡被摧毁时调用
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
            OnGameStateChanged?.Invoke(newState);
            Debug.Log($"[GameManager] State: {oldState} -> {newState}");
        }

        /// <summary>
        /// 获取玩家控制器
        /// </summary>
        public PlayerController GetPlayer(int playerId)
        {
            return players.TryGetValue(playerId, out PlayerController p) ? p : null;
        }

        /// <summary>
        /// 获取敌方城堡
        /// </summary>
        public CastleController GetEnemyCastle(int playerId)
        {
            return playerId == 1 ? player2Castle : player1Castle;
        }

        /// <summary>
        /// 获取己方城堡
        /// </summary>
        public CastleController GetOwnCastle(int playerId)
        {
            return playerId == 1 ? player1Castle : player2Castle;
        }

        /// <summary>
        /// 重启游戏
        /// </summary>
        public void RestartGame()
        {
            // 清理玩家
            foreach (var p in players.Values)
            {
                if (p != null) Destroy(p.gameObject);
            }
            players.Clear();

            // 清理城堡
            if (player1Castle != null) Destroy(player1Castle.gameObject);
            if (player2Castle != null) Destroy(player2Castle.gameObject);

            // 清理单位
            var units = FindObjectsOfType<UnitController>();
            foreach (var u in units) Destroy(u.gameObject);

            Debug.Log("[GameManager] Game restarted!");
            StartGame();
        }
    }
}
