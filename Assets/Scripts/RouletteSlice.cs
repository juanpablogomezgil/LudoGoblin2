using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RouletteSlice : MonoBehaviour
{
    public AbilitySO assignedAbility;
    
    [Tooltip("Arrastra aquí el componente Image de este mismo objeto")]
    public Image sliceIcon; 
    public TextMeshProUGUI sliceLabel;

    private static Sprite cachedFallbackSprite;

    private void Awake()
    {
        if (sliceIcon == null)
        {
            sliceIcon = GetComponent<Image>();
        }
        if (sliceIcon != null && sliceIcon.sprite != null && cachedFallbackSprite == null)
        {
            cachedFallbackSprite = sliceIcon.sprite;
        }
    }

    private void LateUpdate()
    {
        // Mantiene el texto del segmento siempre derecho aunque la rueda gire
        if (sliceLabel != null)
        {
            sliceLabel.transform.rotation = Quaternion.identity;
        }
    }

    public void SetupSlice(AbilitySO ability)
    {
        if (sliceIcon == null)
        {
            sliceIcon = GetComponent<Image>();
        }

        assignedAbility = ability;
        if (sliceIcon != null && ability != null)
        {
            if (ability.icon != null)
            {
                sliceIcon.sprite = ability.icon;
                if (cachedFallbackSprite == null) cachedFallbackSprite = ability.icon;
            }
            else if (sliceIcon.sprite == null && cachedFallbackSprite != null)
            {
                sliceIcon.sprite = cachedFallbackSprite;
            }
            sliceIcon.enabled = true;
            sliceIcon.color = GetAbilityTint(ability);
        }

        EnsureLabelExists();
        if (sliceLabel != null && ability != null)
        {
            string shortName = string.IsNullOrEmpty(ability.abilityName) ? ability.name : ability.abilityName;
            if (shortName.Length > 9) shortName = shortName.Substring(0, 9);
            sliceLabel.text = shortName;
            sliceLabel.transform.rotation = Quaternion.identity;
        }
    }

    private void EnsureLabelExists()
    {
        if (sliceLabel != null) return;
        sliceLabel = GetComponentInChildren<TextMeshProUGUI>();
        if (sliceLabel == null)
        {
            GameObject labelObj = new GameObject("SliceLabel", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelObj.transform.SetParent(transform, false);
            RectTransform rt = labelObj.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(-16f, -14f);
            rt.offsetMax = new Vector2(16f, -2f);

            sliceLabel = labelObj.GetComponent<TextMeshProUGUI>();
            sliceLabel.alignment = TextAlignmentOptions.Bottom;
            sliceLabel.fontSize = 10f;
            sliceLabel.color = Color.white;
            sliceLabel.outlineWidth = 0.25f;
            sliceLabel.outlineColor = Color.black;
            sliceLabel.raycastTarget = false;
        }
    }

    private Color GetAbilityTint(AbilitySO ability)
    {
        if (ability == null) return Color.white;
        if (ability.shieldAmount > 0 || ability.useDiceValueAsShield) return new Color(0.55f, 0.85f, 1f, 1f);
        if (ability.healAmount > 0 || ability.useDiceValueAsHeal) return new Color(0.55f, 1f, 0.65f, 1f);
        if (ability.statusEffects != null && ability.statusEffects.Count > 0) return new Color(0.85f, 0.55f, 1f, 1f);
        return new Color(1f, 0.75f, 0.55f, 1f);
    }
}