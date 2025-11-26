# 城堡战争 - 快速设置指南

## 第一步：环境准备

### 1. 安装Unity

下载并安装 **Unity 2021.3 LTS** 或更高版本
- [Unity Hub下载地址](https://unity.com/download)
- 安装时确保勾选：
  - Android Build Support (包括 Android SDK & NDK Tools)
  - iOS Build Support (如果是Mac)

### 2. 创建Unity项目

1. 打开Unity Hub
2. 点击"新建项目"
3. 选择 **3D (URP)** 模板（Universal Render Pipeline）
4. 项目名称：CastleWarsGame
5. 点击"创建项目"

## 第二步：安装必需包

### 1. 安装Netcode for GameObjects

```
1. 打开Unity编辑器
2. Window > Package Manager
3. 点击左上角 "+" > Add package from git URL
4. 输入: com.unity.netcode.gameobjects
5. 点击 Add
```

### 2. 导入TextMeshPro

```
1. Window > TextMeshPro > Import TMP Essential Resources
2. 点击 Import
```

## 第三步：导入代码

1. 将提供的Scripts文件夹复制到 `Assets/` 目录下
2. 等待Unity编译完成

## 第四步：创建基础场景

### 1. 创建GamePlay场景

```
1. File > New Scene
2. 选择 Basic (Built-in)
3. 保存为 Assets/Scenes/GamePlay.unity
```

### 2. 设置场景对象

#### 创建GameManager
```
1. 右键 Hierarchy > Create Empty
2. 命名为 "GameManager"
3. Add Component > GameManager (脚本)
4. Add Component > NetworkManager
```

#### 配置NetworkManager
```
在NetworkManager组件中：
1. Transport: 选择 UnityTransport
2. 点击 "Select and generate default NetworkPrefabs"
```

#### 创建相机设置
```
1. 选中Main Camera
2. Position: (0, 15, -10)
3. Rotation: (45, 0, 0)
4. 如果是2D视角，改为Orthographic
```

#### 创建地面
```
1. 右键 Hierarchy > 3D Object > Plane
2. 命名为 "Ground"
3. Scale: (10, 1, 10)
```

#### 创建建筑槽位
```
1. 右键 Hierarchy > 3D Object > Cube
2. 命名为 "BuildingSlot_1"
3. Position: (-10, 0, 0)
4. Scale: (2, 0.1, 2)
5. Add Component > BuildingSlot
6. 设置 Slot Index = 0
7. 复制创建更多槽位（建议每方5个）
```

## 第五步：创建Prefabs

### 1. 创建Player Prefab

```
1. Hierarchy > Create Empty
2. 命名为 "Player"
3. Add Component:
   - NetworkObject
   - NetworkPlayer
   - PlayerEconomy
   - BuildingManager
4. 拖拽到 Assets/Prefabs/ 文件夹
5. 删除场景中的Player
```

### 2. 创建城堡Prefab

```
1. 3D Object > Cube
2. 命名为 "Castle"
3. Scale: (5, 5, 5)
4. Add Component:
   - NetworkObject
   - CastleController
   - Box Collider (Is Trigger = true)
5. 拖拽到 Assets/Prefabs/
6. 删除场景中的Castle
```

### 3. 创建示例单位Prefab

```
1. 3D Object > Capsule
2. 命名为 "BasicSoldier"
3. Add Component:
   - NetworkObject
   - UnitBase
   - UnitMovement
   - UnitCombat
   - Rigidbody (Use Gravity = true)
   - Capsule Collider
4. 拖拽到 Assets/Prefabs/Units/
5. 删除场景中的单位
```

### 4. 创建示例建筑Prefab

```
1. 3D Object > Cube
2. 命名为 "Barracks"
3. Scale: (3, 3, 3)
4. Add Component:
   - NetworkObject
   - BuildingBase
5. 创建子对象作为SpawnPoint:
   - Create Empty child
   - 命名为 "SpawnPoint"
   - Position: (2, 0, 0)
6. 在BuildingBase中拖拽SpawnPoint引用
7. 拖拽到 Assets/Prefabs/Buildings/
8. 删除场景中的建筑
```

## 第六步：创建ScriptableObjects

### 1. 创建单位数据

```
1. Assets/ScriptableObjects/ 创建文件夹
2. 右键 > Create > CastleWars > Unit Data
3. 命名为 "BasicSoldier_Data"
4. 配置属性:
   - Unit Name: "步兵"
   - Max Health: 100
   - Move Speed: 3
   - Attack Damage: 10
   - Attack Range: 1.5
   - Attack Speed: 1
   - Unit Type: Ground
   - Attack Type: Melee
   - Prefab: 拖拽BasicSoldier预制体
```

### 2. 创建建筑数据

```
1. 右键 > Create > CastleWars > Building Data
2. 命名为 "Barracks_Data"
3. 配置属性:
   - Building Name: "兵营"
   - Gold Cost: 200
   - Produced Unit: 选择 BasicSoldier_Data
   - Production Interval: 5
   - Prefab: 拖拽Barracks预制体
```

## 第七步：设置UI

### 1. 创建Canvas

```
1. 右键 Hierarchy > UI > Canvas
2. Canvas Scaler:
   - UI Scale Mode: Scale With Screen Size
   - Reference Resolution: 1920 x 1080
```

### 2. 创建资源显示

```
1. 右键 Canvas > UI > Text - TextMeshPro
2. 命名为 "GoldText"
3. Position: 左上角
4. 再创建一个命名为 "IncomeText"

5. 创建空对象 "ResourceDisplay"
6. Add Component > ResourceDisplay
7. 拖拽Text引用
```

### 3. 创建建筑UI（可选）

```
建议使用Unity UI系统创建建筑选择面板
详见 BuildingUI.cs 的使用说明
```

## 第八步：配置NetworkManager

```
1. 选中GameManager
2. 在NetworkManager组件中:
   - Player Prefab: 拖拽Player预制体

3. 在GameManager脚本中:
   - Player1 Spawn Point: 创建空对象在左侧
   - Player2 Spawn Point: 创建空对象在右侧
   - Player Prefab: 拖拽Player预制体
   - Castle Prefab: 拖拽Castle预制体

4. 在Player预制体的NetworkPlayer中:
   - Castle Prefab: 拖拽Castle预制体

5. 在Player预制体的BuildingManager中:
   - Building Slots: 拖拽所有建筑槽位Transform
```

## 第九步：设置Layers

```
1. Edit > Project Settings > Tags and Layers
2. 添加新Layer:
   - Layer 8: Player1Units
   - Layer 9: Player2Units
   - Layer 10: Buildings

3. 设置碰撞矩阵:
   Edit > Project Settings > Physics
   - Player1Units 可以碰撞 Player2Units
   - Player1Units 不碰撞 Player1Units
   - 类似设置 Player2Units
```

## 第十步：测试游戏

### 本地测试

```
1. File > Build Settings
2. 添加GamePlay场景
3. Build And Run

4. 在编辑器中点击Play
5. 在GameManager上:
   - 选择 Start Host（作为服务器+客户端）

6. 在Build的程序中:
   - 选择 Start Client（连接到Host）
```

## 常见问题

### Q: 编译错误怎么办？
A: 确保安装了Netcode for GameObjects包，检查Unity版本是否为2021.3+

### Q: 网络连接失败？
A: 确保两个实例在同一局域网，检查防火墙设置

### Q: 单位不移动？
A: 检查UnitData是否正确配置，Rigidbody是否添加

### Q: 无法建造建筑？
A: 检查BuildingSlot是否正确设置，BuildingManager是否配置槽位引用

## 下一步

完成基础设置后，你可以：
1. 创建更多单位和建筑类型
2. 添加美术资源和特效
3. 实现音效系统
4. 优化移动端性能
5. 添加AI对手

## 获取帮助

- Unity官方文档: https://docs.unity3d.com/
- Netcode文档: https://docs-multiplayer.unity3d.com/
- 项目README.md查看详细说明

---

**完成这些步骤后，你将拥有一个可运行的城堡战争游戏原型！** 🎉
