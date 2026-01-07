using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CastleWars.UI
{
    /// <summary>
    /// 本地游戏UI控制器 - 在本地模式下显示游戏信息和控制
    /// </summary>
    public class LocalGameUI : MonoBehaviour
    {
        [Header("资源显示")]
        [SerializeField] private Text goldText;
        [SerializeField] private Text incomeText;
        [SerializeField] private TMP_Text goldTextTMP;
        [SerializeField] private TMP_Text incomeTextTMP;

        [Header("城堡血量")]
        [SerializeField] private Slider player1HealthBar;
        [SerializeField] private Slider player2HealthBar;
        [SerializeField] private Text player1HealthText;
        [SerializeField] private Text player2HealthText;

        [Header("游戏信息")]
        [SerializeField] private Text gameTimeText;
        [SerializeField] private Text gameStateText;

        [Header("单位训练按钮")]
        [SerializeField] private Button trainUnitButton;
        [SerializeField] private int unitCost = 100;

        [Header("游戏结束面板")]
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private Text winnerText;
        [SerializeField] private Button restartButton;

        private CastleWars.Core.LocalPlayer localPlayer;
        private CastleWars.Core.LocalCastle player1Castle;
        private CastleWars.Core.LocalCastle player2Castle;

        private void Start()
        {
            // 设置按钮事件
            if (trainUnitButton != null)
            {
                trainUnitButton.onClick.AddListener(OnTrainUnitClicked);
            }

            if (restartButton != null)
            {
                restartButton.onClick.AddListener(OnRestartClicked);
            }

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

            // 更新UI
            UpdateResourceDisplay();
            UpdateCastleHealth();
            UpdateGameInfo();
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

            string goldStr = $"Gold: {localPlayer.Gold}";
            string incomeStr = $"Income: {localPlayer.Income}/s";

            if (goldText != null) goldText.text = goldStr;
            if (goldTextTMP != null) goldTextTMP.text = goldStr;
            if (incomeText != null) incomeText.text = incomeStr;
            if (incomeTextTMP != null) incomeTextTMP.text = incomeStr;
        }

        private void UpdateCastleHealth()
        {
            if (player1Castle != null)
            {
                if (player1HealthBar != null)
                {
                    player1HealthBar.value = player1Castle.HealthPercentage;
                }
                if (player1HealthText != null)
                {
                    player1HealthText.text = $"P1: {player1Castle.CurrentHealth:F0}/{player1Castle.MaxHealth}";
                }
            }

            if (player2Castle != null)
            {
                if (player2HealthBar != null)
                {
                    player2HealthBar.value = player2Castle.HealthPercentage;
                }
                if (player2HealthText != null)
                {
                    player2HealthText.text = $"P2: {player2Castle.CurrentHealth:F0}/{player2Castle.MaxHealth}";
                }
            }
        }

        private void UpdateGameInfo()
        {
            if (CastleWars.Core.LocalGameMode.Instance == null) return;

            float gameTime = CastleWars.Core.LocalGameMode.Instance.GameTime;
            int minutes = Mathf.FloorToInt(gameTime / 60);
            int seconds = Mathf.FloorToInt(gameTime % 60);

            if (gameTimeText != null)
            {
                gameTimeText.text = $"Time: {minutes:00}:{seconds:00}";
            }

            if (gameStateText != null)
            {
                gameStateText.text = $"State: {CastleWars.Core.LocalGameMode.Instance.CurrentState}";
            }
        }

        private void OnGoldChanged(int newGold)
        {
            UpdateResourceDisplay();
        }

        private void OnIncomeChanged(int newIncome)
        {
            UpdateResourceDisplay();
        }

        private void OnTrainUnitClicked()
        {
            if (localPlayer == null) return;

            if (localPlayer.SpendGold(unitCost))
            {
                // 生成单位
                if (CastleWars.Core.LocalUnitSpawner.Instance != null)
                {
                    CastleWars.Core.LocalUnitSpawner.Instance.SpawnUnit(1);
                    Debug.Log("Unit trained!");
                }
            }
            else
            {
                Debug.Log("Not enough gold to train unit!");
            }
        }

        private void OnGameEnded(CastleWars.Core.GameEndReason reason, int winnerId)
        {
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
            }

            if (winnerText != null)
            {
                string winnerName = winnerId == 1 ? "Player 1" : "AI";
                winnerText.text = $"{winnerName} Wins!\nReason: {reason}";
            }
        }

        private void OnRestartClicked()
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
