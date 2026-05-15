using UnityEngine;
using UnityEngine.UI;
using CastleWars.Data;
using CastleWars.UI;

namespace CastleWars.Core
{
    /// <summary>
    /// 场景初始化器 - 通过代码创建完整的游戏场景
    /// 使用[RuntimeInitializeOnLoadMethod]在游戏加载前自动执行，无需挂载到场景中的GameObject
    /// </summary>
    public class SceneInitializer
    {
        // 默认单位数据
        private static UnitData warriorData;
        private static UnitData archerData;
        private static UnitData cavalryData;

        // 默认建筑数据
        private static BuildingData barracksData;
        private static BuildingData archerRangeData;
        private static BuildingData stableData;

        /// <summary>
        /// Unity运行时自动引导入口
        /// 在场景加载之前执行，创建初始GameObject并启动整个游戏
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoInitialize()
        {
            Debug.Log("[SceneInitializer] AutoInitialize triggered - building game scene from code...");

            try { CreateDefaultDataAssets(); }
            catch (System.Exception e) { Debug.LogError($"[SceneInitializer] CreateDefaultDataAssets failed: {e}"); }

            try { CreateCamera(); }
            catch (System.Exception e) { Debug.LogError($"[SceneInitializer] CreateCamera failed: {e}"); }

            try { CreateLighting(); }
            catch (System.Exception e) { Debug.LogError($"[SceneInitializer] CreateLighting failed: {e}"); }

            try { CreateGround(); }
            catch (System.Exception e) { Debug.LogError($"[SceneInitializer] CreateGround failed: {e}"); }

            try { CreateGameLauncher(); }
            catch (System.Exception e) { Debug.LogError($"[SceneInitializer] CreateGameLauncher failed: {e}"); }

            try { CreateLocalGameUI(); }
            catch (System.Exception e) { Debug.LogError($"[SceneInitializer] CreateLocalGameUI failed: {e}"); }

            try { CreateQualitySettingsManager(); }
            catch (System.Exception e) { Debug.LogError($"[SceneInitializer] CreateQualitySettingsManager failed: {e}"); }

            try { CreateTouchInputHandler(); }
            catch (System.Exception e) { Debug.LogError($"[SceneInitializer] CreateTouchInputHandler failed: {e}"); }

            Debug.Log("[SceneInitializer] Game scene built successfully!");
        }

        #region 数据资源创建

        private static void CreateDefaultDataAssets()
        {
            // 战士单位
            warriorData = ScriptableObject.CreateInstance<UnitData>();
            warriorData.unitName = "Warrior";
            warriorData.tier = 1;
            warriorData.maxHealth = 120f;
            warriorData.moveSpeed = 3f;
            warriorData.attackDamage = 12f;
            warriorData.attackSpeed = 1f;
            warriorData.attackRange = 1.5f;
            warriorData.attackType = AttackType.Melee;
            warriorData.armorType = ArmorType.Light;
            warriorData.unitType = UnitType.Ground;
            warriorData.canAttackAir = false;
            warriorData.buildingDamageMultiplier = 1.5f;

            // 弓箭手单位
            archerData = ScriptableObject.CreateInstance<UnitData>();
            archerData.unitName = "Archer";
            archerData.tier = 1;
            archerData.maxHealth = 70f;
            archerData.moveSpeed = 3.5f;
            archerData.attackDamage = 8f;
            archerData.attackSpeed = 1.2f;
            archerData.attackRange = 6f;
            archerData.attackType = AttackType.Ranged;
            archerData.armorType = ArmorType.None;
            archerData.unitType = UnitType.Ground;
            archerData.canAttackAir = true;
            archerData.buildingDamageMultiplier = 0.8f;
            archerData.projectileSpeed = 15f;

            // 骑兵单位
            cavalryData = ScriptableObject.CreateInstance<UnitData>();
            cavalryData.unitName = "Cavalry";
            cavalryData.tier = 2;
            cavalryData.maxHealth = 200f;
            cavalryData.moveSpeed = 5f;
            cavalryData.attackDamage = 18f;
            cavalryData.attackSpeed = 0.8f;
            cavalryData.attackRange = 1.8f;
            cavalryData.attackType = AttackType.Melee;
            cavalryData.armorType = ArmorType.Medium;
            cavalryData.unitType = UnitType.Ground;
            cavalryData.canAttackAir = false;
            cavalryData.buildingDamageMultiplier = 2f;

            // 兵营建筑
            barracksData = ScriptableObject.CreateInstance<BuildingData>();
            barracksData.buildingName = "Barracks";
            barracksData.description = "Produces Warrior units";
            barracksData.tier = BuildingTier.Tier1;
            barracksData.category = BuildingCategory.Infantry;
            barracksData.goldCost = 150;
            barracksData.buildTime = 2f;
            barracksData.producedUnit = warriorData;
            barracksData.productionInterval = 8f;
            barracksData.productionCount = 1;

            // 射手营建筑
            archerRangeData = ScriptableObject.CreateInstance<BuildingData>();
            archerRangeData.buildingName = "Archer Range";
            archerRangeData.description = "Produces Archer units";
            archerRangeData.tier = BuildingTier.Tier1;
            archerRangeData.category = BuildingCategory.Ranged;
            archerRangeData.goldCost = 200;
            archerRangeData.buildTime = 2.5f;
            archerRangeData.producedUnit = archerData;
            archerRangeData.productionInterval = 10f;
            archerRangeData.productionCount = 1;

            // 马厩建筑
            stableData = ScriptableObject.CreateInstance<BuildingData>();
            stableData.buildingName = "Stable";
            stableData.description = "Produces Cavalry units";
            stableData.tier = BuildingTier.Tier2;
            stableData.category = BuildingCategory.Cavalry;
            stableData.goldCost = 400;
            stableData.buildTime = 3f;
            stableData.producedUnit = cavalryData;
            stableData.productionInterval = 12f;
            stableData.productionCount = 1;

            Debug.Log("[SceneInitializer] Default data assets created");
        }

        /// <summary>
        /// 获取默认单位数据
        /// </summary>
        public static UnitData GetDefaultUnitData()
        {
            return warriorData;
        }

        /// <summary>
        /// 获取所有建筑数据
        /// </summary>
        public static BuildingData[] GetAllBuildingData()
        {
            return new BuildingData[] { barracksData, archerRangeData, stableData };
        }

        #endregion

        #region 场景对象创建

        private static void CreateCamera()
        {
            // 检查是否已有主相机
            if (Camera.main != null) return;

            GameObject cameraObj = new GameObject("Main Camera");
            cameraObj.tag = "MainCamera";

            Camera cam = cameraObj.AddComponent<Camera>();
            // 透视投影，呈现经典 RTS 3/4 俯视风格
            cam.orthographic = false;
            cam.fieldOfView = 55f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 200f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.4f, 0.6f, 0.8f, 1f);

            // 约 60° 俯斜角，RTS 3/4 视角
            cameraObj.transform.position = new Vector3(0, 17, -10);
            cameraObj.transform.rotation = Quaternion.Euler(60, 0, 0);

            cameraObj.AddComponent<AudioListener>();

            Debug.Log("[SceneInitializer] Camera created (perspective, 60° tilt RTS view)");
        }

        private static void CreateLighting()
        {
            // 检查是否已有方向光
            if (FindObjectOfType<Light>() != null) return;

            GameObject lightObj = new GameObject("Directional Light");
            Light light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = Color.white;
            light.intensity = 1f;
            lightObj.transform.rotation = Quaternion.Euler(50, -30, 0);

            Debug.Log("[SceneInitializer] Lighting created");
        }

        private static void CreateGround()
        {
            // 检查是否已有地面
            if (GameObject.Find("Ground") != null) return;

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(6, 1, 3);
            ground.transform.position = Vector3.zero;

            Renderer renderer = ground.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material groundMat = new Material(Shader.Find("Standard"));
                groundMat.color = new Color(0.4f, 0.7f, 0.3f);
                renderer.material = groundMat;
            }

            ground.layer = LayerMask.NameToLayer("Default");

            Debug.Log("[SceneInitializer] Ground created");
        }

        private static void CreateGameLauncher()
        {
            // 避免重复创建
            if (FindObjectOfType<GameLauncher>() != null) return;

            GameObject launcherObj = new GameObject("GameLauncher");
            launcherObj.AddComponent<GameLauncher>();

            // 创建LocalUnitSpawner
            if (FindObjectOfType<LocalUnitSpawner>() == null)
            {
                GameObject spawnerObj = new GameObject("LocalUnitSpawner");
                spawnerObj.AddComponent<LocalUnitSpawner>();
            }

            Debug.Log("[SceneInitializer] GameLauncher created");
        }

        private static void CreateLocalGameUI()
        {
            // 避免重复创建
            if (FindObjectOfType<LocalGameUI>() != null) return;

            // Canvas
            GameObject canvasObj = new GameObject("GameCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();

            // LocalGameUI组件
            LocalGameUI gameUI = canvasObj.AddComponent<LocalGameUI>();

            // === 资源显示区域（左上角） ===
            GameObject resourcePanel = CreateUIElement("ResourcePanel", canvasObj.transform);
            RectTransform resourceRect = resourcePanel.GetComponent<RectTransform>();
            resourceRect.anchorMin = new Vector2(0, 1);
            resourceRect.anchorMax = new Vector2(0, 1);
            resourceRect.pivot = new Vector2(0, 1);
            resourceRect.anchoredPosition = new Vector2(10, -10);
            resourceRect.sizeDelta = new Vector2(250, 60);

            // 金币文本
            GameObject goldObj = CreateUIElement("GoldText", resourcePanel.transform);
            Text goldText = goldObj.AddComponent<Text>();
            goldText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            goldText.fontSize = 20;
            goldText.color = Color.yellow;
            goldText.text = "Gold: 500";
            goldText.alignment = TextAnchor.MiddleLeft;
            RectTransform goldRect = goldObj.GetComponent<RectTransform>();
            goldRect.anchorMin = new Vector2(0, 0.5f);
            goldRect.anchorMax = new Vector2(1, 1);
            goldRect.offsetMin = new Vector2(5, 0);
            goldRect.offsetMax = new Vector2(-5, -5);

            // 收入文本
            GameObject incomeObj = CreateUIElement("IncomeText", resourcePanel.transform);
            Text incomeText = incomeObj.AddComponent<Text>();
            incomeText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            incomeText.fontSize = 16;
            incomeText.color = Color.green;
            incomeText.text = "Income: 10/s";
            incomeText.alignment = TextAnchor.MiddleLeft;
            RectTransform incomeRect = incomeObj.GetComponent<RectTransform>();
            incomeRect.anchorMin = new Vector2(0, 0);
            incomeRect.anchorMax = new Vector2(1, 0.5f);
            incomeRect.offsetMin = new Vector2(5, 5);
            incomeRect.offsetMax = new Vector2(-5, 0);

            // === 城堡血量区域（顶部中间） ===
            GameObject healthPanel = CreateUIElement("HealthPanel", canvasObj.transform);
            RectTransform healthRect = healthPanel.GetComponent<RectTransform>();
            healthRect.anchorMin = new Vector2(0.5f, 1);
            healthRect.anchorMax = new Vector2(0.5f, 1);
            healthRect.pivot = new Vector2(0.5f, 1);
            healthRect.anchoredPosition = new Vector2(0, -10);
            healthRect.sizeDelta = new Vector2(400, 50);

            // 玩家1血条
            GameObject p1BarObj = CreateUIElement("Player1HealthBar", healthPanel.transform);
            Slider p1HealthBar = p1BarObj.AddComponent<Slider>();
            RectTransform p1BarRect = p1BarObj.GetComponent<RectTransform>();
            p1BarRect.anchorMin = new Vector2(0, 0.5f);
            p1BarRect.anchorMax = new Vector2(0.5f, 1);
            p1BarRect.offsetMin = new Vector2(5, 2);
            p1BarRect.offsetMax = new Vector2(-5, -2);
            SetupSliderVisuals(p1BarObj, new Color(0.2f, 0.4f, 0.8f));

            // 玩家1血量文本
            GameObject p1TextObj = CreateUIElement("Player1HealthText", healthPanel.transform);
            Text p1HealthText = p1TextObj.AddComponent<Text>();
            p1HealthText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            p1HealthText.fontSize = 12;
            p1HealthText.color = Color.white;
            p1HealthText.alignment = TextAnchor.MiddleCenter;
            RectTransform p1TextRect = p1TextObj.GetComponent<RectTransform>();
            p1TextRect.anchorMin = new Vector2(0, 0);
            p1TextRect.anchorMax = new Vector2(0.5f, 0.5f);
            p1TextRect.offsetMin = new Vector2(5, 2);
            p1TextRect.offsetMax = new Vector2(-5, -2);

            // 玩家2血条
            GameObject p2BarObj = CreateUIElement("Player2HealthBar", healthPanel.transform);
            Slider p2HealthBar = p2BarObj.AddComponent<Slider>();
            RectTransform p2BarRect = p2BarObj.GetComponent<RectTransform>();
            p2BarRect.anchorMin = new Vector2(0.5f, 0.5f);
            p2BarRect.anchorMax = new Vector2(1, 1);
            p2BarRect.offsetMin = new Vector2(5, 2);
            p2BarRect.offsetMax = new Vector2(-5, -2);
            SetupSliderVisuals(p2BarObj, new Color(0.8f, 0.2f, 0.2f));

            // 玩家2血量文本
            GameObject p2TextObj = CreateUIElement("Player2HealthText", healthPanel.transform);
            Text p2HealthText = p2TextObj.AddComponent<Text>();
            p2HealthText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            p2HealthText.fontSize = 12;
            p2HealthText.color = Color.white;
            p2HealthText.alignment = TextAnchor.MiddleCenter;
            RectTransform p2TextRect = p2TextObj.GetComponent<RectTransform>();
            p2TextRect.anchorMin = new Vector2(0.5f, 0);
            p2TextRect.anchorMax = new Vector2(1, 0.5f);
            p2TextRect.offsetMin = new Vector2(5, 2);
            p2TextRect.offsetMax = new Vector2(-5, -2);

            // === 游戏信息（右上角） ===
            GameObject infoPanel = CreateUIElement("InfoPanel", canvasObj.transform);
            RectTransform infoRect = infoPanel.GetComponent<RectTransform>();
            infoRect.anchorMin = new Vector2(1, 1);
            infoRect.anchorMax = new Vector2(1, 1);
            infoRect.pivot = new Vector2(1, 1);
            infoRect.anchoredPosition = new Vector2(-10, -10);
            infoRect.sizeDelta = new Vector2(180, 50);

            // 游戏时间文本
            GameObject timeObj = CreateUIElement("GameTimeText", infoPanel.transform);
            Text gameTimeText = timeObj.AddComponent<Text>();
            gameTimeText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            gameTimeText.fontSize = 18;
            gameTimeText.color = Color.white;
            gameTimeText.alignment = TextAnchor.MiddleRight;
            RectTransform timeRect = timeObj.GetComponent<RectTransform>();
            timeRect.anchorMin = new Vector2(0, 0.5f);
            timeRect.anchorMax = new Vector2(1, 1);
            timeRect.offsetMin = new Vector2(5, 0);
            timeRect.offsetMax = new Vector2(-5, -2);

            // 游戏状态文本
            GameObject stateObj = CreateUIElement("GameStateText", infoPanel.transform);
            Text gameStateText = stateObj.AddComponent<Text>();
            gameStateText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            gameStateText.fontSize = 14;
            gameStateText.color = Color.cyan;
            gameStateText.alignment = TextAnchor.MiddleRight;
            RectTransform stateRect = stateObj.GetComponent<RectTransform>();
            stateRect.anchorMin = new Vector2(0, 0);
            stateRect.anchorMax = new Vector2(1, 0.5f);
            stateRect.offsetMin = new Vector2(5, 2);
            stateRect.offsetMax = new Vector2(-5, 0);

            // === 单位训练按钮（底部左侧） ===
            GameObject trainBtnObj = CreateUIElement("TrainUnitButton", canvasObj.transform);
            trainBtnObj.AddComponent<Image>().color = new Color(0.2f, 0.6f, 0.2f);
            Button trainUnitButton = trainBtnObj.AddComponent<Button>();
            RectTransform trainBtnRect = trainBtnObj.GetComponent<RectTransform>();
            trainBtnRect.anchorMin = new Vector2(0, 0);
            trainBtnRect.anchorMax = new Vector2(0, 0);
            trainBtnRect.pivot = new Vector2(0, 0);
            trainBtnRect.anchoredPosition = new Vector2(10, 10);
            trainBtnRect.sizeDelta = new Vector2(160, 45);

            GameObject btnTextObj = CreateUIElement("ButtonText", trainBtnObj.transform);
            Text btnText = btnTextObj.AddComponent<Text>();
            btnText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            btnText.fontSize = 16;
            btnText.color = Color.white;
            btnText.alignment = TextAnchor.MiddleCenter;
            btnText.text = "Train Unit (100G)";
            RectTransform btnTextRect = btnTextObj.GetComponent<RectTransform>();
            btnTextRect.anchorMin = Vector2.zero;
            btnTextRect.anchorMax = Vector2.one;
            btnTextRect.offsetMin = Vector2.zero;
            btnTextRect.offsetMax = Vector2.zero;

            // === 游戏结束面板 ===
            GameObject gameOverPanel = CreateUIElement("GameOverPanel", canvasObj.transform);
            Image panelBg = gameOverPanel.AddComponent<Image>();
            panelBg.color = new Color(0, 0, 0, 0.8f);
            gameOverPanel.SetActive(false);
            RectTransform gameOverRect = gameOverPanel.GetComponent<RectTransform>();
            gameOverRect.anchorMin = new Vector2(0.2f, 0.3f);
            gameOverRect.anchorMax = new Vector2(0.8f, 0.7f);
            gameOverRect.offsetMin = Vector2.zero;
            gameOverRect.offsetMax = Vector2.zero;

            // 胜利文本
            GameObject winTextObj = CreateUIElement("WinnerText", gameOverPanel.transform);
            Text winnerText = winTextObj.AddComponent<Text>();
            winnerText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            winnerText.fontSize = 32;
            winnerText.color = Color.yellow;
            winnerText.alignment = TextAnchor.MiddleCenter;
            winnerText.text = "Game Over!";
            RectTransform winTextRect = winTextObj.GetComponent<RectTransform>();
            winTextRect.anchorMin = new Vector2(0, 0.4f);
            winTextRect.anchorMax = new Vector2(1, 0.9f);
            winTextRect.offsetMin = Vector2.zero;
            winTextRect.offsetMax = Vector2.zero;

            // 重新开始按钮
            GameObject restartBtnObj = CreateUIElement("RestartButton", gameOverPanel.transform);
            restartBtnObj.AddComponent<Image>().color = new Color(0.2f, 0.5f, 0.8f);
            Button restartButton = restartBtnObj.AddComponent<Button>();
            RectTransform restartBtnRect = restartBtnObj.GetComponent<RectTransform>();
            restartBtnRect.anchorMin = new Vector2(0.3f, 0.1f);
            restartBtnRect.anchorMax = new Vector2(0.7f, 0.35f);
            restartBtnRect.offsetMin = Vector2.zero;
            restartBtnRect.offsetMax = Vector2.zero;

            GameObject restartTextObj = CreateUIElement("RestartText", restartBtnObj.transform);
            Text restartText = restartTextObj.AddComponent<Text>();
            restartText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            restartText.fontSize = 20;
            restartText.color = Color.white;
            restartText.alignment = TextAnchor.MiddleCenter;
            restartText.text = "Restart";
            RectTransform restartTextRect = restartTextObj.GetComponent<RectTransform>();
            restartTextRect.anchorMin = Vector2.zero;
            restartTextRect.anchorMax = Vector2.one;
            restartTextRect.offsetMin = Vector2.zero;
            restartTextRect.offsetMax = Vector2.zero;

            // 通过反射设置LocalGameUI的私有字段
            SetPrivateField(gameUI, "goldText", goldText);
            SetPrivateField(gameUI, "incomeText", incomeText);
            SetPrivateField(gameUI, "player1HealthBar", p1HealthBar);
            SetPrivateField(gameUI, "player2HealthBar", p2HealthBar);
            SetPrivateField(gameUI, "player1HealthText", p1HealthText);
            SetPrivateField(gameUI, "player2HealthText", p2HealthText);
            SetPrivateField(gameUI, "gameTimeText", gameTimeText);
            SetPrivateField(gameUI, "gameStateText", gameStateText);
            SetPrivateField(gameUI, "trainUnitButton", trainUnitButton);
            SetPrivateField(gameUI, "gameOverPanel", gameOverPanel);
            SetPrivateField(gameUI, "winnerText", winnerText);
            SetPrivateField(gameUI, "restartButton", restartButton);

            // 创建EventSystem
            if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            Debug.Log("[SceneInitializer] LocalGameUI created");
        }

        private static void CreateQualitySettingsManager()
        {
            if (FindObjectOfType<QualitySettingsManager>() != null) return;

            GameObject qsmObj = new GameObject("QualitySettingsManager");
            qsmObj.AddComponent<QualitySettingsManager>();

            Debug.Log("[SceneInitializer] QualitySettingsManager created");
        }

        private static void CreateTouchInputHandler()
        {
            if (FindObjectOfType<TouchInputHandler>() != null) return;

            GameObject touchObj = new GameObject("TouchInputHandler");
            touchObj.AddComponent<TouchInputHandler>();

            Debug.Log("[SceneInitializer] TouchInputHandler created");
        }

        #endregion

        #region UI辅助方法

        private static GameObject CreateUIElement(string name, Transform parent)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.AddComponent<RectTransform>();
            return obj;
        }

        private static void SetupSliderVisuals(GameObject sliderObj, Color fillColor)
        {
            Slider slider = sliderObj.GetComponent<Slider>();

            // Background
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(sliderObj.transform, false);
            Image bgImage = bgObj.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            RectTransform bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            // Fill Area（必须主动 AddComponent，plain GameObject 不会自动挂 RectTransform）
            GameObject fillAreaObj = new GameObject("Fill Area");
            fillAreaObj.transform.SetParent(sliderObj.transform, false);
            RectTransform fillAreaRect = fillAreaObj.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = Vector2.zero;
            fillAreaRect.offsetMax = Vector2.zero;

            // Fill
            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(fillAreaObj.transform, false);
            Image fillImage = fillObj.AddComponent<Image>();
            fillImage.color = fillColor;
            RectTransform fillRect = fillObj.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(1, 1);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            slider.targetGraphic = bgImage;
            slider.fillRect = fillRect;
        }

        private static void SetPrivateField(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(obj, value);
            }
            else
            {
                Debug.LogWarning($"[SceneInitializer] Could not find field '{fieldName}' on {obj.GetType().Name}");
            }
        }

        #endregion
    }
}
