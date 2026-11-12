using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RobotEscape
{
    public enum CityAction { Vendor, BarDoor, Tower, ExitBar, PushMachine, MazeMachine, PushCoin, MazeCoin, Talk, BatteryPickup }

    [Serializable]
    public sealed class CityTarget
    {
        public CityAction action;
        public Collider2D hit;
        public Transform stand;
        public GameObject visible;
        [TextArea] public string dialogue;
    }

    public sealed class CityChapterController : MonoBehaviour
    {
        public RobotWalker robot;
        public CityArcade arcade;
        public GameObject plaza, bar, gateOpenArt;
        public WalkableArea2D plazaWalk, plazaOpenWalk, barWalk;
        public Transform plazaStart, plazaFromBar, barStart, towerInside;
        public CityTarget[] targets;
        public GameObject vendorFirstLamp, vendorSecondLamp, vendorDispensedArt;
        public Sprite coinIcon, batteryIcon;

        public int Coins { get; private set; }
        public int InsertedCoins { get; private set; }
        public int Batteries { get; private set; }
        public bool InBar { get; private set; }
        public bool BatteriesSelected { get; private set; }
        public bool GateOpen { get; private set; }
        public bool Completed { get; private set; }
        public bool PushCoinAvailable => (rewards & ~collected & 1) != 0;
        public bool MazeCoinAvailable => (rewards & ~collected & 2) != 0;
        public bool BatteriesInTray => InsertedCoins == 2 && Batteries == 0 && !GateOpen;
        public bool InputBlocked => Completed || transitionRemaining > 0f || (arcade != null && arcade.IsOpen);

        private const float FadeSeconds = .36f;
        private static readonly Color Copper = new Color(.92f, .76f, .51f);
        private static readonly Color Paper = new Color(.95f, .89f, .76f);
        private static readonly Color Panel = new Color(.1f, .12f, .11f, .94f);
        private static readonly Color Slot = new Color(.18f, .23f, .2f, .97f);
        private static readonly Color Selected = new Color(.42f, .29f, .14f, 1f);

        private int rewards, collected, suppressInputFrame;
        private float transitionRemaining, speechRemaining, lastAspect;
        private CityTarget pending;
        private GameObject speechPanel, winPanel;
        private Text areaLabel, coinCount, batteryCount, speechText;
        private Image fade;
        private Button batterySlot;
        private Outline batterySelection;
        private Font font;

        private void Start()
        {
            BuildHud();
            ChangeRoom(false, plazaStart, false);
        }

        private void Update()
        {
            Camera camera = Camera.main;
            if (camera != null && Mathf.Abs(camera.aspect - lastAspect) > .001f)
            {
                lastAspect = camera.aspect;
                camera.orthographicSize = Mathf.Max(4.5f, 8f / Mathf.Max(.1f, lastAspect));
            }

            float dt = Time.deltaTime;
            if (speechRemaining > 0f)
            {
                speechRemaining -= dt;
                if (speechRemaining <= 0f) speechPanel.SetActive(false);
            }
            if (transitionRemaining > 0f)
            {
                transitionRemaining = Mathf.Max(0f, transitionRemaining - dt);
                fade.color = new Color(.045f, .052f, .045f, transitionRemaining / FadeSeconds);
            }

            if (InputBlocked || Time.frameCount <= suppressInputFrame) return;
            if (Input.GetMouseButtonDown(0) &&
                (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()))
                ClickAt(Input.mousePosition);

            if (pending != null && !robot.IsMoving)
            {
                CityTarget arrived = pending;
                pending = null;
                if (CanUse(arrived) && Vector2.Distance(robot.transform.position, StandFor(arrived)) < .13f)
                    Interact(arrived);
            }
        }

        public void ClickAt(Vector2 screenPosition)
        {
            if (InputBlocked || Camera.main == null) return;

            Vector2 point = Camera.main.ScreenToWorldPoint(screenPosition);
            CityTarget clicked = null;
            foreach (CityTarget target in targets)
            {
                if (!CanUse(target) || !target.hit.OverlapPoint(point)) continue;
                clicked = target;
                if (IsPickup(target.action)) break;
            }
            if (clicked != null) RequestInteraction(clicked);
            else MoveTo(point);
        }

        public void MoveTo(Vector2 point)
        {
            if (InputBlocked) return;
            pending = null;
            robot.MoveTo(point);
        }

        public void RequestInteraction(CityTarget target)
        {
            if (InputBlocked || !CanUse(target)) return;
            pending = target;
            robot.MoveTo(StandFor(target));
        }

        public void SelectBatteries()
        {
            if (InputBlocked || Batteries != 2) return;
            pending = null;
            robot.Stop();
            BatteriesSelected = !BatteriesSelected;
            RefreshHud();
        }

        private Vector2 StandFor(CityTarget target)
        {
            if (target.action == CityAction.Tower && GateOpen) return towerInside.position;
            return target.stand.position;
        }

        private bool CanUse(CityTarget target)
        {
            if (target == null || target.stand == null || target.hit == null ||
                !target.hit.enabled || !target.hit.gameObject.activeInHierarchy) return false;

            switch (target.action)
            {
                case CityAction.PushCoin: return InBar && PushCoinAvailable;
                case CityAction.MazeCoin: return InBar && MazeCoinAvailable;
                case CityAction.BatteryPickup: return !InBar && BatteriesInTray;
                case CityAction.PushMachine:
                case CityAction.MazeMachine:
                case CityAction.ExitBar: return InBar;
                case CityAction.Vendor:
                case CityAction.Tower:
                case CityAction.BarDoor: return !InBar;
                default: return true;
            }
        }

        private void Interact(CityTarget target)
        {
            switch (target.action)
            {
                case CityAction.BarDoor: ChangeRoom(true, barStart, true); break;
                case CityAction.ExitBar: ChangeRoom(false, plazaFromBar, true); break;
                case CityAction.Talk: Say(target.dialogue); break;
                case CityAction.PushMachine: OpenArcade(false); break;
                case CityAction.MazeMachine: OpenArcade(true); break;
                case CityAction.PushCoin: CollectCoin(1); break;
                case CityAction.MazeCoin: CollectCoin(2); break;
                case CityAction.BatteryPickup:
                    Batteries = 2;
                    RefreshWorld();
                    Say("两节电池。");
                    break;
                case CityAction.Vendor: InsertCoin(); break;
                case CityAction.Tower: TalkAtTower(); break;
            }
        }

        private void OpenArcade(bool maze)
        {
            int reward = maze ? 2 : 1;
            if ((rewards & reward) != 0)
            {
                Say((collected & reward) == 0 ? "叮……" : "荧屏暗了下来。");
                return;
            }

            Say("");
            arcade.Open(maze, () =>
            {
                rewards |= reward;
                RefreshWorld();
            }, () =>
            {
                suppressInputFrame = Time.frameCount + 1;
                if ((rewards & ~collected & reward) != 0) Say("叮。");
            });
        }

        private void CollectCoin(int reward)
        {
            collected |= reward;
            Coins++;
            RefreshWorld();
            Say("叮。");
        }

        private void InsertCoin()
        {
            if (InsertedCoins == 2)
            {
                Say(BatteriesInTray ? "咔哒，取货槽响了一声。" : "取货槽空了。");
                return;
            }
            if (Coins == 0)
            {
                Say("咔……");
                return;
            }

            Coins--;
            InsertedCoins++;
            RefreshWorld();
            Say(InsertedCoins == 1 ? "嗒。" : "嗒、咚！");
        }

        private void TalkAtTower()
        {
            if (GateOpen)
            {
                Completed = true;
                robot.Stop();
                Say("");
                winPanel.SetActive(true);
                return;
            }

            if (!BatteriesSelected || Batteries != 2)
            {
                Say(Batteries == 2 ? "他低头看了看电池，把玩偶往前递了递。" : "「咔？……」他轻敲玩偶，翻出两个空槽。");
                return;
            }

            Batteries = 0;
            BatteriesSelected = false;
            GateOpen = true;
            SetWalkArea(plazaOpenWalk);
            RefreshWorld();
            Say("扑棱、扑棱！他抱着小鸟让到门边。");
        }

        public void ContinueToElevator()
        {
            if (Completed) SceneManager.LoadScene("TowerElevator");
        }

        private void ChangeRoom(bool showBar, Transform entry, bool fadeIn)
        {
            pending = null;
            InBar = showBar;
            plaza.SetActive(!showBar);
            bar.SetActive(showBar);
            robot.transform.position = entry.position;
            robot.Stop();
            SetWalkArea(showBar ? barWalk : GateOpen ? plazaOpenWalk : plazaWalk);
            transitionRemaining = fadeIn ? FadeSeconds : 0f;
            fade.color = new Color(.045f, .052f, .045f, fadeIn ? 1f : 0f);
            suppressInputFrame = Time.frameCount + 1;
            Say("");
            RefreshWorld();
        }

        private void SetWalkArea(WalkableArea2D area)
        {
            robot.walkArea = area;
            Vector2 min = area.boundary[0], max = min;
            foreach (Vector2 p in area.boundary)
            {
                min = Vector2.Min(min, p);
                max = Vector2.Max(max, p);
            }
            robot.minWalk = min;
            robot.maxWalk = max;
        }

        private static bool IsPickup(CityAction action) =>
            action == CityAction.PushCoin || action == CityAction.MazeCoin || action == CityAction.BatteryPickup;

        private void RefreshWorld()
        {
            SetVisible(gateOpenArt, GateOpen);
            SetVisible(vendorFirstLamp, InsertedCoins >= 1);
            SetVisible(vendorSecondLamp, InsertedCoins == 2);
            SetVisible(vendorDispensedArt, BatteriesInTray);
            foreach (CityTarget target in targets)
            {
                if (target == null || !IsPickup(target.action)) continue;
                bool available = target.action == CityAction.PushCoin ? PushCoinAvailable
                    : target.action == CityAction.MazeCoin ? MazeCoinAvailable : BatteriesInTray;
                SetVisible(target.visible, available);
                if (target.hit != null) target.hit.enabled = available;
            }
            RefreshHud();
        }

        private void RefreshHud()
        {
            areaLabel.text = InBar ? "05  旧阀酒吧" : "05  齿轮广场";
            coinCount.text = $"硬币  {Coins}";
            batteryCount.text = $"电池  {Batteries}";
            batterySlot.interactable = Batteries == 2;
            batterySlot.image.color = BatteriesSelected ? Selected : Slot;
            batterySelection.enabled = BatteriesSelected;
        }

        private void Say(string text)
        {
            speechText.text = text;
            speechRemaining = string.IsNullOrEmpty(text) ? 0f : 3.4f;
            speechPanel.SetActive(speechRemaining > 0f);
        }

        private void BuildHud()
        {
            if (EventSystem.current == null)
                new GameObject("City Event System", typeof(EventSystem), typeof(StandaloneInputModule));

            font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "SimHei", "Arial" }, 24);
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var hud = new GameObject("City HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = hud.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5;
            var scaler = hud.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = .5f;
            RectTransform layout = hud.GetComponent<RectTransform>();

            var location = Plate("Area label", layout, Panel, new Vector2(0, 1), new Vector2(113, -40), new Vector2(190, 38));
            areaLabel = Label(location.rectTransform, "", 18, Copper);
            var inventory = Plate("Inventory", layout, Panel, Vector2.zero, new Vector2(116, 76), new Vector2(204, 132));
            Label(Place("Inventory heading", inventory.rectTransform, new Vector2(.5f, .5f),
                new Vector2(0, 48), new Vector2(180, 22)), "物品栏", 16, Copper);
            var coins = Plate("Coin inventory", inventory.rectTransform, Slot, new Vector2(.5f, .5f),
                new Vector2(-46, -9), new Vector2(84, 84));
            coinCount = SlotLabel(coins.rectTransform, coinIcon);
            var batteries = Plate("Battery inventory", inventory.rectTransform, Slot, new Vector2(.5f, .5f),
                new Vector2(46, -9), new Vector2(84, 84));
            batterySlot = ButtonFor(batteries, SelectBatteries);
            batterySelection = batteries.gameObject.AddComponent<Outline>();
            batterySelection.effectColor = Copper;
            batterySelection.effectDistance = new Vector2(2, -2);
            batterySelection.useGraphicAlpha = false;
            batteryCount = SlotLabel(batteries.rectTransform, batteryIcon);

            var speech = Plate("Speech", layout, Panel, new Vector2(.5f, 0), new Vector2(0, 49), new Vector2(535, 47));
            speechPanel = speech.gameObject;
            speechText = Label(speech.rectTransform, "", 21, Paper);
            speechPanel.SetActive(false);

            fade = Plate("Room transition", layout, Color.clear, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
            Stretch(fade.rectTransform);

            var cover = Plate("Tower arrival", layout, new Color(.05f, .06f, .05f, .83f), new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
            Stretch(cover.rectTransform);
            cover.raycastTarget = true;
            winPanel = cover.gameObject;
            var card = Plate("Chapter complete", cover.rectTransform, Panel, new Vector2(.5f, .5f), Vector2.zero, new Vector2(525, 252));
            Label(Place("Heading", card.rectTransform, new Vector2(.5f, .5f), new Vector2(0, 71), new Vector2(460, 52)), "第五关 · 进入塔内", 34, Copper);
            Label(Place("Ending", card.rectTransform, new Vector2(.5f, .5f), new Vector2(0, 14), new Vector2(465, 46)), "塔门在身后合上。楼上传来一声沉闷的齿轮转动。", 20, Paper);
            var replay = Plate("Replay chapter", card.rectTransform, Slot, new Vector2(.5f, .5f), new Vector2(-164, -68), new Vector2(144, 44));
            var next = Plate("Continue elevator", card.rectTransform, Slot, new Vector2(.5f, .5f), new Vector2(0, -68), new Vector2(154, 44));
            var title = Plate("Return to title", card.rectTransform, Slot, new Vector2(.5f, .5f), new Vector2(164, -68), new Vector2(144, 44));
            Label(replay.rectTransform, "重玩本关", 19, Paper);
            Label(title.rectTransform, "返回标题", 19, Paper);
            Label(next.rectTransform, "继续 · 电梯", 19, Paper);
            ButtonFor(next, ContinueToElevator);
            ButtonFor(replay, () => SceneManager.LoadScene(gameObject.scene.name));
            ButtonFor(title, () => SceneManager.LoadScene("RobotEscapeDemo"));
            winPanel.SetActive(false);
        }

        private Text SlotLabel(RectTransform slot, Sprite sprite)
        {
            if (sprite == null) return Label(slot, "", 18, Paper);
            var icon = Plate("Item", slot, Color.white, new Vector2(.5f, .5f), new Vector2(0, 11), new Vector2(44, 44));
            icon.sprite = sprite;
            icon.preserveAspect = true;
            return Label(Place("Count", slot, new Vector2(.5f, .5f), new Vector2(0, -26), new Vector2(82, 24)), "", 17, Paper);
        }

        private Text Label(RectTransform parent, string value, int size, Color color)
        {
            var rect = Place("Text", parent, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
            Stretch(rect);
            var label = rect.gameObject.AddComponent<Text>();
            label.font = font;
            label.text = value;
            label.fontSize = size;
            label.color = color;
            label.alignment = TextAnchor.MiddleCenter;
            label.raycastTarget = false;
            label.supportRichText = false;
            return label;
        }

        private static Button ButtonFor(Image image, UnityEngine.Events.UnityAction onClick)
        {
            image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);
            return button;
        }

        private static Image Plate(string name, RectTransform parent, Color color, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var image = Place(name, parent, anchor, position, size).gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static RectTransform Place(string name, RectTransform parent, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = anchor;
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

        private static void SetVisible(GameObject art, bool visible)
        {
            if (art != null) art.SetActive(visible);
        }
    }
}

