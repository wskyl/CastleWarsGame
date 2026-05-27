/**
 * GameManager.ts
 * 游戏核心管理器 —— 全局单例，协调所有子系统
 */

import { _decorator, Component, director } from 'cc';
import { GameState, GameEvent, Faction, AIDifficulty, WeatherType } from './GameConstants';
import { EventBus } from './EventBus';
import { StateMachine } from './StateMachine';

const { ccclass, property } = _decorator;

export interface GameResult {
    winner: Faction | 'draw';
    reason: 'castle_destroyed' | 'timeout' | 'surrender';
    playerCastleHP: number;
    aiCastleHP: number;
    duration: number;
    playerUnitKills: number;
    aiUnitKills: number;
}

@ccclass('GameManager')
export class GameManager extends Component {
    private static _instance: GameManager | null = null;

    /** 单例访问 */
    static get instance(): GameManager {
        return GameManager._instance!;
    }

    // ── 状态 ──────────────────────────────────────────────
    private _fsm: StateMachine<GameState> = new StateMachine<GameState>();
    private _elapsedTime: number = 0;
    private _matchDuration: number = 600;
    private _paused: boolean = false;

    // ── 城堡HP ─────────────────────────────────────────────
    private _playerCastleHP: number = 5000;
    private _aiCastleHP: number = 5000;
    private readonly _maxCastleHP: number = 5000;

    // ── 统计 ────────────────────────────────────────────────
    private _playerUnitKills: number = 0;
    private _aiUnitKills: number = 0;

    // ── 配置 ────────────────────────────────────────────────
    private _difficulty: AIDifficulty = AIDifficulty.NORMAL;

    // ── 当前天气 ─────────────────────────────────────────────
    private _weather: WeatherType = WeatherType.CLEAR;

    // ── 对局结果 ─────────────────────────────────────────────
    private _result: GameResult | null = null;

    // ────────────────────────────────────────────────────────
    onLoad() {
        if (GameManager._instance && GameManager._instance !== this) {
            this.destroy();
            return;
        }
        GameManager._instance = this;
        director.addPersistRootNode(this.node);
        this._setupFSM();
    }

    onDestroy() {
        if (GameManager._instance === this) {
            GameManager._instance = null;
        }
        EventBus.targetOff(this);
    }

    // ── FSM 初始化 ────────────────────────────────────────────
    private _setupFSM(): void {
        this._fsm
            .addState({
                name: GameState.MAIN_MENU,
                onEnter: () => this._onEnterMainMenu(),
            })
            .addState({
                name: GameState.LOADING,
                onEnter: () => this._onEnterLoading(),
            })
            .addState({
                name: GameState.PLAYING,
                onEnter: (prev) => this._onEnterPlaying(prev),
                onUpdate: (dt) => this._onUpdatePlaying(dt),
            })
            .addState({
                name: GameState.PAUSED,
                onEnter: () => this._onEnterPaused(),
                onExit: () => this._onExitPaused(),
            })
            .addState({
                name: GameState.GAME_OVER,
                onEnter: () => this._onEnterGameOver(),
            });

        this._fsm.transition(GameState.MAIN_MENU);
    }

    // ── 生命周期驱动 ──────────────────────────────────────────
    update(dt: number): void {
        if (this._paused) return;
        this._fsm.update(dt);
    }

    // ── 状态机回调 ────────────────────────────────────────────
    private _onEnterMainMenu(): void {
        this._reset();
        EventBus.emit(GameEvent.GAME_STATE_CHANGED, GameState.MAIN_MENU);
    }

    private _onEnterLoading(): void {
        EventBus.emit(GameEvent.GAME_STATE_CHANGED, GameState.LOADING);
    }

    private _onEnterPlaying(prev?: GameState): void {
        if (prev !== GameState.PAUSED) {
            this._elapsedTime = 0;
        }
        this._paused = false;
        EventBus.emit(GameEvent.GAME_STATE_CHANGED, GameState.PLAYING);
    }

    private _onUpdatePlaying(dt: number): void {
        this._elapsedTime += dt;

        // 广播计时器
        EventBus.emit(GameEvent.MATCH_TIMER_UPDATED, {
            elapsed: this._elapsedTime,
            remaining: Math.max(0, this._matchDuration - this._elapsedTime),
        });

        // 超时判断
        if (this._elapsedTime >= this._matchDuration) {
            this._endByTimeout();
        }
    }

    private _onEnterPaused(): void {
        this._paused = true;
        EventBus.emit(GameEvent.GAME_STATE_CHANGED, GameState.PAUSED);
    }

    private _onExitPaused(): void {
        this._paused = false;
    }

    private _onEnterGameOver(): void {
        EventBus.emit(GameEvent.GAME_STATE_CHANGED, GameState.GAME_OVER);
        EventBus.emit(GameEvent.GAME_OVER, this._result);
    }

    // ── 对外接口 ──────────────────────────────────────────────

    /** 开始游戏 */
    startGame(difficulty: AIDifficulty = AIDifficulty.NORMAL): void {
        this._difficulty = difficulty;
        this._fsm.transition(GameState.LOADING);
        // 加载完成后由 LoadingManager 调用 onLoadingComplete
    }

    /** 加载完成，进入游戏 */
    onLoadingComplete(): void {
        this._reset();
        this._fsm.transition(GameState.PLAYING);
    }

    /** 暂停/恢复 */
    togglePause(): void {
        if (this._fsm.is(GameState.PLAYING)) {
            this._fsm.transition(GameState.PAUSED);
        } else if (this._fsm.is(GameState.PAUSED)) {
            this._fsm.transition(GameState.PLAYING);
        }
    }

    /** 玩家投降 */
    playerSurrender(): void {
        this._endGame({
            winner: Faction.AI,
            reason: 'surrender',
            playerCastleHP: this._playerCastleHP,
            aiCastleHP: this._aiCastleHP,
            duration: this._elapsedTime,
            playerUnitKills: this._playerUnitKills,
            aiUnitKills: this._aiUnitKills,
        });
    }

    /** 返回主菜单 */
    returnToMainMenu(): void {
        this._fsm.transition(GameState.MAIN_MENU);
    }

    // ── 城堡伤害 ──────────────────────────────────────────────

    /** 对城堡造成伤害 */
    damageCastle(faction: Faction, damage: number): void {
        if (!this._fsm.is(GameState.PLAYING)) return;

        if (faction === Faction.PLAYER) {
            this._playerCastleHP = Math.max(0, this._playerCastleHP - damage);
            EventBus.emit(GameEvent.CASTLE_HP_CHANGED, {
                faction: Faction.PLAYER,
                hp: this._playerCastleHP,
                maxHp: this._maxCastleHP,
            });
            if (this._playerCastleHP <= 0) {
                this._endByCastleDestroyed(Faction.AI);
            }
        } else {
            this._aiCastleHP = Math.max(0, this._aiCastleHP - damage);
            EventBus.emit(GameEvent.CASTLE_HP_CHANGED, {
                faction: Faction.AI,
                hp: this._aiCastleHP,
                maxHp: this._maxCastleHP,
            });
            if (this._aiCastleHP <= 0) {
                this._endByCastleDestroyed(Faction.PLAYER);
            }
        }
    }

    // ── 统计更新 ──────────────────────────────────────────────

    recordKill(killer: Faction): void {
        if (killer === Faction.PLAYER) {
            this._playerUnitKills++;
        } else {
            this._aiUnitKills++;
        }
    }

    // ── 天气 ──────────────────────────────────────────────────

    setWeather(weather: WeatherType): void {
        this._weather = weather;
        EventBus.emit(GameEvent.WEATHER_CHANGED, weather);
    }

    // ── 结束逻辑 ──────────────────────────────────────────────

    private _endByCastleDestroyed(winner: Faction): void {
        this._endGame({
            winner,
            reason: 'castle_destroyed',
            playerCastleHP: this._playerCastleHP,
            aiCastleHP: this._aiCastleHP,
            duration: this._elapsedTime,
            playerUnitKills: this._playerUnitKills,
            aiUnitKills: this._aiUnitKills,
        });
    }

    private _endByTimeout(): void {
        let winner: Faction | 'draw';
        if (this._playerCastleHP > this._aiCastleHP) {
            winner = Faction.PLAYER;
        } else if (this._aiCastleHP > this._playerCastleHP) {
            winner = Faction.AI;
        } else {
            winner = 'draw';
        }

        this._endGame({
            winner,
            reason: 'timeout',
            playerCastleHP: this._playerCastleHP,
            aiCastleHP: this._aiCastleHP,
            duration: this._elapsedTime,
            playerUnitKills: this._playerUnitKills,
            aiUnitKills: this._aiUnitKills,
        });
    }

    private _endGame(result: GameResult): void {
        this._result = result;
        this._fsm.transition(GameState.GAME_OVER);
    }

    private _reset(): void {
        this._elapsedTime = 0;
        this._playerCastleHP = this._maxCastleHP;
        this._aiCastleHP = this._maxCastleHP;
        this._playerUnitKills = 0;
        this._aiUnitKills = 0;
        this._result = null;
        this._weather = WeatherType.CLEAR;
    }

    // ── Getter ─────────────────────────────────────────────────
    get gameState(): GameState { return this._fsm.current ?? GameState.NONE; }
    get elapsedTime(): number { return this._elapsedTime; }
    get remainingTime(): number { return Math.max(0, this._matchDuration - this._elapsedTime); }
    get playerCastleHP(): number { return this._playerCastleHP; }
    get aiCastleHP(): number { return this._aiCastleHP; }
    get maxCastleHP(): number { return this._maxCastleHP; }
    get difficulty(): AIDifficulty { return this._difficulty; }
    get weather(): WeatherType { return this._weather; }
    get result(): GameResult | null { return this._result; }
    get isPlaying(): boolean { return this._fsm.is(GameState.PLAYING); }
}
