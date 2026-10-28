using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RobotEscape.Editor
{
    public static class WatchRoomBuilder
    {
        public const string ScenePath = "Assets/Scenes/WatchRoom.unity";
        private const string Art = "Assets/Art/Room02Backdrop/";
        private static readonly Color Ink = new Color(0.065f, 0.105f, 0.115f, 0.98f);
        private static readonly Color Brass = new Color(0.65f, 0.51f, 0.31f);
        private static readonly Color Paper = new Color(0.88f, 0.85f, 0.73f);

        [MenuItem("Tools/Robot Escape/Build Watch Room")]
        public static void BuildRoom()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play mode before building.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save the current scene before building.");
            Sprite background = ImportBackground();
            Dictionary<string, Sprite> art = ImportAtlas();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            WatchRoomController room = new GameObject("WatchRoomController").AddComponent<WatchRoomController>();
            SpriteRenderer backdrop = Illustration(room.transform, "Watch Office Background", background, Vector2.zero, new Vector2(16, 9), -100, false);
            backdrop.transform.position = Vector3.zero;
            Camera camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
            camera.tag = "MainCamera"; camera.transform.position = new Vector3(0, 0, -10);
            camera.orthographic = true; camera.orthographicSize = 4.5f;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Ink;
            room.robot = ((GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Robot.prefab"))).GetComponent<RobotWalker>();
            room.robot.transform.position = new Vector3(-4.4f, -2.65f, 0);
            room.robot.minWalk = new Vector2(-7.4f, -3.6f); room.robot.maxWalk = new Vector2(7.4f, -1.05f);

            Illustration(room.transform, "Writing Desk", art["Desk"], new Vector2(-1.5f, -1.2f), new Vector2(3.9f, 1.85f), 25, false);
            Illustration(room.transform, "Filing Cabinet", art["Cabinet"], new Vector2(-5.4f, -1.2f), new Vector2(1.45f, 1.95f), 20);
            Illustration(room.transform, "Stopped Clock", art["Clock"], new Vector2(-5.3f, 2.2f), new Vector2(0.8f, 0.8f), 10);
            room.radio = Item(room, "Shortwave Receiver", ObjectKind.Radio, art["Radio"], new Vector2(-2.15f, 0.4f), new Vector2(1.25f, 0.95f), new Vector2(-2.1f, -1.9f));
            room.photo = Item(room, "Old Photograph", ObjectKind.Photo, art["Photo"], new Vector2(-0.65f, 0.4f), new Vector2(0.62f, 0.72f), new Vector2(-0.6f, -1.9f));
            room.dutyLog = Item(room, "Late Duty Record", ObjectKind.DutyLog, art["Clipboard"], new Vector2(-3.1f, 0.4f), new Vector2(0.48f, 0.65f), new Vector2(-3.1f, -1.9f));
            room.notice = Item(room, "Shutdown Notice", ObjectKind.Notice, art["Clipboard"], new Vector2(-2.6f, 1.2f), new Vector2(0.6f, 0.9f), new Vector2(-3.2f, -1.55f));
            room.frequencyCard = Item(room, "Faded Frequency Card", ObjectKind.FrequencyCard, art["Clipboard"], new Vector2(-5.4f, -0.05f), new Vector2(0.4f, 0.45f), new Vector2(-5.4f, -1.9f));
            room.npc = Item(room, "A Mao - Watchkeeper", ObjectKind.Npc, art["Watchkeeper"], new Vector2(2.25f, -1.45f), new Vector2(1.55f, 2.35f), new Vector2(1.6f, -2.05f));
            room.npc.GetComponentInChildren<SpriteRenderer>().sortingOrder = 120;
            room.upperExit = Item(room, "Upper Corridor", ObjectKind.UpperExit, art["Door"], new Vector2(6.25f, -1.4f), new Vector2(1.5f, 2.8f), new Vector2(6.15f, -1.8f));
            Vector2 openingSize = room.upperExit.GetComponentInChildren<SpriteRenderer>().bounds.size;
            SpriteRenderer opening = Illustration(room.upperExit.transform, "Dark Corridor", AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Prototype/Square.png"), Vector2.up * openingSize.y * 0.5f, openingSize, 35, false);
            opening.color = new Color(0.035f, 0.055f, 0.055f);
            room.exitLever = Item(room, "Corridor Release", ObjectKind.ExitLever, art["Lever"], new Vector2(4.65f, -0.15f), new Vector2(0.53f, 0.95f), new Vector2(4.65f, -1.8f));
            room.leverHandle = room.exitLever.transform.Find("Illustration");
            room.radioReceiverLight = Dot(room.radio.transform, "Receiver Lamp", new Vector2(0.22f, 0.18f), 0.07f);
            room.exitLamp = Dot(room.upperExit.transform, "Corridor Lamp", new Vector2(0, 2.62f), 0.13f);
            BuildUi(room, art);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new InvalidOperationException("Could not save WatchRoom.");
            Scene demo = EditorSceneManager.OpenScene(RobotEscapeBuilder.DemoPath);
            RobotEscapeRoom first = UnityEngine.Object.FindObjectOfType<RobotEscapeRoom>();
            first.nextRoomScene = "WatchRoom"; EditorUtility.SetDirty(first);
            EditorSceneManager.MarkSceneDirty(demo); EditorSceneManager.SaveScene(demo);
            List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
            foreach (string path in new[] { RobotEscapeBuilder.DemoPath, ScenePath })
            {
                int index = scenes.FindIndex(entry => entry.path == path);
                if (index < 0) scenes.Add(new EditorBuildSettingsScene(path, true));
                else scenes[index].enabled = true;
            }
            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("ROBOT_ESCAPE_WATCH_ROOM_BUILT");
        }

        private static void BuildUi(WatchRoomController room, Dictionary<string, Sprite> art)
        {
            Canvas canvas = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)).GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900); scaler.matchWidthOrHeight = 0.5f;
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            Transform ui = canvas.transform;
            Image badge = Box(ui, "Room Badge", new Vector2(0, 1), new Vector2(120, -30), new Vector2(230, 48), Ink);
            Label(badge.transform, "Room Name", "02  旧值班室", Vector2.zero, new Vector2(220, 40), 25);
            room.restartButton = Button(ui, "Restart Room", "重新开始", new Vector2(1, 1), new Vector2(-85, -30), new Vector2(140, 42));
            room.returnTitleButton = Button(ui, "Return Title", "返回标题", new Vector2(1, 1), new Vector2(-240, -30), new Vector2(140, 42));
            Label(ui, "Controls", "左键交互   Esc 返回", new Vector2(-590, -423), new Vector2(380, 28), 17).color = new Color(0.77f, 0.77f, 0.65f, 0.7f);

            room.inspectPanel = Modal(ui, "Evidence Panel", new Vector2(940, 520));
            Transform inspect = room.inspectPanel.transform.Find("Card");
            room.inspectTitle = Label(inspect, "Title", "", new Vector2(0, 190), new Vector2(800, 55), 31);
            room.inspectIllustration = Box(inspect, "Evidence Art", new Vector2(0.5f, 0.5f), new Vector2(-280, -5), new Vector2(250, 290), Color.white);
            room.inspectIllustration.preserveAspect = true;
            room.inspectText = Label(inspect, "Evidence Text", "", new Vector2(145, 10), new Vector2(510, 250), 26);
            room.inspectText.alignment = TextAnchor.MiddleLeft;
            room.closeInspect = Button(inspect, "Close", "放回原处", new Vector2(0.5f, 0.5f), new Vector2(190, -175), new Vector2(200, 50));

            room.dialoguePanel = Modal(ui, "Watchkeeper Dialogue", new Vector2(1080, 480));
            Transform dialogue = room.dialoguePanel.transform.Find("Card");
            Label(dialogue, "Name", "阿铆 · 留守的值班员", new Vector2(85, 173), new Vector2(650, 50), 28);
            Image portrait = Box(dialogue, "Portrait", new Vector2(0.5f, 0.5f), new Vector2(-380, -12), new Vector2(200, 300), Color.white);
            portrait.sprite = art["Watchkeeper"]; portrait.preserveAspect = true;
            room.dialogueText = Label(dialogue, "Dialogue", "", new Vector2(95, 25), new Vector2(690, 200), 27);
            room.dialogueText.alignment = TextAnchor.MiddleLeft;
            room.askCompanionButton = Button(dialogue, "Ask Companion", "那个同伴", new Vector2(0.5f, 0.5f), new Vector2(-115, -155), new Vector2(180, 48));
            room.askShutdownButton = Button(dialogue, "Ask Shutdown", "工厂停机", new Vector2(0.5f, 0.5f), new Vector2(95, -155), new Vector2(180, 48));
            room.closeDialogueButton = Button(dialogue, "Leave", "待会儿见", new Vector2(0.5f, 0.5f), new Vector2(305, -155), new Vector2(180, 48));

            room.radioPanel = Modal(ui, "Receiver Panel", new Vector2(1040, 650));
            Transform radio = room.radioPanel.transform.Find("Card");
            Label(radio, "Title", "夜班收讯机", new Vector2(0, 265), new Vector2(800, 50), 31);
            Image radioPortrait = Box(radio, "Receiver Art", new Vector2(0.5f, 0.5f), new Vector2(-310, 80), new Vector2(300, 240), Color.white);
            radioPortrait.sprite = art["Radio"]; radioPortrait.preserveAspect = true;
            Box(radio, "Signal Window", new Vector2(0.5f, 0.5f), new Vector2(145, 175), new Vector2(540, 95), new Color(0.025f, 0.07f, 0.08f));
            room.waveformBars = new Image[33];
            for (int i = 0; i < room.waveformBars.Length; i++)
                room.waveformBars[i] = Box(radio, "Wave" + i, new Vector2(0.5f, 0.5f), new Vector2(-100 + i * 15.5f, 175), new Vector2(6, 68), new Color(0.35f, 0.67f, 0.59f));
            room.signalIndicator = Box(radio, "Signal Lamp", new Vector2(0.5f, 0.5f), new Vector2(395, 175), new Vector2(12, 12), Brass);
            room.tuningReadout = Label(radio, "Frequency", "90.0 MHz", new Vector2(150, 85), new Vector2(450, 48), 30);
            room.tuningSlider = MakeSlider(radio, new Vector2(145, 15));
            Label(radio, "Band Start", "88", new Vector2(-115, -26), new Vector2(70, 35), 18).color = Brass;
            Label(radio, "Band End", "108", new Vector2(405, -26), new Vector2(70, 35), 18).color = Brass;
            room.transmissionText = Label(radio, "Transmission", "", new Vector2(0, -140), new Vector2(900, 155), 25);
            room.replayButton = Button(radio, "Replay", "重播留言", new Vector2(0.5f, 0.5f), new Vector2(-140, -265), new Vector2(220, 48));
            room.closeRadioButton = Button(radio, "Close", "松开旋钮", new Vector2(0.5f, 0.5f), new Vector2(140, -265), new Vector2(220, 48));

            room.chapterPanel = Modal(ui, "Chapter Complete", new Vector2(1000, 550));
            Transform chapter = room.chapterPanel.transform.Find("Card");
            Label(chapter, "Title", "楼上的回声", new Vector2(0, 195), new Vector2(850, 65), 42);
            room.chapterText = Label(chapter, "Story", "", new Vector2(0, 20), new Vector2(860, 230), 27);
            Button again = Button(chapter, "Replay Room", "重玩本间", new Vector2(0.5f, 0.5f), new Vector2(-150, -190), new Vector2(230, 54));
            UnityEventTools.AddPersistentListener(again.onClick, room.Restart);
            Button back = Button(chapter, "Back To Title", "返回标题", new Vector2(0.5f, 0.5f), new Vector2(150, -190), new Vector2(230, 54));
            UnityEventTools.AddPersistentListener(back.onClick, room.ReturnToTitle);
            foreach (GameObject panel in new[] { room.inspectPanel, room.dialoguePanel, room.radioPanel, room.chapterPanel }) panel.SetActive(false);
        }

        private static Slider MakeSlider(Transform parent, Vector2 position)
        {
            Image track = Box(parent, "Tuning Dial", new Vector2(0.5f, 0.5f), position, new Vector2(520, 22), new Color(0.2f, 0.27f, 0.27f));
            track.raycastTarget = true;
            Slider slider = track.gameObject.AddComponent<Slider>();
            Image handle = Box(track.transform, "Handle", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(23, 42), Brass);
            handle.raycastTarget = true;
            slider.handleRect = handle.rectTransform; slider.targetGraphic = handle;
            slider.direction = Slider.Direction.LeftToRight; slider.minValue = 0; slider.maxValue = 1; slider.value = 0.1f;
            return slider;
        }

        private static RoomInteractable Item(WatchRoomController room, string name, ObjectKind kind, Sprite sprite, Vector2 foot, Vector2 fit, Vector2 stand)
        {
            RoomInteractable item = new GameObject(name, typeof(BoxCollider2D)).AddComponent<RoomInteractable>();
            item.kind = kind; item.transform.SetParent(room.transform, false); item.transform.position = foot;
            SpriteRenderer image = Illustration(item.transform, "Illustration", sprite, Vector2.zero, fit, 45);
            BoxCollider2D hit = item.GetComponent<BoxCollider2D>();
            hit.size = new Vector2(image.bounds.size.x, image.bounds.size.y); hit.offset = Vector2.up * hit.size.y * 0.5f;
            item.standPoint = new GameObject("Stand Point").transform; item.standPoint.SetParent(item.transform, false); item.standPoint.position = stand;
            return item;
        }

        private static SpriteRenderer Illustration(Transform parent, string name, Sprite sprite, Vector2 foot, Vector2 size, int order, bool preserve = true)
        {
            SpriteRenderer renderer = new GameObject(name, typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            renderer.transform.SetParent(parent, false); renderer.transform.localPosition = foot; renderer.sprite = sprite; renderer.sortingOrder = order;
            float scale = Mathf.Min(size.x / sprite.bounds.size.x, size.y / sprite.bounds.size.y);
            renderer.transform.localScale = preserve ? new Vector3(scale, scale, 1) : new Vector3(size.x / sprite.bounds.size.x, size.y / sprite.bounds.size.y, 1);
            return renderer;
        }

        private static SpriteRenderer Dot(Transform parent, string name, Vector2 position, float size)
        {
            SpriteRenderer dot = new GameObject(name, typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            dot.transform.SetParent(parent, false); dot.transform.localPosition = position;
            dot.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Prototype/Circle.png");
            dot.transform.localScale = new Vector3(size, size, 1); dot.sortingOrder = 90;
            return dot;
        }

        private static Image Box(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size, Color color)
        {
            Image image = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            RectTransform rect = image.rectTransform; rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = anchor; rect.anchoredPosition = position; rect.sizeDelta = size;
            image.color = color; image.raycastTarget = false; return image;
        }

        private static Text Label(Transform parent, string name, string value, Vector2 position, Vector2 size, int fontSize)
        {
            Text text = new GameObject(name, typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            RectTransform rect = text.rectTransform; rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position; rect.sizeDelta = size;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.fontSize = fontSize; text.text = value;
            text.color = Paper; text.alignment = TextAnchor.MiddleCenter; text.raycastTarget = false;
            return text;
        }

        private static Button Button(Transform parent, string name, string value, Vector2 anchor, Vector2 position, Vector2 size)
        {
            Image image = Box(parent, name, anchor, position, size, new Color(0.13f, 0.2f, 0.21f)); image.raycastTarget = true;
            Outline outline = image.gameObject.AddComponent<Outline>(); outline.effectColor = Brass; outline.effectDistance = new Vector2(1, -1);
            Button button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            Label(image.transform, "Text", value, Vector2.zero, size - new Vector2(8, 4), 22);
            return button;
        }

        private static GameObject Modal(Transform parent, string name, Vector2 size)
        {
            Image overlay = Box(parent, name, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0.01f, 0.025f, 0.03f, 0.78f));
            overlay.rectTransform.anchorMin = Vector2.zero; overlay.rectTransform.anchorMax = Vector2.one;
            overlay.rectTransform.offsetMin = overlay.rectTransform.offsetMax = Vector2.zero; overlay.raycastTarget = true;
            Image card = Box(overlay.transform, "Card", new Vector2(0.5f, 0.5f), Vector2.zero, size, Ink);
            Outline outline = card.gameObject.AddComponent<Outline>(); outline.effectColor = Brass; outline.effectDistance = new Vector2(2, -2);
            return overlay.gameObject;
        }

        private static TextureImporter Configure(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("Import the Room02 PNG assets first: " + path);
            importer.textureType = TextureImporterType.Sprite; importer.spritePixelsPerUnit = 256; importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false; importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048; importer.filterMode = FilterMode.Bilinear;
            TextureImporterSettings settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect; importer.SetTextureSettings(settings);
            return importer;
        }

        private static Sprite ImportBackground()
        {
            TextureImporter importer = Configure(Art + "WatchOffice.png"); importer.spriteImportMode = SpriteImportMode.Single;
            importer.SaveAndReimport(); return AssetDatabase.LoadAssetAtPath<Sprite>(Art + "WatchOffice.png");
        }

        private static Dictionary<string, Sprite> ImportAtlas()
        {
            string path = Art + "WatchProps.png";
            string[] names = { "Desk", "Radio", "Cabinet", "Watchkeeper", "Photo", "Clipboard", "Lever", "Door", "Clock" };
            TextureImporter importer = Configure(path); importer.spriteImportMode = SpriteImportMode.Multiple; importer.isReadable = true; importer.SaveAndReimport();
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path); Color32[] pixels = texture.GetPixels32();
            SpriteDataProviderFactories factories = new SpriteDataProviderFactories(); factories.Init();
            ISpriteEditorDataProvider provider = factories.GetSpriteEditorDataProviderFromObject(importer); provider.InitSpriteEditorDataProvider();
            Dictionary<string, GUID> ids = provider.GetSpriteRects().ToDictionary(rect => rect.name, rect => rect.spriteID);
            List<SpriteRect> rects = new List<SpriteRect>();
            // Regions follow the painted atlas rather than clipping wide desks and tall antennas into equal cells.
            RectInt[] regions = {
                new RectInt(0, 0, 495, 405), new RectInt(495, 0, 395, 420), new RectInt(890, 0, 390, 425),
                new RectInt(0, 405, 420, 441), new RectInt(420, 425, 450, 415), new RectInt(870, 425, 410, 425),
                new RectInt(0, 846, 420, 434), new RectInt(420, 840, 430, 440), new RectInt(850, 850, 430, 430)
            };
            for (int index = 0; index < names.Length; index++)
            {
                RectInt region = regions[index];
                int x0 = region.xMin * texture.width / 1280, x1 = region.xMax * texture.width / 1280;
                int y0 = texture.height - region.yMax * texture.height / 1280, y1 = texture.height - region.yMin * texture.height / 1280;
                int left = x1, right = x0, bottom = y1, top = y0;
                for (int y = y0; y < y1; y++) for (int x = x0; x < x1; x++)
                    if (pixels[y * texture.width + x].a > 30) { left = Mathf.Min(left, x); right = Mathf.Max(right, x); bottom = Mathf.Min(bottom, y); top = Mathf.Max(top, y); }
                if (left > right || bottom > top) throw new InvalidOperationException("Empty atlas cell: " + names[index]);
                string name = names[index];
                rects.Add(new SpriteRect { name = name, rect = new Rect(left, bottom, right - left + 1, top - bottom + 1), alignment = SpriteAlignment.BottomCenter, pivot = new Vector2(0.5f, 0), spriteID = ids.ContainsKey(name) ? ids[name] : GUID.Generate() });
            }
            provider.SetSpriteRects(rects.ToArray());
            provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(rects.Select(rect => new SpriteNameFileIdPair(rect.name, rect.spriteID)));
            provider.Apply(); importer.SaveAndReimport();
            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToDictionary(sprite => sprite.name);
        }
    }
}
