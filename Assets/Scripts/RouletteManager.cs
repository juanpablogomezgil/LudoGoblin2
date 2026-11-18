using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class RouletteManager : MonoBehaviour
{
    [Header("Deck y Ruleta Visual")]
    public List<AbilitySO> playerDeck = new List<AbilitySO>();
    public List<RouletteSlice> rouletteSlices = new List<RouletteSlice>(); 
    public RectTransform rouletteWheel; 

    [Header("Configuración de Segmentos Variables (GDD: 6 a 8)")]
    [Range(4, 12)]
    public int targetSegmentCount = 8;
    public float sliceRadius = 66f;

    [Header("Ranuras de Juego (Abajo)")]
    public List<AbilitySlot> playableSlots = new List<AbilitySlot>(); 

    [Header("Configuración del Giro")]
    public float spinDuration = 2.5f;
    public float initialSpinSpeed = 700f;

    [Header("Referencias")]
    public Button rouletteButton; 
    public TurnManager turnManager; 

    private bool isSpinning = false;

    private Sprite cachedDefaultAbilityIcon;

    private void Awake()
    {
        CacheDefaultAbilityIcon();
        if (targetSegmentCount < 6)
        {
            targetSegmentCount = (GameManager.Instance != null && GameManager.Instance.rouletteSegmentCount >= 6)
                ? GameManager.Instance.rouletteSegmentCount
                : 8;
        }
        if (sliceRadius < 20f)
        {
            sliceRadius = 66f;
        }
        EnsureDefaultGDDAbilitiesInDeck();
        ConfigureDynamicSegments(targetSegmentCount);
        EnsurePointerIndicatorExists();
        PopulateSlicesFromDeck();
    }

    public Sprite GetDefaultAbilityIcon()
    {
        CacheDefaultAbilityIcon();
        return cachedDefaultAbilityIcon;
    }

    private void CacheDefaultAbilityIcon()
    {
        if (cachedDefaultAbilityIcon != null) return;

        foreach (AbilitySO ab in playerDeck)
        {
            if (ab != null && ab.icon != null)
            {
                cachedDefaultAbilityIcon = ab.icon;
                return;
            }
        }

        foreach (RouletteSlice slice in rouletteSlices)
        {
            if (slice != null && slice.assignedAbility != null && slice.assignedAbility.icon != null)
            {
                cachedDefaultAbilityIcon = slice.assignedAbility.icon;
                return;
            }
        }
    }

    // Permite cambiar dinámicamente el número de segmentos de la ruleta (ej. entre 6 y 8)
    public void ConfigureDynamicSegments(int desiredCount)
    {
        targetSegmentCount = Mathf.Clamp(desiredCount, 4, 12);
        if (rouletteWheel == null) return;

        // Limpiamos referencias nulas
        rouletteSlices.RemoveAll(s => s == null);

        // Si hacen falta más segmentos que los que hay en escena, clonamos el primero como plantilla
        if (rouletteSlices.Count > 0)
        {
            RouletteSlice template = rouletteSlices[0];
            while (rouletteSlices.Count < targetSegmentCount)
            {
                GameObject newSliceObj = Instantiate(template.gameObject, rouletteWheel);
                newSliceObj.name = $"Slice{rouletteSlices.Count + 1}";
                if (newSliceObj.TryGetComponent(out RouletteSlice newSlice))
                {
                    rouletteSlices.Add(newSlice);
                }
            }
        }

        // Activamos los segmentos hasta targetSegmentCount y distribuimos en círculo
        if (sliceRadius < 20f) sliceRadius = 66f;
        float angleStep = 360f / targetSegmentCount;
        for (int i = 0; i < rouletteSlices.Count; i++)
        {
            bool active = i < targetSegmentCount;
            rouletteSlices[i].gameObject.SetActive(active);
            if (!active) continue;

            if (rouletteSlices[i].TryGetComponent(out RectTransform rt))
            {
                // El índice 0 queda arriba (90 grados) cuando la rotación Z de la rueda es 0
                float angleDeg = 90f - (i * angleStep);
                float angleRad = angleDeg * Mathf.Deg2Rad;
                rt.anchoredPosition = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad)) * sliceRadius;
                rt.sizeDelta = new Vector2(26f, 26f);
                rt.localRotation = Quaternion.identity;
            }
        }
    }

    private void EnsurePointerIndicatorExists()
    {
        if (rouletteWheel == null || rouletteWheel.parent == null) return;

        Transform existingPointer = rouletteWheel.parent.Find("RoulettePointer");
        if (existingPointer != null) return;

        GameObject pointerObj = new GameObject("RoulettePointer", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        pointerObj.transform.SetParent(rouletteWheel.parent, false);

        RectTransform rt = pointerObj.GetComponent<RectTransform>();
        rt.anchorMin = rouletteWheel.anchorMin;
        rt.anchorMax = rouletteWheel.anchorMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = rouletteWheel.anchoredPosition + new Vector2(0f, (rouletteWheel.sizeDelta.y * 0.5f) + 16f);
        rt.sizeDelta = new Vector2(60f, 40f);

        TextMeshProUGUI tmp = pointerObj.GetComponent<TextMeshProUGUI>();
        tmp.text = "▼";
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 28f;
        tmp.color = new Color(1f, 0.85f, 0.2f, 1f);
        tmp.outlineWidth = 0.25f;
        tmp.outlineColor = Color.black;
        tmp.raycastTarget = false;
    }

    // Si el mazo de prueba solo tiene los 2 ataques iniciales, añade las habilidades del GDD (Escudo, Curación, Veneno, Bola de Fuego)
    private void EnsureDefaultGDDAbilitiesInDeck()
    {
        playerDeck.RemoveAll(a => a == null);
        if (playerDeck.Count >= 6) return;

        Sprite fallbackIcon = (playerDeck.Count > 0 && playerDeck[0] != null) ? playerDeck[0].icon : null;

        if (!playerDeck.Exists(a => a.abilityName.Contains("Escudo")))
        {
            playerDeck.Add(CreateRuntimeAbility("Escudo férreo", "Otorga escudo igual a la suma de dados + 2.", fallbackIcon,
                RequirementType.MinimumValue, 4, 2, useDmg: false, baseDmg: 0, useHeal: false, baseHeal: 0, useShield: true, baseShield: 2));
        }

        if (!playerDeck.Exists(a => a.abilityName.Contains("Curación") || a.abilityName.Contains("Cura")))
        {
            playerDeck.Add(CreateRuntimeAbility("Curación Arcana", "Recupera HP igual al valor del dado + 3.", fallbackIcon,
                RequirementType.MinimumValue, 3, 1, useDmg: false, baseDmg: 0, useHeal: true, baseHeal: 3, useShield: false, baseShield: 0));
        }

        if (!playerDeck.Exists(a => a.abilityName.Contains("Veneno") || a.abilityName.Contains("Toxina")))
        {
            AbilitySO poisonAb = CreateRuntimeAbility("Daga Venenosa", "Inflige 2 de daño y aplica 3 cargas de Veneno.", fallbackIcon,
                RequirementType.MaximumValue, 4, 1, useDmg: false, baseDmg: 2, useHeal: false, baseHeal: 0, useShield: false, baseShield: 0);
            poisonAb.statusEffects.Add(new StatusEffect { effectType = StatusEffectType.Poison, durationOrStacks = 3 });
            playerDeck.Add(poisonAb);
        }

        if (!playerDeck.Exists(a => a.abilityName.Contains("Bola de Fuego")))
        {
            AbilitySO fireAb = CreateRuntimeAbility("Bola de Fuego", "Suma 9 o más con hasta 2 dados para causar gran daño y Vulnerabilidad.", fallbackIcon,
                RequirementType.MinimumValue, 9, 2, useDmg: true, baseDmg: 4, useHeal: false, baseHeal: 0, useShield: false, baseShield: 0);
            fireAb.statusEffects.Add(new StatusEffect { effectType = StatusEffectType.Vulnerability, durationOrStacks = 2 });
            playerDeck.Add(fireAb);
        }
    }

    private AbilitySO CreateRuntimeAbility(
        string name, string desc, Sprite icon,
        RequirementType req, int targetVal, int maxSlots,
        bool useDmg, int baseDmg, bool useHeal, int baseHeal, bool useShield, int baseShield)
    {
        AbilitySO ab = ScriptableObject.CreateInstance<AbilitySO>();
        ab.name = name;
        ab.abilityName = name;
        ab.description = desc;
        ab.icon = icon;
        ab.requirement = req;
        ab.targetValue = targetVal;
        ab.maxDiceSlots = maxSlots;
        ab.useDiceValueAsDamage = useDmg;
        ab.baseDamageAmount = baseDmg;
        ab.useDiceValueAsHeal = useHeal;
        ab.healAmount = baseHeal;
        ab.useDiceValueAsShield = useShield;
        ab.shieldAmount = baseShield;
        return ab;
    }

    public void PopulateSlicesFromDeck()
    {
        if (playerDeck.Count == 0) return;

        CacheDefaultAbilityIcon();
        int activeCount = Mathf.Min(targetSegmentCount, rouletteSlices.Count);
        for (int i = 0; i < activeCount; i++)
        {
            AbilitySO ability = playerDeck[i % playerDeck.Count];
            if (ability != null && ability.icon == null && cachedDefaultAbilityIcon != null)
            {
                ability.icon = cachedDefaultAbilityIcon;
            }
            rouletteSlices[i].SetupSlice(ability);
        }
    }

    public void EnableRouletteButton(bool enable)
    {
        if (rouletteButton != null) rouletteButton.interactable = enable;
    }

    public void SpinRoulette()
    {
        if (isSpinning) return;

        EnableRouletteButton(false);
        if (playerDeck.Count == 0)
        {
            Debug.LogWarning("El playerDeck está vacío, no se puede girar.");
            return;
        }

        ConfigureDynamicSegments(targetSegmentCount);
        PopulateSlicesFromDeck();

        int activeCount = Mathf.Min(targetSegmentCount, rouletteSlices.Count);
        int winningIndex = Random.Range(0, activeCount);

        AudioManager.Instance?.PlayRouletteSpin();
        StartCoroutine(SpinCoroutine(winningIndex, activeCount));
    }

    private IEnumerator SpinCoroutine(int winningIndex, int activeCount)
    {
        isSpinning = true;

        float angleStep = 360f / Mathf.Max(1, activeCount);
        // Para que el segmento winningIndex (situado en 90 - winningIndex*angleStep) quede arriba en 90 grados,
        // el ángulo Z final de la rueda debe ser +(winningIndex * angleStep) (o su equivalente en giro horario)
        float targetAngleNormalized = Mathf.Repeat(winningIndex * angleStep, 360f);

        float startZ = rouletteWheel != null ? rouletteWheel.localEulerAngles.z : 0f;
        int fullSpins = Random.Range(4, 7);
        float deltaToTarget = Mathf.Repeat(startZ - targetAngleNormalized, 360f);
        float totalClockwiseRotation = (fullSpins * 360f) + deltaToTarget;

        float timeElapsed = 0f;
        while (timeElapsed < spinDuration)
        {
            timeElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(timeElapsed / spinDuration);
            // Ease-out cúbico para frenado realista de ruleta de casino
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            float currentZ = startZ - (totalClockwiseRotation * eased);

            if (rouletteWheel != null)
            {
                rouletteWheel.localEulerAngles = new Vector3(0f, 0f, currentZ);
            }
            yield return null; 
        }

        if (rouletteWheel != null)
        {
            rouletteWheel.localEulerAngles = new Vector3(0f, 0f, targetAngleNormalized);
        }

        AudioManager.Instance?.PlayRouletteStop();

        for (int i = 0; i < playableSlots.Count; i++)
        {
            AbilitySlot slot = playableSlots[i];
            if (slot == null) continue;

            int sliceIdx = (winningIndex + i) % activeCount;
            RouletteSlice winningSlice = rouletteSlices[sliceIdx];
            Debug.Log($"Puntero en segmento #{sliceIdx + 1}: la ranura {slot.name} desbloquea {winningSlice.assignedAbility.abilityName}");
            slot.SetupSlot(winningSlice.assignedAbility);
        }

        isSpinning = false;

        if (turnManager != null) turnManager.ContinueToDicePhase();
    }
}