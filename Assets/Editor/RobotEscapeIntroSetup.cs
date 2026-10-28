using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;

namespace RobotEscape.Editor
{
    public static class RobotEscapeIntroSetup
    {
        public static void ApplyToRoom(RobotEscapeRoom room)
        {
            Transform canvas = GameObject.Find("Canvas").transform;
            if (canvas.Find("MenuPanel") == null)
            {
                Transform menu = Overlay(canvas, "MenuPanel", new Color(0.025f, 0.06f, 0.075f, 0.96f));
                Text title = Label(menu, "GameTitle", "废厂重启", new Vector2(0, 175), new Vector2(1000, 130), 84);
                title.color = new Color(0.91f, 0.77f, 0.47f);
                Label(menu, "Subtitle", "停摆的工厂里，一段旅程正要开始。", new Vector2(0, 80), new Vector2(1100, 60), 28);
                Button start = Button(menu, "Start", "开始游戏", new Vector2(0, -50), new Vector2(330, 70), new Color(0.44f, 0.32f, 0.17f));
                UnityEventTools.AddPersistentListener(start.onClick, room.ShowStory);
                Button quit = Button(menu, "Quit", "退出游戏", new Vector2(0, -150), new Vector2(330, 62), new Color(0.18f, 0.26f, 0.28f));
                UnityEventTools.AddPersistentListener(quit.onClick, room.QuitGame);
                Label(menu, "Genre", "机器人 · 探索 · 解谜", new Vector2(0, -310), new Vector2(800, 40), 21).color = new Color(0.6f, 0.66f, 0.63f);
            }
            if (canvas.Find("StoryPanel") == null)
            {
                Transform story = Overlay(canvas, "StoryPanel", new Color(0.02f, 0.045f, 0.055f, 0.98f));
                Label(story, "Chapter", "序章 · 苏醒", new Vector2(0, 225), new Vector2(1100, 70), 34).color = new Color(0.85f, 0.72f, 0.47f);
                Label(story, "Story", "", new Vector2(0, 35), new Vector2(1250, 190), 38);
                Label(story, "Page", "01 / 03", new Vector2(0, -150), new Vector2(200, 40), 20).color = new Color(0.55f, 0.62f, 0.61f);
                Button next = Button(story, "Next", "继续", new Vector2(0, -250), new Vector2(330, 68), new Color(0.44f, 0.32f, 0.17f));
                UnityEventTools.AddPersistentListener(next.onClick, room.NextStoryPage);
                Button skip = Button(story, "Skip", "跳过开场", new Vector2(530, -320), new Vector2(210, 50), new Color(0.12f, 0.19f, 0.2f));
                UnityEventTools.AddPersistentListener(skip.onClick, room.BeginAdventure);
            }
            if (canvas.Find("BackToTitle") == null)
            {
                Button back = Button(canvas, "BackToTitle", "返回标题", Vector2.zero, new Vector2(160, 48), new Color(0.21f, 0.29f, 0.3f));
                RectTransform rect = back.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = Vector2.one;
                rect.anchoredPosition = new Vector2(-295, -40);
                UnityEventTools.AddPersistentListener(back.onClick, room.ReturnToTitle);
            }
            room.menuPanel = canvas.Find("MenuPanel").gameObject;
            room.storyPanel = canvas.Find("StoryPanel").gameObject;
            room.storyText = room.storyPanel.transform.Find("Story").GetComponent<Text>();
            room.storyNextText = room.storyPanel.transform.Find("Next/Text").GetComponent<Text>();
            room.storyPageText = room.storyPanel.transform.Find("Page").GetComponent<Text>();
            canvas.Find("BackToTitle").SetSiblingIndex(canvas.Find("MenuPanel").GetSiblingIndex());
            room.menuPanel.transform.SetAsLastSibling();
            room.storyPanel.transform.SetAsLastSibling();
            room.menuPanel.SetActive(true);
            room.storyPanel.SetActive(false);
        }

        private static Transform Overlay(Transform canvas, string name, Color color)
        {
            GameObject root = new GameObject(name, typeof(RectTransform), typeof(Image));
            root.transform.SetParent(canvas, false);
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            root.GetComponent<Image>().color = color;
            return root.transform;
        }

        private static Text Label(Transform parent, string name, string text, Vector2 position, Vector2 size, int fontSize)
        {
            Text label = new GameObject(name, typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            Position(label.rectTransform, parent, position, size);
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.text = text; label.fontSize = fontSize;
            label.color = new Color(0.88f, 0.9f, 0.83f);
            label.alignment = TextAnchor.MiddleCenter;
            label.raycastTarget = false;
            return label;
        }

        private static Button Button(Transform parent, string name, string text, Vector2 position, Vector2 size, Color color)
        {
            GameObject root = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            Position(root.GetComponent<RectTransform>(), parent, position, size);
            root.GetComponent<Image>().color = color;
            Button button = root.GetComponent<Button>();
            button.targetGraphic = root.GetComponent<Image>();
            Label(root.transform, "Text", text, Vector2.zero, size - new Vector2(12, 6), 27);
            return button;
        }

        private static void Position(RectTransform rect, Transform parent, Vector2 position, Vector2 size)
        {
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position; rect.sizeDelta = size;
        }
    }
}
