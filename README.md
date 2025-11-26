# 城堡战争 (Castle Wars) - Unity游戏项目

## 项目简介

这是一款基于魔兽争霸DLC《城堡战争》玩法的2.5D多人对战手游。玩家通过建造建筑来自动生产单位，单位会自动向对方城堡前进并战斗，目标是摧毁对方的城堡。

## 核心玩法

- **双方对战**: 1v1在线对战
- **自动生产**: 建筑自动生产单位
- **自动战斗**: 单位自动前进并攻击敌人
- **资源管理**: 金币自动增长，用于建造建筑
- **策略对抗**: 兵种相克、建筑搭配策略

## 技术栈

- **游戏引擎**: Unity 2021.3 LTS 或更高版本
- **网络框架**: Unity Netcode for GameObjects
- **编程语言**: C# 9.0+
- **目标平台**: iOS / Android

## 项目结构

```
CastleWarsGame/
├── Assets/
│   ├── Scripts/
│   │   ├── Core/              # 核心管理器
│   │   │   ├── GameManager.cs          # 游戏流程管理
│   │   │   ├── CastleController.cs     # 城堡控制器
│   │   │   └── NetworkPlayer.cs        # 网络玩家
│   │   ├── Buildings/         # 建筑系统
│   │   │   └── BuildingBase.cs         # 建筑基类
│   │   ├── Units/             # 单位系统
│   │   │   ├── UnitBase.cs             # 单位基类
│   │   │   ├── UnitMovement.cs         # 移动控制
│   │   │   └── UnitCombat.cs           # 战斗控制
│   │   ├── Economy/           # 经济系统
│   │   │   ├── PlayerEconomy.cs        # 玩家经济
│   │   │   └── BuildingManager.cs      # 建筑管理
│   │   ├── Combat/            # 战斗系统
│   │   │   └── ProjectileController.cs # 投射物
│   │   ├── UI/                # UI系统
│   │   │   ├── BuildingUI.cs           # 建筑UI
│   │   │   ├── ResourceDisplay.cs      # 资源显示
│   │   │   ├── TouchInputHandler.cs    # 触摸输入
│   │   │   └── BuildingSlot.cs         # 建筑槽位
│   │   └── Data/              # 数据定义
│   │       ├── UnitData.cs             # 单位数据
│   │       └── BuildingData.cs         # 建筑数据
│   ├── Prefabs/               # 预制体
│   ├── ScriptableObjects/     # ScriptableObject资源
│   └── Scenes/                # 场景
└── README.md
```

## 核心系统说明

### 1. 单位系统 (Units)

**UnitBase.cs** - 单位基类
- 管理单位生命值、所属玩家
- 状态机控制（移动、战斗、死亡）
- 自动寻找敌人并攻击
- 网络同步

**UnitMovement.cs** - 移动控制
- 自动向对方城堡移动
- 支持地面和空中单位
- 停止/恢复移动功能

**UnitCombat.cs** - 战斗控制
- 近战/远程攻击
- AOE范围伤害
- 敌人搜索和目标锁定

### 2. 建筑系统 (Buildings)

**BuildingBase.cs** - 建筑基类
- 自动生产单位
- 对象池管理优化性能
- 生产间隔控制

**BuildingManager.cs** - 建筑管理
- 建筑建造逻辑
- 前置条件检查
- 建筑槽位管理

### 3. 经济系统 (Economy)

**PlayerEconomy.cs** - 玩家经济
- 金币管理
- 自动收入系统
- 收入递增机制

### 4. 战斗系统 (Combat)

**ProjectileController.cs** - 投射物
- 追踪目标
- 伤害计算
- AOE伤害

**CastleController.cs** - 城堡
- 城堡血量管理
- 胜负判定

### 5. 网络系统 (Network)

**GameManager.cs** - 游戏管理
- 游戏状态管理
- 玩家连接/断线处理
- 游戏开始/结束逻辑

**NetworkPlayer.cs** - 网络玩家
- 玩家信息同步
- 组件初始化

### 6. UI系统 (UI)

**BuildingUI.cs** - 建筑界面
- 显示可建造建筑
- 建筑选择和建造

**ResourceDisplay.cs** - 资源显示
- 实时显示金币和收入

**TouchInputHandler.cs** - 触摸控制
- 相机拖动和缩放
- 建筑槽位点击检测

## 数据配置 (ScriptableObjects)

### UnitData
定义单位属性：
- 生命值、移动速度
- 攻击力、攻击范围、攻击速度
- 单位类型（地面/空中）
- 攻击类型（近战/远程/魔法）
- 护甲类型

### BuildingData
定义建筑属性：
- 建造成本和时间
- 生产的单位类型
- 生产间隔
- 前置建筑要求

## 开发指南

### 必需依赖

1. **Unity Netcode for GameObjects**
   ```
   Window > Package Manager > Unity Registry
   搜索 "Netcode for GameObjects" 并安装
   ```

2. **TextMeshPro**
   ```
   Unity内置，首次使用时会提示导入
   ```

### 创建游戏内容

#### 1. 创建单位

1. 在Unity中创建3D模型/Sprite
2. 创建ScriptableObject: `Assets/Create/CastleWars/Unit Data`
3. 配置单位属性
4. 创建Prefab并添加组件：
   - UnitBase
   - UnitMovement
   - UnitCombat
   - NetworkObject
   - Rigidbody
   - Collider

#### 2. 创建建筑

1. 创建建筑模型
2. 创建ScriptableObject: `Assets/Create/CastleWars/Building Data`
3. 配置建筑属性和生产单位
4. 创建Prefab并添加组件：
   - BuildingBase
   - NetworkObject

#### 3. 设置场景

1. 创建GameManager GameObject
2. 添加NetworkManager组件
3. 设置玩家Prefab和城堡Prefab
4. 放置建筑槽位(BuildingSlot)
5. 设置UI Canvas

### 移动端优化建议

1. **使用对象池**: 已在BuildingBase中实现
2. **限制粒子效果**: 移动端粒子数量<100
3. **纹理压缩**: 使用ASTC格式
4. **减少Draw Calls**: 使用Sprite Atlas
5. **简化碰撞体**: 使用Box/Sphere Collider

### 网络测试

1. **本地测试**:
   - Build & Run创建一个实例
   - Editor中Play创建另一个实例
   - 两者连接同一NetworkManager

2. **真机测试**:
   - 需要部署Dedicated Server
   - 或使用Unity Relay服务

## 扩展功能建议

### 短期扩展
- [ ] AI对手（单机模式）
- [ ] 更多单位类型（至少8种）
- [ ] 更多建筑类型（至少12种）
- [ ] 音效和背景音乐
- [ ] 粒子特效系统

### 中期扩展
- [ ] 技能系统（玩家主动技能）
- [ ] 等级系统和解锁机制
- [ ] 多种游戏模式
- [ ] 排行榜系统
- [ ] 回放系统

### 长期扩展
- [ ] 支持2v2多人对战
- [ ] 地图编辑器
- [ ] 自定义兵种
- [ ] 赛季系统
- [ ] 社交系统

## 性能目标

- **帧率**: 移动端稳定30-60 FPS
- **内存**: <500MB（中端设备）
- **包体**: <200MB
- **网络延迟**: <100ms响应时间

## 平衡性建议

### 兵种克制关系
```
步兵 > 骑兵 > 弓箭手 > 步兵
空军 > 地面远程
防空 > 空军
魔法 > 重甲
```

### 经济平衡
- 基础收入: 10金币/秒
- 收入增长: +1金币/秒
- 初始金币: 500
- 建筑成本: 100-1000（T1-T3）

## 许可证

MIT License

## 联系方式

项目作者: [Your Name]
项目仓库: [GitHub URL]

---

**祝开发顺利！** 🎮
