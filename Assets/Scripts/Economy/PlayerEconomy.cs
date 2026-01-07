using UnityEngine;
using CastleWars.Core;
using System;

#if UNITY_NETCODE
using Unity.Netcode;
#endif

namespace CastleWars.Economy
{
    /// <summary>
    /// 玩家经济系统 - 管理金币和收入
    /// 支持本地模式和网络模式
    /// </summary>
#if UNITY_NETCODE
    public class PlayerEconomy : NetworkBehaviour
#else
    public class PlayerEconomy : MonoBehaviour
#endif
    {
        [Header("初始资源")]
        [SerializeField] private int startingGold = 500;

        [Header("收入设置")]
        [SerializeField] private int baseIncome = 10;
        [SerializeField] private float incomeInterval = 1f;
        [SerializeField] private int incomeIncrement = 1;

        // 网络模式资源
#if UNITY_NETCODE
        private NetworkVariable<int> networkGold = new NetworkVariable<int>();
        private NetworkVariable<int> networkIncome = new NetworkVariable<int>();
#endif

        // 本地模式资源
        private int localGold;
        private int localIncome;

        // 收入计时器
        private float incomeTimer = 0f;

        // 事件
        public event Action<int> OnGoldChanged;
        public event Action<int> OnIncomeChanged;

        // 本地模式判断
        private bool IsLocalMode => LocalGameMode.IsLocalMode;

#if UNITY_NETCODE
        public int Gold => IsLocalMode ? localGold : networkGold.Value;
        public int Income => IsLocalMode ? localIncome : networkIncome.Value;
#else
        public int Gold => localGold;
        public int Income => localIncome;
#endif

#if UNITY_NETCODE
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsServer)
            {
                networkGold.Value = startingGold;
                networkIncome.Value = baseIncome;
            }

            networkGold.OnValueChanged += OnNetworkGoldChanged;
            networkIncome.OnValueChanged += OnNetworkIncomeChanged;
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            networkGold.OnValueChanged -= OnNetworkGoldChanged;
            networkIncome.OnValueChanged -= OnNetworkIncomeChanged;
        }

        private void OnNetworkGoldChanged(int oldValue, int newValue)
        {
            OnGoldChanged?.Invoke(newValue);
        }

        private void OnNetworkIncomeChanged(int oldValue, int newValue)
        {
            OnIncomeChanged?.Invoke(newValue);
        }
#endif

        private void Start()
        {
            // 本地模式初始化
            if (IsLocalMode)
            {
                localGold = startingGold;
                localIncome = baseIncome;
                OnGoldChanged?.Invoke(localGold);
                OnIncomeChanged?.Invoke(localIncome);
            }
        }

        private void Update()
        {
            if (IsLocalMode)
            {
                UpdateLocalMode();
            }
#if UNITY_NETCODE
            else
            {
                UpdateNetworkMode();
            }
#endif
        }

        private void UpdateLocalMode()
        {
            incomeTimer += Time.deltaTime;

            if (incomeTimer >= incomeInterval)
            {
                GenerateIncomeLocal();
                incomeTimer = 0f;
            }
        }

#if UNITY_NETCODE
        private void UpdateNetworkMode()
        {
            if (!IsServer) return;

            incomeTimer += Time.deltaTime;

            if (incomeTimer >= incomeInterval)
            {
                GenerateIncomeNetwork();
                incomeTimer = 0f;
            }
        }

        private void GenerateIncomeNetwork()
        {
            networkGold.Value += networkIncome.Value;
            networkIncome.Value += incomeIncrement;
        }
#endif

        private void GenerateIncomeLocal()
        {
            localGold += localIncome;
            OnGoldChanged?.Invoke(localGold);

            localIncome += incomeIncrement;
            OnIncomeChanged?.Invoke(localIncome);
        }

        /// <summary>
        /// 增加金币
        /// </summary>
        public void AddGold(int amount)
        {
            if (IsLocalMode)
            {
                localGold += amount;
                OnGoldChanged?.Invoke(localGold);
            }
#if UNITY_NETCODE
            else if (IsServer)
            {
                networkGold.Value += amount;
            }
#endif
        }

        /// <summary>
        /// 消耗金币
        /// </summary>
        public bool SpendGold(int amount)
        {
            if (IsLocalMode)
            {
                if (localGold >= amount)
                {
                    localGold -= amount;
                    OnGoldChanged?.Invoke(localGold);
                    return true;
                }
                return false;
            }
#if UNITY_NETCODE
            else if (IsServer)
            {
                if (networkGold.Value >= amount)
                {
                    networkGold.Value -= amount;
                    return true;
                }
            }
#endif
            return false;
        }

        /// <summary>
        /// 检查是否有足够的金币
        /// </summary>
        public bool HasEnoughGold(int amount)
        {
#if UNITY_NETCODE
            return IsLocalMode ? localGold >= amount : networkGold.Value >= amount;
#else
            return localGold >= amount;
#endif
        }

        /// <summary>
        /// 增加收入
        /// </summary>
        public void AddIncome(int amount)
        {
            if (IsLocalMode)
            {
                localIncome += amount;
                OnIncomeChanged?.Invoke(localIncome);
            }
#if UNITY_NETCODE
            else if (IsServer)
            {
                networkIncome.Value += amount;
            }
#endif
        }
    }
}
