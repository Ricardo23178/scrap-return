using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RobotEscape.Editor
{
    public static class RobotEscapeRoomArt
    {
        private const string Art = "Assets/Art/Room01Backdrop/";
        private static readonly Color Ink = new Color(0.10f, 0.16f, 0.17f);
        private static readonly Color Brass = new Color(0.68f, 0.52f, 0.29f);
        private static readonly Vector2Int[] Neighbors = { Vector2Int.left, Vector2Int.right, Vector2Int.up, Vector2Int.down };

        [MenuItem("Tools/Robot Escape/Apply Hand Painted Room")]
        public static void ApplySavedRoom()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play mode first.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save the current scene first.");
            if (SceneManager.GetActiveScene().path != RobotEscapeBuilder.DemoPath) EditorSceneManager.OpenScene(RobotEscapeBuilder.DemoPath);
            Sprite background = ImportBackground();
            Dictionary<string, Sprite> props = ImportAtlas("PropsAtlas.png", 3, 3,
                new[] { "Locker", "Workbench", "Crate", "Pump", "Scrap", "Console", "Door", "Battery", "ToolRack" });
            Dictionary<string, Sprite> robot = ImportAtlas("RobotAtlas.png", 2, 2,
                new[] { "Idle", "WalkA", "WalkB", "WalkC" });
            // Reimporting sprites can reload their prefab consumers; resolve scene references afterward.
            RobotEscapeRoom room = UnityEngine.Object.FindObjectOfType<RobotEscapeRoom>();
            if (room == null || room.robot == null || !room.robot.gameObject.scene.IsValid())
                throw new InvalidOperationException("The scene robot is unavailable after the artwork import.");

            GameObject placeholder = GameObject.Find("Room Art - replace with hand drawn sprites later");
            if (placeholder != null) placeholder.SetActive(false);
            SpriteRenderer backdrop = GetArt(GameObject.Find("RobotEscapeRoom").transform, "Hand Painted Background");
            backdrop.sprite = background; backdrop.sortingOrder = -100;
            backdrop.transform.position = Vector3.zero;
            backdrop.transform.localScale = new Vector3(16 / background.bounds.size.x, 9 / background.bounds.size.y, 1);
            Replace(room.trash, props["Scrap"], new Vector2(0, -0.4f), new Vector2(2.45f, 1.2f), room.trashGlint);
            Replace(room.battery, props["Battery"], new Vector2(0, -0.15f), new Vector2(0.42f, 0.8f));
            Replace(room.terminal, props["Console"], new Vector2(0, -0.9f), new Vector2(1.5f, 2.15f), room.terminalLight, room.batterySocket);
            foreach (TextMesh text in room.battery.GetComponentsInChildren<TextMesh>(true)) text.gameObject.SetActive(false);
            Transform details = GameObject.Find("Room Details").transform;
            Replace(details.Find("Storage Locker").GetComponent<RoomInteractable>(), props["Locker"], new Vector2(0, -1.25f), new Vector2(1.45f, 2.7f));
            Replace(details.Find("Old Workbench").GetComponent<RoomInteractable>(), props["Workbench"], new Vector2(0, -0.85f), new Vector2(2.3f, 1.85f));
            Replace(details.Find("Spare Parts Crate").GetComponent<RoomInteractable>(), props["Crate"], new Vector2(0, -0.65f), new Vector2(1.3f, 1.2f));
            Replace(details.Find("Idle Pump").GetComponent<RoomInteractable>(), props["Pump"], new Vector2(0, -0.8f), new Vector2(1.5f, 1.9f));
            Transform rack = details.Find("Wall Tool Rack");
            foreach (SpriteRenderer old in rack.GetComponentsInChildren<SpriteRenderer>()) old.enabled = false;
            SpriteRenderer rackArt = GetArt(rack, "Illustration");
            Fit(rackArt, props["ToolRack"], new Vector2(0, -0.43f), new Vector2(1.85f, 1.0f));
            rackArt.sortingOrder = -10;

            foreach (SpriteRenderer old in room.robot.body.GetComponentsInChildren<SpriteRenderer>()) old.enabled = false;
            room.robot.poses = new[] { robot["Idle"], robot["WalkA"], robot["WalkB"], robot["WalkC"] };
            room.robot.illustration = GetArt(room.robot.body, "Illustration");
            Fit(room.robot.illustration, robot["Idle"], Vector2.zero, new Vector2(1.2f, 1.65f));
            room.robot.illustration.sortingOrder = 200;
            if (PrefabUtility.IsPartOfPrefabInstance(room.robot.gameObject))
                PrefabUtility.ApplyPrefabInstance(room.robot.gameObject, InteractionMode.AutomatedAction);
            else
                PrefabUtility.SaveAsPrefabAsset(room.robot.gameObject, "Assets/Prefabs/Robot.prefab");

            room.doorLeaf.sprite = props["Door"];
            room.doorLeaf.color = Color.white;
            room.doorLeaf.transform.localPosition = new Vector3(0, -1.45f, 0);
            room.doorLeaf.transform.localScale = new Vector3(1.7f / props["Door"].bounds.size.x, 2.9f / props["Door"].bounds.size.y, 1);
            foreach (SpriteRenderer stripe in room.doorLeaf.GetComponentsInChildren<SpriteRenderer>())
                if (stripe != room.doorLeaf) stripe.enabled = false;
            room.door.transform.Find("Door Frame").GetComponent<SpriteRenderer>().color = new Color(0.035f, 0.055f, 0.055f);
            room.door.transform.Find("Door Frame").localScale = new Vector3(1.82f, 3.02f, 1);
            room.terminalLight.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Prototype/Circle.png");
            room.terminalLight.transform.localPosition = new Vector2(0.4f, 0.25f);
            room.terminalLight.transform.localScale = new Vector3(0.17f, 0.17f, 1);
            room.batterySocket.sprite = props["Battery"];
            room.batterySocket.transform.localPosition = new Vector2(-0.3f, -0.72f);
            room.batterySocket.transform.localScale = new Vector3(0.24f / props["Battery"].bounds.size.x, 0.55f / props["Battery"].bounds.size.y, 1);
            room.terminal.transform.Find("Socket Contact").GetComponent<SpriteRenderer>().enabled = false;
            room.trashGlint.transform.localPosition = new Vector2(0.15f, 0.31f);
            Transform marks = details.Find("Worn Maintenance Marks");
            marks.localScale = Vector3.one * 0.78f;
            marks.Find("Faded Plate").GetComponent<SpriteRenderer>().color = new Color(0.37f, 0.36f, 0.26f, 0.65f);
            foreach (SpriteRenderer mark in marks.GetComponentsInChildren<SpriteRenderer>())
                mark.transform.localRotation = Quaternion.Euler(0, 0, -2);

            StyleUi(room, background, props, robot["Idle"]);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            if (!EditorSceneManager.SaveScene(SceneManager.GetActiveScene())) throw new InvalidOperationException("Could not save hand-painted room.");
            AssetDatabase.SaveAssets();
            Debug.Log("ROBOT_ESCAPE_ART_APPLIED");
        }

        private static Sprite ImportBackground()
        {
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(Art + "MaintenanceRoom.png");
            Configure(importer);
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(Art + "MaintenanceRoom.png");
        }

        private static Dictionary<string, Sprite> ImportAtlas(string filename, int columns, int rows, string[] names)
        {
            string path = Art + filename;
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            Configure(importer);
            importer.isReadable = true;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.SaveAndReimport();
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            Color32[] pixels = texture.GetPixels32();
            SpriteDataProviderFactories factories = new SpriteDataProviderFactories();
            factories.Init();
            ISpriteEditorDataProvider provider = factories.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            Dictionary<string, GUID> previous = provider.GetSpriteRects().ToDictionary(rect => rect.name, rect => rect.spriteID);
            List<SpriteRect> sprites = new List<SpriteRect>();
            for (int row = 0; row < rows; row++) for (int col = 0; col < columns; col++)
            {
                int x0 = col * texture.width / columns, x1 = (col + 1) * texture.width / columns;
                int y0 = (rows - row - 1) * texture.height / rows, y1 = (rows - row) * texture.height / rows;
                Rect bounds = ContentBounds(pixels, texture.width, x0, y0, x1, y1, filename == "PropsAtlas.png");
                string name = names[row * columns + col];
                sprites.Add(new SpriteRect { name = name, rect = bounds, alignment = SpriteAlignment.BottomCenter, pivot = new Vector2(0.5f, 0), spriteID = previous.ContainsKey(name) ? previous[name] : GUID.Generate() });
            }
            provider.SetSpriteRects(sprites.ToArray());
            provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(sprites.Select(rect => new SpriteNameFileIdPair(rect.name, rect.spriteID)));
            provider.Apply();
            importer.SaveAndReimport();
            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToDictionary(sprite => sprite.name);
        }

        private static Rect ContentBounds(Color32[] pixels, int width, int x0, int y0, int x1, int y1, bool ignoreAdjacentFragments)
        {
            int cellWidth = x1 - x0;
            bool[] visited = new bool[cellWidth * (y1 - y0)];
            Queue<int> queue = new Queue<int>();
            int minX = x1, minY = y1, maxX = x0, maxY = y0;
            for (int y = y0; y < y1; y++) for (int x = x0; x < x1; x++)
            {
                int start = (y - y0) * cellWidth + x - x0;
                if (visited[start] || pixels[y * width + x].a < 20) continue;
                int left = x, right = x, bottom = y, top = y;
                queue.Enqueue(start); visited[start] = true;
                while (queue.Count > 0)
                {
                    int index = queue.Dequeue();
                    int px = x0 + index % cellWidth, py = y0 + index / cellWidth;
                    left = Mathf.Min(left, px); right = Mathf.Max(right, px);
                    bottom = Mathf.Min(bottom, py); top = Mathf.Max(top, py);
                    foreach (Vector2Int step in Neighbors)
                    {
                        int nx = px + step.x, ny = py + step.y;
                        if (nx < x0 || nx >= x1 || ny < y0 || ny >= y1) continue;
                        int next = (ny - y0) * cellWidth + nx - x0;
                        if (visited[next] || pixels[ny * width + nx].a < 20) continue;
                        visited[next] = true; queue.Enqueue(next);
                    }
                }
                // Adjacent atlas art may spill across a cell edge; keep the objects centered in this cell.
                float center = (left + right) * 0.5f;
                if (ignoreAdjacentFragments && (center < x0 + cellWidth * 0.1f || center > x1 - cellWidth * 0.1f)) continue;
                minX = Mathf.Min(minX, left); maxX = Mathf.Max(maxX, right);
                minY = Mathf.Min(minY, bottom); maxY = Mathf.Max(maxY, top);
            }
            if (minX > maxX || minY > maxY) throw new InvalidOperationException("Empty sprite atlas cell.");
            return new Rect(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }

        private static void Configure(TextureImporter importer)
        {
            if (importer == null) throw new InvalidOperationException("The hand-painted PNG files must be imported first.");
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 256;
            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
        }

        private static void Replace(RoomInteractable item, Sprite sprite, Vector2 foot, Vector2 size, params SpriteRenderer[] keep)
        {
            foreach (SpriteRenderer old in item.GetComponentsInChildren<SpriteRenderer>(true))
                if (!keep.Contains(old)) old.enabled = false;
            SpriteRenderer art = GetArt(item.transform, "Illustration");
            Fit(art, sprite, foot, size);
            art.sortingOrder = 100;
            BoxCollider2D collider = item.GetComponent<BoxCollider2D>();
            collider.size = new Vector2(sprite.bounds.size.x * art.transform.localScale.x, sprite.bounds.size.y * art.transform.localScale.y);
            collider.offset = foot + Vector2.up * collider.size.y * 0.5f;
        }

        private static SpriteRenderer GetArt(Transform parent, string name)
        {
            Transform old = parent.Find(name);
            SpriteRenderer art = old != null ? old.GetComponent<SpriteRenderer>() : new GameObject(name, typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            art.transform.SetParent(parent, false); art.enabled = true; art.color = Color.white;
            return art;
        }

        private static void Fit(SpriteRenderer renderer, Sprite sprite, Vector2 foot, Vector2 size)
        {
            renderer.sprite = sprite;
            float scale = Mathf.Min(size.x / sprite.bounds.size.x, size.y / sprite.bounds.size.y);
            renderer.transform.localPosition = foot;
            renderer.transform.localScale = Vector3.one * scale;
        }

        private static void StyleUi(RobotEscapeRoom room, Sprite background, Dictionary<string, Sprite> props, Sprite idle)
        {
            Transform ui = GameObject.Find("Canvas").transform;
            foreach (Button button in ui.GetComponentsInChildren<Button>(true))
            {
                Image image = button.GetComponent<Image>(); image.color = Ink;
                Outline border = button.GetComponent<Outline>();
                if (border == null) border = button.gameObject.AddComponent<Outline>();
                border.effectColor = new Color(Brass.r, Brass.g, Brass.b, 0.75f); border.effectDistance = new Vector2(1, -1);
                ColorBlock colors = button.colors;
                colors.highlightedColor = new Color(1.15f, 1.12f, 1.05f);
                colors.pressedColor = new Color(0.85f, 0.8f, 0.7f);
                button.colors = colors;
                foreach (Text text in button.GetComponentsInChildren<Text>(true)) text.color = new Color(0.9f, 0.85f, 0.71f);
            }
            for (int i = 0; i < 3; i++)
                room.puzzlePanel.transform.Find("Color" + i).GetComponent<Image>().color =
                    i == 0 ? new Color(0.63f, 0.25f, 0.18f) : i == 1 ? new Color(0.2f, 0.4f, 0.5f) : new Color(0.66f, 0.55f, 0.25f);
            ui.Find("Header").GetComponent<Image>().color = new Color(0.04f, 0.07f, 0.07f, 0.38f);
            Text title = ui.Find("Title").GetComponent<Text>();
            title.text = "01   维修间"; title.fontSize = 23;
            title.rectTransform.anchoredPosition = new Vector2(175, -30);
            title.rectTransform.sizeDelta = new Vector2(310, 42);
            RectTransform restart = ui.Find("Restart").GetComponent<RectTransform>();
            restart.sizeDelta = new Vector2(130, 38); restart.anchoredPosition = new Vector2(-88, -30);
            RectTransform back = ui.Find("BackToTitle").GetComponent<RectTransform>();
            back.sizeDelta = new Vector2(130, 38); back.anchoredPosition = new Vector2(-235, -30);
            foreach (Transform root in new[] { restart.transform, back.transform })
            {
                Text text = root.GetComponentInChildren<Text>(); text.fontSize = 18;
                text.rectTransform.sizeDelta = new Vector2(125, 35);
            }
            ui.Find("Footer").GetComponent<Image>().color = Color.clear;
            RectTransform inventory = room.inventoryButton.GetComponent<RectTransform>();
            inventory.sizeDelta = new Vector2(80, 80); inventory.anchoredPosition = new Vector2(65, 65);
            Image icon = room.batteryIcon.GetComponent<Image>();
            icon.sprite = props["Battery"]; icon.color = Color.white; icon.preserveAspect = true;
            foreach (Transform child in icon.transform) child.gameObject.SetActive(false);
            Text controls = ui.Find("Controls").GetComponent<Text>();
            controls.rectTransform.anchoredPosition = new Vector2(300, 27);
            controls.fontSize = 16;
            foreach (Transform modal in new[] { room.inspectPanel.transform, room.puzzlePanel.transform, room.winPanel.transform })
            {
                modal.Find("Card").GetComponent<Image>().color = new Color(0.095f, 0.15f, 0.155f, 0.98f);
                Outline border = modal.Find("Card").GetComponent<Outline>();
                if (border == null) border = modal.Find("Card").gameObject.AddComponent<Outline>();
                border.effectColor = Brass; border.effectDistance = new Vector2(2, -2);
            }
            room.inspectPanel.transform.Find("Scrap Closeup").gameObject.SetActive(false);
            Image scrap = ImageAt(room.inspectPanel.transform, "Painted Scrap Closeup", props["Scrap"], new Vector2(0, 45), new Vector2(380, 190));
            scrap.transform.SetSiblingIndex(2);
            foreach (GameObject panel in new[] { room.menuPanel, room.storyPanel })
            {
                Image image = panel.GetComponent<Image>(); image.sprite = background;
                image.color = panel == room.menuPanel ? new Color(0.3f, 0.37f, 0.35f) : new Color(0.12f, 0.17f, 0.18f);
            }
            Transform menu = room.menuPanel.transform;
            Move(menu.Find("GameTitle"), new Vector2(-380, 165), new Vector2(700, 125));
            Move(menu.Find("Subtitle"), new Vector2(-380, 60), new Vector2(780, 60));
            Move(menu.Find("Start"), new Vector2(-380, -70), new Vector2(330, 70));
            Move(menu.Find("Quit"), new Vector2(-380, -165), new Vector2(330, 62));
            Move(menu.Find("Genre"), new Vector2(-380, -320), new Vector2(700, 40));
            Image mascot = ImageAt(menu, "Robot Portrait", idle, new Vector2(420, -20), new Vector2(280, 410));
            mascot.transform.SetAsLastSibling();
        }

        private static Image ImageAt(Transform parent, string name, Sprite sprite, Vector2 position, Vector2 size)
        {
            Transform old = parent.Find(name);
            Image image = old != null ? old.GetComponent<Image>() : new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(parent, false); image.sprite = sprite; image.color = Color.white;
            image.preserveAspect = true; image.raycastTarget = false;
            Move(image.transform, position, size);
            return image;
        }

        private static void Move(Transform transform, Vector2 position, Vector2 size)
        {
            RectTransform rect = transform.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position; rect.sizeDelta = size;
        }
    }
}
