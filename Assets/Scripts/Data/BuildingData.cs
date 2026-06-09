using UnityEngine;

namespace CastleWars.Data
{
    /// <summary>
    /// 建筑等级枚举
    /// </summary>
    public enum BuildingTier
    {
        Tier1,      // 一级建筑（100-200金币）
        Tier2,      // 二级建筑（300-500金币）
        Tier3,      // 三级建筑（600-1000金币）
        Special     // 特殊建筑（1200-2000金币）
    }

    /// <summary>
    /// 建筑类别枚举
    /// </summary>
    public enum BuildingCategory
    {
        Infantry,   // 步兵类
        Ranged,     // 射手类
        Cavalry,    // 骑兵类
        Magic,      // 魔法类
        Air,        // 空军类
        Siege,      // 攻城类
        Special     // 特殊类
    }

    /// <summary>
    /// 建筑数据ScriptableObject
    /// 定义建筑的所有属性配置
    /// </summary>
    [CreateAssetMenu(fileName = "NewBuildingData", menuName = "CastleWars/Building Data")]
    public class BuildingData : ScriptableObject
    {
        [Header("基础信息")]
        [Tooltip("建筑名称")]
        public string buildingName;

        [Tooltip("建筑描述")]
        [TextArea(2, 4)]
        public string description;

        [Tooltip("建筑图标")]
        public Sprite icon;

        [Tooltip("建筑预制体")]
        public GameObject prefab;

        [Header("建筑分类")]
        [Tooltip("建筑等级")]
        public BuildingTier tier = BuildingTier.Tier1;

        [Tooltip("建筑类别")]
        public BuildingCategory category = BuildingCategory.Infantry;

        [Header("成本")]
        [Tooltip("建造所需金币")]
        [Min(0)]
        public int goldCost = 150;

        [Tooltip("建造时间（秒）")]
        [Min(0.1f)]
        public float buildTime = 2f;

        [Header("生产设置")]
        [Tooltip("生产的单位数据")]
        public UnitData producedUnit;

        [Tooltip("生产间隔（秒）")]
        [Min(1f)]
        public float productionInterval = 8f;

        [Tooltip("单次生产数量")]
        [Min(1)]
        public int productionCount = 1;

        [Header("科技树")]
        [Tooltip("前置建筑要求")]
        public BuildingData[] requiredBuildings;

        [Tooltip("需要的前置建筑数量（任意X个）")]
        [Min(0)]
        public int requiredBuildingCount = 0;

        [Tooltip("是否需要全部前置建筑")]
        public bool requireAllBuildings = false;

        [Header("建筑属性")]
        [Tooltip("建筑血量（0表示不可摧毁）")]
        [Min(0)]
        public float maxHealth = 0f;

        [Tooltip("是否提供被动收入")]
        public bool providesIncome = false;

        [Tooltip("每秒额外收入")]
        [Min(0)]
        public int incomePerSecond = 0;

        /// <summary>
        /// 检查是否满足前置建筑条件
        /// </summary>
        /// <param name="builtBuildings">已建造的建筑列表</param>
        /// <returns>是否满足条件</returns>
        public bool CheckPrerequisites(BuildingData[] builtBuildings)
        {
            if (requiredBuildings == null || requiredBuildings.Length == 0)
                return true;

            if (builtBuildings == null || builtBuildings.Length == 0)
                return false;

            int satisfiedCount = 0;

            foreach (var required in requiredBuildings)
            {
                foreach (var built in builtBuildings)
                {
                    if (built == required)
                    {
                        satisfiedCount++;
                        break;
                    }
                }
            }

            if (requireAllBuildings)
            {
                return satisfiedCount >= requiredBuildings.Length;
            }
            else if (requiredBuildingCount > 0)
            {
                return satisfiedCount >= requiredBuildingCount;
            }
            else
            {
                return satisfiedCount > 0;
            }
        }

        /// <summary>
        /// 获取建筑等级对应的成本范围描述
        /// </summary>
        public string GetTierCostRange()
        {
            return tier switch
            {
                BuildingTier.Tier1 => "100-200金币",
                BuildingTier.Tier2 => "300-500金币",
                BuildingTier.Tier3 => "600-1000金币",
                BuildingTier.Special => "1200-2000金币",
                _ => "未知"
            };
        }

        /// <summary>
        /// 获取前置建筑的名称列表
        /// </summary>
        public string GetPrerequisiteNames()
        {
            if (requiredBuildings == null || requiredBuildings.Length == 0)
                return "无";

            string names = "";
            for (int i = 0; i < requiredBuildings.Length; i++)
            {
                if (requiredBuildings[i] != null)
                {
                    names += requiredBuildings[i].buildingName;
                    if (i < requiredBuildings.Length - 1)
                    {
                        names += requireAllBuildings ? " + " : " / ";
                    }
                }
            }
            return names;
        }
    }
}
