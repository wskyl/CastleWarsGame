/**
 * ResultPanel.ts
 * 对局结算界面 —— 显示胜负、双方城堡HP、击杀数、时长
 */

import { _decorator, Component, Label, Button, Node, Color, director, NodeEventType } from 'cc';
import { GameEvent, Faction } from '../core/GameConstants';
import { EventBus } from '../core/EventBus';
import { GameManager, GameResult } from '../core/GameManager';

const { ccclass, property } = _decorator;

@ccclass('ResultPanel')
export class ResultPanel extends Component {

    @property(Node)
    panel: Node = null!;

    @property(Label)
    resultTitleLabel: Label = null!;

    @property(Label)
    playerCastleHPLabel: Label = null!;

    @property(Label)
    aiCastleHPLabel: Label = null!;

    @property(Label)
    playerKillsLabel: Label = null!;

    @property(Label)
    aiKillsLabel: Label = null!;

    @property(Label)
    durationLabel: Label = null!;

    @property(Label)
    reasonLabel: Label = null!;

    @property(Button)
    returnMenuButton: Button = null!;

    @property(Button)
    restartButton: Button = null!;

    onLoad(): void {
        if (this.panel) this.panel.active = false;

        EventBus.on(GameEvent.GAME_OVER, this._onGameOver, this);

        this.returnMenuButton?.node.on(NodeEventType.TOUCH_END, this._onReturnMenu, this);
        this.restartButton?.node.on(NodeEventType.TOUCH_END, this._onRestart, this);
    }

    onDestroy(): void {
        EventBus.targetOff(this);
    }

    private _onGameOver(result: GameResult): void {
        if (!result) return;
        if (this.panel) this.panel.active = true;

        // 胜负标题
        let title = '';
        if (result.winner === Faction.PLAYER) title = '🏆 胜利！';
        else if (result.winner === Faction.AI) title = '💀 失败';
        else title = '🤝 平局';

        if (this.resultTitleLabel) this.resultTitleLabel.string = title;

        // 城堡HP
        if (this.playerCastleHPLabel) {
            this.playerCastleHPLabel.string = `我方城堡: ${Math.ceil(result.playerCastleHP)} HP`;
        }
        if (this.aiCastleHPLabel) {
            this.aiCastleHPLabel.string = `敌方城堡: ${Math.ceil(result.aiCastleHP)} HP`;
        }

        // 击杀统计
        if (this.playerKillsLabel) {
            this.playerKillsLabel.string = `我方击杀: ${result.playerUnitKills}`;
        }
        if (this.aiKillsLabel) {
            this.aiKillsLabel.string = `敌方击杀: ${result.aiUnitKills}`;
        }

        // 时长
        if (this.durationLabel) {
            const m = Math.floor(result.duration / 60);
            const s = Math.floor(result.duration % 60);
            this.durationLabel.string = `对局时长: ${m}:${s.toString().padStart(2, '0')}`;
        }

        // 结束原因
        const reasonMap: Record<string, string> = {
            castle_destroyed: '城堡被摧毁',
            timeout: '时间耗尽',
            surrender: '主动投降',
        };
        if (this.reasonLabel) {
            this.reasonLabel.string = reasonMap[result.reason] ?? '';
        }
    }

    private _onReturnMenu(): void {
        GameManager.instance.returnToMainMenu();
        director.loadScene('MainMenu');
    }

    private _onRestart(): void {
        director.loadScene('GameScene');
    }
}
