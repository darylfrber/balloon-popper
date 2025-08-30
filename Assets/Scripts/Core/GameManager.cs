// 8/30/2025 AI-Tag
// This was created with the help of Assistant, a Unity Artificial Intelligence product.

using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private int score = 0;
    private bool doublePointsActive = false;
    private float doublePointsTimer = 0f;

    private bool isGameOver = false;
    public bool IsGameOver => isGameOver;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Methode om de score toe te voegen
    public void AddScore(int baseAmount)
    {
        if (isGameOver) return;
        int amount = doublePointsActive ? baseAmount * 2 : baseAmount;
        score += amount;
        Debug.Log($"Score updated: {score}");
    }

    // Eigenschap om de huidige score op te halen
    public int Score => score;

    // Methode om dubbele punten te activeren
    public void ActivateDoublePoints(float duration)
    {
        if (isGameOver) return;
        doublePointsActive = true;
        doublePointsTimer = duration;
        Debug.Log("Double points activated!");
    }

    private void Update()
    {
        // Timer voor dubbele punten
        if (!isGameOver && doublePointsActive)
        {
            doublePointsTimer -= Time.deltaTime;
            if (doublePointsTimer <= 0)
            {
                doublePointsActive = false;
                Debug.Log("Double points deactivated.");
            }
        }
    }

    // Methode om het spel te beëindigen
    public void EndGame()
    {
        if (isGameOver) return;
        isGameOver = true;
        Debug.Log("Game Over!");

        // Stop all spawners
        var spawners = FindObjectsOfType<BalloonSpawner>();
        foreach (var sp in spawners)
        {
            try { sp.Stop(); } catch { }
        }

        // Disable click handler to prevent further pops
        var cam = Camera.main;
        if (cam != null)
        {
            var click = cam.GetComponent<ClickToPop>();
            if (click != null) click.enabled = false;
        }

        ShowGameOverUI();
    }

    private void ShowGameOverUI()
    {
        // Ensure EventSystem exists for UI interaction in build
        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
#if ENABLE_INPUT_SYSTEM
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
#endif
        }

        var canvasGO = new GameObject("GameOverCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // Panel background
        var panelGO = new GameObject("Panel");
        panelGO.transform.SetParent(canvasGO.transform, false);
        var panelImg = panelGO.AddComponent<Image>();
        panelImg.color = new Color(0f, 0f, 0f, 0.6f);
        var prt = panelGO.GetComponent<RectTransform>();
        prt.anchorMin = new Vector2(0f, 0f);
        prt.anchorMax = new Vector2(1f, 1f);
        prt.offsetMin = Vector2.zero;
        prt.offsetMax = Vector2.zero;

        // Title text
        var titleGO = new GameObject("Title");
        titleGO.transform.SetParent(canvasGO.transform, false);
        var title = titleGO.AddComponent<Text>();
        title.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        title.text = "Game Over";
        title.fontSize = 48;
        title.alignment = TextAnchor.UpperCenter;
        title.color = Color.white;
        var trt = titleGO.GetComponent<RectTransform>();
        trt.anchorMin = new Vector2(0.5f, 1f);
        trt.anchorMax = new Vector2(0.5f, 1f);
        trt.pivot = new Vector2(0.5f, 1f);
        trt.anchoredPosition = new Vector2(0f, -60f);
        trt.sizeDelta = new Vector2(800f, 80f);

        // Score text
        var scoreGO = new GameObject("Score");
        scoreGO.transform.SetParent(canvasGO.transform, false);
        var st = scoreGO.AddComponent<Text>();
        st.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        st.text = $"Score: {score}";
        st.fontSize = 32;
        st.alignment = TextAnchor.MiddleCenter;
        st.color = Color.white;
        var srt = scoreGO.GetComponent<RectTransform>();
        srt.anchorMin = new Vector2(0.5f, 0.5f);
        srt.anchorMax = new Vector2(0.5f, 0.5f);
        srt.pivot = new Vector2(0.5f, 0.5f);
        srt.anchoredPosition = new Vector2(0f, 40f);
        srt.sizeDelta = new Vector2(600f, 60f);

        // Buttons
        GameObject MakeButton(string name, string label, Vector2 anchoredPos, UnityEngine.Events.UnityAction onClick)
        {
            var btnGO = new GameObject(name);
            btnGO.transform.SetParent(canvasGO.transform, false);
            var img = btnGO.AddComponent<Image>();
            img.color = new Color(0.15f, 0.15f, 0.18f, 0.9f);
            var btn = btnGO.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(onClick);
            var rt = btnGO.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(240f, 60f);
            rt.anchoredPosition = anchoredPos;

            var txtGO = new GameObject("Text");
            txtGO.transform.SetParent(btnGO.transform, false);
            var txt = txtGO.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.text = label;
            txt.fontSize = 28;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            var txtrt = txtGO.GetComponent<RectTransform>();
            txtrt.anchorMin = new Vector2(0f, 0f);
            txtrt.anchorMax = new Vector2(1f, 1f);
            txtrt.offsetMin = Vector2.zero;
            txtrt.offsetMax = Vector2.zero;

            return btnGO;
        }

        MakeButton("RestartButton", "Restart", new Vector2(0f, -20f), RestartCurrentScene);
        MakeButton("QuitButton", "Quit", new Vector2(0f, -100f), Application.Quit);
    }

    private void RestartCurrentScene()
    {
        // Reset state and reload scene to start fresh
        isGameOver = false;
        doublePointsActive = false;
        doublePointsTimer = 0f;
        score = 0;
        var scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.name);
    }
}
