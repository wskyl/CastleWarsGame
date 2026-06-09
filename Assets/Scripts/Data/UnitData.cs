using UnityEngine;

namespace CastleWars.Data
{
    /// <summary>
    /// 单位类型枚举
    /// </summary>
    public enum UnitType
    {
        Ground,     // 地面单位
        Air         // 空中单位
    }

    /// <summary>
    /// 攻击类型枚举
    /// </summary>
    public enum AttackType
    {
        Melee,      // 近战
        Ranged,     // 远程物理
        Magic,      // 魔法
        Siege       // 攻城
    }

    /// <summary>
    /// 护甲类型枚举
    /// </summary>
    public enum ArmorType
    {
        None,       // 无甲
        Light,      // 轻甲
        Medium,     // 中甲
        Heavy,      // 重甲
        Dragon      // 龙鳞（30%减伤）
    }

    /// <summary>
    /// 单位数据ScriptableObject
    /// 定义单位的所有属性配置
    /// </summary>
    [CreateAssetMenu(fileName = "NewUnitData", menuName = "CastleWars/Unit Data")]
    public class UnitData : ScriptableObject
    {
        [Header("基础信息")]
        [Tooltip("单位名称")]
        public string unitName;

        [Tooltip("单位图标")]
        public Sprite icon;

        [Tooltip("单位预制体")]
        public GameObject prefab;

        [Tooltip("单位等级(T1/T2/T3)")]
        [Range(1, 3)]
        public int tier = 1;

        [Header("基础属性")]
        [Tooltip("最大生命值")]
        [Min(1)]
        public float maxHealth = 100f;

        [Tooltip("移动速度")]
        [Min(0.1f)]
        public float moveSpeed = 3f;

        [Tooltip("单位类型")]
        public UnitType unitType = UnitType.Ground;

        [Header("攻击属性")]
        [Tooltip("攻击力")]
        [Min(1)]
        public float attackDamage = 10f;

        [Tooltip("攻击速度（次/秒）")]
        [Min(0.1f)]
        public float attackSpeed = 1f;

        [Tooltip("攻击范围")]
        [Min(0.5f)]
        public float attackRange = 1.5f;

        [Tooltip("攻击类型")]
        public AttackType attackType = AttackType.Melee;

        [Header("防御属性")]
        [Tooltip("护甲类型")]
        public ArmorType armorType = ArmorType.None;

        [Header("AOE属性")]
        [Tooltip("是否AOE攻击")]
        public bool hasAOE = false;

        [Tooltip("AOE范围")]
        [Min(0)]
        public float aoeRadius = 0f;

        [Header("特殊能力")]
        [Tooltip("是否可以攻击空中单位")]
        public bool canAttackAir = true;

        [Tooltip("是否只能攻击空中单位（防空单位）")]
        public bool antiAirOnly = false;

        [Tooltip("对建筑伤害倍率")]
        [Min(1f)]
        public float buildingDamageMultiplier = 1f;

        [Tooltip("克制的单位类型（造成双倍伤害）")]
        public UnitData[] counterUnits;

        [Header("投射物（远程单位）")]
        [Tooltip("投射物预制体")]
        public GameObject projectilePrefab;

        [Tooltip("投射物速度")]
        [Min(1f)]
        public float projectileSpeed = 10f;

        [Header("特殊效果")]
        [Tooltip("光环效果：周围友军攻击加成百分比")]
        [Range(0f, 1f)]
        public float auraAttackBonus = 0f;

        [Tooltip("光环范围")]
        [Min(0)]
        public float auraRange = 0f;

        [Tooltip("击晕几率")]
        [Range(0f, 1f)]
        public float stunChance = 0f;

        [Tooltip("击晕持续时间")]
        [Min(0)]
        public float stunDuration = 0f;

        /// <summary>
        /// 计算对目标的实际伤害
        /// </summary>
        /// <param name="targetArmor">目标护甲类型</param>
        /// <param name="isBuilding">是否是建筑</param>
        /// <returns>实际伤害值</returns>
        public float CalculateDamage(ArmorType targetArmor, bool isBuilding = false)
        {
            float damage = attackDamage;

            // 建筑伤害倍率
            if (isBuilding)
            {
                damage *= buildingDamageMultiplier;
            }

            // 护甲减伤计算
            float armorReduction = GetArmorReduction(targetArmor);
            damage *= (1f - armorReduction);

            // 魔法攻击对重甲有额外伤害
            if (attackType == AttackType.Magic && targetArmor == ArmorType.Heavy)
            {
                damage *= 1.5f;
            }

            return damage;
        }

        /// <summary>
        /// 获取护甲减伤比例
        /// </summary>
        private float GetArmorReduction(ArmorType armor)
        {
            return armor switch
            {
                ArmorType.None => 0f,
                ArmorType.Light => 0.1f,
                ArmorType.Medium => 0.2f,
                ArmorType.Heavy => 0.3f,
                ArmorType.Dragon => 0.3f,
                _ => 0f
            };
        }

        /// <summary>
        /// 检查是否克制目标单位
        /// </summary>
        public bool IsCounterTo(UnitData target)
        {
            if (counterUnits == null) return false;

            foreach (var unit in counterUnits)
            {
                if (unit == target) return true;
            }
            return false;
        }

        /// <summary>
        /// 检查是否可以攻击目标
        /// </summary>
        public bool CanAttack(UnitType targetType)
        {
            if (antiAirOnly && targetType == UnitType.Ground)
                return false;

            if (!canAttackAir && targetType == UnitType.Air)
                return false;

            return true;
        }
    }
}
