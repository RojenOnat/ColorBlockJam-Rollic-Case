using ColorBlockJam.Gameplay;
using ColorBlockJam.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ColorBlockJam.Editor
{
    /// <summary>Validates and repairs the authored Gameplay scene required by the case submission.</summary>
    public static class GameplaySceneConfigurator
    {
        private const string GameplayScenePath = "Assets/ColorBlockJam/Scenes/Gameplay.unity";
        private const string UiRoot = "Assets/Game Developer Case Assets/UI/";
        private const float SettingsTogglePixelsPerUnitMultiplier = 1.5f;
        private const float PauseActionPixelsPerUnitMultiplier = 3f;

        [MenuItem("Color Block Jam/Prepare Gameplay Scene for Submission")]
        public static void Prepare()
        {
            Scene scene = EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Single);
            Canvas canvas = Object.FindObjectOfType<Canvas>();
            GameFlowController gameFlow = Object.FindObjectOfType<GameFlowController>();
            if (canvas == null || gameFlow == null)
            {
                Debug.LogError("Gameplay scene needs a Canvas and GameFlowController before it can be prepared.");
                return;
            }

            EnsureInputSystemEventSystem();
            BuildSettings(canvas.transform);
            BuildPause(canvas.transform, gameFlow);
            ConfigureButtonFeedback(canvas);
            ConfigureCanvasScaler(canvas);
            ConfigureBuildSettings();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Gameplay scene prepared: settings, pause, button feedback, and build settings are ready.");
        }

        private static void BuildSettings(Transform canvas)
        {
            Transform home = FindDescendant(canvas, "MainMenuPanel");
            Button settingsButton = FindDescendant(home, "SettingsButton")?.GetComponent<Button>();
            if (settingsButton == null) return;
            settingsButton.gameObject.SetActive(true);

            Transform existingPanel = FindDescendant(canvas, "SettingsPanel");
            if (existingPanel != null)
            {
                Button existingClose = FindDescendant(existingPanel, "CloseButton")?.GetComponent<Button>();
                SettingsPanelController existingController = canvas.GetComponent<SettingsPanelController>();
                if (existingController != null && existingClose != null)
                {
                    existingController.Configure(existingPanel.gameObject, settingsButton, existingClose);
                    EditorUtility.SetDirty(existingController);
                }
                return;
            }

            RectTransform overlay = CreateRect("SettingsPanel", canvas);
            Stretch(overlay);
            Image dimmer = overlay.gameObject.AddComponent<Image>();
            dimmer.color = new Color(0f, 0.05f, 0.18f, 0.72f);

            RectTransform popup = CreateImage("Popup", overlay, "bg_popup", new Vector2(820f, 920f), Vector2.zero,
                true).rectTransform;
            CreateImage("Header", popup, "bg_popup_header", new Vector2(690f, 175f), new Vector2(0f, 360f), true);
            CreateText("Title", popup, "SETTINGS", 54, new Vector2(520f, 95f), new Vector2(0f, 370f));
            Button close = CreateButton("CloseButton", popup, "Popup_Close_Button", new Vector2(105f, 105f),
                new Vector2(325f, 365f));

            Sprite toggleSprite = Sprite("btn_toggle_on");
            CreateSettingRow(popup, "Sound", "Blockjam_Settings_Sound", SettingKind.Sound, 190f, toggleSprite);
            CreateSettingRow(popup, "Music", "Blockjam_Settings_Music", SettingKind.Music, 0f, toggleSprite);
            CreateSettingRow(popup, "Vibration", "Blockjam_Settings_Vibration", SettingKind.Vibration, -190f,
                toggleSprite);

            SettingsPanelController controller = canvas.GetComponent<SettingsPanelController>();
            if (controller == null) controller = canvas.gameObject.AddComponent<SettingsPanelController>();
            controller.Configure(overlay.gameObject, settingsButton, close);
            EditorUtility.SetDirty(controller);
            overlay.gameObject.SetActive(false);
        }

        private static void CreateSettingRow(Transform parent, string label, string iconName, SettingKind kind,
            float y, Sprite toggleSprite)
        {
            RectTransform row = CreateRect(label + "Row", parent);
            SetCenter(row, new Vector2(650f, 150f), new Vector2(0f, y));
            CreateImage("Icon", row, iconName, new Vector2(115f, 115f), new Vector2(-245f, 0f), false);
            CreateText("Label", row, label.ToUpperInvariant(), 42, new Vector2(300f, 80f), new Vector2(-35f, 0f));
            Button toggle = CreateButton("Toggle", row, "btn_toggle_on", new Vector2(125f, 125f),
                new Vector2(245f, 0f));
            toggle.image.pixelsPerUnitMultiplier = SettingsTogglePixelsPerUnitMultiplier;
            SettingsToggleView view = toggle.gameObject.AddComponent<SettingsToggleView>();
            view.Configure(kind, toggle, toggle.image, toggleSprite, null);
            EditorUtility.SetDirty(view);
        }

        private static void BuildPause(Transform canvas, GameFlowController gameFlow)
        {
            Transform gameplayHud = FindDescendant(canvas, "GameplayHUD");
            Transform header = FindDescendant(gameplayHud, "HeaderBar");
            if (header == null) return;

            Button existingPause = FindDescendant(header, "PauseButton")?.GetComponent<Button>();
            Transform existingPanel = FindDescendant(canvas, "PausePanel");
            if (existingPause != null && existingPanel != null)
            {
                Button existingResume = FindDescendant(existingPanel, "ResumeButton")?.GetComponent<Button>();
                Button existingRestart = FindDescendant(existingPanel, "RestartButton")?.GetComponent<Button>();
                Button existingHome = FindDescendant(existingPanel, "HomeButton")?.GetComponent<Button>();
                PausePanelController existingController = canvas.GetComponent<PausePanelController>();
                if (existingController != null && existingResume != null && existingRestart != null &&
                    existingHome != null)
                {
                    existingController.Configure(gameFlow, existingPanel.gameObject, existingPause, existingResume,
                        existingRestart, existingHome);
                    EditorUtility.SetDirty(existingController);
                }
                return;
            }

            Button pauseButton = CreateButton("PauseButton", header, "Btn_circle_blue_small", new Vector2(112f, 112f),
                new Vector2(210f, 0f));
            CreateImage("Icon", pauseButton.transform, "ic_pause", new Vector2(62f, 62f), Vector2.zero, false);

            RectTransform overlay = CreateRect("PausePanel", canvas);
            Stretch(overlay);
            Image dimmer = overlay.gameObject.AddComponent<Image>();
            dimmer.color = new Color(0f, 0.05f, 0.18f, 0.72f);

            RectTransform popup = CreateImage("Popup", overlay, "bg_popup", new Vector2(780f, 720f), Vector2.zero,
                true).rectTransform;
            CreateImage("Header", popup, "bg_popup_header", new Vector2(650f, 165f), new Vector2(0f, 265f), true);
            CreateText("Title", popup, "PAUSED", 56, new Vector2(500f, 90f), new Vector2(0f, 275f));
            Button resume = CreateLabeledButton("ResumeButton", popup, "Btn_popup_green", "RESUME",
                new Vector2(420f, 110f), new Vector2(0f, 80f));
            Button restart = CreateLabeledButton("RestartButton", popup, "Btn_popup_blue", "RESTART",
                new Vector2(420f, 110f), new Vector2(0f, -80f));
            Button home = CreateLabeledButton("HomeButton", popup, "Btn_popup_blue", "HOME",
                new Vector2(420f, 110f), new Vector2(0f, -240f));
            resume.image.pixelsPerUnitMultiplier = PauseActionPixelsPerUnitMultiplier;
            restart.image.pixelsPerUnitMultiplier = PauseActionPixelsPerUnitMultiplier;
            home.image.pixelsPerUnitMultiplier = PauseActionPixelsPerUnitMultiplier;

            PausePanelController controller = canvas.GetComponent<PausePanelController>();
            if (controller == null) controller = canvas.gameObject.AddComponent<PausePanelController>();
            controller.Configure(gameFlow, overlay.gameObject, pauseButton, resume, restart, home);
            EditorUtility.SetDirty(controller);
            overlay.gameObject.SetActive(false);
        }

        private static void ConfigureButtonFeedback(Canvas canvas)
        {
            foreach (Button button in canvas.GetComponentsInChildren<Button>(true))
            {
                button.transition = Selectable.Transition.ColorTint;
                ColorBlock colors = button.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(1f, 1f, 1f, 0.92f);
                colors.pressedColor = new Color(0.72f, 0.72f, 0.72f, 1f);
                colors.selectedColor = Color.white;
                colors.fadeDuration = 0.08f;
                button.colors = colors;
                EditorUtility.SetDirty(button);
            }
        }

        private static void ConfigureCanvasScaler(Canvas canvas)
        {
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null) return;
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            EditorUtility.SetDirty(scaler);
        }

        private static void EnsureInputSystemEventSystem()
        {
            EventSystem eventSystem = Object.FindObjectOfType<EventSystem>();
            if (eventSystem == null) return;
            StandaloneInputModule legacy = eventSystem.GetComponent<StandaloneInputModule>();
            if (legacy != null) Object.DestroyImmediate(legacy);

            System.Type inputModuleType = System.Type.GetType(
                "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputModuleType != null && eventSystem.GetComponent(inputModuleType) == null)
                eventSystem.gameObject.AddComponent(inputModuleType);
        }

        private static void ConfigureBuildSettings()
        {
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(GameplayScenePath, true) };
        }

        private static Button CreateLabeledButton(string name, Transform parent, string spriteName, string label,
            Vector2 size, Vector2 position)
        {
            Button button = CreateButton(name, parent, spriteName, size, position);
            CreateText("Label", button.transform, label, 42, size - new Vector2(40f, 30f), Vector2.zero);
            return button;
        }

        private static Button CreateButton(string name, Transform parent, string spriteName, Vector2 size,
            Vector2 position)
        {
            Image image = CreateImage(name, parent, spriteName, size, position, true);
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            return button;
        }

        private static Image CreateImage(string name, Transform parent, string spriteName, Vector2 size,
            Vector2 position, bool sliced)
        {
            RectTransform rect = CreateRect(name, parent);
            SetCenter(rect, size, position);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = Sprite(spriteName);
            image.preserveAspect = !sliced;
            image.type = sliced && image.sprite != null && image.sprite.border.sqrMagnitude > 0f
                ? Image.Type.Sliced
                : Image.Type.Simple;
            return image;
        }

        private static Text CreateText(string name, Transform parent, string value, int size, Vector2 dimensions,
            Vector2 position)
        {
            RectTransform rect = CreateRect(name, parent);
            SetCenter(rect, dimensions, position);
            Text text = rect.gameObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = size;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            Shadow shadow = rect.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.04f, 0.1f, 0.34f, 0.9f);
            shadow.effectDistance = new Vector2(2f, -3f);
            return text;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var item = new GameObject(name, typeof(RectTransform));
            item.transform.SetParent(parent, false);
            return (RectTransform)item.transform;
        }

        private static void SetCenter(RectTransform rect, Vector2 size, Vector2 position)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static Transform FindDescendant(Transform root, string name)
        {
            if (root == null) return null;
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                if (child.name == name) return child;
            return null;
        }

        private static Sprite Sprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(UiRoot + name + ".png");
    }
}
