using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class MainMenuController : MonoBehaviour
{
    private Canvas menuCanvas;
    private GameObject epiloguePanel;
    private GameObject creditsPanel;
    private GameObject optionsPanel;

    private void Start()
    {
        EnsureEventSystem();
        ApplySavedDisplaySettings();
        BuildMainMenuUI();
    }

    private void ApplySavedDisplaySettings()
    {
        if (PlayerPrefs.HasKey("Settings_ScreenWidth") && PlayerPrefs.HasKey("Settings_ScreenHeight"))
        {
            int w = PlayerPrefs.GetInt("Settings_ScreenWidth");
            int h = PlayerPrefs.GetInt("Settings_ScreenHeight");
            FullScreenMode mode = (FullScreenMode)PlayerPrefs.GetInt("Settings_ScreenMode", (int)Screen.fullScreenMode);
            if (w > 0 && h > 0)
            {
                Screen.SetResolution(w, h, mode);
            }
        }
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
        CreateButton(bgObj.transform, "NUEVA PARTIDA (Nueva Run)", new Vector2(0f, 35f), new Vector2(380f, 54f), new Color(0.22f, 0.65f, 0.35f, 1f), () =>
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
            new Vector2(0f, -25f), new Vector2(380f, 54f),
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
        CreateButton(bgObj.transform, "COMBATE RÁPIDO", new Vector2(0f, -85f), new Vector2(380f, 50f), new Color(0.55f, 0.38f, 0.72f, 1f), () =>
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.LoadSceneSafe(GameManager.CombatSceneName, GameManager.CombatSceneName);
            }
        });

        // Botón Opciones
        CreateButton(bgObj.transform, "OPCIONES", new Vector2(0f, -145f), new Vector2(380f, 50f), new Color(0.24f, 0.48f, 0.65f, 1f), () =>
        {
            ShowOptionsModal();
        });

        // Botón Créditos
        CreateButton(bgObj.transform, "CRÉDITOS", new Vector2(0f, -205f), new Vector2(380f, 50f), new Color(0.85f, 0.65f, 0.18f, 1f), () =>
        {
            ShowCreditsModal();
        });

        // Botón Salir
        CreateButton(bgObj.transform, "SALIR DEL JUEGO", new Vector2(0f, -265f), new Vector2(380f, 50f), new Color(0.68f, 0.24f, 0.24f, 1f), () =>
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

    private void ShowCreditsModal()
    {
        if (creditsPanel != null)
        {
            Destroy(creditsPanel);
        }
        if (optionsPanel != null)
        {
            Destroy(optionsPanel);
            optionsPanel = null;
        }

        creditsPanel = new GameObject("CreditsModal", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        creditsPanel.transform.SetParent(menuCanvas.transform, false);
        RectTransform rt = creditsPanel.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        creditsPanel.GetComponent<Image>().color = new Color(0.04f, 0.03f, 0.07f, 0.96f);

        // Tarjeta central de créditos
        GameObject cardObj = new GameObject("CreditsCard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        cardObj.transform.SetParent(creditsPanel.transform, false);
        RectTransform cardRt = cardObj.GetComponent<RectTransform>();
        cardRt.sizeDelta = new Vector2(760f, 420f);
        cardRt.anchoredPosition = Vector2.zero;
        cardObj.GetComponent<Image>().color = new Color(0.12f, 0.09f, 0.18f, 1f);

        // Título y Subtítulo
        CreateText(cardObj.transform, "<color=#FFD700><b>CRÉDITOS</b></color>", new Vector2(0f, 140f), new Vector2(700f, 50f), 36f, Color.white);
        CreateText(cardObj.transform, "<i>LudoGoblin</i>", new Vector2(0f, 95f), new Vector2(700f, 30f), 18f, new Color(0.8f, 0.7f, 0.95f, 1f));

        // Nombres solicitados
        string creditsText =
            "<size=22><b>Creado por:</b></size>\n\n" +
            "<size=24><color=#66FF99>Vicenç Martín de Vidales \nJuan Pablo Gómez \nJordi Puig</color></size>";
        CreateText(cardObj.transform, creditsText, new Vector2(0f, 0f), new Vector2(700f, 130f), 22f, Color.white);

        // Botón Volver
        CreateButton(cardObj.transform, "VOLVER", new Vector2(0f, -145f), new Vector2(260f, 52f), new Color(0.3f, 0.55f, 0.8f, 1f), () =>
        {
            if (creditsPanel != null)
            {
                Destroy(creditsPanel);
                creditsPanel = null;
            }
        });
    }

    private void ShowOptionsModal()
    {
        if (optionsPanel != null)
        {
            Destroy(optionsPanel);
        }
        if (creditsPanel != null)
        {
            Destroy(creditsPanel);
            creditsPanel = null;
        }

        optionsPanel = new GameObject("OptionsModal", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        optionsPanel.transform.SetParent(menuCanvas.transform, false);
        RectTransform rt = optionsPanel.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        optionsPanel.GetComponent<Image>().color = new Color(0.04f, 0.03f, 0.07f, 0.96f);

        // Tarjeta central del modal
        GameObject cardObj = new GameObject("OptionsCard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        cardObj.transform.SetParent(optionsPanel.transform, false);
        RectTransform cardRt = cardObj.GetComponent<RectTransform>();
        cardRt.sizeDelta = new Vector2(860f, 720f);
        cardRt.anchoredPosition = Vector2.zero;
        cardObj.GetComponent<Image>().color = new Color(0.12f, 0.09f, 0.18f, 1f);

        // Cabecera
        CreateText(cardObj.transform, "<color=#FFD700><b>AJUSTES Y OPCIONES</b></color>", new Vector2(0f, 310f), new Vector2(800f, 45f), 32f, Color.white);
        CreateText(cardObj.transform, "<i>Configuración de Audio, Pantalla y Resolución</i>", new Vector2(0f, 275f), new Vector2(800f, 26f), 17f, new Color(0.78f, 0.68f, 0.95f, 1f));

        // ----------------------------------------------------
        // SECCIÓN 1: AUDIO (Master, Música, SFX)
        // ----------------------------------------------------
        CreateText(cardObj.transform, "<color=#88EEFF><b>VOLUMEN DE AUDIO</b></color>", new Vector2(0f, 230f), new Vector2(780f, 28f), 20f, Color.white);

        float currentMaster = AudioManager.Instance != null ? AudioManager.Instance.masterVolume : PlayerPrefs.GetFloat("Settings_MasterVolume", 1f);
        float currentMusic = AudioManager.Instance != null ? AudioManager.Instance.musicVolume : PlayerPrefs.GetFloat("Settings_MusicVolume", 0.75f);
        float currentSfx = AudioManager.Instance != null ? AudioManager.Instance.sfxVolume : PlayerPrefs.GetFloat("Settings_SfxVolume", 1f);

        CreateVolumeRow(cardObj.transform, "Volumen General", 180f, currentMaster, (val) =>
        {
            if (AudioManager.Instance != null) AudioManager.Instance.SetMasterVolume(val);
            else PlayerPrefs.SetFloat("Settings_MasterVolume", val);
        });

        CreateVolumeRow(cardObj.transform, "Música", 130f, currentMusic, (val) =>
        {
            if (AudioManager.Instance != null) AudioManager.Instance.SetMusicVolume(val);
            else PlayerPrefs.SetFloat("Settings_MusicVolume", val);
        });

        CreateVolumeRow(cardObj.transform, "Efectos (SFX)", 80f, currentSfx, (val) =>
        {
            if (AudioManager.Instance != null) AudioManager.Instance.SetSfxVolume(val);
            else PlayerPrefs.SetFloat("Settings_SfxVolume", val);
        });

        // ----------------------------------------------------
        // SECCIÓN 2: RESOLUCIÓN DE PANTALLA
        // ----------------------------------------------------
        CreateText(cardObj.transform, "<color=#88EEFF><b>PANTALLA Y RESOLUCIÓN</b></color>", new Vector2(0f, 20f), new Vector2(780f, 28f), 20f, Color.white);

        List<Vector2Int> resList = GetSupportedResolutions();
        int activeWidth = PlayerPrefs.GetInt("Settings_ScreenWidth", Screen.width);
        int activeHeight = PlayerPrefs.GetInt("Settings_ScreenHeight", Screen.height);
        int currentResIndex = resList.FindIndex(r => r.x == activeWidth && r.y == activeHeight);
        if (currentResIndex < 0) currentResIndex = 0;

        CreateText(cardObj.transform, "Resolución:", new Vector2(-280f, -35f), new Vector2(220f, 35f), 19f, Color.white, TextAlignmentOptions.MidlineRight);

        GameObject resDisplayObj = new GameObject("ResDisplay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        resDisplayObj.transform.SetParent(cardObj.transform, false);
        RectTransform resDisplayRt = resDisplayObj.GetComponent<RectTransform>();
        resDisplayRt.anchoredPosition = new Vector2(50f, -35f);
        resDisplayRt.sizeDelta = new Vector2(200f, 38f);
        resDisplayObj.GetComponent<Image>().color = new Color(0.20f, 0.16f, 0.28f, 1f);

        TextMeshProUGUI resText = CreateText(resDisplayObj.transform, $"{resList[currentResIndex].x} x {resList[currentResIndex].y}", Vector2.zero, new Vector2(200f, 38f), 19f, new Color(1f, 0.88f, 0.3f, 1f));

        FullScreenMode activeMode = (FullScreenMode)PlayerPrefs.GetInt("Settings_ScreenMode", (int)Screen.fullScreenMode);

        CreateButton(cardObj.transform, "<", new Vector2(-110f, -35f), new Vector2(105f, 38f), new Color(0.26f, 0.34f, 0.48f, 1f), () =>
        {
            
            currentResIndex = (currentResIndex + 1) % resList.Count;
            Vector2Int chosen = resList[currentResIndex];
            resText.text = $"{chosen.x} x {chosen.y}";
            ApplyAndSaveScreenSettings(chosen.x, chosen.y, activeMode);
        });

        CreateButton(cardObj.transform, ">", new Vector2(210f, -35f), new Vector2(105f, 38f), new Color(0.26f, 0.34f, 0.48f, 1f), () =>
        {
            currentResIndex = (currentResIndex - 1 + resList.Count) % resList.Count;
            Vector2Int chosen = resList[currentResIndex];
            resText.text = $"{chosen.x} x {chosen.y}";
            ApplyAndSaveScreenSettings(chosen.x, chosen.y, activeMode);
        });

        // ----------------------------------------------------
        // SECCIÓN 3: TIPO DE PANTALLA (Completa, Sin Bordes, Ventana)
        // ----------------------------------------------------
        CreateText(cardObj.transform, "Tipo de Pantalla:", new Vector2(-260f, -105f), new Vector2(220f, 35f), 19f, Color.white, TextAlignmentOptions.MidlineRight);

        Button btnFullscreen = null;
        Button btnBorderless = null;
        Button btnWindowed = null;

        System.Action updateModeButtons = () =>
        {
            Color activeColor = new Color(0.18f, 0.62f, 0.35f, 1f);
            Color inactiveColor = new Color(0.24f, 0.21f, 0.32f, 1f);

            if (btnFullscreen != null) btnFullscreen.GetComponent<Image>().color = (activeMode == FullScreenMode.ExclusiveFullScreen) ? activeColor : inactiveColor;
            if (btnBorderless != null) btnBorderless.GetComponent<Image>().color = (activeMode == FullScreenMode.FullScreenWindow) ? activeColor : inactiveColor;
            if (btnWindowed != null) btnWindowed.GetComponent<Image>().color = (activeMode == FullScreenMode.Windowed) ? activeColor : inactiveColor;
        };

        btnFullscreen = CreateButton(cardObj.transform, "Pantalla Completa", new Vector2(-65f, -105f), new Vector2(165f, 42f), Color.white, () =>
        {
            activeMode = FullScreenMode.ExclusiveFullScreen;
            Vector2Int chosen = resList[currentResIndex];
            ApplyAndSaveScreenSettings(chosen.x, chosen.y, activeMode);
            updateModeButtons();
        });

        btnBorderless = CreateButton(cardObj.transform, "Sin Bordes", new Vector2(110f, -105f), new Vector2(165f, 42f), Color.white, () =>
        {
            activeMode = FullScreenMode.FullScreenWindow;
            Vector2Int chosen = resList[currentResIndex];
            ApplyAndSaveScreenSettings(chosen.x, chosen.y, activeMode);
            updateModeButtons();
        });

        btnWindowed = CreateButton(cardObj.transform, "Modo Ventana", new Vector2(285f, -105f), new Vector2(165f, 42f), Color.white, () =>
        {
            activeMode = FullScreenMode.Windowed;
            Vector2Int chosen = resList[currentResIndex];
            ApplyAndSaveScreenSettings(chosen.x, chosen.y, activeMode);
            updateModeButtons();
        });

        updateModeButtons();

        // ----------------------------------------------------
        // BOTÓN CERRAR / VOLVER
        // ----------------------------------------------------
        CreateButton(cardObj.transform, "GUARDAR Y VOLVER", new Vector2(0f, -220f), new Vector2(300f, 54f), new Color(0.3f, 0.55f, 0.8f, 1f), () =>
        {
            PlayerPrefs.Save();
            if (optionsPanel != null)
            {
                Destroy(optionsPanel);
                optionsPanel = null;
            }
        });
    }

    private void CreateVolumeRow(Transform parent, string label, float yPos, float initialValue, System.Action<float> onValueChanged)
    {
        CreateText(parent, label, new Vector2(-260f, yPos), new Vector2(220f, 35f), 19f, Color.white, TextAlignmentOptions.MidlineRight);

        TextMeshProUGUI valText = CreateText(parent, $"{(int)(initialValue * 100f)}%", new Vector2(285f, yPos), new Vector2(65f, 35f), 18f, new Color(1f, 0.88f, 0.3f, 1f));

        Slider slider = CreateSlider(parent, new Vector2(50f, yPos), new Vector2(280f, 20f), initialValue, (val) =>
        {
            valText.text = $"{(int)(val * 100f)}%";
            onValueChanged?.Invoke(val);
        });

        CreateButton(parent, "-", new Vector2(-115f, yPos), new Vector2(34f, 32f), new Color(0.32f, 0.25f, 0.42f, 1f), () =>
        {
            slider.value = Mathf.Clamp01(slider.value - 0.05f);
        });

        CreateButton(parent, "+", new Vector2(215f, yPos), new Vector2(34f, 32f), new Color(0.32f, 0.25f, 0.42f, 1f), () =>
        {
            slider.value = Mathf.Clamp01(slider.value + 0.05f);
        });
    }

    private Slider CreateSlider(Transform parent, Vector2 pos, Vector2 size, float initialValue, UnityEngine.Events.UnityAction<float> onValueChanged)
    {
        GameObject sliderObj = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
        sliderObj.transform.SetParent(parent, false);
        RectTransform sliderRt = sliderObj.GetComponent<RectTransform>();
        sliderRt.anchoredPosition = pos;
        sliderRt.sizeDelta = size;

        // Fondo de barra
        GameObject bgObj = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bgObj.transform.SetParent(sliderObj.transform, false);
        RectTransform bgRt = bgObj.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;
        bgObj.GetComponent<Image>().color = new Color(0.20f, 0.16f, 0.28f, 1f);

        // Área de relleno
        GameObject fillAreaObj = new GameObject("Fill Area", typeof(RectTransform));
        fillAreaObj.transform.SetParent(sliderObj.transform, false);
        RectTransform fillAreaRt = fillAreaObj.GetComponent<RectTransform>();
        fillAreaRt.anchorMin = Vector2.zero;
        fillAreaRt.anchorMax = Vector2.one;
        fillAreaRt.offsetMin = new Vector2(5f, 0f);
        fillAreaRt.offsetMax = new Vector2(-5f, 0f);

        // Barra de relleno
        GameObject fillObj = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fillObj.transform.SetParent(fillAreaObj.transform, false);
        RectTransform fillRt = fillObj.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = Vector2.zero;
        fillRt.offsetMax = Vector2.zero;
        fillObj.GetComponent<Image>().color = new Color(0.22f, 0.72f, 0.40f, 1f);

        // Área de manija / cursor
        GameObject handleAreaObj = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleAreaObj.transform.SetParent(sliderObj.transform, false);
        RectTransform handleAreaRt = handleAreaObj.GetComponent<RectTransform>();
        handleAreaRt.anchorMin = Vector2.zero;
        handleAreaRt.anchorMax = Vector2.one;
        handleAreaRt.offsetMin = new Vector2(10f, 0f);
        handleAreaRt.offsetMax = new Vector2(-10f, 0f);

        // Manija / Ficha dorada
        GameObject handleObj = new GameObject("Handle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        handleObj.transform.SetParent(handleAreaObj.transform, false);
        RectTransform handleRt = handleObj.GetComponent<RectTransform>();
        handleRt.sizeDelta = new Vector2(18f, size.y + 10f);
        handleObj.GetComponent<Image>().color = new Color(1f, 0.86f, 0.25f, 1f);

        Slider slider = sliderObj.GetComponent<Slider>();
        slider.fillRect = fillRt;
        slider.handleRect = handleRt;
        slider.targetGraphic = handleObj.GetComponent<Image>();
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = Mathf.Clamp01(initialValue);
        slider.onValueChanged.AddListener(onValueChanged);

        return slider;
    }

    private List<Vector2Int> GetSupportedResolutions()
    {
        List<Vector2Int> list = new List<Vector2Int>();
        Resolution[] resolutions = Screen.resolutions;
        if (resolutions != null && resolutions.Length > 0)
        {
            for (int i = 0; i < resolutions.Length; i++)
            {
                if (resolutions[i].width >= 1024 && resolutions[i].height >= 600)
                {
                    Vector2Int res = new Vector2Int(resolutions[i].width, resolutions[i].height);
                    if (!list.Contains(res))
                    {
                        list.Add(res);
                    }
                }
            }
        }

        if (list.Count == 0)
        {
            list.Add(new Vector2Int(2560, 1440));
            list.Add(new Vector2Int(1920, 1080));
            list.Add(new Vector2Int(1600, 900));
            list.Add(new Vector2Int(1366, 768));
            list.Add(new Vector2Int(1280, 720));
        }

        list.Sort((a, b) => (b.x * b.y).CompareTo(a.x * a.y));
        return list;
    }

    private void ApplyAndSaveScreenSettings(int width, int height, FullScreenMode mode)
    {
        Screen.SetResolution(width, height, mode);
        PlayerPrefs.SetInt("Settings_ScreenWidth", width);
        PlayerPrefs.SetInt("Settings_ScreenHeight", height);
        PlayerPrefs.SetInt("Settings_ScreenMode", (int)mode);
        PlayerPrefs.Save();
        Debug.Log($"[LudoGoblin] Pantalla configurada: {width}x{height} en modo {mode}");
    }

    private TextMeshProUGUI CreateText(Transform parent, string text, Vector2 pos, Vector2 size, float fontSize, Color color, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
    {
        GameObject obj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        TextMeshProUGUI tmp = obj.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.alignment = alignment;
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
        btn.onClick.AddListener(() => AudioManager.Instance?.PlayButtonClick());
        btn.onClick.AddListener(onClick);

        CreateText(btnObj.transform, $"<b>{label}</b>", Vector2.zero, size, 20f, Color.white);
        return btn;
    }
}
