using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace VectorSlash.EditorTools
{
    /// <summary>
    /// Builds the HUD (hull, fuel, distance, score, combo, tiles, events) and the title / win / lose
    /// panel on a Canvas, and connects them to GameUI and GameManager.
    /// Run it from the menu: Vector Slash > Build Game UI. Running it again replaces the old UI.
    /// </summary>
    public static class GameUIBuilder
    {
        const string ScenePath = "Assets/Scenes/VectorSlash.unity";
        const string FontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

        static readonly Color LabelColor = new Color(0.75f, 0.82f, 0.95f);
        static readonly Color BarBackColor = new Color(0.06f, 0.08f, 0.16f, 0.9f);
        static readonly Color BarEdgeColor = new Color(0.62f, 0.79f, 1f, 0.35f);
        static readonly Color HullColor = new Color(0.62f, 0.79f, 1f);       // matches the ship
        static readonly Color FuelColor = new Color(0.3f, 0.85f, 0.39f);     // matches the fuel collectors
        static readonly Color ProgressColor = new Color(0.95f, 0.93f, 0.85f);
        static readonly Color ComboColor = new Color(0.26f, 0.91f, 1f);      // matches the energy tiles
        static readonly Color EventColor = new Color(1f, 0.7f, 0.28f);       // matches the fragments
        static readonly Color PanelColor = new Color(0.02f, 0.03f, 0.08f, 0.82f);

        static TMP_FontAsset font;

        [MenuItem("Vector Slash/Build Game UI")]
        public static void BuildFromMenu()
        {
            GameManager game = Object.FindAnyObjectByType<GameManager>();
            if (game == null)
            {
                EditorUtility.DisplayDialog("Build Game UI", "Open the scene with the Game Manager first.", "OK");
                return;
            }
            if (!LoadFont())
            {
                EditorUtility.DisplayDialog("Build Game UI",
                    "TextMesh Pro's essential resources are missing. Import them in the window that opens, then run this again.", "OK");
                EditorApplication.ExecuteMenuItem("Window/TextMeshPro/Import TMP Essential Resources");
                return;
            }

            GameUI existing = Object.FindAnyObjectByType<GameUI>(FindObjectsInactive.Include);
            if (existing != null && !EditorUtility.DisplayDialog("Build Game UI",
                    $"The scene already has a Game UI on \"{existing.name}\". Replace it?", "Replace", "Cancel"))
                return;

            Selection.activeGameObject = Build(game).gameObject;
        }

        /// <summary>For the command line: -executeMethod VectorSlash.EditorTools.GameUIBuilder.BuildInMainScene</summary>
        public static void BuildInMainScene()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath);
            GameManager game = Object.FindAnyObjectByType<GameManager>();
            if (game == null || !LoadFont())
                throw new System.InvalidOperationException("Need a Game Manager in the scene and TMP Essential Resources imported.");
            Build(game);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("GameUIBuilder: built the UI in " + ScenePath);
        }

        static bool LoadFont()
        {
            if (Resources.Load<TMP_Settings>("TMP Settings") != null) font = TMP_Settings.defaultFontAsset;
            if (font == null) font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            return font != null;
        }

        static GameUI Build(GameManager game)
        {
            GameUI existing = Object.FindAnyObjectByType<GameUI>(FindObjectsInactive.Include);
            if (existing != null) RemoveOldUI(existing);

            var canvasObject = new GameObject("Game UI", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(canvasObject, "Build Game UI");
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            var ui = canvasObject.AddComponent<GameUI>();
            Transform root = canvasObject.transform;

            // Top left: hull, fuel and tile count.
            RectTransform status = Rect("Ship Status", root, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(36f, -30f), new Vector2(520f, 140f));
            Label("Hull Label", status, "HULL", new Vector2(0f, 0f), new Vector2(110f, 30f), 24f, TextAlignmentOptions.MidlineLeft);
            ui.healthBar = Bar("Health Bar", status, new Vector2(110f, -3f), new Vector2(380f, 24f), HullColor);
            Label("Fuel Label", status, "FUEL", new Vector2(0f, -44f), new Vector2(110f, 30f), 24f, TextAlignmentOptions.MidlineLeft);
            ui.fuelBar = Bar("Fuel Bar", status, new Vector2(110f, -47f), new Vector2(380f, 24f), FuelColor);
            ui.tilesText = Label("Tiles Text", status, "Tiles 0 / 3", new Vector2(0f, -90f), new Vector2(380f, 32f), 24f, TextAlignmentOptions.MidlineLeft);
            ui.tilesText.color = ComboColor;

            // Top centre: distance to the destination.
            RectTransform distance = Rect("Distance", root, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(640f, 70f));
            Label("Distance Label", distance, "DISTANCE", new Vector2(0f, 0f), new Vector2(640f, 30f), 22f, TextAlignmentOptions.Center);
            ui.progressBar = Bar("Progress Bar", distance, new Vector2(0f, -38f), new Vector2(640f, 16f), ProgressColor);
            ui.progressBar.value = 0f;

            // Top right: score and combo.
            RectTransform scoring = Rect("Scoring", root, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-36f, -24f), new Vector2(460f, 160f));
            ui.scoreText = Label("Score Text", scoring, "Score 0\nBest 0", new Vector2(0f, 0f), new Vector2(460f, 100f), 38f, TextAlignmentOptions.TopRight);
            ui.scoreText.color = Color.white;
            ui.scoreText.fontStyle = FontStyles.Bold;
            ui.comboText = Label("Combo Text", scoring, "", new Vector2(0f, -104f), new Vector2(460f, 44f), 30f, TextAlignmentOptions.TopRight);
            ui.comboText.color = ComboColor;
            ui.comboText.fontStyle = FontStyles.Bold;

            // Short messages ("PERFECT SPLIT!") above the middle of the screen.
            ui.eventText = Label("Event Text", root, "", Vector2.zero, new Vector2(1400f, 80f), 46f, TextAlignmentOptions.Center);
            Place(ui.eventText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 250f));
            ui.eventText.color = EventColor;
            ui.eventText.fontStyle = FontStyles.Bold;

            // Title / win / lose screen, drawn over the HUD.
            RectTransform panel = Rect("Message Panel", root, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            panel.anchorMax = Vector2.one;
            var panelImage = panel.gameObject.AddComponent<Image>();
            panelImage.color = PanelColor;
            panelImage.raycastTarget = false;
            ui.messagePanel = panel.gameObject;
            ui.messageText = Label("Message Text", panel, "VECTOR SLASH", Vector2.zero, new Vector2(1500f, 860f), 34f, TextAlignmentOptions.Center);
            Place(ui.messageText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero);
            ui.messageText.color = Color.white;
            ui.messageText.lineSpacing = 12f;

            Undo.RecordObject(game, "Build Game UI");
            game.ui = ui;
            EditorUtility.SetDirty(game);
            EditorSceneManager.MarkSceneDirty(canvasObject.scene);
            return ui;
        }

        /// <summary>Deletes the old UI's whole Canvas, or only the GameUI component if that Canvas holds anything else important.</summary>
        static void RemoveOldUI(GameUI old)
        {
            Canvas[] canvases = old.GetComponentsInParent<Canvas>(true);
            GameObject top = canvases.Length > 0 ? canvases[canvases.Length - 1].gameObject : null;
            if (top != null && top.GetComponentInChildren<GameManager>(true) == null) Undo.DestroyObjectImmediate(top);
            else Undo.DestroyObjectImmediate(old);
        }

        static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.sizeDelta = size;
            Place(rect, anchor, pivot, position);
            return rect;
        }

        static void Place(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
        }

        static void Stretch(RectTransform rect, float inset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }

        /// <summary>A label placed from the parent's top-left corner.</summary>
        static TextMeshProUGUI Label(string name, Transform parent, string text, Vector2 position, Vector2 size, float fontSize, TextAlignmentOptions alignment)
        {
            RectTransform rect = Rect(name, parent, new Vector2(0f, 1f), new Vector2(0f, 1f), position, size);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = font;
            label.text = text;
            label.fontSize = fontSize;
            label.alignment = alignment;
            label.color = LabelColor;
            label.raycastTarget = false;
            return label;
        }

        /// <summary>A display-only Slider: a dark background with a coloured fill and no handle.</summary>
        static Slider Bar(string name, Transform parent, Vector2 position, Vector2 size, Color fillColor)
        {
            RectTransform rect = Rect(name, parent, new Vector2(0f, 1f), new Vector2(0f, 1f), position, size);
            var background = rect.gameObject.AddComponent<Image>();
            background.color = BarBackColor;
            background.raycastTarget = false;
            var edge = rect.gameObject.AddComponent<Outline>();
            edge.effectColor = BarEdgeColor;
            edge.effectDistance = new Vector2(2f, -2f);

            RectTransform fillArea = Rect("Fill Area", rect, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Stretch(fillArea, 3f);
            RectTransform fill = Rect("Fill", fillArea, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Stretch(fill, 0f);
            var fillImage = fill.gameObject.AddComponent<Image>();
            fillImage.color = fillColor;
            fillImage.raycastTarget = false;

            var slider = rect.gameObject.AddComponent<Slider>();
            slider.interactable = false;
            slider.transition = Selectable.Transition.None;
            slider.navigation = new Navigation { mode = Navigation.Mode.None };
            slider.fillRect = fill;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;
            return slider;
        }
    }
}
