using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class MainMenuController : MonoBehaviour
{
    private Canvas menuCanvas;
    private GameObject epiloguePanel;

    private void Start()
    {
        EnsureEventSystem();
        BuildMainMenuUI();
    }

    private void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem", typeof(EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
        }
    }

    private void BuildMainMenuUI()
    {
        menuCanvas = FindFirstObjectByType<Canvas>();
        if (menuCanvas == null)
        {
            GameObject canvasObj = new GameObject("MainMenuCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            menuCanvas = canvasObj.GetComponent<Canvas>();
            menuCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
        }

        // Fondo del Casino-Mazmorra
        GameObject bgObj = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bgObj.transform.SetParent(menuCanvas.transform, false);
        RectTransform bgRt = bgObj.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;
        bgObj.GetComponent<Image>().color = new Color(0.09f, 0.06f, 0.14f, 1f);

        // Título y Lore (GDD)
        CreateText(bgObj.transform, "<b>LUDOGOBLIN</b>", new Vector2(0f, 320f), new Vector2(900f, 90f), 64f, new Color(1f, 0.84f, 0.25f, 1f));
        CreateText(bgObj.transform, "<i>El Casino-Mazmorra del Rey Goblin Brujo</i>", new Vector2(0f, 250f), new Vector2(900f, 45f), 26f, new Color(0.85f, 0.75f, 0.95f, 1f));

        string loreIntro =
            "Kael apostó su propia alma contra el avaricioso Rey Goblin Brujo y perdió.\n" +
            "Despojado de sus armas y arrojado a las profundidades del Casino-Mazmorra,\n" +
            "solo podrá recuperar su libertad dominando la Ruleta Mágica y los Dados de Poder.";
        CreateText(bgObj.transform, loreIntro, new Vector2(0f, 145f), new Vector2(1050f, 120f), 20f, new Color(0.9f, 0.9f, 0.9f, 1f));

        // Botón Nueva Partida
        CreateButton(bgObj.transform, "NUEVA PARTIDA (Nueva Run)", new Vector2(0f, 10f), new Vector2(380f, 64f), new Color(0.22f, 0.65f, 0.35f, 1f), () =>
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StartNewRun();
                GameManager.Instance.LoadSceneSafe(GameManager.BuildSceneName, GameManager.CombatSceneName);
            }
        });

        // Botón Continuar Run (si existe archivo de guardado)
        bool hasSave = GameManager.Instance != null && GameManager.Instance.HasSavedRun();
        Button continueBtn = CreateButton(bgObj.transform,
            hasSave ? "CONTINUAR RUN GUARDADA" : "SIN PARTIDA GUARDADA",
            new Vector2(0f, -75f), new Vector2(380f, 64f),
            hasSave ? new Color(0.25f, 0.48f, 0.82f, 1f) : new Color(0.3f, 0.3f, 0.35f, 1f),
            () =>
            {
                if (GameManager.Instance != null && GameManager.Instance.LoadRun())
                {
                    GameManager.Instance.LoadSceneSafe(GameManager.BuildSceneName, GameManager.CombatSceneName);
                }
            });
        continueBtn.interactable = hasSave;

        // Botón Combate Directo (Atajo de prueba rápida)
        CreateButton(bgObj.transform, "COMBATE RÁPIDO", new Vector2(0f, -160f), new Vector2(380f, 56f), new Color(0.55f, 0.38f, 0.72f, 1f), () =>
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.LoadSceneSafe(GameManager.CombatSceneName, GameManager.CombatSceneName);
            }
        });

        // Botón Salir
        CreateButton(bgObj.transform, "SALIR DEL JUEGO", new Vector2(0f, -240f), new Vector2(380f, 56f), new Color(0.68f, 0.24f, 0.24f, 1f), () =>
        {
            Application.Quit();
        });

        // Mostrar Epílogo del GDD si venimos de terminar una Run (Victoria o Permadeath)
        if (GameManager.Instance != null && GameManager.Instance.ShowEpilogueOnMenu)
        {
            GameManager.Instance.ShowEpilogueOnMenu = false;
            ShowEpilogueModal(GameManager.Instance.LastRunEndedInVictory);
        }
    }

    private void ShowEpilogueModal(bool victory)
    {
        epiloguePanel = new GameObject("EpilogueModal", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        epiloguePanel.transform.SetParent(menuCanvas.transform, false);
        RectTransform rt = epiloguePanel.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        epiloguePanel.GetComponent<Image>().color = new Color(0.04f, 0.03f, 0.07f, 0.96f);

        string title = victory
            ? "<color=#66FF66><b>EPÍLOGO DE VICTORIA — BANCARROTA DEL CASINO</b></color>"
            : "<color=#FF5555><b>EPÍLOGO DE DERROTA — ALMA ENGRANADA</b></color>";

        string body = victory
            ? "Tras derrotar al Rey Goblin Brujo y llevar su Casino-Mazmorra a la bancarrota,\n" +
              "Kael recupera su alma y emerge finalmente a la superficie con su libertad.\n\n" +
              "Sin embargo, mientras observa los dados brillar en su mano, queda la duda:\n" +
              "¿Habrá aprendido la lección, o su sed de adrenalina lo llevará a buscar apuestas aún mayores?"
            : "Las fuerzas de Kael se agotan en las profundidades del Casino-Mazmorra.\n" +
              "El Rey Goblin Brujo reclama definitivamente su deuda.\n\n" +
              "El alma de Kael se transforma en una pieza más de la maquinaria del casino para toda la eternidad,\n" +
              "esperando al próximo temerario que se atreva a desafiar a la ruleta.";

        CreateText(epiloguePanel.transform, title, new Vector2(0f, 170f), new Vector2(1000f, 70f), 34f, Color.white);
        CreateText(epiloguePanel.transform, body, new Vector2(0f, 20f), new Vector2(980f, 220f), 22f, new Color(0.92f, 0.92f, 0.92f, 1f));

        CreateButton(epiloguePanel.transform, "Volver al Menú Principal", new Vector2(0f, -160f), new Vector2(340f, 60f), new Color(0.3f, 0.55f, 0.8f, 1f), () =>
        {
            Destroy(epiloguePanel);
        });
    }

    private TextMeshProUGUI CreateText(Transform parent, string text, Vector2 pos, Vector2 size, float fontSize, Color color)
    {
        GameObject obj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        TextMeshProUGUI tmp = obj.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = fontSize;
        tmp.color = color;
        return tmp;
    }

    private Button CreateButton(Transform parent, string label, Vector2 pos, Vector2 size, Color bgColor, UnityEngine.Events.UnityAction onClick)
    {
        GameObject btnObj = new GameObject($"Btn_{label}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(parent, false);
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        btnObj.GetComponent<Image>().color = bgColor;
        Button btn = btnObj.GetComponent<Button>();
        btn.onClick.AddListener(onClick);

        CreateText(btnObj.transform, $"<b>{label}</b>", Vector2.zero, size, 20f, Color.white);
        return btn;
    }
}
