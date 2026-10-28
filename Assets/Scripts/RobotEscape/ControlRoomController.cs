using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RobotEscape
{
    public sealed class ControlRoomController : MonoBehaviour
    {
        public RobotWalker robot;
        public RoomInteractable pressureCabinet, console, trace, archive, exitLever, exit;
        public GameObject pressurePanel, boardPanel, inspectPanel, chapterPanel;
        public Transform coverLeaf, doorLeaf, leverHandle;
        public SpriteRenderer consoleLight, exitLight;
        public Text inspectTitle, inspectText, chapterText, pressureReadout, boardReadout;
        public Button inletButton, bleedButton, resetPressureButton, closePressureButton;
        public Button resetBoardButton, closeBoardButton, closeInspectButton, restartButton, returnTitleButton;
        public Button moveBackButton, moveForwardButton;
        public RectTransform inletWheel, bleedWheel, leftNeedle, rightNeedle, boardArea;
        public RectTransform[] blocks;
        public Image[] blockImages;
        public Image inspectIllustration;
        public Button plotNextButton;
        public Image plotIllustration;
        public Sprite[] plotSprites;
        public GameObject capturePanel, gangReveal;
        public Text captureText;
        public Transform ventGrille;

        public bool PressureSolved { get; private set; }
        public bool BoardSolved { get; private set; }
        public bool ArchiveSeen { get; private set; }
        public bool TraceSeen { get; private set; }
        public bool ExitOpen { get; private set; }
        public bool Completed { get; private set; }
        public int PlotPage { get; private set; }
        public bool PlotHeard { get; private set; }
        public bool Capturing { get; private set; }
        public bool Captured { get; private set; }
        public int InletSetting { get; private set; }
        public int BleedSetting { get; private set; }
        public int LeftPressure { get { return InletSetting; } }
        public int RightPressure { get { return Mathf.Max(0, InletSetting - BleedSetting); } }
        public int SelectedBlock { get; private set; }
        public bool InputBlocked { get { return Capturing || Completed || Active(pressurePanel) || Active(boardPanel) || Active(inspectPanel) || Active(chapterPanel) || Active(capturePanel); } }
        public bool BoardInputEnabled { get { return isActiveAndEnabled && !Capturing && !Completed && PressureSolved && !BoardSolved && Active(boardPanel); } }
        public Vector2 BoardCellSize
        {
            get
            {
                Vector2 size = boardArea != null ? boardArea.rect.size / 4f : new Vector2(110, 110);
                return new Vector2(Mathf.Max(1, size.x), Mathf.Max(1, size.y));
            }
        }

        private static readonly int[] InitialPositions = { 0, 0, 1, 1, 2 };
        private static readonly int[] FixedLines = { 1, 2, 2, 3, 3 };
        private static readonly string[] PlotLines =
        {
            "格栅另一侧，三个戴黑帽的机器人围着一张图。\n图上画着全城最高的塔。\n\n瘦混混：塔顶的路，我已经探过了。",
            "胖混混把一捆炸药推到桌子中央。\n\n黑帽头目：把炸弹装到塔顶。\n城里的中枢一毁，这座城就归我们了。",
            "瘦混混：厨房里那个贝塔呢？\n\n黑帽头目：继续锁着，让她做饭。\n今晚先把炸药送上去。"
        };
        private readonly int[] positions = { 0, 0, 1, 1, 2 };
        private readonly Color[] blockColors = new Color[5];
        private RoomInteractable pending;
        private Vector3 robotStart, coverStart, doorStart, ventStart, rattleOrigin;
        private Quaternion leverStart, inletStart, bleedStart, leftNeedleStart, rightNeedleStart;
        private Transform rattling;
        private float rattleUntil, lastAspect;
        private int suppressInputUntilFrame;
        private bool plotActive;
        private float captureStartedAt;

        private void Start()
        {
            Application.runInBackground = true;
            robotStart = robot != null ? robot.transform.position : Vector3.zero;
            coverStart = coverLeaf != null ? coverLeaf.localPosition : Vector3.zero;
            doorStart = doorLeaf != null ? doorLeaf.localPosition : Vector3.zero;
            ventStart = ventGrille != null ? ventGrille.localPosition : Vector3.zero;
            leverStart = RotationOf(leverHandle);
            inletStart = RotationOf(inletWheel);
            bleedStart = RotationOf(bleedWheel);
            leftNeedleStart = RotationOf(leftNeedle);
            rightNeedleStart = RotationOf(rightNeedle);
            Font font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "SimHei", "Arial" }, 24);
            if (font != null)
                foreach (Text label in FindObjectsOfType<Text>(true)) label.font = font;
            for (int i = 0; i < 5; i++)
            {
                blockColors[i] = blockImages != null && i < blockImages.Length && blockImages[i] != null ? blockImages[i].color : Color.white;
                if (blocks == null || i >= blocks.Length || blocks[i] == null) continue;
                SlideBlockHandle handle = blocks[i].GetComponent<SlideBlockHandle>();
                if (handle == null) handle = blocks[i].gameObject.AddComponent<SlideBlockHandle>();
                handle.controller = this;
                handle.blockIndex = i;
            }
            Bind(inletButton, CycleInlet);
            Bind(bleedButton, CycleBleed);
            Bind(resetPressureButton, ResetPressure);
            Bind(closePressureButton, ClosePanels);
            Bind(resetBoardButton, ResetBoard);
            Bind(closeBoardButton, ClosePanels);
            Bind(closeInspectButton, ClosePanels);
            Bind(moveBackButton, () => MoveSelected(-1));
            Bind(moveForwardButton, () => MoveSelected(1));
            Bind(restartButton, Restart);
            Bind(returnTitleButton, ReturnToTitle);
            Bind(plotNextButton, ContinuePlot);
            Restart();
        }

        private void Update()
        {
            Camera camera = Camera.main;
            if (camera != null && Mathf.Abs(camera.aspect - lastAspect) > 0.001f)
            {
                lastAspect = camera.aspect;
                camera.orthographicSize = Mathf.Max(4.5f, 8f / Mathf.Max(0.1f, lastAspect));
            }
            UpdateFeedback();
            if (Capturing)
            {
                if (Time.unscaledTime - captureStartedAt >= 2f) FinishCapture();
                return;
            }
            if (Input.GetKeyDown(KeyCode.Escape)) ClosePanels();
            if (Time.frameCount > suppressInputUntilFrame && Input.GetMouseButtonDown(0) && !InputBlocked &&
                (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject())) ClickAt(Input.mousePosition);
            if (pending != null && robot != null && !robot.IsMoving && !InputBlocked)
            {
                RoomInteractable target = pending;
                pending = null;
                if (target.gameObject.activeInHierarchy && Vector2.Distance(robot.transform.position, target.StandPosition) < 0.12f)
                    Interact(target);
            }
        }

        public void ClickAt(Vector2 screenPosition)
        {
            if (InputBlocked || Camera.main == null) return;
            Vector2 point = Camera.main.ScreenToWorldPoint(screenPosition);
            Collider2D hit = Physics2D.OverlapPoint(point);
            RoomInteractable target = hit != null ? hit.GetComponentInParent<RoomInteractable>() : null;
            if (target != null) RequestInteraction(target);
            else MoveTo(point);
        }

        public void MoveTo(Vector2 point)
        {
            if (InputBlocked || robot == null) return;
            pending = null;
            robot.MoveTo(point);
        }

        public void RequestInteraction(RoomInteractable target)
        {
            if (InputBlocked || robot == null || target == null || !target.gameObject.activeInHierarchy) return;
            pending = target;
            robot.MoveTo(target.StandPosition);
        }

        private void Interact(RoomInteractable target)
        {
            switch (target.kind)
            {
                case ObjectKind.PressureCabinet:
                    OpenModal(pressurePanel);
                    RefreshPressure();
                    break;
                case ObjectKind.ControlConsole:
                    if (!PressureSolved) { Rattle(target.transform); return; }
                    if (BoardSolved) ShowArchive(target);
                    else { OpenModal(boardPanel); RefreshBoard(); }
                    break;
                case ObjectKind.ControlTrace:
                    TraceSeen = true;
                    Inspect(target, "夹道里的痕迹", "地上有三道不同宽窄的轮印。\n墙缝里夹着一小片黑色帽檐。\n\n隔墙偶尔传来压低的交谈声。");
                    break;
                case ObjectKind.ControlArchive:
                    if (BoardSolved) ShowArchive(target);
                    else Inspect(target, "紧闭的格栅", "格栅后面透出一点暖光。\n几块生锈的锁片卡住了开口。\n用力拉扯，只会让整面墙响起来。");
                    break;
                case ObjectKind.ControlExitLever:
                    if (!PlotHeard) { Rattle(target.transform); return; }
                    ExitOpen = true;
                    if (leverHandle != null) leverHandle.localRotation = leverStart * Quaternion.Euler(0, 0, -48);
                    break;
                case ObjectKind.ControlExit:
                    if (!ExitOpen || !PlotHeard) { Rattle(target.transform); return; }
                    BeginCapture();
                    break;
            }
        }

        private void ShowArchive(RoomInteractable target)
        {
            if (!BoardSolved || Capturing || Completed) return;
            OpenModal(inspectPanel);
            plotActive = true;
            PlotPage = 0;
            ShowPlotPage();
        }

        private void ShowPlotPage()
        {
            SetText(inspectTitle, "隔墙的低语  " + (PlotPage + 1) + " / " + PlotLines.Length);
            SetText(inspectText, PlotLines[PlotPage]);
            SetActive(plotNextButton != null ? plotNextButton.gameObject : null, true);
            SetText(plotNextButton != null ? plotNextButton.GetComponentInChildren<Text>() : null,
                PlotPage == PlotLines.Length - 1 ? "悄悄退开" : "继续听");
            Image illustration = plotIllustration != null ? plotIllustration : inspectIllustration;
            if (plotIllustration != null && inspectIllustration != null && plotIllustration != inspectIllustration)
                inspectIllustration.enabled = false;
            if (illustration != null)
            {
                illustration.sprite = plotSprites != null && plotSprites.Length > 0 ? plotSprites[Mathf.Min(PlotPage, plotSprites.Length - 1)] : null;
                illustration.enabled = illustration.sprite != null;
                illustration.preserveAspect = true;
            }
        }

        public void ContinuePlot()
        {
            if (!plotActive || !Active(inspectPanel) || !BoardSolved || Capturing || Completed) return;
            if (PlotPage < PlotLines.Length - 1)
            {
                PlotPage++;
                ShowPlotPage();
                return;
            }
            PlotHeard = ArchiveSeen = true;
            ClosePanels();
        }

        private void BeginCapture()
        {
            if (!PlotHeard || !ExitOpen || Capturing || Completed) return;
            ClosePanels();
            RestoreRattle();
            if (robot != null) robot.Stop();
            Capturing = true;
            captureStartedAt = Time.unscaledTime;
            SetActive(gangReveal, true);
            SetText(chapterText, "黑帽头目：谁在外面？！\n\n一道黑影堵住了退路。");
            SetActive(chapterPanel, true);
        }

        private void FinishCapture()
        {
            if (!Capturing) return;
            Capturing = false;
            Captured = Completed = true;
            pending = null;
            if (robot != null) robot.Stop();
            SetActive(gangReveal, false);
            SetActive(chapterPanel, capturePanel == null);
            SetActive(capturePanel, true);
            const string ending = "铁门在身后重重合上。\n\n黑帽帮要炸毁城市中枢，贝塔还被锁在厨房。\n机器人必须先想办法离开这间牢房。\n\n第三关 · 被捕\n下一章：越狱（尚未制作）";
            SetText(chapterText, ending);
            SetText(captureText, ending);
        }

        private void Inspect(RoomInteractable target, string title, string text)
        {
            OpenModal(inspectPanel);
            SetText(inspectTitle, title);
            SetText(inspectText, text);
            if (inspectIllustration == null) return;
            SpriteRenderer artwork = target != null ? target.GetComponentInChildren<SpriteRenderer>() : null;
            inspectIllustration.sprite = artwork != null ? artwork.sprite : null;
            inspectIllustration.preserveAspect = true;
            inspectIllustration.enabled = inspectIllustration.sprite != null;
        }

        private void OpenModal(GameObject panel)
        {
            ClosePanels();
            if (robot != null) robot.Stop();
            RestoreRattle();
            SetActive(panel, true);
        }

        public void ClosePanels()
        {
            if (Capturing || Completed) return;
            SetActive(pressurePanel, false);
            SetActive(boardPanel, false);
            SetActive(inspectPanel, false);
            plotActive = false;
            SetActive(plotNextButton != null ? plotNextButton.gameObject : null, false);
            if (plotIllustration != null) plotIllustration.enabled = false;
            pending = null;
            suppressInputUntilFrame = Time.frameCount + 1;
        }

        public void CycleInlet()
        {
            if (!Active(pressurePanel) || PressureSolved || Capturing || Completed) return;
            InletSetting = (InletSetting + 1) % 3;
            CheckPressure();
        }

        public void CycleBleed()
        {
            if (!Active(pressurePanel) || PressureSolved || Capturing || Completed) return;
            BleedSetting = (BleedSetting + 1) % 3;
            CheckPressure();
        }

        private void CheckPressure()
        {
            if (LeftPressure == 2 && RightPressure == 1) PressureSolved = true;
            RefreshPressure();
        }

        public void ResetPressure()
        {
            if (!Active(pressurePanel) || PressureSolved || Capturing || Completed) return;
            InletSetting = BleedSetting = 0;
            RefreshPressure();
        }

        private void RefreshPressure()
        {
            if (inletWheel != null) inletWheel.localRotation = inletStart * Quaternion.Euler(0, 0, -120 * InletSetting);
            if (bleedWheel != null) bleedWheel.localRotation = bleedStart * Quaternion.Euler(0, 0, -120 * BleedSetting);
            if (leftNeedle != null) leftNeedle.localRotation = leftNeedleStart * Quaternion.Euler(0, 0, -60 * LeftPressure);
            if (rightNeedle != null) rightNeedle.localRotation = rightNeedleStart * Quaternion.Euler(0, 0, -60 * RightPressure);
            SetText(pressureReadout, "进气 " + InletSetting + "  /  泄压 " + BleedSetting + "\n左表 " + LeftPressure + "  /  右表 " + RightPressure +
                (PressureSolved ? " · 气压稳定" : " · 气压未校准"));
            SetInteractable(inletButton, !PressureSolved);
            SetInteractable(bleedButton, !PressureSolved);
            SetInteractable(resetPressureButton, !PressureSolved);
        }

        public bool IsBlockHorizontal(int index) { return index >= 0 && index < 5 && index % 2 == 0; }
        public int GetBlockPosition(int index) { return index >= 0 && index < 5 ? positions[index] : -1; }

        public void SelectBlock(int index)
        {
            if (!BoardInputEnabled || index < 0 || index >= 5) return;
            SelectedBlock = index;
            RefreshBoard();
        }

        public void MoveSelected(int delta) { TryMoveBlock(SelectedBlock, delta); }

        public bool TryMoveBlock(int index, int delta)
        {
            if (!BoardInputEnabled || index < 0 || index >= 5 || delta == 0 || delta < -3 || delta > 3) return false;
            int direction = delta > 0 ? 1 : -1;
            int destination = positions[index] + delta;
            if (destination < 0 || destination > 2) return false;
            for (int next = positions[index] + direction; next != destination + direction; next += direction)
                if (!BlockFits(index, next)) return false;
            positions[index] = destination;
            SelectedBlock = index;
            if (positions[0] == 2) BoardSolved = true;
            RefreshBoard();
            return true;
        }

        private bool BlockFits(int index, int origin)
        {
            bool horizontal = IsBlockHorizontal(index);
            for (int cell = 0; cell < 2; cell++)
            {
                int row = horizontal ? FixedLines[index] : origin + cell;
                int column = horizontal ? origin + cell : FixedLines[index];
                for (int other = 0; other < 5; other++)
                {
                    if (other == index) continue;
                    if (IsBlockHorizontal(other))
                    {
                        if (row == FixedLines[other] && column >= positions[other] && column < positions[other] + 2) return false;
                    }
                    else if (column == FixedLines[other] && row >= positions[other] && row < positions[other] + 2) return false;
                }
            }
            return true;
        }

        public void ResetBoard()
        {
            if (!BoardInputEnabled) return;
            for (int i = 0; i < 5; i++) positions[i] = InitialPositions[i];
            SelectedBlock = 0;
            RefreshBoard();
        }

        private void RefreshBoard()
        {
            Vector2 cell = BoardCellSize;
            for (int i = 0; i < 5; i++)
            {
                bool horizontal = IsBlockHorizontal(i);
                int row = horizontal ? FixedLines[i] : positions[i];
                int column = horizontal ? positions[i] : FixedLines[i];
                if (blocks != null && i < blocks.Length && blocks[i] != null)
                {
                    blocks[i].anchorMin = blocks[i].anchorMax = new Vector2(0, 1);
                    blocks[i].pivot = new Vector2(0.5f, 0.5f);
                    blocks[i].sizeDelta = new Vector2(cell.x * (horizontal ? 2 : 1) - 8, cell.y * (horizontal ? 1 : 2) - 8);
                    blocks[i].anchoredPosition = new Vector2((column + (horizontal ? 1 : 0.5f)) * cell.x, -(row + (horizontal ? 0.5f : 1)) * cell.y);
                }
                if (blockImages != null && i < blockImages.Length && blockImages[i] != null)
                    blockImages[i].color = BoardSolved ? Color.Lerp(blockColors[i], new Color(0.35f, 0.86f, 0.65f), 0.35f) :
                        i == SelectedBlock ? Color.Lerp(blockColors[i], Color.white, 0.24f) : blockColors[i];
            }
            SetText(boardReadout, BoardSolved ? "锁片滑开 · 格栅可以掀起" : "选中锁片 " + (char)('A' + SelectedBlock) +
                (IsBlockHorizontal(SelectedBlock) ? " · 沿水平方向移动" : " · 沿竖直方向移动"));
            SetInteractable(resetBoardButton, !BoardSolved);
            SetInteractable(moveBackButton, !BoardSolved);
            SetInteractable(moveForwardButton, !BoardSolved);
            SetText(moveBackButton != null ? moveBackButton.GetComponentInChildren<Text>() : null, IsBlockHorizontal(SelectedBlock) ? "←" : "↑");
            SetText(moveForwardButton != null ? moveForwardButton.GetComponentInChildren<Text>() : null, IsBlockHorizontal(SelectedBlock) ? "→" : "↓");
        }

        private void Rattle(Transform target)
        {
            RestoreRattle();
            rattling = target;
            rattleOrigin = target.position;
            rattleUntil = Time.time + 0.3f;
        }

        private void RestoreRattle()
        {
            if (rattling != null) rattling.position = rattleOrigin;
            rattling = null;
        }

        private void UpdateFeedback()
        {
            if (coverLeaf != null) coverLeaf.localPosition = Vector3.MoveTowards(coverLeaf.localPosition, coverStart + (PressureSolved ? Vector3.up * 1.25f : Vector3.zero), Time.deltaTime * 2.1f);
            if (ventGrille != null) ventGrille.localPosition = Vector3.MoveTowards(ventGrille.localPosition, ventStart + (BoardSolved ? Vector3.up * 1.25f : Vector3.zero), Time.deltaTime * 2.1f);
            if (doorLeaf != null) doorLeaf.localPosition = Vector3.MoveTowards(doorLeaf.localPosition, doorStart + (ExitOpen ? Vector3.up * 2.8f : Vector3.zero), Time.deltaTime * 2.6f);
            if (rattling != null)
            {
                rattling.position = rattleOrigin + Vector3.right * Mathf.Sin(Time.time * 40) * 0.025f;
                if (Time.time >= rattleUntil) RestoreRattle();
            }
            if (consoleLight != null) consoleLight.color = BoardSolved ? new Color(0.38f, 0.86f, 0.7f) : new Color(0.28f, 0.35f, 0.35f);
            if (exitLight != null) exitLight.color = ExitOpen ? new Color(0.46f, 0.8f, 0.57f) : new Color(0.55f, 0.31f, 0.16f);
        }

        public void Restart()
        {
            Capturing = Captured = Completed = PlotHeard = false;
            PlotPage = 0;
            captureStartedAt = 0;
            ClosePanels();
            RestoreRattle();
            PressureSolved = BoardSolved = ArchiveSeen = TraceSeen = ExitOpen = Completed = false;
            InletSetting = BleedSetting = 0;
            SelectedBlock = 0;
            for (int i = 0; i < 5; i++) positions[i] = InitialPositions[i];
            SetActive(chapterPanel, false);
            SetActive(capturePanel, false);
            SetActive(gangReveal, false);
            if (robot != null) { robot.transform.position = robotStart; robot.Stop(); }
            if (coverLeaf != null) coverLeaf.localPosition = coverStart;
            if (doorLeaf != null) doorLeaf.localPosition = doorStart;
            if (ventGrille != null) ventGrille.localPosition = ventStart;
            if (leverHandle != null) leverHandle.localRotation = leverStart;
            SetText(inspectTitle, "");
            SetText(inspectText, "");
            SetText(chapterText, "");
            SetText(captureText, "");
            if (inspectIllustration != null) { inspectIllustration.sprite = null; inspectIllustration.enabled = false; }
            RefreshPressure();
            RefreshBoard();
            UpdateFeedback();
        }

        public void ReturnToTitle()
        {
            Restart();
            SceneManager.LoadScene("RobotEscapeDemo");
        }

        private static Quaternion RotationOf(Transform target) { return target != null ? target.localRotation : Quaternion.identity; }
        private static bool Active(GameObject panel) { return panel != null && panel.activeInHierarchy; }
        private static void SetActive(GameObject panel, bool active) { if (panel != null) panel.SetActive(active); }
        private static void SetText(Text label, string text) { if (label != null) label.text = text; }
        private static void SetInteractable(Button button, bool value) { if (button != null) button.interactable = value; }
        private static void Bind(Button button, UnityEngine.Events.UnityAction action) { if (button != null) button.onClick.AddListener(action); }
    }
}
