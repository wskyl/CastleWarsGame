using UnityEngine;
using CastleWars.Economy;
using CastleWars.Core;

namespace CastleWars.UI
{
    /// <summary>
    /// 资源显示UI - 显示玩家的金币和收入
    /// 支持本地模式和网络模式
    /// 不依赖TMPro，使用Debug.Log输出
    /// </summary>
    public class ResourceDisplay : MonoBehaviour
    {
        private PlayerEconomy economy;
        private LocalPlayer localPlayer;

        private int lastGold = -1;
        private int lastIncome = -1;

        private void Start()
        {
            FindPlayerEconomy();
        }

        private void FindPlayerEconomy()
        {
            // 优先使用本地模式
            if (LocalGameMode.IsLocalMode && LocalGameMode.Instance != null)
            {
                localPlayer = LocalGameMode.Instance.GetPlayer(1);
                if (localPlayer != null)
                {
                    localPlayer.OnGoldChanged += UpdateGoldDisplay;
                    localPlayer.OnIncomeChanged += UpdateIncomeDisplay;

                    UpdateGoldDisplay(localPlayer.Gold);
                    UpdateIncomeDisplay(localPlayer.Income);
                    return;
                }
            }

            // 网络模式
            var networkPlayers = FindObjectsOfType<NetworkPlayer>();
            foreach (var player in networkPlayers)
            {
                economy = player.GetComponent<PlayerEconomy>();
                if (economy != null)
                {
                    economy.OnGoldChanged += UpdateGoldDisplay;
                    economy.OnIncomeChanged += UpdateIncomeDisplay;

                    UpdateGoldDisplay(economy.Gold);
                    UpdateIncomeDisplay(economy.Income);
                    break;
                }
            }
        }

        private void UpdateGoldDisplay(int gold)
        {
            if (gold != lastGold)
            {
                lastGold = gold;
                Debug.Log($"[ResourceDisplay] Gold: {gold}");
            }
        }

        private void UpdateIncomeDisplay(int income)
        {
            if (income != lastIncome)
            {
                lastIncome = income;
                Debug.Log($"[ResourceDisplay] Income: +{income}/s");
            }
        }

        private void OnDestroy()
        {
            if (economy != null)
            {
                economy.OnGoldChanged -= UpdateGoldDisplay;
                economy.OnIncomeChanged -= UpdateIncomeDisplay;
            }

            if (localPlayer != null)
            {
                localPlayer.OnGoldChanged -= UpdateGoldDisplay;
                localPlayer.OnIncomeChanged -= UpdateIncomeDisplay;
            }
        }
    }
}
