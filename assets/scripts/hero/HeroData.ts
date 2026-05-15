/**
 * HeroData.ts
 * 英雄数据类型定义与默认英雄配置
 */

import { ArmorType, DamageType, UnitType } from '../core/GameConstants';

export enum HeroSkillType {
    SHIELD_AURA = 'shield_aura',       // 护盾光环（守护将军）
    METEOR = 'meteor',                   // 陨石术（元素法师）
    MARK = 'mark',                       // 标记（暗影猎手）
}

export interface HeroSkillConfig {
    type: HeroSkillType;
    cooldown: number;           // 冷却时间（秒）
    range: number;              // 作用范围
    damage?: number;            // 技能伤害（陨石术）
    duration?: number;          // 效果持续（秒）
    aoeRadius?: number;         // AOE半径
    multiplier?: number;        // 增伤倍数（标记）
    description: string;
}

export interface HeroConfig {
    id: string;
    name: string;
    displayName: string;
    hp: number;
    atk: number;
    attackSpeed: number;
    moveSpeed: number;
    attackRange: number;
    armorType: ArmorType;
    damageType: DamageType;
    unitType: UnitType;
    skill: HeroSkillConfig;
    gloryRequired: number;      // 召唤所需荣耀值
    reviveTime: number;         // 复活冷却（秒）
    description: string;
}

export const DEFAULT_HERO_CONFIGS: HeroConfig[] = [
    {
        id: 'guardian_general',
        name: 'GuardianGeneral',
        displayName: '守护将军',
        hp: 800,
        atk: 60,
        attackSpeed: 0.8,
        moveSpeed: 3,
        attackRange: 1,
        armorType: ArmorType.HEAVY,
        damageType: DamageType.PHYSICAL,
        unitType: UnitType.GROUND,
        gloryRequired: 100,
        reviveTime: 60,
        skill: {
            type: HeroSkillType.SHIELD_AURA,
            cooldown: 30,
            range: 5,
            duration: 5,
            description: '护盾：为附近友军提供5秒免伤护盾',
        },
        description: '近战坦克型英雄，拥有极高的生存能力与友军保护技能',
    },
    {
        id: 'elemental_archmage',
        name: 'ElementalArchmage',
        displayName: '元素法师',
        hp: 400,
        atk: 50,
        attackSpeed: 0.6,
        moveSpeed: 2.5,
        attackRange: 10,
        armorType: ArmorType.NONE,
        damageType: DamageType.MAGIC,
        unitType: UnitType.GROUND,
        gloryRequired: 100,
        reviveTime: 60,
        skill: {
            type: HeroSkillType.METEOR,
            cooldown: 25,
            range: 15,
            damage: 300,
            aoeRadius: 5,
            description: '陨石术：对目标区域造成大范围魔法AOE伤害',
        },
        description: '远程法术型英雄，AOE陨石术可一次清空大量敌军',
    },
    {
        id: 'shadow_hunter',
        name: 'ShadowHunter',
        displayName: '暗影猎手',
        hp: 500,
        atk: 80,
        attackSpeed: 1.5,
        moveSpeed: 5,
        attackRange: 1,
        armorType: ArmorType.LIGHT,
        damageType: DamageType.PHYSICAL,
        unitType: UnitType.GROUND,
        gloryRequired: 100,
        reviveTime: 60,
        skill: {
            type: HeroSkillType.MARK,
            cooldown: 20,
            range: 12,
            duration: 8,
            multiplier: 1.5,
            description: '标记：使目标单位受到的所有伤害提升50%，持续8秒',
        },
        description: '刺杀型英雄，高爆发输出与标记技能可快速击杀敌方高价值目标',
    },
];
