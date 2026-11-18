using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class BuildScreenController : MonoBehaviour
{
    private Canvas buildCanvas;
    private TextMeshProUGUI headerStatsText;
    private TextMeshProUGUI segmentCountLabel;
    private Transform segmentsListContainer;
    private Transform shopListContainer;

    private void Start()
    {
        EnsureEventSystem();
        if (GameManager.Instance != null)
        {
            GameManager.Instance.InitializeCatalogsIfNeeded();
            GameManager.Instance.SaveRun();
        }
        BuildUI();
        RefreshAll();
    }

    private void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
        }
    }

    private void BuildUI()
    {
        buildCanvas = FindFirstObjectByType<Canvas>();
        if (buildCanvas == null)
        {
            GameObject canvasObj = new GameObject("BuildCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            buildCanvas = canvasObj.GetComponent<Canvas>();
            buildCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
        }

        // Fondo general
        GameObject bgObj = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bgObj.transform.SetParent(buildCanvas.transform, false);
        RectTransform bgRt = bgObj.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;
        bgObj.GetComponent<Image>().color = new Color(0.11f, 0.09f, 0.16f, 1f);

        // Cabecera
        headerStatsText = CreateText(bgObj.transform, "PANTALLA DE BUILD Y TIENDA", new Vector2(0f, 470f), new Vector2(1600f, 70f), 32f, new Color(1f, 0.86f, 0.3f, 1f));

        // Columna Izquierda: Editor de Segmentos de la Ruleta (Variable 4 a 10, recomendado 6 a 8)
        GameObject leftPanel = CreatePanel(bgObj.transform, "RouletteBuildPanel", new Vector2(-430f, 10f), new Vector2(880f, 760f), new Color(0.16f, 0.14f, 0.22f, 0.96f));
        CreateText(leftPanel.transform, "<b>CONFIGURACIÓN DE LA RULETA DE HABILIDADES</b>\n<size=75%>Haz clic en cualquier segmento para rotar la habilidad equipada</size>",
            new Vector2(0f, 330f), new Vector2(820f, 65f), 22f, Color.white);

        // Selector de número de segmentos variable
        segmentCountLabel = CreateText(leftPanel.transform, "Segmentos activos: 8", new Vector2(0f, 265f), new Vector2(340f, 45f), 22f, new Color(0.6f, 0.9f, 1f, 1f));
        CreateButton(leftPanel.transform, "- Segmento", new Vector2(-240f, 265f), new Vector2(150f, 42f), new Color(0.55f, 0.3f, 0.3f, 1f), () => ChangeSegmentCount(-1));
        CreateButton(leftPanel.transform, "+ Segmento", new Vector2(240f, 265f), new Vector2(150f, 42f), new Color(0.3f, 0.55f, 0.35f, 1f), () => ChangeSegmentCount(1));

        GameObject segContainerObj = new GameObject("SegmentsList", typeof(RectTransform), typeof(VerticalLayoutGroup));
        segContainerObj.transform.SetParent(leftPanel.transform, false);
        RectTransform segRt = segContainerObj.GetComponent<RectTransform>();
        segRt.anchoredPosition = new Vector2(0f, -45f);
        segRt.sizeDelta = new Vector2(820f, 520f);

        VerticalLayoutGroup vlgLeft = segContainerObj.GetComponent<VerticalLayoutGroup>();
        vlgLeft.spacing = 8f;
        vlgLeft.padding = new RectOffset(10, 10, 6, 6);
        vlgLeft.childControlWidth = true;
        vlgLeft.childControlHeight = true;
        vlgLeft.childForceExpandWidth = true;
        vlgLeft.childForceExpandHeight = true;
        segmentsListContainer = segContainerObj.transform;

        // Columna Derecha: Tienda de Dados Consumibles y Curación de Posada
        GameObject rightPanel = CreatePanel(bgObj.transform, "ShopPanel", new Vector2(450f, 10f), new Vector2(820f, 760f), new Color(0.16f, 0.14f, 0.22f, 0.96f));
        CreateText(rightPanel.transform, "<b>TIENDA DEL CASINO (DADOS ESPECIALES)</b>\n<size=75%>Compra dados consumibles para invocarlos en momentos críticos</size>",
            new Vector2(0f, 330f), new Vector2(780f, 65f), 22f, Color.white);

        GameObject shopContainerObj = new GameObject("ShopList", typeof(RectTransform), typeof(VerticalLayoutGroup));
        shopContainerObj.transform.SetParent(rightPanel.transform, false);
        RectTransform shopRt = shopContainerObj.GetComponent<RectTransform>();
        shopRt.anchoredPosition = new Vector2(0f, -20f);
        shopRt.sizeDelta = new Vector2(760f, 580f);

        VerticalLayoutGroup vlgRight = shopContainerObj.GetComponent<VerticalLayoutGroup>();
        vlgRight.spacing = 10f;
        vlgRight.padding = new RectOffset(10, 10, 6, 6);
        vlgRight.childControlWidth = true;
        vlgRight.childControlHeight = true;
        vlgRight.childForceExpandWidth = true;
        vlgRight.childForceExpandHeight = true;
        shopListContainer = shopContainerObj.transform;

        // Botones Inferiores: Curar en el Casino / Iniciar Combate / Menú Principal
        CreateButton(bgObj.transform, "Restaurar +25 HP (15 Oro)", new Vector2(-420f, -450f), new Vector2(340f, 62f), new Color(0.25f, 0.58f, 0.45f, 1f), () =>
        {
            if (GameManager.Instance != null && GameManager.Instance.gold >= 15 && GameManager.Instance.playerCurrentHealth < GameManager.Instance.playerMaxHealth)
            {
                GameManager.Instance.gold -= 15;
                GameManager.Instance.playerCurrentHealth = Mathf.Min(GameManager.Instance.playerMaxHealth, GameManager.Instance.playerCurrentHealth + 25);
                GameManager.Instance.SaveRun();
                RefreshAll();
            }
        });

        CreateButton(bgObj.transform, "¡INICIAR COMBATE! (Entrar a la Sala)", new Vector2(60f, -450f), new Vector2(460f, 66f), new Color(0.22f, 0.72f, 0.32f, 1f), () =>
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SaveRun();
                GameManager.Instance.LoadSceneSafe(GameManager.CombatSceneName, GameManager.CombatSceneName);
            }
        });

        CreateButton(bgObj.transform, "Guardar y Salir al Menú", new Vector2(560f, -450f), new Vector2(320f, 62f), new Color(0.45f, 0.35f, 0.55f, 1f), () =>
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SaveRun();
                GameManager.Instance.LoadSceneSafe(GameManager.MainMenuSceneName, GameManager.CombatSceneName);
            }
        });
    }

    private void ChangeSegmentCount(int delta)
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.rouletteSegmentCount = Mathf.Clamp(GameManager.Instance.rouletteSegmentCount + delta, 4, 10);
        GameManager.Instance.SyncEquippedRouletteSize();
        GameManager.Instance.SaveRun();
        RefreshAll();
    }

    private void CycleSegmentAbility(int segmentIndex)
    {
        if (GameManager.Instance == null) return;
        var catalog = GameManager.Instance.allAbilitiesCatalog;
        var equipped = GameManager.Instance.equippedRouletteAbilities;
        if (catalog.Count == 0 || segmentIndex < 0 || segmentIndex >= equipped.Count) return;

        AbilitySO current = equipped[segmentIndex];
        int currentIdx = catalog.IndexOf(current);
        int nextIdx = (currentIdx + 1) % catalog.Count;
        equipped[segmentIndex] = catalog[nextIdx];

        GameManager.Instance.SaveRun();
        RefreshAll();
    }

    private void BuyConsumable(int index)
    {
        if (GameManager.Instance == null) return;
        var shop = GameManager.Instance.consumableDiceCatalog;
        if (index < 0 || index >= shop.Count) return;

        ConsumableDiceEntry item = shop[index];
        if (GameManager.Instance.gold < item.price) return;

        GameManager.Instance.gold -= item.price;
        item.count++;
        GameManager.Instance.SaveRun();
        RefreshAll();
    }

    private void RefreshAll()
    {
        if (GameManager.Instance == null) return;
        GameManager gm = GameManager.Instance;

        string nextEnemy = gm.currentRoom switch
        {
            1 => "Esbirro Goblin",
            2 => "Guardia del Casino",
            3 => "Chamán Goblin",
            _ => "REY GOBLIN BRUJO (JEFE FINAL)"
        };

        if (headerStatsText != null)
        {
            headerStatsText.text =
                $"<b>ANTESALA DE BUILD — PRÓXIMA SALA {gm.currentRoom}/{gm.maxRooms}: {nextEnemy}</b>\n" +
                $"<size=75%><color=#66FF66>HP Kael: {gm.playerCurrentHealth}/{gm.playerMaxHealth}</color>   |   <color=#FFD700>Oro disponible: {gm.gold}</color></size>";
        }

        if (segmentCountLabel != null)
        {
            segmentCountLabel.text = $"Segmentos en Ruleta: <b>{gm.rouletteSegmentCount}</b>";
        }

        // Reconstruir lista de segmentos de la ruleta
        foreach (Transform child in segmentsListContainer) Destroy(child.gameObject);
        for (int i = 0; i < gm.equippedRouletteAbilities.Count; i++)
        {
            int segIndex = i;
            AbilitySO ab = gm.equippedRouletteAbilities[i];
            string abName = ab != null ? ab.abilityName : "Vacío";
            string abDesc = ab != null ? $"{ab.GetRequirementSummary()} — {ab.description}" : "";

            CreateButton(segmentsListContainer,
                $"Segmento #{i + 1}: <color=#FFE066>{abName}</color>  <size=78%>({abDesc})</size>  [Cambiar]",
                Vector2.zero, new Vector2(780f, 50f), new Color(0.24f, 0.22f, 0.34f, 1f),
                () => CycleSegmentAbility(segIndex));
        }

        // Reconstruir tienda de dados consumibles
        foreach (Transform child in shopListContainer) Destroy(child.gameObject);
        for (int i = 0; i < gm.consumableDiceCatalog.Count; i++)
        {
            int itemIndex = i;
            ConsumableDiceEntry entry = gm.consumableDiceCatalog[i];
            bool canAfford = gm.gold >= entry.price;

            Button b = CreateButton(shopListContainer,
                $"<color=#88EEFF>{entry.displayName}</color> (Tienes: x{entry.count}) — <color=#FFD700>{entry.price} Oro</color>\n<size=75%>{entry.description}</size>",
                Vector2.zero, new Vector2(740f, 75f),
                canAfford ? new Color(0.22f, 0.36f, 0.48f, 1f) : new Color(0.25f, 0.22f, 0.25f, 0.7f),
                () => BuyConsumable(itemIndex));
            b.interactable = canAfford;
        }
    }

    private GameObject CreatePanel(Transform parent, string name, Vector2 pos, Vector2 size, Color color)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(parent, false);
        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        panel.GetComponent<Image>().color = color;
        return panel;
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
        GameObject btnObj = new GameObject("Button", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(parent, false);
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        btnObj.GetComponent<Image>().color = bgColor;
        Button btn = btnObj.GetComponent<Button>();
        btn.onClick.AddListener(onClick);

        CreateText(btnObj.transform, label, Vector2.zero, size, 17f, Color.white);
        return btn;
    }
}
