using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RobotEscape.Editor
{
    public static class MechanicalCityStory
    {
        private const string Art = "Assets/Art/MechanicalCityStory/";
        private static readonly Color Ink = new Color(0.08f, 0.13f, 0.14f, 0.97f);
        private static readonly Color Brass = new Color(0.62f, 0.43f, 0.22f);
        private static Dictionary<string, Sprite> props, plot;
        private static Sprite square;

        [MenuItem("Tools/Robot Escape/Apply Mechanical City Story")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play mode before migrating scenes.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save open scenes before migrating; no changes were discarded.");
            square = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Prototype/Square.png");
            props = ImportProps();
            plot = ImportPlot();
            First(); Second(); Third();
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(RobotEscapeBuilder.DemoPath);
            Debug.Log("MECHANICAL_CITY_STORY_APPLIED");
        }

        private static void First()
        {
            EditorSceneManager.OpenScene(RobotEscapeBuilder.DemoPath);
            RobotEscapeRoom room = UnityEngine.Object.FindObjectOfType<RobotEscapeRoom>();
            SetBackdrop(room.transform.Find("Hand Painted Background"), "Scrapyard.png");
            Sprite bg = LoadBackground("Scrapyard.png");
            room.menuPanel.GetComponent<Image>().sprite = bg;
            room.storyPanel.GetComponent<Image>().sprite = bg;
            SetText("Canvas/MenuPanel/GameTitle", "机械城归途");
            SetText("Canvas/MenuPanel/Subtitle", "城门之外，齿轮仍在转动。");
            SetText("Canvas/StoryPanel/Chapter", "序章 · 城外");
            ReplaceLabels(new Dictionary<string, string> { { "01   维修间", "01  城外废料场" }, { "01  维修间", "01  城外废料场" }, { "01 维修间", "01  城外废料场" } });
            room.nextRoomScene = "WatchRoom";
            room.inventoryButton.gameObject.SetActive(false);
            room.battery.gameObject.SetActive(false);
            room.terminal.gameObject.SetActive(false);
            room.robot.transform.position = new Vector3(-5.15f, -2.85f, 0);
            room.robot.minWalk = new Vector2(-5.5f, -3.25f);
            room.robot.maxWalk = new Vector2(6.55f, -1.55f);
            room.trash.standPoint.position = new Vector3(-4.9f, -2.85f, 0);
            Transform details = GameObject.Find("Room Details").transform;
            foreach (string name in new[] { "Storage Locker", "Old Workbench", "Wall Tool Rack", "Worn Maintenance Marks" })
                if (details.Find(name) != null) details.Find(name).gameObject.SetActive(false);
            ScrapAssembly assembly = room.GetComponent<ScrapAssembly>();
            if (assembly == null) assembly = room.gameObject.AddComponent<ScrapAssembly>();
            room.assembly = assembly; assembly.room = room;
            Transform story = Child(room.transform, "Scrapyard Story");
            assembly.leg = Item(story, "Recovered Leg", ObjectKind.LooseLeg, props["Leg"], new Vector2(-4.3f, -2.5f), new Vector2(0.4f, 0.6f), new Vector2(-4.9f, -2.85f));
            assembly.arm = Item(story, "Recovered Arm", ObjectKind.LooseArm, props["Arm"], new Vector2(0.9f, -2.18f), new Vector2(0.48f, 0.65f), new Vector2(0.8f, -2.85f));
            assembly.armCrate = details.Find("Spare Parts Crate").GetComponent<RoomInteractable>();
            assembly.armCrate.kind = ObjectKind.ScrapCrate;
            Prop(assembly.armCrate, props["Crate"], new Vector2(0.7f, -1.95f), new Vector2(1.25f, 0.95f), new Vector2(0.6f, -2.65f));
            assembly.leg.transform.position += Vector3.back * 0.2f;
            assembly.arm.transform.position += Vector3.back * 0.2f;
            Transform self = Child(room.robot.transform, "Assembly Socket");
            self.localPosition = new Vector3(0, 0, -0.3f);
            assembly.self = self.GetComponent<RoomInteractable>();
            if (assembly.self == null) assembly.self = self.gameObject.AddComponent<RoomInteractable>();
            assembly.self.kind = ObjectKind.RobotBody;
            assembly.self.standPoint = Child(self, "Stand Here");
            assembly.self.standPoint.localPosition = Vector3.zero;
            BoxCollider2D hit = self.GetComponent<BoxCollider2D>();
            if (hit == null) hit = self.gameObject.AddComponent<BoxCollider2D>();
            hit.offset = new Vector2(0, 0.65f); hit.size = new Vector2(0.95f, 1.35f);
            assembly.damagedBody = SpritePiece(room.robot.body, "Damaged Body", props["Torso"], new Vector2(0, 0.3f), new Vector2(1.08f, 1.0f), 170).gameObject;
            Transform legs = Child(room.robot.body, "Repaired Legs");
            for (int i = 0; i < 2; i++)
            {
                SpriteRenderer limb = SpritePiece(legs, "Leg " + i, props["Leg"], new Vector2(i == 0 ? -0.17f : 0.17f, 0), new Vector2(0.32f, 0.48f), 169);
                limb.flipX = i == 0;
            }
            assembly.repairedLegs = legs.gameObject;
            Transform canvas = GameObject.Find("Canvas").transform;
            if (canvas.Find("Controls") != null) canvas.Find("Controls").gameObject.SetActive(false);
            assembly.legButton = Slot(canvas, "Leg Inventory", new Vector2(65, 60), props["Leg"], out assembly.legIcon);
            assembly.armButton = Slot(canvas, "Arm Inventory", new Vector2(165, 60), props["Arm"], out assembly.armIcon);
            Text controls = Label(canvas, "Story Controls", "左键交互   Esc 返回", new Vector2(0, 0), new Vector2(385, 18), new Vector2(420, 30), 17);
            controls.color = new Color(0.8f, 0.77f, 0.62f, 0.7f);
            room.inspectPanel.transform.Find("Description").gameObject.SetActive(false);
            room.shakeButton.GetComponentInChildren<Text>().text = "摇一摇";
            assembly.leg.gameObject.SetActive(false); assembly.arm.gameObject.SetActive(false);
            assembly.damagedBody.SetActive(true); assembly.repairedLegs.SetActive(false);
            room.robot.illustration.enabled = false;
            room.menuPanel.transform.SetAsLastSibling();
            room.storyPanel.transform.SetAsLastSibling();
            Save(room);
        }

        private static void Second()
        {
            EditorSceneManager.OpenScene(WatchRoomBuilder.ScenePath);
            WatchRoomController room = UnityEngine.Object.FindObjectOfType<WatchRoomController>();
            SetBackdrop(room.transform.Find("Watch Office Background"), "CityGate.png");
            foreach (string name in new[] { "Writing Desk", "Filing Cabinet", "Stopped Clock" })
                if (room.transform.Find(name) != null) room.transform.Find(name).gameObject.SetActive(false);
            room.nextRoomScene = "ControlRoom";
            Prop(room.frequencyCard, props["Helmet"], new Vector2(-4.7f, -2.25f), new Vector2(0.7f, 0.55f), new Vector2(-4.6f, -2.85f));
            room.frequencyCard.kind = ObjectKind.DisguiseHat;
            Prop(room.dutyLog, props["Coat"], new Vector2(-0.65f, -1.65f), new Vector2(0.8f, 0.9f), new Vector2(-0.7f, -2.5f));
            room.dutyLog.kind = ObjectKind.DisguiseCoat;
            Prop(room.npc, props["Guard"], new Vector2(3.7f, -1.6f), new Vector2(1.1f, 2.6f), new Vector2(2.9f, -2.45f));
            room.npc.kind = ObjectKind.GateGuard;
            Prop(room.photo, props["Photo"], new Vector2(-2.8f, -1.72f), new Vector2(0.55f, 0.5f), new Vector2(-2.8f, -2.5f));
            Prop(room.notice, props["Guard"], new Vector2(1.55f, 0.05f), new Vector2(0.65f, 1.1f), new Vector2(1.5f, -2.0f));
            room.radio.transform.position = new Vector3(-2.3f, -0.25f, 0);
            room.radio.standPoint.position = new Vector3(-2.3f, -1.9f, 0);
            room.robot.transform.position = new Vector3(-5.8f, -2.85f, 0);
            room.robot.minWalk = new Vector2(-6.6f, -3.45f); room.robot.maxWalk = new Vector2(6.6f, -1.4f);
            room.helmetOverlay = SpritePiece(room.robot.body, "Worn Guard Helmet", props["Helmet"], new Vector2(0, 1.02f), new Vector2(0.91f, 0.45f), 180).gameObject;
            room.coatOverlay = SpritePiece(room.robot.body, "Worn Guard Coat", props["Coat"], new Vector2(0, 0.36f), new Vector2(0.72f, 0.67f), 178).gameObject;
            room.helmetOverlay.SetActive(false); room.coatOverlay.SetActive(false);
            Transform canvas = GameObject.Find("Canvas").transform;
            room.helmetButton = Slot(canvas, "Helmet Inventory", new Vector2(65, 60), props["Helmet"], out room.wearHelmetIcon);
            room.coatButton = Slot(canvas, "Coat Inventory", new Vector2(165, 60), props["Coat"], out room.wearCoatIcon);
            ReplaceLabels(new Dictionary<string, string> { { "02  旧值班室", "02  城门检查口" }, { "阿铆 · 留守的值班员", "城门守卫" }, { "那个同伴", "出示合照" }, { "工厂停机", "接受检查" }, { "夜班收讯机", "城门值勤簿" }, { "楼上的回声", "混进机械城" } });
            room.radioPanel.SetActive(false);
            Save(room);
        }

        private static void Third()
        {
            EditorSceneManager.OpenScene(ControlRoomBuilder.ScenePath);
            ControlRoomController room = UnityEngine.Object.FindObjectOfType<ControlRoomController>();
            SetBackdrop(room.transform.Find("Control Room Background"), "Hideout.png");
            ReplaceLabels(new Dictionary<string, string> { { "03  上层控制室", "03  隔墙的阴谋" }, { "气动检修系统", "格栅升降气路" }, { "主控机械联锁", "窥视口锁片" }, { "停机之后", "被发现" } });
            foreach (SpriteRenderer old in room.archive.GetComponentsInChildren<SpriteRenderer>()) old.enabled = false;
            room.archive.transform.position = new Vector3(3.2f, -0.6f, -0.1f);
            room.archive.standPoint.position = new Vector3(3.05f, -2.0f, 0);
            BoxCollider2D ventHit = room.archive.GetComponent<BoxCollider2D>();
            ventHit.offset = new Vector2(0, 0.6f); ventHit.size = new Vector2(1.55f, 1.5f);
            SpriteRenderer gap = SpritePiece(room.archive.transform, "Listening Gap", square, Vector2.zero, new Vector2(1.45f, 1.3f), 44);
            gap.color = new Color(0.025f, 0.035f, 0.035f);
            Transform grille = Child(room.archive.transform, "Listening Grille");
            room.ventGrille = grille;
            for (int i = 0; i < 6; i++)
            {
                SpriteRenderer slat = SpritePiece(grille, "Slat " + i, square, new Vector2(0, 0.1f + i * 0.2f), new Vector2(1.5f, 0.085f), 46);
                slat.color = new Color(0.31f, 0.33f, 0.29f);
            }
            room.plotSprites = new[] { plot["Meeting"], plot["Bomb"], plot["Kitchen"] };
            room.plotIllustration = room.inspectIllustration;
            RectTransform card = room.inspectPanel.transform.Find("Card").GetComponent<RectTransform>();
            card.sizeDelta = new Vector2(1320, 780);
            Position(room.inspectTitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 335), new Vector2(1150, 55));
            Position(room.inspectIllustration.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 90), new Vector2(1120, 400));
            Position(room.inspectText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -195), new Vector2(1120, 160));
            room.inspectText.fontSize = 25; room.inspectText.alignment = TextAnchor.MiddleCenter;
            Position(room.closeInspectButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(-205, -325), new Vector2(210, 50));
            room.plotNextButton = Button(card, "Continue Listening", "继续听", new Vector2(0.5f, 0.5f), new Vector2(205, -325), new Vector2(210, 50));
            room.plotNextButton.gameObject.SetActive(false);
            Transform canvas = GameObject.Find("Canvas").transform;
            Transform capture = ChildUi(canvas, "Prison Ending");
            Image captureImage = capture.GetComponent<Image>();
            if (captureImage == null) captureImage = capture.gameObject.AddComponent<Image>();
            captureImage.sprite = plot["Prison"]; captureImage.color = Color.white; captureImage.raycastTarget = true;
            RectTransform captureRect = capture.GetComponent<RectTransform>();
            captureRect.anchorMin = Vector2.zero; captureRect.anchorMax = Vector2.one; captureRect.offsetMin = captureRect.offsetMax = Vector2.zero;
            Transform textPlate = ChildUi(capture, "Story Plate");
            Image plate = textPlate.GetComponent<Image>(); if (plate == null) plate = textPlate.gameObject.AddComponent<Image>();
            plate.color = new Color(0.025f, 0.045f, 0.05f, 0.9f);
            Position(textPlate.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0, -185), new Vector2(1080, 415));
            room.captureText = Label(textPlate, "Story", "", new Vector2(0.5f, 0.5f), new Vector2(0, 35), new Vector2(1000, 295), 26);
            Button replay = Button(textPlate, "Replay", "重玩第三关", new Vector2(0.5f, 0.5f), new Vector2(-150, -160), new Vector2(230, 54));
            Button title = Button(textPlate, "Title", "返回标题", new Vector2(0.5f, 0.5f), new Vector2(150, -160), new Vector2(230, 54));
            if (replay.onClick.GetPersistentEventCount() == 0) UnityEventTools.AddPersistentListener(replay.onClick, room.Restart);
            if (title.onClick.GetPersistentEventCount() == 0) UnityEventTools.AddPersistentListener(title.onClick, room.ReturnToTitle);
            Transform robotMark = ChildUi(capture, "Imprisoned Robot");
            Image prisoner = robotMark.GetComponent<Image>(); if (prisoner == null) prisoner = robotMark.gameObject.AddComponent<Image>();
            prisoner.sprite = room.robot.poses[0]; prisoner.preserveAspect = true; prisoner.raycastTarget = false;
            Position(prisoner.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-570, -170), new Vector2(150, 240));
            room.capturePanel = capture.gameObject; room.capturePanel.SetActive(false);
            room.gangReveal = SpritePiece(room.transform, "Gang At The Exit", props["Gang"], new Vector2(4.75f, -2.25f), new Vector2(3.0f, 2.2f), 240).gameObject;
            room.gangReveal.SetActive(false);
            Save(room);
        }

        private static void Save(Component room)
        {
            EditorUtility.SetDirty(room);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            if (!EditorSceneManager.SaveScene(SceneManager.GetActiveScene())) throw new InvalidOperationException("Could not save story scene.");
        }

        private static Sprite LoadBackground(string file)
        {
            string path = Art + file;
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 256; importer.mipmapEnabled = false; importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void SetBackdrop(Transform target, string file)
        {
            Sprite sprite = LoadBackground(file); target.GetComponent<SpriteRenderer>().sprite = sprite;
            target.localScale = new Vector3(16 / sprite.bounds.size.x, 9 / sprite.bounds.size.y, 1);
        }

        private static Dictionary<string, Sprite> ImportProps()
        {
            string[] names = { "Torso", "Leg", "Arm", "Helmet", "Coat", "Guard", "Gang", "Crate", "Photo" };
            // Authored atlas uses irregular gutters: keep each full silhouette rather than cutting equal cells.
            Rect[] bounds = { new Rect(190, 0, 365, 356), new Rect(660, 0, 300, 352), new Rect(1055, 0, 385, 310), new Rect(155, 350, 395, 300), new Rect(670, 348, 335, 342), new Rect(1150, 305, 255, 392), new Rect(0, 640, 620, 384), new Rect(625, 688, 420, 336), new Rect(1080, 685, 456, 339) };
            return ImportAtlas("StoryProps.png", names, bounds, true);
        }

        private static Dictionary<string, Sprite> ImportPlot()
        {
            return ImportAtlas("PlotStory.png", new[] { "Meeting", "Bomb", "Kitchen", "Prison" }, new[] { new Rect(0, 0, .5f, .5f), new Rect(.5f, 0, .5f, .5f), new Rect(0, .5f, .5f, .5f), new Rect(.5f, .5f, .5f, .5f) }, false);
        }

        private static Dictionary<string, Sprite> ImportAtlas(string file, string[] names, Rect[] topRects, bool crop)
        {
            string path = Art + file;
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 256; importer.isReadable = true; importer.alphaIsTransparency = crop;
            importer.mipmapEnabled = false; importer.maxTextureSize = 2048; importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            SpriteDataProviderFactories factories = new SpriteDataProviderFactories(); factories.Init();
            ISpriteEditorDataProvider provider = factories.GetSpriteEditorDataProviderFromObject(importer); provider.InitSpriteEditorDataProvider();
            Dictionary<string, GUID> previous = provider.GetSpriteRects().ToDictionary(r => r.name, r => r.spriteID);
            List<SpriteRect> rects = new List<SpriteRect>();
            for (int i = 0; i < names.Length; i++)
            {
                Rect top = topRects[i];
                if (crop) top = new Rect(top.x * texture.width / 1536f, top.y * texture.height / 1024f, top.width * texture.width / 1536f, top.height * texture.height / 1024f);
                else top = new Rect(top.x * texture.width + 5, top.y * texture.height + 5, top.width * texture.width - 10, top.height * texture.height - 10);
                int left = Mathf.Clamp(Mathf.FloorToInt(top.x), 0, texture.width - 1), right = Mathf.Min(texture.width - 1, Mathf.CeilToInt(top.xMax) - 1);
                int bottom = Mathf.Max(0, texture.height - Mathf.CeilToInt(top.yMax)), ceiling = Mathf.Min(texture.height - 1, texture.height - Mathf.FloorToInt(top.y) - 1);
                if (crop)
                {
                    int xmin = right, xmax = left, ymin = ceiling, ymax = bottom;
                    for (int y = bottom; y <= ceiling; y++) for (int x = left; x <= right; x++)
                        if (texture.GetPixel(x, y).a > 0.15f) { xmin = Mathf.Min(xmin, x); xmax = Mathf.Max(xmax, x); ymin = Mathf.Min(ymin, y); ymax = Mathf.Max(ymax, y); }
                    if (xmax < xmin || ymax < ymin) throw new InvalidOperationException("Empty story sprite " + names[i]);
                    left = xmin; right = xmax; bottom = ymin; ceiling = ymax;
                }
                rects.Add(new SpriteRect { name = names[i], rect = new Rect(left, bottom, right - left + 1, ceiling - bottom + 1), alignment = SpriteAlignment.Custom, pivot = crop ? new Vector2(.5f, 0) : new Vector2(.5f, .5f), spriteID = previous.ContainsKey(names[i]) ? previous[names[i]] : GUID.Generate() });
            }
            provider.SetSpriteRects(rects.ToArray());
            provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
            provider.Apply(); importer.SaveAndReimport();
            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToDictionary(s => s.name);
        }

        private static Transform Child(Transform parent, string name)
        {
            Transform found = parent.Find(name); if (found != null) return found;
            Transform child = new GameObject(name).transform; child.SetParent(parent, false); return child;
        }

        private static Transform ChildUi(Transform parent, string name)
        {
            Transform found = parent.Find(name); if (found != null) return found;
            Transform child = new GameObject(name, typeof(RectTransform)).transform; child.SetParent(parent, false); return child;
        }

        private static SpriteRenderer SpritePiece(Transform parent, string name, Sprite sprite, Vector2 foot, Vector2 size, int order)
        {
            Transform target = Child(parent, name); target.localPosition = foot;
            SpriteRenderer renderer = target.GetComponent<SpriteRenderer>(); if (renderer == null) renderer = target.gameObject.AddComponent<SpriteRenderer>();
            renderer.enabled = true; renderer.sprite = sprite; renderer.sortingOrder = order;
            if (sprite == square) { target.localScale = new Vector3(size.x / sprite.bounds.size.x, size.y / sprite.bounds.size.y, 1); target.localPosition += Vector3.up * size.y * .5f; }
            else target.localScale = Vector3.one * Mathf.Min(size.x / sprite.bounds.size.x, size.y / sprite.bounds.size.y);
            return renderer;
        }

        private static RoomInteractable Item(Transform parent, string name, ObjectKind kind, Sprite sprite, Vector2 foot, Vector2 size, Vector2 stand)
        {
            Transform root = Child(parent, name);
            RoomInteractable item = root.GetComponent<RoomInteractable>(); if (item == null) item = root.gameObject.AddComponent<RoomInteractable>();
            item.kind = kind; item.standPoint = Child(root, "Stand Here");
            if (root.GetComponent<BoxCollider2D>() == null) root.gameObject.AddComponent<BoxCollider2D>();
            if (root.GetComponent<SortingGroup>() == null) root.gameObject.AddComponent<SortingGroup>();
            SpritePiece(root, "Illustration", sprite, Vector2.zero, size, 1);
            Prop(item, sprite, foot, size, stand); return item;
        }

        private static void Prop(RoomInteractable item, Sprite sprite, Vector2 foot, Vector2 size, Vector2 stand)
        {
            item.gameObject.SetActive(true); item.transform.position = foot; item.transform.rotation = Quaternion.identity;
            SpriteRenderer renderer = item.transform.Find("Illustration").GetComponent<SpriteRenderer>(); renderer.sprite = sprite;
            renderer.transform.localScale = Vector3.one * Mathf.Min(size.x / sprite.bounds.size.x, size.y / sprite.bounds.size.y);
            renderer.transform.localPosition = Vector3.zero;
            item.standPoint.position = stand;
            Vector2 actual = renderer.bounds.size;
            BoxCollider2D collider = item.GetComponent<BoxCollider2D>(); collider.size = actual; collider.offset = new Vector2(0, actual.y * .5f);
            SortingGroup group = item.GetComponent<SortingGroup>(); if (group != null) group.sortingOrder = 100 + Mathf.RoundToInt(-foot.y * 20);
        }

        private static void Position(RectTransform rect, Vector2 anchor, Vector2 at, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = anchor; rect.anchoredPosition = at; rect.sizeDelta = size;
        }

        private static Button Button(Transform parent, string name, string title, Vector2 anchor, Vector2 at, Vector2 size)
        {
            Transform target = ChildUi(parent, name);
            Position(target.GetComponent<RectTransform>(), anchor, at, size);
            Image image = target.GetComponent<Image>(); if (image == null) image = target.gameObject.AddComponent<Image>(); image.color = Ink;
            Button button = target.GetComponent<Button>(); if (button == null) button = target.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            if (!string.IsNullOrEmpty(title)) Label(target, "Text", title, new Vector2(.5f, .5f), Vector2.zero, size - new Vector2(8, 8), 23);
            return button;
        }

        private static Button Slot(Transform canvas, string name, Vector2 at, Sprite sprite, out GameObject icon)
        {
            Button button = Button(canvas, name, "", Vector2.zero, at, new Vector2(88, 88));
            Transform target = ChildUi(button.transform, "Icon");
            Position(target.GetComponent<RectTransform>(), new Vector2(.5f, .5f), Vector2.zero, new Vector2(68, 68));
            Image image = target.GetComponent<Image>(); if (image == null) image = target.gameObject.AddComponent<Image>(); image.sprite = sprite; image.preserveAspect = true; image.raycastTarget = false;
            icon = target.gameObject; icon.SetActive(false); return button;
        }

        private static Text Label(Transform parent, string name, string value, Vector2 anchor, Vector2 at, Vector2 size, int fontSize)
        {
            Transform target = ChildUi(parent, name); Position(target.GetComponent<RectTransform>(), anchor, at, size);
            Text label = target.GetComponent<Text>(); if (label == null) label = target.gameObject.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); label.text = value; label.fontSize = fontSize; label.color = new Color(.9f, .84f, .67f); label.alignment = TextAnchor.MiddleCenter; label.raycastTarget = false; return label;
        }

        private static void SetText(string path, string value)
        {
            Transform target = path.StartsWith("Canvas/") ? GameObject.Find("Canvas").transform.Find(path.Substring(7)) : null;
            if (target != null) target.GetComponent<Text>().text = value;
        }

        private static void ReplaceLabels(Dictionary<string, string> changes)
        {
            foreach (Text label in UnityEngine.Object.FindObjectsOfType<Text>(true))
                if (changes.ContainsKey(label.text)) label.text = changes[label.text];
        }
    }
}
