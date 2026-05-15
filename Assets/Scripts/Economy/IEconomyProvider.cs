using System;

namespace CastleWars.Economy
{
    /// <summary>
    /// 经济系统接口
    /// 提供PlayerEconomy和LocalPlayerEconomyAdapter的统一接口
    /// 使得BuildingUI和ResourceDisplay可以同时兼容网络模式和本地模式
    /// </summary>
    public interface IEconomyProvider
    {
        int Gold { get; }
        int Income { get; }
        int PlayerId { get; }

        event Action<int> OnGoldChanged;
        event Action<int> OnIncomeChanged;

        void AddGold(int amount);
        bool SpendGold(int amount);
        bool HasEnoughGold(int amount);
        void AddIncome(int amount);
    }
}
