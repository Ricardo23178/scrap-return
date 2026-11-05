using System;
using System.Collections.Generic;
using System.IO;
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
    public static class PrisonChapterBuilder
    {
        public const string ScenePath = "Assets/Scenes/PrisonEscape.unity";
        private const string ArtPath = "Assets/Art/Room04Prison/";
        private static readonly Color Ink = new Color(.055f, .095f, .105f, .97f);
        private static readonly Color Paper = new Color(.9f, .85f, .71f);
        private static readonly Color Brass = new Color(.65f, .46f, .25f);
        private static Dictionary<string, Sprite> props;
        private static Sprite square, circle;
        private static readonly List<PrisonTarget> targets = new List<PrisonTarget>();

        [MenuItem("Tools/Robot Escape/Build Prison Chapter")]
        public static void Build()
        {
            RequireEditable();
            if (File.Exists(ScenePath)) throw new InvalidOperationException("PrisonEscape already exists; the builder will not replace an existing level.");
            foreach (string name in new[] { "Cell.png", "Void.png", "Corridor.png", "Props.png", "Climb.png" })
                if (!File.Exists(ArtPath + name)) throw new FileNotFoundException("Prison chapter art is missing.", ArtPath + name);
            square = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Prototype/Square.png");
            circle = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Room03Backdrop/UiDisc.png");
            if (square == null || circle == null) throw new InvalidOperationException("Existing UI shape sprites are missing.");
            props = ImportAtlas("Props.png", 3, 3, new[] { "Rod", "Magnet", "Tool", "Key", "Gear", "Clock", "Vent", "Door", "Guard" });
            Dictionary<string, Sprite> climbing = ImportAtlas("Climb.png", 2, 2, new[] { "Climb0", "Climb1", "Climb2", "Climb3" });
            Sprite[] backgrounds = { ImportBackground("Cell.png"), ImportBackground("Void.png"), ImportBackground("Corridor.png") };
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            PrisonChapterController room = new GameObject("Prison Chapter").AddComponent<PrisonChapterController>();
            room.patrol = room.gameObject.AddComponent<PrisonPatrol>();
            room.patrol.audioSource = room.gameObject.AddComponent<AudioSource>();
            room.patrol.audioSource.playOnAwake = false;
            room.patrol.audioSource.spatialBlend = 0;
            room.patrol.audioSource.volume = .45f;
            room.areas = new GameObject[3];
            for (int i = 0; i < 3; i++)
            {
                room.areas[i] = new GameObject(new[] { "Cell", "Maintenance Void", "Guard Corridor" }[i]);
                room.areas[i].transform.SetParent(room.transform, false);
                SpriteRenderer backdrop = Piece(room.areas[i].transform, "Background", backgrounds[i], Vector2.zero, new Vector2(16, 9), -100);
                backdrop.transform.localPosition = Vector3.zero;
                backdrop.transform.localScale = new Vector3(16f / backgrounds[i].bounds.size.x, 9f / backgrounds[i].bounds.size.y, 1);
            }
            Camera camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
            camera.tag = "MainCamera";
            camera.transform.position = new Vector3(0, 0, -10);
            camera.orthographic = true;
            camera.orthographicSize = 4.5f;
            camera.backgroundColor = Ink;
            camera.clearFlags = CameraClearFlags.SolidColor;
            GameObject robotAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Robot.prefab");
            if (robotAsset == null) throw new InvalidOperationException("The existing robot prefab is missing.");
            room.robot = ((GameObject)PrefabUtility.InstantiatePrefab(robotAsset)).GetComponent<RobotWalker>();
            room.robot.transform.position = new Vector3(-3.6f, -2.9f, 0);
            room.robot.minWalk = new Vector2(-6.5f, -3.2f);
            room.robot.maxWalk = new Vector2(6.5f, -2.7f);
            room.climbPoses = Enumerable.Range(0, 4).Select(i => climbing["Climb" + i]).ToArray();
            targets.Clear();
            Cell(room);
            Void(room);
            Corridor(room);
            room.targets = targets.ToArray();
            Ui(room);
            room.areas[0].SetActive(true); room.areas[1].SetActive(false); room.areas[2].SetActive(false);
            EditorUtility.SetDirty(room); EditorUtility.SetDirty(room.patrol);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new InvalidOperationException("Could not save PrisonEscape.");
            List<EditorBuildSettingsScene> buildScenes = EditorBuildSettings.scenes.ToList();
            int existing = buildScenes.FindIndex(entry => entry.path == ScenePath);
            if (existing < 0) buildScenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            else buildScenes[existing].enabled = true;
            EditorBuildSettings.scenes = buildScenes.ToArray();
            ApplyConnection();
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(ScenePath);
            Debug.Log("PRISON_CHAPTER_BUILT");
        }

        private static void Cell(PrisonChapterController room)
        {
            Transform area = room.areas[0].transform;
            PrisonTarget bed = Target(area, "Loose Bed Brace", PrisonAction.Bed, 0, false, new Vector2(-3, -2.25f), new Vector2(2.1f, .7f), new Vector2(-3, -2.85f));
            room.rodArt = Piece(bed.transform, "Loose Rod", props["Rod"], new Vector2(.2f, .25f), new Vector2(1.5f, .3f), 45);
            room.rodArt.transform.localRotation = Quaternion.Euler(0, 0, 22);
            PrisonTarget clock = Target(area, "Broken Mechanical Clock", PrisonAction.Clock, 0, false, new Vector2(1.9f, 1.3f), new Vector2(1.8f, 1.8f), new Vector2(1.8f, -2.7f));
            Piece(clock.transform, "Clock", props["Clock"], Vector2.zero, new Vector2(1.8f, 1.8f), 35);
            PrisonTarget vent = Target(area, "Cell Vent", PrisonAction.Vent, 0, false, new Vector2(3.4f, -2.2f), new Vector2(1.7f, 1.55f), new Vector2(3.1f, -2.9f));
            room.ventLeaf = Piece(vent.transform, "Vent Grille", props["Vent"], Vector2.zero, new Vector2(1.7f, 1.55f), 45);
            PrisonTarget door = Target(area, "Cell Door", PrisonAction.Door, 0, false, new Vector2(6.65f, -2.2f), new Vector2(2.75f, 5.5f), new Vector2(5.4f, -2.85f));
            room.cellDoor = Piece(door.transform, "Cell Door Leaf", props["Door"], Vector2.zero, new Vector2(1.65f, 4.35f), 55);
            DoorMask(room.cellDoor, new Vector2(2.75f, 5.5f));
            Target(area, "Old Cleaning Robot", PrisonAction.Prisoner, 0, false, new Vector2(-6.65f, -1.5f), new Vector2(1.1f, 2.25f), new Vector2(-5.8f, -2.7f));
            Target(area, "Dripping Sink", PrisonAction.Scenery, 0, false, new Vector2(1, -.9f), new Vector2(1.65f, 1.3f), new Vector2(.6f, -2.8f));
            Target(area, "Old Books", PrisonAction.Scenery, 0, false, new Vector2(-5.75f, -3.1f), new Vector2(1.1f, .55f), new Vector2(-5.1f, -2.9f));
            room.patrol.guardInCell = Piece(area, "Guard Behind Cell Door", props["Guard"], new Vector2(6.15f, -2.05f), new Vector2(1.35f, 3.4f), 40);
            room.patrol.warningLamp = Piece(area, "Patrol Light", circle, new Vector2(5.3f, -3.2f), new Vector2(2.3f, .35f), 65);
            room.patrol.warningLamp.color = new Color(.93f, .58f, .2f, .15f);
        }

        private static void Void(PrisonChapterController room)
        {
            Transform area = room.areas[1].transform;
            Target(area, "Return Through Vent", PrisonAction.ToCell, 1, false, new Vector2(-5.6f, -2.4f), new Vector2(2, 2.2f), new Vector2(-5.5f, -2.9f));
            PrisonTarget ladder = Target(area, "Maintenance Ladder", PrisonAction.Ladder, 1, false, new Vector2(0, -3), new Vector2(1.4f, 5.3f), new Vector2(0, -3));
            ladder.GetComponent<BoxCollider2D>().offset = new Vector2(0, 2.65f);
            PrisonTarget magnet = Target(area, "Discarded Motor Magnet", PrisonAction.Magnet, 1, false, new Vector2(4, -2.2f), new Vector2(.85f, .9f), new Vector2(4, -2.85f));
            room.magnetArt = Piece(magnet.transform, "Magnet", props["Magnet"], Vector2.zero, new Vector2(.85f, .9f), 40);
            PrisonTarget gear = Target(area, "Spare Clock Gear", PrisonAction.Gear, 1, false, new Vector2(4.5f, -.4f), new Vector2(.65f, .65f), new Vector2(4, -2.85f));
            room.gearArt = Piece(gear.transform, "Gear", props["Gear"], Vector2.zero, new Vector2(.65f, .65f), 40);
            PrisonTarget hole = Target(area, "Inspection Hole", PrisonAction.Hole, 1, true, new Vector2(5.6f, 2.4f), new Vector2(.8f, .9f), new Vector2(5.3f, 2));
            room.toolArt = Piece(hole.transform, "Reaching Tool", props["Tool"], new Vector2(-1.0f, .22f), new Vector2(1.2f, .32f), 80);
            room.toolArt.enabled = false;
            Target(area, "Dusty Tool Case", PrisonAction.Scenery, 1, true, new Vector2(-2.2f, 2), new Vector2(1.3f, .8f), new Vector2(-2.2f, 2));
        }

        private static void Corridor(PrisonChapterController room)
        {
            Transform area = room.areas[2].transform;
            PrisonTarget door = Target(area, "Return To Cell", PrisonAction.Door, 2, false, new Vector2(-6.1f, -2.3f), new Vector2(2.85f, 5.5f), new Vector2(-5.65f, -2.8f));
            room.corridorDoor = Piece(door.transform, "Corridor Door Leaf", props["Door"], Vector2.zero, new Vector2(1.6f, 4.35f), 55);
            DoorMask(room.corridorDoor, new Vector2(2.85f, 5.5f));
            room.keyArt = Piece(area, "Hanging Keys", props["Key"], new Vector2(.75f, .9f), new Vector2(.55f, .7f), 35);
            Target(area, "Escape Service Passage", PrisonAction.Exit, 2, false, new Vector2(6, -1.8f), new Vector2(1.7f, 3), new Vector2(5.9f, -2.6f));
            Target(area, "Guard Shift Book", PrisonAction.Scenery, 2, false, new Vector2(-.9f, -.6f), new Vector2(2, .65f), new Vector2(-1, -2.8f));
            room.patrol.guard = Piece(area, "Patrolling Guard", props["Guard"], new Vector2(-4.4f, -2.5f), new Vector2(1.5f, 3.4f), 175);
            room.patrol.guardPoses = new[] { props["Guard"] };
        }

        private static void Ui(PrisonChapterController room)
        {
            Canvas canvas = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)).GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scale = canvas.GetComponent<CanvasScaler>();
            scale.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scale.referenceResolution = new Vector2(1600, 900); scale.matchWidthOrHeight = .5f;
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            Transform ui = canvas.transform;
            Image badge = Box(ui, "Area Badge", new Vector2(0, 1), new Vector2(155, -32), new Vector2(300, 52), Ink);
            room.areaText = Label(badge.transform, "Area", "04  牢房", Vector2.zero, new Vector2(290, 45), 25);
            room.restartButton = Button(ui, "Restart", "重新开始", new Vector2(1, 1), new Vector2(-85, -32), new Vector2(140, 44));
            room.titleButton = Button(ui, "Title", "返回标题", new Vector2(1, 1), new Vector2(-240, -32), new Vector2(140, 44));
            room.inventoryButtons = new Button[5]; room.inventoryIcons = new Image[5];
            string[] names = { "Rod", "Magnet", "Tool", "Gear", "Key" };
            for (int i = 0; i < 5; i++)
            {
                Button slot = Button(ui, "Inventory " + names[i], "", Vector2.zero, new Vector2(65 + 100 * i, 62), new Vector2(88, 88));
                Image image = Box(slot.transform, "Icon", new Vector2(.5f, .5f), Vector2.zero, new Vector2(70, 70), Color.white);
                image.sprite = props[names[i]]; image.preserveAspect = true; image.raycastTarget = false;
                room.inventoryButtons[i] = slot; room.inventoryIcons[i] = image;
            }
            room.speechText = Label(ui, "Short Reactions", "", new Vector2(0, -305), new Vector2(950, 70), 23);
            Label(ui, "Controls", "左键交互  ·  Esc 返回", new Vector2(570, -425), new Vector2(420, 28), 17).color = new Color(.73f, .75f, .65f, .7f);

            room.inspectPanel = Modal(ui, "Inspect Panel", new Vector2(1010, 460));
            Transform inspect = room.inspectPanel.transform.Find("Card");
            room.inspectText = Label(inspect, "Response", "", new Vector2(0, 35), new Vector2(900, 250), 28);
            room.closeInspectButton = Button(inspect, "Close", "收起", new Vector2(.5f, .5f), new Vector2(0, -155), new Vector2(210, 52));

            room.clockPanel = Modal(ui, "Clock Panel", new Vector2(1160, 630));
            Transform clock = room.clockPanel.transform.Find("Card");
            Label(clock, "Title", "机械钟", new Vector2(0, 255), new Vector2(900, 50), 32);
            room.gearButtons = new Button[3]; room.gearWheels = new Transform[3];
            for (int i = 0; i < 3; i++)
            {
                Vector2 at = new Vector2(-300 + i * 300, 25);
                Button turn = Button(clock, "Gear " + i, "", new Vector2(.5f, .5f), at, new Vector2(220, 220));
                turn.GetComponent<Image>().sprite = circle; turn.GetComponent<Image>().color = new Color(.12f, .16f, .16f);
                Image wheel = Box(turn.transform, "Wheel", new Vector2(.5f, .5f), Vector2.zero, new Vector2(185, 185), Color.white);
                wheel.sprite = props["Gear"]; wheel.preserveAspect = true; wheel.raycastTarget = false;
                Image contact = Box(wheel.transform, "Contact", new Vector2(.5f, .5f), new Vector2(0, 65), new Vector2(8, 25), new Color(.24f, .66f, .63f));
                contact.raycastTarget = false;
                Vector3 goal = Quaternion.Euler(0, 0, (i + 1) * 90) * (Vector3.up * 103);
                Image fixedContact = Box(turn.transform, "Fixed Contact", new Vector2(.5f, .5f), goal, new Vector2(15, 15), Brass);
                fixedContact.sprite = circle; fixedContact.raycastTarget = false;
                room.gearButtons[i] = turn; room.gearWheels[i] = wheel.transform;
            }
            room.closeClockButton = Button(clock, "Close", "放开齿轮", new Vector2(.5f, .5f), new Vector2(0, -245), new Vector2(230, 52));

            room.winPanel = Modal(ui, "Chapter Complete", new Vector2(1080, 540));
            Transform ending = room.winPanel.transform.Find("Card");
            Label(ending, "Title", "离开监狱", new Vector2(0, 190), new Vector2(900, 65), 42);
            Label(ending, "Story", "铁门的声响渐渐远去。\n维修通道通往机械城的街巷。\n\n贝塔还在厨房，黑帽帮的炸弹正被送往最高塔。", new Vector2(0, 15), new Vector2(940, 220), 27);
            Button again = Button(ending, "Replay", "重玩本关", new Vector2(.5f, .5f), new Vector2(-150, -190), new Vector2(230, 54));
            Button title = Button(ending, "Title", "返回标题", new Vector2(.5f, .5f), new Vector2(150, -190), new Vector2(230, 54));
            UnityEventTools.AddPersistentListener(again.onClick, room.Restart);
            UnityEventTools.AddPersistentListener(title.onClick, room.ReturnToTitle);
            foreach (GameObject panel in new[] { room.inspectPanel, room.clockPanel, room.winPanel }) panel.SetActive(false);
            room.transitionShade = Box(ui, "Area Transition", new Vector2(.5f, .5f), Vector2.zero, new Vector2(3200, 2400), new Color(.025f, .045f, .05f, 0));
            room.transitionShade.raycastTarget = false;
        }

        public static void ApplyConnection()
        {
            RequireEditable();
            EditorSceneManager.OpenScene(ControlRoomBuilder.ScenePath);
            ControlRoomController room = UnityEngine.Object.FindObjectOfType<ControlRoomController>();
            if (room == null || room.capturePanel == null) throw new InvalidOperationException("The saved third-room capture ending is missing.");
            Transform plate = room.capturePanel.transform.Find("Story Plate");
            if (plate == null) throw new InvalidOperationException("The saved prison-ending story plate is missing.");
            Transform replay = plate.Find("Replay"), title = plate.Find("Title");
            if (replay != null) replay.GetComponent<RectTransform>().anchoredPosition = new Vector2(-310, -160);
            if (title != null) title.GetComponent<RectTransform>().anchoredPosition = new Vector2(310, -160);
            Transform existing = plate.Find("ContinuePrison");
            Button next = existing != null ? existing.GetComponent<Button>() : Button(plate, "ContinuePrison", "继续 · 越狱", new Vector2(.5f, .5f), new Vector2(0, -160), new Vector2(230, 54));
            if (next.onClick.GetPersistentEventCount() == 0) UnityEventTools.AddPersistentListener(next.onClick, room.ContinueToPrison);
            if (room.captureText != null) room.captureText.text = "铁门在身后重重合上。\n\n黑帽帮要炸毁城市中枢，贝塔还被锁在厨房。\n机器人必须先想办法离开这间牢房。\n\n第三关 · 被捕";
            EditorUtility.SetDirty(room); EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            if (!EditorSceneManager.SaveScene(SceneManager.GetActiveScene())) throw new InvalidOperationException("Could not save the third-room continuation.");
        }

        private static void RequireEditable()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play mode before building the prison chapter.");
            for (int i = 0; i < SceneManager.sceneCount; i++) if (SceneManager.GetSceneAt(i).isDirty)
                throw new InvalidOperationException("Save open scenes first; the prison builder did not discard unsaved work.");
        }

        private static PrisonTarget Target(Transform parent, string name, PrisonAction action, int area, bool upper, Vector2 foot, Vector2 size, Vector2 stand)
        {
            GameObject item = new GameObject(name, typeof(BoxCollider2D), typeof(PrisonTarget));
            item.transform.SetParent(parent, false); item.transform.localPosition = foot;
            BoxCollider2D hit = item.GetComponent<BoxCollider2D>(); hit.size = size; hit.offset = Vector2.up * size.y * .5f;
            PrisonTarget target = item.GetComponent<PrisonTarget>(); target.action = action; target.area = area; target.upperLanding = upper;
            target.standPoint = new GameObject("Stand Here").transform; target.standPoint.SetParent(item.transform, false); target.standPoint.position = stand;
            targets.Add(target); return target;
        }

        private static SpriteRenderer Piece(Transform parent, string name, Sprite sprite, Vector2 foot, Vector2 fit, int order)
        {
            SpriteRenderer item = new GameObject(name, typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            item.transform.SetParent(parent, false); item.transform.localPosition = foot; item.sprite = sprite; item.sortingOrder = order;
            float scale = Mathf.Min(fit.x / sprite.bounds.size.x, fit.y / sprite.bounds.size.y); item.transform.localScale = Vector3.one * scale;
            return item;
        }

        private static void DoorMask(SpriteRenderer leaf, Vector2 opening)
        {
            leaf.transform.localScale = new Vector3(opening.x / leaf.sprite.bounds.size.x, opening.y / leaf.sprite.bounds.size.y, 1);
            leaf.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            SpriteMask mask = new GameObject("Doorway Mask", typeof(SpriteMask)).GetComponent<SpriteMask>();
            mask.transform.SetParent(leaf.transform.parent, false);
            mask.transform.localPosition = Vector3.up * opening.y * .5f;
            mask.transform.localScale = new Vector3(opening.x / square.bounds.size.x, opening.y / square.bounds.size.y, 1);
            mask.sprite = square; mask.isCustomRangeActive = true;
            mask.backSortingOrder = 54; mask.frontSortingOrder = 56;
        }

        private static GameObject Modal(Transform parent, string name, Vector2 size)
        {
            Image overlay = Box(parent, name, new Vector2(.5f, .5f), Vector2.zero, new Vector2(3200, 2400), new Color(0, .015f, .02f, .69f));
            Box(overlay.transform, "Card", new Vector2(.5f, .5f), Vector2.zero, size, Ink);
            return overlay.gameObject;
        }

        private static Image Box(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size, Color color)
        {
            Image image = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(parent, false); RectTransform rect = image.rectTransform;
            rect.anchorMin = rect.anchorMax = anchor; rect.anchoredPosition = position; rect.sizeDelta = size;
            image.color = color; return image;
        }

        private static Button Button(Transform parent, string name, string text, Vector2 anchor, Vector2 position, Vector2 size)
        {
            Image image = Box(parent, name, anchor, position, size, new Color(.14f, .2f, .2f));
            Button button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            if (!string.IsNullOrEmpty(text)) Label(image.transform, "Text", text, Vector2.zero, size - new Vector2(8, 8), 23);
            return button;
        }

        private static Text Label(Transform parent, string name, string text, Vector2 position, Vector2 size, int fontSize)
        {
            Text label = new GameObject(name, typeof(RectTransform), typeof(Text)).GetComponent<Text>(); label.transform.SetParent(parent, false);
            label.rectTransform.anchorMin = label.rectTransform.anchorMax = new Vector2(.5f, .5f); label.rectTransform.anchoredPosition = position; label.rectTransform.sizeDelta = size;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); label.fontSize = fontSize; label.text = text;
            label.color = Paper; label.alignment = TextAnchor.MiddleCenter; label.raycastTarget = false;
            return label;
        }

        private static Sprite ImportBackground(string file)
        {
            string path = ArtPath + file; AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single; importer.spritePixelsPerUnit = 256;
            importer.mipmapEnabled = false; importer.maxTextureSize = 2048; importer.textureCompression = TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static Dictionary<string, Sprite> ImportAtlas(string file, int columns, int rows, string[] names)
        {
            string path = ArtPath + file; AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 256; importer.isReadable = true; importer.alphaIsTransparency = true; importer.mipmapEnabled = false;
            importer.maxTextureSize = 2048; importer.textureCompression = TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            SpriteDataProviderFactories factories = new SpriteDataProviderFactories(); factories.Init();
            ISpriteEditorDataProvider provider = factories.GetSpriteEditorDataProviderFromObject(importer); provider.InitSpriteEditorDataProvider();
            Dictionary<string, GUID> previous = provider.GetSpriteRects().ToDictionary(item => item.name, item => item.spriteID);
            Color32[] pixels = texture.GetPixels32(); List<SpriteRect> rects = new List<SpriteRect>();
            // The approved prop sheet has uneven rows; explicit regions keep neighbouring silhouettes out.
            Rect[] propRegions = {
                new Rect(0, 0, 420, 300), new Rect(435, 0, 395, 355), new Rect(830, 0, 431, 305),
                new Rect(0, 300, 420, 425), new Rect(440, 360, 380, 350), new Rect(835, 305, 426, 410),
                new Rect(0, 725, 465, 522), new Rect(465, 700, 355, 547), new Rect(825, 719, 436, 528)
            };
            for (int i = 0; i < names.Length; i++)
            {
                int col = i % columns, row = rows - 1 - i / columns;
                int startX = col * texture.width / columns, endX = (col + 1) * texture.width / columns - 1;
                int startY = row * texture.height / rows, endY = (row + 1) * texture.height / rows - 1;
                if (file == "Props.png")
                {
                    Rect region = propRegions[i];
                    startX = Mathf.FloorToInt(region.x * texture.width / 1261f);
                    endX = Mathf.Min(texture.width - 1, Mathf.CeilToInt(region.xMax * texture.width / 1261f) - 1);
                    startY = Mathf.Max(0, texture.height - Mathf.CeilToInt(region.yMax * texture.height / 1247f));
                    endY = Mathf.Min(texture.height - 1, texture.height - Mathf.FloorToInt(region.y * texture.height / 1247f) - 1);
                }
                int left = endX, right = startX, bottom = endY, top = startY;
                for (int y = startY; y <= endY; y++) for (int x = startX; x <= endX; x++)
                    if (pixels[y * texture.width + x].a > 35) { left = Mathf.Min(left, x); right = Mathf.Max(right, x); bottom = Mathf.Min(bottom, y); top = Mathf.Max(top, y); }
                if (right < left || top < bottom) throw new InvalidOperationException("Empty prison sprite: " + names[i]);
                rects.Add(new SpriteRect { name = names[i], rect = new Rect(left, bottom, right - left + 1, top - bottom + 1), pivot = new Vector2(.5f, 0), alignment = SpriteAlignment.Custom, spriteID = previous.ContainsKey(names[i]) ? previous[names[i]] : GUID.Generate() });
            }
            provider.SetSpriteRects(rects.ToArray());
            provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(rects.Select(item => new SpriteNameFileIdPair(item.name, item.spriteID)));
            provider.Apply(); importer.SaveAndReimport();
            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToDictionary(item => item.name);
        }
    }
}
