/**
 * HeroController.ts
 * 英雄单位控制器 —— 继承普通单位特性，附加主动技能与复活机制
 */

import { _decorator, Component, Vec3, Node } from 'cc';
import { HeroConfig, HeroSkillType } from './HeroData';
import { Faction, GameEvent, DamageType, ArmorType, UnitState } from '../core/GameConstants';
import { StateMachine } from '../core/StateMachine';
import { EventBus } from '../core/EventBus';
import { GameManager } from '../core/GameManager';
import { UnitController } from '../units/UnitController';
import { UnitManager } from '../units/UnitManager';

const { ccclass, property } = _decorator;

@ccclass('HeroController')
export class HeroController extends Component {

    private _config!: HeroConfig;
    private _faction!: Faction;
    private _currentHP: number = 0;
    private _isAlive: boolean = false;
    private _isOnField: boolean = false;

    // ── 技能冷却 ──────────────────────────────────────────────
    private _skillCooldown: number = 0;
    private _reviveCooldown: number = 0;

    // ── 攻击状态 ──────────────────────────────────────────────
    private _attackTimer: number = 0;
    private _target: UnitController | null = null;

    // ── 回调 ──────────────────────────────────────────────────
    private _unitManager!: UnitManager;
    private _onDamageCastle!: (faction: Faction, dmg: number) => void;

    init(
        config: HeroConfig,
        faction: Faction,
        unitManager: UnitManager,
        onDamageCastle: (faction: Faction, dmg: number) => void
    ): void {
        this._config = config;
        this._faction = faction;
        this._currentHP = config.hp;
        this._isAlive = true;
        this._isOnField = true;
        this._unitManager = unitManager;
        this._onDamageCastle = onDamageCastle;
        this._skillCooldown = 0;
        this._reviveCooldown = 0;
    }

    update(dt: number): void {
        if (!GameManager.instance?.isPlaying) return;

        // 冷却计时
        if (this._skillCooldown > 0) this._skillCooldown -= dt;

        // 复活倒计时
        if (!this._isAlive) {
            this._reviveCooldown -= dt;
            if (this._reviveCooldown <= 0) {
                this._revive();
            }
            return;
        }

        this._updateCombat(dt);
    }

    // ── 战斗更新 ──────────────────────────────────────────────
    private _updateCombat(dt: number): void {
        // 寻找目标
        this._target = this._unitManager.getAllAlive(
            this._faction === Faction.PLAYER ? Faction.AI : Faction.PLAYER
        ).reduce<UnitController | null>((nearest, unit) => {
            if (!nearest) return unit;
            const distUnit = Vec3.distance(this.node.worldPosition, unit.node.worldPosition);
            const distNearest = Vec3.distance(this.node.worldPosition, nearest.node.worldPosition);
            return distUnit < distNearest ? unit : nearest;
        }, null);

        if (this._target) {
            const dist = Vec3.distance(this.node.worldPosition, this._target.node.worldPosition);
            if (dist <= this._config.attackRange) {
                // 攻击
                this._attackTimer += dt;
                if (this._attackTimer >= 1 / this._config.attackSpeed) {
                    this._attackTimer = 0;
                    this._performAttack(this._target);
                }
            } else {
                // 向目标移动
                const dir = new Vec3();
                Vec3.subtract(dir, this._target.node.worldPosition, this.node.worldPosition);
                dir.normalize();
                dir.multiplyScalar(this._config.moveSpeed * dt);
                const pos = this.node.position.clone();
                pos.add(dir);
                this.node.setPosition(pos);
            }
        } else {
            // 无目标，向敌方城堡行进
            const targetX = this._faction === Faction.PLAYER
                ? GameManager.instance.aiCastleHP > 0 ? 38 : 0
                : -38;
            const dir = this._faction === Faction.PLAYER ? 1 : -1;
            const pos = this.node.position.clone();
            pos.x += dir * this._config.moveSpeed * dt;
            this.node.setPosition(pos);
        }
    }

    private _performAttack(target: UnitController): void {
        let dmg = this._config.atk;
        target.takeDamage(dmg, this._config.damageType);
    }

    // ── 主动技能 ──────────────────────────────────────────────

    /** 玩家/AI手动触发技能 */
    useSkill(targetPosition?: Vec3, targetUnit?: UnitController): boolean {
        if (this._skillCooldown > 0) return false;
        if (!this._isAlive) return false;

        const skill = this._config.skill;
        this._skillCooldown = skill.cooldown;

        switch (skill.type) {
            case HeroSkillType.SHIELD_AURA:
                this._useShieldAura();
                break;
            case HeroSkillType.METEOR:
                if (targetPosition) this._useMeteor(targetPosition);
                break;
            case HeroSkillType.MARK:
                if (targetUnit) this._useMark(targetUnit);
                else this._autoMark(); // AI自动选目标
                break;
        }

        EventBus.emit(GameEvent.HERO_SKILL_USED, {
            heroId: this._config.id,
            faction: this._faction,
            skillType: skill.type,
        });
        return true;
    }

    // ── 护盾光环 ──────────────────────────────────────────────
    private _useShieldAura(): void {
        const skill = this._config.skill;
        const allies = this._unitManager.getAllAlive(this._faction);
        const radius = skill.range;
        allies.forEach(unit => {
            const dist = Vec3.distance(this.node.worldPosition, unit.node.worldPosition);
            if (dist <= radius) {
                unit.applyShield(skill.duration ?? 5);
            }
        });
        EventBus.emit(GameEvent.SHIELD_APPLIED, { hero: this, radius });
    }

    // ── 陨石术 ────────────────────────────────────────────────
    private _useMeteor(targetPos: Vec3): void {
        const skill = this._config.skill;
        this._unitManager.damageArea(
            targetPos,
            skill.aoeRadius ?? 5,
            skill.damage ?? 300,
            this._faction,
            DamageType.MAGIC
        );
        EventBus.emit(GameEvent.METEOR_STRIKE, {
            position: targetPos,
            radius: skill.aoeRadius ?? 5,
            damage: skill.damage ?? 300,
        });
    }

    // ── 标记 ──────────────────────────────────────────────────
    private _useMark(target: UnitController): void {
        const skill = this._config.skill;
        target.applyMark(skill.multiplier ?? 1.5, skill.duration ?? 8);
        EventBus.emit(GameEvent.MARK_APPLIED, { target, duration: skill.duration ?? 8 });
    }

    private _autoMark(): void {
        // AI自动标记：找最高价值（HP最高的存活敌方单位）
        const enemies = this._unitManager.getAllAlive(
            this._faction === Faction.PLAYER ? Faction.AI : Faction.PLAYER
        );
        if (enemies.length === 0) return;

        const target = enemies.reduce((prev, cur) =>
            cur.currentHP > prev.currentHP ? cur : prev
        );
        this._useMark(target);
    }

    // ── 受伤 ──────────────────────────────────────────────────
    takeDamage(damage: number, dmgType: DamageType): void {
        if (!this._isAlive) return;
        let finalDmg = damage;

        // 英雄护甲减免（参考普通单位规则）
        const armor = this._config.armorType;
        const isMagic = dmgType === DamageType.MAGIC || dmgType === DamageType.FLAME;
        if (armor === ArmorType.HEAVY) {
            finalDmg *= isMagic ? 1.0 : 0.7;
        } else if (armor === ArmorType.MEDIUM) {
            finalDmg *= isMagic ? 0.9 : 0.85;
        }

        this._currentHP = Math.max(0, this._currentHP - finalDmg);
        if (this._currentHP <= 0) {
            this._die();
        }
    }

    // ── 死亡与复活 ────────────────────────────────────────────
    private _die(): void {
        this._isAlive = false;
        this._reviveCooldown = this._config.reviveTime;
        this.node.active = false;
        EventBus.emit(GameEvent.HERO_DIED, {
            heroId: this._config.id,
            faction: this._faction,
            reviveIn: this._config.reviveTime,
        });
    }

    private _revive(): void {
        this._isAlive = true;
        this._currentHP = this._config.hp;
        this._reviveCooldown = 0;
        this.node.active = true;
        // 重置到己方城堡位置
        const spawnX = this._faction === Faction.PLAYER
            ? -36
            : 36;
        this.node.setPosition(new Vec3(spawnX, 0, 0));
        EventBus.emit(GameEvent.HERO_SUMMONED, {
            heroId: this._config.id,
            faction: this._faction,
            isRevive: true,
        });
    }

    // ── Getter ────────────────────────────────────────────────
    get config(): HeroConfig { return this._config; }
    get currentHP(): number { return this._currentHP; }
    get maxHP(): number { return this._config.hp; }
    get isAlive(): boolean { return this._isAlive; }
    get faction(): Faction { return this._faction; }
    get skillCooldown(): number { return this._skillCooldown; }
    get skillMaxCooldown(): number { return this._config.skill.cooldown; }
    get reviveCooldown(): number { return this._reviveCooldown; }
    get isOnField(): boolean { return this._isAlive; }
}
