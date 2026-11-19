using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

// Definimos todas las fases posibles del combate
public enum BattleState
{
    Start,
    RouletteSpin,
    DiceRoll,
    PlayerTurn,
    EnemyTurn,
    Won,
    Lost
}

public class TurnManager : MonoBehaviour
{
    [Header("Estado Actual")]
    public BattleState state;

    [Header("Referencias")]
    public RouletteManager rouletteManager;
    public EnemyController enemyController;
    public HealthSystem playerHealth; 
    public DiceManager diceManager;

    [Header("Interfaz de Usuario")]
    public Button endTurnButton;
    public TextMeshProUGUI turnBannerText;

    private HealthSystem enemyHealth;
    private GameObject endOfBattlePanel;
    private ConsumableBeltUI consumableBeltUI;

    private void Awake()
    {
        if (enemyController != null)
        {
            enemyHealth = enemyController.GetComponent<HealthSystem>();
        }

        if (GetComponent<PauseMenuController>() == null)
        {
            gameObject.AddComponent<PauseMenuController>();
        }

        consumableBeltUI = GetComponent<ConsumableBeltUI>();
        if (consumableBeltUI == null)
        {
            consumableBeltUI = gameObject.AddComponent<ConsumableBeltUI>();
        }
    }

    private void OnEnable()
    {
        if (playerHealth != null) playerHealth.OnDied += HandleUnitDied;
        if (enemyHealth != null) enemyHealth.OnDied += HandleUnitDied;
    }

    private void OnDisable()
    {
        if (playerHealth != null) playerHealth.OnDied -= HandleUnitDied;
        if (enemyHealth != null) enemyHealth.OnDied -= HandleUnitDied;
    }

    private void Start()
    {
        ConfigureResponsiveCombatLayout();
        EnsureBannerUIExists();
        state = BattleState.Start;
        if (endTurnButton != null) endTurnButton.interactable = false;
        if (rouletteManager != null) rouletteManager.EnableRouletteButton(false);
        StartCoroutine(SetupBattle());
    }

    private void ConfigureResponsiveCombatLayout()
    {
        Canvas mainCanvas = FindFirstObjectByType<Canvas>();
        if (mainCanvas != null && mainCanvas.TryGetComponent(out CanvasScaler scaler))
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }

        // 1. Ruleta arriba en el centro
        if (rouletteManager != null && rouletteManager.rouletteWheel != null)
        {
            rouletteManager.rouletteWheel.anchoredPosition = new Vector2(0f, 235f);
            rouletteManager.rouletteWheel.sizeDelta = new Vector2(210f, 210f);
        }

        // 2. Ranura de Habilidad Activa justo debajo de la Ruleta (centro de la pantalla, bien visible)
        if (rouletteManager != null && rouletteManager.playableSlots != null)
        {
            for (int i = 0; i < rouletteManager.playableSlots.Count; i++)
            {
                AbilitySlot slot = rouletteManager.playableSlots[i];
                if (slot != null && slot.TryGetComponent(out RectTransform slotRt))
                {
                    slotRt.anchorMin = new Vector2(0.5f, 0.5f);
                    slotRt.anchorMax = new Vector2(0.5f, 0.5f);
                    slotRt.pivot = new Vector2(0.5f, 0.5f);
                    slotRt.anchoredPosition = new Vector2(0f, -15f);
                    slotRt.sizeDelta = new Vector2(520f, 115f);
                }
            }
        }

        // 3. Mesa de dados (Dadopool) debajo de la ranura de habilidad
        if (diceManager != null && diceManager.diceSpawnArea != null)
        {
            if (diceManager.diceSpawnArea.TryGetComponent(out RectTransform diceAreaRt))
            {
                diceAreaRt.anchorMin = new Vector2(0.5f, 0.5f);
                diceAreaRt.anchorMax = new Vector2(0.5f, 0.5f);
                diceAreaRt.pivot = new Vector2(0.5f, 0.5f);
                diceAreaRt.anchoredPosition = new Vector2(0f, -195f);
                diceAreaRt.sizeDelta = new Vector2(880f, 135f);
            }
            if (diceManager.diceSpawnArea.TryGetComponent(out HorizontalLayoutGroup hlg))
            {
                hlg.spacing = 18f;
                hlg.childAlignment = TextAnchor.MiddleCenter;
            }
        }

        // 4. Botón de Finalizar Turno abajo a la derecha (dentro del área segura)
        if (endTurnButton != null)
        {
            if (endTurnButton.TryGetComponent(out RectTransform endBtnRt))
            {
                endBtnRt.anchorMin = new Vector2(0.5f, 0.5f);
                endBtnRt.anchorMax = new Vector2(0.5f, 0.5f);
                endBtnRt.pivot = new Vector2(0.5f, 0.5f);
                endBtnRt.anchoredPosition = new Vector2(390f, -350f);
                endBtnRt.sizeDelta = new Vector2(280f, 70f);
            }
            if (endTurnButton.TryGetComponent(out Image btnImg))
            {
                btnImg.color = new Color(0.20f, 0.68f, 0.32f, 1f);
            }
            TextMeshProUGUI btnTmp = endTurnButton.GetComponentInChildren<TextMeshProUGUI>();
            if (btnTmp != null)
            {
                btnTmp.text = "<b>FINALIZAR TURNO >></b>";
                btnTmp.fontSize = 22f;
                btnTmp.color = Color.white;
            }
        }
    }

    private void EnsureBannerUIExists()
    {
        if (turnBannerText != null) return;

        Canvas mainCanvas = FindFirstObjectByType<Canvas>();
        if (mainCanvas == null) return;

        Transform existing = mainCanvas.transform.Find("TurnStatusBanner");
        if (existing != null && existing.TryGetComponent(out TextMeshProUGUI existingTmp))
        {
            turnBannerText = existingTmp;
            return;
        }

        GameObject bannerObj = new GameObject("TurnStatusBanner", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        bannerObj.transform.SetParent(mainCanvas.transform, false);

        RectTransform rt = bannerObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -15f);
        rt.sizeDelta = new Vector2(760f, 50f);

        turnBannerText = bannerObj.GetComponent<TextMeshProUGUI>();
        turnBannerText.alignment = TextAlignmentOptions.Center;
        turnBannerText.fontSize = 24f;
        turnBannerText.color = new Color(1f, 0.92f, 0.55f, 1f);
        turnBannerText.outlineWidth = 0.25f;
        turnBannerText.outlineColor = Color.black;
        turnBannerText.raycastTarget = false;
    }

    private void SetBannerMessage(string message)
    {
        if (turnBannerText != null)
        {
            turnBannerText.text = message;
        }
    }

    private IEnumerator SetupBattle()
    {
        Debug.Log("Configurando la batalla... Repartiendo cartas/dados iniciales.");
        ApplyGameManagerProgression();

        if (consumableBeltUI != null)
        {
            consumableBeltUI.Initialize(this, diceManager);
        }

        string rivalName = enemyController != null ? enemyController.enemyName : "Enemigo";
        int roomNum = GameManager.Instance != null ? GameManager.Instance.currentRoom : 1;
        int maxRooms = GameManager.Instance != null ? GameManager.Instance.maxRooms : 4;
        SetBannerMessage($"Sala {roomNum}/{maxRooms} — ¡Combate contra {rivalName}!");
        
        yield return new WaitForSeconds(1f);
        
        StartCoroutine(RoulettePhase());
    }

    private void ApplyGameManagerProgression()
    {
        if (GameManager.Instance == null) return;

        GameManager gm = GameManager.Instance;
        gm.InitializeCatalogsIfNeeded();

        if (playerHealth != null)
        {
            playerHealth.InitializeHealth(gm.playerMaxHealth, gm.playerCurrentHealth);
        }

        if (rouletteManager != null && gm.equippedRouletteAbilities.Count > 0)
        {
            rouletteManager.playerDeck = new System.Collections.Generic.List<AbilitySO>(gm.equippedRouletteAbilities);
            rouletteManager.ConfigureDynamicSegments(gm.rouletteSegmentCount);
            rouletteManager.PopulateSlicesFromDeck();
        }

        if (enemyController != null && enemyHealth != null)
        {
            switch (gm.currentRoom)
            {
                case 1:
                    enemyController.enemyName = "Esbirro Goblin";
                    enemyController.baseAttackDamage = 6;
                    enemyController.defendShieldAmount = 4;
                    enemyController.poisonStacksToApply = 2;
                    enemyController.attackWeight = 70;
                    enemyController.defendWeight = 20;
                    enemyController.poisonWeight = 10;
                    enemyHealth.InitializeHealth(22);
                    break;
                case 2:
                    enemyController.enemyName = "Guardia del Casino";
                    enemyController.baseAttackDamage = 8;
                    enemyController.defendShieldAmount = 6;
                    enemyController.poisonStacksToApply = 2;
                    enemyController.attackWeight = 55;
                    enemyController.defendWeight = 35;
                    enemyController.poisonWeight = 10;
                    enemyHealth.InitializeHealth(32);
                    break;
                case 3:
                    enemyController.enemyName = "Chamán Goblin";
                    enemyController.baseAttackDamage = 9;
                    enemyController.defendShieldAmount = 6;
                    enemyController.poisonStacksToApply = 4;
                    enemyController.attackWeight = 40;
                    enemyController.defendWeight = 20;
                    enemyController.poisonWeight = 40;
                    enemyHealth.InitializeHealth(42);
                    break;
                default:
                    enemyController.enemyName = "Rey Goblin Brujo (Jefe)";
                    enemyController.baseAttackDamage = 12;
                    enemyController.defendShieldAmount = 8;
                    enemyController.poisonStacksToApply = 5;
                    enemyController.attackWeight = 50;
                    enemyController.defendWeight = 25;
                    enemyController.poisonWeight = 25;
                    enemyHealth.InitializeHealth(65);
                    break;
            }
        }
    }

    private IEnumerator RoulettePhase()
    {
        if (EvaluateBattleOutcome()) yield break;

        // Al iniciar el turno de Kael, se limpia el escudo temporal del turno previo y se aplican sus estados (ej. Veneno)
        if (playerHealth != null)
        {
            playerHealth.ClearShield();
            playerHealth.ProcessEndOfTurnEffects();
        }

        if (EvaluateBattleOutcome()) yield break;

        state = BattleState.RouletteSpin;
        Debug.Log("Fase 1: Girando la Ruleta de Habilidades...");
        SetBannerMessage("Fase 1: Haz clic en la Ruleta para girar tu suerte");

        if (rouletteManager != null)
        {
            if (rouletteManager.playableSlots != null)
            {
                foreach (AbilitySlot slot in rouletteManager.playableSlots)
                {
                    if (slot != null) slot.PrepareForRouletteSpin();
                }
            }
            rouletteManager.EnableRouletteButton(true);
        }

        yield break;
    }

    public void ContinueToDicePhase()
    {
        if (state == BattleState.Won || state == BattleState.Lost) return;
        StartCoroutine(DiceRollPhase());
    }

    private IEnumerator DiceRollPhase()
    {
        if (EvaluateBattleOutcome()) yield break;

        state = BattleState.DiceRoll;
        Debug.Log("Fase 2: Lanzando el Dadopool...");
        SetBannerMessage("Fase 2: Lanzando dados del Dadopool...");

        if (diceManager != null)
        {
            diceManager.RollAllDice();
        }

        yield return new WaitForSeconds(0.6f); 

        PlayerPhase();
    }

    private void PlayerPhase()
    {
        if (EvaluateBattleOutcome()) return;

        state = BattleState.PlayerTurn;
        Debug.Log("Fase 3: Turno del Jugador. Esperando interacciones Drag & Drop...");
        SetBannerMessage("Tu Turno: Arrastra dados a la habilidad o pulsa Finalizar Turno");
        if (endTurnButton != null) endTurnButton.interactable = true;
        if (consumableBeltUI != null) consumableBeltUI.RefreshUI();
    }

    // Este método se llama desde el botón "EndTurnButton" en la UI de Unity (OnClick)
    public void OnEndTurnButton()
    {
        if (state != BattleState.PlayerTurn) return; 

        AudioManager.Instance?.PlayButtonClick();
        if (endTurnButton != null) endTurnButton.interactable = false;
        
        if (diceManager != null)
        {
            diceManager.ClearDice();
            Debug.Log("Mesa limpiada: dados sobrantes destruidos.");
        }

        state = BattleState.EnemyTurn;
        if (consumableBeltUI != null) consumableBeltUI.RefreshUI();

        StartCoroutine(EnemyPhase());
    }

    private IEnumerator EnemyPhase()
    {
        if (EvaluateBattleOutcome()) yield break;

        state = BattleState.EnemyTurn;
        Debug.Log("Fase 4: Turno del Enemigo.");
        string rivalName = enemyController != null ? enemyController.enemyName : "Enemigo";
        SetBannerMessage($"Turno de {rivalName}...");

        if (enemyController != null)
        {
            // 1. El enemigo sufre sus efectos de inicio de turno (ej. Veneno)
            enemyController.ProcessEndOfTurnEffects();
            yield return new WaitForSeconds(0.8f); 

            if (EvaluateBattleOutcome()) yield break;

            // 2. La IA enemiga decide y ejecuta su acción (Atacar, Defender o Envenenar)
            if (playerHealth != null && enemyHealth != null && !enemyHealth.IsDead)
            {
                enemyController.ExecuteAITurn(playerHealth);
                SetBannerMessage(enemyController.LastActionDescription);
            }
            yield return new WaitForSeconds(1.2f);
        }

        if (EvaluateBattleOutcome()) yield break;

        // 3. Volvemos a empezar el bucle para el jugador
        StartCoroutine(RoulettePhase());
    }

    private void HandleUnitDied(HealthSystem deadUnit)
    {
        EvaluateBattleOutcome();
    }

    public bool EvaluateBattleOutcome()
    {
        if (state == BattleState.Won || state == BattleState.Lost) return true;

        int eHp = enemyHealth != null ? enemyHealth.CurrentHealth : 1;
        int pHp = playerHealth != null ? playerHealth.CurrentHealth : 1;

        CheckBattleEnd(eHp, pHp);
        return state == BattleState.Won || state == BattleState.Lost;
    }

    public void CheckBattleEnd(int enemyHp, int playerHp)
    {
        if (state == BattleState.Won || state == BattleState.Lost) return;

        if (enemyHp <= 0)
        {
            state = BattleState.Won;
            AudioManager.Instance?.PlayVictorySound();
            StopAllCoroutines();
            DisableCombatControls();
            Debug.Log("¡Victoria! Abriendo menú de recompensas...");
            SetBannerMessage("<color=#66FF66>¡VICTORIA! Has derrotado al enemigo.</color>");
            ShowEndOfBattleOverlay(true);
        }
        else if (playerHp <= 0)
        {
            state = BattleState.Lost;
            AudioManager.Instance?.PlayDefeatSound();
            StopAllCoroutines();
            DisableCombatControls();
            Debug.Log("Permadeath: Has perdido todo el progreso de la partida.");
            SetBannerMessage("<color=#FF5555>DERROTA (Muerte Permanente): Tu alma pertenece al Casino.</color>");
            ShowEndOfBattleOverlay(false);
        }
    }

    private void DisableCombatControls()
    {
        if (endTurnButton != null) endTurnButton.interactable = false;
        if (rouletteManager != null) rouletteManager.EnableRouletteButton(false);
        if (diceManager != null) diceManager.ClearDice();
        if (consumableBeltUI != null) consumableBeltUI.RefreshUI();
    }

    private void ShowEndOfBattleOverlay(bool victory)
    {
        if (endOfBattlePanel != null) return;

        Canvas mainCanvas = FindFirstObjectByType<Canvas>();
        if (mainCanvas == null) return;

        endOfBattlePanel = new GameObject("EndOfBattleOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        endOfBattlePanel.transform.SetParent(mainCanvas.transform, false);

        RectTransform panelRt = endOfBattlePanel.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.sizeDelta = new Vector2(460f, 220f);
        panelRt.anchoredPosition = Vector2.zero;

        Image bg = endOfBattlePanel.GetComponent<Image>();
        bg.color = new Color(0.08f, 0.08f, 0.12f, 0.94f);

        GameObject titleObj = new GameObject("ResultTitle", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(endOfBattlePanel.transform, false);
        RectTransform titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 0.45f);
        titleRt.anchorMax = new Vector2(1f, 0.95f);
        titleRt.offsetMin = new Vector2(20f, 0f);
        titleRt.offsetMax = new Vector2(-20f, 0f);

        int roomNum = GameManager.Instance != null ? GameManager.Instance.currentRoom : 1;
        int goldEarned = 25 + (roomNum * 10);

        TextMeshProUGUI titleTmp = titleObj.GetComponent<TextMeshProUGUI>();
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.fontSize = 24f;
        titleTmp.text = victory
            ? $"<color=#66FF66><b>¡SALA {roomNum} SUPERADA!</b></color>\n<size=70%>Recompensa: +{goldEarned} Oro</size>"
            : "<color=#FF5555><b>HAS CAÍDO EN EL CASINO</b></color>\n<size=70%>Muerte Permanente (Permadeath)</size>";

        GameObject btnObj = new GameObject("ContinueButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(endOfBattlePanel.transform, false);
        RectTransform btnRt = btnObj.GetComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(0.5f, 0.12f);
        btnRt.anchorMax = new Vector2(0.5f, 0.12f);
        btnRt.sizeDelta = new Vector2(260f, 50f);
        btnRt.anchoredPosition = new Vector2(0f, 20f);

        Image btnImg = btnObj.GetComponent<Image>();
        btnImg.color = victory ? new Color(0.2f, 0.65f, 0.3f, 1f) : new Color(0.7f, 0.25f, 0.25f, 1f);

        GameObject btnTextObj = new GameObject("BtnText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        btnTextObj.transform.SetParent(btnObj.transform, false);
        RectTransform btnTextRt = btnTextObj.GetComponent<RectTransform>();
        btnTextRt.anchorMin = Vector2.zero;
        btnTextRt.anchorMax = Vector2.one;
        btnTextRt.offsetMin = Vector2.zero;
        btnTextRt.offsetMax = Vector2.zero;

        TextMeshProUGUI btnTmp = btnTextObj.GetComponent<TextMeshProUGUI>();
        btnTmp.alignment = TextAlignmentOptions.Center;
        btnTmp.fontSize = 20f;
        btnTmp.color = Color.white;
        btnTmp.text = victory ? "Ir a Build / Siguiente Sala" : "Continuar";

        Button btn = btnObj.GetComponent<Button>();
        btn.onClick.AddListener(() =>
        {
            if (GameManager.Instance != null)
            {
                if (victory)
                {
                    int remainingHp = playerHealth != null ? playerHealth.CurrentHealth : 100;
                    GameManager.Instance.OnCombatVictory(remainingHp);
                }
                else
                {
                    GameManager.Instance.OnCombatDefeatPermadeath();
                }
            }
            else
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
        });
    }
}