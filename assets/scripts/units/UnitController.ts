/**
 * UnitController.ts
 * 单个兵种单位的运行时 AI 控制器（行军 + 战斗状态机）
 */

import { _decorator, Component, Vec3, Node } from 'cc';
import { UnitConfig, AbilityType } from './UnitData';
import {
    Faction, UnitState, UnitType, ArmorType, DamageType,
    GameConstants, GameEvent,
} from '../core/GameConstants';
import { StateMachine } from '../core/StateMachine';
import { EventBus } from '../core/EventBus';
import { GameManager } from '../core/GameManager';
import { WeatherType } from '../core/GameConstants';

const { ccclass, property } = _decorator;

@ccclass('UnitController')
export class UnitController extends Component {

    private _config!: UnitConfig;
    private _faction!: Faction;
    private _currentHP: number = 0;
    private _isAlive: boolean = false;
    private _fsm!: StateMachine<UnitState>;

    // ── 战斗状态 ──────────────────────────────────────────────
    private _attackTimer: number = 0;
    private _stunTimer: number = 0;
    private _target: UnitController | null = null;
    private _isStealthed: boolean = false;
    private _isMarked: boolean = false;
    private _markMultiplier: number = 1.0;
    private _shieldActive: boolean = false;

    // ── 回调引用（由 UnitManager 提供） ─────────────────────────
    private _findNearestEnemy!: (self: UnitController) => UnitController | null;
    private _onDied!: (unit: UnitController) => void;
    private _onDamageCastle!: (faction: Faction, dmg: number) => void;
    private _onRequestAoeHit!: (center: Vec3, radius: number, dmg: number, faction: Faction, dtype: DamageType) => void;

    // ── 动态属性（受天气影响） ────────────────────────────────
    private _effectiveMoveSpeed: number = 0;
    private _effectiveRange: number = 0;
    private _effectiveFlameDamageMult: number = 1.0; // 火焰伤害倍率（雨天减少）

    /** 初始化 */
    init(
        config: UnitConfig,
        faction: Faction,
        callbacks: {
            findNearestEnemy: (self: UnitController) => UnitController | null;
            onDied: (unit: UnitController) => void;
            onDamageCastle: (faction: Faction, dmg: number) => void;
            onRequestAoeHit: (center: Vec3, radius: number, dmg: number, faction: Faction, dtype: DamageType) => void;
        }
    ): void {
        this._config = config;
        this._faction = faction;
        this._currentHP = config.hp;
        this._isAlive = true;
        this._findNearestEnemy = callbacks.findNearestEnemy;
        this._onDied = callbacks.onDied;
        this._onDamageCastle = callbacks.onDamageCastle;
        this._onRequestAoeHit = callbacks.onRequestAoeHit;

        // 初始潜行
        this._isStealthed = config.abilities.some(a => a.type === AbilityType.STEALTH);

        this._applyWeatherEffects();
        this._setupFSM();

        // 监听天气变化
        EventBus.on(GameEvent.WEATHER_CHANGED, this._applyWeatherEffects, this);
    }

    onDestroy(): void {
        EventBus.targetOff(this);
    }

    update(dt: number): void {
        if (!this._isAlive || !GameManager.instance?.isPlaying) return;
        if (this._stunTimer > 0) {
            this._stunTimer -= dt;
            return;
        }
        this._fsm?.update(dt);
    }

    // ── 状态机 ────────────────────────────────────────────────
    private _setupFSM(): void {
        this._fsm = new StateMachine<UnitState>();
        this._fsm
            .addState({
                name: UnitState.MARCHING,
                onEnter: () => { this._target = null; },
                onUpdate: (dt) => this._updateMarching(dt),
            })
            .addState({
                name: UnitState.ATTACKING,
                onEnter: () => { this._attackTimer = 0; },
                onUpdate: (dt) => this._updateAttacking(dt),
            })
            .addState({
                name: UnitState.STUNNED,
            })
            .addState({
                name: UnitState.DEAD,
            })
            .addState({
                name: UnitState.STEALTHED,
                onUpdate: (dt) => this._updateMarching(dt),
            });

        const startState = this._isStealthed ? UnitState.STEALTHED : UnitState.MARCHING;
        this._fsm.transition(startState);
    }

    // ── 行军更新 ──────────────────────────────────────────────
    private _updateMarching(dt: number): void {
        const enemy = this._findNearestEnemy(this);
        if (enemy && this._inRange(enemy)) {
            this._target = enemy;
            this._isStealthed = false;
            this._fsm.transition(UnitState.ATTACKING);
            return;
        }

        // 向敌方主堡方向移动
        const dir = this._faction === Faction.PLAYER ? 1 : -1;
        const pos = this.node.position.clone();
        pos.x += dir * this._effectiveMoveSpeed * dt;
        this.node.setPosition(pos);

        // 到达敌方主堡
        const castleX = this._faction === Faction.PLAYER
            ? GameConstants.AI_CASTLE_X
            : GameConstants.PLAYER_CASTLE_X;

        if (this._faction === Faction.PLAYER && pos.x >= castleX) {
            this._attackCastle();
        } else if (this._faction === Faction.AI && pos.x <= castleX) {
            this._attackCastle();
        }
    }

    // ── 战斗更新 ──────────────────────────────────────────────
    private _updateAttacking(dt: number): void {
        // 目标失效
        if (!this._target || !this._target.isAlive) {
            this._target = null;
            this._fsm.transition(UnitState.MARCHING);
            return;
        }

        // 目标超出范围
        if (!this._inRange(this._target)) {
            this._fsm.transition(UnitState.MARCHING);
            return;
        }

        // 攻击冷却
        this._attackTimer += dt;
        const attackInterval = 1 / this._config.attackSpeed;
        if (this._attackTimer >= attackInterval) {
            this._attackTimer = 0;
            this._performAttack(this._target);
        }
    }

    // ── 攻击逻辑 ──────────────────────────────────────────────
    private _performAttack(target: UnitController): void {
        const isAoe = this._config.aoeRadius && this._config.aoeRadius > 0;
        const rawDmg = this._calcDamage(this._config.atk, this._config.damageType);

        if (isAoe) {
            this._onRequestAoeHit(
                target.node.worldPosition.clone(),
                this._config.aoeRadius!,
                rawDmg,
                this._faction,
                this._config.damageType
            );
        } else {
            target.takeDamage(rawDmg, this._config.damageType, this);
        }

        // 眩晕触发
        const stunAbility = this._config.abilities.find(a => a.type === AbilityType.STUN);
        if (stunAbility && Math.random() < (stunAbility.chance ?? 0)) {
            target.applyStun(stunAbility.duration ?? 1);
        }
    }

    private _calcDamage(baseDmg: number, dmgType: DamageType): number {
        // 火焰伤害受天气影响（雨天 -30%）
        if (dmgType === DamageType.FLAME) {
            return baseDmg * this._effectiveFlameDamageMult;
        }
        return baseDmg;
    }

    // ── 受到伤害 ──────────────────────────────────────────────
    takeDamage(damage: number, dmgType: DamageType, attacker?: UnitController): void {
        if (!this._isAlive) return;
        if (this._shieldActive) return; // 护盾免伤

        let finalDmg = damage;

        // 护甲减免
        finalDmg = this._applyArmorReduction(finalDmg, dmgType);

        // 克制关系修正（攻击方能力）
        if (attacker) {
            finalDmg = this._applyAttackerAbility(finalDmg, attacker);
        }

        // 标记增伤
        if (this._isMarked) {
            finalDmg *= this._markMultiplier;
        }

        // 自身伤害减免能力
        const dmgRedAbility = this._config.abilities.find(a => a.type === AbilityType.DAMAGE_REDUCTION);
        if (dmgRedAbility) {
            finalDmg *= (1 - (dmgRedAbility.value ?? 0));
        }

        this._currentHP = Math.max(0, this._currentHP - finalDmg);

        if (this._currentHP <= 0) {
            this._die();
        }
    }

    private _applyArmorReduction(damage: number, dmgType: DamageType): number {
        const armor = this._config.armorType;

        // 穿甲伤害特殊处理
        if (dmgType === DamageType.ARMOR_PIERCING) {
            // 火枪手忽略50%护甲
            const pierce = 0.5;
            return damage * (1 - pierce * this._getArmorFactor(armor, DamageType.PHYSICAL));
        }

        const reduction = this._getArmorFactor(armor, dmgType);
        return damage * (1 - reduction);
    }

    private _getArmorFactor(armor: ArmorType, dmgType: DamageType): number {
        const G = GameConstants;
        const isMagic = (dmgType === DamageType.MAGIC || dmgType === DamageType.FLAME);

        switch (armor) {
            case ArmorType.HEAVY:
                return isMagic ? G.ARMOR_HEAVY_MAGIC : G.ARMOR_HEAVY_PHYSICAL;
            case ArmorType.MEDIUM:
                return isMagic ? G.ARMOR_MEDIUM_MAGIC : G.ARMOR_MEDIUM_PHYSICAL;
            case ArmorType.LIGHT:
                return isMagic ? G.ARMOR_LIGHT_MAGIC : G.ARMOR_LIGHT_PHYSICAL;
            default:
                return 0;
        }
    }

    private _applyAttackerAbility(damage: number, attacker: UnitController): number {
        for (const ability of attacker._config.abilities) {
            if (ability.type === AbilityType.ANTI_CAVALRY &&
                this._config.id === 'cavalry') {
                damage *= (ability.value ?? 2.0);
            }
            if (ability.type === AbilityType.ANTI_BUILDING &&
                this._config.unitType === UnitType.BUILDING) {
                damage *= (ability.value ?? 1.0);
            }
            if (ability.type === AbilityType.BACKSTAB && !this._isStealthed) {
                damage *= (1 + (ability.value ?? 0.5));
            }
        }
        return damage;
    }

    // ── 眩晕 ──────────────────────────────────────────────────
    applyStun(duration: number): void {
        this._stunTimer = Math.max(this._stunTimer, duration);
        EventBus.emit(GameEvent.UNIT_STUN, { unit: this, duration });
    }

    // ── 标记（英雄暗影猎手技能） ──────────────────────────────
    applyMark(multiplier: number, duration: number): void {
        this._isMarked = true;
        this._markMultiplier = multiplier;
        this.scheduleOnce(() => {
            this._isMarked = false;
            this._markMultiplier = 1.0;
        }, duration);
    }

    // ── 护盾（英雄守护将军技能） ─────────────────────────────
    applyShield(duration: number): void {
        this._shieldActive = true;
        this.scheduleOnce(() => { this._shieldActive = false; }, duration);
        EventBus.emit(GameEvent.SHIELD_APPLIED, { unit: this });
    }

    // ── 光环加成（圣骑士） ───────────────────────────────────
    applyAtkAura(multiplier: number): void {
        // 光环由UnitManager在每帧统一处理
    }

    // ── 攻击城堡 ──────────────────────────────────────────────
    private _attackCastle(): void {
        const targetFaction = this._faction === Faction.PLAYER ? Faction.AI : Faction.PLAYER;
        // 攻城坦克对建筑3倍
        let dmg = this._config.atk;
        const abuildAbility = this._config.abilities.find(a => a.type === AbilityType.ANTI_BUILDING);
        if (abuildAbility) dmg *= (abuildAbility.value ?? 1.0);

        this._onDamageCastle(targetFaction, dmg);
        this._die(); // 到达城堡后消亡
    }

    // ── 死亡 ──────────────────────────────────────────────────
    private _die(): void {
        if (!this._isAlive) return;
        this._isAlive = false;
        this._fsm.transition(UnitState.DEAD);
        EventBus.emit(GameEvent.UNIT_DIED, { unit: this, faction: this._faction });
        this._onDied(this);
    }

    // ── 天气效果 ──────────────────────────────────────────────
    private _applyWeatherEffects(weather?: WeatherType): void {
        const w = weather ?? GameManager.instance?.weather ?? WeatherType.CLEAR;
        let speedMult = 1.0;
        let rangeMult = 1.0;
        let flameMult = 1.0;

        switch (w) {
            case WeatherType.RAIN:
                speedMult = 0.8;
                flameMult = 0.7; // 雨天火焰伤害 -30%
                break;
            case WeatherType.FOG:
                rangeMult = 0.6;
                break;
            case WeatherType.BLIZZARD:
                speedMult = 0.7;
                if (this._config.unitType === UnitType.AIR) {
                    speedMult *= 0.8;
                }
                break;
        }

        this._effectiveMoveSpeed = this._config.moveSpeed * speedMult;
        this._effectiveRange = this._config.attackRange * rangeMult;
        this._effectiveFlameDamageMult = flameMult;

        // 刺客在浓雾中潜行效果增强（由 UnitManager 额外处理）
    }

    // ── 工具 ──────────────────────────────────────────────────
    private _inRange(target: UnitController): boolean {
        // 飞行单位只能被有 ANTI_AIR 能力的单位攻击
        if (target.config.unitType === UnitType.AIR) {
            const hasAntiAir = this._config.abilities.some(a => a.type === AbilityType.ANTI_AIR);
            if (!hasAntiAir) return false;
        }

        const dist = Vec3.distance(this.node.worldPosition, target.node.worldPosition);
        return dist <= this._effectiveRange;
    }

    // ── Getter ────────────────────────────────────────────────
    get config(): UnitConfig { return this._config; }
    get currentHP(): number { return this._currentHP; }
    get maxHP(): number { return this._config.hp; }
    get isAlive(): boolean { return this._isAlive; }
    get faction(): Faction { return this._faction; }
    get isStealthed(): boolean { return this._isStealthed; }
    get isFlying(): boolean { return this._config.unitType === UnitType.AIR; }
    get state(): UnitState { return this._fsm?.current ?? UnitState.IDLE; }
}
