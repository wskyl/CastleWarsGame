using UnityEngine;
using System;

namespace CastleWars.Core
{
    /// <summary>
    /// 本地玩家 - 在本地模式下管理玩家状态
    /// </summary>
    public class LocalPlayer : MonoBehaviour
    {
        [Header("玩家信息")]
        public int PlayerId { get; private set; }
        public string PlayerName { get; private set; }
        public bool IsAI { get; private set; }
        public bool IsLocalPlayer => !IsAI;

        [Header("资源")]
        [SerializeField] private int startingGold = 500;
        [SerializeField] private int baseIncome = 10;
        [SerializeField] private float incomeInterval = 1f;
        [SerializeField] private int incomeIncrement = 1;

        private int currentGold;
        private int currentIncome;
        private float incomeTimer = 0f;

        // 事件
        public event Action<int> OnGoldChanged;
        public event Action<int> OnIncomeChanged;

        public int Gold => currentGold;
        public int Income => currentIncome;

        /// <summary>
        /// 初始化玩家
        /// </summary>
        public void Initialize(int playerId, bool isAI = false)
        {
            PlayerId = playerId;
            IsAI = isAI;
            PlayerName = isAI ? $"AI Player {playerId}" : $"Player {playerId}";

            // 初始化资源
            currentGold = startingGold;
            currentIncome = baseIncome;

            OnGoldChanged?.Invoke(currentGold);
            OnIncomeChanged?.Invoke(currentIncome);

            Debug.Log($"Player {playerId} initialized with {currentGold} gold, {currentIncome}/s income");
        }

        private void Update()
        {
            if (!LocalGameMode.IsLocalMode) return;
            if (LocalGameMode.Instance.CurrentState != GameState.Playing) return;

            // 收入计时
            incomeTimer += Time.deltaTime;
            if (incomeTimer >= incomeInterval)
            {
                GenerateIncome();
                incomeTimer = 0f;
            }

            // AI逻辑
            if (IsAI)
            {
                UpdateAI();
            }
        }

        private void GenerateIncome()
        {
            AddGold(currentIncome);
            currentIncome += incomeIncrement;
            OnIncomeChanged?.Invoke(currentIncome);
        }

        /// <summary>
        /// 增加金币
        /// </summary>
        public void AddGold(int amount)
        {
            currentGold += amount;
            OnGoldChanged?.Invoke(currentGold);
        }

        /// <summary>
        /// 消耗金币
        /// </summary>
        public bool SpendGold(int amount)
        {
            if (currentGold >= amount)
            {
                currentGold -= amount;
                OnGoldChanged?.Invoke(currentGold);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 检查是否有足够金币
        /// </summary>
        public bool HasEnoughGold(int amount)
        {
            return currentGold >= amount;
        }

        /// <summary>
        /// 增加收入
        /// </summary>
        public void AddIncome(int amount)
        {
            currentIncome += amount;
            OnIncomeChanged?.Invoke(currentIncome);
        }

        #region AI Logic

        private float aiActionTimer = 0f;
        private float aiActionInterval = 3f; // AI每3秒决策一次

        private void UpdateAI()
        {
            aiActionTimer += Time.deltaTime;
            if (aiActionTimer >= aiActionInterval)
            {
                MakeAIDecision();
                aiActionTimer = 0f;
            }
        }

        private void MakeAIDecision()
        {
            // 简单AI逻辑：随机决定是否建造或出兵
            float decision = UnityEngine.Random.value;

            if (decision < 0.6f && currentGold >= 100)
            {
                // 60%概率尝试训练单位
                TryTrainUnit();
            }
            else if (currentGold >= 300)
            {
                // 有足够金币时尝试建造建筑
                TryBuildBuilding();
            }
        }

        private void TryTrainUnit()
        {
            // 简化的训练逻辑 - 实际项目中需要与单位生成系统集成
            if (SpendGold(100))
            {
                Debug.Log($"AI Player {PlayerId} trained a unit");
                // TODO: 实际生成单位
            }
        }

        private void TryBuildBuilding()
        {
            // 简化的建造逻辑 - 实际项目中需要与建筑系统集成
            if (SpendGold(300))
            {
                Debug.Log($"AI Player {PlayerId} built a building");
                AddIncome(5); // 建筑增加收入
            }
        }

        #endregion
    }
}
