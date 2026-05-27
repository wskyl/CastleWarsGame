/**
 * GameConstants.ts
 * 游戏全局常量定义
 */

export const GameConstants = {
    // 对局参数
    MATCH_DURATION: 600,          // 总时长（秒）10分钟
    CASTLE_HP: 5000,              // 主堡初始HP
    BUILDING_SLOTS: 5,            // 每方建筑槽位数量

    // 经济参数
    INITIAL_GOLD: 500,            // 初始金币
    BASE_INCOME: 10,              // 基础收入（金/秒）
    INCOME_GROWTH: 1,             // 收入成长（金/秒²）
    MAX_INCOME: 50,               // 收入上限（金/秒）

    // 英雄参数
    HERO_REVIVE_TIME: 60,         // 英雄复活冷却（秒）
    GLORY_PER_UNIT_KILL: 10,      // 每击杀普通单位获得荣耀值
    GLORY_PER_HERO_KILL: 50,      // 击杀敌方英雄获得荣耀值
    HERO_SUMMON_GLORY: 100,       // 召唤英雄所需荣耀值

    // 天气参数
    WEATHER_CHECK_INTERVAL: 120,  // 天气检查间隔（秒）
    WEATHER_TRIGGER_CHANCE: 0.3,  // 天气触发概率

    // 建筑血量（按Tier）
    BUILDING_HP_TIER1: 200,
    BUILDING_HP_TIER2: 350,
    BUILDING_HP_TIER3: 500,
    BUILDING_HP_SPECIAL: 800,

    // 地图参数
    MAP_WIDTH: 80,                // 战场总宽度（单位）
    PLAYER_CASTLE_X: -38,         // 玩家主堡X坐标
    AI_CASTLE_X: 38,              // AI主堡X坐标
    PLAYER_BUILD_ZONE_X: -28,     // 玩家建筑区起始X
    AI_BUILD_ZONE_X: 18,          // AI建筑区起始X
    BUILD_SLOT_SPACING: 4,        // 建筑槽间距

    // 战斗阶段
    EARLY_GAME_END: 120,          // 前期结束（秒）
    MID_GAME_END: 300,            // 中期结束（秒）

    // 护甲减免
    ARMOR_HEAVY_PHYSICAL: 0.30,   // 重甲物理减免30%
    ARMOR_HEAVY_MAGIC: 0.00,      // 重甲魔法减免0%
    ARMOR_MEDIUM_PHYSICAL: 0.15,  // 中甲物理减免15%
    ARMOR_MEDIUM_MAGIC: 0.10,     // 中甲魔法减免10%
    ARMOR_LIGHT_PHYSICAL: 0.00,   // 轻甲物理减免0%
    ARMOR_LIGHT_MAGIC: 0.20,      // 轻甲魔法减免20%
    ARMOR_NONE_PHYSICAL: 0.00,    // 无甲无减免
    ARMOR_NONE_MAGIC: 0.00,

    // 图层
    LAYER_GROUND: 'ground',
    LAYER_AIR: 'air',
    LAYER_BUILDING: 'building',
    LAYER_UI: 'ui',
};

/** 游戏状态枚举 */
export enum GameState {
    NONE = 'none',
    MAIN_MENU = 'main_menu',
    LOADING = 'loading',
    PLAYING = 'playing',
    PAUSED = 'paused',
    GAME_OVER = 'game_over',
}

/** 阵营枚举 */
export enum Faction {
    PLAYER = 'player',
    AI = 'ai',
}

/** 护甲类型 */
export enum ArmorType {
    HEAVY = 'heavy',    // 重甲
    MEDIUM = 'medium',  // 中甲
    LIGHT = 'light',    // 轻甲
    NONE = 'none',      // 无甲
}

/** 伤害类型 */
export enum DamageType {
    PHYSICAL = 'physical',     // 物理
    MAGIC = 'magic',           // 魔法
    SIEGE = 'siege',           // 攻城
    ARMOR_PIERCING = 'armor_piercing', // 穿甲
    DIVINE = 'divine',         // 神圣
    FLAME = 'flame',           // 火焰
}

/** 单位类型 */
export enum UnitType {
    GROUND = 'ground',    // 地面
    AIR = 'air',          // 飞行
    BUILDING = 'building',// 建筑
    HERO = 'hero',        // 英雄
}

/** 建筑Tier */
export enum BuildingTier {
    TIER1 = 1,
    TIER2 = 2,
    TIER3 = 3,
    SPECIAL = 4,
}

/** AI难度 */
export enum AIDifficulty {
    EASY = 'easy',
    NORMAL = 'normal',
    HARD = 'hard',
}

/** 天气类型 */
export enum WeatherType {
    CLEAR = 'clear',       // 晴天
    RAIN = 'rain',         // 雨天
    FOG = 'fog',           // 浓雾
    BLIZZARD = 'blizzard', // 暴雪
}

/** 单位状态 */
export enum UnitState {
    IDLE = 'idle',
    MARCHING = 'marching',
    ATTACKING = 'attacking',
    STUNNED = 'stunned',
    DEAD = 'dead',
    STEALTHED = 'stealthed',
}

/** 游戏事件名称 */
export enum GameEvent {
    GOLD_CHANGED = 'gold_changed',
    BUILDING_BUILT = 'building_built',
    BUILDING_DESTROYED = 'building_destroyed',
    UNIT_SPAWNED = 'unit_spawned',
    UNIT_DIED = 'unit_died',
    CASTLE_HP_CHANGED = 'castle_hp_changed',
    HERO_SUMMONED = 'hero_summoned',
    HERO_DIED = 'hero_died',
    HERO_SKILL_USED = 'hero_skill_used',
    GLORY_CHANGED = 'glory_changed',
    WEATHER_CHANGED = 'weather_changed',
    GAME_STATE_CHANGED = 'game_state_changed',
    MATCH_TIMER_UPDATED = 'match_timer_updated',
    GAME_OVER = 'game_over',
    INCOME_RATE_CHANGED = 'income_rate_changed',
    UNIT_STUN = 'unit_stun',
    MARK_APPLIED = 'mark_applied',
    SHIELD_APPLIED = 'shield_applied',
    METEOR_STRIKE = 'meteor_strike',
}
