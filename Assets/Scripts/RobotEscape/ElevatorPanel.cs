using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RobotEscape
{
    public sealed class ElevatorPanel : MonoBehaviour
    {
        public Sprite faceplate;
        public bool IsOpen { get; private set; }
        private static readonly Color Paper = new Color32(237, 223, 193, 255);
        private static readonly Color Copper = new Color32(223, 180, 117, 255);
        private static readonly Color Dark = new Color32(37, 43, 34, 255);
        private ElevatorCircuit circuit;
        private Action<int> onTravel, onSocket, onRemove;
        private Action onClose, selectInventory;
        private GameObject overlay;
        private RectTransform content;
        private ElevatorCircuitGraphic diagram;
        private readonly Button[] nodeButtons = new Button[8];
        private readonly Image[] floorLights = new Image[4];
        private Button inventory;
        private Outline inventoryOutline;
        private Text inventoryText, feedback;
        private Font font;
        private Texture2D circleTexture;
        private Sprite circle;
        private int currentFloor;

        public void Open(ElevatorCircuit value, int floor, Action<int> travel, Action<int> emptySocket,
            Action close, Action<int> removeLamp = null)
        {
            if (IsOpen || value == null) return;
            if (overlay == null) BuildOverlay();
            circuit = value; currentFloor = floor;
            onTravel = travel; onSocket = emptySocket; onClose = close; onRemove = removeLamp;
            IsOpen = true;
            overlay.SetActive(true);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            SetFeedback("");
            Refresh();
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            circuit.CancelSelection();
            overlay.SetActive(false);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            Action closed = onClose;
            onClose = null; onTravel = onSocket = onRemove = null;
            closed?.Invoke();
        }

        public void Refresh()
        {
            if (diagram == null || circuit == null) return;
            diagram.circuit = circuit;
            diagram.SetVerticesDirty();
            for (int i = 0; i < floorLights.Length; i++)
                floorLights[i].color = ElevatorCircuit.Floors[i] == currentFloor ? new Color32(220, 179, 77, 255) : new Color32(57, 63, 40, 255);
        }

        public void SetInventory(int count, bool selected, Action select)
        {
            if (overlay == null) BuildOverlay();
            selectInventory = select;
            inventoryText.text = "信号灯  " + count;
            inventory.interactable = count > 0;
            inventory.image.color = selected ? new Color32(107, 81, 42, 255) : Dark;
            inventoryOutline.enabled = selected;
        }

        public void SetFeedback(string text) { if (feedback != null) feedback.text = text; }
        public Button GetNodeButton(int index) { return index >= 0 && index < 8 ? nodeButtons[index] : null; }

        public void PressNode(int node)
        {
            if (!IsOpen || node < 0 || node >= 8) return;
            if (!circuit.HasLamp(node)) onSocket?.Invoke(node);
            else { circuit.SelectNode(node); SetFeedback(""); }
            Refresh();
        }

        public bool RemoveNode(int node)
        {
            if (!IsOpen || (node != 1 && node != 6) || !circuit.HasLamp(node) || onRemove == null) return false;
            int before = circuit.InstalledMask;
            onRemove(node);
            Refresh();
            return before != circuit.InstalledMask;
        }

        public void ClearWires()
        {
            if (!IsOpen) return;
            circuit.Clear();
            SetFeedback("");
            Refresh();
        }

        public bool TryTravel()
        {
            if (!IsOpen) return false;
            int floor = circuit.MatchedFloor;
            if (floor == ElevatorCircuit.Unmatched) { SetFeedback("咔……"); return false; }
            if (floor == currentFloor) { SetFeedback("已经停在这一层。"); return false; }
            Action<int> travel = onTravel;
            Close();
            travel?.Invoke(floor);
            return true;
        }

        private void Update()
        {
            if (!IsOpen) return;
            if (Input.GetKeyDown(KeyCode.Escape)) Close();
            if (Input.GetKeyDown(KeyCode.R)) ClearWires();
            RectTransform canvas = overlay.GetComponent<RectTransform>();
            float scale = Mathf.Min(1f, Mathf.Min(canvas.rect.width / 780f, canvas.rect.height / 680f));
            content.localScale = Vector3.one * scale;
        }

        private void BuildOverlay()
        {
            if (EventSystem.current == null) new GameObject("Elevator EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "SimHei", "Arial" }, 24);
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            circleTexture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++)
                circleTexture.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(15.5f - Vector2.Distance(new Vector2(x, y), new Vector2(15.5f, 15.5f)))));
            circleTexture.Apply();
            circle = Sprite.Create(circleTexture, new Rect(0, 0, 32, 32), new Vector2(.5f, .5f));
            overlay = new GameObject("Elevator Control Overlay", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = overlay.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 100;
            CanvasScaler scaler = overlay.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = .5f;
            Image shade = Plate("Control backdrop", overlay.transform, Vector2.zero, Vector2.zero, new Color(0.025f, .035f, .025f, .94f));
            shade.rectTransform.anchorMin = Vector2.zero; shade.rectTransform.anchorMax = Vector2.one;
            shade.rectTransform.sizeDelta = Vector2.zero; shade.raycastTarget = true;
            content = Place("Control layout", overlay.transform, Vector2.zero, new Vector2(760, 660));
            Image art = Plate("Control faceplate", content, new Vector2(-115, 0), new Vector2(420, 630), Color.white);
            art.sprite = faceplate; art.preserveAspect = true;
            RectTransform graph = Place("Signal circuit", art.transform, new Vector2(0, 630f * (.5f - 975f / 1536f)), Vector2.one * (540f * 420f / 1024f));
            diagram = graph.gameObject.AddComponent<ElevatorCircuitGraphic>();
            diagram.raycastTarget = false;
            for (int i = 0; i < 8; i++)
            {
                int node = i;
                Vector2 at = ElevatorCircuitGraphic.NodePosition(i, new Rect(-graph.sizeDelta * .5f, graph.sizeDelta));
                Button button = MakeButton("Lamp " + i, graph, at, Vector2.one * 38f, "", () => PressNode(node));
                button.image.color = Color.clear;
                var rightClick = button.gameObject.AddComponent<ElevatorLampPointer>();
                rightClick.panel = this; rightClick.node = node;
                nodeButtons[i] = button;
            }
            string[] floorLabels = { "II", "I", "0", "−I" };
            for (int i = 0; i < 4; i++)
            {
                Vector2 at = new Vector2((500f / 1024f - .5f) * 420f, (.5f - (470f + i * 53f) / 1536f) * 630f);
                Image border = Plate("Floor lamp border " + i, art.transform, at, Vector2.one * 11f, Copper); border.sprite = circle;
                floorLights[i] = Plate("Floor lamp " + i, border.transform, Vector2.zero, Vector2.one * 7f, Dark); floorLights[i].sprite = circle;
                Label("Floor marking " + i, art.transform, at + Vector2.right * 18f, new Vector2(26, 18), floorLabels[i], 13, Paper);
            }
            MakeButton("Start elevator", art.transform, new Vector2(0, -220), new Vector2(78, 26), "↑ ↓", () => TryTravel());
            Label("Control heading", content, new Vector2(230, 190), new Vector2(250, 52), "电梯控制盘", 28, Copper);
            Label("Inventory heading", content, new Vector2(230, 116), new Vector2(220, 30), "物品栏", 18, Copper);
            inventory = MakeButton("Panel lamp inventory", content, new Vector2(230, 53), new Vector2(156, 92), "", () => selectInventory?.Invoke());
            inventoryText = Label("Lamp count", inventory.transform, Vector2.zero, new Vector2(148, 84), "信号灯  0", 22, Paper);
            inventoryOutline = inventory.gameObject.AddComponent<Outline>(); inventoryOutline.effectColor = Copper;
            inventoryOutline.effectDistance = new Vector2(2, -2); inventoryOutline.useGraphicAlpha = false;
            inventoryOutline.enabled = false; inventory.interactable = false;
            MakeButton("Reset elevator wires", content, new Vector2(230, -56), new Vector2(156, 42), "清除接线", ClearWires);
            feedback = Label("Control feedback", content, new Vector2(230, -118), new Vector2(240, 60), "", 18, Paper);
            Label("Control gestures", content, new Vector2(230, -218), new Vector2(270, 70), "点两盏灯接线\n右键取下补装灯", 17, new Color32(176, 169, 144, 255));
            MakeButton("Close elevator panel", content, new Vector2(335, 288), new Vector2(45, 36), "×", Close);
            overlay.SetActive(false);
        }

        private Text Label(string name, Transform parent, Vector2 at, Vector2 size, string text, int fontSize, Color shade)
        {
            Text label = Place(name, parent, at, size).gameObject.AddComponent<Text>();
            label.font = font; label.fontSize = fontSize; label.text = text; label.color = shade;
            label.alignment = TextAnchor.MiddleCenter; label.raycastTarget = false; label.supportRichText = false;
            return label;
        }

        private Button MakeButton(string name, Transform parent, Vector2 at, Vector2 size, string label, UnityEngine.Events.UnityAction action)
        {
            Image image = Plate(name, parent, at, size, Dark); image.raycastTarget = true;
            Button button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            button.onClick.AddListener(action);
            if (!string.IsNullOrEmpty(label)) Label("Caption", image.transform, Vector2.zero, size, label, 20, Paper);
            return button;
        }

        private static Image Plate(string name, Transform parent, Vector2 at, Vector2 size, Color shade)
        {
            Image image = Place(name, parent, at, size).gameObject.AddComponent<Image>();
            image.color = shade; image.raycastTarget = false; return image;
        }

        private static RectTransform Place(string name, Transform parent, Vector2 at, Vector2 size)
        {
            RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.anchoredPosition = at; rect.sizeDelta = size; return rect;
        }

        private void OnDestroy()
        {
            if (overlay != null) Destroy(overlay);
            if (circle != null) Destroy(circle);
            if (circleTexture != null) Destroy(circleTexture);
        }
    }

    public sealed class ElevatorLampPointer : MonoBehaviour, IPointerClickHandler
    {
        public ElevatorPanel panel;
        public int node;
        public void OnPointerClick(PointerEventData data)
        {
            if (data.button == PointerEventData.InputButton.Right && panel != null) panel.RemoveNode(node);
        }
    }
}
