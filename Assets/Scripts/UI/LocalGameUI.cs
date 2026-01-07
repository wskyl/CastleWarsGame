using UnityEngine;

namespace CastleWars.UI
{
    /// <summary>
    /// 本地游戏UI控制器 - 在本地模式下显示游戏信息和控制
    /// 不依赖UnityEngine.UI和TMPro模块
    /// </summary>
    public class LocalGameUI : MonoBehaviour
    {
        [Header("游戏结束面板")]
        [SerializeField] private GameObject gameOverPanel;

        [Header("单位训练设置")]
        [SerializeField] private int unitCost = 100;

        private CastleWars.Core.LocalPlayer localPlayer;
        private CastleWars.Core.LocalCastle player1Castle;
        private CastleWars.Core.LocalCastle player2Castle;

        // 缓存上次的值避免频繁Log
        private int lastGold = -1;
        private int lastIncome = -1;
        private float lastP1Health = -1;
        private float lastP2Health = -1;

        private void Start()
        {
            // 隐藏游戏结束面板
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(false);
            }

            // 订阅游戏事件
            if (CastleWars.Core.LocalGameMode.Instance != null)
            {
                CastleWars.Core.LocalGameMode.Instance.OnGameEnded += OnGameEnded;
            }
        }

        private void Update()
        {
            if (!CastleWars.Core.LocalGameMode.IsLocalMode) return;

            // 查找玩家引用
            FindReferences();

            // 更新UI（通过Log显示）
            UpdateResourceDisplay();
            UpdateCastleHealth();

            // 检测按键输入来训练单位
            if (Input.GetKeyDown(KeyCode.Space))
            {
                OnTrainUnitClicked();
            }

            // R键重启游戏
            if (Input.GetKeyDown(KeyCode.R))
            {
                OnRestartClicked();
            }
        }

        private void FindReferences()
        {
            if (localPlayer == null && CastleWars.Core.LocalGameMode.Instance != null)
            {
                localPlayer = CastleWars.Core.LocalGameMode.Instance.GetPlayer(1);
                if (localPlayer != null)
                {
                    localPlayer.OnGoldChanged += OnGoldChanged;
                    localPlayer.OnIncomeChanged += OnIncomeChanged;
                }
            }

            if (player1Castle == null && CastleWars.Core.LocalGameMode.Instance != null)
            {
                player1Castle = CastleWars.Core.LocalGameMode.Instance.GetCastle(1);
            }

            if (player2Castle == null && CastleWars.Core.LocalGameMode.Instance != null)
            {
                player2Castle = CastleWars.Core.LocalGameMode.Instance.GetCastle(2);
            }
        }

        private void UpdateResourceDisplay()
        {
            if (localPlayer == null) return;

            if (localPlayer.Gold != lastGold || localPlayer.Income != lastIncome)
            {
                lastGold = localPlayer.Gold;
                lastIncome = localPlayer.Income;
                // 只在变化时输出日志
            }
        }

        private void UpdateCastleHealth()
        {
            if (player1Castle != null && player1Castle.CurrentHealth != lastP1Health)
            {
                lastP1Health = player1Castle.CurrentHealth;
                Debug.Log($"[LocalGameUI] P1 Castle HP: {player1Castle.CurrentHealth:F0}/{player1Castle.MaxHealth}");
            }

            if (player2Castle != null && player2Castle.CurrentHealth != lastP2Health)
            {
                lastP2Health = player2Castle.CurrentHealth;
                Debug.Log($"[LocalGameUI] P2 Castle HP: {player2Castle.CurrentHealth:F0}/{player2Castle.MaxHealth}");
            }
        }

        private void OnGoldChanged(int newGold)
        {
            Debug.Log($"[LocalGameUI] Gold: {newGold}");
        }

        private void OnIncomeChanged(int newIncome)
        {
            Debug.Log($"[LocalGameUI] Income: {newIncome}/s");
        }

        /// <summary>
        /// 训练单位（按空格键或由外部调用）
        /// </summary>
        public void OnTrainUnitClicked()
        {
            if (localPlayer == null) return;

            if (localPlayer.SpendGold(unitCost))
            {
                if (CastleWars.Core.LocalUnitSpawner.Instance != null)
                {
                    CastleWars.Core.LocalUnitSpawner.Instance.SpawnUnit(1);
                    Debug.Log("[LocalGameUI] Unit trained!");
                }
            }
            else
            {
                Debug.Log("[LocalGameUI] Not enough gold to train unit!");
            }
        }

        private void OnGameEnded(CastleWars.Core.GameEndReason reason, int winnerId)
        {
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
            }

            string winnerName = winnerId == 1 ? "Player 1" : "AI";
            Debug.Log($"[LocalGameUI] Game Over! {winnerName} Wins! Reason: {reason}");
        }

        /// <summary>
        /// 重启游戏（按R键或由外部调用）
        /// </summary>
        public void OnRestartClicked()
        {
            if (CastleWars.Core.LocalGameMode.Instance != null)
            {
                CastleWars.Core.LocalGameMode.Instance.RestartGame();
            }

            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(false);
            }

            // 重置引用
            localPlayer = null;
            player1Castle = null;
            player2Castle = null;
            lastGold = -1;
            lastIncome = -1;
            lastP1Health = -1;
            lastP2Health = -1;

            Debug.Log("[LocalGameUI] Game restarted!");
        }

        private void OnDestroy()
        {
            if (localPlayer != null)
            {
                localPlayer.OnGoldChanged -= OnGoldChanged;
                localPlayer.OnIncomeChanged -= OnIncomeChanged;
            }

            if (CastleWars.Core.LocalGameMode.Instance != null)
            {
                CastleWars.Core.LocalGameMode.Instance.OnGameEnded -= OnGameEnded;
            }
        }
    }
}
