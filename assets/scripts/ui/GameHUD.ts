/**
 * GameHUD.ts
 * 游戏 HUD 主控 —— 顶部城堡血条/计时器、底部金币/建筑菜单、英雄面板
 */

import { _decorator, Component, Label, ProgressBar, Node, Button } from 'cc';
import { GameEvent, Faction, WeatherType, GameConstants } from '../core/GameConstants';
import { EventBus } from '../core/EventBus';
import { GameManager } from '../core/GameManager';
import { BuildingMenuPanel } from './BuildingMenuPanel';
import { HeroPanel } from './HeroPanel';
import { WeatherSystem } from '../weather/WeatherSystem';

const { ccclass, property } = _decorator;

@ccclass('GameHUD')
export class GameHUD extends Component {

    // ── 顶部 HUD ──────────────────────────────────────────────
    @property(ProgressBar)
    playerCastleHPBar: ProgressBar = null!;

    @property(ProgressBar)
    aiCastleHPBar: ProgressBar = null!;

    @property(Label)
    playerCastleHPLabel: Label = null!;

    @property(Label)
    aiCastleHPLabel: Label = null!;

    @property(Label)
    timerLabel: Label = null!;

    @property(Label)
    weatherLabel: Label = null!;

    // ── 底部 HUD ──────────────────────────────────────────────
    @property(Label)
    goldLabel: Label = null!;

    @property(Label)
    incomeRateLabel: Label = null!;

    @property(BuildingMenuPanel)
    buildingMenu: BuildingMenuPanel = null!;

    // ── 英雄面板 ──────────────────────────────────────────────
    @property(HeroPanel)
    heroPanel: HeroPanel = null!;

    // ── 按钮 ──────────────────────────────────────────────────
    @property(Button)
    pauseButton: Button = null!;

    @property(Button)
    surrenderButton: Button = null!;

    // ── 内部状态 ──────────────────────────────────────────────
    private _playerGold: number = 0;
    private _incomeRate: number = 0;

    onLoad(): void {
        // 订阅事件
        EventBus.on(GameEvent.GOLD_CHANGED, this._onGoldChanged, this);
        EventBus.on(GameEvent.CASTLE_HP_CHANGED, this._onCastleHPChanged, this);
        EventBus.on(GameEvent.MATCH_TIMER_UPDATED, this._onTimerUpdated, this);
        EventBus.on(GameEvent.WEATHER_CHANGED, this._onWeatherChanged, this);
        EventBus.on(GameEvent.INCOME_RATE_CHANGED, this._onIncomeRateChanged, this);

        // 注册按钮
        this.pauseButton?.node.on(Button.EventType.CLICK, this._onPause, this);
        this.surrenderButton?.node.on(Button.EventType.CLICK, this._onSurrender, this);

        // 初始化显示
        this._refreshCastleHP(Faction.PLAYER, GameConstants.CASTLE_HP, GameConstants.CASTLE_HP);
        this._refreshCastleHP(Faction.AI, GameConstants.CASTLE_HP, GameConstants.CASTLE_HP);
        this._refreshGold();
        this._refreshTimer(GameConstants.MATCH_DURATION);
    }

    onDestroy(): void {
        EventBus.targetOff(this);
    }

    // ── 事件响应 ──────────────────────────────────────────────

    private _onGoldChanged(data: { faction: Faction; gold: number; incomeRate: number }): void {
        if (data.faction !== Faction.PLAYER) return;
        this._playerGold = data.gold;
        this._incomeRate = data.incomeRate;
        this._refreshGold();
    }

    private _onCastleHPChanged(data: { faction: Faction; hp: number; maxHp: number }): void {
        this._refreshCastleHP(data.faction, data.hp, data.maxHp);
    }

    private _onTimerUpdated(data: { elapsed: number; remaining: number }): void {
        this._refreshTimer(data.remaining);
    }

    private _onWeatherChanged(weather: WeatherType): void {
        const names: Record<WeatherType, string> = {
            [WeatherType.CLEAR]: '☀ 晴天',
            [WeatherType.RAIN]: '🌧 雨天',
            [WeatherType.FOG]: '🌫 浓雾',
            [WeatherType.BLIZZARD]: '❄ 暴雪',
        };
        if (this.weatherLabel) {
            this.weatherLabel.string = names[weather] ?? '';
        }
    }

    private _onIncomeRateChanged(data: { rate: number }): void {
        this._incomeRate = data.rate;
        this._refreshGold();
    }

    // ── 刷新方法 ──────────────────────────────────────────────

    private _refreshCastleHP(faction: Faction, hp: number, maxHp: number): void {
        const ratio = maxHp > 0 ? hp / maxHp : 0;

        if (faction === Faction.PLAYER) {
            if (this.playerCastleHPBar) this.playerCastleHPBar.progress = ratio;
            if (this.playerCastleHPLabel) this.playerCastleHPLabel.string = `${Math.ceil(hp)}`;
        } else {
            if (this.aiCastleHPBar) this.aiCastleHPBar.progress = ratio;
            if (this.aiCastleHPLabel) this.aiCastleHPLabel.string = `${Math.ceil(hp)}`;
        }
    }

    private _refreshGold(): void {
        if (this.goldLabel) {
            this.goldLabel.string = `💰 ${Math.floor(this._playerGold)}`;
        }
        if (this.incomeRateLabel) {
            this.incomeRateLabel.string = `+${this._incomeRate}/s`;
        }
    }

    private _refreshTimer(remaining: number): void {
        if (!this.timerLabel) return;
        const minutes = Math.floor(remaining / 60);
        const seconds = Math.floor(remaining % 60);
        this.timerLabel.string = `${minutes}:${seconds.toString().padStart(2, '0')}`;
    }

    // ── 按钮响应 ──────────────────────────────────────────────

    private _onPause(): void {
        GameManager.instance.togglePause();
    }

    private _onSurrender(): void {
        GameManager.instance.playerSurrender();
    }
}
