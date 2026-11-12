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
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RobotEscape.Editor
{
    public static class CityChapterBuilder
    {
        public const string ScenePath = "Assets/Scenes/CitySquare.unity";
        private const string ArtPath = "Assets/Art/Room05City/";
        private static readonly Color Ink = new Color(.055f, .08f, .075f);
        private static readonly Vector2 Middle = new Vector2(.5f, .5f);

        [MenuItem("Tools/Robot Escape/Build City Chapter")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty))
                throw new InvalidOperationException("Save open scenes and stop Play mode before building CitySquare.");
            if (File.Exists(ScenePath))
                throw new InvalidOperationException("CitySquare already exists. Edit the saved scene directly.");
            Sprite plaza = Import("Plaza.png"), bar = Import("Bar.png"), gateOpen = Import("PlazaGateOpen.png");
            Sprite[] pickups = ImportPickups();
            Sprite coin = pickups[0], battery = pickups[1];
            Sprite[] gait = AssetDatabase.LoadAllAssetsAtPath("Assets/Art/CharacterMotion/HeroWalk.png")
                .OfType<Sprite>().OrderBy(frame => frame.name, StringComparer.Ordinal).ToArray();
            GameObject hero = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Robot.prefab");
            if (hero == null || gait.Length != 8)
                throw new InvalidOperationException("The existing hero or its eight gait frames are missing.");

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CityChapterController city = new GameObject("City Chapter").AddComponent<CityChapterController>();
            city.arcade = city.gameObject.AddComponent<CityArcade>();
            city.coinIcon = coin;
            city.batteryIcon = battery;
            city.plaza = Child(city.transform, "City Square");
            city.bar = Child(city.transform, "Old Valve Bar");
            Backdrop(city.plaza.transform, "Square Backdrop", plaza, -100);
            Backdrop(city.bar.transform, "Bar Backdrop", bar, -100);
            city.gateOpenArt = Backdrop(city.plaza.transform, "Gate Cleared Backdrop", gateOpen, -99).gameObject;
            city.gateOpenArt.SetActive(false);

            var targets = new List<CityTarget>();
            BuildPlaza(city, targets, battery);
            BuildBar(city, targets, coin);
            city.targets = targets.ToArray();

            Camera camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
            camera.tag = "MainCamera";
            camera.transform.position = new Vector3(0, 0, -10);
            camera.orthographic = true;
            camera.orthographicSize = 4.5f;
            camera.backgroundColor = Ink;
            camera.clearFlags = CameraClearFlags.SolidColor;
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            city.robot = ((GameObject)PrefabUtility.InstantiatePrefab(hero)).GetComponent<RobotWalker>();
            city.robot.transform.position = city.plazaStart.position;
            city.robot.walkArea = city.plazaWalk;
            city.robot.walkingPoses = gait;
            city.robot.walkCycleDistance = 1.25f;
            city.robot.minWalk = World(0, 800);
            city.robot.maxWalk = World(1672, 370);
            if (city.robot.GetComponent<SortingGroup>() == null) city.robot.gameObject.AddComponent<SortingGroup>();
            city.bar.SetActive(false);

            VerifyPaths(city);
            CityPumpDepth.Add(city);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException("Could not save CitySquare.");
            ConnectPrison();
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(ScenePath);
            Debug.Log("CITY_CHAPTER_BUILT: walking routes, build registration and prison exit connected.");
        }

        private static void BuildPlaza(CityChapterController city, List<CityTarget> targets, Sprite battery)
        {
            Transform room = city.plaza.transform;
            Vector2[] boundary = Outline(
                76, 623, 115, 606, 276, 602, 317, 567, 474, 558, 489, 506,
                451, 468, 438, 424, 354, 395, 354, 369, 440, 369, 461, 407,
                524, 441, 579, 467, 630, 482, 742, 477, 841, 479, 876, 424,
                947, 369, 1035, 342, 1149, 361, 1183, 376, 1266, 363,
                1392, 371, 1424, 417, 1138, 427, 1034, 446, 1020, 506,
                1114, 539, 1214, 567, 1224, 631, 1407, 650, 1514, 635,
                1573, 614, 1563, 663, 1481, 716, 1388, 757, 1180, 799,
                1132, 769, 1060, 753, 879, 750, 847, 725, 638, 781,
                576, 806, 468, 797, 302, 779, 166, 744, 84, 681);
            Rect[] furniture = {
                PixelRect(456, 552, 891, 652), PixelRect(611, 515, 771, 560),
                PixelRect(124, 573, 271, 613), PixelRect(301, 524, 469, 564),
                PixelRect(1238, 570, 1443, 638), PixelRect(1443, 579, 1521, 622)
            };
            city.plazaWalk = Zone(room, "Square Floor - Gate Blocked", boundary,
                furniture.Concat(new[] { PixelRect(1200, 362, 1416, 412) }).ToArray());
            city.plazaOpenWalk = Zone(room, "Square Floor - Gate Clear", boundary,
                furniture.Concat(new[] { PixelRect(1351, 362, 1416, 413) }).ToArray());
            city.plazaStart = Mark(room, "From Service Passage", 981, 728);
            city.plazaFromBar = Mark(room, "From Bar", 402, 388);
            city.towerInside = Mark(room, "Through Tower Threshold", 1298, 392);

            targets.Add(Target(room, "Battery Vendor", CityAction.Vendor, 111, 344, 277, 540, 215, 633));
            Sprite indicator = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Room03Backdrop/UiDisc.png");
            city.vendorFirstLamp = Prop(room, "First Coin Lamp", indicator, 211, 445, .12f, new Color(1f, .67f, .22f));
            city.vendorSecondLamp = Prop(room, "Second Coin Lamp", indicator, 244, 445, .12f, new Color(1f, .67f, .22f));
            city.vendorFirstLamp.SetActive(false);
            city.vendorSecondLamp.SetActive(false);
            CityTarget pickup = Target(room, "Battery Pickup Tray", CityAction.BatteryPickup, 196, 543, 260, 593, 215, 633);
            pickup.visible = Child(room, "Dispensed Batteries");
            Prop(pickup.visible.transform, "Left Battery", battery, 222, 562, .25f);
            Prop(pickup.visible.transform, "Right Battery", battery, 239, 561, .25f);
            pickup.visible.SetActive(false);
            targets.Add(pickup);
            targets.Add(Target(room, "Bar Entrance", CityAction.BarDoor, 305, 207, 456, 393, 404, 398));
            targets.Add(Target(room, "Tower Entrance", CityAction.Tower, 1179, 122, 1433, 412, 1171, 402));
            targets.Add(Target(room, "Old Watchmaker", CityAction.Talk, 304, 404, 479, 552, 410, 587,
                "「滴答，滴答……塔钟今天慢了一拍。」"));
            targets.Add(Target(room, "Courier", CityAction.Talk, 1239, 415, 1462, 627, 1341, 677,
                "「他每天都在等那只小鸟飞起来。」"));
        }

        private static void BuildBar(CityChapterController city, List<CityTarget> targets, Sprite coin)
        {
            Transform room = city.bar.transform;
            city.barWalk = Zone(room, "Bar Floor", Outline(
                31, 751, 112, 704, 145, 610, 157, 555, 190, 511,
                215, 561, 222, 624, 391, 657, 446, 626, 471, 582, 555, 570,
                615, 532, 653, 481, 708, 449, 757, 469, 959, 471, 1041, 483,
                1107, 462, 1203, 470, 1358, 496, 1445, 514, 1595, 533,
                1628, 554, 1552, 599, 1490, 637, 1445, 652, 1388, 659,
                1317, 694, 1290, 750, 1212, 788, 1102, 824, 1016, 881,
                966, 916, 867, 922, 701, 920, 478, 910, 310, 892, 151, 857,
                71, 855, 30, 825),
                new[] {
                    PixelRect(229, 564, 457, 635),
                    PixelRect(473, 634, 874, 872),
                    PixelRect(739, 423, 1048, 469)
                });
            city.barStart = Mark(room, "From Square", 198, 790);
            targets.Add(Target(room, "Bar Exit", CityAction.ExitBar, 14, 483, 120, 807, 78, 801));
            targets.Add(Target(room, "U Pusher Cabinet", CityAction.PushMachine, 1211, 142, 1442, 390, 1301, 527));
            targets.Add(Target(room, "Pipe Maze Cabinet", CityAction.MazeMachine, 1445, 124, 1640, 411, 1519, 555));
            targets.Add(Coin(room, coin, "Pusher Coin Tray", CityAction.PushCoin, 1265, 391, 1334, 448, 1301, 527));
            targets.Add(Coin(room, coin, "Maze Coin Tray", CityAction.MazeCoin, 1485, 415, 1550, 469, 1519, 555));
            targets.Add(Target(room, "Barkeep", CityAction.Talk, 303, 152, 464, 334, 669, 511,
                "老板放下杯子，朝最右边点点头。 「那两个槽还出币。」"));
            targets.Add(Target(room, "Tall Patron", CityAction.Talk, 202, 334, 346, 606, 172, 638,
                "「咕嘟……」他抬起杯子，让出一点视线。"));
            targets.Add(Target(room, "Copper Patron", CityAction.Talk, 430, 377, 607, 532, 480, 616,
                "「咣。」他拿杯底轻敲一下桌面。"));
            targets.Add(Target(room, "Back Table Left", CityAction.Talk, 753, 195, 899, 416, 805, 509,
                "「今天的阀门可真吵……」"));
            targets.Add(Target(room, "Back Table Right", CityAction.Talk, 930, 245, 1058, 441, 989, 522,
                "「塔后的厨房？灯还亮着。」"));
            targets.Add(Target(room, "Front Table Patron", CityAction.Talk, 475, 537, 650, 729, 442, 739,
                "他晃了晃杯子。「唔。」"));
        }

        private static void VerifyPaths(CityChapterController city)
        {
            var path = new List<Vector2>();
            foreach (CityTarget target in city.targets)
            {
                bool plaza = target.hit.transform.IsChildOf(city.plaza.transform);
                WalkableArea2D zone = plaza ? city.plazaWalk : city.barWalk;
                Vector2 from = plaza ? city.plazaStart.position : city.barStart.position;
                if (!zone.TryPath(from, target.stand.position, path))
                    throw new InvalidOperationException("No safe walking route to " + target.hit.name);
                if (plaza && !city.plazaOpenWalk.TryPath(city.plazaFromBar.position, target.stand.position, path))
                    throw new InvalidOperationException("No route after clearing the gate to " + target.hit.name);
            }
            if (!city.plazaWalk.TryPath(city.plazaStart.position, city.plazaFromBar.position, path) ||
                city.plazaWalk.Contains(city.towerInside.position) ||
                !city.plazaOpenWalk.TryPath(city.plazaFromBar.position, city.towerInside.position, path))
                throw new InvalidOperationException("The bar return or locked/open threshold geometry is invalid.");
        }

        private static CityTarget Coin(Transform room, Sprite sprite, string name, CityAction action,
            float left, float top, float right, float bottom, float standX, float standY)
        {
            CityTarget target = Target(room, name, action, left, top, right, bottom, standX, standY);
            target.visible = Prop(room, name + " Reward", sprite, (left + right) * .5f, (top + bottom) * .5f, .23f);
            target.visible.SetActive(false);
            return target;
        }

        private static GameObject Prop(Transform parent, string name, Sprite sprite, float x, float y, float height, Color? tint = null)
        {
            SpriteRenderer item = Child(parent, name).AddComponent<SpriteRenderer>();
            item.sprite = sprite;
            item.color = tint ?? Color.white;
            item.sortingOrder = -60;
            item.transform.position = World(x, y);
            item.transform.localScale = Vector3.one * (height / sprite.bounds.size.y);
            return item.gameObject;
        }

        private static CityTarget Target(Transform parent, string name, CityAction action, float left, float top,
            float right, float bottom, float standX, float standY, string dialogue = "")
        {
            Rect rect = PixelRect(left, top, right, bottom);
            BoxCollider2D hit = Child(parent, name).AddComponent<BoxCollider2D>();
            hit.transform.position = rect.center;
            hit.size = rect.size;
            hit.isTrigger = true;
            return new CityTarget { hit = hit, action = action, stand = Mark(hit.transform, "Stand Here", standX, standY), dialogue = dialogue };
        }

        private static WalkableArea2D Zone(Transform parent, string name, Vector2[] boundary, Rect[] obstacles)
        {
            WalkableArea2D zone = Child(parent, name).AddComponent<WalkableArea2D>();
            zone.boundary = boundary;
            zone.obstacles = obstacles;
            zone.radius = .08f;
            return zone;
        }

        // Pixel locations use the approved frame from the top left; floor data is stored as world-space foot positions.
        private static Vector2 World(float x, float y) => new Vector2(x * 16f / 1672 - 8, 4.5f - y * 9f / 941);
        private static Vector2[] Outline(params int[] points)
        {
            var result = new Vector2[points.Length / 2];
            for (int i = 0; i < result.Length; i++) result[i] = World(points[i * 2], points[i * 2 + 1]);
            return result;
        }
        private static Rect PixelRect(float left, float top, float right, float bottom)
        {
            Vector2 min = World(left, bottom), max = World(right, top);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }
        private static GameObject Child(Transform parent, string name)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            return obj;
        }
        private static Transform Mark(Transform parent, string name, float x, float y)
        {
            Transform mark = Child(parent, name).transform;
            mark.position = World(x, y);
            return mark;
        }
        private static SpriteRenderer Backdrop(Transform parent, string name, Sprite sprite, int order)
        {
            SpriteRenderer renderer = Child(parent, name).AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            renderer.transform.localScale = new Vector3(16 / sprite.bounds.size.x, 9 / sprite.bounds.size.y, 1);
            return renderer;
        }
        private static Sprite[] ImportPickups()
        {
            string path = ArtPath + "Pickups.png";
            if (!File.Exists(path)) throw new FileNotFoundException("City pickup art is missing.", path);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 256;
            importer.alphaIsTransparency = true;
            importer.isReadable = true;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            Color32[] pixels = texture.GetPixels32();
            SpriteDataProviderFactories factories = new SpriteDataProviderFactories();
            factories.Init();
            ISpriteEditorDataProvider provider = factories.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            var previous = provider.GetSpriteRects().ToDictionary(item => item.name, item => item.spriteID);
            var slices = new SpriteRect[2];
            int half = texture.width / 2;
            string[] names = { "Coin", "Battery" };
            for (int i = 0; i < slices.Length; i++)
            {
                int left = texture.width, right = -1, bottom = texture.height, top = -1;
                int first = i == 0 ? 0 : half, last = i == 0 ? half : texture.width;
                for (int y = 0; y < texture.height; y++)
                    for (int x = first; x < last; x++)
                        if (pixels[y * texture.width + x].a > 40)
                        { left = Mathf.Min(left, x); right = Mathf.Max(right, x); bottom = Mathf.Min(bottom, y); top = Mathf.Max(top, y); }
                if (right < left || top < bottom) throw new InvalidOperationException("Empty pickup art: " + names[i]);
                slices[i] = new SpriteRect
                {
                    name = names[i], rect = new Rect(left, bottom, right - left + 1, top - bottom + 1),
                    pivot = Middle, alignment = SpriteAlignment.Center,
                    spriteID = previous.TryGetValue(names[i], out GUID id) ? id : GUID.Generate()
                };
            }
            provider.SetSpriteRects(slices);
            provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(slices.Select(item => new SpriteNameFileIdPair(item.name, item.spriteID)));
            provider.Apply();
            importer.isReadable = false;
            importer.SaveAndReimport();
            Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
            return names.Select(name => sprites.Single(sprite => sprite.name == name)).ToArray();
        }

        private static Sprite Import(string name)
        {
            string path = ArtPath + name;
            if (!File.Exists(path)) throw new FileNotFoundException("City chapter art is missing.", path);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 256;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void EnableScene()
        {
            var entries = EditorBuildSettings.scenes.ToList();
            int i = entries.FindIndex(entry => entry.path == ScenePath);
            var city = new EditorBuildSettingsScene(ScenePath, true);
            if (i < 0) entries.Add(city);
            else entries[i] = city;
            EditorBuildSettings.scenes = entries.ToArray();
        }

        public static void ConnectPrison()
        {
            Scene scene = EditorSceneManager.OpenScene(PrisonChapterBuilder.ScenePath);
            PrisonChapterController prison = UnityEngine.Object.FindObjectOfType<PrisonChapterController>();
            Transform card = prison != null && prison.winPanel != null ? prison.winPanel.transform.Find("Card") : null;
            if (card == null) throw new InvalidOperationException("Could not locate the saved prison completion panel.");
            Button[] buttons = card.GetComponentsInChildren<Button>(true);
            Button again = buttons.Single(button => button.name == "Replay");
            Button title = buttons.Single(button => button.name == "Title");
            again.GetComponent<RectTransform>().anchoredPosition = new Vector2(-305, -190);
            title.GetComponent<RectTransform>().anchoredPosition = new Vector2(305, -190);
            Transform existing = card.Find("ContinueCity");
            Button next = existing == null ? UnityEngine.Object.Instantiate(again, card, false) : existing.GetComponent<Button>();
            next.name = "ContinueCity";
            next.onClick = new Button.ButtonClickedEvent();
            RectTransform rect = next.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = Middle;
            rect.anchoredPosition = new Vector2(0, -190);
            next.GetComponentInChildren<Text>(true).text = "继续 · 齿轮广场";
            UnityEventTools.AddPersistentListener(next.onClick, prison.ContinueToCity);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save the prison continuation.");
            EnableScene();
            AssetDatabase.SaveAssets();
        }
    }
}
