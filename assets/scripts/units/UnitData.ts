/**
 * UnitData.ts
 * 兵种数据类型定义与默认配置
 */

import { ArmorType, DamageType, UnitType } from '../core/GameConstants';

/** 兵种配置（来自 JSON） */
export interface UnitConfig {
    id: string;
    name: string;
    displayName: string;
    tier: number;
    hp: number;
    atk: number;
    attackSpeed: number;           // 攻击次数/秒
    moveSpeed: number;             // 移动速度（单位/秒）
    attackRange: number;           // 攻击范围（单位）
    armorType: ArmorType;
    damageType: DamageType;
    unitType: UnitType;
    aoeRadius?: number;            // AOE攻击半径（0=单体）
    abilities: UnitAbility[];
    prefabPath?: string;
}

/** 兵种特殊能力 */
export interface UnitAbility {
    type: AbilityType;
    value?: number;                // 能力数值参数
    chance?: number;               // 触发概率（0-1）
    duration?: number;             // 持续时间（秒）
    target?: 'enemy' | 'ally' | 'building';
}

export enum AbilityType {
    ANTI_CAVALRY = 'anti_cavalry',         // 对骑兵2倍伤害
    ARMOR_PIERCE = 'armor_pierce',         // 忽略N%护甲
    STUN = 'stun',                         // 概率眩晕
    FLYING = 'flying',                     // 飞行
    AOE = 'aoe',                           // 范围攻击
    ANTI_BUILDING = 'anti_building',       // 对建筑N倍伤害
    DAMAGE_REDUCTION = 'damage_reduction', // 受到伤害减少N%
    STEALTH = 'stealth',                   // 潜行
    BACKSTAB = 'backstab',                 // 背刺额外伤害
    AURA_ATK = 'aura_atk',                // 攻击力光环
    FIRE_BREATH = 'fire_breath',           // 火焰吐息（AOE火焰）
    ANTI_AIR = 'anti_air',                // 可攻击空中单位
}

/** 兵种默认配置 */
export const DEFAULT_UNIT_CONFIGS: UnitConfig[] = [
    // ── Tier 1 ──────────────────────────────────────────────
    {
        id: 'infantry',
        name: 'Infantry',
        displayName: '步兵',
        tier: 1,
        hp: 100,
        atk: 15,
        attackSpeed: 1.0,
        moveSpeed: 3,
        attackRange: 1,
        armorType: ArmorType.HEAVY,
        damageType: DamageType.PHYSICAL,
        unitType: UnitType.GROUND,
        abilities: [],
    },
    {
        id: 'archer',
        name: 'Archer',
        displayName: '弓箭手',
        tier: 1,
        hp: 60,
        atk: 12,
        attackSpeed: 1.2,
        moveSpeed: 3,
        attackRange: 8,
        armorType: ArmorType.LIGHT,
        damageType: DamageType.PHYSICAL,
        unitType: UnitType.GROUND,
        abilities: [{ type: AbilityType.ANTI_AIR }],
    },
    {
        id: 'cavalry',
        name: 'Cavalry',
        displayName: '骑兵',
        tier: 1,
        hp: 150,
        atk: 20,
        attackSpeed: 0.8,
        moveSpeed: 5,
        attackRange: 1,
        armorType: ArmorType.MEDIUM,
        damageType: DamageType.PHYSICAL,
        unitType: UnitType.GROUND,
        abilities: [],
    },
    // ── Tier 2 ──────────────────────────────────────────────
    {
        id: 'spearman',
        name: 'Spearman',
        displayName: '枪兵',
        tier: 2,
        hp: 120,
        atk: 25,
        attackSpeed: 1.0,
        moveSpeed: 3,
        attackRange: 1,
        armorType: ArmorType.MEDIUM,
        damageType: DamageType.PHYSICAL,
        unitType: UnitType.GROUND,
        abilities: [
            { type: AbilityType.ANTI_CAVALRY, value: 2.0, target: 'enemy' },
        ],
    },
    {
        id: 'musketeer',
        name: 'Musketeer',
        displayName: '火枪手',
        tier: 2,
        hp: 80,
        atk: 30,
        attackSpeed: 0.5,
        moveSpeed: 2.5,
        attackRange: 10,
        armorType: ArmorType.LIGHT,
        damageType: DamageType.ARMOR_PIERCING,
        unitType: UnitType.GROUND,
        abilities: [
            { type: AbilityType.ARMOR_PIERCE, value: 0.5 },
            { type: AbilityType.ANTI_AIR },
        ],
    },
    {
        id: 'wolf_rider',
        name: 'WolfRider',
        displayName: '狼骑士',
        tier: 2,
        hp: 180,
        atk: 25,
        attackSpeed: 1.0,
        moveSpeed: 6,
        attackRange: 1,
        armorType: ArmorType.MEDIUM,
        damageType: DamageType.PHYSICAL,
        unitType: UnitType.GROUND,
        abilities: [
            { type: AbilityType.STUN, chance: 0.1, duration: 1 },
        ],
    },
    // ── Tier 3 ──────────────────────────────────────────────
    {
        id: 'griffin_rider',
        name: 'GriffinRider',
        displayName: '狮鹫骑士',
        tier: 3,
        hp: 200,
        atk: 35,
        attackSpeed: 1.0,
        moveSpeed: 7,
        attackRange: 2,
        armorType: ArmorType.LIGHT,
        damageType: DamageType.PHYSICAL,
        unitType: UnitType.AIR,
        abilities: [
            { type: AbilityType.FLYING },
            { type: AbilityType.ANTI_AIR },
        ],
    },
    {
        id: 'mage',
        name: 'Mage',
        displayName: '法师',
        tier: 3,
        hp: 100,
        atk: 40,
        attackSpeed: 0.7,
        moveSpeed: 2,
        attackRange: 8,
        armorType: ArmorType.NONE,
        damageType: DamageType.MAGIC,
        unitType: UnitType.GROUND,
        aoeRadius: 3,
        abilities: [
            { type: AbilityType.AOE, value: 3 },
            { type: AbilityType.ANTI_AIR },
        ],
    },
    {
        id: 'siege_tank',
        name: 'SiegeTank',
        displayName: '攻城坦克',
        tier: 3,
        hp: 400,
        atk: 80,
        attackSpeed: 0.3,
        moveSpeed: 1.5,
        attackRange: 6,
        armorType: ArmorType.HEAVY,
        damageType: DamageType.SIEGE,
        unitType: UnitType.GROUND,
        abilities: [
            { type: AbilityType.ANTI_BUILDING, value: 3.0 },
            { type: AbilityType.ANTI_AIR },
        ],
    },
    // ── Special ─────────────────────────────────────────────
    {
        id: 'dragon',
        name: 'Dragon',
        displayName: '龙',
        tier: 4,
        hp: 500,
        atk: 50,
        attackSpeed: 0.8,
        moveSpeed: 6,
        attackRange: 5,
        armorType: ArmorType.HEAVY,
        damageType: DamageType.FLAME,
        unitType: UnitType.AIR,
        aoeRadius: 4,
        abilities: [
            { type: AbilityType.FLYING },
            { type: AbilityType.FIRE_BREATH, value: 4 },
            { type: AbilityType.DAMAGE_REDUCTION, value: 0.3 },
            { type: AbilityType.ANTI_AIR },
        ],
    },
    {
        id: 'paladin',
        name: 'Paladin',
        displayName: '圣骑士',
        tier: 4,
        hp: 300,
        atk: 45,
        attackSpeed: 1.0,
        moveSpeed: 3,
        attackRange: 1,
        armorType: ArmorType.HEAVY,
        damageType: DamageType.DIVINE,
        unitType: UnitType.GROUND,
        abilities: [
            { type: AbilityType.AURA_ATK, value: 0.20 },
        ],
    },
    {
        id: 'assassin',
        name: 'Assassin',
        displayName: '刺客',
        tier: 4,
        hp: 120,
        atk: 60,
        attackSpeed: 1.5,
        moveSpeed: 5,
        attackRange: 1,
        armorType: ArmorType.LIGHT,
        damageType: DamageType.PHYSICAL,
        unitType: UnitType.GROUND,
        abilities: [
            { type: AbilityType.STEALTH },
            { type: AbilityType.BACKSTAB, value: 0.5 },
        ],
    },
];
