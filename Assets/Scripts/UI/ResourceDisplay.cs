using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CastleWars.Economy;
using CastleWars.Core;

namespace CastleWars.UI
{
    /// <summary>
    /// 资源显示UI
    /// 显示玩家的金币、收入等资源信息
    /// </summary>
    public class ResourceDisplay : MonoBehaviour
    {
        [Header("金币显示")]
        [Tooltip("金币文本")]
        [SerializeField] private TextMeshProUGUI goldText;

        [Tooltip("金币图标")]
        [SerializeField] private Image goldIcon;

        [Header("收入显示")]
        [Tooltip("收入文本")]
        [SerializeField] private TextMeshProUGUI incomeText;

        [Tooltip("收入图标")]
        [SerializeField] private Image incomeIcon;

        [Header("动画设置")]
        [Tooltip("金币变化动画")]
        [SerializeField] private bool animateGoldChange = true;

        [Tooltip("动画持续时间")]
        [SerializeField] private float animationDuration = 0.3f;

        [Tooltip("金币增加颜色")]
        [SerializeField] private Color goldIncreaseColor = Color.green;

        [Tooltip("金币减少颜色")]
        [SerializeField] private Color goldDecreaseColor = Color.red;

        // 组件引用
        private PlayerEconomy _economy;

        // 动画状态
        private int _displayedGold;
        private int _targetGold;
        private float _animationTimer;
        private bool _isAnimating;
        private Color _originalGoldColor;

        private void Start()
        {
            FindPlayerEconomy();

            // 保存原始颜色
            if (goldText != null)
            {
                _originalGoldColor = goldText.color;
            }
        }

        /// <summary>
        /// 查找玩家经济组件
        /// </summary>
        private void FindPlayerEconomy()
        {
            // 查找本地玩家
            NetworkPlayer[] players = FindObjectsOfType<NetworkPlayer>();
            foreach (var player in players)
            {
                if (player.IsLocalPlayer())
                {
                    _economy = player.GetEconomy();
                    break;
                }
            }

            // 订阅事件
            if (_economy != null)
            {
                _economy.OnGoldChanged += OnGoldChanged;
                _economy.OnIncomeChanged += OnIncomeChanged;

                // 初始化显示
                _displayedGold = _economy.Gold;
                _targetGold = _economy.Gold;
                UpdateGoldDisplay(_economy.Gold);
                UpdateIncomeDisplay(_economy.Income);
            }
        }

        /// <summary>
        /// 设置经济组件引用
        /// </summary>
        public void SetEconomy(PlayerEconomy economy)
        {
            // 取消旧订阅
            if (_economy != null)
            {
                _economy.OnGoldChanged -= OnGoldChanged;
                _economy.OnIncomeChanged -= OnIncomeChanged;
            }

            _economy = economy;

            // 新订阅
            if (_economy != null)
            {
                _economy.OnGoldChanged += OnGoldChanged;
                _economy.OnIncomeChanged += OnIncomeChanged;

                _displayedGold = _economy.Gold;
                _targetGold = _economy.Gold;
                UpdateGoldDisplay(_economy.Gold);
                UpdateIncomeDisplay(_economy.Income);
            }
        }

        private void Update()
        {
            // 金币数字动画
            if (_isAnimating && animateGoldChange)
            {
                _animationTimer += Time.deltaTime;
                float progress = Mathf.Clamp01(_animationTimer / animationDuration);

                // 平滑插值
                _displayedGold = Mathf.RoundToInt(Mathf.Lerp(_displayedGold, _targetGold, progress));

                if (goldText != null)
                {
                    goldText.text = FormatGold(_displayedGold);
                }

                // 动画结束
                if (progress >= 1f)
                {
                    _isAnimating = false;
                    _displayedGold = _targetGold;

                    // 恢复原始颜色
                    if (goldText != null)
                    {
                        goldText.color = _originalGoldColor;
                    }
                }
            }
        }

        #region 事件处理

        private void OnGoldChanged(int newGold)
        {
            if (animateGoldChange)
            {
                // 开始动画
                int previousGold = _targetGold;
                _targetGold = newGold;
                _animationTimer = 0f;
                _isAnimating = true;

                // 设置颜色
                if (goldText != null)
                {
                    goldText.color = newGold > previousGold ? goldIncreaseColor : goldDecreaseColor;
                }
            }
            else
            {
                UpdateGoldDisplay(newGold);
            }
        }

        private void OnIncomeChanged(int newIncome)
        {
            UpdateIncomeDisplay(newIncome);
        }

        #endregion

        #region 显示更新

        /// <summary>
        /// 更新金币显示
        /// </summary>
        private void UpdateGoldDisplay(int gold)
        {
            if (goldText != null)
            {
                goldText.text = FormatGold(gold);
            }

            _displayedGold = gold;
            _targetGold = gold;
        }

        /// <summary>
        /// 更新收入显示
        /// </summary>
        private void UpdateIncomeDisplay(int income)
        {
            if (incomeText != null)
            {
                incomeText.text = $"+{income}/s";
            }
        }

        /// <summary>
        /// 格式化金币数字
        /// </summary>
        private string FormatGold(int gold)
        {
            if (gold >= 10000)
            {
                return $"{gold / 1000f:F1}K";
            }
            else if (gold >= 1000)
            {
                return $"{gold:N0}";
            }
            return gold.ToString();
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 显示金币变化提示
        /// </summary>
        public void ShowGoldChange(int amount)
        {
            // 可以在这里添加浮动文字效果
            string prefix = amount >= 0 ? "+" : "";
            Debug.Log($"[ResourceDisplay] 金币变化: {prefix}{amount}");
        }

        /// <summary>
        /// 刷新显示
        /// </summary>
        public void Refresh()
        {
            if (_economy != null)
            {
                UpdateGoldDisplay(_economy.Gold);
                UpdateIncomeDisplay(_economy.Income);
            }
        }

        #endregion

        private void OnDestroy()
        {
            if (_economy != null)
            {
                _economy.OnGoldChanged -= OnGoldChanged;
                _economy.OnIncomeChanged -= OnIncomeChanged;
            }
        }
    }
}
