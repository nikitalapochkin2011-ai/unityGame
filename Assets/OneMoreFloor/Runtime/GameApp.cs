using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace OneMoreFloor
{
    public sealed class GameApp : MonoBehaviour
    {
        public GameSettings Settings;
        GameSession game;
        RectTransform safe, board, cabin;
        RectTransform overlay;
        Text score, occupancy, losses, hint, feedback, record, soundLabel;
        Text[] floorLabels;
        Image[][] patience;
        Text[][] destinations;
        Text[] riders;
        int best;
        bool sound;
        float feedbackTime;
        Rect lastSafe;
        Vector2Int lastScreen;
        AudioSource audioSource;
        AudioClip tone;
        Font font;
        readonly Color ink = new Color(.12f, .18f, .28f);
        readonly Color blue = new Color(.23f, .52f, .94f);
        readonly Color mint = new Color(.25f, .8f, .65f);
        void Start()
        {
            Application.targetFrameRate = 60;
            Screen.orientation = ScreenOrientation.Portrait;
            if (Settings == null) Settings = ScriptableObject.CreateInstance<GameSettings>();
            Settings.Rules.Validate();
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            best = PlayerPrefs.GetInt("OneMoreFloor.Best", 0);
            sound = PlayerPrefs.GetInt("OneMoreFloor.Sound", 1) != 0;
            audioSource = gameObject.AddComponent<AudioSource>();
            float[] samples = new float[4410];
            for (int i = 0; i < samples.Length; i++) samples[i] = Mathf.Sin(2 * Mathf.PI * 660 * i / 44100) * .12f * (1f - (float)i / samples.Length);
            tone = AudioClip.Create("Feedback", samples.Length, 1, 44100, false); tone.SetData(samples, 0);
            BuildUI(); ShowMenu();
        }
        RectTransform Box(string name, Transform parent, Vector2 min, Vector2 max, Color? color = null)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>(); rect.anchorMin = min; rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            if (color.HasValue) { var image = go.AddComponent<Image>(); image.color = color.Value; image.raycastTarget = false; }
            return rect;
        }
        Text Label(string name, Transform parent, string value, Vector2 min, Vector2 max, int size = 26, Color? color = null)
        {
            var rect = Box(name, parent, min, max); var text = rect.gameObject.AddComponent<Text>();
            text.font = font; text.text = value; text.fontSize = size; text.color = color ?? ink;
            text.alignment = TextAnchor.MiddleCenter; text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 10; text.resizeTextMaxSize = size; text.raycastTarget = false;
            return text;
        }
        Button Button(string name, Transform parent, string value, Vector2 min, Vector2 max, Action action, Color? color = null)
        {
            var rect = Box(name, parent, min, max, color ?? blue);
            rect.GetComponent<Image>().raycastTarget = true;
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = rect.GetComponent<Image>();
            button.onClick.AddListener(() => action());
            Label("Label", rect, value, new Vector2(.04f,.04f), new Vector2(.96f,.96f), 30, Color.white);
            return button;
        }
        void BuildUI()
        {
            var root = new GameObject("Game UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(transform, false); var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(540, 960); scaler.matchWidthOrHeight = .5f;
            Box("Background", root.transform, Vector2.zero, Vector2.one, new Color(.94f,.96f,.99f));
            safe = Box("Safe Area", root.transform, Vector2.zero, Vector2.one);
            Label("Title", safe, "ЕЩЁ ОДИН ЭТАЖ", new Vector2(.04f,.93f), new Vector2(.78f,.99f), 30);
            Button("Pause", safe, "Ⅱ", new Vector2(.82f,.935f), new Vector2(.96f,.99f), Pause);
            score = Label("Score", safe, "", new Vector2(.02f,.875f), new Vector2(.5f,.93f), 26);
            losses = Label("Losses", safe, "", new Vector2(.5f,.875f), new Vector2(.98f,.93f), 24);
            occupancy = Label("Occupancy", safe, "", new Vector2(.02f,.82f), new Vector2(.5f,.875f), 23);
            hint = Label("Route", safe, "", new Vector2(.5f,.82f), new Vector2(.98f,.875f), 22);
            board = Box("Building", safe, new Vector2(.04f,.14f), new Vector2(.96f,.81f), new Color(.86f,.9f,.95f));
            int floors = Settings.Rules.Floors;
            floorLabels = new Text[floors]; patience = new Image[floors][]; destinations = new Text[floors][];
            for (int f = 0; f < floors; f++)
            {
                int selected = f; float bottom = (float)f / floors, top = (float)(f + 1) / floors;
                var row = Box("Floor " + (f+1), board, new Vector2(0,bottom), new Vector2(1,top));
                Box("Line", row, Vector2.zero, new Vector2(1,.02f), Color.white);
                var button = Button("Select floor", row, (f+1).ToString(), new Vector2(.01f,.13f), new Vector2(.14f,.88f), () => { if (game != null) game.SelectFloor(selected); });
                floorLabels[f] = button.GetComponentInChildren<Text>();
                int slots = Mathf.Max(10, Mathf.CeilToInt(Settings.Rules.PatienceSeconds / Settings.Rules.MinimumSpawnSeconds) + 2);
                int queueRows = Mathf.CeilToInt(slots / 5f);
                destinations[f] = new Text[slots]; patience[f] = new Image[slots];
                // Two rows preserve an individual destination and patience bar for every visible passenger.
                for (int p = 0; p < slots; p++)
                {
                    float x = .18f + p % 5 * .092f, y = 1f - (p / 5 + 1f) / queueRows;
                    var person = Box("Passenger " + p, row, new Vector2(x,y), new Vector2(x+.077f,y+.9f/queueRows), mint);
                    destinations[f][p] = Label("Destination", person, "", new Vector2(0,.22f), Vector2.one, 22);
                    var bar = Box("Patience", person, new Vector2(0,0), new Vector2(1,.14f), mint);
                    patience[f][p] = bar.GetComponent<Image>(); person.gameObject.SetActive(false);
                }
            }
            Box("Shaft", board, new Vector2(.69f,0), new Vector2(.99f,1), new Color(.74f,.8f,.89f));
            cabin = Box("Elevator", board, new Vector2(.7f,0), new Vector2(.98f,1f/floors), ink);
            riders = new Text[Settings.Rules.Capacity];
            for (int i=0;i<riders.Length;i++)
            {
                int columns = 2, rows = Mathf.CeilToInt(riders.Length / 2f);
                float x = (i % columns) * .5f, y = 1f - (i / columns + 1f) / rows;
                riders[i] = Label("Rider", cabin, "", new Vector2(x,y), new Vector2(x+.5f,y+1f/rows), 26, Color.white);
            }
            feedback = Label("Feedback", safe, "", new Vector2(.03f,.075f), new Vector2(.97f,.135f), 26);
            Label("Help", safe, "Нажмите этаж • Число на пассажире — его цель", new Vector2(.02f,.01f), new Vector2(.98f,.07f), 20);
            overlay = Box("Overlay", safe, Vector2.zero, Vector2.one, new Color(.94f,.96f,.99f,.98f));
            overlay.GetComponent<Image>().raycastTarget = true;
            ApplySafeArea();
        }
        void ClearOverlay()
        {
            // Disable immediately: Destroy completes at the end of the frame.
            foreach (Transform child in overlay) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            overlay.gameObject.SetActive(true);
        }
        void ShowMenu()
        {
            game = null; ClearOverlay();
            Label("Title", overlay, "ЕЩЁ ОДИН\nЭТАЖ", new Vector2(.08f,.66f), new Vector2(.92f,.89f), 58);
            record = Label("Record", overlay, "Рекорд: " + best, new Vector2(.1f,.56f), new Vector2(.9f,.64f), 30);
            Label("Rules", overlay, "6 этажей. 4 места.\nДоставляйте пассажиров, пока трое не ушли.\nКаждые 5 доставок подряд повышают множитель.", new Vector2(.08f,.33f), new Vector2(.92f,.54f), 25);
            Button("Play", overlay, "Играть", new Vector2(.18f,.21f), new Vector2(.82f,.31f), NewGame);
            var b = Button("Sound", overlay, "", new Vector2(.18f,.09f), new Vector2(.82f,.18f), ToggleSound);
            soundLabel = b.GetComponentInChildren<Text>(); UpdateSoundLabel();
        }
        void ToggleSound() { sound = !sound; PlayerPrefs.SetInt("OneMoreFloor.Sound", sound ? 1 : 0); PlayerPrefs.Save(); UpdateSoundLabel(); }
        void UpdateSoundLabel() { if (soundLabel != null) soundLabel.text = sound ? "Звук: вкл." : "Звук: выкл."; }
        void NewGame()
        {
            game = new GameSession(Settings.Rules, Environment.TickCount);
            game.Feedback += message => { feedback.text = message; feedbackTime = 1.8f; if (sound) audioSource.PlayOneShot(tone); };
            feedback.text = "Выберите этаж"; feedbackTime = 3; overlay.gameObject.SetActive(false); Render();
        }
        void Pause()
        {
            if (game == null || game.State != PlayState.Playing) return;
            game.Pause(); ClearOverlay();
            Label("Paused", overlay, "ПАУЗА", new Vector2(.1f,.65f), new Vector2(.9f,.8f), 48);
            Button("Resume", overlay, "Продолжить", new Vector2(.18f,.45f), new Vector2(.82f,.56f), () => { game.Resume(); overlay.gameObject.SetActive(false); });
            Button("Restart", overlay, "Заново", new Vector2(.18f,.3f), new Vector2(.82f,.41f), NewGame);
            Button("Menu", overlay, "В меню", new Vector2(.18f,.15f), new Vector2(.82f,.26f), ShowMenu);
        }
        void EndGame()
        {
            best = Math.Max(best, game.Score); PlayerPrefs.SetInt("OneMoreFloor.Best", best); PlayerPrefs.Save(); ClearOverlay();
            Label("Game Over", overlay, "СМЕНА ОКОНЧЕНА", new Vector2(.06f,.72f), new Vector2(.94f,.84f), 40);
            Label("Results", overlay, "Очки: " + game.Score + "\nДоставлено: " + game.Delivered + "\nРекорд: " + best, new Vector2(.08f,.47f), new Vector2(.92f,.69f), 32);
            Button("Retry", overlay, "Ещё раз", new Vector2(.18f,.3f), new Vector2(.82f,.41f), NewGame);
            Button("Menu", overlay, "В меню", new Vector2(.18f,.15f), new Vector2(.82f,.26f), ShowMenu);
        }
        void Update()
        {
            ApplySafeArea();
            if (game == null) return;
            var before = game.State; game.Tick(Time.unscaledDeltaTime);
            if (before == PlayState.Playing)
            {
                feedbackTime -= Time.unscaledDeltaTime; if (feedbackTime <= 0) feedback.text = "";
                Render();
            }
            if (before != PlayState.GameOver && game.State == PlayState.GameOver) EndGame();
        }
        void Render()
        {
            score.text = "Очки: " + game.Score + "  ×" + game.Multiplier;
            losses.text = "Ушли: " + game.Lost + "/" + game.Rules.LossLimit;
            occupancy.text = "В лифте: " + game.Lift.Riders.Count + "/" + game.Rules.Capacity;
            hint.text = game.Lift.Moving ? "Едем → " + (game.Lift.TargetFloor+1) : "Этаж " + (game.Lift.CurrentFloor+1);
            if (game.Lift.NextFloor.HasValue) hint.text += " • далее " + (game.Lift.NextFloor.Value+1);
            float y = game.Lift.Position / game.Rules.Floors;
            cabin.anchorMin = new Vector2(.7f,y); cabin.anchorMax = new Vector2(.98f,y+1f/game.Rules.Floors);
            for (int i=0;i<riders.Length;i++) riders[i].text = i < game.Lift.Riders.Count ? "● " + (game.Lift.Riders[i].Destination+1) : "·";
            for (int f=0;f<game.Rules.Floors;f++)
            {
                floorLabels[f].color = game.Lift.NextFloor == f ? Color.yellow : Color.white;
                var queue = game.Queues.Floors[f];
                for (int p=0;p<destinations[f].Length;p++)
                {
                    var text = destinations[f][p]; text.transform.parent.gameObject.SetActive(p < queue.Count);
                    if (p >= queue.Count) continue;
                    text.text = (queue[p].Destination+1).ToString();
                    float ratio = Mathf.Clamp01(queue[p].Remaining / game.Rules.PatienceSeconds);
                    patience[f][p].rectTransform.anchorMax = new Vector2(ratio,.14f);
                    patience[f][p].color = Color.Lerp(new Color(1,.25f,.25f), new Color(.05f,.55f,.35f), ratio);
                }
            }
        }
        void ApplySafeArea()
        {
            if (safe == null || Screen.width == 0 || Screen.height == 0) return;
            Rect area = Screen.safeArea;
            if (area == lastSafe && lastScreen == new Vector2Int(Screen.width,Screen.height)) return;
            lastSafe = area; lastScreen = new Vector2Int(Screen.width,Screen.height);
            safe.anchorMin = new Vector2(area.xMin/Screen.width,area.yMin/Screen.height);
            safe.anchorMax = new Vector2(area.xMax/Screen.width,area.yMax/Screen.height);
        }
        void OnApplicationPause(bool paused) { if (paused) Pause(); }
        void OnApplicationFocus(bool focused) { if (!focused) Pause(); }
        void OnDestroy() { if (tone != null) Destroy(tone); }
    }
}
