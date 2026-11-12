using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RobotEscape
{
    public sealed class CityArcade : MonoBehaviour
    {
        private static readonly string[] PushBoards =
        {
            "#######/#.....#/#P.o..>/#.....#/#######",
            "########/#......#/#......#/#.o#.#.#/#...P..>/########",
            "#########/#.#.##..#/#...P...#/#.......#/##o...#.#/#.......>/#########"
        };

        private const string MazeBoard =
            "###########/#S..#...#.#/###.#.#.#.#/#...#.#...#/#.###.###.#/#.#...#...#/#.#.###.###/#.......#E#/###.#####.#/#.........#/###########";

        private static readonly Color Ink = new Color32(231, 217, 183, 255);
        private static readonly Color Orange = new Color32(242, 153, 79, 255);
        private static readonly Color Green = new Color32(156, 218, 154, 255);
        private static readonly Color Blue = new Color32(90, 190, 224, 255);
        private static readonly Color Floor = new Color32(27, 49, 43, 255);
        private static readonly Color Wall = new Color32(57, 79, 70, 255);
        private static readonly Color Control = new Color32(73, 66, 52, 255);

        private sealed class Run
        {
            public string[] rows;
            public int stage;
            public bool solved;
            public Vector2Int player, ball, exit;
            public Vector2Int facing;
        }

        public bool IsOpen { get; private set; }
        public bool Solved => current != null && current.solved;
        public int StageIndex => current != null ? current.stage : 0;
        public Vector2Int PlayerCell => current != null ? current.player : Vector2Int.zero;
        public Vector2Int BallCell => current != null ? current.ball : Vector2Int.zero;

        private Run pushRun, mazeRun, current;
        private bool isMaze;
        private Action onComplete, onClose;
        private GameObject overlay;
        private Font font;
        private Texture2D circleTexture;
        private Sprite circle;
        private Image shell;
        private readonly Image[] tiles = new Image[121];
        private RectTransform fork, gate;
        private Image ball, mazeDot, mazeExit;
        private Text title, status, controls, closeLabel;
        private Button reset;
        private readonly Button[] directions = new Button[4];
        private float cellSize;

        public void Open(bool maze, Action complete, Action close)
        {
            if (IsOpen) return;
            if (overlay == null) BuildOverlay();

            isMaze = maze;
            current = maze ? mazeRun : pushRun;
            if (current == null)
            {
                current = new Run();
                LoadBoard(current, maze ? MazeBoard : PushBoards[0]);
                if (maze) mazeRun = current;
                else pushRun = current;
            }

            onComplete = complete;
            onClose = close;
            IsOpen = true;
            overlay.SetActive(true);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            shell.color = isMaze ? new Color32(87, 94, 65, 255) : new Color32(127, 77, 51, 255);
            DrawBoard();
            RefreshText();
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            overlay.SetActive(false);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            Action closed = onClose;
            onClose = onComplete = null;
            closed?.Invoke();
        }

        public void ResetProgress()
        {
            Close();
            current = pushRun = mazeRun = null;
        }

        public void ResetBoard()
        {
            if (!IsOpen || Solved) return;
            LoadBoard(current, isMaze ? MazeBoard : PushBoards[current.stage]);
            DrawActors();
        }

        public bool Step(Vector2Int direction)
        {
            if (!IsOpen || Solved || (direction != Vector2Int.up && direction != Vector2Int.down &&
                direction != Vector2Int.left && direction != Vector2Int.right)) return false;

            Vector2Int offset = new Vector2Int(direction.x, -direction.y);
            Vector2Int next = current.player + offset;
            current.facing = direction;
            if (Cell(next) == '#' || (!isMaze && next == current.exit))
            {
                DrawActors();
                return false;
            }

            if (!isMaze && next == current.ball)
            {
                Vector2Int pushed = next + offset;
                if (Cell(pushed) == '#')
                {
                    DrawActors();
                    return false;
                }
                current.ball = pushed;
            }

            current.player = next;
            if (isMaze ? current.player == current.exit : current.ball == current.exit)
            {
                if (!isMaze && current.stage < PushBoards.Length - 1)
                {
                    current.stage++;
                    LoadBoard(current, PushBoards[current.stage]);
                    DrawBoard();
                    RefreshText();
                    return true;
                }

                current.solved = true;
                DrawActors();
                RefreshText();
                Action completed = onComplete;
                onComplete = null;
                completed?.Invoke();
                return true;
            }

            DrawActors();
            return true;
        }

        private void Update()
        {
            if (!IsOpen) return;
            if (Input.GetKeyDown(KeyCode.Escape)) Close();
            else if (Input.GetKeyDown(KeyCode.R)) ResetBoard();
            else if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) Step(Vector2Int.up);
            else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) Step(Vector2Int.down);
            else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) Step(Vector2Int.left);
            else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) Step(Vector2Int.right);
        }

        private char Cell(Vector2Int point)
        {
            if (point.y < 0 || point.y >= current.rows.Length) return '#';
            string row = current.rows[point.y];
            return point.x < 0 || point.x >= row.Length ? '#' : row[point.x];
        }

        private static void LoadBoard(Run run, string layout)
        {
            run.rows = layout.Split('/');
            run.facing = Vector2Int.right;
            for (int y = 0; y < run.rows.Length; y++)
            {
                for (int x = 0; x < run.rows[y].Length; x++)
                {
                    switch (run.rows[y][x])
                    {
                        case 'P':
                        case 'S': run.player = new Vector2Int(x, y); break;
                        case 'o': run.ball = new Vector2Int(x, y); break;
                        case '>':
                        case 'E': run.exit = new Vector2Int(x, y); break;
                    }
                }
            }
        }

        private void DrawBoard()
        {
            int width = current.rows[0].Length;
            int height = current.rows.Length;
            cellSize = Mathf.Min(46, Mathf.Min(420f / height, 482f / width));

            for (int index = 0; index < tiles.Length; index++)
            {
                bool visible = index < width * height;
                Image tile = tiles[index];
                tile.gameObject.SetActive(visible);
                if (!visible) continue;
                Vector2Int point = new Vector2Int(index % width, index / width);
                tile.rectTransform.anchoredPosition = Position(point);
                tile.rectTransform.sizeDelta = Vector2.one * (cellSize - 2);
                tile.color = Cell(point) == '#' ? Wall : Floor;
            }

            fork.gameObject.SetActive(!isMaze);
            gate.gameObject.SetActive(!isMaze);
            ball.gameObject.SetActive(!isMaze);
            mazeDot.gameObject.SetActive(isMaze);
            mazeExit.gameObject.SetActive(isMaze);
            gate.anchoredPosition = mazeExit.rectTransform.anchoredPosition = Position(current.exit);
            gate.localScale = Vector3.one * cellSize;
            mazeExit.rectTransform.sizeDelta = Vector2.one * (cellSize * .54f);
            DrawActors();
        }

        private void DrawActors()
        {
            if (isMaze)
            {
                mazeDot.rectTransform.anchoredPosition = Position(current.player);
                mazeDot.rectTransform.sizeDelta = Vector2.one * (cellSize * .45f);
                return;
            }

            fork.anchoredPosition = Position(current.player);
            fork.localScale = Vector3.one * cellSize;
            fork.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(current.facing.y, current.facing.x) * Mathf.Rad2Deg);
            ball.rectTransform.anchoredPosition = Position(current.ball);
            ball.rectTransform.sizeDelta = Vector2.one * (cellSize * .51f);
        }

        private Vector2 Position(Vector2Int point) => cellSize * new Vector2(
            point.x - (current.rows[0].Length - 1) * .5f,
            (current.rows.Length - 1) * .5f - point.y);

        private void RefreshText()
        {
            title.text = isMaze ? "管道迷宫" : "拨叉推球";
            status.text = Solved ? "叮！硬币掉进了机身下方的取物槽。" :
                isMaze ? "沿旧管网走到绿灯处" : $"第 {current.stage + 1} / {PushBoards.Length} 关   ·   把蓝球推出右侧橙色口";
            status.color = Solved ? Green : Ink;
            controls.text = isMaze ? "方向键 / WASD\n走到绿色出口" : "方向键 / WASD\n拨叉只能推球";
            closeLabel.text = Solved ? "回到酒吧" : "退出街机";
            reset.interactable = !Solved;
            foreach (Button direction in directions) direction.interactable = !Solved;
        }

        private void BuildOverlay()
        {
            font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "SimHei", "Arial" }, 24);
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            circle = MakeCircle();

            Canvas canvas = new GameObject("Arcade Modal Canvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster)).GetComponent<Canvas>();
            canvas.transform.SetParent(transform, false);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            scaler.referenceResolution = new Vector2(1280, 720);
            overlay = canvas.gameObject;

            Image shade = Plate(canvas.transform, "Shade", Vector2.zero, Vector2.zero, new Color32(12, 14, 13, 218));
            shade.raycastTarget = true;
            shade.rectTransform.anchorMin = Vector2.zero;
            shade.rectTransform.anchorMax = Vector2.one;
            Image rim = Plate(shade.transform, "Metal Rim", Vector2.zero, new Vector2(944, 650), new Color32(41, 33, 28, 255));
            shell = Plate(rim.transform, "Painted Shell", Vector2.zero, new Vector2(928, 634), Control);
            RectTransform cabinet = shell.rectTransform;
            foreach (float x in new[] { -439f, 439f })
                foreach (float y in new[] { -291f, 291f })
                    Plate(cabinet, "Rivet", new Vector2(x, y), Vector2.one * 10, Ink).sprite = circle;

            title = Label(cabinet, "Title", Vector2.up * 279, new Vector2(640, 44), 34);
            status = Label(cabinet, "Status", Vector2.down * 282, new Vector2(854, 40), 24);
            Image bezel = Plate(cabinet, "CRT Bezel", new Vector2(-138, 3), new Vector2(570, 483), new Color32(40, 45, 36, 255));
            Image screen = Plate(bezel.transform, "CRT Glass", Vector2.zero, new Vector2(538, 451), new Color32(14, 30, 24, 255));

            for (int i = 0; i < tiles.Length; i++)
                tiles[i] = Plate(screen.transform, "Tile", Vector2.zero, Vector2.zero, Floor);

            for (int i = -5; i <= 5; i++)
                Plate(screen.transform, "Scanline", new Vector2(0, i * 39), new Vector2(529, 1), new Color32(142, 195, 151, 36));

            gate = ForkShape(screen.transform, "Orange Gate", Orange);
            mazeExit = Plate(screen.transform, "Maze Exit", Vector2.zero, Vector2.zero, Green);
            fork = ForkShape(screen.transform, "White Fork", Ink);
            ball = Plate(screen.transform, "Blue Ball", Vector2.zero, Vector2.zero, Blue);
            ball.sprite = circle;
            mazeDot = Plate(screen.transform, "Maze Player", Vector2.zero, Vector2.zero, Blue);
            mazeDot.sprite = circle;

            RectTransform pad = Plate(cabinet, "Control Pad", new Vector2(284, 3), new Vector2(238, 483), new Color32(57, 52, 42, 255)).rectTransform;
            controls = Label(pad, "Instructions", new Vector2(0, 157), new Vector2(206, 84), 23);
            directions[0] = Key(pad, "Up", "↑", new Vector2(0, 70), Vector2.one * 57, () => Step(Vector2Int.up));
            directions[1] = Key(pad, "Left", "←", new Vector2(-63, 7), Vector2.one * 57, () => Step(Vector2Int.left));
            directions[2] = Key(pad, "Down", "↓", new Vector2(0, 7), Vector2.one * 57, () => Step(Vector2Int.down));
            directions[3] = Key(pad, "Right", "→", new Vector2(63, 7), Vector2.one * 57, () => Step(Vector2Int.right));
            reset = Key(pad, "Reset", "重置本关", new Vector2(0, -97), new Vector2(190, 49), ResetBoard);
            closeLabel = Key(pad, "Close", "退出街机", new Vector2(0, -168), new Vector2(190, 52), Close).GetComponentInChildren<Text>();
            overlay.SetActive(false);
        }

        private Button Key(Transform parent, string name, string caption, Vector2 at, Vector2 size, Action clicked)
        {
            Image face = Plate(parent, name, at, size, Control);
            face.raycastTarget = true;
            Button key = face.gameObject.AddComponent<Button>();
            key.targetGraphic = face;
            Navigation navigation = key.navigation;
            navigation.mode = Navigation.Mode.None;
            key.navigation = navigation;
            ColorBlock colors = key.colors;
            colors.highlightedColor = new Color32(254, 224, 178, 255);
            colors.pressedColor = new Color32(190, 161, 121, 255);
            key.colors = colors;
            key.onClick.AddListener(() => clicked());
            Label(face.transform, "Label", Vector2.zero, size, caption.Length == 1 ? 30 : 22).text = caption;
            return key;
        }

        private Text Label(Transform parent, string name, Vector2 at, Vector2 size, int fontSize)
        {
            Text label = new GameObject(name, typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            Place(label.rectTransform, parent, at, size);
            label.font = font;
            label.fontSize = fontSize;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Ink;
            label.raycastTarget = false;
            return label;
        }

        private static RectTransform ForkShape(Transform parent, string name, Color color)
        {
            RectTransform shape = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            Place(shape, parent, Vector2.zero, Vector2.one);
            Plate(shape, "Back", new Vector2(-.25f, 0), new Vector2(.11f, .62f), color);
            Plate(shape, "Upper", new Vector2(0, .255f), new Vector2(.5f, .11f), color);
            Plate(shape, "Lower", new Vector2(0, -.255f), new Vector2(.5f, .11f), color);
            return shape;
        }

        private static Image Plate(Transform parent, string name, Vector2 at, Vector2 size, Color color)
        {
            Image image = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            Place(image.rectTransform, parent, at, size);
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static void Place(RectTransform rect, Transform parent, Vector2 at, Vector2 size)
        {
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.anchoredPosition = at;
            rect.sizeDelta = size;
        }

        private Sprite MakeCircle()
        {
            const int size = 32;
            circleTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color32[] pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float distance = new Vector2(x - 15.5f, y - 15.5f).magnitude;
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(255 * Mathf.Clamp01(15.5f - distance)));
                }
            circleTexture.SetPixels32(pixels);
            circleTexture.Apply(false, true);
            return Sprite.Create(circleTexture, new Rect(0, 0, size, size), new Vector2(.5f, .5f));
        }

        private void OnDestroy()
        {
            if (circle != null) Destroy(circle);
            if (circleTexture != null) Destroy(circleTexture);
        }
    }
}
