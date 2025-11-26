using UnityEngine;

namespace CastleWars.Core
{
    /// <summary>
    /// 质量设置管理器 - 根据设备性能自动调整
    /// </summary>
    public class QualitySettingsManager : MonoBehaviour
    {
        [Header("调试")]
        [SerializeField] private bool forceQualityLevel = false;
        [SerializeField] private int forcedLevel = 1;

        private void Start()
        {
            if (forceQualityLevel)
            {
                ApplyQualityLevel(forcedLevel);
            }
            else
            {
                ApplyMobileOptimizations();
            }

            LogDeviceInfo();
        }

        private void ApplyMobileOptimizations()
        {
            // 根据设备性能自动调整
            int deviceTier = GetDeviceTier();

            ApplyQualityLevel(deviceTier);
        }

        private void ApplyQualityLevel(int tier)
        {
            switch (tier)
            {
                case 0: // 低端设备
                    ApplyLowQuality();
                    break;

                case 1: // 中端设备
                    ApplyMediumQuality();
                    break;

                case 2: // 高端设备
                    ApplyHighQuality();
                    break;
            }

            Debug.Log($"Quality Level Applied: {tier} ({GetTierName(tier)})");
        }

        private void ApplyLowQuality()
        {
            QualitySettings.SetQualityLevel(0, true);
            Application.targetFrameRate = 30;

            // 图形设置
            QualitySettings.shadows = ShadowQuality.Disable;
            QualitySettings.shadowResolution = ShadowResolution.Low;
            QualitySettings.particleRaycastBudget = 64;
            QualitySettings.softParticles = false;
            QualitySettings.realtimeReflectionProbes = false;
            QualitySettings.billboardsFaceCameraPosition = false;

            // 抗锯齿
            QualitySettings.antiAliasing = 0;

            // LOD
            QualitySettings.maximumLODLevel = 2;
            QualitySettings.lodBias = 0.5f;

            Debug.Log("Applied Low Quality Settings - Target: 30 FPS");
        }

        private void ApplyMediumQuality()
        {
            QualitySettings.SetQualityLevel(1, true);
            Application.targetFrameRate = 45;

            // 图形设置
            QualitySettings.shadows = ShadowQuality.HardOnly;
            QualitySettings.shadowResolution = ShadowResolution.Medium;
            QualitySettings.particleRaycastBudget = 256;
            QualitySettings.softParticles = false;
            QualitySettings.realtimeReflectionProbes = false;

            // 抗锯齿
            QualitySettings.antiAliasing = 2;

            // LOD
            QualitySettings.maximumLODLevel = 1;
            QualitySettings.lodBias = 1.0f;

            Debug.Log("Applied Medium Quality Settings - Target: 45 FPS");
        }

        private void ApplyHighQuality()
        {
            QualitySettings.SetQualityLevel(2, true);
            Application.targetFrameRate = 60;

            // 图形设置
            QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.shadowResolution = ShadowResolution.High;
            QualitySettings.particleRaycastBudget = 512;
            QualitySettings.softParticles = true;
            QualitySettings.realtimeReflectionProbes = true;

            // 抗锯齿
            QualitySettings.antiAliasing = 4;

            // LOD
            QualitySettings.maximumLODLevel = 0;
            QualitySettings.lodBias = 1.5f;

            Debug.Log("Applied High Quality Settings - Target: 60 FPS");
        }

        private int GetDeviceTier()
        {
            // 基于系统信息的设备分级
            int memoryMB = SystemInfo.systemMemorySize;
            int processorCount = SystemInfo.processorCount;
            int graphicsMemoryMB = SystemInfo.graphicsMemorySize;

            // 计算得分
            int score = 0;

            // 内存评分
            if (memoryMB >= 6144) score += 3;
            else if (memoryMB >= 4096) score += 2;
            else if (memoryMB >= 2048) score += 1;

            // CPU评分
            if (processorCount >= 8) score += 3;
            else if (processorCount >= 6) score += 2;
            else if (processorCount >= 4) score += 1;

            // 显存评分
            if (graphicsMemoryMB >= 2048) score += 2;
            else if (graphicsMemoryMB >= 1024) score += 1;

            // 根据总分确定等级
            if (score >= 6) return 2; // 高端
            if (score >= 3) return 1; // 中端
            return 0; // 低端
        }

        private string GetTierName(int tier)
        {
            switch (tier)
            {
                case 0: return "Low";
                case 1: return "Medium";
                case 2: return "High";
                default: return "Unknown";
            }
        }

        private void LogDeviceInfo()
        {
            Debug.Log("=== Device Information ===");
            Debug.Log($"Device Model: {SystemInfo.deviceModel}");
            Debug.Log($"Device Type: {SystemInfo.deviceType}");
            Debug.Log($"Operating System: {SystemInfo.operatingSystem}");
            Debug.Log($"Processor Type: {SystemInfo.processorType}");
            Debug.Log($"Processor Count: {SystemInfo.processorCount}");
            Debug.Log($"System Memory: {SystemInfo.systemMemorySize} MB");
            Debug.Log($"Graphics Device: {SystemInfo.graphicsDeviceName}");
            Debug.Log($"Graphics Memory: {SystemInfo.graphicsMemorySize} MB");
            Debug.Log($"Graphics API: {SystemInfo.graphicsDeviceType}");
            Debug.Log($"Max Texture Size: {SystemInfo.maxTextureSize}");
            Debug.Log($"Supports Compute Shaders: {SystemInfo.supportsComputeShaders}");
            Debug.Log("========================");
        }

        // 运行时切换质量等级（用于设置菜单）
        public void SetQualityLevel(int level)
        {
            if (level < 0 || level > 2)
            {
                Debug.LogWarning($"Invalid quality level: {level}");
                return;
            }

            ApplyQualityLevel(level);
            PlayerPrefs.SetInt("QualityLevel", level);
            PlayerPrefs.Save();
        }

        // 从玩家设置加载质量等级
        private void LoadSavedQuality()
        {
            if (PlayerPrefs.HasKey("QualityLevel"))
            {
                int savedLevel = PlayerPrefs.GetInt("QualityLevel");
                ApplyQualityLevel(savedLevel);
            }
        }
    }
}
