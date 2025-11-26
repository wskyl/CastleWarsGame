using UnityEngine;

namespace CastleWars.Data
{
    /// <summary>
    /// 单位数据配置（ScriptableObject）
    /// </summary>
    [CreateAssetMenu(fileName = "New Unit", menuName = "CastleWars/Unit Data")]
    public class UnitData : ScriptableObject
    {
        [Header("基础属性")]
        public string unitName;
        public GameObject prefab;
        public Sprite icon;

        [Header("战斗属性")]
        public float maxHealth = 100f;
        public float moveSpeed = 3f;
        public float attackDamage = 10f;
        public float attackRange = 5f;
        public float attackSpeed = 1f; // 每秒攻击次数

        [Header("单位类型")]
        public UnitType unitType;
        public AttackType attackType;
        public ArmorType armorType;

        [Header("生产设置")]
        public float productionTime = 5f; // 生产时间
        public int goldCost = 100; // 建造该单位的建筑成本

        [Header("特殊能力")]
        public bool canFly = false;
        public bool canAttackAir = false;
        public bool hasAOE = false;
        public float aoeRadius = 0f;
    }

    public enum UnitType
    {
        Ground,     // 地面单位
        Air         // 空中单位
    }

    public enum AttackType
    {
        Melee,      // 近战
        Ranged,     // 远程
        Magic       // 魔法
    }

    public enum ArmorType
    {
        Light,      // 轻甲（对魔法弱）
        Heavy,      // 重甲（对物理强）
        Unarmored   // 无甲（平衡）
    }
}
