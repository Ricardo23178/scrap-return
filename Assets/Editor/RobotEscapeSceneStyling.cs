using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RobotEscape.Editor
{
    public static class RobotEscapeSceneStyling
    {
        [MenuItem("Tools/Robot Escape/Apply Subtle Room Hints")]
        public static void ApplySavedRoom()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play mode first.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save the current scene first.");
            if (SceneManager.GetActiveScene().path != RobotEscapeBuilder.DemoPath) EditorSceneManager.OpenScene(RobotEscapeBuilder.DemoPath);
            ApplyToRoom(UnityEngine.Object.FindObjectOfType<RobotEscapeRoom>());
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            if (!EditorSceneManager.SaveScene(SceneManager.GetActiveScene())) throw new InvalidOperationException("The room could not be saved.");
        }

        public static void ApplyToRoom(RobotEscapeRoom room)
        {
            Transform ui = GameObject.Find("Canvas").transform;
            foreach (string path in new[] { "Objective", "Status", "Wall Note", "InspectPanel/Description", "PuzzlePanel/PuzzleMessage" })
                ui.Find(path).gameObject.SetActive(false);
            ui.Find("Title").GetComponent<Text>().text = "废厂重启 · 维修间";
            RectTransform header = ui.Find("Header").GetComponent<RectTransform>();
            header.anchoredPosition = new Vector2(0, -30); header.sizeDelta = new Vector2(1600, 60);
            RectTransform footer = ui.Find("Footer").GetComponent<RectTransform>();
            footer.anchorMin = footer.anchorMax = Vector2.zero;
            footer.anchoredPosition = new Vector2(110, 58); footer.sizeDelta = new Vector2(220, 116);
            Text controls = ui.Find("Controls").GetComponent<Text>();
            controls.text = "左键交互 · Esc 返回";
            controls.rectTransform.anchoredPosition = new Vector2(430, 24);
            controls.rectTransform.sizeDelta = new Vector2(400, 30);
            controls.color = new Color(0.8f, 0.78f, 0.68f, 0.65f);
            room.inventoryText.text = "";
            ui.Find("InspectPanel/Heading").gameObject.SetActive(false);
            ui.Find("InspectPanel/Leave/Text").GetComponent<Text>().text = "返回";
            ui.Find("PuzzlePanel/Heading").GetComponent<Text>().text = "控制面板";
            ui.Find("PuzzlePanel/Close/Text").GetComponent<Text>().text = "返回";
            ui.Find("WinPanel/Summary").GetComponent<Text>().text = "维修间的门缓缓打开了。";
            for (int i = 0; i < 3; i++) ui.Find("PuzzlePanel/Color" + i + "/Text").GetComponent<Text>().text = "";
            foreach (TextMesh text in UnityEngine.Object.FindObjectsOfType<TextMesh>())
                if (text.text == "JUNK" || text.text == "POWER" || text.text == "EXIT") text.gameObject.SetActive(false);

            Sprite square = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Prototype/Square.png");
            if (GameObject.Find("Room Details") == null) CreateDetails(room, square);
            room.trashGlint = room.trash.transform.Find("Metal Glint").GetComponent<SpriteRenderer>();
            room.batterySocket = room.terminal.transform.Find("Battery Socket").GetComponent<SpriteRenderer>();

            if (room.inventoryButton.transform.Find("BatteryIcon") == null)
            {
                Image icon = UiImage(room.inventoryButton.transform, "BatteryIcon", new Vector2(0, -2), new Vector2(34, 52), new Color(0.85f, 0.65f, 0.25f));
                UiImage(icon.transform, "Cap", new Vector2(0, 30), new Vector2(13, 7), new Color(0.65f, 0.69f, 0.65f));
                UiImage(icon.transform, "Charge Mark", Vector2.zero, new Vector2(14, 4), new Color(0.24f, 0.28f, 0.24f));
            }
            room.batteryIcon = room.inventoryButton.transform.Find("BatteryIcon").gameObject;
            room.batteryIcon.SetActive(false);
            room.puzzleIndicators = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                string name = "Progress" + i;
                Transform old = room.puzzlePanel.transform.Find(name);
                room.puzzleIndicators[i] = old != null ? old.GetComponent<Image>() :
                    UiImage(room.puzzlePanel.transform, name, new Vector2(-45 + i * 45, 55), new Vector2(16, 16), new Color(0.22f, 0.29f, 0.29f));
            }
            if (room.inspectPanel.transform.Find("Scrap Closeup") == null)
            {
                GameObject preview = new GameObject("Scrap Closeup", typeof(RectTransform));
                preview.transform.SetParent(room.inspectPanel.transform, false);
                for (int i = 0; i < 5; i++)
                {
                    Image piece = UiImage(preview.transform, "Metal Scrap", new Vector2(-120 + i * 55, 15 + i % 2 * 20), new Vector2(110, 65), new Color(0.35f + i * 0.02f, 0.39f, 0.35f));
                    piece.transform.localRotation = Quaternion.Euler(0, 0, -25 + i * 11);
                }
                UiImage(preview.transform, "Caught Reflection", new Vector2(18, 30), new Vector2(12, 4), new Color(0.78f, 0.72f, 0.49f));
            }
            RobotEscapeIntroSetup.ApplyToRoom(room);
        }

        private static void CreateDetails(RobotEscapeRoom room, Sprite square)
        {
            Transform decor = new GameObject("Room Details").transform;
            Color dark = new Color(0.17f, 0.23f, 0.23f);
            Color metal = new Color(0.34f, 0.4f, 0.39f);
            Transform locker = Prop(decor, "Storage Locker", new Vector2(-6.5f, 0.1f), new Vector2(-6.4f, -1.8f), new Vector2(1.4f, 2.8f));
            Piece(square, locker, "Cabinet", Vector2.zero, new Vector2(1.3f, 2.7f), metal);
            Piece(square, locker, "Divider", Vector2.zero, new Vector2(0.04f, 2.5f), dark);
            foreach (int sign in new[] { -1, 1 })
            {
                Piece(square, locker, "Handle", new Vector2(sign * 0.13f, -0.1f), new Vector2(0.05f, 0.3f), new Color(0.6f, 0.54f, 0.39f));
                for (int i = 0; i < 3; i++) Piece(square, locker, "Vent", new Vector2(sign * 0.35f, 0.8f - i * 0.15f), new Vector2(0.3f, 0.04f), dark);
            }

            Transform workbench = Prop(decor, "Old Workbench", new Vector2(-0.9f, -0.25f), new Vector2(-0.9f, -1.9f), new Vector2(2.3f, 1.8f));
            Piece(square, workbench, "Bench Top", new Vector2(0, 0.6f), new Vector2(2.3f, 0.15f), new Color(0.45f, 0.35f, 0.24f));
            foreach (int sign in new[] { -1, 1 }) Piece(square, workbench, "Bench Leg", new Vector2(sign * 0.85f, -0.15f), new Vector2(0.14f, 1.4f), metal);
            Piece(square, workbench, "Toolbox", new Vector2(-0.35f, 0.91f), new Vector2(0.9f, 0.45f), new Color(0.45f, 0.29f, 0.23f));
            Piece(square, workbench, "Toolbox Handle", new Vector2(-0.35f, 1.19f), new Vector2(0.35f, 0.08f), dark);
            Piece(square, workbench, "Loose Tool", new Vector2(0.7f, 0.74f), new Vector2(0.45f, 0.07f), new Color(0.52f, 0.55f, 0.5f));

            Transform machine = Prop(decor, "Idle Pump", new Vector2(3.8f, -0.3f), new Vector2(3.8f, -2f), new Vector2(1.6f, 1.9f));
            Piece(square, machine, "Pump Body", Vector2.zero, new Vector2(1.4f, 1.55f), new Color(0.28f, 0.36f, 0.34f));
            Piece(square, machine, "Gauge Face", new Vector2(0, 0.45f), new Vector2(0.55f, 0.45f), new Color(0.63f, 0.61f, 0.48f));
            Piece(square, machine, "Gauge Needle", new Vector2(0.1f, 0.45f), new Vector2(0.2f, 0.035f), dark);
            for (int i = 0; i < 4; i++) Piece(square, machine, "Cooling Fin", new Vector2(0, -0.1f - i * 0.16f), new Vector2(0.9f, 0.04f), dark);
            Transform crate = Prop(decor, "Spare Parts Crate", new Vector2(-2.9f, -0.3f), new Vector2(-2.9f, -1.6f), new Vector2(1.4f, 1.1f));
            Piece(square, crate, "Crate Body", new Vector2(0, 0.3f), new Vector2(1.3f, 0.9f), new Color(0.4f, 0.31f, 0.22f));
            for (int i = -1; i <= 1; i++) Piece(square, crate, "Wood Slat", new Vector2(i * 0.4f, 0.3f), new Vector2(0.035f, 0.85f), dark);

            Transform rack = new GameObject("Wall Tool Rack").transform;
            rack.SetParent(decor, false); rack.localPosition = new Vector2(-0.8f, 1.6f);
            Piece(square, rack, "Board", Vector2.zero, new Vector2(2.1f, 0.8f), new Color(0.29f, 0.3f, 0.27f), -15);
            for (int i = 0; i < 4; i++)
            {
                Piece(square, rack, "Hanging Tool", new Vector2(-0.7f + i * 0.45f, 0), new Vector2(0.07f, 0.4f), new Color(0.47f, 0.49f, 0.44f), -14);
                Piece(square, rack, "Tool Head", new Vector2(-0.7f + i * 0.45f, 0.2f), new Vector2(0.24f, 0.07f), new Color(0.47f, 0.49f, 0.44f), -14);
            }
            Transform marks = new GameObject("Worn Maintenance Marks").transform;
            marks.SetParent(decor, false); marks.localPosition = new Vector2(-4.3f, 1.5f);
            Piece(square, marks, "Faded Plate", Vector2.zero, new Vector2(1.8f, 0.6f), new Color(0.29f, 0.32f, 0.29f), -15);
            Color[] clueColors = { new Color(0.28f, 0.39f, 0.44f), new Color(0.51f, 0.46f, 0.29f), new Color(0.47f, 0.32f, 0.27f) };
            for (int i = 0; i < 3; i++)
            {
                Piece(square, marks, "Paint Mark " + i, new Vector2(-0.55f + i * 0.55f, 0), new Vector2(0.22f, 0.16f), clueColors[i], -14);
                for (int j = 0; j <= i; j++) Piece(square, marks, "Scratch", new Vector2(-0.62f + i * 0.55f + j * 0.07f, -0.18f), new Vector2(0.025f, 0.07f), new Color(0.48f, 0.49f, 0.42f), -14);
            }
            Piece(square, room.trash.transform, "Metal Glint", new Vector2(0.18f, 0.37f), new Vector2(0.12f, 0.035f), new Color(0.8f, 0.75f, 0.5f), 159);
            Piece(square, room.terminal.transform, "Battery Socket", new Vector2(-0.3f, -0.4f), new Vector2(0.25f, 0.43f), new Color(0.1f, 0.15f, 0.15f), 124);
            Piece(square, room.terminal.transform, "Socket Contact", new Vector2(-0.3f, -0.15f), new Vector2(0.09f, 0.06f), new Color(0.62f, 0.57f, 0.4f), 125);
        }

        private static Transform Prop(Transform parent, string name, Vector2 position, Vector2 stand, Vector2 hitbox)
        {
            Transform root = new GameObject(name, typeof(RoomInteractable), typeof(BoxCollider2D), typeof(SortingGroup)).transform;
            root.SetParent(parent, false); root.localPosition = position;
            root.GetComponent<BoxCollider2D>().size = hitbox;
            root.GetComponent<SortingGroup>().sortingOrder = 100 + Mathf.RoundToInt(-position.y * 20);
            RoomInteractable item = root.GetComponent<RoomInteractable>(); item.kind = ObjectKind.Scenery;
            item.standPoint = new GameObject("Stand Here").transform;
            item.standPoint.SetParent(root, false); item.standPoint.position = stand;
            return root;
        }

        private static SpriteRenderer Piece(Sprite sprite, Transform parent, string name, Vector2 point, Vector2 size, Color color, int order = 10)
        {
            SpriteRenderer renderer = new GameObject(name, typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            renderer.transform.SetParent(parent, false); renderer.transform.localPosition = point;
            renderer.transform.localScale = new Vector3(size.x, size.y, 1);
            renderer.sprite = sprite; renderer.color = color; renderer.sortingOrder = order;
            return renderer;
        }

        private static Image UiImage(Transform parent, string name, Vector2 point, Vector2 size, Color color)
        {
            Image image = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(parent, false);
            image.rectTransform.anchorMin = image.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            image.rectTransform.anchoredPosition = point; image.rectTransform.sizeDelta = size;
            image.color = color; image.raycastTarget = false;
            return image;
        }
    }
}
