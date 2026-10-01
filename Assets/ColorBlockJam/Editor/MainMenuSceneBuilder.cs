using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ColorBlockJam.Editor
{
    /// <summary>Creates the editable MainMenuPanel preview from the supplied UI sprite library.</summary>
    internal static class MainMenuSceneBuilder
    {
        private const string BuildKey = "ColorBlockJam.MainMenuSceneBuilder.v3";
        private const string UiPath = "Assets/Game Developer Case Assets/UI/";
        private static int pendingBuildFrames;

        [InitializeOnLoadMethod]
        private static void BuildAfterCompilation()
        {
            if (SessionState.GetBool(BuildKey, false)) return;
            SessionState.SetBool(BuildKey, true);
            pendingBuildFrames = 60;
            EditorApplication.update += BuildWhenSceneIsReady;
        }

        private static void BuildWhenSceneIsReady()
        {
            if (GameObject.Find("Canvas") != null && SceneManager.GetActiveScene().name == "Gameplay")
            {
                EditorApplication.update -= BuildWhenSceneIsReady;
                Build();
                return;
            }

            pendingBuildFrames--;
            if (pendingBuildFrames <= 0) EditorApplication.update -= BuildWhenSceneIsReady;
        }

        [MenuItem("Color Block Jam/Build Main Menu Panel")]
        private static void Build()
        {
            if (SceneManager.GetActiveScene().name != "Gameplay") return;

            var canvas = GameObject.Find("Canvas")?.transform;
            if (canvas == null) return;

            ConfigureNineSlices();

            var existing = canvas.Find("MainMenuPanel");
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            var panel = Create("MainMenuPanel", canvas, Vector2.zero, new Vector2(1080f, 1920f));
            panel.SetAsLastSibling();
            AddImage(panel, "Background", "bg_home_screen", Vector2.zero, new Vector2(1080f, 1920f), false);

            BuildTopBar(panel);
            BuildLevelPath(panel);
            BuildBottomNavigation(panel);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            Selection.activeGameObject = panel.gameObject;
        }

        private static void BuildTopBar(Transform panel)
        {
            var topBar = Create("TopBar", panel, new Vector2(0f, 700f), new Vector2(980f, 190f));

            var profile = AddButton(topBar, "ProfileButton", "profile_box_blue", new Vector2(-395f, 0f), new Vector2(170f, 170f));
            AddImage(profile, "AvatarPlaceholder", "Lego_Brickv1", Vector2.zero, new Vector2(110f, 110f), true);

            var lives = AddImage(topBar, "LivesPanel", "bg_home_resource_holder", new Vector2(-145f, 0f), new Vector2(330f, 100f), false, true);
            AddText(lives, "LivesLabel", "Full", new Vector2(-35f, 0f), new Vector2(150f, 70f), 48, new Color(0.07f, 0.06f, 0.35f));
            AddImage(lives, "HeartIcon", "Heart", new Vector2(95f, 0f), new Vector2(88f, 88f), true);
            AddText(lives, "LivesValue", "5", new Vector2(138f, 12f), new Vector2(64f, 64f), 48, Color.white);
            AddButton(lives, "AddLivesButton", "Btn_circle_green_small", new Vector2(-160f, 0f), new Vector2(72f, 72f));

            var coins = AddImage(topBar, "CoinsPanel", "bg_home_coin_holder", new Vector2(230f, 0f), new Vector2(300f, 100f), false, true);
            AddButton(coins, "AddCoinsButton", "Btn_circle_green_small", new Vector2(-145f, 0f), new Vector2(72f, 72f));
            AddText(coins, "CoinAmount", "910", new Vector2(-10f, 0f), new Vector2(135f, 70f), 48, new Color(0.07f, 0.06f, 0.35f));
            AddImage(coins, "CoinIcon", "Coin_Single", new Vector2(105f, 0f), new Vector2(105f, 105f), true);
            var settings = AddButton(topBar, "SettingsButton", "Btn_settings_normal", new Vector2(430f, 0f), new Vector2(115f, 115f));
            AddImage(settings, "SettingsIcon", "Settings_Icon", Vector2.zero, new Vector2(72f, 72f), true);
        }

        private static void BuildLevelPath(Transform panel)
        {
            var levelPath = Create("LevelPath", panel, new Vector2(0f, 45f), new Vector2(700f, 1300f));
            AddImage(levelPath, "PathLine", "bg_home_line", new Vector2(0f, 90f), new Vector2(120f, 1170f), false);

            var futureLevel = AddButton(levelPath, "LockedLevel37", "Btn_levelBg_normal", new Vector2(0f, 440f), new Vector2(250f, 250f), true);
            AddText(futureLevel, "LevelNumber", "37", new Vector2(0f, 0f), new Vector2(150f, 100f), 76, Color.white);
            AddImage(futureLevel, "LockIcon", "lock", new Vector2(-92f, -80f), new Vector2(100f, 100f), true);

            var nextLevel = AddButton(levelPath, "LockedLevel36", "Btn_levelBg_normal", new Vector2(0f, 50f), new Vector2(250f, 250f), true);
            AddText(nextLevel, "LevelNumber", "36", new Vector2(0f, 0f), new Vector2(150f, 100f), 76, new Color(0.15f, 0.45f, 0.1f));
            AddImage(nextLevel, "LockIcon", "lock", new Vector2(-92f, -80f), new Vector2(100f, 100f), true);

            var current = AddButton(levelPath, "CurrentLevel35", "Btn_level_green", new Vector2(0f, -350f), new Vector2(280f, 280f), true);
            AddText(current, "LevelNumber", "35", Vector2.zero, new Vector2(180f, 110f), 86, Color.white);

            var play = AddButton(levelPath, "PlayLevelButton", "Btn_level_green", new Vector2(0f, -610f), new Vector2(470f, 150f), true);
            AddText(play, "Label", "Level 35", Vector2.zero, new Vector2(390f, 90f), 62, Color.white);
        }

        private static void BuildBottomNavigation(Transform panel)
        {
            var navigation = Create("BottomNavigation", panel, new Vector2(0f, -825f), new Vector2(1080f, 260f));
            AddImage(navigation, "Background", "bg_menu", Vector2.zero, new Vector2(1080f, 260f), false, true);

            var locked = AddButton(navigation, "LockedTab", "Btn_booster_normal", new Vector2(-420f, 0f), new Vector2(160f, 175f));
            AddImage(locked, "LockIcon", "lock", Vector2.zero, new Vector2(85f, 85f), true);

            var shop = AddButton(navigation, "ShopTab", "Btn_booster_normal", new Vector2(-210f, 0f), new Vector2(160f, 175f));
            AddImage(shop, "ShopIcon", "shop", Vector2.zero, new Vector2(110f, 110f), true);

            var home = AddButton(navigation, "HomeTab", "ActiveTab", Vector2.zero, new Vector2(235f, 270f), true);
            AddImage(home, "HomeIcon", "Lego_Brickv1", new Vector2(0f, 28f), new Vector2(125f, 125f), true);
            AddText(home, "Label", "Home", new Vector2(0f, -75f), new Vector2(180f, 65f), 42, Color.white);

            var collection = AddButton(navigation, "CollectionTab", "Btn_booster_normal", new Vector2(210f, 0f), new Vector2(160f, 175f));
            AddImage(collection, "CollectionIcon", "block_collection", Vector2.zero, new Vector2(110f, 110f), true);

            var lockedLevel = AddButton(navigation, "LockedLevel50Tab", "Btn_booster_normal", new Vector2(420f, 0f), new Vector2(160f, 175f));
            AddImage(lockedLevel, "LockIcon", "lock", new Vector2(0f, 22f), new Vector2(80f, 80f), true);
            AddText(lockedLevel, "Label", "Lvl 50", new Vector2(0f, -67f), new Vector2(145f, 50f), 32, Color.white);
        }

        private static Transform AddButton(Transform parent, string name, string spriteName, Vector2 position, Vector2 size, bool sliced = false)
        {
            var target = AddImage(parent, name, spriteName, position, size, !sliced, sliced);
            target.gameObject.AddComponent<Button>();
            return target;
        }

        private static Transform AddImage(Transform parent, string name, string spriteName, Vector2 position, Vector2 size, bool preserveAspect, bool sliced = false)
        {
            var target = Create(name, parent, position, size);
            var image = target.gameObject.AddComponent<Image>();
            image.sprite = Sprite(spriteName);
            image.preserveAspect = preserveAspect;
            image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            return target;
        }

        private static void AddText(Transform parent, string name, string value, Vector2 position, Vector2 size, int fontSize, Color color)
        {
            var target = Create(name, parent, position, size);
            var text = target.gameObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
        }

        private static Transform Create(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var item = new GameObject(name, typeof(RectTransform));
            item.transform.SetParent(parent, false);
            var rect = (RectTransform)item.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private static Sprite Sprite(string name)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(UiPath + name + ".png");
        }

        private static void ConfigureNineSlices()
        {
            SetBorder("bg_home_resource_holder", 22f);
            SetBorder("bg_home_coin_holder", 18f);
            SetBorder("bg_menu", 35f);
            SetBorder("Btn_level_green", 55f);
            SetBorder("Btn_levelBg_normal", 48f);
            SetBorder("ActiveTab", 38f);
        }

        private static void SetBorder(string spriteName, float border)
        {
            var path = UiPath + spriteName + ".png";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;

            var targetBorder = new Vector4(border, border, border, border);
            if (importer.spriteBorder == targetBorder) return;
            importer.spriteBorder = targetBorder;
            importer.SaveAndReimport();
        }
    }
}
