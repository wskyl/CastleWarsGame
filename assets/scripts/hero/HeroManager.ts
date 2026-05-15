/**
 * HeroManager.ts
 * 英雄管理器 —— 荣耀值积累、英雄召唤、技能面板交互
 */

import { _decorator, Component, Node, Prefab, Vec3, instantiate } from 'cc';
import { HeroConfig, DEFAULT_HERO_CONFIGS } from './HeroData';
import { HeroController } from './HeroController';
import { Faction, GameEvent, GameConstants } from '../core/GameConstants';
import { EventBus } from '../core/EventBus';
import { GameManager } from '../core/GameManager';
import { UnitManager } from '../units/UnitManager';

const { ccclass, property } = _decorator;

@ccclass('HeroManager')
export class HeroManager extends Component {

    @property(Node)
    heroContainer: Node = null!;

    @property(Prefab)
    heroPrefab: Prefab = null!;

    @property({ type: UnitManager })
    unitManager: UnitManager = null!;

    // ── 荣耀值 ────────────────────────────────────────────────
    private _playerGlory: number = 0;
    private _aiGlory: number = 0;

    // ── 已召唤英雄 ────────────────────────────────────────────
    private _playerHero: HeroController | null = null;
    private _aiHero: HeroController | null = null;

    // ── 选择的英雄ID ──────────────────────────────────────────
    private _playerHeroId: string = 'guardian_general';
    private _aiHeroId: string = 'guardian_general';

    // ── 配置索引 ──────────────────────────────────────────────
    private _configMap: Map<string, HeroConfig> = new Map();

    onLoad(): void {
        DEFAULT_HERO_CONFIGS.forEach(cfg => this._configMap.set(cfg.id, cfg));

        EventBus.on(GameEvent.UNIT_DIED, this._onUnitDied, this);
        EventBus.on(GameEvent.HERO_DIED, this._onHeroDied, this);
    }

    onDestroy(): void {
        EventBus.targetOff(this);
    }

    // ── 初始化（每局开始时） ──────────────────────────────────
    reset(playerHeroId: string = 'guardian_general', aiHeroId: string = 'guardian_general'): void {
        this._playerGlory = 0;
        this._aiGlory = 0;
        this._playerHeroId = playerHeroId;
        this._aiHeroId = aiHeroId;

        // 清理旧英雄
        this._playerHero?.node?.destroy();
        this._aiHero?.node?.destroy();
        this._playerHero = null;
        this._aiHero = null;
    }

    // ── 荣耀值管理 ────────────────────────────────────────────

    addGlory(faction: Faction, amount: number): void {
        if (faction === Faction.PLAYER) {
            this._playerGlory += amount;
            EventBus.emit(GameEvent.GLORY_CHANGED, {
                faction,
                glory: this._playerGlory,
                required: GameConstants.HERO_SUMMON_GLORY,
            });
        } else {
            this._aiGlory += amount;
        }
    }

    getGlory(faction: Faction): number {
        return faction === Faction.PLAYER ? this._playerGlory : this._aiGlory;
    }

    // ── 英雄召唤 ──────────────────────────────────────────────

    /** 玩家召唤英雄 */
    playerSummonHero(): boolean {
        return this._summonHero(Faction.PLAYER, this._playerHeroId);
    }

    /** AI召唤英雄 */
    aiSummonHero(heroId: string): boolean {
        return this._summonHero(Faction.AI, heroId);
    }

    private _summonHero(faction: Faction, heroId: string): boolean {
        const config = this._configMap.get(heroId);
        if (!config) return false;

        // 校验荣耀值
        const glory = this.getGlory(faction);
        if (glory < config.gloryRequired) return false;

        // 已有英雄且活着
        const existingHero = faction === Faction.PLAYER ? this._playerHero : this._aiHero;
        if (existingHero?.isAlive) return false;

        // 扣除荣耀值
        if (faction === Faction.PLAYER) {
            this._playerGlory -= config.gloryRequired;
        } else {
            this._aiGlory -= config.gloryRequired;
        }

        // 实例化英雄
        const node = instantiate(this.heroPrefab);
        node.setParent(this.heroContainer);
        const spawnX = faction === Faction.PLAYER ? -36 : 36;
        node.setWorldPosition(new Vec3(spawnX, 0, 0));

        const ctrl = node.getComponent(HeroController) ?? node.addComponent(HeroController);
        ctrl.init(config, faction, this.unitManager, GameManager.instance.damageCastle.bind(GameManager.instance));

        if (faction === Faction.PLAYER) {
            this._playerHero = ctrl;
        } else {
            this._aiHero = ctrl;
        }

        EventBus.emit(GameEvent.HERO_SUMMONED, {
            heroId,
            faction,
            isRevive: false,
        });
        return true;
    }

    // ── 玩家手动释放技能 ──────────────────────────────────────

    usePlayerHeroSkill(targetPosition?: Vec3): boolean {
        if (!this._playerHero?.isAlive) return false;
        return this._playerHero.useSkill(targetPosition);
    }

    // ── 事件监听 ──────────────────────────────────────────────

    private _onUnitDied(data: { unit: any; faction: Faction }): void {
        // 击杀方获得荣耀值
        const killerFaction = data.faction === Faction.PLAYER ? Faction.AI : Faction.PLAYER;
        this.addGlory(killerFaction, GameConstants.GLORY_PER_UNIT_KILL);
    }

    private _onHeroDied(data: { faction: Faction }): void {
        const killerFaction = data.faction === Faction.PLAYER ? Faction.AI : Faction.PLAYER;
        this.addGlory(killerFaction, GameConstants.GLORY_PER_HERO_KILL);
    }

    // ── Getter ────────────────────────────────────────────────
    get playerHero(): HeroController | null { return this._playerHero; }
    get aiHero(): HeroController | null { return this._aiHero; }
    get playerGlory(): number { return this._playerGlory; }
    get aiGlory(): number { return this._aiGlory; }

    getHeroConfig(id: string): HeroConfig | undefined {
        return this._configMap.get(id);
    }

    getAllHeroConfigs(): HeroConfig[] {
        return DEFAULT_HERO_CONFIGS;
    }
}
