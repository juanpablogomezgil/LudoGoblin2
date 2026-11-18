using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ConsumableBeltUI : MonoBehaviour
{
    public TurnManager turnManager;
    public DiceManager diceManager;

    private RectTransform beltContainer;
    private List<Button> itemButtons = new List<Button>();
    private List<TextMeshProUGUI> itemLabels = new List<TextMeshProUGUI>();

    public void Initialize(TurnManager tm, DiceManager dm)
    {
        turnManager = tm;
        diceManager = dm;
        BuildBeltUI();
        RefreshUI();
    }

    private void BuildBeltUI()
    {
        if (beltContainer != null) return;

        Canvas mainCanvas = FindFirstObjectByType<Canvas>();
        if (mainCanvas == null || GameManager.Instance == null) return;

        GameObject panelObj = new GameObject("ConsumableDiceBelt", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(HorizontalLayoutGroup));
        panelObj.transform.SetParent(mainCanvas.transform, false);

        beltContainer = panelObj.GetComponent<RectTransform>();
        beltContainer.anchorMin = new Vector2(0.5f, 0.5f);
        beltContainer.anchorMax = new Vector2(0.5f, 0.5f);
        beltContainer.pivot = new Vector2(0.5f, 0.5f);
        beltContainer.anchoredPosition = new Vector2(-150f, -350f);
        beltContainer.sizeDelta = new Vector2(740f, 70f);

        Image panelBg = panelObj.GetComponent<Image>();
        panelBg.color = new Color(0.12f, 0.1f, 0.16f, 0.88f);

        HorizontalLayoutGroup hlg = panelObj.GetComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(8, 8, 6, 6);
        hlg.spacing = 6f;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;

        var catalog = GameManager.Instance.consumableDiceCatalog;
        for (int i = 0; i < catalog.Count; i++)
        {
            int index = i;
            ConsumableDiceEntry entry = catalog[i];

            GameObject btnObj = new GameObject($"Btn_{entry.id}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(beltContainer, false);

            Image btnImg = btnObj.GetComponent<Image>();
            btnImg.color = GetColorForBonus(entry.bonusType);

            Button btn = btnObj.GetComponent<Button>();
            btn.onClick.AddListener(() => OnConsumableClicked(index));

            GameObject txtObj = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            txtObj.transform.SetParent(btnObj.transform, false);
            RectTransform txtRt = txtObj.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = new Vector2(2f, 2f);
            txtRt.offsetMax = new Vector2(-2f, -2f);

            TextMeshProUGUI tmp = txtObj.GetComponent<TextMeshProUGUI>();
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 11f;
            tmp.color = Color.black;
            tmp.raycastTarget = false;

            itemButtons.Add(btn);
            itemLabels.Add(tmp);
        }
    }

    private Color GetColorForBonus(SpecialDiceBonusType bonus)
    {
        return bonus switch
        {
            SpecialDiceBonusType.IronArmor => new Color(0.65f, 0.85f, 1f, 1f),
            SpecialDiceBonusType.HealBasic => new Color(0.65f, 0.95f, 0.7f, 1f),
            SpecialDiceBonusType.HealSuperiorCleanse => new Color(0.45f, 1f, 0.82f, 1f),
            SpecialDiceBonusType.PoisonEnemy => new Color(0.82f, 0.62f, 1f, 1f),
            _ => Color.white
        };
    }

    private void OnConsumableClicked(int index)
    {
        if (turnManager == null || turnManager.state != BattleState.PlayerTurn) return;
        if (diceManager == null || GameManager.Instance == null) return;

        var catalog = GameManager.Instance.consumableDiceCatalog;
        if (index < 0 || index >= catalog.Count) return;

        ConsumableDiceEntry entry = catalog[index];
        if (entry.count <= 0) return;

        entry.count--;
        AudioManager.Instance?.PlaySpecialDiceUseSound();

        string shortTag = entry.sides > 0 ? $"d{entry.sides}" : $"Fijo {entry.fixedValue}";
        if (entry.sides > 0)
        {
            diceManager.SpawnDice(entry.sides, entry.bonusType, shortTag);
        }
        else
        {
            diceManager.SpawnFixedValueDice(entry.fixedValue, entry.bonusType, shortTag);
        }

        RefreshUI();
    }

    public void RefreshUI()
    {
        if (GameManager.Instance == null) return;
        var catalog = GameManager.Instance.consumableDiceCatalog;
        bool isPlayerTurn = turnManager != null && turnManager.state == BattleState.PlayerTurn;

        for (int i = 0; i < itemButtons.Count && i < catalog.Count; i++)
        {
            ConsumableDiceEntry entry = catalog[i];
            itemButtons[i].interactable = isPlayerTurn && entry.count > 0;
            itemLabels[i].text = $"<b>{entry.displayName}</b>\n(x{entry.count})";
        }
    }
}
