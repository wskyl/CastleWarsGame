using UnityEngine;
using Unity.Netcode;
using CastleWars.Economy;

namespace CastleWars.Core
{
    /// <summary>
    /// 网络玩家 - 管理玩家的网络同步和游戏逻辑
    /// </summary>
    public class NetworkPlayer : NetworkBehaviour
    {
        [Header("玩家信息")]
        public NetworkVariable<int> playerId = new NetworkVariable<int>();
        public NetworkVariable<string> playerName = new NetworkVariable<string>();

        [Header("组件")]
        private PlayerEconomy economy;
        private BuildingManager buildingManager;

        [Header("城堡")]
        public GameObject castlePrefab;
        private GameObject castle;

        private void Awake()
        {
            economy = GetComponent<PlayerEconomy>();
            buildingManager = GetComponent<BuildingManager>();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsServer)
            {
                // 服务器生成城堡
                SpawnCastle();
            }

            // 设置建筑管理器的玩家ID
            if (buildingManager != null)
            {
                buildingManager.playerId = playerId.Value;
            }

            // 如果是本地玩家，初始化UI
            if (IsOwner)
            {
                InitializeLocalPlayer();
            }
        }

        private void SpawnCastle()
        {
            if (castlePrefab == null) return;

            // 根据玩家ID确定城堡位置
            Vector3 castlePos = playerId.Value == 1
                ? new Vector3(-20, 0, 0)
                : new Vector3(20, 0, 0);

            castle = Instantiate(castlePrefab, castlePos, Quaternion.identity);

            // 设置城堡所属
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

        private void InitializeLocalPlayer()
        {
            // 初始化本地玩家UI
            Debug.Log($"Local player initialized: Player {playerId.Value}");

            // 订阅经济事件
            if (economy != null)
            {
                economy.OnGoldChanged += OnGoldChanged;
                economy.OnIncomeChanged += OnIncomeChanged;
            }
        }

        private void OnGoldChanged(int newGold)
        {
            // 更新UI显示金币
            Debug.Log($"Gold: {newGold}");
        }

        private void OnIncomeChanged(int newIncome)
        {
            // 更新UI显示收入
            Debug.Log($"Income: {newIncome}/s");
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
    }
}
