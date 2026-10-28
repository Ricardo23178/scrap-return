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
    public static class ControlRoomBuilder
    {
        public const string ScenePath = "Assets/Scenes/ControlRoom.unity";
        private const string Art = "Assets/Art/Room03Backdrop/";
        private static readonly Color Ink = new Color(0.065f, 0.105f, 0.115f, 0.98f);
        private static readonly Color Brass = new Color(0.65f, 0.51f, 0.31f);
        private static readonly Color Paper = new Color(0.88f, 0.85f, 0.73f);
        private static Sprite Circle { get { return AssetDatabase.LoadAssetAtPath<Sprite>(Art + "UiDisc.png"); } }
        private static Sprite Square { get { return AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Prototype/Square.png"); } }

        [MenuItem("Tools/Robot Escape/Build Control Room")]
        public static void BuildRoom()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play mode before building.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save the current scene before building.");
            Sprite background = ImportBackground();
            Dictionary<string, Sprite> art = ImportAtlas();
            PrepareUiDisc();
            Dictionary<string, Sprite> old = AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Room02Backdrop/WatchProps.png").OfType<Sprite>().ToDictionary(s => s.name);
            Dictionary<string, Sprite> first = AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Room01Backdrop/PropsAtlas.png").OfType<Sprite>().ToDictionary(s => s.name);
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            ControlRoomController room = new GameObject("ControlRoomController").AddComponent<ControlRoomController>();
            Illustration(room.transform, "Control Room Background", background, Vector2.zero, new Vector2(16, 9), -100, false);
            Camera camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
            camera.tag = "MainCamera"; camera.transform.position = new Vector3(0, 0, -10);
            camera.orthographic = true; camera.orthographicSize = 4.5f;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Ink;
            room.robot = ((GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Robot.prefab"))).GetComponent<RobotWalker>();
            room.robot.transform.position = new Vector3(-5.6f, -2.8f, 0);
            room.robot.minWalk = new Vector2(-7.4f, -3.6f); room.robot.maxWalk = new Vector2(7.4f, -1.05f);
            room.pressureCabinet = Item(room, "Pneumatic Regulator", ObjectKind.PressureCabinet, art["Pressure"], new Vector2(-4.35f, -1.2f), new Vector2(2.15f, 2.8f), new Vector2(-4.3f, -1.85f));
            room.console = Item(room, "Control Console", ObjectKind.ControlConsole, art["Console"], new Vector2(-0.3f, -1.2f), new Vector2(3.0f, 2.65f), new Vector2(-0.3f, -1.95f));
            WorldRect(room.console.transform, "Recess", new Vector2(0, 1.72f), new Vector2(1.85f, 1.25f), 47, new Color(0.03f, 0.06f, 0.065f));
            for (int row = 0; row < 2; row++) for (int col = 0; col < 3; col++)
                WorldRect(room.console.transform, "Interlock" + row + col, new Vector2(-0.55f + col * 0.55f, 1.5f + row * 0.43f), new Vector2(0.47f, 0.3f), 48, col == 0 ? Brass : new Color(0.25f, 0.32f, 0.31f));
            room.coverLeaf = Illustration(room.console.transform, "Inspection Shutter", art["Cover"], new Vector2(0, 1.09f), new Vector2(1.92f, 1.3f), 53, false).transform;
            WorldRect(room.console.transform, "Left Guide", new Vector2(-1.03f, 2), new Vector2(0.07f, 2.5f), 46, Brass);
            WorldRect(room.console.transform, "Right Guide", new Vector2(1.03f, 2), new Vector2(0.07f, 2.5f), 46, Brass);
            room.trace = Item(room, "Bent Antenna Mark", ObjectKind.ControlTrace, art["Note"], new Vector2(1.75f, 0.65f), new Vector2(0.45f, 0.61f), new Vector2(1.55f, -1.9f));
            room.archive = Item(room, "Archive Reader", ObjectKind.ControlArchive, first["Console"], new Vector2(3.05f, -1.25f), new Vector2(1.3f, 1.95f), new Vector2(3.0f, -1.9f));
            room.exit = Item(room, "Isolated Corridor", ObjectKind.ControlExit, old["Door"], new Vector2(6.15f, -1.3f), new Vector2(1.5f, 2.8f), new Vector2(6.1f, -1.8f));
            room.doorLeaf = room.exit.transform.Find("Illustration");
            Vector2 openingSize = room.exit.GetComponentInChildren<SpriteRenderer>().bounds.size;
            WorldRect(room.exit.transform, "Dark Corridor", Vector2.up * openingSize.y * 0.5f, openingSize, 35, new Color(0.035f, 0.055f, 0.055f));
            room.exitLever = Item(room, "Corridor Switch", ObjectKind.ControlExitLever, old["Lever"], new Vector2(4.65f, -0.15f), new Vector2(0.55f, 0.95f), new Vector2(4.65f, -1.8f));
            room.leverHandle = room.exitLever.transform.Find("Illustration");
            room.consoleLight = Dot(room.console.transform, "Console Contact Light", new Vector2(0.85f, 0.72f), 0.1f);
            room.exitLight = Dot(room.exit.transform, "Exit Lamp", new Vector2(0, 2.66f), 0.13f);
            Illustration(room.transform, "Old Parts", first["Crate"], new Vector2(-6.5f, -1.4f), new Vector2(1.15f, 1.0f), 30);
            ApplyLayout(room);
            BuildUi(room, art);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new InvalidOperationException("Could not save ControlRoom.");
            Scene watchScene = EditorSceneManager.OpenScene(WatchRoomBuilder.ScenePath);
            WatchRoomController watch = UnityEngine.Object.FindObjectOfType<WatchRoomController>();
            watch.nextRoomScene = "ControlRoom"; EditorUtility.SetDirty(watch);
            EditorSceneManager.MarkSceneDirty(watchScene); EditorSceneManager.SaveScene(watchScene);
            List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
            int existing = scenes.FindIndex(entry => entry.path == ScenePath);
            if (existing < 0) scenes.Add(new EditorBuildSettingsScene(ScenePath, true)); else scenes[existing].enabled = true;
            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(RobotEscapeBuilder.DemoPath);
            Debug.Log("ROBOT_ESCAPE_CONTROL_ROOM_BUILT");
        }

        [MenuItem("Tools/Robot Escape/Arrange Control Room")]
        public static void ArrangeSavedRoom()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play mode before arranging.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save the current scene before arranging.");
            if (SceneManager.GetActiveScene().path != ScenePath) EditorSceneManager.OpenScene(ScenePath);
            ControlRoomController room = UnityEngine.Object.FindObjectOfType<ControlRoomController>();
            if (room == null) throw new InvalidOperationException("ControlRoomController is missing.");
            ApplyLayout(room);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            if (!EditorSceneManager.SaveScene(SceneManager.GetActiveScene())) throw new InvalidOperationException("Could not save the arranged room.");
            Debug.Log("ROBOT_ESCAPE_LAYOUT_APPLIED");
        }

        private static void ApplyLayout(ControlRoomController room)
        {
            Place(room.pressureCabinet, new Vector2(-5.45f, -0.95f), new Vector2(-5.0f, -1.75f), -0.6f);
            Place(room.console, new Vector2(-1.45f, -2.05f), new Vector2(-1.55f, -2.95f), 0.7f);
            Place(room.archive, new Vector2(3.65f, -1.45f), new Vector2(3.2f, -2.25f), -1.0f);
            Place(room.trace, new Vector2(1.75f, 0.65f), new Vector2(1.55f, -1.9f), -5.0f);
            Transform parts = room.transform.Find("Old Parts");
            if (parts != null)
            {
                parts.position = new Vector3(-3.7f, -1.6f, 0);
                parts.rotation = Quaternion.Euler(0, 0, -4);
            }
            DressEdges(room);
        }

        private static void DressEdges(ControlRoomController room)
        {
            Dictionary<string, Sprite> first = AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Room01Backdrop/PropsAtlas.png").OfType<Sprite>().ToDictionary(s => s.name);
            Dictionary<string, Sprite> old = AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Room02Backdrop/WatchProps.png").OfType<Sprite>().ToDictionary(s => s.name);
            Transform edges = room.transform.Find("Edge Dressing");
            if (edges == null) { edges = new GameObject("Edge Dressing").transform; edges.SetParent(room.transform, false); }
            Decor(edges, "Left Locker", first["Locker"], new Vector2(-7.2f, -1.05f), new Vector2(1.05f, 2.1f), 0.6f, 25);
            Decor(edges, "Side Workbench", first["Workbench"], new Vector2(-7.1f, -2.45f), new Vector2(1.55f, 0.98f), -2, 55);
            Decor(edges, "Workbench Radio", old["Radio"], new Vector2(-7.05f, -1.62f), new Vector2(0.57f, 0.5f), -0.8f, 56);
            Decor(edges, "Wall Tools", first["ToolRack"], new Vector2(-5.35f, 2.25f), new Vector2(1.45f, 0.8f), -2, 15);
            Decor(edges, "Rear Spare Crate", first["Crate"], new Vector2(1.45f, -1.05f), new Vector2(1.05f, 0.8f), 2.5f, 25);
            Decor(edges, "Idle Reserve Pump", first["Pump"], new Vector2(2.3f, -0.95f), new Vector2(0.9f, 1.35f), -0.5f, 26);
            Decor(edges, "Right Filing Cabinet", old["Cabinet"], new Vector2(7.48f, -1.35f), new Vector2(0.88f, 1.6f), -0.8f, 25);
            Decor(edges, "Right Wall Clock", old["Clock"], new Vector2(7.05f, 2.35f), new Vector2(0.62f, 0.62f), -3, 15);
            Decor(edges, "Pinned Folder", old["Clipboard"], new Vector2(7.4f, 0.75f), new Vector2(0.45f, 0.65f), 9, 16);
            Decor(edges, "Near Left Crate", first["Crate"], new Vector2(-7.55f, -4.2f), new Vector2(1.45f, 1.15f), -4, 85);
            Decor(edges, "Near Right Scrap", first["Scrap"], new Vector2(7.1f, -4.08f), new Vector2(2.0f, 1.1f), 3, 85);
        }

        private static void Decor(Transform parent, string name, Sprite sprite, Vector2 foot, Vector2 size, float degrees, int order)
        {
            Transform existing = parent.Find(name);
            SpriteRenderer renderer = existing != null ? existing.GetComponent<SpriteRenderer>() : Illustration(parent, name, sprite, foot, size, order);
            renderer.sprite = sprite; renderer.sortingOrder = order; renderer.color = new Color(0.88f, 0.88f, 0.84f);
            renderer.transform.localPosition = foot;
            renderer.transform.localRotation = Quaternion.Euler(0, 0, degrees);
            float scale = Mathf.Min(size.x / sprite.bounds.size.x, size.y / sprite.bounds.size.y);
            renderer.transform.localScale = new Vector3(scale, scale, 1);
        }

        private static void Place(RoomInteractable item, Vector2 foot, Vector2 stand, float degrees)
        {
            item.transform.position = foot;
            item.transform.rotation = Quaternion.Euler(0, 0, degrees);
            item.standPoint.position = stand;
        }

        private static void BuildUi(ControlRoomController room, Dictionary<string, Sprite> art)
        {
            Canvas canvas = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)).GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900); scaler.matchWidthOrHeight = 0.5f;
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            Transform ui = canvas.transform;
            Image badge = Box(ui, "Room Badge", new Vector2(0, 1), new Vector2(140, -30), new Vector2(270, 48), Ink);
            Label(badge.transform, "Room Name", "03  上层控制室", Vector2.zero, new Vector2(260, 40), 25);
            room.restartButton = Button(ui, "Restart Room", "重新开始", new Vector2(1, 1), new Vector2(-85, -30), new Vector2(140, 42));
            room.returnTitleButton = Button(ui, "Return Title", "返回标题", new Vector2(1, 1), new Vector2(-240, -30), new Vector2(140, 42));
            Label(ui, "Controls", "左键交互   Esc 返回", new Vector2(-590, -423), new Vector2(380, 28), 17).color = new Color(0.77f, 0.77f, 0.65f, 0.7f);

            room.pressurePanel = Modal(ui, "Pressure Panel", new Vector2(1100, 650));
            Transform pressure = room.pressurePanel.transform.Find("Card");
            Label(pressure, "Title", "气动检修系统", new Vector2(0, 265), new Vector2(900, 50), 31);
            Image machine = Box(pressure, "Equipment", new Vector2(0.5f, 0.5f), new Vector2(-345, 0), new Vector2(255, 365), Color.white);
            machine.sprite = art["Pressure"]; machine.preserveAspect = true;
            room.leftNeedle = Gauge(pressure, "Main Line", new Vector2(-5, 105), "主路", 2);
            room.rightNeedle = Gauge(pressure, "Return Line", new Vector2(285, 105), "旁路", 1);
            room.inletButton = Wheel(pressure, "Inlet", new Vector2(-5, -130), "进气", out room.inletWheel);
            room.bleedButton = Wheel(pressure, "Bleed", new Vector2(285, -130), "泄压", out room.bleedWheel);
            room.pressureReadout = Label(pressure, "Status", "", new Vector2(140, -28), new Vector2(560, 75), 20);
            room.resetPressureButton = Button(pressure, "Reset", "复位", new Vector2(0.5f, 0.5f), new Vector2(-345, -265), new Vector2(190, 48));
            room.closePressureButton = Button(pressure, "Close", "返回房间", new Vector2(0.5f, 0.5f), new Vector2(285, -265), new Vector2(200, 48));

            room.boardPanel = Modal(ui, "Interlock Panel", new Vector2(1080, 650));
            Transform board = room.boardPanel.transform.Find("Card");
            Label(board, "Title", "主控机械联锁", new Vector2(0, 265), new Vector2(900, 50), 31);
            Box(board, "Metal Frame", new Vector2(0.5f, 0.5f), new Vector2(-145, 8), new Vector2(430, 430), Brass);
            Image area = Box(board, "Board Area", new Vector2(0.5f, 0.5f), new Vector2(-145, 8), new Vector2(400, 400), new Color(0.035f, 0.065f, 0.07f));
            room.boardArea = area.rectTransform;
            for (int row = 0; row < 4; row++) for (int col = 0; col < 4; col++)
                Box(area.transform, "Cell" + row + col, new Vector2(0, 1), new Vector2(col * 100 + 50, -row * 100 - 50), new Vector2(96, 96), new Color(0.1f, 0.15f, 0.16f));
            Image contact = Box(board, "Contact", new Vector2(0.5f, 0.5f), new Vector2(75, 58), new Vector2(25, 70), Brass);
            Label(contact.transform, "Contact Mark", "›", Vector2.zero, new Vector2(30, 50), 32);
            room.blocks = new RectTransform[5]; room.blockImages = new Image[5];
            Color[] metals = { new Color(0.69f, 0.42f, 0.22f), new Color(0.27f, 0.43f, 0.41f), new Color(0.33f, 0.4f, 0.46f), new Color(0.41f, 0.43f, 0.31f), new Color(0.38f, 0.32f, 0.27f) };
            for (int i = 0; i < 5; i++)
            {
                Image block = Box(area.transform, "Module" + (char)('A' + i), new Vector2(0, 1), Vector2.zero, new Vector2(192, 92), metals[i]);
                block.raycastTarget = true; room.blocks[i] = block.rectTransform; room.blockImages[i] = block;
                Outline edge = block.gameObject.AddComponent<Outline>(); edge.effectColor = new Color(0.055f, 0.075f, 0.07f); edge.effectDistance = new Vector2(3, -3);
                SlideBlockHandle handle = block.gameObject.AddComponent<SlideBlockHandle>(); handle.controller = room; handle.blockIndex = i;
                Label(block.transform, "Stamp", i == 0 ? "◇" : ((char)('A' + i)).ToString(), Vector2.zero, new Vector2(70, 60), 25).color = Paper;
                for (int corner = 0; corner < 4; corner++)
                {
                    Vector2 anchor = new Vector2(corner % 2, corner / 2);
                    Image screw = Box(block.transform, "Rivet" + corner, anchor, new Vector2(anchor.x < 0.5f ? 10 : -10, anchor.y < 0.5f ? 10 : -10), new Vector2(7, 7), Brass);
                    screw.sprite = Circle;
                }
            }
            Image sideArt = Box(board, "Contact Housing", new Vector2(0.5f, 0.5f), new Vector2(335, 132), new Vector2(250, 120), new Color(0.04f, 0.08f, 0.085f));
            Label(sideArt.transform, "Contact Icon", "◇  ━  ●", Vector2.zero, new Vector2(240, 80), 35).color = Brass;
            room.boardReadout = Label(board, "Status", "", new Vector2(320, -3), new Vector2(330, 110), 23);
            room.moveBackButton = Button(board, "Move Back", "←", new Vector2(0.5f, 0.5f), new Vector2(250, -115), new Vector2(90, 55));
            room.moveForwardButton = Button(board, "Move Forward", "→", new Vector2(0.5f, 0.5f), new Vector2(390, -115), new Vector2(90, 55));
            room.resetBoardButton = Button(board, "Reset", "复位", new Vector2(0.5f, 0.5f), new Vector2(-145, -265), new Vector2(190, 48));
            room.closeBoardButton = Button(board, "Close", "返回房间", new Vector2(0.5f, 0.5f), new Vector2(325, -265), new Vector2(200, 48));

            room.inspectPanel = Modal(ui, "Archive Panel", new Vector2(1000, 560));
            Transform inspect = room.inspectPanel.transform.Find("Card");
            room.inspectTitle = Label(inspect, "Title", "", new Vector2(0, 210), new Vector2(850, 55), 31);
            room.inspectIllustration = Box(inspect, "Evidence Art", new Vector2(0.5f, 0.5f), new Vector2(-315, 0), new Vector2(245, 295), Color.white);
            room.inspectIllustration.preserveAspect = true;
            room.inspectText = Label(inspect, "Text", "", new Vector2(135, 5), new Vector2(590, 290), 25); room.inspectText.alignment = TextAnchor.MiddleLeft;
            room.closeInspectButton = Button(inspect, "Close", "收起", new Vector2(0.5f, 0.5f), new Vector2(170, -205), new Vector2(210, 50));

            room.chapterPanel = Modal(ui, "Chapter Complete", new Vector2(1040, 560));
            Transform chapter = room.chapterPanel.transform.Find("Card");
            Label(chapter, "Title", "停机之后", new Vector2(0, 205), new Vector2(900, 60), 42);
            room.chapterText = Label(chapter, "Story", "", new Vector2(0, 15), new Vector2(900, 250), 27);
            Button replay = Button(chapter, "Replay Room", "重玩本间", new Vector2(0.5f, 0.5f), new Vector2(-150, -200), new Vector2(230, 54));
            UnityEventTools.AddPersistentListener(replay.onClick, room.Restart);
            Button title = Button(chapter, "Back To Title", "返回标题", new Vector2(0.5f, 0.5f), new Vector2(150, -200), new Vector2(230, 54));
            UnityEventTools.AddPersistentListener(title.onClick, room.ReturnToTitle);
            foreach (GameObject panel in new[] { room.pressurePanel, room.boardPanel, room.inspectPanel, room.chapterPanel }) panel.SetActive(false);
        }

        private static RectTransform Gauge(Transform parent, string name, Vector2 position, string title, int target)
        {
            Image rim = Box(parent, name, new Vector2(0.5f, 0.5f), position, new Vector2(190, 190), Brass); rim.sprite = Circle;
            Image face = Box(rim.transform, "Face", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(172, 172), new Color(0.8f, 0.77f, 0.64f)); face.sprite = Circle;
            for (int i = 0; i < 3; i++)
            {
                float angle = 60 - 60 * i; Vector2 point = Quaternion.Euler(0, 0, angle) * (Vector3.up * 70);
                Image tick = Box(face.transform, "Tick" + i, new Vector2(0.5f, 0.5f), point, new Vector2(i == target ? 10 : 4, 18), i == target ? new Color(0.35f, 0.43f, 0.29f) : new Color(0.35f, 0.34f, 0.27f));
                tick.rectTransform.localRotation = Quaternion.Euler(0, 0, angle);
            }
            Label(face.transform, "Name", title, new Vector2(0, -38), new Vector2(100, 35), 19).color = new Color(0.24f, 0.27f, 0.23f);
            Image needle = Box(face.transform, "Needle", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(5, 65), new Color(0.32f, 0.19f, 0.12f));
            needle.rectTransform.pivot = new Vector2(0.5f, 0); needle.rectTransform.localRotation = Quaternion.Euler(0, 0, 60);
            Image cap = Box(face.transform, "Cap", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(15, 15), Brass); cap.sprite = Circle;
            return needle.rectTransform;
        }

        private static Button Wheel(Transform parent, string name, Vector2 position, string label, out RectTransform wheel)
        {
            Image image = Box(parent, name, new Vector2(0.5f, 0.5f), position, new Vector2(125, 125), new Color(0.4f, 0.27f, 0.17f));
            image.sprite = Circle; image.raycastTarget = true; Button button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            Image disk = Box(image.transform, "Wheel", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(100, 100), Brass); disk.sprite = Circle; wheel = disk.rectTransform;
            Image inset = Box(wheel, "Inset", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(78, 78), Ink); inset.sprite = Circle;
            for (int i = 0; i < 3; i++)
            {
                Image spoke = Box(wheel, "Spoke" + i, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(8, 91), Brass);
                spoke.rectTransform.localRotation = Quaternion.Euler(0, 0, i * 60);
            }
            Image mark = Box(wheel, "Notch", new Vector2(0.5f, 0.5f), new Vector2(0, 49), new Vector2(14, 9), Paper);
            Label(image.transform, "Label", label, new Vector2(0, -88), new Vector2(150, 40), 23);
            return button;
        }

        private static RoomInteractable Item(ControlRoomController room, string name, ObjectKind kind, Sprite sprite, Vector2 foot, Vector2 fit, Vector2 stand)
        {
            RoomInteractable item = new GameObject(name, typeof(BoxCollider2D)).AddComponent<RoomInteractable>();
            item.kind = kind; item.transform.SetParent(room.transform, false); item.transform.position = foot;
            SpriteRenderer image = Illustration(item.transform, "Illustration", sprite, Vector2.zero, fit, 45);
            BoxCollider2D hit = item.GetComponent<BoxCollider2D>(); hit.size = image.bounds.size; hit.offset = Vector2.up * hit.size.y * 0.5f;
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
        private static SpriteRenderer WorldRect(Transform parent, string name, Vector2 center, Vector2 size, int order, Color color)
        {
            SpriteRenderer renderer = Illustration(parent, name, Square, center, size, order, false); renderer.color = color; return renderer;
        }
        private static SpriteRenderer Dot(Transform parent, string name, Vector2 position, float size)
        {
            return Illustration(parent, name, Circle, position, new Vector2(size, size), 60, false);
        }
        private static Image Box(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size, Color color)
        {
            Image image = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            RectTransform rect = image.rectTransform; rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = anchor; rect.anchoredPosition = position; rect.sizeDelta = size;
            image.color = color; image.raycastTarget = false; return image;
        }
        private static Text Label(Transform parent, string name, string value, Vector2 position, Vector2 size, int fontSize)
        {
            Text text = new GameObject(name, typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            RectTransform rect = text.rectTransform; rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position; rect.sizeDelta = size; text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize; text.text = value; text.color = Paper; text.alignment = TextAnchor.MiddleCenter; text.raycastTarget = false; return text;
        }
        private static Button Button(Transform parent, string name, string value, Vector2 anchor, Vector2 position, Vector2 size)
        {
            Image image = Box(parent, name, anchor, position, size, new Color(0.13f, 0.2f, 0.21f)); image.raycastTarget = true;
            Outline outline = image.gameObject.AddComponent<Outline>(); outline.effectColor = Brass; outline.effectDistance = new Vector2(1, -1);
            Button button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image; Label(image.transform, "Text", value, Vector2.zero, size - new Vector2(8, 4), 22); return button;
        }
        private static GameObject Modal(Transform parent, string name, Vector2 size)
        {
            Image overlay = Box(parent, name, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0.01f, 0.025f, 0.03f, 0.78f));
            overlay.rectTransform.anchorMin = Vector2.zero; overlay.rectTransform.anchorMax = Vector2.one; overlay.rectTransform.offsetMin = overlay.rectTransform.offsetMax = Vector2.zero; overlay.raycastTarget = true;
            Image card = Box(overlay.transform, "Card", new Vector2(0.5f, 0.5f), Vector2.zero, size, Ink);
            Outline outline = card.gameObject.AddComponent<Outline>(); outline.effectColor = Brass; outline.effectDistance = new Vector2(2, -2); return overlay.gameObject;
        }
        private static TextureImporter Configure(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("Import Room03 PNG assets first: " + path);
            importer.textureType = TextureImporterType.Sprite; importer.spritePixelsPerUnit = 256; importer.alphaIsTransparency = true; importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed; importer.maxTextureSize = 2048; importer.filterMode = FilterMode.Bilinear;
            TextureImporterSettings settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings); settings.spriteMeshType = SpriteMeshType.FullRect; importer.SetTextureSettings(settings); return importer;
        }
        private static Sprite ImportBackground()
        {
            TextureImporter importer = Configure(Art + "ControlRoom.png"); importer.spriteImportMode = SpriteImportMode.Single; importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(Art + "ControlRoom.png");
        }
        private static void PrepareUiDisc()
        {
            string path = Art + "UiDisc.png";
            if (!System.IO.File.Exists(path))
            {
                Texture2D texture = new Texture2D(256, 256, TextureFormat.RGBA32, false);
                Color[] pixels = new Color[256 * 256];
                for (int y = 0; y < 256; y++) for (int x = 0; x < 256; x++)
                {
                    float radius = new Vector2(x - 127.5f, y - 127.5f).magnitude;
                    pixels[y * 256 + x] = new Color(1, 1, 1, Mathf.Clamp01(126.5f - radius));
                }
                texture.SetPixels(pixels); texture.Apply(); System.IO.File.WriteAllBytes(path, texture.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            }
            TextureImporter importer = Configure(path); importer.spriteImportMode = SpriteImportMode.Single; importer.SaveAndReimport();
        }
        private static Dictionary<string, Sprite> ImportAtlas()
        {
            string path = Art + "ControlProps.png"; string[] names = { "Pressure", "Console", "Cover", "Note" };
            TextureImporter importer = Configure(path); importer.spriteImportMode = SpriteImportMode.Multiple; importer.isReadable = true; importer.SaveAndReimport();
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path); Color32[] pixels = texture.GetPixels32();
            SpriteDataProviderFactories factories = new SpriteDataProviderFactories(); factories.Init(); ISpriteEditorDataProvider provider = factories.GetSpriteEditorDataProviderFromObject(importer); provider.InitSpriteEditorDataProvider();
            Dictionary<string, GUID> ids = provider.GetSpriteRects().ToDictionary(rect => rect.name, rect => rect.spriteID); List<SpriteRect> rects = new List<SpriteRect>();
            RectInt[] regions = { new RectInt(0, 0, 640, 700), new RectInt(640, 0, 640, 700), new RectInt(0, 700, 720, 580), new RectInt(720, 700, 560, 580) };
            for (int index = 0; index < names.Length; index++)
            {
                RectInt region = regions[index];
                int x0 = region.xMin * texture.width / 1280, x1 = region.xMax * texture.width / 1280;
                int y0 = texture.height - region.yMax * texture.height / 1280, y1 = texture.height - region.yMin * texture.height / 1280;
                int left = x1, right = x0, bottom = y1, top = y0;
                for (int y = y0; y < y1; y++) for (int x = x0; x < x1; x++)
                    if (pixels[y * texture.width + x].a > 30) { left = Mathf.Min(left, x); right = Mathf.Max(right, x); bottom = Mathf.Min(bottom, y); top = Mathf.Max(top, y); }
                string name = names[index]; if (left > right || bottom > top) throw new InvalidOperationException("Empty sprite region: " + name);
                rects.Add(new SpriteRect { name = name, rect = new Rect(left, bottom, right - left + 1, top - bottom + 1), alignment = SpriteAlignment.BottomCenter, pivot = new Vector2(0.5f, 0), spriteID = ids.ContainsKey(name) ? ids[name] : GUID.Generate() });
            }
            provider.SetSpriteRects(rects.ToArray()); provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(rects.Select(rect => new SpriteNameFileIdPair(rect.name, rect.spriteID)));
            provider.Apply(); importer.SaveAndReimport(); return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToDictionary(sprite => sprite.name);
        }
    }
}
