using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RobotEscape.Editor
{
    public static class Room01Polish
    {
        private const string BackgroundPath = "Assets/Art/Room01Backdrop/MaintenanceRoomV2.png";
        private static Sprite square, disc;
        private static readonly Color PipeDark = new Color(0.16f, 0.14f, 0.11f);
        private static readonly Color PipeCopper = new Color(0.39f, 0.25f, 0.13f);

        [MenuItem("Tools/Robot Escape/Polish First Room")]
        public static void ApplySavedRoom()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play mode before arranging the workshop.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save open scenes before arranging the workshop.");
            if (SceneManager.GetActiveScene().path != RobotEscapeBuilder.DemoPath)
                EditorSceneManager.OpenScene(RobotEscapeBuilder.DemoPath);

            ImportBackground();
            RobotEscapeRoom room = UnityEngine.Object.FindObjectOfType<RobotEscapeRoom>();
            if (room == null) throw new InvalidOperationException("The saved workshop controller is missing.");
            square = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Prototype/Square.png");
            disc = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Room03Backdrop/UiDisc.png");
            if (square == null || disc == null) throw new InvalidOperationException("Existing workshop shape sprites are missing.");
            var props = AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Room01Backdrop/PropsAtlas.png")
                .OfType<Sprite>().ToDictionary(sprite => sprite.name);
            var parts = ImportParts();
            Sprite backdrop = AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundPath);
            SpriteRenderer background = room.transform.Find("Hand Painted Background").GetComponent<SpriteRenderer>();
            background.sprite = backdrop;
            background.transform.position = Vector3.zero;
            background.transform.localScale = new Vector3(16f / backdrop.bounds.size.x, 9f / backdrop.bounds.size.y, 1);
            foreach (GameObject overlay in new[] { room.menuPanel, room.storyPanel })
                overlay.GetComponent<Image>().sprite = backdrop;

            Transform details = GameObject.Find("Room Details").transform;
            Place(room.trash, props["Scrap"], new Vector2(-4.1f, -2.02f), new Vector2(2.35f, 1.25f), new Vector2(-3.8f, -2.7f));
            Place(room.battery, props["Battery"], new Vector2(-3.55f, -2.06f), new Vector2(0.36f, 0.65f), new Vector2(-3.5f, -2.65f));
            room.battery.transform.position += Vector3.back * 0.05f;
            Place(room.terminal, props["Console"], new Vector2(0.7f, -1.55f), new Vector2(1.45f, 2.15f), new Vector2(0.65f, -2.3f));
            Place(details.Find("Storage Locker").GetComponent<RoomInteractable>(), props["Locker"],
                new Vector2(-6.05f, -1.38f), new Vector2(1.25f, 2.3f), new Vector2(-5.25f, -1.9f));
            Place(details.Find("Old Workbench").GetComponent<RoomInteractable>(), props["Workbench"],
                new Vector2(-1.8f, -1.35f), new Vector2(2.4f, 1.85f), new Vector2(-1.7f, -2.2f));
            Place(details.Find("Spare Parts Crate").GetComponent<RoomInteractable>(), props["Crate"],
                new Vector2(-5.6f, -2.15f), new Vector2(1.1f, 0.95f), new Vector2(-5.1f, -2.7f));
            RoomInteractable pump = details.Find("Idle Pump").GetComponent<RoomInteractable>();
            Place(pump, props["Pump"], new Vector2(3.55f, -1.85f), new Vector2(1.75f, 1.95f), new Vector2(3.55f, -2.65f));
            Transform rack = details.Find("Wall Tool Rack");
            rack.position = new Vector3(-1.15f, 1.1f, 0);
            rack.localScale = Vector3.one * 0.9f;
            Transform marks = details.Find("Worn Maintenance Marks");
            marks.position = new Vector3(-3.35f, 0.85f, 0);
            room.trashGlint.transform.localPosition = new Vector3(0.15f, 0.35f, 0);
            room.terminalLight.transform.localPosition = new Vector3(0.36f, 1.23f, 0);
            room.batterySocket.transform.localPosition = new Vector3(-0.26f, 0.45f, 0);

            room.door.transform.position = new Vector3(6.15f, 0, 0);
            room.door.standPoint.position = new Vector3(5.95f, -2.15f, 0);
            room.doorLeaf.transform.localPosition = new Vector3(0, -1.43f, 0);
            room.doorLeaf.transform.localScale = new Vector3(2.02f / props["Door"].bounds.size.x, 3.45f / props["Door"].bounds.size.y, 1);
            Transform oldFrame = room.door.transform.Find("Door Frame");
            if (oldFrame != null) oldFrame.GetComponent<SpriteRenderer>().enabled = false;
            BoxCollider2D doorHit = room.door.GetComponent<BoxCollider2D>();
            doorHit.offset = new Vector2(0, 0.29f); doorHit.size = new Vector2(1.95f, 3.4f);
            room.door.GetComponent<SortingGroup>().sortingOrder = 130;
            Transform opening = Child(room.door.transform, "Doorway Mask");
            opening.localPosition = new Vector3(0, 0.295f, 0);
            opening.localScale = new Vector3(2.08f / square.bounds.size.x, 3.52f / square.bounds.size.y, 1);
            SpriteMask mask = opening.GetComponent<SpriteMask>();
            if (mask == null) mask = opening.gameObject.AddComponent<SpriteMask>();
            mask.sprite = square; mask.isCustomRangeActive = true;
            mask.backSortingOrder = 110; mask.frontSortingOrder = 112;
            room.doorLeaf.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;

            room.robot.transform.position = new Vector3(-5.15f, -2.85f, 0);
            room.robot.minWalk = new Vector2(-5.4f, -3.25f);
            room.robot.maxWalk = new Vector2(6.55f, -1.4f);
            room.robot.Stop();
            Shadow(room.robot.transform, 0.73f);
            Transform assembly = Child(room.transform, "Workshop Pneumatics");
            assembly.localPosition = Vector3.zero;
            ConnectPipe(assembly, "Pump Riser", new Vector2(3.38f, -0.15f), new Vector2(3.38f, 1.13f));
            ConnectPipe(assembly, "Valve Feed", new Vector2(2.85f, -1.2f), new Vector2(3.3f, -1.2f));

            Transform valve = Child(assembly, "Pressure Valve");
            valve.position = new Vector3(2.85f, -1.2f, -0.1f);
            SortingGroup valveSorting = valve.GetComponent<SortingGroup>();
            if (valveSorting == null) valveSorting = valve.gameObject.AddComponent<SortingGroup>();
            valveSorting.sortingOrder = 155;
            RoomInteractable valveTarget = valve.GetComponent<RoomInteractable>();
            if (valveTarget == null) valveTarget = valve.gameObject.AddComponent<RoomInteractable>();
            valveTarget.kind = ObjectKind.WorkshopValve;
            valveTarget.standPoint = Child(valve, "Stand Here");
            valveTarget.standPoint.position = new Vector3(2.3f, -2.22f, 0);
            CircleCollider2D valveHit = valve.GetComponent<CircleCollider2D>();
            if (valveHit == null) valveHit = valve.gameObject.AddComponent<CircleCollider2D>();
            valveHit.radius = 0.43f;
            Transform wheel = Child(valve, "Wheel");
            wheel.localPosition = Vector3.zero; wheel.localRotation = Quaternion.identity;
            foreach (SpriteRenderer old in wheel.GetComponentsInChildren<SpriteRenderer>()) old.enabled = false;
            ArtPiece(wheel, "Painted Wheel", parts["Wheel"], Vector2.zero, new Vector2(0.66f, 0.66f), 4);
            SpriteRenderer lamp = Disc(valve, "Interlock Lamp", new Vector2(0.4f, 0.3f), new Vector2(0.11f, 0.11f), new Color(0.46f, 0.17f, 0.1f), 5);

            Transform gauge = Child(assembly, "Pressure Gauge");
            gauge.position = new Vector3(3.9f, -0.37f, -0.1f);
            foreach (SpriteRenderer old in gauge.GetComponentsInChildren<SpriteRenderer>()) old.enabled = false;
            ArtPiece(gauge, "Painted Face", parts["Gauge"], Vector2.zero, new Vector2(0.64f, 0.64f), 161);
            Transform needle = Child(gauge, "Needle");
            needle.localRotation = Quaternion.Euler(0, 0, -65);
            Piece(needle, "Pointer", new Vector2(0, 0.085f), new Vector2(0.025f, 0.2f), new Color(0.33f, 0.12f, 0.075f), 163);
            Disc(needle, "Pin", Vector2.zero, new Vector2(0.065f, 0.065f), PipeDark, 164);

            Transform pistonHousing = Child(assembly, "Door Cylinder");
            pistonHousing.position = new Vector3(6.15f, 2.36f, 0);
            foreach (SpriteRenderer old in pistonHousing.GetComponentsInChildren<SpriteRenderer>()) old.enabled = false;
            ArtPiece(pistonHousing, "Painted Cylinder", parts["Cylinder"], Vector2.zero, new Vector2(0.48f, 0.7f), 96);
            Transform piston = Child(assembly, "Piston Rod");
            piston.position = new Vector3(6.15f, 1.96f, 0);
            foreach (SpriteRenderer old in piston.GetComponentsInChildren<SpriteRenderer>()) old.enabled = false;
            ArtPiece(piston, "Painted Rod", parts["Rod"], Vector2.zero, new Vector2(0.3f, 0.57f), 98);

            Room01Mechanism mechanism = room.GetComponent<Room01Mechanism>();
            if (mechanism == null) mechanism = room.gameObject.AddComponent<Room01Mechanism>();
            room.mechanism = mechanism;
            mechanism.room = room; mechanism.valve = valveTarget; mechanism.valveWheel = wheel;
            mechanism.gaugeNeedle = needle; mechanism.piston = piston; mechanism.readyLamp = lamp;
            mechanism.steam = new SpriteRenderer[7];
            Transform steam = Child(assembly, "Exhaust Steam");
            steam.position = new Vector3(3.45f, 0.08f, 0);
            for (int i = 0; i < mechanism.steam.Length; i++)
            {
                SpriteRenderer puff = Disc(steam, "Puff " + i, new Vector2(Mathf.Sin(i * 2.1f) * 0.15f, i * 0.055f),
                    new Vector2(0.23f + i * 0.03f, 0.17f + i * 0.04f), new Color(0.7f, 0.76f, 0.7f, 0.16f), 180 + i);
                puff.enabled = false; mechanism.steam[i] = puff;
            }
            // Keep the foreground physical sprites above the character without narrowing the usable floor.
            SpriteRenderer foreground = Piece(assembly, "Near Scrap", new Vector2(-7.25f, -4.4f), Vector2.one, new Color(0.7f, 0.74f, 0.69f), 250);
            foreground.sprite = props["Scrap"];
            Fit(foreground, new Vector2(2.5f, 1.45f));

            EditorUtility.SetDirty(room); EditorUtility.SetDirty(mechanism);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            if (!EditorSceneManager.SaveScene(SceneManager.GetActiveScene())) throw new InvalidOperationException("Could not save the polished workshop.");
            AssetDatabase.SaveAssets();
            Debug.Log("ROBOT_ESCAPE_FIRST_ROOM_POLISHED");
        }

        private static void ImportBackground()
        {
            AssetDatabase.ImportAsset(BackgroundPath, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer = AssetImporter.GetAtPath(BackgroundPath) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("The new workshop background is missing.");
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 256; importer.mipmapEnabled = false; importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.filterMode = FilterMode.Bilinear; importer.SaveAndReimport();
        }

        private static Dictionary<string, Sprite> ImportParts()
        {
            const string path = "Assets/Art/Room01Backdrop/PneumaticParts.png";
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("The painted pneumatic parts are missing.");
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.isReadable = true; importer.alphaIsTransparency = true; importer.spritePixelsPerUnit = 256;
            importer.mipmapEnabled = false; importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            Color32[] pixels = texture.GetPixels32();
            SpriteDataProviderFactories factories = new SpriteDataProviderFactories(); factories.Init();
            ISpriteEditorDataProvider provider = factories.GetSpriteEditorDataProviderFromObject(importer); provider.InitSpriteEditorDataProvider();
            var previous = provider.GetSpriteRects().ToDictionary(rect => rect.name, rect => rect.spriteID);
            string[] names = { "Wheel", "Gauge", "Cylinder", "Rod" };
            List<SpriteRect> rects = new List<SpriteRect>();
            for (int i = 0; i < names.Length; i++)
            {
                int x0 = i % 2 * texture.width / 2, x1 = (i % 2 + 1) * texture.width / 2;
                int y0 = (1 - i / 2) * texture.height / 2, y1 = (2 - i / 2) * texture.height / 2;
                int left = x1, right = x0, bottom = y1, top = y0;
                for (int y = y0; y < y1; y++) for (int x = x0; x < x1; x++)
                    if (pixels[y * texture.width + x].a >= 20)
                    { left = Mathf.Min(left, x); right = Mathf.Max(right, x); bottom = Mathf.Min(bottom, y); top = Mathf.Max(top, y); }
                if (left > right || bottom > top) throw new InvalidOperationException("Empty pneumatic part: " + names[i]);
                rects.Add(new SpriteRect { name = names[i], rect = new Rect(left, bottom, right - left + 1, top - bottom + 1),
                    alignment = SpriteAlignment.Center, pivot = new Vector2(0.5f, 0.5f), spriteID = previous.ContainsKey(names[i]) ? previous[names[i]] : GUID.Generate() });
            }
            provider.SetSpriteRects(rects.ToArray());
            provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(rects.Select(rect => new SpriteNameFileIdPair(rect.name, rect.spriteID)));
            provider.Apply(); importer.SaveAndReimport();
            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToDictionary(sprite => sprite.name);
        }

        private static void Place(RoomInteractable item, Sprite sprite, Vector2 foot, Vector2 size, Vector2 stand)
        {
            item.transform.position = foot; item.transform.localRotation = Quaternion.identity;
            item.transform.localScale = Vector3.one;
            SpriteRenderer art = item.transform.Find("Illustration").GetComponent<SpriteRenderer>();
            art.sprite = sprite; art.transform.localPosition = Vector3.zero; art.color = Color.white;
            Fit(art, size);
            item.standPoint.position = stand;
            BoxCollider2D hit = item.GetComponent<BoxCollider2D>();
            hit.size = (Vector2)sprite.bounds.size * art.transform.localScale.x;
            hit.offset = Vector2.up * hit.size.y * 0.5f;
            SortingGroup sorting = item.GetComponent<SortingGroup>();
            if (sorting != null) sorting.sortingOrder = 100 + Mathf.RoundToInt(-foot.y * 20);
            Shadow(item.transform, hit.size.x);
        }

        private static void Shadow(Transform parent, float width)
        {
            Disc(parent, "Contact Shadow", new Vector2(0.04f, 0.02f), new Vector2(width * 0.85f, 0.13f), new Color(0.025f, 0.035f, 0.03f, 0.26f), -1);
        }

        private static void Fit(SpriteRenderer art, Vector2 size)
        {
            float scale = Mathf.Min(size.x / art.sprite.bounds.size.x, size.y / art.sprite.bounds.size.y);
            art.transform.localScale = Vector3.one * scale;
        }

        private static SpriteRenderer ArtPiece(Transform parent, string name, Sprite sprite, Vector2 position, Vector2 size, int order)
        {
            SpriteRenderer renderer = Piece(parent, name, position, size, Color.white, order);
            renderer.sprite = sprite; Fit(renderer, size); return renderer;
        }

        private static Transform Child(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child == null) { child = new GameObject(name).transform; child.SetParent(parent, false); }
            return child;
        }

        private static SpriteRenderer Piece(Transform parent, string name, Vector2 position, Vector2 size, Color color, int order)
        {
            Transform child = Child(parent, name);
            SpriteRenderer renderer = child.GetComponent<SpriteRenderer>();
            if (renderer == null) renderer = child.gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = square; renderer.color = color; renderer.sortingOrder = order; renderer.enabled = true;
            child.localPosition = position; child.localScale = new Vector3(size.x / square.bounds.size.x, size.y / square.bounds.size.y, 1);
            return renderer;
        }

        private static SpriteRenderer Disc(Transform parent, string name, Vector2 position, Vector2 size, Color color, int order)
        {
            SpriteRenderer renderer = Piece(parent, name, position, size, color, order);
            renderer.sprite = disc;
            renderer.transform.localScale = new Vector3(size.x / disc.bounds.size.x, size.y / disc.bounds.size.y, 1);
            return renderer;
        }

        private static void ConnectPipe(Transform parent, string name, Vector2 start, Vector2 end)
        {
            Vector2 direction = end - start;
            SpriteRenderer shadow = Piece(parent, name + " Shadow", (start + end) * 0.5f, new Vector2(direction.magnitude, 0.13f), PipeDark, 92);
            SpriteRenderer pipe = Piece(parent, name, (start + end) * 0.5f, new Vector2(direction.magnitude, 0.075f), PipeCopper, 93);
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            shadow.transform.localRotation = pipe.transform.localRotation = Quaternion.Euler(0, 0, angle);
        }
    }
}
