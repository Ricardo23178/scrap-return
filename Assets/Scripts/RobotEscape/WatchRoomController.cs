using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RobotEscape
{
    public sealed class WatchRoomController : MonoBehaviour
    {
        public string nextRoomScene;
        public RobotWalker robot;
        public RoomInteractable photo, notice, dutyLog, radio, npc, exitLever, upperExit, frequencyCard;
        public GameObject inspectPanel, dialoguePanel, radioPanel, chapterPanel;
        public Text inspectTitle, inspectText, dialogueText, tuningReadout, transmissionText, chapterText;
        public Button closeInspect, askCompanionButton, askShutdownButton, closeDialogueButton;
        public Button closeRadioButton, replayButton, restartButton, returnTitleButton;
        public Slider tuningSlider;
        public Image[] waveformBars;
        public Image signalIndicator;
        public Image inspectIllustration;
        public SpriteRenderer radioReceiverLight, exitLamp;
        public Transform leverHandle;
        public Button helmetButton, coatButton;
        public GameObject helmetOverlay, coatOverlay, wearHelmetIcon, wearCoatIcon;

        public const float TargetFrequency = 0.37f;
        public const float FrequencyTolerance = 0.018f;
        public const float LockSeconds = 1.1f;
        public bool PhotoSeen { get; private set; }
        public bool NoticeSeen { get; private set; }
        public bool LogSeen { get; private set; }
        public bool NpcSpoken { get; private set; }
        public bool FrequencyKnown { get; private set; }
        public bool HeardMessage { get; private set; }
        public bool ExitOpen { get; private set; }
        public bool Completed { get; private set; }
        public bool HasHelmet { get; private set; }
        public bool HasCoat { get; private set; }
        public bool HelmetWorn { get; private set; }
        public bool CoatWorn { get; private set; }
        public bool PassedInspection { get; private set; }
        public float SignalStrength { get; private set; }
        public float LockProgress { get { return 0; } }
        public bool IsReplaying { get { return false; } }
        public bool InputBlocked { get { return Completed || Active(inspectPanel) || Active(dialoguePanel) || Active(radioPanel) || Active(chapterPanel); } }

        private const string Message = "城门值勤簿\n\n换岗铃响后，值勤机器人由侧门入城。\n今夜的登记页上，多了三个漆黑的指印。";
        private RoomInteractable pending;
        private Vector3 robotStart, rattleOrigin;
        private Quaternion leverStart;
        private Transform rattling;
        private Transform exitLeaf;
        private Vector3 exitLeafStart;
        private float rattleUntil, lastAspect;
        private int suppressInputUntilFrame;

        private void Start()
        {
            Application.runInBackground = true;
            robotStart = robot != null ? robot.transform.position : Vector3.zero;
            leverStart = leverHandle != null ? leverHandle.localRotation : Quaternion.identity;
            SpriteRenderer doorImage = upperExit != null ? upperExit.GetComponentInChildren<SpriteRenderer>() : null;
            exitLeaf = doorImage != null ? doorImage.transform : null;
            if (exitLeaf != null) exitLeafStart = exitLeaf.localPosition;
            Font font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "SimHei", "Arial" }, 24);
            if (font != null)
                foreach (Text label in FindObjectsOfType<Text>(true)) label.font = font;
            Bind(closeInspect, ClosePanels);
            Bind(closeDialogueButton, ClosePanels);
            Bind(closeRadioButton, ClosePanels);
            Bind(askCompanionButton, AskCompanion);
            Bind(askShutdownButton, AskShutdown);
            Bind(replayButton, ReplayMessage);
            Bind(restartButton, Restart);
            Bind(returnTitleButton, ReturnToTitle);
            Bind(helmetButton, WearHelmet);
            Bind(coatButton, WearCoat);
            if (tuningSlider != null) tuningSlider.interactable = false;
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
                case ObjectKind.Photo:
                    PhotoSeen = true;
                    Inspect(target, "折过的合照", "照片上，你和贝塔挤在同一盏街灯下。\n她的手搭在你的肩上。\n\n你把照片上的煤灰轻轻抹掉。");
                    break;
                case ObjectKind.Notice:
                    NoticeSeen = true;
                    Inspect(target, "褪色的值勤画像", "画像里的机器人，帽檐和肩头都已磨白。\n胸前的一排铜扣，倒还亮着。\n\n守卫瞥了一眼画像，拉了拉自己的衣领。");
                    break;
                case ObjectKind.DutyLog:
                case ObjectKind.DisguiseCoat:
                    LogSeen = true;
                    if (HasCoat) return;
                    HasCoat = true;
                    target.gameObject.SetActive(false);
                    RefreshDisguise();
                    break;
                case ObjectKind.FrequencyCard:
                case ObjectKind.DisguiseHat:
                    FrequencyKnown = true;
                    if (HasHelmet) return;
                    HasHelmet = true;
                    target.gameObject.SetActive(false);
                    RefreshDisguise();
                    break;
                case ObjectKind.Npc:
                case ObjectKind.GateGuard:
                    NpcSpoken = true;
                    OpenModal(dialoguePanel);
                    CheckDisguise();
                    break;
                case ObjectKind.Radio:
                    HeardMessage = true;
                    Inspect(target, "城门值勤簿", Message);
                    break;
                case ObjectKind.ExitLever:
                    if (!PassedInspection || !HelmetWorn || !CoatWorn) { Rattle(target.transform); return; }
                    ExitOpen = true;
                    if (leverHandle != null) leverHandle.localRotation = leverStart * Quaternion.Euler(0, 0, -48);
                    break;
                case ObjectKind.UpperExit:
                    if (!ExitOpen || !PassedInspection || !HelmetWorn || !CoatWorn) { Rattle(target.transform); return; }
                    Completed = true;
                    if (!string.IsNullOrEmpty(nextRoomScene))
                    {
                        if (robot != null) robot.Stop();
                        SceneManager.LoadScene(nextRoomScene);
                        return;
                    }
                    OpenModal(chapterPanel);
                    SetText(chapterText, "守卫已经转过身去。\n你混进了机械城，街巷深处传来金属箱拖动的响声。\n\n贝塔，你会在哪里？\n第二关 · 混进城");
                    break;
                default:
                    Rattle(target.transform);
                    break;
            }
        }

        public void AskCompanion()
        {
            if (!Active(dialoguePanel)) return;
            SetText(dialogueText, PassedInspection ? "守卫：贝塔？这儿只记换岗，不记街坊。\n别堵着门。" : "守卫把合照推了回来，没有回答。");
        }

        public void AskShutdown()
        {
            if (!Active(dialoguePanel)) return;
            CheckDisguise();
        }

        public void CheckDisguise()
        {
            if (!Active(dialoguePanel) || Completed) return;
            if (HasHelmet && HasCoat && HelmetWorn && CoatWorn)
            {
                PassedInspection = true;
                SetText(dialogueText, "守卫打了个呵欠，敲了一下门旁的通行灯。\n\n“换岗？进去吧。”");
            }
            else SetText(dialogueText, "守卫打量着你，长柄扳手横在门前。\n\n“不在这一班。退后。”");
            UpdateFeedback();
        }

        public void WearHelmet()
        {
            if (InputBlocked || !HasHelmet || HelmetWorn) return;
            pending = null;
            if (robot != null) robot.Stop();
            HelmetWorn = true;
            RefreshDisguise();
        }

        public void WearCoat()
        {
            if (InputBlocked || !HasCoat || CoatWorn) return;
            pending = null;
            if (robot != null) robot.Stop();
            CoatWorn = true;
            RefreshDisguise();
        }

        private void RefreshDisguise()
        {
            SetActive(helmetOverlay, HelmetWorn);
            SetActive(coatOverlay, CoatWorn);
            SetActive(wearHelmetIcon, HasHelmet);
            SetActive(wearCoatIcon, HasCoat);
            if (helmetButton != null) helmetButton.interactable = HasHelmet && !HelmetWorn;
            if (coatButton != null) coatButton.interactable = HasCoat && !CoatWorn;
        }

        private void Inspect(RoomInteractable target, string title, string text)
        {
            OpenModal(inspectPanel);
            SetText(inspectTitle, title);
            SetText(inspectText, text);
            if (inspectIllustration != null)
            {
                SpriteRenderer artwork = target != null ? target.GetComponentInChildren<SpriteRenderer>() : null;
                inspectIllustration.sprite = artwork != null ? artwork.sprite : null;
                inspectIllustration.preserveAspect = true;
                inspectIllustration.enabled = inspectIllustration.sprite != null;
            }
        }

        private void OpenModal(GameObject panel)
        {
            ClosePanels();
            pending = null;
            if (robot != null) robot.Stop();
            RestoreRattle();
            SetActive(panel, true);
        }

        public void ClosePanels()
        {
            SetActive(inspectPanel, false);
            SetActive(dialoguePanel, false);
            SetActive(radioPanel, false);
            pending = null;
            suppressInputUntilFrame = Time.frameCount + 1;
        }

        public void ReplayMessage()
        {
            if (!Active(radioPanel)) return;
            SetText(transmissionText, Message);
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
            if (exitLeaf != null && ExitOpen)
                exitLeaf.localPosition = Vector3.MoveTowards(exitLeaf.localPosition, exitLeafStart + Vector3.up * 2.8f, Time.deltaTime * 2.6f);
            if (rattling != null)
            {
                rattling.position = rattleOrigin + Vector3.right * Mathf.Sin(Time.time * 40) * 0.025f;
                if (Time.time >= rattleUntil) RestoreRattle();
            }
            if (radioReceiverLight != null) radioReceiverLight.color = new Color(0.35f, 0.46f, 0.39f);
            if (exitLamp != null) exitLamp.color = PassedInspection ? new Color(0.46f, 0.8f, 0.57f) : new Color(0.55f, 0.31f, 0.16f);
        }

        public void Restart()
        {
            ClosePanels();
            RestoreRattle();
            PhotoSeen = NoticeSeen = LogSeen = NpcSpoken = FrequencyKnown = HeardMessage = ExitOpen = Completed = false;
            HasHelmet = HasCoat = HelmetWorn = CoatWorn = PassedInspection = false;
            SetActive(chapterPanel, false);
            if (frequencyCard != null) frequencyCard.gameObject.SetActive(true);
            if (dutyLog != null) dutyLog.gameObject.SetActive(true);
            if (robot != null) { robot.transform.position = robotStart; robot.Stop(); }
            if (leverHandle != null) leverHandle.localRotation = leverStart;
            if (exitLeaf != null) exitLeaf.localPosition = exitLeafStart;
            if (tuningSlider != null)
            {
                tuningSlider.minValue = 0; tuningSlider.maxValue = 1;
                tuningSlider.wholeNumbers = false;
                tuningSlider.SetValueWithoutNotify(0.1f);
            }
            SignalStrength = 0;
            SetText(inspectTitle, ""); SetText(inspectText, ""); SetText(dialogueText, "");
            if (inspectIllustration != null) { inspectIllustration.sprite = null; inspectIllustration.enabled = false; }
            SetText(transmissionText, ""); SetText(chapterText, "");
            SetText(tuningReadout, "城门值勤簿");
            SetActive(replayButton != null ? replayButton.gameObject : null, false);
            if (signalIndicator != null) signalIndicator.color = new Color(0.31f, 0.22f, 0.15f);
            if (waveformBars != null)
                foreach (Image bar in waveformBars)
                    if (bar != null) bar.rectTransform.localScale = new Vector3(1, 0.1f, 1);
            RefreshDisguise();
            UpdateFeedback();
        }

        public void ReturnToTitle()
        {
            ClosePanels();
            if (robot != null) robot.Stop();
            SceneManager.LoadScene("RobotEscapeDemo");
        }

        private static bool Active(GameObject panel) { return panel != null && panel.activeSelf; }
        private static void SetActive(GameObject panel, bool active) { if (panel != null) panel.SetActive(active); }
        private static void SetText(Text label, string text) { if (label != null) label.text = text; }
        private static void Bind(Button button, UnityEngine.Events.UnityAction action) { if (button != null) button.onClick.AddListener(action); }
    }
}
