/**
 * ConfigLoader.ts
 * JSON 配置加载器 —— 从 resources/configs/ 加载游戏数据配置
 */

import { _decorator, Component, resources, JsonAsset } from 'cc';
import { UnitConfig } from '../units/UnitData';
import { BuildingConfig } from '../buildings/BuildingData';
import { HeroConfig } from '../hero/HeroData';

const { ccclass, property } = _decorator;

@ccclass('ConfigLoader')
export class ConfigLoader extends Component {

    private static _instance: ConfigLoader | null = null;
    static get instance(): ConfigLoader { return ConfigLoader._instance!; }

    private _units: Map<string, UnitConfig> = new Map();
    private _buildings: Map<string, BuildingConfig> = new Map();
    private _heroes: Map<string, HeroConfig> = new Map();
    private _loaded: boolean = false;

    onLoad(): void {
        if (ConfigLoader._instance && ConfigLoader._instance !== this) {
            this.destroy();
            return;
        }
        ConfigLoader._instance = this;
    }

    onDestroy(): void {
        if (ConfigLoader._instance === this) {
            ConfigLoader._instance = null;
        }
    }

    // ── 异步加载所有配置 ──────────────────────────────────────
    async loadAll(): Promise<void> {
        await Promise.all([
            this._loadConfig<UnitConfig[]>('configs/units', (data) => {
                data.forEach(cfg => this._units.set(cfg.id, cfg));
            }),
            this._loadConfig<BuildingConfig[]>('configs/buildings', (data) => {
                data.forEach(cfg => this._buildings.set(cfg.id, cfg));
            }),
            this._loadConfig<HeroConfig[]>('configs/heroes', (data) => {
                data.forEach(cfg => this._heroes.set(cfg.id, cfg));
            }),
        ]);
        this._loaded = true;
        console.log('[ConfigLoader] All configs loaded successfully.');
    }

    private _loadConfig<T>(path: string, onSuccess: (data: T) => void): Promise<void> {
        return new Promise((resolve, reject) => {
            resources.load(path, JsonAsset, (err, asset) => {
                if (err) {
                    console.error(`[ConfigLoader] Failed to load "${path}":`, err);
                    // 降级：使用内置默认配置，不中断游戏
                    resolve();
                    return;
                }
                try {
                    onSuccess(asset.json as T);
                } catch (e) {
                    console.error(`[ConfigLoader] Parse error in "${path}":`, e);
                }
                resolve();
            });
        });
    }

    // ── 查询接口 ──────────────────────────────────────────────
    getUnit(id: string): UnitConfig | undefined {
        return this._units.get(id);
    }

    getBuilding(id: string): BuildingConfig | undefined {
        return this._buildings.get(id);
    }

    getHero(id: string): HeroConfig | undefined {
        return this._heroes.get(id);
    }

    getAllUnits(): UnitConfig[] {
        return Array.from(this._units.values());
    }

    getAllBuildings(): BuildingConfig[] {
        return Array.from(this._buildings.values());
    }

    getAllHeroes(): HeroConfig[] {
        return Array.from(this._heroes.values());
    }

    get isLoaded(): boolean { return this._loaded; }
}
