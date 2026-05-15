/**
 * BuildingMenuPanel.ts
 * 建筑菜单面板 —— 显示5个槽位，支持点击选择建造
 */

import { _decorator, Component, Node, Label, Button, Sprite, Color, ScrollView, instantiate, Prefab } from 'cc';
import { BuildingConfig } from '../buildings/BuildingData';
import { BuildingManager } from '../buildings/BuildingManager';
import { EconomyManager } from '../economy/EconomyManager';
import { Faction, GameEvent, BuildingTier } from '../core/GameConstants';
import { EventBus } from '../core/EventBus';

const { ccclass, property } = _decorator;

/** 槽位UI项 */
interface SlotUIItem {
    slotNode: Node;
    nameLabel: Label;
    buildButton: Button;
    buildListNode: Node;
}

@ccclass('BuildingMenuPanel')
export class BuildingMenuPanel extends Component {

    @property([Node])
    slotNodes: Node[] = [];       // 5个槽位节点

    @property(Prefab)
    buildOptionPrefab: Prefab = null!; // 建筑选项预制体

    @property({ type: BuildingManager })
    buildingManager: BuildingManager = null!;

    @property({ type: EconomyManager })
    economyManager: EconomyManager = null!;

    // ── 当前展开的槽位 ─────────────────────────────────────────
    private _selectedSlot: number = -1;
    private _slotItems: SlotUIItem[] = [];

    onLoad(): void {
        this._initSlotItems();

        EventBus.on(GameEvent.GOLD_CHANGED, this._onGoldChanged, this);
        EventBus.on(GameEvent.BUILDING_BUILT, this._onBuildingChanged, this);
        EventBus.on(GameEvent.BUILDING_DESTROYED, this._onBuildingChanged, this);
    }

    onDestroy(): void {
        EventBus.targetOff(this);
    }

    // ── 初始化槽位UI ──────────────────────────────────────────
    private _initSlotItems(): void {
        this.slotNodes.forEach((node, i) => {
            if (!node) return;

            const nameLabel = node.getComponentInChildren(Label)!;
            const buildButton = node.getComponent(Button)!;
            const buildListNode = node.getChildByName('BuildList')!;

            const item: SlotUIItem = { slotNode: node, nameLabel, buildButton, buildListNode };
            this._slotItems.push(item);

            buildButton?.node.on(Button.EventType.CLICK, () => this._onSlotClicked(i), this);
            if (buildListNode) buildListNode.active = false;

            this._refreshSlot(i);
        });
    }

    // ── 槽位点击 ──────────────────────────────────────────────
    private _onSlotClicked(slotIndex: number): void {
        const slots = this.buildingManager.getSlots(Faction.PLAYER);
        const slot = slots[slotIndex];

        if (slot.building !== null) {
            // 已有建筑 → 显示建筑信息（此处简化：不操作）
            this._closeAllBuildLists();
            return;
        }

        // 空槽位 → 显示可建造列表
        if (this._selectedSlot === slotIndex) {
            this._closeAllBuildLists();
            return;
        }

        this._closeAllBuildLists();
        this._selectedSlot = slotIndex;
        this._showBuildList(slotIndex);
    }

    private _showBuildList(slotIndex: number): void {
        const item = this._slotItems[slotIndex];
        if (!item?.buildListNode) return;

        // 清空旧内容
        item.buildListNode.removeAllChildren();

        const gold = this.economyManager.getGold(Faction.PLAYER);
        const availables = this.buildingManager.getAvailableBuildings(Faction.PLAYER, Number.MAX_VALUE);

        availables.forEach(cfg => {
            const optNode = instantiate(this.buildOptionPrefab);
            optNode.setParent(item.buildListNode);

            // 设置建筑名/费用标签
            const labels = optNode.getComponentsInChildren(Label);
            if (labels[0]) labels[0].string = cfg.displayName;
            if (labels[1]) labels[1].string = `${cfg.cost} 💰`;

            // 费用不足时置灰
            const btn = optNode.getComponent(Button);
            if (btn) {
                const canAfford = gold >= cfg.cost;
                btn.interactable = canAfford;
                if (labels[1]) labels[1].color = canAfford ? Color.WHITE : Color.GRAY;

                btn.node.on(Button.EventType.CLICK, () => {
                    this._buildBuilding(cfg, slotIndex);
                }, this);
            }
        });

        item.buildListNode.active = true;
    }

    private _buildBuilding(cfg: BuildingConfig, slotIndex: number): void {
        const success = this.buildingManager.playerBuild(cfg.id, slotIndex);
        if (success) {
            this._closeAllBuildLists();
            this._refreshSlot(slotIndex);
        }
    }

    private _closeAllBuildLists(): void {
        this._selectedSlot = -1;
        this._slotItems.forEach(item => {
            if (item?.buildListNode) item.buildListNode.active = false;
        });
    }

    // ── 刷新单个槽位 ──────────────────────────────────────────
    private _refreshSlot(slotIndex: number): void {
        const item = this._slotItems[slotIndex];
        if (!item) return;

        const slots = this.buildingManager.getSlots(Faction.PLAYER);
        const slot = slots[slotIndex];

        if (slot?.building?.isAlive) {
            item.nameLabel.string = slot.building.config.displayName;
        } else {
            item.nameLabel.string = `槽位 ${slotIndex + 1}`;
        }
    }

    // ── 事件响应 ──────────────────────────────────────────────
    private _onGoldChanged(data: { faction: Faction }): void {
        if (data.faction !== Faction.PLAYER) return;
        // 更新可建造列表的可用性
        if (this._selectedSlot !== -1) {
            this._showBuildList(this._selectedSlot);
        }
    }

    private _onBuildingChanged(data: { faction: Faction; slotIndex: number }): void {
        if (data.faction !== Faction.PLAYER) return;
        this._refreshSlot(data.slotIndex);
    }
}
