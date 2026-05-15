using UnityEngine;
using System;
using Unity.Netcode;

namespace CastleWars.Economy
{
    /// <summary>
    /// 玩家经济系统
    /// 管理金币、收入、资源获取
    /// </summary>
    public class PlayerEconomy : NetworkBehaviour, IEconomyProvider
    {
        [Header("初始资源")]
        [Tooltip("初始金币")]
        [SerializeField] private int startingGold = 500;

        [Header("收入设置")]
        [Tooltip("基础收入（金币/秒）")]
        [SerializeField] private int baseIncome = 10;

        [Tooltip("收入发放间隔（秒）")]
        [SerializeField] private float incomeInterval = 1f;

        [Tooltip("每秒收入增长")]
        [SerializeField] private int incomeIncrement = 1;

        [Tooltip("最大收入上限")]
        [SerializeField] private int maxIncome = 50;

        // 网络同步变量
        private NetworkVariable<int> _currentGold = new NetworkVariable<int>(
            500,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        private NetworkVariable<int> _currentIncome = new NetworkVariable<int>(
            10,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        private NetworkVariable<int> _playerId = new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        // 收入计时器
        private float _incomeTimer = 0f;

        // 事件
        public event Action<int> OnGoldChanged;
        public event Action<int> OnIncomeChanged;
        public event Action<int> OnGoldSpent;
        public event Action<int> OnGoldEarned;

        // 属性
        public int Gold => _currentGold.Value;
        public int Income => _currentIncome.Value;
        public int PlayerId => _playerId.Value;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            // 订阅变化事件
            _currentGold.OnValueChanged += HandleGoldChanged;
            _currentIncome.OnValueChanged += HandleIncomeChanged;

            Debug.Log($"[PlayerEconomy] 经济系统初始化 - 金币: {_currentGold.Value}, 收入: {_currentIncome.Value}");
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            _currentGold.OnValueChanged -= HandleGoldChanged;
            _currentIncome.OnValueChanged -= HandleIncomeChanged;
        }

        /// <summary>
        /// 初始化经济系统
        /// </summary>
        public void Initialize(int playerId)
        {
            if (!IsServer) return;

            _playerId.Value = playerId;
            _currentGold.Value = startingGold;
            _currentIncome.Value = baseIncome;

            Debug.Log($"[PlayerEconomy] Player {playerId} 经济初始化 - 金币: {startingGold}, 收入: {baseIncome}/秒");
        }

        private void Update()
        {
            if (!IsServer) return;

            // 收入计时
            _incomeTimer += Time.deltaTime;

            if (_incomeTimer >= incomeInterval)
            {
                GenerateIncome();
                _incomeTimer = 0f;
            }
        }

        #region 收入系统

        /// <summary>
        /// 生成收入
        /// </summary>
        private void GenerateIncome()
        {
            // 增加金币
            _currentGold.Value += _currentIncome.Value;

            // 增长收入（不超过上限）
            if (_currentIncome.Value < maxIncome)
            {
                _currentIncome.Value = Mathf.Min(_currentIncome.Value + incomeIncrement, maxIncome);
            }
        }

        /// <summary>
        /// 增加额外收入
        /// </summary>
        public void AddIncome(int amount)
        {
            if (!IsServer || amount <= 0) return;

            _currentIncome.Value = Mathf.Min(_currentIncome.Value + amount, maxIncome);
            Debug.Log($"[PlayerEconomy] 收入增加: +{amount}, 当前: {_currentIncome.Value}/秒");
        }

        /// <summary>
        /// 设置收入上限
        /// </summary>
        public void SetMaxIncome(int max)
        {
            maxIncome = max;
        }

        #endregion

        #region 金币操作

        /// <summary>
        /// 增加金币
        /// </summary>
        public void AddGold(int amount)
        {
            if (!IsServer || amount <= 0) return;

            _currentGold.Value += amount;
            OnGoldEarned?.Invoke(amount);

            Debug.Log($"[PlayerEconomy] 获得金币: +{amount}, 当前: {_currentGold.Value}");
        }

        /// <summary>
        /// 消耗金币
        /// </summary>
        /// <param name="amount">消耗数量</param>
        /// <returns>是否成功消耗</returns>
        public bool SpendGold(int amount)
        {
            if (!IsServer || amount <= 0) return false;

            if (_currentGold.Value >= amount)
            {
                _currentGold.Value -= amount;
                OnGoldSpent?.Invoke(amount);

                Debug.Log($"[PlayerEconomy] 消耗金币: -{amount}, 剩余: {_currentGold.Value}");
                return true;
            }

            Debug.LogWarning($"[PlayerEconomy] 金币不足: 需要 {amount}, 当前 {_currentGold.Value}");
            return false;
        }

        /// <summary>
        /// 检查是否有足够金币
        /// </summary>
        public bool HasEnoughGold(int amount)
        {
            return _currentGold.Value >= amount;
        }

        /// <summary>
        /// 请求消耗金币（客户端调用）
        /// </summary>
        [ServerRpc]
        public void RequestSpendGoldServerRpc(int amount, ServerRpcParams rpcParams = default)
        {
            SpendGold(amount);
        }

        #endregion

        #region 事件处理

        private void HandleGoldChanged(int previousValue, int newValue)
        {
            OnGoldChanged?.Invoke(newValue);
        }

        private void HandleIncomeChanged(int previousValue, int newValue)
        {
            OnIncomeChanged?.Invoke(newValue);
        }

        #endregion

        #region 工具方法

        /// <summary>
        /// 获取金币差额
        /// </summary>
        public int GetGoldDeficit(int required)
        {
            return Mathf.Max(0, required - _currentGold.Value);
        }

        /// <summary>
        /// 获取可负担的最大成本
        /// </summary>
        public int GetAffordableAmount()
        {
            return _currentGold.Value;
        }

        /// <summary>
        /// 计算攒够指定金币需要的时间
        /// </summary>
        public float GetTimeToAfford(int amount)
        {
            if (_currentGold.Value >= amount) return 0f;

            int deficit = amount - _currentGold.Value;
            // 简化计算，假设收入不变
            return (float)deficit / _currentIncome.Value;
        }

        /// <summary>
        /// 重置经济状态
        /// </summary>
        public void Reset()
        {
            if (!IsServer) return;

            _currentGold.Value = startingGold;
            _currentIncome.Value = baseIncome;
            _incomeTimer = 0f;
        }

        #endregion
    }
}
