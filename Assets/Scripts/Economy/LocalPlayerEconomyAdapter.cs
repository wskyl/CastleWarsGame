using UnityEngine;
using System;
using CastleWars.Core;

namespace CastleWars.Economy
{
    /// <summary>
    /// 本地玩家经济适配器
    /// 将LocalPlayer的经济功能适配为IEconomyProvider接口
    /// 使得BuildingUI和ResourceDisplay在本地模式下可以正常工作
    /// </summary>
    public class LocalPlayerEconomyAdapter : MonoBehaviour, IEconomyProvider
    {
        private LocalPlayer _localPlayer;

        public int Gold => _localPlayer != null ? _localPlayer.Gold : 0;
        public int Income => _localPlayer != null ? _localPlayer.Income : 0;
        public int PlayerId => _localPlayer != null ? _localPlayer.PlayerId : 0;

        public event Action<int> OnGoldChanged;
        public event Action<int> OnIncomeChanged;

        /// <summary>
        /// 初始化适配器
        /// </summary>
        public void Initialize(LocalPlayer localPlayer)
        {
            _localPlayer = localPlayer;

            if (_localPlayer != null)
            {
                _localPlayer.OnGoldChanged += OnLocalGoldChanged;
                _localPlayer.OnIncomeChanged += OnLocalIncomeChanged;
            }
        }

        private void OnLocalGoldChanged(int newGold)
        {
            OnGoldChanged?.Invoke(newGold);
        }

        private void OnLocalIncomeChanged(int newIncome)
        {
            OnIncomeChanged?.Invoke(newIncome);
        }

        public void AddGold(int amount)
        {
            if (_localPlayer != null)
            {
                _localPlayer.AddGold(amount);
            }
        }

        public bool SpendGold(int amount)
        {
            if (_localPlayer != null)
            {
                return _localPlayer.SpendGold(amount);
            }
            return false;
        }

        public bool HasEnoughGold(int amount)
        {
            if (_localPlayer != null)
            {
                return _localPlayer.HasEnoughGold(amount);
            }
            return false;
        }

        public void AddIncome(int amount)
        {
            if (_localPlayer != null)
            {
                _localPlayer.AddIncome(amount);
            }
        }

        private void OnDestroy()
        {
            if (_localPlayer != null)
            {
                _localPlayer.OnGoldChanged -= OnLocalGoldChanged;
                _localPlayer.OnIncomeChanged -= OnLocalIncomeChanged;
            }
        }
    }
}
