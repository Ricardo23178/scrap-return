using System;
using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace RobotEscape.Editor
{
    public static class RobotEscapeBuilder
    {
        public const string DemoPath = "Assets/Scenes/RobotEscapeDemo.unity";
        private static Sprite square;
        private static Sprite circle;
        private static Font font;

        [MenuItem("Tools/Robot Escape/Create Prototype Room")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play mode first.");
            if (File.Exists(DemoPath)) throw new InvalidOperationException("RobotEscapeDemo already exists; open it instead of overwriting it.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save the current scene first.");
            Directory.CreateDirectory("Assets/Art/Prototype");
            Directory.CreateDirectory("Assets/Prefabs");
            Directory.CreateDirectory("Assets/Scenes");
            square = Shape("Square", false);
            circle = Shape("Circle", true);
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Camera camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
            camera.tag = "MainCamera";
            camera.transform.position = new Vector3(0, 0, -10);
            camera.orthographic = true;
            camera.orthographicSize = 4.5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.09f, 0.14f, 0.16f);
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            GameObject decor = new GameObject("Room Art - replace with hand drawn sprites later");
            RectSprite("Back Wall", decor.transform, new Vector2(0, 1), new Vector2(17, 6), new Color(0.24f, 0.31f, 0.32f), -50);
            RectSprite("Floor", decor.transform, new Vector2(0, -2.4f), new Vector2(17, 3.2f), new Color(0.38f, 0.35f, 0.29f), -40);
            RectSprite("Wall Skirting", decor.transform, new Vector2(0, -0.75f), new Vector2(17, 0.14f), new Color(0.13f, 0.2f, 0.2f), -39);
            for (int i = -7; i <= 7; i += 2)
            {
                RectSprite("Wall Panel", decor.transform, new Vector2(i, 1.5f), new Vector2(0.04f, 4.3f), new Color(0.19f, 0.26f, 0.27f), -49);
                RectSprite("Floor Seam", decor.transform, new Vector2(i, -2.4f), new Vector2(0.025f, 3), new Color(0.3f, 0.28f, 0.24f), -39);
            }
            RectSprite("Pipe", decor.transform, new Vector2(-1, 2.5f), new Vector2(12, 0.2f), new Color(0.48f, 0.39f, 0.26f), -30);
            RectSprite("Pipe Down", decor.transform, new Vector2(4.9f, 1.2f), new Vector2(0.2f, 2.8f), new Color(0.48f, 0.39f, 0.26f), -30);
            for (int i = -5; i <= 5; i += 5)
                RectSprite("Lamp", decor.transform, new Vector2(i, 2.9f), new Vector2(1.15f, 0.12f), new Color(0.95f, 0.8f, 0.43f), -20);

            RobotEscapeRoom room = new GameObject("RobotEscapeRoom").AddComponent<RobotEscapeRoom>();
            room.robot = CreateRobot();
            GameObject prefabSource = room.robot.gameObject;
            PrefabUtility.SaveAsPrefabAssetAndConnect(prefabSource, "Assets/Prefabs/Robot.prefab", InteractionMode.AutomatedAction);
            room.trash = Target("Trash Pile", ObjectKind.Trash, new Vector2(-4.4f, -1.05f), new Vector2(-4.4f, -2), new Vector2(2.3f, 1.4f));
            for (int i = 0; i < 7; i++)
            {
                SpriteRenderer scrap = RectSprite("Scrap", room.trash.transform, new Vector2(-0.8f + i * 0.26f, 0.12f + i % 3 * 0.12f),
                    new Vector2(0.75f, 0.5f), i % 2 == 0 ? new Color(0.48f, 0.44f, 0.35f) : new Color(0.3f, 0.37f, 0.36f), 150 + i);
                scrap.transform.localRotation = Quaternion.Euler(0, 0, -30 + i * 11);
            }
            room.battery = Target("Hidden Battery", ObjectKind.Battery, new Vector2(-2.95f, -1.95f), new Vector2(-2.95f, -2.3f), new Vector2(0.65f, 0.9f));
            RectSprite("Battery Body", room.battery.transform, new Vector2(0, 0.25f), new Vector2(0.45f, 0.65f), new Color(0.93f, 0.7f, 0.23f), 210);
            RectSprite("Battery Cap", room.battery.transform, new Vector2(0, 0.61f), new Vector2(0.18f, 0.1f), Color.gray, 211);
            WorldLabel("+", room.battery.transform, new Vector2(0, 0.26f), 0.14f, 220);
            room.terminal = Target("Power Console", ObjectKind.Console, new Vector2(1.7f, -0.15f), new Vector2(1.7f, -1.7f), new Vector2(1.5f, 2.2f));
            RectSprite("Console Body", room.terminal.transform, Vector2.zero, new Vector2(1.3f, 1.8f), new Color(0.48f, 0.53f, 0.49f), 120);
            RectSprite("Screen", room.terminal.transform, new Vector2(0, 0.35f), new Vector2(0.94f, 0.55f), new Color(0.09f, 0.18f, 0.19f), 121);
            room.terminalLight = RectSprite("Power Light", room.terminal.transform, new Vector2(0.35f, -0.4f), new Vector2(0.18f, 0.18f), Color.red, 122);
            room.door = Target("Exit Door", ObjectKind.Door, new Vector2(6, 0), new Vector2(6, -1.6f), new Vector2(2.1f, 3.5f));
            RectSprite("Door Frame", room.door.transform, Vector2.zero, new Vector2(2.05f, 3.2f), new Color(0.12f, 0.2f, 0.2f), 110);
            room.doorLeaf = RectSprite("Door Leaf", room.door.transform, Vector2.zero, new Vector2(1.65f, 2.9f), new Color(0.42f, 0.29f, 0.21f), 111);
            RectSprite("Door Stripe", room.doorLeaf.transform, new Vector2(0, -0.2f), new Vector2(1.4f, 0.13f), new Color(0.85f, 0.68f, 0.25f), 112);
            WorldLabel("JUNK", room.trash.transform, new Vector2(0, 1.05f), 0.12f, 230);
            WorldLabel("POWER", room.terminal.transform, new Vector2(0, 1.25f), 0.1f, 230);
            WorldLabel("EXIT", room.door.transform, new Vector2(0, 1.95f), 0.13f, 230);

            CreateUi(room);
            RobotEscapeSceneStyling.ApplyToRoom(room);
            if (!EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), DemoPath)) throw new IOException("Could not save the demo scene.");
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(DemoPath, true) };
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes) if (scene.path != DemoPath) scenes.Add(scene);
            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("ROBOT_ESCAPE_BUILD_OK " + DemoPath);
        }

        private static RobotWalker CreateRobot()
        {
            GameObject root = new GameObject("Robot", typeof(SortingGroup));
            root.transform.position = new Vector3(-0.8f, -2.7f, 0);
            RobotWalker walker = root.AddComponent<RobotWalker>();
            RectSprite("Shadow", root.transform, new Vector2(0, -0.03f), new Vector2(0.85f, 0.2f), new Color(0.1f, 0.13f, 0.12f, 0.65f), 160).sprite = circle;
            walker.body = new GameObject("Robot Visuals").transform;
            walker.body.SetParent(root.transform, false);
            Color orange = new Color(0.87f, 0.54f, 0.22f);
            RectSprite("Body", walker.body, new Vector2(0, 0.55f), new Vector2(0.6f, 0.6f), orange, 161);
            RectSprite("Head", walker.body, new Vector2(0, 1.08f), new Vector2(0.75f, 0.4f), new Color(0.94f, 0.69f, 0.32f), 161);
            foreach (int sign in new[] { -1, 1 })
            {
                RectSprite("Eye", walker.body, new Vector2(sign * 0.19f, 1.1f), new Vector2(0.13f, 0.11f), new Color(0.09f, 0.2f, 0.24f), 162);
                RectSprite("Leg", walker.body, new Vector2(sign * 0.19f, 0.15f), new Vector2(0.18f, 0.25f), new Color(0.2f, 0.28f, 0.28f), 161);
                RectSprite("Arm", walker.body, new Vector2(sign * 0.43f, 0.56f), new Vector2(0.18f, 0.52f), orange, 161);
            }
            RectSprite("Antenna", walker.body, new Vector2(0, 1.4f), new Vector2(0.05f, 0.25f), Color.gray, 161);
            return walker;
        }

        private static RoomInteractable Target(string name, ObjectKind kind, Vector2 position, Vector2 standing, Vector2 hitSize)
        {
            GameObject target = new GameObject(name, typeof(BoxCollider2D), typeof(RoomInteractable), typeof(SortingGroup));
            target.transform.position = position;
            target.GetComponent<SortingGroup>().sortingOrder = 100 + Mathf.RoundToInt(-position.y * 20);
            target.GetComponent<BoxCollider2D>().size = hitSize;
            RoomInteractable item = target.GetComponent<RoomInteractable>();
            item.kind = kind;
            item.standPoint = new GameObject("Stand Here").transform;
            item.standPoint.SetParent(target.transform, false);
            item.standPoint.position = standing;
            return item;
        }

        private static Sprite Shape(string name, bool round)
        {
            string path = "Assets/Art/Prototype/" + name + ".png";
            if (File.Exists(path)) throw new IOException("Refusing to replace existing art: " + path);
            Texture2D texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++)
                texture.SetPixel(x, y, !round || Vector2.Distance(new Vector2(x, y), new Vector2(15.5f, 15.5f)) <= 15 ? Color.white : Color.clear);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 32;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static SpriteRenderer RectSprite(string name, Transform parent, Vector2 position, Vector2 size, Color color, int order)
        {
            SpriteRenderer sprite = new GameObject(name, typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            sprite.transform.SetParent(parent, false);
            sprite.transform.localPosition = position;
            sprite.transform.localScale = new Vector3(size.x, size.y, 1);
            sprite.sprite = square;
            sprite.color = color;
            sprite.sortingOrder = order;
            return sprite;
        }

        private static void WorldLabel(string value, Transform parent, Vector2 position, float scale, int order)
        {
            TextMesh text = new GameObject(value + " Label", typeof(TextMesh)).GetComponent<TextMesh>();
            text.transform.SetParent(parent, false);
            text.transform.localPosition = position;
            text.text = value;
            text.font = font;
            text.fontSize = 40;
            text.characterSize = scale;
            text.anchor = TextAnchor.MiddleCenter;
            MeshRenderer renderer = text.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = font.material;
            renderer.sortingOrder = order;
            text.color = new Color(0.94f, 0.88f, 0.69f);
        }

        private static void CreateUi(RobotEscapeRoom room)
        {
            Canvas canvas = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)).GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900);
            scaler.matchWidthOrHeight = 0.5f;
            Transform ui = canvas.transform;
            Panel(ui, "Header", new Vector2(0.5f, 1), new Vector2(0, -48), new Vector2(1600, 96), new Color(0.07f, 0.12f, 0.14f, 0.96f));
            Label(ui, "Title", "废厂重启 · 维修间 / 流程原型", new Vector2(0, 1), new Vector2(340, -30), new Vector2(650, 42), 30, TextAnchor.MiddleLeft);
            room.objectiveText = Label(ui, "Objective", "", new Vector2(0, 1), new Vector2(560, -70), new Vector2(1090, 36), 23, TextAnchor.MiddleLeft);
            Button restart = UiButton(ui, "Restart", "重新开始", new Vector2(1, 1), new Vector2(-110, -45), new Vector2(180, 52), new Color(0.28f, 0.36f, 0.37f));
            UnityEventTools.AddPersistentListener(restart.onClick, room.Restart);
            Label(ui, "Wall Note", "维修记录：蓝 → 黄 → 红", new Vector2(0.5f, 0.5f), new Vector2(-80, 140), new Vector2(550, 50), 27, TextAnchor.MiddleCenter);
            Panel(ui, "Footer", new Vector2(0.5f, 0), new Vector2(0, 58), new Vector2(1600, 116), new Color(0.07f, 0.12f, 0.14f, 0.97f));
            room.inventoryButton = UiButton(ui, "InventoryBattery", "背包：空", new Vector2(0, 0), new Vector2(110, 58), new Vector2(180, 82), new Color(0.19f, 0.25f, 0.27f));
            room.inventoryText = room.inventoryButton.GetComponentInChildren<Text>();
            UnityEventTools.AddPersistentListener(room.inventoryButton.onClick, room.SelectBattery);
            room.statusText = Label(ui, "Status", "", new Vector2(0, 0), new Vector2(850, 65), new Vector2(1240, 60), 25, TextAnchor.MiddleLeft);
            Label(ui, "Controls", "鼠标左键：行走 / 调查 / 使用道具     Esc：关闭调查或机关界面", new Vector2(0, 0), new Vector2(850, 20), new Vector2(1240, 28), 18, TextAnchor.MiddleLeft);

            room.inspectPanel = Modal(ui, "InspectPanel", "调查垃圾堆");
            Label(room.inspectPanel.transform, "Description", "缝隙里有一点金属反光。\n轻轻摇一摇，也许能把它弄出来。", new Vector2(0.5f, 0.5f), new Vector2(0, 35), new Vector2(650, 100), 27, TextAnchor.MiddleCenter);
            room.shakeButton = UiButton(room.inspectPanel.transform, "Shake", "摇一摇", new Vector2(0.5f, 0.5f), new Vector2(-130, -100), new Vector2(220, 62), new Color(0.62f, 0.39f, 0.16f));
            UnityEventTools.AddPersistentListener(room.shakeButton.onClick, room.ShakeTrash);
            Button leave = UiButton(room.inspectPanel.transform, "Leave", "暂时离开", new Vector2(0.5f, 0.5f), new Vector2(130, -100), new Vector2(220, 62), new Color(0.24f, 0.32f, 0.34f));
            UnityEventTools.AddPersistentListener(leave.onClick, room.ClosePanels);
            room.puzzlePanel = Modal(ui, "PuzzlePanel", "控制台 · 解除锁定");
            room.puzzleText = Label(room.puzzlePanel.transform, "PuzzleMessage", "", new Vector2(0.5f, 0.5f), new Vector2(0, 45), new Vector2(720, 100), 26, TextAnchor.MiddleCenter);
            Color[] colors = { new Color(0.68f, 0.23f, 0.18f), new Color(0.16f, 0.38f, 0.65f), new Color(0.66f, 0.53f, 0.14f) };
            string[] names = { "红", "蓝", "黄" };
            for (int i = 0; i < 3; i++)
            {
                Button button = UiButton(room.puzzlePanel.transform, "Color" + i, names[i], new Vector2(0.5f, 0.5f), new Vector2(-210 + i * 210, -70), new Vector2(170, 72), colors[i]);
                UnityEventTools.AddIntPersistentListener(button.onClick, room.PressPuzzle, i);
            }
            Button close = UiButton(room.puzzlePanel.transform, "Close", "返回房间查看线索", new Vector2(0.5f, 0.5f), new Vector2(0, -170), new Vector2(340, 52), new Color(0.24f, 0.32f, 0.34f));
            UnityEventTools.AddPersistentListener(close.onClick, room.ClosePanels);
            room.winPanel = Modal(ui, "WinPanel", "维修间已完成！");
            Label(room.winPanel.transform, "Summary", "调查垃圾堆 → 找到电池 → 恢复供电\n解开机关 → 打开出口 → 成功离开", new Vector2(0.5f, 0.5f), new Vector2(0, 30), new Vector2(730, 100), 27, TextAnchor.MiddleCenter);
            Button again = UiButton(room.winPanel.transform, "PlayAgain", "再玩一次", new Vector2(0.5f, 0.5f), new Vector2(0, -110), new Vector2(270, 64), new Color(0.62f, 0.39f, 0.16f));
            UnityEventTools.AddPersistentListener(again.onClick, room.Restart);
            room.battery.gameObject.SetActive(false);
            room.inspectPanel.SetActive(false);
            room.puzzlePanel.SetActive(false);
            room.winPanel.SetActive(false);
        }

        private static GameObject Modal(Transform parent, string name, string title)
        {
            GameObject overlay = Panel(parent, name, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0.01f, 0.03f, 0.04f, 0.88f));
            RectTransform rect = overlay.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            Panel(overlay.transform, "Card", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(850, 480), new Color(0.13f, 0.21f, 0.23f));
            Label(overlay.transform, "Heading", title, new Vector2(0.5f, 0.5f), new Vector2(0, 175), new Vector2(760, 60), 38, TextAnchor.MiddleCenter);
            return overlay;
        }

        private static GameObject Panel(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size, Color color)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            Position(panel, parent, anchor, position, size);
            panel.GetComponent<Image>().color = color;
            return panel;
        }

        private static Text Label(Transform parent, string name, string value, Vector2 anchor, Vector2 position, Vector2 size, int fontSize, TextAnchor alignment)
        {
            Text text = new GameObject(name, typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            Position(text.gameObject, parent, anchor, position, size);
            text.font = font; text.fontSize = fontSize; text.text = value;
            text.alignment = alignment;
            text.color = new Color(0.95f, 0.91f, 0.79f);
            text.raycastTarget = false;
            return text;
        }

        private static Button UiButton(Transform parent, string name, string value, Vector2 anchor, Vector2 position, Vector2 size, Color color)
        {
            GameObject root = Panel(parent, name, anchor, position, size, color);
            Button button = root.AddComponent<Button>();
            button.targetGraphic = root.GetComponent<Image>();
            Text text = Label(root.transform, "Text", value, new Vector2(0.5f, 0.5f), Vector2.zero, size - new Vector2(10, 4), 24, TextAnchor.MiddleCenter);
            return button;
        }

        private static void Position(GameObject target, Transform parent, Vector2 anchor, Vector2 position, Vector2 size)
        {
            RectTransform rect = target.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }
    }
}
