using UnityEngine;

namespace CastleWars.Data
{
    /// <summary>
    /// 建筑数据配置
    /// </summary>
    [CreateAssetMenu(fileName = "New Building", menuName = "CastleWars/Building Data")]
    public class BuildingData : ScriptableObject
    {
        [Header("基础信息")]
        public string buildingName;
        public string description;
        public GameObject prefab;
        public Sprite icon;

        [Header("建造成本")]
        public int goldCost = 100;
        public float buildTime = 3f;

        [Header("生产单位")]
        public UnitData producedUnit;
        public float productionInterval = 10f; // 每多少秒生产一个单位

        [Header("建筑属性")]
        public BuildingTier tier = BuildingTier.Tier1;
        public BuildingCategory category;

        [Header("前置条件")]
        public BuildingData[] requiredBuildings; // 需要先建造的建筑
        public int playerLevelRequired = 1;
    }

    public enum BuildingTier
    {
        Tier1,  // 初级建筑
        Tier2,  // 中级建筑
        Tier3,  // 高级建筑
        Special // 特殊建筑
    }

    public enum BuildingCategory
    {
        Infantry,   // 步兵营
        Ranged,     // 射手营
        Cavalry,    // 骑兵营
        Magic,      // 魔法塔
        Air,        // 空军基地
        Special     // 特殊建筑
    }
}
