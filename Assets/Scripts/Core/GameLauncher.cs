using UnityEngine;
using UnityEngine.UI;

namespace CastleWars.Core
{
    /// <summary>
    /// 游戏启动器 - 提供简单的本地/网络模式选择界面
    /// </summary>
    public class GameLauncher : MonoBehaviour
    {
        [Header("UI元素")]
        [SerializeField] private Button localGameButton;
        [SerializeField] private Button hostGameButton;
        [SerializeField] private Button joinGameButton;
        [SerializeField] private GameObject menuPanel;
        [SerializeField] private GameObject gamePanel;

        [Header("预制体")]
        [SerializeField] private GameObject gameManagerPrefab;
        [SerializeField] private GameObject localGameModePrefab;

        [Header("自动启动设置")]
        [SerializeField] private bool autoStartLocalGame = true;

        private void Start()
        {
            // 设置按钮事件
            SetupButtons();

            // 自动启动本地游戏
            if (autoStartLocalGame)
            {
                StartLocalGame();
            }
        }

        private void SetupButtons()
        {
            if (localGameButton != null)
            {
                localGameButton.onClick.AddListener(StartLocalGame);
            }

            if (hostGameButton != null)
            {
                hostGameButton.onClick.AddListener(StartHostGame);
            }

            if (joinGameButton != null)
            {
                joinGameButton.onClick.AddListener(JoinGame);
            }
        }

        /// <summary>
        /// 开始本地游戏
        /// </summary>
        public void StartLocalGame()
        {
            Debug.Log("Starting local game...");

            // 启用本地模式
            LocalGameMode.EnableLocalMode();

            // 隐藏菜单
            if (menuPanel != null) menuPanel.SetActive(false);
            if (gamePanel != null) gamePanel.SetActive(true);

            // 创建本地游戏管理器
            if (LocalGameMode.Instance == null)
            {
                if (localGameModePrefab != null)
                {
                    Instantiate(localGameModePrefab);
                }
                else
                {
                    GameObject localModeObj = new GameObject("LocalGameMode");
                    localModeObj.AddComponent<LocalGameMode>();
                }
            }

            // 创建单位生成器
            if (LocalUnitSpawner.Instance == null)
            {
                GameObject spawnerObj = new GameObject("LocalUnitSpawner");
                spawnerObj.AddComponent<LocalUnitSpawner>();
            }

            Debug.Log("Local game started successfully!");
        }

        /// <summary>
        /// 作为主机开始游戏
        /// </summary>
        public void StartHostGame()
        {
            Debug.Log("Starting as host...");

            // 禁用本地模式
            LocalGameMode.DisableLocalMode();

            // 隐藏菜单
            if (menuPanel != null) menuPanel.SetActive(false);
            if (gamePanel != null) gamePanel.SetActive(true);

            // 启动网络主机
            if (Unity.Netcode.NetworkManager.Singleton != null)
            {
                Unity.Netcode.NetworkManager.Singleton.StartHost();
            }
            else
            {
                Debug.LogWarning("NetworkManager not found. Please add NetworkManager to the scene.");
            }
        }

        /// <summary>
        /// 加入游戏
        /// </summary>
        public void JoinGame()
        {
            Debug.Log("Joining game...");

            // 禁用本地模式
            LocalGameMode.DisableLocalMode();

            // 隐藏菜单
            if (menuPanel != null) menuPanel.SetActive(false);
            if (gamePanel != null) gamePanel.SetActive(true);

            // 加入网络游戏
            if (Unity.Netcode.NetworkManager.Singleton != null)
            {
                Unity.Netcode.NetworkManager.Singleton.StartClient();
            }
            else
            {
                Debug.LogWarning("NetworkManager not found. Please add NetworkManager to the scene.");
            }
        }

        /// <summary>
        /// 返回主菜单
        /// </summary>
        public void ReturnToMenu()
        {
            // 停止网络
            if (Unity.Netcode.NetworkManager.Singleton != null && Unity.Netcode.NetworkManager.Singleton.IsListening)
            {
                Unity.Netcode.NetworkManager.Singleton.Shutdown();
            }

            // 停止本地游戏
            if (LocalGameMode.Instance != null)
            {
                Destroy(LocalGameMode.Instance.gameObject);
            }

            // 显示菜单
            if (menuPanel != null) menuPanel.SetActive(true);
            if (gamePanel != null) gamePanel.SetActive(false);
        }
    }
}
