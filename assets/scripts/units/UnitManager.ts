/**
 * UnitManager.ts
 * 单位管理器 —— 管理全场所有单位的生命周期，使用对象池优化性能
 */

import { _decorator, Component, Node, Prefab, Vec3, instantiate } from 'cc';
import { UnitController } from './UnitController';
import { UnitConfig, DEFAULT_UNIT_CONFIGS, AbilityType } from './UnitData';
import { Faction, GameEvent, DamageType, UnitType } from '../core/GameConstants';
import { EventBus } from '../core/EventBus';
import { GameManager } from '../core/GameManager';
import { ObjectPool } from '../core/ObjectPool';

const { ccclass, property } = _decorator;

@ccclass('UnitManager')
export class UnitManager extends Component {

    @property(Node)
    unitContainer: Node = null!;

    @property(Prefab)
    unitPrefab: Prefab = null!;

    // ── 运行时单位列表 ────────────────────────────────────────
    private _playerUnits: UnitController[] = [];
    private _aiUnits: UnitController[] = [];

    // ── 对象池 ────────────────────────────────────────────────
    private _pool!: ObjectPool;

    // ── 配置索引 ──────────────────────────────────────────────
    private _configMap: Map<string, UnitConfig> = new Map();

    onLoad(): void {
        DEFAULT_UNIT_CONFIGS.forEach(cfg => this._configMap.set(cfg.id, cfg));
        this._pool = new ObjectPool(this.unitPrefab, 'unit_pool');
        this._pool.warmUp(30, this.unitContainer);

        EventBus.on(GameEvent.UNIT_DIED, this._onUnitDied, this);
    }

    onDestroy(): void {
        EventBus.targetOff(this);
        this._pool?.clear();
    }

    update(dt: number): void {
        // 处理圣骑士光环
        this._processAuras();
    }

    // ── 单位生成（由 BuildingController 回调触发） ────────────
    spawnUnit(unitId: string, position: Vec3, faction: Faction): UnitController | null {
        const config = this._configMap.get(unitId);
        if (!config) {
            console.warn(`[UnitManager] Unknown unit: ${unitId}`);
            return null;
        }

        const node = this._pool.get(this.unitContainer);
        node.setWorldPosition(position);

        const ctrl = node.getComponent(UnitController) ?? node.addComponent(UnitController);
        ctrl.init(config, faction, {
            findNearestEnemy: this._findNearestEnemy.bind(this),
            onDied: this._returnToPool.bind(this),
            onDamageCastle: GameManager.instance.damageCastle.bind(GameManager.instance),
            onRequestAoeHit: this._handleAoeHit.bind(this),
        });

        if (faction === Faction.PLAYER) {
            this._playerUnits.push(ctrl);
        } else {
            this._aiUnits.push(ctrl);
        }

        EventBus.emit(GameEvent.UNIT_SPAWNED, { unit: ctrl, faction });
        return ctrl;
    }

    // ── 找最近的敌方单位 ──────────────────────────────────────
    private _findNearestEnemy(self: UnitController): UnitController | null {
        const enemies = self.faction === Faction.PLAYER ? this._aiUnits : this._playerUnits;
        let nearest: UnitController | null = null;
        let minDist = Infinity;

        for (const enemy of enemies) {
            if (!enemy.isAlive) continue;
            // 飞行单位只能被有反空能力的单位锁定（已在 inRange 处理）
            const dist = Vec3.distance(self.node.worldPosition, enemy.node.worldPosition);
            if (dist < minDist) {
                minDist = dist;
                nearest = enemy;
            }
        }
        return nearest;
    }

    // ── AOE 打击 ──────────────────────────────────────────────
    private _handleAoeHit(
        center: Vec3,
        radius: number,
        damage: number,
        attackerFaction: Faction,
        dmgType: DamageType
    ): void {
        const targets = attackerFaction === Faction.PLAYER ? this._aiUnits : this._playerUnits;
        for (const unit of targets) {
            if (!unit.isAlive) continue;
            const dist = Vec3.distance(center, unit.node.worldPosition);
            if (dist <= radius) {
                unit.takeDamage(damage, dmgType);
            }
        }
    }

    // ── 圣骑士光环 ────────────────────────────────────────────
    private _processAuras(): void {
        this._applyAuraForFaction(Faction.PLAYER, this._playerUnits);
        this._applyAuraForFaction(Faction.AI, this._aiUnits);
    }

    private _applyAuraForFaction(faction: Faction, units: UnitController[]): void {
        // 找到圣骑士
        const paladins = units.filter(u => u.isAlive && u.config.id === 'paladin');
        if (paladins.length === 0) return;
        // 光环半径：粗略用5单位
        const auraRadius = 5;
        for (const paladin of paladins) {
            // 通知附近友军（通过事件，由英雄/单位接收）
            // 实际 ATK 加成可在攻击时检查附近是否有活着的圣骑士
        }
    }

    /** 检查指定位置附近是否有存活的圣骑士（供 UnitController 使用） */
    hasNearbyPaladin(position: Vec3, faction: Faction, radius: number = 5): boolean {
        const units = faction === Faction.PLAYER ? this._playerUnits : this._aiUnits;
        return units.some(u =>
            u.isAlive && u.config.id === 'paladin' &&
            Vec3.distance(u.node.worldPosition, position) <= radius
        );
    }

    // ── 对指定区域内所有单位造成伤害（陨石术等） ──────────────
    damageArea(center: Vec3, radius: number, damage: number, attackerFaction: Faction, dmgType: DamageType): void {
        const allUnits = [...this._playerUnits, ...this._aiUnits];
        for (const unit of allUnits) {
            if (!unit.isAlive) continue;
            if (unit.faction === attackerFaction) continue; // 不伤己方
            const dist = Vec3.distance(center, unit.node.worldPosition);
            if (dist <= radius) {
                unit.takeDamage(damage, dmgType);
            }
        }
    }

    // ── 眩晕区域（可扩展） ────────────────────────────────────
    stunArea(center: Vec3, radius: number, duration: number, attackerFaction: Faction): void {
        const targets = attackerFaction === Faction.PLAYER ? this._aiUnits : this._playerUnits;
        for (const unit of targets) {
            if (!unit.isAlive) continue;
            if (Vec3.distance(center, unit.node.worldPosition) <= radius) {
                unit.applyStun(duration);
            }
        }
    }

    // ── 单位死亡回调 ──────────────────────────────────────────
    private _returnToPool(unit: UnitController): void {
        // 从列表移除
        const pIdx = this._playerUnits.indexOf(unit);
        if (pIdx !== -1) this._playerUnits.splice(pIdx, 1);
        const aIdx = this._aiUnits.indexOf(unit);
        if (aIdx !== -1) this._aiUnits.splice(aIdx, 1);

        // 归还对象池
        this._pool.put(unit.node);
    }

    private _onUnitDied(data: { unit: UnitController; faction: Faction }): void {
        // 统计击杀（击杀方是对方）
        const killerFaction = data.faction === Faction.PLAYER ? Faction.AI : Faction.PLAYER;
        GameManager.instance.recordKill(killerFaction);
    }

    // ── 清空所有单位 ──────────────────────────────────────────
    clearAll(): void {
        [...this._playerUnits, ...this._aiUnits].forEach(u => {
            if (u.node.isValid) this._pool.put(u.node);
        });
        this._playerUnits = [];
        this._aiUnits = [];
    }

    // ── Getter ────────────────────────────────────────────────
    get playerUnits(): UnitController[] { return this._playerUnits; }
    get aiUnits(): UnitController[] { return this._aiUnits; }
    get totalUnits(): number { return this._playerUnits.length + this._aiUnits.length; }

    getConfigById(id: string): UnitConfig | undefined {
        return this._configMap.get(id);
    }

    getAllAlive(faction: Faction): UnitController[] {
        return (faction === Faction.PLAYER ? this._playerUnits : this._aiUnits)
            .filter(u => u.isAlive);
    }
}
