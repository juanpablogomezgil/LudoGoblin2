using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;

public class PauseMenuController : MonoBehaviour
{
    private GameObject pausePanel;
    private bool isPaused = false;

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        EnsurePauseUIExists();
        if (pausePanel == null) return;

        isPaused = !isPaused;
        pausePanel.SetActive(isPaused);
        Time.timeScale = isPaused ? 0f : 1f;
    }

    private void OnDestroy()
    {
        if (isPaused)
        {
            Time.timeScale = 1f;
        }
    }

    private void EnsurePauseUIExists()
    {
        if (pausePanel != null) return;

        Canvas mainCanvas = FindFirstObjectByType<Canvas>();
        if (mainCanvas == null) return;

        pausePanel = new GameObject("PauseMenuOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        pausePanel.transform.SetParent(mainCanvas.transform, false);

        RectTransform rt = pausePanel.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image overlayBg = pausePanel.GetComponent<Image>();
        overlayBg.color = new Color(0f, 0f, 0f, 0.78f);

        GameObject box = new GameObject("PauseBox", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        box.transform.SetParent(pausePanel.transform, false);
        RectTransform boxRt = box.GetComponent<RectTransform>();
        boxRt.sizeDelta = new Vector2(400f, 280f);
        box.GetComponent<Image>().color = new Color(0.14f, 0.12f, 0.18f, 0.98f);

        CreateLabel(box.transform, "PAUSA — LUDOGOBLIN", new Vector2(0f, 90f), 24f);

        CreateButton(box.transform, "Reanudar (Esc)", new Vector2(0f, 25f), new Color(0.25f, 0.65f, 0.35f, 1f), () =>
        {
            TogglePause();
        });

        CreateButton(box.transform, "Ir a Pantalla de Build", new Vector2(0f, -35f), new Color(0.3f, 0.45f, 0.75f, 1f), () =>
        {
            Time.timeScale = 1f;
            isPaused = false;
            if (GameManager.Instance != null)
            {
                GameManager.Instance.LoadSceneSafe(GameManager.BuildSceneName, SceneManager.GetActiveScene().name);
            }
        });

        CreateButton(box.transform, "Menú Principal", new Vector2(0f, -95f), new Color(0.7f, 0.25f, 0.25f, 1f), () =>
        {
            Time.timeScale = 1f;
            isPaused = false;
            if (GameManager.Instance != null)
            {
                GameManager.Instance.LoadSceneSafe(GameManager.MainMenuSceneName, SceneManager.GetActiveScene().name);
            }
        });

        pausePanel.SetActive(false);
    }

    private void CreateLabel(Transform parent, string text, Vector2 anchoredPos, float fontSize)
    {
        GameObject obj = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(360f, 45f);
        rt.anchoredPosition = anchoredPos;

        TextMeshProUGUI tmp = obj.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = fontSize;
        tmp.color = new Color(1f, 0.88f, 0.35f, 1f);
    }

    private void CreateButton(Transform parent, string label, Vector2 anchoredPos, Color bgColor, UnityEngine.Events.UnityAction onClick)
    {
        GameObject btnObj = new GameObject($"Btn_{label}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(parent, false);
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(260f, 46f);
        rt.anchoredPosition = anchoredPos;

        btnObj.GetComponent<Image>().color = bgColor;
        btnObj.GetComponent<Button>().onClick.AddListener(onClick);

        GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        txtObj.transform.SetParent(btnObj.transform, false);
        RectTransform txtRt = txtObj.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = Vector2.zero;
        txtRt.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = txtObj.GetComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 18f;
        tmp.color = Color.white;
    }
}
