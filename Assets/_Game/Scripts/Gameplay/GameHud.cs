using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace ProtocoloZero.Gameplay
{
    public sealed class GameHud : MonoBehaviour
    {
        public Button StartButton { get; private set; }
        public Button RestartButton { get; private set; }
        public Text HealthLabel { get; private set; }
        public Text CardsLabel { get; private set; }
        public Text ObjectiveLabel { get; private set; }
        public Text OutcomeLabel { get; private set; }
        public GameObject Overlay { get; private set; }
        public Image DamageFlash { get; private set; }

        private GameFlow flow;
        private Font font;
        private GameObject hud;
        private GameObject canvasRoot;
        private int lastHealth;

        public void Initialize(GameFlow owner)
        {
            flow = owner;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            canvasRoot = new GameObject("PA4 Canvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasRoot.transform.SetParent(transform, false);
            canvasRoot.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasRoot.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                var events = new GameObject("PA4 EventSystem", typeof(EventSystem),
                    typeof(InputSystemUIInputModule));
                events.transform.SetParent(transform, false);
            }
            hud = Panel("HUD", canvasRoot.transform, Color.clear);
            hud.GetComponent<Image>().raycastTarget = false;
            HealthLabel = Label(hud.transform, "", new Vector2(0, 270));
            CardsLabel = Label(hud.transform, "", new Vector2(0, 230));
            ObjectiveLabel = Label(hud.transform, "", new Vector2(0, 190));
            DamageFlash = Panel("Damage flash", hud.transform, new Color(1f, 0f, 0f, 0f)).GetComponent<Image>();
            DamageFlash.raycastTarget = false;
            lastHealth = flow.Health.CurrentHealth;
            Overlay = Panel("Menu and outcome", canvasRoot.transform, new Color(0.02f, 0.04f, 0.06f, 0.94f));
            OutcomeLabel = Label(Overlay.transform, "", new Vector2(0, 100));
            OutcomeLabel.fontSize = 34;
            StartButton = MakeButton("Jugar", new Vector2(0, -30));
            StartButton.onClick.AddListener(flow.BeginGame);
            RestartButton = MakeButton("Reiniciar", new Vector2(0, -30));
            RestartButton.onClick.AddListener(Restart);
            flow.StateChanged += OnStateChanged;
            flow.Health.HealthChanged += OnHealthChanged;
            flow.Progress.CardCollected += OnCardCollected;
            flow.Progress.PanelActivated += Refresh;
            OnStateChanged(flow.State);
        }

        private void Restart() { flow.Restart(); }
        private void OnHealthChanged(int current, int maximum)
        {
            if (current < lastHealth && current > 0 && flow.State == GameFlow.GameState.Playing)
                DamageFlash.color = new Color(1f, 0f, 0f, 0.4f);
            lastHealth = current;
            Refresh();
        }

        private void Update()
        {
            if (DamageFlash == null || DamageFlash.color.a <= 0f) return;
            Color color = DamageFlash.color;
            color.a = Mathf.Max(0f, color.a - Time.unscaledDeltaTime * (0.4f / 0.3f));
            DamageFlash.color = color;
        }
        private void OnCardCollected(int count) { Refresh(); }

        private void OnStateChanged(GameFlow.GameState state)
        {
            bool playing = state == GameFlow.GameState.Playing;
            hud.SetActive(playing);
            Overlay.SetActive(!playing);
            StartButton.gameObject.SetActive(state == GameFlow.GameState.Menu);
            RestartButton.gameObject.SetActive(state == GameFlow.GameState.Victory || state == GameFlow.GameState.Defeat);
            OutcomeLabel.text = state == GameFlow.GameState.Menu ? "Protocolo Zero — Zona de cuarentena"
                : state == GameFlow.GameState.Victory ? "Victoria — Salida alcanzada" : "Derrota — Sin salud";
            Refresh();
            if (!playing && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(state == GameFlow.GameState.Menu
                    ? StartButton.gameObject : RestartButton.gameObject);
        }

        private void Refresh()
        {
            HealthLabel.text = $"Salud {flow.Health.CurrentHealth}/{flow.Health.MaxHealth}";
            CardsLabel.text = $"Tarjetas {flow.Progress.CollectedCount}/{flow.Progress.RequiredCards}";
            ObjectiveLabel.text = flow.Progress.IsPanelActivated ? "Objetivo: alcanza la salida"
                : flow.Progress.CollectedCount == flow.Progress.RequiredCards ? "Objetivo: activa el panel"
                : "Objetivo: recoge las tres tarjetas (E)";
        }

        private static GameObject Panel(string name, Transform parent, Color color)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            panel.GetComponent<Image>().color = color;
            return panel;
        }

        private Text Label(Transform parent, string value, Vector2 position)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(1150, 50);
            rect.anchoredPosition = position;
            var text = go.GetComponent<Text>();
            text.font = font;
            text.fontSize = 24;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            text.text = value;
            return text;
        }

        private Button MakeButton(string caption, Vector2 position)
        {
            var go = new GameObject(caption, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(Overlay.transform, false);
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(280, 65);
            rect.anchoredPosition = position;
            go.GetComponent<Image>().color = new Color(0.12f, 0.3f, 0.4f);
            Text label = Label(go.transform, caption, Vector2.zero);
            label.rectTransform.sizeDelta = rect.sizeDelta;
            return go.GetComponent<Button>();
        }

        private void OnDestroy()
        {
            if (flow == null) return;
            flow.StateChanged -= OnStateChanged;
            flow.Health.HealthChanged -= OnHealthChanged;
            flow.Progress.CardCollected -= OnCardCollected;
            flow.Progress.PanelActivated -= Refresh;
        }
    }
}
