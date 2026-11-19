using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class AbilitySlot : MonoBehaviour, IDropHandler
{
    [Header("Habilidad Asignada")]
    public AbilitySO abilityData; 
    public Image abilityIcon;
    public TextMeshProUGUI slotInfoText;

    [Header("Conexión de Combate")]
    [Tooltip("El enemigo que recibirá el daño de esta habilidad")]
    public EnemyController targetEnemy; 
    public HealthSystem playerHealth;
    public TurnManager turnManager;

    private bool isUsed = false; // Controla si la ranura ya se activó este turno
    private List<DiceDraggable> slottedDice = new List<DiceDraggable>();
    private Image backgroundPanelImage;
    private Image displayedAbilityIcon;

    private void Awake()
    {
        backgroundPanelImage = GetComponent<Image>();

        if (turnManager == null)
        {
            turnManager = FindFirstObjectByType<TurnManager>();
        }

        if (targetEnemy == null && turnManager != null)
        {
            targetEnemy = turnManager.enemyController;
        }

        if (playerHealth == null && turnManager != null)
        {
            playerHealth = turnManager.playerHealth;
        }

        EnsureInfoTextExists();
        EnsureAbilityIconImageExists();
    }

    private void EnsureAbilityIconImageExists()
    {
        // Si abilityIcon no está asignado o apunta al mismo Image del fondo del panel,
        // creamos/recuperamos un sub-objeto específico a la izquierda para mostrar el icono del ScriptableObject.
        if (abilityIcon != null && abilityIcon.gameObject != gameObject)
        {
            displayedAbilityIcon = abilityIcon;
        }
        else
        {
            Transform existingIcon = transform.Find("SlotAbilityIcon");
            if (existingIcon != null && existingIcon.TryGetComponent(out Image existingImg))
            {
                displayedAbilityIcon = existingImg;
            }
            else
            {
                GameObject iconObj = new GameObject("SlotAbilityIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement));
                iconObj.transform.SetParent(transform, false);
                displayedAbilityIcon = iconObj.GetComponent<Image>();
            }
            abilityIcon = displayedAbilityIcon;
        }

        if (displayedAbilityIcon.TryGetComponent(out LayoutElement le))
        {
            le.ignoreLayout = true;
        }

        if (displayedAbilityIcon.TryGetComponent(out RectTransform rt))
        {
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(56f, 0f);
            rt.sizeDelta = new Vector2(76f, 76f);
        }

        displayedAbilityIcon.preserveAspect = true;
        displayedAbilityIcon.raycastTarget = false;
    }

    private void EnsureInfoTextExists()
    {
        // IMPORTANTE: Desactivar el HorizontalLayoutGroup que tenía AbilitySlot en SampleScene
        // porque aplastaba el ancho del texto a 0px causando una columna vertical de letras.
        if (TryGetComponent(out HorizontalLayoutGroup hlg))
        {
            hlg.enabled = false;
        }

        if (slotInfoText == null)
        {
            slotInfoText = GetComponentInChildren<TextMeshProUGUI>();
        }

        if (slotInfoText == null)
        {
            GameObject textObj = new GameObject("SlotInfoText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI), typeof(LayoutElement));
            textObj.transform.SetParent(transform, false);
            slotInfoText = textObj.GetComponent<TextMeshProUGUI>();
        }

        if (slotInfoText.TryGetComponent(out LayoutElement le))
        {
            le.ignoreLayout = true;
        }

        if (slotInfoText.TryGetComponent(out RectTransform rt))
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(44f, 0f);
            rt.sizeDelta = new Vector2(410f, 105f);
        }

        slotInfoText.alignment = TextAlignmentOptions.Center;
        slotInfoText.fontSize = 18f;
        slotInfoText.color = Color.white;
        slotInfoText.raycastTarget = false;
    }

    public void PrepareForRouletteSpin()
    {
        EnsureInfoTextExists();
        EnsureAbilityIconImageExists();
        slottedDice.Clear();
        abilityData = null;
        isUsed = false;

        if (backgroundPanelImage == null) backgroundPanelImage = GetComponent<Image>();
        if (backgroundPanelImage != null)
        {
            backgroundPanelImage.sprite = null;
            backgroundPanelImage.color = new Color(0.14f, 0.12f, 0.20f, 0.94f);
        }

        if (displayedAbilityIcon != null)
        {
            displayedAbilityIcon.sprite = null;
            displayedAbilityIcon.enabled = false;
        }

        UpdateSlotLabel();
    }

    public void SetupSlot(AbilitySO newAbility)
    {
        EnsureInfoTextExists();
        EnsureAbilityIconImageExists();

        slottedDice.Clear();
        abilityData = newAbility;
        isUsed = false;

        if (backgroundPanelImage == null) backgroundPanelImage = GetComponent<Image>();
        if (backgroundPanelImage != null)
        {
            backgroundPanelImage.sprite = null;
            backgroundPanelImage.color = new Color(0.16f, 0.12f, 0.26f, 0.96f);
        }

        if (displayedAbilityIcon != null)
        {
            if (newAbility != null && newAbility.icon != null)
            {
                displayedAbilityIcon.sprite = newAbility.icon;
                displayedAbilityIcon.type = Image.Type.Simple;
                displayedAbilityIcon.preserveAspect = true;
                displayedAbilityIcon.color = Color.white;
                displayedAbilityIcon.enabled = true;
            }
            else
            {
                displayedAbilityIcon.sprite = null;
                displayedAbilityIcon.enabled = false;
            }
        }

        UpdateSlotLabel();
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (isUsed || abilityData == null) return;
        if (turnManager != null && turnManager.state != BattleState.PlayerTurn) return;

        if (eventData.pointerDrag != null && eventData.pointerDrag.TryGetComponent(out DiceDraggable droppedDice))
        {
            // 1. Rechazamos si ya hemos alcanzado el límite de ranuras
            if (slottedDice.Count >= abilityData.maxDiceSlots) return;

            // 2. Aceptamos el dado temporalmente
            droppedDice.transform.SetParent(transform);
            droppedDice.transform.localPosition = Vector3.zero;
            slottedDice.Add(droppedDice);
            AudioManager.Instance?.PlayDicePlace();

            // Hacemos que este dado sea "transparente" al ratón para que no bloquee los siguientes dados
            if (droppedDice.TryGetComponent(out CanvasGroup cg))
            {
                cg.blocksRaycasts = false;
            }

            // 3. Extraemos los valores de todos los dados colocados hasta ahora
            int[] currentValues = GetCurrentSlottedValues();

            // 4. Evaluamos la jugada
            if (abilityData.IsMoveValid(currentValues))
            {
                ExecuteAbility(currentValues);
            }
            else if (slottedDice.Count >= abilityData.maxDiceSlots || abilityData.IsImpossibleToComplete(currentValues))
            {
                // Si la ranura está llena o ya se pasó del límite máximo, devolvemos todos los dados
                ReturnAllSlottedDice();
                Debug.Log($"Jugada no válida para {abilityData.abilityName}. Dados devueltos al Dadopool.");
                UpdateSlotLabel();
            }
            else
            {
                // Aún faltan dados para completar una habilidad multi-dado
                droppedDice.gameObject.SetActive(false);
                UpdateSlotLabel();
            }
        }
    }

    private int[] GetCurrentSlottedValues()
    {
        int[] currentValues = new int[slottedDice.Count];
        for (int i = 0; i < slottedDice.Count; i++)
        {
            currentValues[i] = slottedDice[i].diceValue;
        }
        return currentValues;
    }

    public void ReturnAllSlottedDice()
    {
        foreach (DiceDraggable dice in slottedDice)
        {
            if (dice == null) continue;
            dice.gameObject.SetActive(true);
            if (dice.TryGetComponent(out CanvasGroup cg))
            {
                cg.blocksRaycasts = true;
            }
            dice.ReturnToOriginalPosition();
        }
        slottedDice.Clear();
    }

    private void ExecuteAbility(int[] finalDiceValues)
    {
        string displayName = string.IsNullOrEmpty(abilityData.abilityName) ? abilityData.name : abilityData.abilityName;
        Debug.Log($"¡Éxito! Ejecutando {displayName}");

        if (playerHealth == null && turnManager != null) playerHealth = turnManager.playerHealth;
        if (targetEnemy == null && turnManager != null) targetEnemy = turnManager.enemyController;

        // Animación de ataque de Kael
        if (playerHealth != null && playerHealth.unitAnimator != null)
        {
            playerHealth.unitAnimator.SetTrigger("2_Attack");
        }

        // 1. Daño y Estados Alterados al Enemigo
        if (targetEnemy != null)
        {
            int finalDamage = abilityData.CalculateDamage(finalDiceValues);
            if (finalDamage > 0)
            {
                AudioManager.Instance?.PlayAttackSound();
                targetEnemy.ReceiveAttack(finalDamage);
            }
            
            if (abilityData.statusEffects != null && abilityData.statusEffects.Count > 0)
            {
                targetEnemy.ApplyStatusEffects(abilityData.statusEffects);
            }
        }

        // 2. Curación y Escudo para Kael
        if (playerHealth != null)
        {
            int finalHeal = abilityData.CalculateHeal(finalDiceValues);
            if (finalHeal > 0)
            {
                playerHealth.Heal(finalHeal);
            }

            int finalShield = abilityData.CalculateShield(finalDiceValues);
            if (finalShield > 0)
            {
                playerHealth.AddShield(finalShield);
            }
        }

        // 3. Efectos secundarios de Dados Consumibles Especiales (Hierro, Curación, Veneno)
        foreach (DiceDraggable dice in slottedDice)
        {
            if (dice == null) continue;
            dice.ApplySpecialBonus(playerHealth, targetEnemy);
            dice.gameObject.SetActive(false);
        }

        isUsed = true;
        if (backgroundPanelImage == null) backgroundPanelImage = GetComponent<Image>();
        if (backgroundPanelImage != null) backgroundPanelImage.color = new Color(0.14f, 0.32f, 0.18f, 0.94f);
        UpdateSlotLabel();

        // 4. Comprobar si el enemigo ha muerto tras ejecutar la habilidad
        if (turnManager != null)
        {
            turnManager.EvaluateBattleOutcome();
        }
    }

    private void UpdateSlotLabel()
    {
        if (slotInfoText == null) return;

        if (abilityData == null)
        {
            slotInfoText.text = "<color=#FFD700><b>1º GIRA LA RULETA</b></color>\n<size=80%>Haz clic sobre la rueda de arriba para desbloquear tu habilidad de este turno</size>";
            return;
        }

        string title = string.IsNullOrEmpty(abilityData.abilityName) ? abilityData.name : abilityData.abilityName;
        string reqText = abilityData.GetRequirementSummary();
        string effectText = abilityData.GetEffectSummary();

        if (isUsed)
        {
            slotInfoText.text = $"<color=#66FF66><b>[OK] {title} ¡EJECUTADA!</b></color>\n<size=80%>{effectText}\nPulsa 'FINALIZAR TURNO' abajo a la derecha</size>";
            return;
        }

        if (slottedDice.Count > 0)
        {
            int partialSum = abilityData.GetDiceSum(GetCurrentSlottedValues());
            slotInfoText.text =
                $"<color=#FFD700><b>{title}</b></color> — <color=#88EEFF>{effectText}</color>\n" +
                $"<size=82%>{reqText}</size>\n" +
                $"<color=#FFE066><b>Suma actual: {partialSum} ({slottedDice.Count}/{abilityData.maxDiceSlots} dados) — ¡Arrastra otro dado!</b></color>";
        }
        else
        {
            slotInfoText.text =
                $"<color=#FFD700><b>HABILIDAD ACTIVA: {title}</b></color>\n" +
                $"<size=82%><color=#88EEFF>{reqText}</color>   |   <color=#FFB870>{effectText}</color></size>\n" +
                $"<size=78%><color=#DDDDDD>>> Arrastra aquí tus dados desde la mesa inferior <<</color></size>";
        }
    }
}