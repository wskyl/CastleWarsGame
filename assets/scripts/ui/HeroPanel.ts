/**
 * HeroPanel.ts
 * 英雄面板 UI —— 英雄头像、HP条、荣耀值进度、技能按钮（含冷却显示）
 */

import { _decorator, Component, Label, ProgressBar, Button, Node, Vec2, UITransform, EventTouch } from 'cc';
import { HeroManager } from '../hero/HeroManager';
import { Faction, GameEvent, GameConstants } from '../core/GameConstants';
import { EventBus } from '../core/EventBus';
import { HeroSkillType } from '../hero/HeroData';
import { GameManager } from '../core/GameManager';

const { ccclass, property } = _decorator;

@ccclass('HeroPanel')
export class HeroPanel extends Component {

    // ── 英雄召唤 ──────────────────────────────────────────────
    @property(Button)
    summonButton: Button = null!;

    @property(Label)
    gloryLabel: Label = null!;

    @property(ProgressBar)
    gloryBar: ProgressBar = null!;

    // ── 英雄状态 ──────────────────────────────────────────────
    @property(ProgressBar)
    heroHPBar: ProgressBar = null!;

    @property(Label)
    heroHPLabel: Label = null!;

    @property(Label)
    heroNameLabel: Label = null!;

    @property(Label)
    reviveLabel: Label = null!;

    // ── 技能按钮 ──────────────────────────────────────────────
    @property(Button)
    skillButton: Button = null!;

    @property(Label)
    skillCooldownLabel: Label = null!;

    @property(ProgressBar)
    skillCooldownBar: ProgressBar = null!;

    @property({ type: HeroManager })
    heroManager: HeroManager = null!;

    // ── 内部 ──────────────────────────────────────────────────
    private _glory: number = 0;
    private _needAimSkill: boolean = false; // 陨石术需要点击目标位置

    onLoad(): void {
        EventBus.on(GameEvent.GLORY_CHANGED, this._onGloryChanged, this);
        EventBus.on(GameEvent.HERO_SUMMONED, this._onHeroSummoned, this);
        EventBus.on(GameEvent.HERO_DIED, this._onHeroDied, this);

        this.summonButton?.node.on(Button.EventType.CLICK, this._onSummonClicked, this);
        this.skillButton?.node.on(Button.EventType.CLICK, this._onSkillClicked, this);

        this._refreshGlory(0);
        this._setHeroPanelVisible(false);
    }

    onDestroy(): void {
        EventBus.targetOff(this);
    }

    update(dt: number): void {
        const hero = this.heroManager?.playerHero;
        if (!hero) return;

        // 更新HP
        if (this.heroHPBar) this.heroHPBar.progress = hero.currentHP / hero.maxHP;
        if (this.heroHPLabel) this.heroHPLabel.string = `${Math.ceil(hero.currentHP)}/${hero.maxHP}`;

        // 更新技能冷却
        const cdRatio = hero.skillCooldown / hero.skillMaxCooldown;
        if (this.skillCooldownBar) this.skillCooldownBar.progress = cdRatio;
        if (this.skillCooldownLabel) {
            this.skillCooldownLabel.string = hero.skillCooldown > 0
                ? `${Math.ceil(hero.skillCooldown)}s`
                : '就绪';
        }
        if (this.skillButton) this.skillButton.interactable = hero.skillCooldown <= 0 && hero.isAlive;

        // 复活倒计时
        if (!hero.isAlive && this.reviveLabel) {
            this.reviveLabel.string = `复活: ${Math.ceil(hero.reviveCooldown)}s`;
            this.reviveLabel.node.active = true;
        } else if (this.reviveLabel) {
            this.reviveLabel.node.active = false;
        }
    }

    // ── 荣耀值 ────────────────────────────────────────────────
    private _onGloryChanged(data: { faction: Faction; glory: number; required: number }): void {
        if (data.faction !== Faction.PLAYER) return;
        this._glory = data.glory;
        this._refreshGlory(data.glory);
    }

    private _refreshGlory(glory: number): void {
        const required = GameConstants.HERO_SUMMON_GLORY;
        if (this.gloryLabel) this.gloryLabel.string = `${glory}/${required}`;
        if (this.gloryBar) this.gloryBar.progress = Math.min(1, glory / required);
        if (this.summonButton) {
            this.summonButton.interactable = glory >= required && !this.heroManager?.playerHero?.isAlive;
        }
    }

    // ── 英雄召唤 ──────────────────────────────────────────────
    private _onSummonClicked(): void {
        const success = this.heroManager?.playerSummonHero();
        if (success) {
            this._setHeroPanelVisible(true);
        }
    }

    private _onHeroSummoned(data: { faction: Faction }): void {
        if (data.faction !== Faction.PLAYER) return;
        const hero = this.heroManager.playerHero;
        if (!hero) return;
        if (this.heroNameLabel) this.heroNameLabel.string = hero.config.displayName;
        this._setHeroPanelVisible(true);
    }

    private _onHeroDied(data: { faction: Faction }): void {
        if (data.faction !== Faction.PLAYER) return;
        // 英雄死亡时显示复活倒计时，面板保持可见
    }

    private _setHeroPanelVisible(visible: boolean): void {
        [this.heroHPBar?.node, this.heroHPLabel?.node, this.heroNameLabel?.node,
         this.skillButton?.node, this.skillCooldownBar?.node, this.skillCooldownLabel?.node]
            .forEach(n => { if (n) n.active = visible; });
    }

    // ── 技能释放 ──────────────────────────────────────────────
    private _onSkillClicked(): void {
        const hero = this.heroManager?.playerHero;
        if (!hero?.isAlive) return;

        const skillType = hero.config.skill.type;
        if (skillType === HeroSkillType.METEOR) {
            // 陨石术需要玩家点击目标位置：开启瞄准模式
            this._needAimSkill = true;
            // 监听点击事件来确定目标
            this.node.on(Node.EventType.TOUCH_END, this._onAimTouchEnd, this);
        } else {
            // 护盾/标记直接使用
            this.heroManager.usePlayerHeroSkill();
            this._needAimSkill = false;
        }
    }

    private _onAimTouchEnd(event: EventTouch): void {
        if (!this._needAimSkill) return;
        this._needAimSkill = false;
        this.node.off(Node.EventType.TOUCH_END, this._onAimTouchEnd, this);

        // 将屏幕坐标转换为世界坐标（简化：直接传touch位置）
        const touchPos = event.getUILocation();
        // 实际项目中需通过摄像机将 UI 坐标转换为世界坐标
        this.heroManager.usePlayerHeroSkill(
            // 粗略转换（实际应使用Camera.screenToWorld）
            new (require('cc').Vec3)(touchPos.x - 400, 0, touchPos.y - 300)
        );
    }
}
