/**
 * BuildingData.ts
 * 建筑数据类型定义
 */

import { BuildingTier, DamageType, ArmorType, UnitType } from '../core/GameConstants';

/** 建筑配置（来自 JSON） */
export interface BuildingConfig {
    id: string;
    name: string;
    displayName: string;
    tier: BuildingTier;
    cost: number;
    hp: number;
    produceUnitId: string;         // 生产的兵种ID
    produceInterval: number;       // 生产间隔（秒）
    prerequisites: string[];       // 前置建筑ID列表
    prerequisiteLogic?: 'AND' | 'OR' | 'COUNT'; // 前置条件逻辑
    prerequisiteCount?: number;    // 配合COUNT使用：至少满足几个
    description?: string;
}

/** 建筑运行时状态 */
export interface BuildingRuntimeData {
    config: BuildingConfig;
    currentHP: number;
    produceTimer: number;          // 生产计时器（倒计时）
    isAlive: boolean;
    slotIndex: number;
}

/** 所有建筑配置 */
export const DEFAULT_BUILDING_CONFIGS: BuildingConfig[] = [
    // ── Tier 1 ──────────────────────────────────────────────
    {
        id: 'infantry_barracks',
        name: 'InfantryBarracks',
        displayName: '步兵营',
        tier: BuildingTier.TIER1,
        cost: 100,
        hp: 200,
        produceUnitId: 'infantry',
        produceInterval: 8,
        prerequisites: [],
        description: '生产步兵，费用低廉的基础兵营',
    },
    {
        id: 'archery_range',
        name: 'ArcheryRange',
        displayName: '射箭场',
        tier: BuildingTier.TIER1,
        cost: 120,
        hp: 200,
        produceUnitId: 'archer',
        produceInterval: 10,
        prerequisites: [],
        description: '生产弓箭手，提供远程火力支援',
    },
    {
        id: 'stable',
        name: 'Stable',
        displayName: '马厩',
        tier: BuildingTier.TIER1,
        cost: 150,
        hp: 200,
        produceUnitId: 'cavalry',
        produceInterval: 12,
        prerequisites: [],
        description: '生产骑兵，移动迅速的突击单位',
    },
    // ── Tier 2 ──────────────────────────────────────────────
    {
        id: 'elite_barracks',
        name: 'EliteBarracks',
        displayName: '精英兵营',
        tier: BuildingTier.TIER2,
        cost: 300,
        hp: 350,
        produceUnitId: 'spearman',
        produceInterval: 10,
        prerequisites: ['infantry_barracks'],
        prerequisiteLogic: 'AND',
        description: '生产枪兵，骑兵克星',
    },
    {
        id: 'gunsmith',
        name: 'Gunsmith',
        displayName: '枪械坊',
        tier: BuildingTier.TIER2,
        cost: 350,
        hp: 350,
        produceUnitId: 'musketeer',
        produceInterval: 14,
        prerequisites: ['archery_range'],
        prerequisiteLogic: 'AND',
        description: '生产火枪手，穿甲远程单位',
    },
    {
        id: 'beast_den',
        name: 'BeastDen',
        displayName: '兽巢',
        tier: BuildingTier.TIER2,
        cost: 400,
        hp: 350,
        produceUnitId: 'wolf_rider',
        produceInterval: 12,
        prerequisites: ['stable'],
        prerequisiteLogic: 'AND',
        description: '生产狼骑士，高速突击并有几率眩晕敌人',
    },
    // ── Tier 3 ──────────────────────────────────────────────
    {
        id: 'griffin_tower',
        name: 'GriffinTower',
        displayName: '狮鹫塔',
        tier: BuildingTier.TIER3,
        cost: 600,
        hp: 500,
        produceUnitId: 'griffin_rider',
        produceInterval: 16,
        prerequisites: ['stable', 'archery_range'],
        prerequisiteLogic: 'AND',
        description: '生产狮鹫骑士，飞行单位无视地面阻碍',
    },
    {
        id: 'mage_tower',
        name: 'MageTower',
        displayName: '法师塔',
        tier: BuildingTier.TIER3,
        cost: 700,
        hp: 500,
        produceUnitId: 'mage',
        produceInterval: 14,
        prerequisites: ['elite_barracks', 'gunsmith', 'beast_den'],
        prerequisiteLogic: 'COUNT',
        prerequisiteCount: 2,
        description: '生产法师，AOE魔法攻击，穿透重甲',
    },
    {
        id: 'siege_workshop',
        name: 'SiegeWorkshop',
        displayName: '攻城工坊',
        tier: BuildingTier.TIER3,
        cost: 800,
        hp: 500,
        produceUnitId: 'siege_tank',
        produceInterval: 20,
        prerequisites: ['elite_barracks'],
        prerequisiteLogic: 'AND',
        description: '生产攻城坦克，对建筑造成3倍伤害',
    },
    // ── Special ─────────────────────────────────────────────
    {
        id: 'dragon_nest',
        name: 'DragonNest',
        displayName: '龙巢',
        tier: BuildingTier.SPECIAL,
        cost: 2000,
        hp: 800,
        produceUnitId: 'dragon',
        produceInterval: 30,
        prerequisites: ['griffin_tower', 'mage_tower'],
        prerequisiteLogic: 'AND',
        description: '生产龙，传奇飞行单位，AOE火焰吐息',
    },
    {
        id: 'temple',
        name: 'Temple',
        displayName: '圣殿',
        tier: BuildingTier.SPECIAL,
        cost: 1500,
        hp: 800,
        produceUnitId: 'paladin',
        produceInterval: 20,
        prerequisites: ['elite_barracks', 'gunsmith', 'beast_den'],
        prerequisiteLogic: 'COUNT',
        prerequisiteCount: 3,
        description: '生产圣骑士，友军光环加成',
    },
    {
        id: 'shadow_altar',
        name: 'ShadowAltar',
        displayName: '暗影祭坛',
        tier: BuildingTier.SPECIAL,
        cost: 1200,
        hp: 800,
        produceUnitId: 'assassin',
        produceInterval: 16,
        prerequisites: ['beast_den', 'archery_range'],
        prerequisiteLogic: 'AND',
        description: '生产刺客，潜行能力与背刺暴击',
    },
];
