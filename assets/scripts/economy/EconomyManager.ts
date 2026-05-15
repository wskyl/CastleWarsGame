/**
 * EconomyManager.ts
 * 经济系统 —— 金币管理、收入计算（双方均适用）
 */

import { _decorator, Component } from 'cc';
import { Faction, GameEvent, GameConstants } from '../core/GameConstants';
import { EventBus } from '../core/EventBus';
import { GameManager } from '../core/GameManager';

const { ccclass, property } = _decorator;

interface FactionEconomy {
    gold: number;
    incomeRate: number;    // 当前收入速率（金/秒）
    incomeAccum: number;   // 收入积累计时器
}

@ccclass('EconomyManager')
export class EconomyManager extends Component {

    private static _instance: EconomyManager | null = null;
    static get instance(): EconomyManager { return EconomyManager._instance!; }

    private _player: FactionEconomy = {
        gold: GameConstants.INITIAL_GOLD,
        incomeRate: GameConstants.BASE_INCOME,
        incomeAccum: 0,
    };

    private _ai: FactionEconomy = {
        gold: GameConstants.INITIAL_GOLD,
        incomeRate: GameConstants.BASE_INCOME,
        incomeAccum: 0,
    };

    // 收入成长计时器（每秒 +1 金/秒，上限50）
    private _growthAccum: number = 0;

    onLoad(): void {
        if (EconomyManager._instance && EconomyManager._instance !== this) {
            this.destroy();
            return;
        }
        EconomyManager._instance = this;
    }

    onDestroy(): void {
        if (EconomyManager._instance === this) {
            EconomyManager._instance = null;
        }
    }

    /** 重置（新局开始时调用） */
    reset(): void {
        this._player = {
            gold: GameConstants.INITIAL_GOLD,
            incomeRate: GameConstants.BASE_INCOME,
            incomeAccum: 0,
        };
        this._ai = {
            gold: GameConstants.INITIAL_GOLD,
            incomeRate: GameConstants.BASE_INCOME,
            incomeAccum: 0,
        };
        this._growthAccum = 0;
        this._broadcastAll();
    }

    update(dt: number): void {
        if (!GameManager.instance?.isPlaying) return;

        // 收入成长：每秒 +1 金/秒（双方同步增长）
        this._growthAccum += dt;
        if (this._growthAccum >= 1) {
            this._growthAccum -= 1;
            const newRate = Math.min(
                this._player.incomeRate + GameConstants.INCOME_GROWTH,
                GameConstants.MAX_INCOME
            );
            if (newRate !== this._player.incomeRate) {
                this._player.incomeRate = newRate;
                this._ai.incomeRate = newRate;
                EventBus.emit(GameEvent.INCOME_RATE_CHANGED, { rate: newRate });
            }
        }

        // 按当前收入速率每帧累积金币（非整秒精度）
        this._accumulateGold(this._player, Faction.PLAYER, dt);
        this._accumulateGold(this._ai, Faction.AI, dt);
    }

    private _accumulateGold(economy: FactionEconomy, faction: Faction, dt: number): void {
        economy.incomeAccum += economy.incomeRate * dt;
        if (economy.incomeAccum >= 1) {
            const earned = Math.floor(economy.incomeAccum);
            economy.incomeAccum -= earned;
            this._addGold(faction, earned);
        }
    }

    // ── 金币操作 ──────────────────────────────────────────────

    /** 花费金币（失败返回false） */
    spendGold(faction: Faction, amount: number): boolean {
        const economy = this._getEconomy(faction);
        if (economy.gold < amount) return false;
        economy.gold -= amount;
        this._broadcast(faction);
        return true;
    }

    /** 增加金币（击杀奖励、技能等） */
    addGold(faction: Faction, amount: number): void {
        this._addGold(faction, amount);
    }

    private _addGold(faction: Faction, amount: number): void {
        const economy = this._getEconomy(faction);
        economy.gold += amount;
        this._broadcast(faction);
    }

    // ── 查询 ──────────────────────────────────────────────────

    getGold(faction: Faction): number {
        return this._getEconomy(faction).gold;
    }

    getIncomeRate(faction: Faction): number {
        return this._getEconomy(faction).incomeRate;
    }

    canAfford(faction: Faction, cost: number): boolean {
        return this._getEconomy(faction).gold >= cost;
    }

    // ── 广播 ──────────────────────────────────────────────────

    private _broadcast(faction: Faction): void {
        const economy = this._getEconomy(faction);
        EventBus.emit(GameEvent.GOLD_CHANGED, {
            faction,
            gold: economy.gold,
            incomeRate: economy.incomeRate,
        });
    }

    private _broadcastAll(): void {
        this._broadcast(Faction.PLAYER);
        this._broadcast(Faction.AI);
    }

    private _getEconomy(faction: Faction): FactionEconomy {
        return faction === Faction.PLAYER ? this._player : this._ai;
    }
}
