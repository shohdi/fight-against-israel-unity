using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;

namespace FightAgainstIsrael
{
    public sealed class CombatHud : MonoBehaviour
    {
        private FighterGame game;
        private Canvas canvas;
        private Font regular, bold;
        private GameObject menu, playing, pause, debrief;
        private Text scoreText, bestText, timeText, phaseText, musicText, effectsText;
        private Text debriefScore, debriefBest, debriefTime;
        private int lastScore = -1, lastBest = -1, lastSecond = -1;
        private int screenWidth, screenHeight;
        private float width, height;
        private bool compact;
        private readonly Color ink = new Color(0.026f, 0.064f, 0.078f, 0.94f);
        private readonly Color white = new Color(0.94f, 0.97f, 0.94f);
        private readonly Color muted = new Color(0.57f, 0.71f, 0.72f);
        private readonly Color amber = new Color(1, 0.72f, 0.3f);
        private readonly Color teal = new Color(0.3f, 0.82f, 0.79f);

        public void Initialize(FighterGame controller)
        {
            game = controller;
            regular = Resources.Load<Font>("Fonts/Barlow-Regular");
            bold = Resources.Load<Font>("Fonts/Barlow-SemiBold");
            if (regular == null) regular = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (bold == null) bold = regular;
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var eventSystem = new GameObject("Flight input events");
                eventSystem.AddComponent<EventSystem>();
                eventSystem.AddComponent<StandaloneInputModule>();
            }
            BuildLayout();
        }

        private void BuildLayout()
        {
            if (canvas != null) Destroy(canvas.gameObject);
            compact = Screen.width < Screen.height;
            screenWidth = Screen.width;
            screenHeight = Screen.height;
            width = compact ? 720 : 1280;
            height = compact ? width * Screen.height / Mathf.Max(1f, Screen.width) : 800;
            var canvasObject = new GameObject("Responsive flight HUD");
            canvasObject.transform.SetParent(transform, false);
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(width, compact ? height : 800);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = compact ? 0 : 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();

            Image(canvas.transform, "Header", 0, 0, width, 88, new Color(0.025f, 0.06f, 0.073f, 0.94f));
            Image(canvas.transform, "Brand mark", 30, 27, 4, 31, amber);
            Label(canvas.transform, "FIGHT AGAINST ISRAEL", 48, 21, compact ? 375 : 540, 26, compact ? 23 : 24, white, true);
            Label(canvas.transform, "TOP-DOWN 3D AIR COMBAT", 48, 49, 400, 18, 12, muted);
            Image(canvas.transform, "Header rule", 30, 87, width - 60, 1, new Color(0.32f, 0.55f, 0.55f, 0.4f));
            float audioX = compact ? width - 270 : width - 368;
            musicText = Button(canvas.transform, "MUSIC ON", audioX, 28, compact ? 115 : 128, 34, game.ToggleMusic, false, 13).GetComponentInChildren<Text>();
            effectsText = Button(canvas.transform, "FX ON", audioX + (compact ? 126 : 144), 28, compact ? 102 : 110, 34, game.ToggleEffects, false, 13).GetComponentInChildren<Text>();

            playing = Group("Playing interface");
            Image(playing.transform, "Score plate", 28, 108, compact ? 282 : 292, 76, ink);
            Label(playing.transform, "SCORE", 44, 116, 80, 19, 12, muted);
            scoreText = Label(playing.transform, "000000", 43, 135, 160, 40, 34, white, true);
            Label(playing.transform, "LIVES", compact ? 220 : 226, 116, 72, 19, 12, muted);
            Label(playing.transform, "01", compact ? 220 : 226, 138, 65, 36, 28, amber, true);
            float timerX = width - (compact ? 302 : 292);
            Image(playing.transform, "Flight plate", timerX, 108, compact ? 274 : 264, 76, ink);
            Label(playing.transform, "FLIGHT TIME", timerX + 16, 116, 170, 18, 12, muted);
            timeText = Label(playing.transform, "00:00", timerX + 15, 136, 156, 36, 29, white, true);
            Button(playing.transform, "II", width - 88, 127, 40, 39, game.TogglePause, false, 20);
            Image(playing.transform, "Flight footer", 28, height - 80, compact ? 350 : 440, 52, ink);
            Label(playing.transform, "MIG-29  /  EGYPT + PALESTINE", 43, height - 74, compact ? 320 : 400, 18, 13, teal, true);
            phaseText = Label(playing.transform, "PATROL 01  //  STAY SHARP", 43, height - 52, 380, 18, 12, muted);
            var fire = Button(playing.transform, "FIRE", width - 154, height - 108, 124, 78, () => { }, true, 24);
            var hold = fire.gameObject.AddComponent<HoldFireButton>();
            hold.Game = game;

            menu = Group("Mission menu");
            float panelX = compact ? 34 : 48;
            float panelY = compact ? 222 : 148;
            float panelWidth = compact ? width - 68 : 494;
            float innerX = panelX + 34;
            float innerWidth = panelWidth - 68;
            Image(menu.transform, "Mission briefing", panelX, panelY, panelWidth, compact ? 665 : 542, ink);
            Image(menu.transform, "Mission accent", panelX, panelY, 4, compact ? 665 : 542, amber);
            Label(menu.transform, "MISSION 01  /  COASTAL PATROL", innerX, panelY + 30, innerWidth, 25, compact ? 18 : 14, teal, true);
            Label(menu.transform, "FIGHT AGAINST", innerX - 2, panelY + 66, innerWidth + 10, 62, compact ? 47 : 41, white, true);
            Label(menu.transform, "ISRAEL", innerX - 5, panelY + 116, innerWidth, 96, compact ? 86 : 78, white, true);
            Image(menu.transform, "Title rule", innerX, panelY + 221, 48, 3, amber);
            Label(menu.transform, "ONE AIRCRAFT. ONE LIFE.", innerX, panelY + 246, innerWidth, 27, compact ? 22 : 18, amber, true);
            Label(menu.transform, "Fly a MiG-29-inspired fighter above a moving coastline.\nDodge incoming fire and take down F-35-inspired aircraft.",
                innerX, panelY + 288, innerWidth, compact ? 92 : 72, compact ? 23 : 18, muted);
            float buttonY = panelY + (compact ? 414 : 391);
            Button(menu.transform, "BEGIN FLIGHT  →", innerX, buttonY, innerWidth, compact ? 66 : 58, game.StartFlight, true, compact ? 26 : 23);
            Label(menu.transform, "KEYBOARD", innerX, buttonY + 79, innerWidth, 20, compact ? 16 : 12, teal, true);
            Label(menu.transform, "WASD / Arrows to move  ·  Space to fire", innerX, buttonY + 104, innerWidth, 27, compact ? 22 : 17, white);
            if (compact)
                Label(menu.transform, "TOUCH  ·  Drag the plane. Tap elsewhere or hold FIRE.", innerX, buttonY + 144, innerWidth, 48, 21, muted);
            else
            {
                Image(menu.transform, "Aircraft intelligence", 890, 575, 340, 104, ink);
                Label(menu.transform, "PLAYER  //  MIG-29", 910, 588, 300, 22, 15, teal, true);
                Label(menu.transform, "ENEMY  //  F-35", 910, 617, 300, 22, 15, amber, true);
                Label(menu.transform, "3D AIRCRAFT  ·  ORIGINAL SOUNDTRACK", 910, 649, 300, 18, 11, muted);
            }
            bestText = Label(menu.transform, "BEST SCORE  000000", compact ? 38 : 48, height - 57, width - 90, 24, compact ? 18 : 15, teal, true);
            Label(menu.transform, "Touch: drag to move · tap or hold FIRE to shoot    /    P: pause    M: music", compact ? 38 : 48, height - 30, width - 90, 20, compact ? 12 : 12, muted);

            pause = Group("Pause overlay");
            BuildModal(pause.transform, "FLIGHT PAUSED", "Take a breath. Your flight will resume from here.", out float modalX, out float modalY, out float modalWidth);
            Button(pause.transform, "RESUME FLIGHT  →", modalX + 36, modalY + 235, modalWidth - 72, 60, game.TogglePause, true, 24);
            Button(pause.transform, "RETURN TO MENU", modalX + 36, modalY + 318, modalWidth - 72, 42, game.ShowMenu, false, 17);

            debrief = Group("Flight debrief");
            BuildModal(debrief.transform, "AIRCRAFT LOST", "One life. Make your next flight count.", out modalX, out modalY, out modalWidth);
            float third = (modalWidth - 72) / 3;
            Label(debrief.transform, "SCORE", modalX + 36, modalY + 168, third, 18, 12, muted);
            Label(debrief.transform, "BEST", modalX + 36 + third, modalY + 168, third, 18, 12, muted);
            Label(debrief.transform, "FLIGHT TIME", modalX + 36 + third * 2, modalY + 168, third, 18, 12, muted);
            debriefScore = Label(debrief.transform, "0", modalX + 36, modalY + 190, third, 36, 30, white, true);
            debriefBest = Label(debrief.transform, "0", modalX + 36 + third, modalY + 190, third, 36, 30, amber, true);
            debriefTime = Label(debrief.transform, "00:00", modalX + 36 + third * 2, modalY + 190, third, 36, 30, white, true);
            Button(debrief.transform, "FLY AGAIN  →", modalX + 36, modalY + 252, modalWidth - 72, 60, game.StartFlight, true, 24);
            Button(debrief.transform, "RETURN TO MENU", modalX + 36, modalY + 332, modalWidth - 72, 42, game.ShowMenu, false, 17);
            RefreshAudioButtons();
            lastScore = lastBest = lastSecond = -1;
        }

        private void BuildModal(Transform parent, string title, string subtitle, out float x, out float y, out float panelWidth)
        {
            Image(parent, "Dim flight", 0, 88, width, height - 88, new Color(0.01f, 0.024f, 0.03f, 0.5f), true);
            panelWidth = compact ? 636 : 536;
            x = (width - panelWidth) / 2;
            y = Mathf.Max(130, (height - 420) / 2);
            Image(parent, "Debrief panel", x, y, panelWidth, 410, ink);
            Image(parent, "Debrief accent", x, y, panelWidth, 4, amber);
            Label(parent, "FLIGHT REPORT", x + 36, y + 31, panelWidth - 72, 22, 13, teal, true);
            Label(parent, title, x + 33, y + 67, panelWidth - 66, 57, compact ? 47 : 41, white, true);
            Label(parent, subtitle, x + 36, y + 129, panelWidth - 72, 27, 19, muted);
        }

        private GameObject Group(string name)
        {
            var group = new GameObject(name, typeof(RectTransform));
            group.transform.SetParent(canvas.transform, false);
            var rect = (RectTransform)group.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return group;
        }
        private RectTransform Place(GameObject item, Transform parent, float x, float y, float w, float h)
        {
            item.transform.SetParent(parent, false);
            var rect = item.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(w, h);
            return rect;
        }
        private Image Image(Transform parent, string name, float x, float y, float w, float h, Color color, bool blocks = false)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            Place(obj, parent, x, y, w, h);
            var image = obj.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = blocks;
            return image;
        }
        private Text Label(Transform parent, string content, float x, float y, float w, float h, int size, Color color, bool strong = false)
        {
            var obj = new GameObject(content, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            Place(obj, parent, x, y, w, h);
            var text = obj.GetComponent<Text>();
            text.font = strong ? bold : regular;
            text.fontSize = size;
            text.text = content;
            text.color = color;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.alignment = TextAnchor.UpperLeft;
            return text;
        }
        private Button Button(Transform parent, string title, float x, float y, float w, float h, UnityAction action, bool primary, int fontSize)
        {
            var image = Image(parent, title, x, y, w, h, primary ? amber : new Color(0.08f, 0.16f, 0.18f, 0.95f), true);
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.88f, 0.95f, 0.94f);
            colors.pressedColor = new Color(0.72f, 0.8f, 0.81f);
            button.colors = colors;
            var label = Label(image.transform, title, 8, 0, w - 16, h, fontSize, primary ? ink : white, true);
            label.alignment = TextAnchor.MiddleCenter;
            button.onClick.AddListener(action);
            return button;
        }

        public void RefreshAudioButtons()
        {
            if (musicText != null) musicText.text = game.MusicEnabled ? "MUSIC ON" : "MUSIC OFF";
            if (effectsText != null) effectsText.text = game.EffectsEnabled ? "FX ON" : "FX OFF";
        }
        private void Display(bool menuVisible, bool playVisible, bool pauseVisible, bool overVisible)
        {
            menu.SetActive(menuVisible);
            playing.SetActive(playVisible);
            pause.SetActive(pauseVisible);
            debrief.SetActive(overVisible);
            RefreshAudioButtons();
        }
        public void ShowMenu() { Display(true, false, false, false); }
        public void ShowPlaying() { Display(false, true, false, false); }
        public void ShowPause() { Display(false, true, true, false); }
        public void ShowGameOver(int score, int best, float time)
        {
            Display(false, false, false, true);
            debriefScore.text = score.ToString("D4");
            debriefBest.text = best.ToString("D4");
            debriefTime.text = FormatTime(time);
        }
        public void Tick(int score, int best, float time)
        {
            if (screenWidth != Screen.width || screenHeight != Screen.height)
            {
                BuildLayout();
                switch (game.State)
                {
                    case FighterGame.FlightState.Menu: game.ArrangeShowcase(); ShowMenu(); break;
                    case FighterGame.FlightState.Playing: ShowPlaying(); break;
                    case FighterGame.FlightState.Paused: ShowPause(); break;
                    case FighterGame.FlightState.GameOver: ShowGameOver(score, best, time); break;
                }
            }
            if (score != lastScore) { scoreText.text = score.ToString("D6"); lastScore = score; }
            if (best != lastBest) { bestText.text = "BEST SCORE  " + best.ToString("D6"); lastBest = best; }
            int second = Mathf.FloorToInt(time);
            if (second != lastSecond)
            {
                timeText.text = FormatTime(time);
                phaseText.text = "PATROL " + (1 + second / 30).ToString("D2") + "  //  " + (second < 30 ? "STAY SHARP" : "HOSTILES INBOUND");
                lastSecond = second;
            }
        }
        private static string FormatTime(float time)
        {
            int seconds = Mathf.FloorToInt(time);
            return (seconds / 60).ToString("D2") + ":" + (seconds % 60).ToString("D2");
        }
    }

    public sealed class HoldFireButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public FighterGame Game;
        public void OnPointerDown(PointerEventData eventData) { Game.SetTouchFiring(true); }
        public void OnPointerUp(PointerEventData eventData) { Game.SetTouchFiring(false); }
        private void OnDisable() { if (Game != null) Game.SetTouchFiring(false); }
    }
}
