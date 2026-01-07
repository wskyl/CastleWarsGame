using UnityEngine;
using CastleWars.Data;

namespace CastleWars.Core
{
    /// <summary>
    /// 玩家控制器 - 管理玩家资源和操作（纯本地模式）
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        [Header("玩家信息")]
        public int PlayerId { get; private set; }
        public bool IsAI { get; private set; }

        [Header("资源设置")]
        [SerializeField] private int startingGold = 500;
        [SerializeField] private int baseIncome = 10;
        [SerializeField] private float incomeInterval = 1f;

        [Header("单位设置")]
        [SerializeField] private int unitCost = 100;
        [SerializeField] private float aiSpawnInterval = 3f;

        // 资源
        private int gold;
        private int income;
        private float incomeTimer;
        private float aiSpawnTimer;

        // 事件
        public System.Action<int> OnGoldChanged;
        public System.Action<int> OnIncomeChanged;

        // 属性
        public int Gold => gold;
        public int Income => income;

        /// <summary>
        /// 初始化玩家
        /// </summary>
        public void Initialize(int playerId, bool isAI)
        {
            PlayerId = playerId;
            IsAI = isAI;
            gold = startingGold;
            income = baseIncome;

            Debug.Log($"[PlayerController] Player {playerId} initialized. AI: {isAI}, Gold: {gold}");
        }

        private void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Playing)
                return;

            UpdateIncome();

            if (IsAI)
            {
                UpdateAI();
            }
        }

        /// <summary>
        /// 更新收入
        /// </summary>
        private void UpdateIncome()
        {
            incomeTimer += Time.deltaTime;
            if (incomeTimer >= incomeInterval)
            {
                AddGold(income);
                incomeTimer = 0f;
            }
        }

        /// <summary>
        /// AI 行为更新
        /// </summary>
        private void UpdateAI()
        {
            aiSpawnTimer += Time.deltaTime;
            if (aiSpawnTimer >= aiSpawnInterval)
            {
                TrySpawnUnit();
                aiSpawnTimer = 0f;
            }
        }

        /// <summary>
        /// 尝试生成单位
        /// </summary>
        public bool TrySpawnUnit(UnitData unitData = null)
        {
            if (gold < unitCost)
            {
                return false;
            }

            SpendGold(unitCost);

            if (UnitSpawner.Instance != null)
            {
                UnitSpawner.Instance.SpawnUnit(PlayerId, unitData);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 添加金币
        /// </summary>
        public void AddGold(int amount)
        {
            gold += amount;
            OnGoldChanged?.Invoke(gold);
        }

        /// <summary>
        /// 花费金币
        /// </summary>
        public bool SpendGold(int amount)
        {
            if (gold < amount) return false;

            gold -= amount;
            OnGoldChanged?.Invoke(gold);
            return true;
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
