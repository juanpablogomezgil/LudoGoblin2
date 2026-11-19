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
        if (ability != null)
        {
            // Asigna el icono temático pixel-art representativo de la habilidad/arma
            Sprite weaponIcon = AbilityIconFactory.GetIconForAbility(ability);
            ability.icon = weaponIcon;

            if (sliceIcon != null)
            {
                sliceIcon.sprite = weaponIcon;
                sliceIcon.type = Image.Type.Simple;
                sliceIcon.preserveAspect = true;
                sliceIcon.color = Color.white;
                sliceIcon.enabled = true;
            }
        }

        EnsureLabelExists();
        if (sliceLabel != null && ability != null)
        {
            string shortName = string.IsNullOrEmpty(ability.abilityName) ? ability.name : ability.abilityName;
            if (shortName.Length > 8) shortName = shortName.Substring(0, 8);
            sliceLabel.text = $"<b>{shortName}</b>";
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
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -2f);
            rt.sizeDelta = new Vector2(66f, 18f);

            sliceLabel = labelObj.GetComponent<TextMeshProUGUI>();
            sliceLabel.alignment = TextAlignmentOptions.Center;
            sliceLabel.fontSize = 9.5f;
            sliceLabel.color = Color.white;
            sliceLabel.outlineWidth = 0.35f;
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