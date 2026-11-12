using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RobotEscape
{
    public sealed class PrisonChapterController : MonoBehaviour
    {
        public RobotWalker robot;
        public WalkableArea2D[] walkZones;
        public PrisonPatrol patrol;
        public GameObject[] areas;
        public PrisonTarget[] targets;
        public Sprite[] climbPoses;
        public SpriteRenderer cellDoor, corridorDoor, ventLeaf, keyArt, magnetArt, gearArt;
        public SpriteRenderer rodArt, toolArt;
        public GameObject inspectPanel, clockPanel, winPanel;
        public Text inspectText, areaText, speechText;
        public Button closeInspectButton, closeClockButton, restartButton, titleButton;
        public Button[] inventoryButtons, gearButtons;
        public Image[] inventoryIcons;
        public Transform[] gearWheels;
        public Image transitionShade;

        public int Area { get; private set; }
        public bool UpperLanding { get; private set; }
        public bool HasRod { get; private set; }
        public bool HasMagnet { get; private set; }
        public bool HasTool { get; private set; }
        public bool HasGear { get; private set; }
        public bool HasKey { get; private set; }
        public bool GearInstalled { get; private set; }
        public bool VentOpen { get; private set; }
        public bool ClockRepaired { get; private set; }
        public bool DoorUnlocked { get; private set; }
        public bool Completed { get; private set; }
        public bool Climbing { get; private set; }
        public bool Busy { get { return Climbing || job != null || extractionRemaining > 0 || transitionRemaining > 0 || stunnedRemaining > 0; } }
        public bool InputBlocked { get { return Busy || Completed || Active(inspectPanel) || Active(clockPanel) || Active(winPanel); } }
        public int SelectedItem { get; private set; }
        public bool PrisonerSpoken { get; private set; }
        public bool ClockArmed { get { return clockDelay > 0; } }
        public bool NoisyAction { get { return job != null; } }
        public int SoftFailures { get; private set; }
        public int[] GearOrientations { get { return (int[])gearTurns.Clone(); } }

        private readonly int[] gearTurns = new int[3];
        private readonly int[] gearGoal = { 1, 2, 3 };
        private PrisonTarget pending, job;
        private Vector3 jobOrigin, doorOrigin, corridorDoorOrigin, ventOrigin, bodyOrigin, walkArtScale;
        private Vector3 reachOrigin;
        private float jobRemaining, extractionRemaining, transitionRemaining, stunnedRemaining, clockDelay;
        private float climbElapsed, climbStartY, climbEndY, speechRemaining, lastAspect;
        private bool climbingUp;
        private float walkArtHeight;
        private int suppressInputFrame;
        private Vector3 cellStart;

        private void Start()
        {
            Application.runInBackground = true;
            cellStart = robot.transform.position;
            bodyOrigin = robot.body != null ? robot.body.localPosition : Vector3.zero;
            walkArtScale = robot.illustration.transform.localScale;
            walkArtHeight = robot.illustration.sprite.bounds.size.y * walkArtScale.y;
            doorOrigin = Position(cellDoor); corridorDoorOrigin = Position(corridorDoor); ventOrigin = Position(ventLeaf);
            reachOrigin = Position(toolArt);
            Bind(closeInspectButton, ClosePanels); Bind(closeClockButton, ClosePanels);
            Bind(restartButton, Restart); Bind(titleButton, ReturnToTitle);
            if (inventoryButtons != null) for (int i = 0; i < inventoryButtons.Length; i++)
            { int item = i; Bind(inventoryButtons[i], () => SelectItem(item)); }
            if (gearButtons != null) for (int i = 0; i < gearButtons.Length; i++)
            { int gear = i; Bind(gearButtons[i], () => TurnGear(gear)); }
            Font font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "SimHei", "Arial" }, 24);
            if (font != null) foreach (Text label in FindObjectsOfType<Text>(true)) label.font = font;
            Restart();
        }

        private void Update()
        {
            Camera camera = Camera.main;
            if (camera != null && Mathf.Abs(camera.aspect - lastAspect) > .001f)
            { lastAspect = camera.aspect; camera.orthographicSize = Mathf.Max(4.5f, 8f / Mathf.Max(.1f, lastAspect)); }
            float dt = Time.deltaTime;
            transitionRemaining = Mathf.Max(0, transitionRemaining - dt);
            stunnedRemaining = Mathf.Max(0, stunnedRemaining - dt);
            speechRemaining = Mathf.Max(0, speechRemaining - dt);
            if (speechText != null && speechRemaining <= 0) speechText.text = "";
            if (clockDelay > 0)
            {
                clockDelay = Mathf.Max(0, clockDelay - dt);
                if (clockDelay <= 0) patrol.Distract(14f);
            }
            if (Climbing) TickClimb(dt);
            if (job != null) TickNoisyAction(dt);
            if (extractionRemaining > 0)
            {
                if (!patrol.Distracted) { extractionRemaining = 0; Say("咔……", 1.1f); }
                else
                {
                    extractionRemaining = Mathf.Max(0, extractionRemaining - dt);
                    if (extractionRemaining <= 0) { HasKey = true; SelectedItem = -1; patrol.PlayMetal(); Refresh(); }
                }
            }
            if (Area == 2 && !Completed && !Busy && patrol.NearCell) SoftCatch();
            UpdateVisuals(dt);
            if (Input.GetKeyDown(KeyCode.Escape)) ClosePanels();
            if (Time.frameCount > suppressInputFrame && Input.GetMouseButtonDown(0) && !InputBlocked &&
                (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject())) ClickAt(Input.mousePosition);
            if (pending != null && !robot.IsMoving && !InputBlocked)
            {
                PrisonTarget target = pending; pending = null;
                if (CanReach(target) && Vector2.Distance(robot.transform.position, target.StandPosition) < .13f) Interact(target);
            }
        }

        public void ClickAt(Vector2 screenPosition)
        {
            if (InputBlocked || Camera.main == null) return;
            Vector2 point = Camera.main.ScreenToWorldPoint(screenPosition);
            Collider2D hit = Physics2D.OverlapPoint(point);
            PrisonTarget target = hit != null ? hit.GetComponentInParent<PrisonTarget>() : null;
            if (target != null) RequestInteraction(target); else MoveTo(point);
        }

        public void MoveTo(Vector2 point)
        {
            if (InputBlocked) return;
            pending = null; robot.MoveTo(point);
        }

        public void RequestInteraction(PrisonTarget target)
        {
            if (InputBlocked || !CanReach(target) || target.action == PrisonAction.Guard) return;
            pending = target; robot.MoveTo(target.StandPosition);
        }

        private bool CanReach(PrisonTarget target)
        {
            return target != null && target.gameObject.activeInHierarchy && target.area == Area &&
                (Area != 1 || target.action == PrisonAction.Ladder || target.upperLanding == UpperLanding);
        }

        private void Interact(PrisonTarget target)
        {
            switch (target.action)
            {
                case PrisonAction.Bed:
                    if (!HasRod && !HasTool) BeginNoisyAction(target); else Say("吱……", 1);
                    break;
                case PrisonAction.Vent:
                    if (VentOpen) ChangeArea(1, new Vector2(-5.2f, -3.45f), false);
                    else if ((SelectedItem == 0 && HasRod) || (SelectedItem == 2 && HasTool)) BeginNoisyAction(target);
                    else { patrol.PlayMetal(); Say("咔。", 1); }
                    break;
                case PrisonAction.ToCell:
                    ChangeArea(0, new Vector2(3.4f, -2.85f), false); break;
                case PrisonAction.Ladder:
                    BeginClimb(); break;
                case PrisonAction.Magnet:
                    if (!HasMagnet && !HasTool) { HasMagnet = true; patrol.PlayMetal(); Refresh(); }
                    break;
                case PrisonAction.Gear:
                    if (!HasGear && !GearInstalled) { HasGear = true; patrol.PlayMetal(); Refresh(); }
                    break;
                case PrisonAction.Hole:
                    if (HasKey) Say("……", .8f);
                    else if (SelectedItem == 2 && HasTool && UpperLanding && patrol.Distracted)
                    { robot.Stop(); extractionRemaining = 1.2f; patrol.PlayMetal(); }
                    else Inspect(patrol.Distracted ? "桌边静了下来。\n挂钩上，还留着一点铜色。" : "墙孔外，灯影偶尔划过。\n一串钥匙挂在桌边。");
                    break;
                case PrisonAction.Clock:
                    if (ClockRepaired)
                    {
                        if (!ClockArmed && !patrol.Distracted) { clockDelay = 3; patrol.PlayMetal(); Say("嗒、嗒……", 1.5f); }
                    }
                    else
                    {
                        if (!GearInstalled && SelectedItem == 3 && HasGear)
                        { GearInstalled = true; HasGear = false; SelectedItem = -1; patrol.PlayMetal(); Refresh(); }
                        if (GearInstalled) { ClosePanels(); robot.Stop(); SetActive(clockPanel, true); }
                        else Inspect("钟摆停着。\n缺口里只剩一圈发亮的磨痕。");
                    }
                    break;
                case PrisonAction.Door:
                    if (Area == 2) { ChangeArea(0, new Vector2(5.2f, -2.85f), false); break; }
                    if (!DoorUnlocked && SelectedItem == 4 && HasKey)
                    { DoorUnlocked = true; SelectedItem = -1; patrol.PlayMetal(); Refresh(); break; }
                    if (!DoorUnlocked) { patrol.PlayMetal(); Say("咔哒。", 1); break; }
                    if (patrol.NearCell) SoftCatch(); else ChangeArea(2, new Vector2(-5.5f, -2.85f), false);
                    break;
                case PrisonAction.Exit:
                    if (!DoorUnlocked || !HasKey) return;
                    if (patrol.NearCell) { SoftCatch(); break; }
                    pending = null; robot.Stop(); Completed = true; ClearPanels(); SetActive(winPanel, true);
                    if (inspectText != null) inspectText.text = "你钻进了通向城内的维修通道。\n远处，厨房的炉火还亮着。\n\n第四关 · 越狱\n下一关：齿轮广场";
                    break;
                case PrisonAction.Prisoner:
                    PrisonerSpoken = true;
                    Inspect(HasKey ? "老囚犯点点头，把披布裹紧。\n“去吧……轻一点。”" : VentOpen ? "老囚犯指了指墙，做了个侧耳倾听的动作。\n“唔……隔壁的脚步，一直都是这个节奏。”" : HasRod ? "老囚犯看着金属杆，轻轻点头。\n“旧东西，总还有点用。”" : "老囚犯抬起头。\n“嘘……他听得见。”");
                    break;
                case PrisonAction.Guard:
                    break;
                default:
                    Say("嗯……", 1); break;
            }
        }

        private void BeginNoisyAction(PrisonTarget target)
        {
            pending = null; robot.Stop();
            if (patrol.NearCell) { InterruptNoise(); return; }
            job = target; jobOrigin = target.transform.position; jobRemaining = 1.2f;
            patrol.PlayMetal();
        }

        private void TickNoisyAction(float dt)
        {
            if (patrol.NearCell) { RestoreJob(); InterruptNoise(); return; }
            jobRemaining -= dt;
            job.transform.position = jobOrigin + Vector3.right * Mathf.Sin(Time.time * 36) * .025f;
            if (jobRemaining > 0) return;
            PrisonAction completed = job.action; RestoreJob(); SelectedItem = -1;
            if (completed == PrisonAction.Bed) HasRod = true;
            else if (completed == PrisonAction.Vent) VentOpen = true;
            Refresh();
        }

        private void InterruptNoise()
        {
            if (patrol.ReportNoise()) SoftCatch();
            else { stunnedRemaining = .5f; Say("铛！", 1.2f); }
        }

        private void RestoreJob()
        {
            if (job != null) job.transform.position = jobOrigin;
            job = null; jobRemaining = 0;
        }

        public void SelectItem(int index)
        {
            if (InputBlocked || !Owns(index)) return;
            pending = null; robot.Stop();
            if ((SelectedItem == 0 && index == 1) || (SelectedItem == 1 && index == 0))
            {
                if (!HasRod || !HasMagnet) return;
                HasRod = HasMagnet = false; HasTool = true; SelectedItem = 2; patrol.PlayMetal();
            }
            else SelectedItem = SelectedItem == index ? -1 : index;
            Refresh();
        }

        private bool Owns(int index)
        {
            switch (index) { case 0: return HasRod; case 1: return HasMagnet; case 2: return HasTool; case 3: return HasGear; case 4: return HasKey; default: return false; }
        }

        public void TurnGear(int index)
        {
            if (!Active(clockPanel) || Busy || Completed || !GearInstalled || ClockRepaired || index < 0 || index >= 3) return;
            gearTurns[index] = (gearTurns[index] + 1) % 4; patrol.PlayMetal();
            ClockRepaired = gearTurns[0] == gearGoal[0] && gearTurns[1] == gearGoal[1] && gearTurns[2] == gearGoal[2];
            Refresh();
            if (ClockRepaired) { ClosePanels(); Say("嗒。", 1); }
        }

        private void BeginClimb()
        {
            robot.Stop(); pending = null; Climbing = true; climbElapsed = 0;
            climbingUp = !UpperLanding; climbStartY = UpperLanding ? 2f : -3.35f; climbEndY = UpperLanding ? -3.35f : 2f;
            robot.transform.position = new Vector3(0, climbStartY, 0); robot.enabled = false;
            if (robot.body != null) robot.body.localPosition = bodyOrigin;
            robot.illustration.flipX = false;
        }

        private void TickClimb(float dt)
        {
            climbElapsed += dt;
            float fraction = Mathf.Clamp01(climbElapsed / 2f);
            robot.transform.position = new Vector3(0, Mathf.Lerp(climbStartY, climbEndY, fraction), 0);
            robot.transform.localScale = Vector3.one;
            if (climbPoses != null && climbPoses.Length > 0)
            {
                int frame = Mathf.FloorToInt(climbElapsed * 5) % climbPoses.Length;
                Sprite pose = climbPoses[climbingUp ? frame : climbPoses.Length - 1 - frame];
                if (pose != null)
                {
                    robot.illustration.sprite = pose;
                    robot.illustration.transform.localScale = Vector3.one * walkArtHeight / pose.bounds.size.y;
                }
            }
            if (fraction < 1) return;
            UpperLanding = climbingUp; Climbing = false;
            robot.illustration.transform.localScale = walkArtScale;
            robot.enabled = true; robot.Stop(); SetWalkBounds(); SetLadderStand();
            suppressInputFrame = Time.frameCount + 1;
        }

        private void ChangeArea(int area, Vector2 position, bool upper)
        {
            RestoreJob(); pending = null; ClearPanels();
            Area = area; UpperLanding = upper; Climbing = false;
            robot.enabled = true; robot.illustration.transform.localScale = walkArtScale;
            if (robot.body != null) robot.body.localPosition = bodyOrigin;
            robot.transform.position = position; robot.Stop(); SetWalkBounds(); SetLadderStand();
            if (areas != null) for (int i = 0; i < areas.Length; i++) SetActive(areas[i], i == Area);
            transitionRemaining = .35f; suppressInputFrame = Time.frameCount + 1;
            Refresh();
        }

        private void SetWalkBounds()
        {
            if (walkZones != null && walkZones.Length == 4 && robot.walkArea != null)
            {
                WalkableArea2D zone = walkZones[Area == 0 ? 0 : Area == 1 ? UpperLanding ? 2 : 1 : 3];
                robot.walkArea.boundary = zone.boundary; robot.walkArea.obstacles = zone.obstacles; robot.walkArea.radius = zone.radius;
                Vector2 min = zone.boundary[0], max = min;
                foreach (Vector2 point in zone.boundary) { min = Vector2.Min(min, point); max = Vector2.Max(max, point); }
                robot.minWalk = min; robot.maxWalk = max;
            }
            else
            {
                robot.minWalk = Area == 1 && UpperLanding ? new Vector2(-1.2f, 1.85f) : new Vector2(-6.5f, -3.7f);
                robot.maxWalk = Area == 1 && UpperLanding ? new Vector2(5.7f, 2.25f) : new Vector2(6.5f, -2.6f);
            }
        }

        private void SetLadderStand()
        {
            if (targets == null) return;
            foreach (PrisonTarget target in targets)
                if (target != null && target.action == PrisonAction.Ladder && target.standPoint != null)
                    target.standPoint.position = new Vector3(0, UpperLanding ? 2f : -3.35f, 0);
        }

        private void SoftCatch()
        {
            RestoreJob(); extractionRemaining = 0; SelectedItem = -1; SoftFailures++;
            ChangeArea(0, cellStart, false); stunnedRemaining = .9f; patrol.PlayKnock(); Say("铛——！", 1.3f);
        }

        private void Inspect(string text)
        {
            ClearPanels(); pending = null; robot.Stop();
            if (inspectText != null) inspectText.text = text;
            SetActive(inspectPanel, true);
        }

        private void Say(string text, float seconds)
        {
            speechRemaining = seconds;
            if (speechText != null) speechText.text = text;
        }

        public void ClosePanels()
        {
            if (Busy || Completed) return;
            ClearPanels(); pending = null; suppressInputFrame = Time.frameCount + 1;
        }

        private void ClearPanels() { SetActive(inspectPanel, false); SetActive(clockPanel, false); SetActive(winPanel, false); }

        private void Refresh()
        {
            if (areaText != null) areaText.text = Area == 0 ? "04  牢房" : Area == 1 ? "04  检修夹层" : "04  看守走廊";
            if (inventoryButtons != null) for (int i = 0; i < inventoryButtons.Length; i++)
            {
                Button button = inventoryButtons[i];
                if (button != null) { button.interactable = Owns(i); button.GetComponent<Image>().color = i == SelectedItem ? new Color(.57f, .4f, .2f) : new Color(.12f, .2f, .21f); }
                if (inventoryIcons != null && i < inventoryIcons.Length && inventoryIcons[i] != null) inventoryIcons[i].enabled = Owns(i);
            }
            if (magnetArt != null) magnetArt.enabled = !HasMagnet && !HasTool;
            if (rodArt != null) rodArt.enabled = !HasRod && !HasTool;
            if (gearArt != null) gearArt.enabled = !HasGear && !GearInstalled;
            if (keyArt != null) keyArt.enabled = !HasKey;
            if (gearWheels != null) for (int i = 0; i < Mathf.Min(3, gearWheels.Length); i++)
                if (gearWheels[i] != null) gearWheels[i].localRotation = Quaternion.Euler(0, 0, gearTurns[i] * 90);
            if (targets != null) foreach (PrisonTarget target in targets)
            {
                if (target == null) continue;
                if (target.action == PrisonAction.Magnet) target.gameObject.SetActive(!HasMagnet && !HasTool);
                else if (target.action == PrisonAction.Gear) target.gameObject.SetActive(!HasGear && !GearInstalled);
            }
        }

        private void UpdateVisuals(float dt)
        {
            if (toolArt != null)
            {
                toolArt.enabled = Area == 1 && UpperLanding && extractionRemaining > 0;
                toolArt.transform.localPosition = reachOrigin + Vector3.right * (extractionRemaining > 0 ? Mathf.Sin((1.2f - extractionRemaining) / 1.2f * Mathf.PI) * .7f : 0);
            }
            if (cellDoor != null) cellDoor.transform.localPosition = Vector3.MoveTowards(cellDoor.transform.localPosition, doorOrigin + Vector3.up * (DoorUnlocked ? 5.7f : 0), dt * 3);
            if (corridorDoor != null) corridorDoor.transform.localPosition = Vector3.MoveTowards(corridorDoor.transform.localPosition, corridorDoorOrigin + Vector3.up * (DoorUnlocked ? 5.7f : 0), dt * 3);
            if (ventLeaf != null) ventLeaf.transform.localPosition = Vector3.MoveTowards(ventLeaf.transform.localPosition, ventOrigin + Vector3.right * (VentOpen ? .9f : 0), dt * 2);
            if (transitionShade != null)
            {
                transitionShade.raycastTarget = transitionRemaining > 0 || stunnedRemaining > 0;
                Color color = transitionShade.color; color.a = Mathf.Clamp01(transitionRemaining * 2 + stunnedRemaining * .3f); transitionShade.color = color;
            }
        }

        public void Restart()
        {
            RestoreJob(); Climbing = Completed = false; pending = null;
            extractionRemaining = transitionRemaining = stunnedRemaining = clockDelay = speechRemaining = 0;
            HasRod = HasMagnet = HasTool = HasGear = HasKey = GearInstalled = VentOpen = ClockRepaired = DoorUnlocked = PrisonerSpoken = false;
            SelectedItem = -1; SoftFailures = 0;
            for (int i = 0; i < gearTurns.Length; i++) gearTurns[i] = 0;
            if (cellDoor != null) cellDoor.transform.localPosition = doorOrigin;
            if (corridorDoor != null) corridorDoor.transform.localPosition = corridorDoorOrigin;
            if (ventLeaf != null) ventLeaf.transform.localPosition = ventOrigin;
            if (speechText != null) speechText.text = "";
            if (inspectText != null) inspectText.text = "";
            if (patrol != null) patrol.ResetPatrol();
            ChangeArea(0, cellStart, false); transitionRemaining = 0;
            if (robot.poses != null && robot.poses.Length > 0) robot.illustration.sprite = robot.poses[0];
            UpdateVisuals(0);
        }

        public void ContinueToCity()
        {
            if (Completed) SceneManager.LoadScene("CitySquare");
        }
        public void ReturnToTitle() { Restart(); SceneManager.LoadScene("RobotEscapeDemo"); }
        private static bool Active(GameObject value) { return value != null && value.activeSelf; }
        private static void SetActive(GameObject value, bool state) { if (value != null) value.SetActive(state); }
        private static Vector3 Position(SpriteRenderer value) { return value != null ? value.transform.localPosition : Vector3.zero; }
        private static void Bind(Button button, UnityEngine.Events.UnityAction action) { if (button != null) button.onClick.AddListener(action); }
    }
}

