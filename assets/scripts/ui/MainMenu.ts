/**
 * MainMenu.ts
 * 主菜单 UI —— 开始游戏、选择难度、设置
 */

import { _decorator, Component, Button, Label, Node, director } from 'cc';
import { AIDifficulty, GameEvent, GameState } from '../core/GameConstants';
import { GameManager } from '../core/GameManager';
import { EventBus } from '../core/EventBus';

const { ccclass, property } = _decorator;

@ccclass('MainMenu')
export class MainMenu extends Component {

    // ── 按钮 ──────────────────────────────────────────────────
    @property(Button)
    easyButton: Button = null!;

    @property(Button)
    normalButton: Button = null!;

    @property(Button)
    hardButton: Button = null!;

    @property(Button)
    startButton: Button = null!;

    @property(Button)
    settingsButton: Button = null!;

    // ── 难度提示 ──────────────────────────────────────────────
    @property(Label)
    difficultyLabel: Label = null!;

    @property(Node)
    settingsPanel: Node = null!;

    // ── 当前选择的难度 ────────────────────────────────────────
    private _selectedDifficulty: AIDifficulty = AIDifficulty.NORMAL;

    onLoad(): void {
        this.easyButton?.node.on(Button.EventType.CLICK, () => this._selectDifficulty(AIDifficulty.EASY), this);
        this.normalButton?.node.on(Button.EventType.CLICK, () => this._selectDifficulty(AIDifficulty.NORMAL), this);
        this.hardButton?.node.on(Button.EventType.CLICK, () => this._selectDifficulty(AIDifficulty.HARD), this);
        this.startButton?.node.on(Button.EventType.CLICK, this._onStartGame, this);
        this.settingsButton?.node.on(Button.EventType.CLICK, this._onSettings, this);

        if (this.settingsPanel) this.settingsPanel.active = false;

        this._selectDifficulty(AIDifficulty.NORMAL);
    }

    onDestroy(): void {
        EventBus.targetOff(this);
    }

    private _selectDifficulty(diff: AIDifficulty): void {
        this._selectedDifficulty = diff;

        const names: Record<AIDifficulty, string> = {
            [AIDifficulty.EASY]: '简单',
            [AIDifficulty.NORMAL]: '普通',
            [AIDifficulty.HARD]: '困难',
        };

        if (this.difficultyLabel) {
            this.difficultyLabel.string = `难度: ${names[diff]}`;
        }

        // 更新按钮选中状态（通过颜色/缩放表现，需美术配合）
    }

    private _onStartGame(): void {
        GameManager.instance.startGame(this._selectedDifficulty);
        // 由 GameManager 的状态机驱动场景切换（或在此直接 loadScene）
        director.loadScene('GameScene');
    }

    private _onSettings(): void {
        if (this.settingsPanel) {
            this.settingsPanel.active = !this.settingsPanel.active;
        }
    }
}
