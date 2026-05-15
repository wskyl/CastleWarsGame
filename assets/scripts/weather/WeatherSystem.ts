/**
 * WeatherSystem.ts
 * 天气系统 —— 随机触发天气变化，影响全场单位属性
 */

import { _decorator, Component } from 'cc';
import { WeatherType, GameConstants } from '../core/GameConstants';
import { GameManager } from '../core/GameManager';

const { ccclass, property } = _decorator;

/** 天气效果描述 */
export interface WeatherEffect {
    type: WeatherType;
    displayName: string;
    description: string;
    moveSpeedMultiplier: number;
    attackRangeMultiplier: number;
    flameDamageMultiplier: number;   // 火焰伤害倍率（雨天减少）
    flyingSpeedMultiplier: number;   // 飞行单位额外移速倍率
    stealthDetectionMultiplier: number; // 刺客潜行探测范围倍率
}

export const WEATHER_EFFECTS: Record<WeatherType, WeatherEffect> = {
    [WeatherType.CLEAR]: {
        type: WeatherType.CLEAR,
        displayName: '晴天',
        description: '阳光明媚，无任何加成/减益',
        moveSpeedMultiplier: 1.0,
        attackRangeMultiplier: 1.0,
        flameDamageMultiplier: 1.0,
        flyingSpeedMultiplier: 1.0,
        stealthDetectionMultiplier: 1.0,
    },
    [WeatherType.RAIN]: {
        type: WeatherType.RAIN,
        displayName: '雨天',
        description: '大雨磅礴：移动速度-20%，火焰伤害-30%',
        moveSpeedMultiplier: 0.8,
        attackRangeMultiplier: 1.0,
        flameDamageMultiplier: 0.7,
        flyingSpeedMultiplier: 1.0,
        stealthDetectionMultiplier: 1.0,
    },
    [WeatherType.FOG]: {
        type: WeatherType.FOG,
        displayName: '浓雾',
        description: '能见度极低：所有单位攻击范围-40%，刺客潜行探测范围再减半',
        moveSpeedMultiplier: 1.0,
        attackRangeMultiplier: 0.6,
        flameDamageMultiplier: 1.0,
        flyingSpeedMultiplier: 1.0,
        stealthDetectionMultiplier: 0.5,
    },
    [WeatherType.BLIZZARD]: {
        type: WeatherType.BLIZZARD,
        displayName: '暴雪',
        description: '极寒风暴：移动速度-30%，飞行单位速度额外-20%',
        moveSpeedMultiplier: 0.7,
        attackRangeMultiplier: 1.0,
        flameDamageMultiplier: 1.0,
        flyingSpeedMultiplier: 0.8,
        stealthDetectionMultiplier: 1.0,
    },
};

@ccclass('WeatherSystem')
export class WeatherSystem extends Component {

    private _currentWeather: WeatherType = WeatherType.CLEAR;
    private _checkTimer: number = 0;
    private _weatherDuration: number = 0;
    private _weatherTimer: number = 0;

    // 天气持续时间范围（秒）
    private readonly MIN_DURATION = 30;
    private readonly MAX_DURATION = 60;

    onLoad(): void {
        this._currentWeather = WeatherType.CLEAR;
        this._checkTimer = GameConstants.WEATHER_CHECK_INTERVAL;
    }

    update(dt: number): void {
        if (!GameManager.instance?.isPlaying) return;

        // 天气持续计时
        if (this._currentWeather !== WeatherType.CLEAR) {
            this._weatherTimer += dt;
            if (this._weatherTimer >= this._weatherDuration) {
                this._setWeather(WeatherType.CLEAR);
            }
        }

        // 周期性天气检查
        this._checkTimer -= dt;
        if (this._checkTimer <= 0) {
            this._checkTimer = GameConstants.WEATHER_CHECK_INTERVAL;
            this._tryTriggerWeather();
        }
    }

    // ── 天气触发 ──────────────────────────────────────────────
    private _tryTriggerWeather(): void {
        if (Math.random() > GameConstants.WEATHER_TRIGGER_CHANCE) return;
        if (this._currentWeather !== WeatherType.CLEAR) return; // 已有天气时不叠加

        const options = [WeatherType.RAIN, WeatherType.FOG, WeatherType.BLIZZARD];
        const weather = options[Math.floor(Math.random() * options.length)];
        this._setWeather(weather);
    }

    private _setWeather(weather: WeatherType): void {
        if (this._currentWeather === weather) return;

        this._currentWeather = weather;
        this._weatherTimer = 0;

        if (weather !== WeatherType.CLEAR) {
            this._weatherDuration = this.MIN_DURATION +
                Math.random() * (this.MAX_DURATION - this.MIN_DURATION);
        }

        // 通知 GameManager（内部统一广播 WEATHER_CHANGED，避免重复触发）
        GameManager.instance.setWeather(weather);
    }

    // ── 强制设置（调试/测试用） ────────────────────────────────
    forceWeather(weather: WeatherType): void {
        this._setWeather(weather);
    }

    // ── Getter ────────────────────────────────────────────────
    get currentWeather(): WeatherType { return this._currentWeather; }
    get currentEffect(): WeatherEffect { return WEATHER_EFFECTS[this._currentWeather]; }
    get weatherProgress(): number {
        if (this._weatherDuration <= 0) return 0;
        return Math.min(1, this._weatherTimer / this._weatherDuration);
    }
    get remainingDuration(): number {
        return Math.max(0, this._weatherDuration - this._weatherTimer);
    }
}
