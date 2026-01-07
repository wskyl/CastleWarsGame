using UnityEngine;
using TMPro;
using CastleWars.Economy;
using CastleWars.Core;

namespace CastleWars.UI
{
    /// <summary>
    /// 资源显示UI - 显示玩家的金币和收入
    /// 支持本地模式和网络模式
    /// </summary>
    public class ResourceDisplay : MonoBehaviour
    {
        [Header("UI组件")]
        [SerializeField] private TextMeshProUGUI goldText;
        [SerializeField] private TextMeshProUGUI incomeText;

        private PlayerEconomy economy;
        private LocalPlayer localPlayer;

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
                // 在本地模式下，使用第一个玩家
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
            if (goldText != null)
            {
                goldText.text = $"Gold: {gold}";
            }
        }

        private void UpdateIncomeDisplay(int income)
        {
            if (incomeText != null)
            {
                incomeText.text = $"+{income}/s";
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
