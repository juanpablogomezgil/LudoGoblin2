using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public enum SpecialDiceBonusType
{
    None,
    IronArmor,          // Dado de Hierro / Maestro: otorga valor como Armadura
    HealBasic,          // Dado Curación Básico: cura el valor
    HealSuperiorCleanse,// Dado Curación Superior: cura el valor y limpia efectos negativos
    PoisonEnemy         // Dado de Veneno Básico / Superior: envenena al enemigo con el valor
}

[RequireComponent(typeof(CanvasGroup))]
public class DiceDraggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public int diceValue; 
    public TextMeshProUGUI valueText; 
    public SpecialDiceBonusType bonusType = SpecialDiceBonusType.None;
    public string diceDisplayName = "d6";
    
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Canvas mainCanvas;
    private Image backgroundImage;
    
    public Transform originalParent;
    public int originalSiblingIndex;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        backgroundImage = GetComponent<Image>();
        
        // Buscamos el Canvas principal para escalar el movimiento correctamente
        mainCanvas = GetComponentInParent<Canvas>(); 
    }

    public void SetupDice(int value, SpecialDiceBonusType specialBonus = SpecialDiceBonusType.None, string labelPrefix = "")
    {
        diceValue = value;
        bonusType = specialBonus;
        diceDisplayName = string.IsNullOrEmpty(labelPrefix) ? "d6" : labelPrefix;

        if (valueText != null)
        {
            valueText.text = string.IsNullOrEmpty(labelPrefix)
                ? value.ToString()
                : $"{value}\n<size=50%>{labelPrefix}</size>";
        }

        if (backgroundImage != null)
        {
            backgroundImage.color = specialBonus switch
            {
                SpecialDiceBonusType.IronArmor => new Color(0.65f, 0.82f, 0.95f, 1f),
                SpecialDiceBonusType.HealBasic => new Color(0.65f, 0.95f, 0.70f, 1f),
                SpecialDiceBonusType.HealSuperiorCleanse => new Color(0.45f, 0.98f, 0.80f, 1f),
                SpecialDiceBonusType.PoisonEnemy => new Color(0.78f, 0.55f, 0.95f, 1f),
                _ => Color.white
            };
        }
    }

    public void ApplySpecialBonus(HealthSystem playerHealth, EnemyController targetEnemy)
    {
        switch (bonusType)
        {
            case SpecialDiceBonusType.IronArmor:
                if (playerHealth != null)
                {
                    playerHealth.AddShield(diceValue);
                    Debug.Log($"[Dado Especial: {diceDisplayName}] Kael gana {diceValue} de Armadura.");
                }
                break;

            case SpecialDiceBonusType.HealBasic:
                if (playerHealth != null)
                {
                    playerHealth.Heal(diceValue);
                    Debug.Log($"[Dado Especial: {diceDisplayName}] Kael se cura {diceValue} HP.");
                }
                break;

            case SpecialDiceBonusType.HealSuperiorCleanse:
                if (playerHealth != null)
                {
                    playerHealth.Heal(diceValue);
                    playerHealth.ClearNegativeEffects();
                    Debug.Log($"[Dado Especial: {diceDisplayName}] Kael se cura {diceValue} HP y elimina estados negativos.");
                }
                break;

            case SpecialDiceBonusType.PoisonEnemy:
                if (targetEnemy != null)
                {
                    targetEnemy.ApplySingleStatusEffect(StatusEffectType.Poison, diceValue);
                    Debug.Log($"[Dado Especial: {diceDisplayName}] Aplica {diceValue} de Veneno al enemigo.");
                }
                break;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (mainCanvas == null) mainCanvas = GetComponentInParent<Canvas>();
        if (mainCanvas == null) return;

        // Verificamos de forma segura si el padre NO es una ranura
        if (transform.parent != null && !transform.parent.TryGetComponent(out AbilitySlot slot))
        {
            originalParent = transform.parent;
            originalSiblingIndex = transform.GetSiblingIndex(); 
        }

        transform.SetParent(mainCanvas.transform);
        transform.SetAsLastSibling(); 
        
        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.8f; 
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.alpha = 1f;

        if (mainCanvas == null) mainCanvas = GetComponentInParent<Canvas>();

        // Comprobamos si el padre del dado es la mesa principal (Canvas) o su zona original
        if (mainCanvas != null && (transform.parent == mainCanvas.transform || transform.parent == originalParent))
        {
            // Si no está en una ranura, reactivamos colisiones para poder volver a tocarlo
            canvasGroup.blocksRaycasts = true; 
            
            if (transform.parent == mainCanvas.transform)
            {
                ReturnToOriginalPosition();
            }
        }
        else
        {
            // Si el dado se quedó dentro de la ranura (AbilitySlot), lo hacemos intocable
            canvasGroup.blocksRaycasts = false;
        }
    }

    public void ReturnToOriginalPosition()
    {
        if (originalParent != null)
        {
            transform.SetParent(originalParent);
            transform.SetSiblingIndex(Mathf.Clamp(originalSiblingIndex, 0, originalParent.childCount - 1));
            transform.localScale = Vector3.one;
            if (originalParent is RectTransform parentRect)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
            }
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (mainCanvas == null) return;
        rectTransform.anchoredPosition += eventData.delta / mainCanvas.scaleFactor;
    }
}