/**
 * BuildingManager.ts
 * 建筑管理器 —— 管理双方所有建筑槽位与建造逻辑
 */

import { _decorator, Component, Node, Prefab, Vec3, instantiate } from 'cc';
import { BuildingConfig, DEFAULT_BUILDING_CONFIGS } from './BuildingData';
import { BuildingController } from './BuildingController';
import { Faction, GameEvent, GameConstants } from '../core/GameConstants';
import { EventBus } from '../core/EventBus';
import { EconomyManager } from '../economy/EconomyManager';

const { ccclass, property } = _decorator;

interface SlotState {
    building: BuildingController | null;
    position: Vec3;
}

@ccclass('BuildingManager')
export class BuildingManager extends Component {

    @property(Node)
    playerBuildZone: Node = null!;

    @property(Node)
    aiBuildZone: Node = null!;

    @property(Prefab)
    buildingPrefab: Prefab = null!;

    // ── 槽位状态 ───────────────────────────────────────────────
    private _playerSlots: SlotState[] = [];
    private _aiSlots: SlotState[] = [];

    // ── 建筑ID→Config索引 ───────────────────────────────────────
    private _configMap: Map<string, BuildingConfig> = new Map();

    // ── 单位生成回调（由UnitManager注入） ───────────────────────
    private _onSpawnUnit!: (unitId: string, pos: Vec3, faction: Faction) => void;

    onLoad(): void {
        // 构建配置索引
        DEFAULT_BUILDING_CONFIGS.forEach(cfg => this._configMap.set(cfg.id, cfg));
        // 初始化槽位
        this._initSlots(Faction.PLAYER, this._playerSlots, this.playerBuildZone);
        this._initSlots(Faction.AI, this._aiSlots, this.aiBuildZone);

        // 监听建筑摧毁
        EventBus.on(GameEvent.BUILDING_DESTROYED, this._onBuildingDestroyed, this);
    }

    onDestroy(): void {
        EventBus.targetOff(this);
    }

    setSpawnCallback(cb: (unitId: string, pos: Vec3, faction: Faction) => void): void {
        this._onSpawnUnit = cb;
    }

    // ── 初始化槽位 ────────────────────────────────────────────
    private _initSlots(faction: Faction, slots: SlotState[], zone: Node): void {
        const startX = faction === Faction.PLAYER
            ? GameConstants.PLAYER_BUILD_ZONE_X
            : GameConstants.AI_BUILD_ZONE_X;

        for (let i = 0; i < GameConstants.BUILDING_SLOTS; i++) {
            const x = startX + i * GameConstants.BUILD_SLOT_SPACING;
            slots.push({
                building: null,
                position: new Vec3(x, 0, 0),
            });
        }
    }

    // ── 建造建筑 ──────────────────────────────────────────────

    /** 玩家建造建筑 */
    playerBuild(buildingId: string, slotIndex: number): boolean {
        return this._build(Faction.PLAYER, buildingId, slotIndex);
    }

    /** AI建造建筑 */
    aiBuild(buildingId: string, slotIndex: number): boolean {
        return this._build(Faction.AI, buildingId, slotIndex);
    }

    private _build(faction: Faction, buildingId: string, slotIndex: number): boolean {
        const config = this._configMap.get(buildingId);
        if (!config) {
            console.warn(`[BuildingManager] Unknown building: ${buildingId}`);
            return false;
        }

        const slots = faction === Faction.PLAYER ? this._playerSlots : this._aiSlots;

        // 校验槽位
        if (slotIndex < 0 || slotIndex >= slots.length) return false;
        if (slots[slotIndex].building !== null) return false;

        // 校验金币
        const economy = EconomyManager.instance;
        if (!economy.spendGold(faction, config.cost)) return false;

        // 校验前置条件
        if (!this._checkPrerequisites(faction, config)) {
            economy.spendGold(faction, -config.cost); // 退还（实际调用应在前置检查后）
            return false;
        }

        // 实例化建筑节点
        const node = instantiate(this.buildingPrefab);
        const zone = faction === Faction.PLAYER ? this.playerBuildZone : this.aiBuildZone;
        node.setParent(zone);
        node.setWorldPosition(slots[slotIndex].position);

        const ctrl = node.getComponent(BuildingController)
            ?? node.addComponent(BuildingController);

        ctrl.init(config, slotIndex, faction, this._onSpawnUnit);
        slots[slotIndex].building = ctrl;

        EventBus.emit(GameEvent.BUILDING_BUILT, {
            buildingId,
            faction,
            slotIndex,
            config,
        });

        return true;
    }

    // ── 前置条件校验 ──────────────────────────────────────────
    private _checkPrerequisites(faction: Faction, config: BuildingConfig): boolean {
        if (!config.prerequisites || config.prerequisites.length === 0) return true;

        const slots = faction === Faction.PLAYER ? this._playerSlots : this._aiSlots;
        const ownedIds = new Set<string>();
        slots.forEach(slot => {
            if (slot.building?.isAlive) {
                ownedIds.add(slot.building.config.id);
            }
        });

        const logic = config.prerequisiteLogic ?? 'AND';

        switch (logic) {
            case 'AND':
                return config.prerequisites.every(id => ownedIds.has(id));
            case 'OR':
                return config.prerequisites.some(id => ownedIds.has(id));
            case 'COUNT': {
                const count = config.prerequisites.filter(id => ownedIds.has(id)).length;
                return count >= (config.prerequisiteCount ?? 1);
            }
            default:
                return true;
        }
    }

    // ── 建筑摧毁回调 ─────────────────────────────────────────
    private _onBuildingDestroyed(data: { faction: Faction; slotIndex: number }): void {
        const slots = data.faction === Faction.PLAYER ? this._playerSlots : this._aiSlots;
        if (slots[data.slotIndex]) {
            slots[data.slotIndex].building = null;
        }
    }

    // ── 查询接口 ──────────────────────────────────────────────

    getSlots(faction: Faction): SlotState[] {
        return faction === Faction.PLAYER ? this._playerSlots : this._aiSlots;
    }

    getAvailableBuildings(faction: Faction, currentGold: number): BuildingConfig[] {
        return DEFAULT_BUILDING_CONFIGS.filter(cfg =>
            cfg.cost <= currentGold &&
            this._checkPrerequisites(faction, cfg) &&
            this._hasEmptySlot(faction)
        );
    }

    private _hasEmptySlot(faction: Faction): boolean {
        const slots = faction === Faction.PLAYER ? this._playerSlots : this._aiSlots;
        return slots.some(s => s.building === null);
    }

    getFirstEmptySlot(faction: Faction): number {
        const slots = faction === Faction.PLAYER ? this._playerSlots : this._aiSlots;
        return slots.findIndex(s => s.building === null);
    }

    getAllBuildings(faction: Faction): BuildingController[] {
        const slots = faction === Faction.PLAYER ? this._playerSlots : this._aiSlots;
        return slots
            .filter(s => s.building !== null && s.building.isAlive)
            .map(s => s.building!);
    }

    getConfigById(id: string): BuildingConfig | undefined {
        return this._configMap.get(id);
    }

    getAllConfigs(): BuildingConfig[] {
        return DEFAULT_BUILDING_CONFIGS;
    }
}
