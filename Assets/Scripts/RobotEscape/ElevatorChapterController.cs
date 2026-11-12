using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RobotEscape
{
    public enum ElevatorAction { None, Soil, Control, Clues, Door }

    public sealed class ElevatorChapterController : MonoBehaviour
    {
        public RobotWalker robot;
        public ElevatorPanel panel;
        public WalkableArea2D walkArea;
        public Transform start, soilStand, panelStand, clueStand, doorStand;
        public Collider2D soilHit, panelHit, clueHit, doorHit;
        public GameObject soilLampCover;
        public ElevatorCircuitGraphic roomCircuit;
        public Image[] floorIndicators;
        public Sprite lampIcon;

        public ElevatorCircuit Circuit { get; } = new ElevatorCircuit();
        public int LampCount { get; private set; }
        public bool LampSelected { get; private set; }
        public bool SoilLampCollected { get; private set; }
        public bool TowerLampCollected { get; private set; }
        public int CurrentFloor { get; private set; }
        public bool Traveling { get; private set; }
        public bool InputBlocked => busy || Traveling || panel.IsOpen || (clues != null && clues.activeSelf);

        private static readonly Color Copper = new Color(.88f, .71f, .45f);
        private static readonly Color Paper = new Color(.93f, .88f, .74f);
        private static readonly Color Dark = new Color(.1f, .12f, .1f, .96f);
        private static readonly Color Slot = new Color(.18f, .22f, .18f);
        private ElevatorAction pending;
        private bool busy;
        private int suppressInputFrame;
        private float speechRemaining, lastAspect;
        private GameObject hud, clues, speech;
        private Text floorLabel, lampCount, speechText;
        private Button inventory;
        private Outline selection;
        private Image fade;
        private Font font;

        private void Start()
        {
            robot.walkArea = walkArea;
            Vector2 min = walkArea.boundary[0], max = min;
            foreach (Vector2 point in walkArea.boundary)
            {
                min = Vector2.Min(min, point);
                max = Vector2.Max(max, point);
            }
            robot.minWalk = min;
            robot.maxWalk = max;
            robot.transform.position = start.position;
            robot.Stop();
            if (soilLampCover != null) soilLampCover.SetActive(false);
            if (roomCircuit != null) roomCircuit.circuit = Circuit;
            BuildHud();
            Refresh();
            suppressInputFrame = Time.frameCount + 1;
        }

        private void Update()
        {
            Camera camera = Camera.main;
            if (camera != null && Mathf.Abs(camera.aspect - lastAspect) > .001f)
            {
                lastAspect = camera.aspect;
                camera.orthographicSize = Mathf.Max(4.5f, 8f / Mathf.Max(.1f, lastAspect));
            }
            if (speechRemaining > 0)
            {
                speechRemaining -= Time.deltaTime;
                if (speechRemaining <= 0) speech.SetActive(false);
            }
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (panel.IsOpen) panel.Close();
                else if (clues.activeSelf) CloseClues();
            }
            if (InputBlocked || Time.frameCount <= suppressInputFrame) return;
            if (Input.GetMouseButtonDown(0) &&
                (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()))
                ClickAt(Input.mousePosition);
            if (pending == ElevatorAction.None || robot.IsMoving) return;
            ElevatorAction action = pending;
            pending = ElevatorAction.None;
            if (Vector2.Distance(robot.transform.position, StandFor(action).position) < .13f) Interact(action);
        }

        public void ClickAt(Vector2 screenPosition)
        {
            if (InputBlocked || Camera.main == null) return;
            Vector2 point = Camera.main.ScreenToWorldPoint(screenPosition);
            if (soilHit.OverlapPoint(point)) RequestInteraction(ElevatorAction.Soil);
            else if (panelHit.OverlapPoint(point)) RequestInteraction(ElevatorAction.Control);
            else if (clueHit.OverlapPoint(point)) RequestInteraction(ElevatorAction.Clues);
            else if (doorHit.OverlapPoint(point)) RequestInteraction(ElevatorAction.Door);
            else MoveTo(point);
        }

        public void MoveTo(Vector2 point)
        {
            if (InputBlocked) return;
            pending = ElevatorAction.None;
            robot.MoveTo(point);
        }

        public void RequestInteraction(ElevatorAction action)
        {
            if (InputBlocked || action == ElevatorAction.None || !Enum.IsDefined(typeof(ElevatorAction), action)) return;
            pending = action;
            robot.MoveTo(StandFor(action).position);
        }

        private Transform StandFor(ElevatorAction action)
        {
            switch (action)
            {
                case ElevatorAction.Soil: return soilStand;
                case ElevatorAction.Control: return panelStand;
                case ElevatorAction.Clues: return clueStand;
                default: return doorStand;
            }
        }

        private void Interact(ElevatorAction action)
        {
            switch (action)
            {
                case ElevatorAction.Soil:
                    if (SoilLampCollected) Say("沙沙……只剩下潮湿的泥土。");
                    else StartCoroutine(InvestigateSoil());
                    break;
                case ElevatorAction.Control:
                    Say("");
                    panel.Open(Circuit, CurrentFloor, RequestTravel, InstallSelectedLamp,
                        CloseControl, RecoverLamp);
                    Refresh();
                    break;
                case ElevatorAction.Clues:
                    Say("");
                    clues.SetActive(true);
                    break;
                case ElevatorAction.Door:
                    Say(CurrentFloor == 2 ? "门缝外传来高处的风声。" : "门外只剩下低沉的齿轮声。");
                    break;
            }
        }

        private IEnumerator InvestigateSoil()
        {
            busy = true;
            Say("沙沙……");
            yield return new WaitForSeconds(.55f);
            SoilLampCollected = true;
            LampCount++;
            LampSelected = false;
            if (soilLampCover != null) soilLampCover.SetActive(true);
            busy = false;
            Refresh();
            Say("一盏沾着泥的信号灯。");
        }

        public void SelectLamp()
        {
            if (LampCount == 0 || busy || Traveling || clues.activeSelf) return;
            pending = ElevatorAction.None;
            robot.Stop();
            LampSelected = !LampSelected;
            Refresh();
        }

        private void InstallSelectedLamp(int socket)
        {
            if (!LampSelected || LampCount == 0)
            {
                panel.SetFeedback("空灯座里露着两片触点。");
                return;
            }
            if (!Circuit.InstallLamp(socket)) return;
            LampCount--;
            LampSelected = false;
            Refresh();
            panel.SetFeedback("咔哒。");
        }

        private void RecoverLamp(int socket)
        {
            if (!Circuit.RemoveLamp(socket)) return;
            LampCount++;
            LampSelected = false;
            Refresh();
            panel.SetFeedback("灯从接座里退了出来。");
        }

        // Called by the later rooftop pickup; this room never grants the eighth lamp itself.
        public bool ReceiveTowerLamp()
        {
            if (CurrentFloor != 2 || TowerLampCollected || Traveling) return false;
            TowerLampCollected = true;
            LampCount++;
            LampSelected = false;
            Refresh();
            return true;
        }

        private void RequestTravel(int floor)
        {
            if (Traveling || busy || Circuit.MatchedFloor != floor) return;
            if (CurrentFloor == floor)
            {
                panel.SetFeedback("楼层指示灯没有改变。");
                return;
            }
            StartCoroutine(Ride(floor));
        }

        private IEnumerator Ride(int floor)
        {
            Traveling = true;
            panel.Close();
            robot.Stop();
            pending = ElevatorAction.None;
            Say("");
            Camera camera = Camera.main;
            Vector3 origin = camera != null ? camera.transform.position : Vector3.zero;
            const float duration = 1.45f;
            for (float elapsed = 0; elapsed < duration; elapsed += Time.deltaTime)
            {
                float t = Mathf.Clamp01(elapsed / duration);
                float strength = Mathf.Sin(t * Mathf.PI);
                fade.color = new Color(.04f, .045f, .035f, strength * .42f);
                if (camera != null) camera.transform.position = origin + Vector3.up * (Mathf.Sin(elapsed * 65f) * .012f * strength);
                yield return null;
            }
            if (camera != null) camera.transform.position = origin;
            fade.color = Color.clear;
            CurrentFloor = floor;
            Traveling = false;
            suppressInputFrame = Time.frameCount + 1;
            Refresh();
            Say("叮。");
        }

        private void Refresh()
        {
            floorLabel.text = "06  塔内电梯  ·  " + FloorName(CurrentFloor);
            lampCount.text = "信号灯  " + LampCount;
            inventory.interactable = LampCount > 0 && !busy && !Traveling;
            inventory.image.color = LampSelected ? new Color(.4f, .29f, .14f) : Slot;
            selection.enabled = LampSelected;
            if (roomCircuit != null) roomCircuit.SetVerticesDirty();
            for (int i = 0; floorIndicators != null && i < floorIndicators.Length; i++)
                if (floorIndicators[i] != null) floorIndicators[i].color = ElevatorCircuit.Floors[i] == CurrentFloor
                    ? new Color(.93f, .72f, .3f) : new Color(.24f, .25f, .16f);
            if (panel.IsOpen)
            {
                panel.Refresh();
                panel.SetInventory(LampCount, LampSelected, SelectLamp);
            }
        }

        public static string FloorName(int floor) => floor == 2 ? "塔顶" : floor + " 层";

        private void Say(string text)
        {
            speechText.text = text;
            speechRemaining = string.IsNullOrEmpty(text) ? 0 : 3f;
            speech.SetActive(speechRemaining > 0);
        }

        private void CloseControl()
        {
            suppressInputFrame = Time.frameCount + 1;
            if (roomCircuit != null) roomCircuit.SetVerticesDirty();
        }

        private void CloseClues()
        {
            clues.SetActive(false);
            suppressInputFrame = Time.frameCount + 1;
        }

        private void BuildHud()
        {
            if (EventSystem.current == null)
                new GameObject("Elevator Event System", typeof(EventSystem), typeof(StandaloneInputModule));
            font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "SimHei", "Arial" }, 24);
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            hud = new GameObject("Elevator HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            hud.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            hud.GetComponent<Canvas>().sortingOrder = 5;
            var scaler = hud.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = .5f;
            RectTransform root = hud.GetComponent<RectTransform>();
            var area = Plate("Floor", root, Dark, new Vector2(0, 1), new Vector2(142, -36), new Vector2(254, 38));
            floorLabel = Label(area.rectTransform, "", 18, Copper);
            var bag = Plate("Inventory", root, Dark, Vector2.zero, new Vector2(69, 76), new Vector2(110, 132));
            Label(Place("Heading", bag.rectTransform, new Vector2(0, 48), new Vector2(100, 22)), "物品栏", 16, Copper);
            var item = Plate("Lamp inventory", bag.rectTransform, Slot, new Vector2(.5f, .5f), new Vector2(0, -9), new Vector2(84, 84));
            inventory = ButtonFor(item, SelectLamp);
            selection = item.gameObject.AddComponent<Outline>();
            selection.effectColor = Copper;
            selection.effectDistance = new Vector2(2, -2);
            selection.useGraphicAlpha = false;
            if (lampIcon != null)
            {
                var icon = Plate("Signal lamp", item.rectTransform, Color.white, new Vector2(.5f, .5f), new Vector2(0, 10), new Vector2(42, 42));
                icon.sprite = lampIcon;
                icon.preserveAspect = true;
            }
            else
            {
                var cap = Plate("Lamp cap", item.rectTransform, Copper, new Vector2(.5f, .5f), new Vector2(0, 9), new Vector2(22, 30));
                var basePlate = Plate("Lamp base", item.rectTransform, new Color(.4f, .3f, .16f), new Vector2(.5f, .5f), new Vector2(0, -5), new Vector2(30, 7));
                basePlate.raycastTarget = false;
            }
            lampCount = Label(Place("Count", item.rectTransform, new Vector2(0, -27), new Vector2(82, 22)), "", 16, Paper);
            var replay = Plate("Replay elevator", root, Dark, Vector2.one, new Vector2(-182, -36), new Vector2(100, 36));
            Label(replay.rectTransform, "重玩本关", 16, Paper);
            ButtonFor(replay, () => SceneManager.LoadScene(gameObject.scene.name));
            var title = Plate("Return to title", root, Dark, Vector2.one, new Vector2(-71, -36), new Vector2(100, 36));
            Label(title.rectTransform, "返回标题", 16, Paper);
            ButtonFor(title, () => SceneManager.LoadScene("RobotEscapeDemo"));
            var word = Plate("Speech", root, Dark, new Vector2(.5f, 0), new Vector2(0, 42), new Vector2(515, 44));
            speech = word.gameObject;
            speechText = Label(word.rectTransform, "", 20, Paper);
            speech.SetActive(false);
            fade = Plate("Elevator movement", root, Color.clear, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
            Stretch(fade.rectTransform);
            var cover = Plate("Wall clues", root, new Color(.035f, .045f, .035f, .91f), new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
            Stretch(cover.rectTransform);
            cover.raycastTarget = true;
            clues = cover.gameObject;
            var card = Plate("Chalk details", cover.rectTransform, Dark, new Vector2(.5f, .5f), Vector2.zero, new Vector2(730, 292));
            for (int i = 0; i < 4; i++)
            {
                float x = -264 + i * 176;
                var drawing = Place("Pattern " + i, card.rectTransform, new Vector2(x, 15), new Vector2(145, 145));
                var graph = drawing.gameObject.AddComponent<ElevatorCircuitGraphic>();
                graph.clueIndex = i;
                graph.raycastTarget = false;
                Label(Place("Floor " + i, card.rectTransform, new Vector2(x, -88), new Vector2(150, 32)),
                    i == 0 ? "II" : i == 1 ? "I" : i == 2 ? "0" : "−I", 24, Paper);
            }
            var close = Plate("Close clues", card.rectTransform, Slot, new Vector2(.5f, .5f), new Vector2(330, 113), new Vector2(44, 38));
            Label(close.rectTransform, "×", 25, Paper);
            ButtonFor(close, CloseClues);
            clues.SetActive(false);
        }

        private Text Label(RectTransform parent, string value, int size, Color color)
        {
            var rect = Place("Text", parent, Vector2.zero, Vector2.zero);
            Stretch(rect);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            text.raycastTarget = false;
            text.supportRichText = false;
            return text;
        }

        private static Button ButtonFor(Image image, UnityEngine.Events.UnityAction action)
        {
            image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(action);
            return button;
        }

        private static Image Plate(string name, RectTransform parent, Color color, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var rect = Place(name, parent, position, size);
            rect.anchorMin = rect.anchorMax = anchor;
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static RectTransform Place(string name, RectTransform parent, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
