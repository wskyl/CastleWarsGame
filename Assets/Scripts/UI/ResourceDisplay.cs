using UnityEngine;
using CastleWars.Economy;
using CastleWars.Core;

namespace CastleWars.UI
{
    /// <summary>
    /// 资源显示UI - 显示玩家的金币和收入（纯本地模式）
    /// </summary>
    public class ResourceDisplay : MonoBehaviour
    {
        private PlayerController playerController;

        private int lastGold = -1;
        private int lastIncome = -1;

        private void Start()
        {
            FindPlayer();
        }

        private void FindPlayer()
        {
            if (GameManager.Instance != null)
            {
                playerController = GameManager.Instance.GetPlayer(1);
                if (playerController != null)
                {
                    playerController.OnGoldChanged += UpdateGoldDisplay;
                    playerController.OnIncomeChanged += UpdateIncomeDisplay;

                    UpdateGoldDisplay(playerController.Gold);
                    UpdateIncomeDisplay(playerController.Income);
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
            if (playerController != null)
            {
                playerController.OnGoldChanged -= UpdateGoldDisplay;
                playerController.OnIncomeChanged -= UpdateIncomeDisplay;
            }
        }
    }
}
