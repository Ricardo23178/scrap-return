using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace RobotEscape
{
    public sealed class RobotEscapeRoom : MonoBehaviour
    {
        public string nextRoomScene;
        public RobotWalker robot;
        public RoomInteractable trash;
        public RoomInteractable battery;
        public RoomInteractable terminal;
        public RoomInteractable door;
        public Room01Mechanism mechanism;
        public ScrapAssembly assembly;
        public SpriteRenderer doorLeaf;
        public SpriteRenderer terminalLight;
        public Text statusText;
        public Text objectiveText;
        public Text inventoryText;
        public Text puzzleText;
        public Button inventoryButton;
        public GameObject inspectPanel;
        public Button shakeButton;
        public GameObject puzzlePanel;
        public GameObject winPanel;
        public SpriteRenderer trashGlint;
        public SpriteRenderer batterySocket;
        public GameObject batteryIcon;
        public Image[] puzzleIndicators;
        public GameObject menuPanel;
        public GameObject storyPanel;
        public Text storyText;
        public Text storyNextText;
        public Text storyPageText;
        public bool Started { get; private set; }
        public int StoryPage { get; private set; }
        public bool BatteryRevealed { get; private set; }
        public bool HasBattery { get; private set; }
        public bool BatterySelected { get; private set; }
        public bool Powered { get; private set; }
        public bool DoorOpen { get; private set; }
        public bool Won { get; private set; }
        public bool Shaking { get { return shakeRemaining > 0; } }
        public int PuzzleProgress { get; private set; }
        public bool InputBlocked { get { return !Started || inspectPanel.activeSelf || puzzlePanel.activeSelf || winPanel.activeSelf || Shaking || (mechanism != null && mechanism.Cycling); } }
        private RoomInteractable pending;
        private Vector3 robotStart;
        private Vector3 trashStart;
        private Vector3 doorStart;
        private float shakeRemaining;
        private readonly int[] sequence = { 1, 2, 0 };
        private float lastAspect;
        private float incorrectUntil;
        private float powerFeedbackUntil;
        private Transform rattlingObject;
        private Vector3 rattleStart;
        private float rattleUntil;
        private readonly string[] storyPages =
        {
            "机械城，烟囱与齿轮组成的家。\n你和贝塔，曾在最高塔里一起生活。",
            "垃圾吸尘器追错了目标。\n本该被抓走的黑帽打手逃了，你却被拆散，丢在城外。",
            "齿轮还在轻轻转动。\n废料堆里的你睁开眼睛，远处是回家的城门。"
        };

        private void Start()
        {
            Application.runInBackground = true;
            Font font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "SimHei", "Arial" }, 24);
            if (font != null)
                foreach (Text label in FindObjectsOfType<Text>(true)) label.font = font;
            robotStart = robot.transform.position;
            trashStart = trash.transform.position;
            doorStart = doorLeaf.transform.localPosition;
            ReturnToTitle();
        }

        public void ShowStory()
        {
            if (Started || storyPanel == null) return;
            menuPanel.SetActive(false);
            storyPanel.SetActive(true);
            StoryPage = 0;
            ShowStoryPage();
        }

        public void NextStoryPage()
        {
            if (Started || storyPanel == null || !storyPanel.activeSelf) return;
            StoryPage++;
            if (StoryPage >= storyPages.Length) { BeginAdventure(); return; }
            ShowStoryPage();
        }

        private void ShowStoryPage()
        {
            storyText.text = storyPages[StoryPage];
            storyPageText.text = (StoryPage + 1).ToString("00") + " / " + storyPages.Length.ToString("00");
            storyNextText.text = StoryPage == storyPages.Length - 1 ? "睁开眼睛" : "继续";
        }

        public void BeginAdventure()
        {
            if (Started) return;
            Started = true;
            if (menuPanel != null) menuPanel.SetActive(false);
            if (storyPanel != null) storyPanel.SetActive(false);
        }

        public void ReturnToTitle()
        {
            Restart();
            StoryPage = 0;
            if (menuPanel == null) return;
            Started = false;
            menuPanel.SetActive(true);
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void Update()
        {
            UpdateVisualFeedback();
            if (Camera.main != null && Mathf.Abs(Camera.main.aspect - lastAspect) > 0.001f)
            {
                lastAspect = Camera.main.aspect;
                Camera.main.orthographicSize = Mathf.Max(4.5f, 8f / Mathf.Max(0.1f, lastAspect));
            }
            if (Shaking)
            {
                shakeRemaining = Mathf.Max(0, shakeRemaining - Time.deltaTime);
                trash.transform.position = trashStart + Vector3.right * Mathf.Sin(Time.time * 48) * 0.08f;
                if (!Shaking)
                {
                    trash.transform.position = trashStart;
                    BatteryRevealed = true;
                    if (assembly != null) assembly.RevealLeg();
                    else battery.gameObject.SetActive(true);
                }
                return;
            }
            if (Input.GetKeyDown(KeyCode.Escape)) ClosePanels();
            if (Input.GetMouseButtonDown(0) && !InputBlocked &&
                (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()))
                ClickAt(Input.mousePosition);
            if (pending != null && !robot.IsMoving && !InputBlocked)
            {
                RoomInteractable target = pending;
                pending = null;
                if (Vector2.Distance(robot.transform.position, target.StandPosition) < 0.12f) Interact(target);
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
            if (InputBlocked || Won) return;
            if (assembly != null && !assembly.LegAttached) return;
            pending = null;
            robot.MoveTo(point);
        }

        public void RequestInteraction(RoomInteractable target)
        {
            if (InputBlocked || Won || target == null || !target.gameObject.activeInHierarchy) return;
            if (assembly != null && !assembly.LegAttached)
            {
                if (assembly.CanReachWithoutLeg(target)) Interact(target);
                return;
            }
            pending = target;
            robot.MoveTo(target.StandPosition);
        }

        private void Interact(RoomInteractable target)
        {
            if (assembly != null && assembly.HandleInteraction(target)) return;
            switch (target.kind)
            {
                case ObjectKind.Trash:
                    if (!BatteryRevealed) inspectPanel.SetActive(true);
                    else Rattle(target.transform);
                    break;
                case ObjectKind.Battery:
                    if (!BatteryRevealed || HasBattery || Powered) return;
                    HasBattery = true;
                    battery.gameObject.SetActive(false);
                    inventoryButton.interactable = true;
                    if (batteryIcon != null) batteryIcon.SetActive(true);
                    break;
                case ObjectKind.Console:
                    if (!Powered)
                    {
                        if (!HasBattery || !BatterySelected) { powerFeedbackUntil = Time.time + 0.65f; return; }
                        HasBattery = BatterySelected = false;
                        Powered = true;
                        inventoryButton.interactable = false;
                        if (batteryIcon != null) batteryIcon.SetActive(false);
                        if (batterySocket != null) batterySocket.color = new Color(0.78f, 0.61f, 0.27f);
                        inventoryButton.GetComponent<Image>().color = new Color(0.19f, 0.25f, 0.27f);
                        terminalLight.color = Color.cyan;
                    }
                    if (!DoorOpen && (mechanism == null || !mechanism.InterlockReleased)) puzzlePanel.SetActive(true);
                    break;
                case ObjectKind.WorkshopValve:
                    if (mechanism != null && target == mechanism.valve) mechanism.TryTurnValve();
                    break;
                case ObjectKind.Door:
                    if (!DoorOpen) { Rattle(door.transform); return; }
                    Won = true;
                    robot.Stop();
                    if (!string.IsNullOrEmpty(nextRoomScene))
                    {
                        SceneManager.LoadScene(nextRoomScene);
                        return;
                    }
                    winPanel.SetActive(true);
                    break;
                case ObjectKind.Scenery:
                    Rattle(target.transform);
                    break;
            }
        }

        public void ShakeTrash()
        {
            if (!inspectPanel.activeSelf || BatteryRevealed || Shaking || Won) return;
            inspectPanel.SetActive(false);
            pending = null;
            robot.Stop();
            shakeRemaining = 0.8f;
        }

        public void SelectBattery()
        {
            if (!HasBattery || InputBlocked) return;
            BatterySelected = !BatterySelected;
            inventoryButton.GetComponent<Image>().color = BatterySelected ? new Color(0.75f, 0.48f, 0.15f) : new Color(0.19f, 0.25f, 0.27f);
        }

        public void PressPuzzle(int index)
        {
            if (!puzzlePanel.activeSelf || !Powered || DoorOpen || Won || PuzzleProgress >= sequence.Length ||
                (mechanism != null && mechanism.InterlockReleased)) return;
            if (index < 0 || index > 2) return;
            if (index != sequence[PuzzleProgress])
            {
                PuzzleProgress = 0;
                incorrectUntil = Time.time + 0.5f;
                return;
            }
            PuzzleProgress++;
            incorrectUntil = 0;
            if (PuzzleProgress < sequence.Length) return;
            if (mechanism != null) mechanism.ReleaseInterlock();
            else DoorOpen = true;
            puzzlePanel.SetActive(false);
            doorLeaf.color = Color.white;
        }

        public void OpenPneumaticDoor()
        {
            if (!Started || Won || !Powered || DoorOpen || mechanism == null ||
                !mechanism.isActiveAndEnabled || !mechanism.InterlockReleased ||
                !mechanism.Cycling || mechanism.Pressure < 1f) return;
            DoorOpen = true;
            doorLeaf.color = Color.white;
        }

        public void CompleteAssembly()
        {
            if (assembly == null || !assembly.LegAttached || !assembly.ArmAttached) return;
            Powered = true;
            terminalLight.color = new Color(0.32f, 0.65f, 0.5f);
            if (mechanism != null) mechanism.ReleaseInterlock();
        }

        private void Rattle(Transform target)
        {
            if (rattlingObject != null) rattlingObject.position = rattleStart;
            rattlingObject = target;
            rattleStart = target.position;
            rattleUntil = Time.time + 0.35f;
        }

        private void UpdateVisualFeedback()
        {
            if (DoorOpen)
                doorLeaf.transform.localPosition = Vector3.MoveTowards(doorLeaf.transform.localPosition, doorStart + Vector3.up * 1.45f, Time.deltaTime * 2.6f);
            if (trashGlint != null)
            {
                float phase = Mathf.Repeat(Time.time, 4.2f);
                trashGlint.enabled = Started && !BatteryRevealed && phase < 0.32f;
                trashGlint.color = new Color(0.86f, 0.82f, 0.61f, Mathf.Sin(phase / 0.32f * Mathf.PI) * 0.65f);
            }
            if (!Powered)
                terminalLight.color = Time.time < powerFeedbackUntil && Mathf.Sin(Time.time * 24) > 0 ? new Color(0.8f, 0.32f, 0.14f) : new Color(0.35f, 0.18f, 0.14f);
            if (puzzleIndicators != null)
                for (int i = 0; i < puzzleIndicators.Length; i++)
                    puzzleIndicators[i].color = Time.time < incorrectUntil ? new Color(0.65f, 0.2f, 0.15f) :
                        (i < PuzzleProgress ? new Color(0.28f, 0.68f, 0.48f) : new Color(0.22f, 0.29f, 0.29f));
            if (rattlingObject != null)
            {
                rattlingObject.position = rattleStart + Vector3.right * Mathf.Sin(Time.time * 45) * 0.025f;
                if (Time.time >= rattleUntil) { rattlingObject.position = rattleStart; rattlingObject = null; }
            }
        }

        public void ClosePanels()
        {
            if (!Started) { if (storyPanel != null && storyPanel.activeSelf) ReturnToTitle(); return; }
            if (Won || Shaking || (mechanism != null && mechanism.Cycling)) return;
            inspectPanel.SetActive(false);
            puzzlePanel.SetActive(false);
            if (mechanism == null || !mechanism.InterlockReleased) PuzzleProgress = 0;
            incorrectUntil = powerFeedbackUntil = 0;
            if (rattlingObject != null) { rattlingObject.position = rattleStart; rattlingObject = null; }
        }

        public void Restart()
        {
            Started = true;
            if (menuPanel != null) menuPanel.SetActive(false);
            if (storyPanel != null) storyPanel.SetActive(false);
            pending = null;
            shakeRemaining = 0;
            if (mechanism != null) mechanism.ResetMechanism();
            BatteryRevealed = HasBattery = BatterySelected = Powered = DoorOpen = Won = false;
            PuzzleProgress = 0;
            incorrectUntil = powerFeedbackUntil = 0;
            if (rattlingObject != null) { rattlingObject.position = rattleStart; rattlingObject = null; }
            robot.transform.position = robotStart;
            robot.Stop();
            trash.transform.position = trashStart;
            battery.gameObject.SetActive(false);
            doorLeaf.transform.localPosition = doorStart;
            doorLeaf.color = Color.white;
            terminalLight.color = new Color(0.35f, 0.18f, 0.14f);
            inspectPanel.SetActive(false);
            puzzlePanel.SetActive(false);
            winPanel.SetActive(false);
            inventoryButton.interactable = false;
            inventoryText.text = "";
            if (batteryIcon != null) batteryIcon.SetActive(false);
            if (batterySocket != null) batterySocket.color = new Color(0.1f, 0.15f, 0.15f);
            inventoryButton.GetComponent<Image>().color = new Color(0.19f, 0.25f, 0.27f);
            objectiveText.text = statusText.text = puzzleText.text = "";
            if (assembly != null) assembly.ResetAssembly();
        }
    }
}
