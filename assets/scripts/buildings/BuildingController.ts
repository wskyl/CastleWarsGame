/**
 * BuildingController.ts
 * 单个建筑的运行时控制器组件
 */

import { _decorator, Component, Node, Vec3 } from 'cc';
import { BuildingConfig, BuildingRuntimeData } from './BuildingData';
import { Faction, GameEvent, BuildingTier } from '../core/GameConstants';
import { EventBus } from '../core/EventBus';
import { GameManager } from '../core/GameManager';

const { ccclass, property } = _decorator;

@ccclass('BuildingController')
export class BuildingController extends Component {

    private _data!: BuildingRuntimeData;
    private _faction!: Faction;
    private _onSpawnUnit!: (unitId: string, position: Vec3, faction: Faction) => void;

    /** 初始化（替代构造函数） */
    init(
        config: BuildingConfig,
        slotIndex: number,
        faction: Faction,
        onSpawnUnit: (unitId: string, position: Vec3, faction: Faction) => void
    ): void {
        this._faction = faction;
        this._onSpawnUnit = onSpawnUnit;
        this._data = {
            config,
            currentHP: config.hp,
            produceTimer: config.produceInterval,
            isAlive: true,
            slotIndex,
        };
    }

    update(dt: number): void {
        if (!this._data?.isAlive) return;
        if (!GameManager.instance?.isPlaying) return;

        this._data.produceTimer -= dt;
        if (this._data.produceTimer <= 0) {
            this._data.produceTimer = this._data.config.produceInterval;
            this._spawnUnit();
        }
    }

    /** 受到伤害 */
    takeDamage(damage: number): void {
        if (!this._data.isAlive) return;
        this._data.currentHP = Math.max(0, this._data.currentHP - damage);

        if (this._data.currentHP <= 0) {
            this._destroy();
        }
    }

    /** 生产单位 */
    private _spawnUnit(): void {
        const spawnPos = this.node.worldPosition.clone();
        this._onSpawnUnit(this._data.config.produceUnitId, spawnPos, this._faction);
    }

    /** 建筑摧毁 */
    private _destroy(): void {
        this._data.isAlive = false;
        EventBus.emit(GameEvent.BUILDING_DESTROYED, {
            buildingId: this._data.config.id,
            faction: this._faction,
            slotIndex: this._data.slotIndex,
        });
        this.node.destroy();
    }

    // ── Getter ────────────────────────────────────────────────
    get config(): BuildingConfig { return this._data.config; }
    get currentHP(): number { return this._data.currentHP; }
    get maxHP(): number { return this._data.config.hp; }
    get isAlive(): boolean { return this._data.isAlive; }
    get faction(): Faction { return this._faction; }
    get slotIndex(): number { return this._data.slotIndex; }
    get produceProgress(): number {
        return 1 - this._data.produceTimer / this._data.config.produceInterval;
    }
}
