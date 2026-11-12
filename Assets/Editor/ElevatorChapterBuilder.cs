using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RobotEscape.Editor
{
    public static class ElevatorChapterBuilder
    {
        public const string ScenePath = "Assets/Scenes/TowerElevator.unity";
        private const string ArtPath = "Assets/Art/Room06Elevator/";
        private static readonly Vector2 Middle = new Vector2(.5f, .5f);
        private static readonly Color Chalk = new Color(.84f, .82f, .7f, .68f);

        [MenuItem("Tools/Robot Escape/Build Tower Elevator")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode ||
                Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty))
                throw new InvalidOperationException("Save open scenes and stop Play mode before building TowerElevator.");
            if (File.Exists(ScenePath))
                throw new InvalidOperationException("TowerElevator already exists. Edit its saved scene directly.");

            Sprite room = Import("Room.png"), panelArt = Import("Panel.png");
            GameObject hero = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Robot.prefab");
            Sprite[] gait = AssetDatabase.LoadAllAssetsAtPath("Assets/Art/CharacterMotion/HeroWalk.png")
                .OfType<Sprite>().OrderBy(frame => frame.name, StringComparer.Ordinal).ToArray();
            if (hero == null || gait.Length != 8)
                throw new InvalidOperationException("The existing hero or its eight gait frames are missing.");

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var chapter = new GameObject("Tower Elevator Chapter").AddComponent<ElevatorChapterController>();
            chapter.panel = chapter.gameObject.AddComponent<ElevatorPanel>();
            chapter.panel.faceplate = panelArt;

            SpriteRenderer backdrop = Child(chapter.transform, "Elevator Backdrop").AddComponent<SpriteRenderer>();
            backdrop.sprite = room;
            backdrop.sortingOrder = -100;
            backdrop.transform.localScale = new Vector3(16 / room.bounds.size.x, 9 / room.bounds.size.y, 1);

            Camera camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
            camera.tag = "MainCamera";
            camera.transform.position = new Vector3(0, 0, -10);
            camera.orthographic = true;
            camera.orthographicSize = 4.5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.055f, .07f, .06f);
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            BuildFloor(chapter);
            BuildWall(chapter, camera);
            chapter.panelHit = Hit(chapter.transform, "Lift Controller", 196, 266, 383, 699);
            chapter.clueHit = Hit(chapter.transform, "Four Chalk Floor Patterns", 439, 374, 570, 655);
            chapter.soilHit = Hit(chapter.transform, "Planter Soil", 1119, 642, 1465, 715);
            chapter.doorHit = Hit(chapter.transform, "Lift Door", 704, 315, 979, 751);

            chapter.soilLampCover = MakeSoilCover(chapter.transform, room.texture);
            chapter.soilLampCover.SetActive(false);

            chapter.robot = ((GameObject)PrefabUtility.InstantiatePrefab(hero)).GetComponent<RobotWalker>();
            chapter.robot.transform.position = chapter.start.position;
            chapter.robot.walkArea = chapter.walkArea;
            chapter.robot.walkingPoses = gait;
            chapter.robot.walkCycleDistance = 1.25f;
            chapter.robot.minWalk = World(0, 927);
            chapter.robot.maxWalk = World(1672, 751);
            if (chapter.robot.illustration == null || chapter.robot.illustration.sprite == null)
                throw new InvalidOperationException("The existing hero illustration is missing.");
            chapter.robot.illustration.transform.localScale =
                Vector3.one * (2.16f / chapter.robot.illustration.sprite.bounds.size.y);
            if (chapter.robot.GetComponent<SortingGroup>() == null)
                chapter.robot.gameObject.AddComponent<SortingGroup>();

            VerifyPaths(chapter);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException("Could not save TowerElevator.");
            var entries = EditorBuildSettings.scenes.ToList();
            int existing = entries.FindIndex(item => item.path == ScenePath);
            var entry = new EditorBuildSettingsScene(ScenePath, true);
            if (existing < 0) entries.Add(entry);
            else entries[existing] = entry;
            EditorBuildSettings.scenes = entries.ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("ELEVATOR_CHAPTER_BUILT: floor, original hero, four wall patterns and eight lamp positions saved.");
        }

        private static void BuildFloor(ElevatorChapterController chapter)
        {
            chapter.walkArea = Child(chapter.transform, "Carpet - Safe Foot Positions").AddComponent<WalkableArea2D>();
            chapter.walkArea.boundary = Outline(
                93, 900, 143, 842, 260, 795, 416, 769, 584, 751,
                1034, 751, 1110, 772, 1110, 814, 1170, 829, 1465, 829,
                1532, 850, 1566, 898, 1490, 927, 220, 927);
            chapter.walkArea.obstacles = new[] { PixelRect(1120, 740, 1470, 815) };
            chapter.walkArea.radius = .08f;
            chapter.start = Mark(chapter.transform, "Arrival Foot Position", 902, 863);
            chapter.panelStand = Mark(chapter.transform, "At Controller", 380, 800);
            chapter.clueStand = Mark(chapter.transform, "Reading Chalk Patterns", 550, 790);
            chapter.doorStand = Mark(chapter.transform, "At Closed Door", 850, 800);
            chapter.soilStand = Mark(chapter.transform, "At Planter", 1280, 850);
        }

        private static void BuildWall(ElevatorChapterController chapter, Camera camera)
        {
            var wall = new GameObject("Wall Circuits And Chalk", typeof(RectTransform), typeof(Canvas));
            wall.transform.SetParent(chapter.transform, false);
            Canvas canvas = wall.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = camera;
            canvas.overrideSorting = true;
            canvas.sortingOrder = -80;
            var layout = wall.GetComponent<RectTransform>();
            layout.sizeDelta = new Vector2(1672, 941);
            layout.localScale = new Vector3(16f / 1672, 9f / 941, 1);
            layout.localPosition = new Vector3(0, 0, -.02f);

            chapter.roomCircuit = At("Eight Signal Lamps", layout, 289, 564, 126, 126)
                .gameObject.AddComponent<ElevatorCircuitGraphic>();
            chapter.roomCircuit.raycastTarget = false;

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Sprite disc = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Room03Backdrop/UiDisc.png");
            chapter.floorIndicators = new Image[4];
            string[] symbols = { "II", "I", "0", "−I" };
            for (int i = 0; i < 4; i++)
            {
                float y = 419 + 20 * i;
                Image indicator = At("Floor Indicator " + i, layout, 284, y, 8, 8).gameObject.AddComponent<Image>();
                indicator.sprite = disc;
                indicator.color = i == 2 ? new Color(.8f, .65f, .29f) : new Color(.23f, .24f, .16f);
                indicator.raycastTarget = false;
                chapter.floorIndicators[i] = indicator;
                Label(layout, font, "Floor " + symbols[i], symbols[i], 301, y, 26, 19, 12,
                    new Color(.73f, .68f, .49f));

                float clueY = 411 + i * 68;
                // A 67-pixel graphic gives the approved 26-pixel ring radius.
                var diagram = At("Chalk Route " + i, layout, 518, clueY, 67, 67)
                    .gameObject.AddComponent<ElevatorCircuitGraphic>();
                diagram.clueIndex = i;
                diagram.color = Chalk;
                diagram.raycastTarget = false;
                Label(layout, font, "Chalk Floor " + symbols[i], symbols[i], 463, clueY + 1, 34, 28, 19, Chalk);
            }
        }

        private static GameObject MakeSoilCover(Transform parent, Texture2D texture)
        {
            const string path = ArtPath + "SoilCleared.asset";
            Sprite patch = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (patch == null)
            {
                // Reuse neighbouring soil from the same image so taking the lamp leaves an empty patch.
                float sx = texture.width / 1672f, sy = texture.height / 941f;
                var source = new Rect(1310 * sx, texture.height - 703 * sy, 53 * sx, 37 * sy);
                patch = Sprite.Create(texture, source, Middle, 256, 0, SpriteMeshType.FullRect);
                patch.name = "Soil After Taking Signal Lamp";
                AssetDatabase.CreateAsset(patch, path);
            }
            SpriteRenderer cover = Child(parent, "Soil After Taking Lamp").AddComponent<SpriteRenderer>();
            cover.sprite = patch;
            cover.sortingOrder = -90;
            cover.transform.position = World(1391, 681);
            cover.transform.localScale = new Vector3(53 * 16f / 1672 / patch.bounds.size.x,
                37 * 9f / 941 / patch.bounds.size.y, 1);
            return cover.gameObject;
        }

        private static void VerifyPaths(ElevatorChapterController chapter)
        {
            var path = new List<Vector2>();
            Transform[] stands = { chapter.start, chapter.panelStand, chapter.clueStand, chapter.doorStand, chapter.soilStand };
            foreach (Transform from in stands)
                foreach (Transform to in stands)
                    if (!chapter.walkArea.TryPath(from.position, to.position, path))
                        throw new InvalidOperationException("No safe elevator route from " + from.name + " to " + to.name);
            if (chapter.walkArea.Contains(World(850, 700)) || chapter.walkArea.Contains(World(1290, 790)))
                throw new InvalidOperationException("The elevator wall or planter is incorrectly walkable.");
        }

        private static Sprite Import(string name)
        {
            string path = ArtPath + name;
            if (!File.Exists(path)) throw new FileNotFoundException("Elevator art is missing.", path);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
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

        private static Text Label(RectTransform parent, Font font, string name, string value,
            float x, float y, float width, float height, int size, Color color)
        {
            Text text = At(name, parent, x, y, width, height).gameObject.AddComponent<Text>();
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            text.supportRichText = false;
            return text;
        }

        private static RectTransform At(string name, RectTransform parent, float x, float y, float width, float height)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = Middle;
            rect.anchoredPosition = new Vector2(x - 836, 470.5f - y);
            rect.sizeDelta = new Vector2(width, height);
            return rect;
        }

        private static Collider2D Hit(Transform parent, string name, float left, float top, float right, float bottom)
        {
            Rect rect = PixelRect(left, top, right, bottom);
            var hit = Child(parent, name).AddComponent<BoxCollider2D>();
            hit.transform.position = rect.center;
            hit.size = rect.size;
            hit.isTrigger = true;
            return hit;
        }

        private static Transform Mark(Transform parent, string name, float x, float y)
        {
            Transform mark = Child(parent, name).transform;
            mark.position = World(x, y);
            return mark;
        }

        private static GameObject Child(Transform parent, string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child;
        }

        // Approved image coordinates use a top-left origin; the floor stores world-space foot positions.
        private static Vector2 World(float x, float y) => new Vector2(x * 16f / 1672 - 8, 4.5f - y * 9f / 941);
        private static Vector2[] Outline(params int[] values)
        {
            var points = new Vector2[values.Length / 2];
            for (int i = 0; i < points.Length; i++) points[i] = World(values[i * 2], values[i * 2 + 1]);
            return points;
        }

        private static Rect PixelRect(float left, float top, float right, float bottom)
        {
            Vector2 min = World(left, bottom), max = World(right, top);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }
    }
}
