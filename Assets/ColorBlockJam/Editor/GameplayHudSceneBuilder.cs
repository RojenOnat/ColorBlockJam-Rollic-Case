using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ColorBlockJam.Editor
{
    /// <summary>Creates the editable gameplay HUD scene from the supplied case UI art.</summary>
    public static class GameplayHudSceneBuilder
    {
        private const string ScenePath = "Assets/ColorBlockJam/Scenes/Gameplay.unity";
        private const string UiPath = "Assets/Game Developer Case Assets/UI/";

        [MenuItem("Color Block Jam/Build Gameplay HUD", priority = 20)]
        public static void Build()
        {
            var scene = File.Exists(ScenePath)
                ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single)
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            if (Camera.main == null) CreateCamera();
            if (Object.FindObjectOfType<EventSystem>() == null) CreateEventSystem();

            Canvas canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null) canvas = CreateCanvas();
            ClearExistingHud(canvas.transform);
            RectTransform hud = CreateRect("Gameplay HUD", canvas.transform);
            Stretch(hud);
            CreateBackground(hud);
            CreateTopHud(hud);

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath) ?? "Assets");
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = hud.gameObject;
        }

        private static void ClearExistingHud(Transform canvas)
        {
            Transform existingHud = canvas.Find("Gameplay HUD");
            if (existingHud != null) Object.DestroyImmediate(existingHud.gameObject);

            // This is left only by the first HUD generator version. New HUD backgrounds live under Gameplay HUD.
            Transform legacyBackground = canvas.Find("Background");
            if (legacyBackground != null) Object.DestroyImmediate(legacyBackground.gameObject);
        }

        private static void CreateCamera()
        {
            var cameraObject = new GameObject("Main Camera", typeof(Camera));
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.75f, 0.86f, 1f);
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        }

        private static void CreateEventSystem()
        {
            var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            eventSystem.name = "EventSystem";
        }

        private static Canvas CreateCanvas()
        {
            var canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        private static void CreateBackground(Transform parent)
        {
            Image background = CreateImage("Background", parent, "bg_home_screen.png");
            Stretch(background.rectTransform);
            background.color = Color.white;
        }

        private static void CreateTopHud(RectTransform parent)
        {
            RectTransform topHud = CreateRect("Top HUD", parent);
            SetTopCenter(topHud, new Vector2(1000f, 310f), new Vector2(0f, -42f));

            RectTransform bar = CreateRect("Header Bar", topHud);
            SetCenter(bar, new Vector2(900f, 195f), new Vector2(20f, -54f));
            Image barBackground = CreateImage("Background", bar, "bg_gameplay_time_holder.png");
            SetCenter(barBackground.rectTransform, new Vector2(900f, 195f), Vector2.zero);

            RectTransform level = CreateRect("Level Badge", bar);
            SetCenter(level, new Vector2(238f, 238f), new Vector2(-385f, 4f));
            Image levelBackground = CreateImage("Background", level, "Btn_circle_blue_small.png");
            SetCenter(levelBackground.rectTransform, new Vector2(238f, 238f), Vector2.zero);
            CreateText("Title", level, "LEVEL", 38, FontStyle.Bold, new Vector2(0f, 34f), new Vector2(180f, 54f));
            CreateText("Value", level, "45", 46, FontStyle.Bold, new Vector2(0f, -30f), new Vector2(120f, 64f));

            RectTransform timer = CreateRect("Timer", bar);
            SetCenter(timer, new Vector2(270f, 132f), new Vector2(0f, 0f));
            CreateText("Title", timer, "TIME", 36, FontStyle.Bold, new Vector2(0f, 43f), new Vector2(200f, 45f));
            Image timerBackground = CreateImage("Value Background", timer, "bg_home_coin_holder.png");
            SetCenter(timerBackground.rectTransform, new Vector2(240f, 74f), new Vector2(0f, -24f));
            CreateText("Value", timerBackground.transform, "03:00", 39, FontStyle.Bold, Vector2.zero, new Vector2(220f, 58f));

            Image restart = CreateButton("Restart Button", bar, "Btn_circle_blue_small.png");
            SetCenter(restart.rectTransform, new Vector2(132f, 132f), new Vector2(337f, 0f));
            Image restartIcon = CreateImage("Icon", restart.transform, "ic_restart.png");
            SetCenter(restartIcon.rectTransform, new Vector2(78f, 78f), Vector2.zero);

            RectTransform coinCounter = CreateRect("Coin Counter", topHud);
            SetCenter(coinCounter, new Vector2(330f, 106f), new Vector2(300f, -206f));
            Image coinBackground = CreateImage("Background", coinCounter, "bg_home_coin_holder.png");
            SetCenter(coinBackground.rectTransform, new Vector2(270f, 78f), new Vector2(2f, 0f));
            Image addCoin = CreateButton("Add Coins Button", coinCounter, "Btn_circle_green_small.png");
            SetCenter(addCoin.rectTransform, new Vector2(80f, 80f), new Vector2(-124f, 0f));
            Image plusIcon = CreateImage("Icon", addCoin.transform, "ic_plus.png");
            SetCenter(plusIcon.rectTransform, new Vector2(42f, 42f), Vector2.zero);
            CreateText("Amount", coinCounter, "9.50k", 36, FontStyle.Bold, new Vector2(-3f, 0f), new Vector2(160f, 56f));
            Image coin = CreateImage("Icon", coinCounter, "Coin_Single.png");
            SetCenter(coin.rectTransform, new Vector2(98f, 98f), new Vector2(128f, 0f));
        }

        private static Image CreateButton(string name, Transform parent, string assetName)
        {
            Image image = CreateImage(name, parent, assetName);
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            return image;
        }

        private static Image CreateImage(string name, Transform parent, string assetName)
        {
            var imageObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imageObject.transform.SetParent(parent, false);
            Image image = imageObject.GetComponent<Image>();
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(UiPath + assetName);
            image.preserveAspect = true;
            return image;
        }

        private static Text CreateText(string name, Transform parent, string value, int size, FontStyle style,
            Vector2 position, Vector2 dimensions)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text), typeof(Shadow));
            textObject.transform.SetParent(parent, false);
            Text text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            Shadow shadow = textObject.GetComponent<Shadow>();
            shadow.effectColor = new Color(0.05f, 0.15f, 0.48f, 0.9f);
            shadow.effectDistance = new Vector2(2f, -3f);
            SetCenter(text.rectTransform, dimensions, position);
            return text;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var item = new GameObject(name, typeof(RectTransform));
            item.transform.SetParent(parent, false);
            return item.GetComponent<RectTransform>();
        }

        private static void SetTopCenter(RectTransform transform, Vector2 size, Vector2 position)
        {
            transform.anchorMin = transform.anchorMax = new Vector2(0.5f, 1f);
            transform.pivot = new Vector2(0.5f, 1f);
            transform.sizeDelta = size;
            transform.anchoredPosition = position;
        }

        private static void SetCenter(RectTransform transform, Vector2 size, Vector2 position)
        {
            transform.anchorMin = transform.anchorMax = new Vector2(0.5f, 0.5f);
            transform.pivot = new Vector2(0.5f, 0.5f);
            transform.sizeDelta = size;
            transform.anchoredPosition = position;
        }

        private static void Stretch(RectTransform transform)
        {
            transform.anchorMin = Vector2.zero;
            transform.anchorMax = Vector2.one;
            transform.offsetMin = Vector2.zero;
            transform.offsetMax = Vector2.zero;
        }
    }
}
