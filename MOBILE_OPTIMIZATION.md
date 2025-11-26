## 📱 移动端优化清单

### 性能优化

#### 1. Draw Call优化
- [ ] 使用Sprite Atlas合并UI纹理
- [ ] Static Batching（静态建筑）
- [ ] Dynamic Batching（相同材质单位）
- [ ] 目标: <100 draw calls

#### 2. 内存优化
- [ ] 纹理压缩（ASTC for iOS/Android）
- [ ] 最大纹理尺寸: 1024x1024
- [ ] 对象池（已实现）
- [ ] 及时释放未使用资源
- [ ] 目标: <500MB内存占用

#### 3. CPU优化
- [ ] 减少Update()调用
- [ ] 使用事件代替轮询
- [ ] 空间分区（四叉树）
- [ ] 降低AI更新频率
- [ ] 目标: 单帧<16ms (60fps)

#### 4. GPU优化
- [ ] 简化Shader（使用Mobile着色器）
- [ ] 减少粒子数量（<100个）
- [ ] 限制光源数量（≤2个）
- [ ] 禁用实时阴影或降低质量

#### 5. 网络优化
- [ ] 数据压缩
- [ ] 减少同步频率（20Hz足够）
- [ ] 仅同步必要数据
- [ ] 客户端预测

### 质量设置

```csharp
// 创建文件: Assets/Scripts/Core/QualitySettingsManager.cs

using UnityEngine;

namespace CastleWars.Core
{
    public class QualitySettingsManager : MonoBehaviour
    {
        private void Start()
        {
            ApplyMobileOptimizations();
        }

        private void ApplyMobileOptimizations()
        {
            // 根据设备性能自动调整
            int deviceTier = GetDeviceTier();

            switch (deviceTier)
            {
                case 0: // 低端设备
                    QualitySettings.SetQualityLevel(0);
                    Application.targetFrameRate = 30;
                    break;

                case 1: // 中端设备
                    QualitySettings.SetQualityLevel(1);
                    Application.targetFrameRate = 45;
                    break;

                case 2: // 高端设备
                    QualitySettings.SetQualityLevel(2);
                    Application.targetFrameRate = 60;
                    break;
            }

            // 通用优化
            QualitySettings.vSyncCount = 0;
            QualitySettings.maxQueuedFrames = 2;
        }

        private int GetDeviceTier()
        {
            // 简单的设备分级
            int memoryMB = SystemInfo.systemMemorySize;
            int processorCount = SystemInfo.processorCount;

            if (memoryMB >= 4096 && processorCount >= 6)
                return 2; // 高端

            if (memoryMB >= 2048 && processorCount >= 4)
                return 1; // 中端

            return 0; // 低端
        }
    }
}
```

### 触摸优化

#### 建议的触摸目标大小
- 最小: 44x44 点（iOS标准）
- 推荐: 60x60 点
- 按钮间距: ≥8点

#### 虚拟摇杆（如需要）
```csharp
// 已在TouchInputHandler.cs中实现相机控制
// 如需添加单位手动控制，可参考以下设计
```

### 测试设备建议

#### 必测设备
1. **iOS**:
   - iPhone 8 (低端基准)
   - iPhone 12 (中端)
   - iPhone 15 Pro (高端)

2. **Android**:
   - 骁龙660设备（低端）
   - 骁龙870设备（中端）
   - 骁龙8 Gen2设备（高端）

#### 测试项目
- [ ] 帧率稳定性（30-60fps）
- [ ] 发热控制（<45°C）
- [ ] 电池消耗（<15%/小时）
- [ ] 网络延迟（<100ms）
- [ ] 内存占用
- [ ] 包体大小

### 包体优化

#### 目标大小
- 首包: <150MB
- 完整包: <300MB

#### 优化方法
- [ ] 纹理压缩和尺寸控制
- [ ] 音频压缩（MP3/OGG）
- [ ] AssetBundle分离
- [ ] 代码剥离（Strip Engine Code）
- [ ] IL2CPP编译

### 兼容性

#### 最低系统要求
- **iOS**: iOS 12.0+
- **Android**: Android 7.0+ (API Level 24)
- **RAM**: 2GB+
- **存储**: 500MB可用空间

#### 适配屏幕
- 16:9 (传统)
- 18:9 (全面屏)
- 19.5:9 (刘海屏)
- 20:9 (挖孔屏)

### 热更新准备（可选）

如使用热更新方案（如HybridCLR），需考虑：
- [ ] 脚本热更新
- [ ] 资源热更新
- [ ] 版本管理
- [ ] 更新下载

---

## 💡 关键性能指标 (KPI)

### 必须达成
✅ 帧率: 中端设备稳定30fps+
✅ 内存: <500MB
✅ 加载时间: <5秒进入游戏
✅ 网络延迟: <150ms可玩

### 理想目标
🎯 帧率: 60fps
🎯 内存: <300MB
🎯 加载时间: <3秒
🎯 网络延迟: <80ms

### 监控工具
- Unity Profiler
- Xcode Instruments (iOS)
- Android Profiler
- 自定义性能监控UI
