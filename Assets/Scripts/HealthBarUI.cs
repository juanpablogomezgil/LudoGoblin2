using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // Necesario para TextMeshPro

public class HealthBarUI : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private HealthSystem healthSystem;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI statusText;

    private void OnEnable()
    {
        if (healthSystem != null)
        {
            healthSystem.OnHealthChanged += UpdateUI;
            UpdateUI();
        }
    }

    private void Start()
    {
        AlignHealthTextWithSlider();
        UpdateUI();
    }

    private void AlignHealthTextWithSlider()
    {
        if (healthSlider != null && healthText != null)
        {
            RectTransform sliderRt = healthSlider.GetComponent<RectTransform>();
            RectTransform textRt = healthText.GetComponent<RectTransform>();
            if (sliderRt != null && textRt != null)
            {
                textRt.anchorMin = sliderRt.anchorMin;
                textRt.anchorMax = sliderRt.anchorMax;
                textRt.pivot = new Vector2(0.5f, 0.5f);
                textRt.anchoredPosition = sliderRt.anchoredPosition + new Vector2(0f, -34f);
                textRt.sizeDelta = new Vector2(360f, 44f);
            }
            healthText.alignment = TextAlignmentOptions.Center;
            healthText.fontSize = 22f;
            healthText.color = Color.white;
            healthText.outlineWidth = 0.25f;
            healthText.outlineColor = Color.black;
        }
    }

    private void OnDisable()
    {
        if (healthSystem != null)
        {
            healthSystem.OnHealthChanged -= UpdateUI;
        }
    }

    public void BindHealthSystem(HealthSystem newHealthSystem)
    {
        if (healthSystem != null)
        {
            healthSystem.OnHealthChanged -= UpdateUI;
        }

        healthSystem = newHealthSystem;

        if (healthSystem != null && isActiveAndEnabled)
        {
            healthSystem.OnHealthChanged += UpdateUI;
            UpdateUI();
        }
    }

    private void UpdateUI()
    {
        if (healthSystem == null) return;

        if (healthSlider != null)
        {
            healthSlider.maxValue = healthSystem.MaxHealth;
            healthSlider.value = healthSystem.CurrentHealth;
        }

        string statusSummary = BuildStatusSummary();

        if (healthText != null)
        {
            string baseHp = $"{healthSystem.CurrentHealth}/{healthSystem.MaxHealth}";
            if (healthSystem.CurrentShield > 0)
            {
                baseHp += $" <color=#66CCFF>[Esc:{healthSystem.CurrentShield}]</color>";
            }

            if (statusText == null && !string.IsNullOrEmpty(statusSummary))
            {
                healthText.text = $"{baseHp} {statusSummary}";
            }
            else
            {
                healthText.text = baseHp;
            }
        }

        if (statusText != null)
        {
            statusText.text = statusSummary;
        }
    }

    private string BuildStatusSummary()
    {
        if (healthSystem == null || healthSystem.ActiveEffects.Count == 0) return string.Empty;

        StringBuilder sb = new StringBuilder();
        foreach (var kvp in healthSystem.ActiveEffects)
        {
            if (kvp.Value <= 0) continue;

            string label = kvp.Key switch
            {
                StatusEffectType.Poison => $"<color=#66FF66>Ven:{kvp.Value}</color>",
                StatusEffectType.Vulnerability => $"<color=#FF9933>Vuln:{kvp.Value}</color>",
                StatusEffectType.Weakness => $"<color=#CCCCCC>Deb:{kvp.Value}</color>",
                StatusEffectType.Stun => $"<color=#FFFF66>Aturd:{kvp.Value}</color>",
                _ => $"{kvp.Key}:{kvp.Value}"
            };

            if (sb.Length > 0) sb.Append(" ");
            sb.Append(label);
        }

        return sb.ToString();
    }
}