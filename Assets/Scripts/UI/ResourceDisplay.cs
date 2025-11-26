using UnityEngine;
using TMPro;
using CastleWars.Economy;
using CastleWars.Core;

namespace CastleWars.UI
{
    /// <summary>
    /// 资源显示UI - 显示玩家的金币和收入
    /// </summary>
    public class ResourceDisplay : MonoBehaviour
    {
        [Header("UI组件")]
        [SerializeField] private TextMeshProUGUI goldText;
        [SerializeField] private TextMeshProUGUI incomeText;

        private PlayerEconomy economy;

        private void Start()
        {
            // 查找本地玩家的经济组件
            FindLocalPlayerEconomy();
        }

        private void FindLocalPlayerEconomy()
        {
            var networkPlayers = FindObjectsOfType<NetworkPlayer>();
            foreach (var player in networkPlayers)
            {
                if (player.IsOwner)
                {
                    economy = player.GetComponent<PlayerEconomy>();
                    if (economy != null)
                    {
                        // 订阅事件
                        economy.OnGoldChanged += UpdateGoldDisplay;
                        economy.OnIncomeChanged += UpdateIncomeDisplay;

                        // 初始显示
                        UpdateGoldDisplay(economy.Gold);
                        UpdateIncomeDisplay(economy.Income);
                    }
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
        }
    }
}
