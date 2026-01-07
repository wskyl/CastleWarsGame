using UnityEngine;
using CastleWars.Economy;

#if UNITY_NETCODE
using Unity.Netcode;
#endif

namespace CastleWars.Core
{
    /// <summary>
    /// 网络玩家 - 管理玩家的网络同步和游戏逻辑
    /// 支持本地模式和网络模式
    /// </summary>
#if UNITY_NETCODE
    public class NetworkPlayer : NetworkBehaviour
#else
    public class NetworkPlayer : MonoBehaviour
#endif
    {
        [Header("玩家信息")]
#if UNITY_NETCODE
        public NetworkVariable<int> playerId = new NetworkVariable<int>();
        public NetworkVariable<string> playerName = new NetworkVariable<string>();
#else
        private int _playerId;
        private string _playerName = "";
        public int PlayerIdValue
        {
            get => _playerId;
            set => _playerId = value;
        }
        public string PlayerNameValue
        {
            get => _playerName;
            set => _playerName = value;
        }
#endif

        [Header("组件")]
        private PlayerEconomy economy;
        private BuildingManager buildingManager;

        [Header("城堡")]
        public GameObject castlePrefab;
        private GameObject castle;

        // 本地模式判断
        private bool IsLocalMode => LocalGameMode.IsLocalMode;

        private void Awake()
        {
            economy = GetComponent<PlayerEconomy>();
            buildingManager = GetComponent<BuildingManager>();
        }

#if UNITY_NETCODE
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsServer)
            {
                SpawnCastleNetwork();
            }

            if (buildingManager != null)
            {
                buildingManager.playerId = playerId.Value;
            }

            if (IsOwner)
            {
                InitializeLocalPlayer();
            }
        }

        private void SpawnCastleNetwork()
        {
            if (castlePrefab == null) return;

            Vector3 castlePos = playerId.Value == 1
                ? new Vector3(-20, 0, 0)
                : new Vector3(20, 0, 0);

            castle = Instantiate(castlePrefab, castlePos, Quaternion.identity);

            CastleController castleController = castle.GetComponent<CastleController>();
            if (castleController != null)
            {
                castleController.ownerId = playerId.Value;
            }

            NetworkObject netObj = castle.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                netObj.Spawn();
            }
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            if (IsOwner && economy != null)
            {
                economy.OnGoldChanged -= OnGoldChanged;
                economy.OnIncomeChanged -= OnIncomeChanged;
            }
        }
#endif

        private void Start()
        {
            // 本地模式初始化
            if (IsLocalMode)
            {
                InitializeLocalMode();
            }
        }

        private void InitializeLocalMode()
        {
            if (buildingManager != null)
            {
#if UNITY_NETCODE
                buildingManager.playerId = playerId.Value;
#else
                buildingManager.playerId = _playerId;
#endif
            }

            InitializeLocalPlayer();
        }

        private void InitializeLocalPlayer()
        {
#if UNITY_NETCODE
            Debug.Log($"Local player initialized: Player {playerId.Value}");
#else
            Debug.Log($"Local player initialized: Player {_playerId}");
#endif

            if (economy != null)
            {
                economy.OnGoldChanged += OnGoldChanged;
                economy.OnIncomeChanged += OnIncomeChanged;
            }
        }

        private void OnGoldChanged(int newGold)
        {
            Debug.Log($"Gold: {newGold}");
        }

        private void OnIncomeChanged(int newIncome)
        {
            Debug.Log($"Income: {newIncome}/s");
        }

        private void OnDestroy()
        {
            if (economy != null)
            {
                economy.OnGoldChanged -= OnGoldChanged;
                economy.OnIncomeChanged -= OnIncomeChanged;
            }
        }
    }
}
