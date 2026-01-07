using UnityEngine;
using CastleWars.Core;
using System;

namespace CastleWars.Economy
{
    /// <summary>
    /// 玩家经济系统 - 管理金币和收入（纯本地模式）
    /// </summary>
    public class PlayerEconomy : MonoBehaviour
    {
        [Header("初始资源")]
        [SerializeField] private int startingGold = 500;

        [Header("收入设置")]
        [SerializeField] private int baseIncome = 10;
        [SerializeField] private float incomeInterval = 1f;
        [SerializeField] private int incomeIncrement = 1;

        // 资源
        private int gold;
        private int income;

        // 收入计时器
        private float incomeTimer = 0f;

        // 事件
        public event Action<int> OnGoldChanged;
        public event Action<int> OnIncomeChanged;

        // 属性
        public int Gold => gold;
        public int Income => income;

        private void Start()
        {
            gold = startingGold;
            income = baseIncome;
            OnGoldChanged?.Invoke(gold);
            OnIncomeChanged?.Invoke(income);
        }

        private void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Playing)
                return;

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
            gold += income;
            OnGoldChanged?.Invoke(gold);

            income += incomeIncrement;
            OnIncomeChanged?.Invoke(income);
        }

        /// <summary>
        /// 增加金币
        /// </summary>
        public void AddGold(int amount)
        {
            gold += amount;
            OnGoldChanged?.Invoke(gold);
        }

        /// <summary>
        /// 消耗金币
        /// </summary>
        public bool SpendGold(int amount)
        {
            if (gold >= amount)
            {
                gold -= amount;
                OnGoldChanged?.Invoke(gold);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 检查是否有足够的金币
        /// </summary>
        public bool HasEnoughGold(int amount)
        {
            return gold >= amount;
        }

        /// <summary>
        /// 增加收入
        /// </summary>
        public void AddIncome(int amount)
        {
            income += amount;
            OnIncomeChanged?.Invoke(income);
        }
    }
}
