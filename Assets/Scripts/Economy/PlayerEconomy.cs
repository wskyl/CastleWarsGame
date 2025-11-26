using UnityEngine;
using Unity.Netcode;
using System;

namespace CastleWars.Economy
{
    /// <summary>
    /// 玩家经济系统 - 管理金币和收入
    /// </summary>
    public class PlayerEconomy : NetworkBehaviour
    {
        [Header("初始资源")]
        [SerializeField] private int startingGold = 500;

        [Header("收入设置")]
        [SerializeField] private int baseIncome = 10; // 基础收入
        [SerializeField] private float incomeInterval = 1f; // 收入间隔（秒）
        [SerializeField] private int incomeIncrement = 1; // 每次收入增长量

        // 当前资源
        private NetworkVariable<int> currentGold = new NetworkVariable<int>();
        private NetworkVariable<int> currentIncome = new NetworkVariable<int>();

        // 收入计时器
        private float incomeTimer = 0f;

        // 事件
        public event Action<int> OnGoldChanged;
        public event Action<int> OnIncomeChanged;

        public int Gold => currentGold.Value;
        public int Income => currentIncome.Value;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsServer)
            {
                currentGold.Value = startingGold;
                currentIncome.Value = baseIncome;
            }

            // 客户端监听资源变化
            currentGold.OnValueChanged += OnGoldValueChanged;
            currentIncome.OnValueChanged += OnIncomeValueChanged;
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            currentGold.OnValueChanged -= OnGoldValueChanged;
            currentIncome.OnValueChanged -= OnIncomeValueChanged;
        }

        private void Update()
        {
            // 只在服务器上执行收入逻辑
            if (!IsServer) return;

            incomeTimer += Time.deltaTime;

            if (incomeTimer >= incomeInterval)
            {
                GenerateIncome();
                incomeTimer = 0f;
            }
        }

        /// <summary>
        /// 生成收入
        /// </summary>
        private void GenerateIncome()
        {
            AddGold(currentIncome.Value);

            // 收入逐渐增长
            currentIncome.Value += incomeIncrement;
        }

        /// <summary>
        /// 增加金币
        /// </summary>
        public void AddGold(int amount)
        {
            if (!IsServer) return;
            currentGold.Value += amount;
        }

        /// <summary>
        /// 消耗金币
        /// </summary>
        public bool SpendGold(int amount)
        {
            if (!IsServer) return false;

            if (currentGold.Value >= amount)
            {
                currentGold.Value -= amount;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 检查是否有足够的金币
        /// </summary>
        public bool HasEnoughGold(int amount)
        {
            return currentGold.Value >= amount;
        }

        /// <summary>
        /// 增加收入（购买建筑时可能增加收入）
        /// </summary>
        public void AddIncome(int amount)
        {
            if (!IsServer) return;
            currentIncome.Value += amount;
        }

        private void OnGoldValueChanged(int oldValue, int newValue)
        {
            OnGoldChanged?.Invoke(newValue);
        }

        private void OnIncomeValueChanged(int oldValue, int newValue)
        {
            OnIncomeChanged?.Invoke(newValue);
        }
    }
}
