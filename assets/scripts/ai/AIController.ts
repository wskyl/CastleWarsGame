/**
 * AIController.ts
 * AI 对手决策系统 —— 支持 Easy / Normal / Hard 三档难度
 */

import { _decorator, Component } from 'cc';
import { AIDifficulty, Faction, GameConstants } from '../core/GameConstants';
import { BuildingConfig, DEFAULT_BUILDING_CONFIGS } from '../buildings/BuildingData';
import { BuildingManager } from '../buildings/BuildingManager';
import { EconomyManager } from '../economy/EconomyManager';
import { UnitManager } from '../units/UnitManager';
import { UnitController } from '../units/UnitController';
import { GameManager } from '../core/GameManager';

const { ccclass, property } = _decorator;

/** AI 行为优先级 */
interface BuildingPriority {
    config: BuildingConfig;
    score: number;
}

@ccclass('AIController')
export class AIController extends Component {

    @property({ type: BuildingManager })
    buildingManager: BuildingManager = null!;

    @property({ type: EconomyManager })
    economyManager: EconomyManager = null!;

    @property({ type: UnitManager })
    unitManager: UnitManager = null!;

    private _difficulty: AIDifficulty = AIDifficulty.NORMAL;
    private _decisionTimer: number = 0;
    private _decisionInterval: number = 3; // 秒

    // ── 初始化 ────────────────────────────────────────────────
    init(difficulty: AIDifficulty): void {
        this._difficulty = difficulty;
        this._decisionInterval = this._getDecisionInterval();
        this._decisionTimer = this._decisionInterval;
    }

    update(dt: number): void {
        if (!GameManager.instance?.isPlaying) return;

        this._decisionTimer -= dt;
        if (this._decisionTimer <= 0) {
            this._decisionTimer = this._getDecisionInterval();
            this._makeDecision();
        }
    }

    // ── 决策间隔（难度相关） ──────────────────────────────────
    private _getDecisionInterval(): number {
        switch (this._difficulty) {
            case AIDifficulty.EASY:
                return 5 + Math.random() * 3;   // 5~8秒
            case AIDifficulty.NORMAL:
                return 2 + Math.random() * 2;   // 2~4秒
            case AIDifficulty.HARD:
                return 0.5 + Math.random() * 0.5; // 0.5~1秒
        }
    }

    // ── 核心决策逻辑 ──────────────────────────────────────────
    private _makeDecision(): void {
        const currentGold = this.economyManager.getGold(Faction.AI);
        const emptySlot = this.buildingManager.getFirstEmptySlot(Faction.AI);

        if (emptySlot === -1) return; // 没有空槽位

        const candidates = this.buildingManager.getAvailableBuildings(Faction.AI, currentGold);
        if (candidates.length === 0) return;

        let chosen: BuildingConfig | null = null;

        switch (this._difficulty) {
            case AIDifficulty.EASY:
                chosen = this._easyPickBuilding(candidates);
                break;
            case AIDifficulty.NORMAL:
                chosen = this._normalPickBuilding(candidates);
                break;
            case AIDifficulty.HARD:
                chosen = this._hardPickBuilding(candidates);
                break;
        }

        if (chosen) {
            this.buildingManager.aiBuild(chosen.id, emptySlot);
        }
    }

    // ── Easy：随机选择可建造建筑 ─────────────────────────────
    private _easyPickBuilding(candidates: BuildingConfig[]): BuildingConfig {
        return candidates[Math.floor(Math.random() * candidates.length)];
    }

    // ── Normal：优先 Tier1→Tier2 线性升级，有限克制判断 ──────
    private _normalPickBuilding(candidates: BuildingConfig[]): BuildingConfig {
        // 优先低 Tier 建筑
        const sorted = [...candidates].sort((a, b) => a.tier - b.tier);

        // 有限克制判断：如果玩家有大量骑兵，优先枪兵
        const playerUnits = this.unitManager.getAllAlive(Faction.PLAYER);
        const cavalryCount = playerUnits.filter(u => u.config.id === 'cavalry').length;

        if (cavalryCount >= 2) {
            const spearman = sorted.find(c => c.id === 'elite_barracks');
            if (spearman) return spearman;
        }

        return sorted[0];
    }

    // ── Hard：分析玩家兵种并选择克制建筑 ────────────────────
    private _hardPickBuilding(candidates: BuildingConfig[]): BuildingConfig {
        const playerUnits = this.unitManager.getAllAlive(Faction.PLAYER);
        const priorities = this._calcBuildingPriorities(candidates, playerUnits);

        // 按分数排序（降序）
        priorities.sort((a, b) => b.score - a.score);
        return priorities[0].config;
    }

    private _calcBuildingPriorities(
        candidates: BuildingConfig[],
        playerUnits: UnitController[]
    ): BuildingPriority[] {
        // 统计玩家兵种组成
        const unitCounts: Record<string, number> = {};
        playerUnits.forEach(u => {
            unitCounts[u.config.id] = (unitCounts[u.config.id] ?? 0) + 1;
        });

        const totalUnits = playerUnits.length || 1;

        return candidates.map(cfg => {
            let score = 0;

            // 基础分：偏向高Tier（但不超出当前阶段）
            score += cfg.tier * 10;

            // 克制加分
            switch (cfg.id) {
                case 'elite_barracks': // 枪兵 → 克骑兵
                    score += ((unitCounts['cavalry'] ?? 0) / totalUnits) * 50;
                    break;
                case 'gunsmith': // 火枪手 → 穿甲，对重甲步兵有利
                    score += ((unitCounts['infantry'] ?? 0) / totalUnits) * 40;
                    break;
                case 'beast_den': // 狼骑士 → 快速输出
                    score += 15;
                    break;
                case 'griffin_tower': // 狮鹫骑士 → 对抗地面
                    score += 20;
                    break;
                case 'mage_tower': // 法师 → 对抗重甲/密集兵种
                    {
                        const heavyCount = playerUnits.filter(u =>
                            u.config.armorType === 'heavy' as any
                        ).length;
                        score += (heavyCount / totalUnits) * 60;
                    }
                    break;
                case 'siege_workshop': // 攻城坦克 → 直攻城堡
                    score += 25;
                    // 玩家有很多建筑时加分
                    const playerBuildingCount = this.buildingManager.getAllBuildings(Faction.PLAYER).length;
                    score += playerBuildingCount * 10;
                    break;
                case 'dragon_nest': // 龙 → 最强传奇
                    score += 80;
                    break;
                case 'temple': // 圣骑士 → 光环支援
                    score += 35;
                    break;
                case 'shadow_altar': // 刺客 → 对抗脆弱单位
                    {
                        const lightCount = playerUnits.filter(u =>
                            u.config.armorType === 'light' as any
                        ).length;
                        score += (lightCount / totalUnits) * 45;
                    }
                    break;
            }

            // 玩家出飞行单位时，优先远程/法师
            const flyingCount = playerUnits.filter(u => u.isFlying).length;
            if (flyingCount > 0) {
                if (['gunsmith', 'archery_range', 'mage_tower'].includes(cfg.id)) {
                    score += flyingCount * 20;
                }
            }

            return { config: cfg, score };
        });
    }

    // ── 英雄召唤决策 ─────────────────────────────────────────
    shouldSummonHero(glory: number, requiredGlory: number): boolean {
        if (glory < requiredGlory) return false;
        switch (this._difficulty) {
            case AIDifficulty.EASY:
                return Math.random() < 0.3;
            case AIDifficulty.NORMAL:
                return Math.random() < 0.6;
            case AIDifficulty.HARD:
                return true;
        }
    }

    /** 选择英雄（Hard模式选最强）*/
    pickHeroId(): string {
        switch (this._difficulty) {
            case AIDifficulty.EASY:
                return 'guardian_general'; // 简单选最基础英雄
            case AIDifficulty.NORMAL:
                return Math.random() < 0.5 ? 'guardian_general' : 'elemental_archmage';
            case AIDifficulty.HARD:
                // 分析玩家兵种选反制英雄
                const playerUnits = this.unitManager.getAllAlive(Faction.PLAYER);
                const hasHeavy = playerUnits.some(u => u.config.armorType === 'heavy' as any);
                return hasHeavy ? 'elemental_archmage' : 'shadow_hunter';
        }
    }
}
