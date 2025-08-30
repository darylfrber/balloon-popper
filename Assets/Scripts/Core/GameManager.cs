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
    private int highScore = 0;
    private const string HighScoreKey = "HighScore";
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

        // Load persisted high score (single-number leaderboard)
        highScore = PlayerPrefs.GetInt(HighScoreKey, 0);
    }

    // Methode om de score toe te voegen
    public void AddScore(int baseAmount)
    {
        if (isGameOver) return;
        int amount = doublePointsActive ? baseAmount * 2 : baseAmount;
        score += amount;
        
        // Update high score if beaten and persist immediately
        if (score > highScore)
        {
            highScore = score;
            PlayerPrefs.SetInt(HighScoreKey, highScore);
            PlayerPrefs.Save();
        }
        
        Debug.Log($"Score updated: {score} (High: {highScore})");
    }

    // Eigenschap om de huidige score op te halen
    public int Score => score;

    // Hoogste score (leaderboard: slechts 1 getal)
    public int HighScore => highScore;

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

        // As a safeguard, update and persist high score at end
        if (score > highScore)
        {
            highScore = score;
            PlayerPrefs.SetInt(HighScoreKey, highScore);
            PlayerPrefs.Save();
        }

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

        // High score text
        var highGO = new GameObject("HighScore");
        highGO.transform.SetParent(canvasGO.transform, false);
        var hst = highGO.AddComponent<Text>();
        hst.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        hst.text = $"High Score: {highScore}";
        hst.fontSize = 28;
        hst.alignment = TextAnchor.MiddleCenter;
        hst.color = Color.white;
        var hrt = highGO.GetComponent<RectTransform>();
        hrt.anchorMin = new Vector2(0.5f, 0.5f);
        hrt.anchorMax = new Vector2(0.5f, 0.5f);
        hrt.pivot = new Vector2(0.5f, 0.5f);
        hrt.anchoredPosition = new Vector2(0f, 0f);
        hrt.sizeDelta = new Vector2(600f, 50f);

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
        MakeButton("QuitButton", "Quit", new Vector2(0f, -100f), QuitGame);
    }

    private void RestartCurrentScene()
    {
        // Reset state and start fresh gameplay without full scene reload
        // 1) Reset internal state
        isGameOver = false;
        doublePointsActive = false;
        doublePointsTimer = 0f;
        score = 0;

        // 2) Cleanup Game Over UI if present
        var goCanvas = GameObject.Find("GameOverCanvas");
        if (goCanvas != null)
        {
            try { Destroy(goCanvas); } catch { }
        }

        // 3) Stop and remove spawners
        var spawners = FindObjectsOfType<BalloonSpawner>();
        foreach (var sp in spawners)
        {
            try { sp.Stop(); } catch { }
            try { Destroy(sp.gameObject); } catch { }
        }

        // 4) Destroy existing balloons
        var balloons = FindObjectsOfType<Balloon>();
        foreach (var b in balloons)
        {
            if (b != null) try { Destroy(b.gameObject); } catch { }
        }

        // 5) Remove old HUD (will be recreated)
        var hud = GameObject.Find("HUD");
        if (hud != null)
        {
            try { Destroy(hud); } catch { }
        }

        // 6) Re-enable click handler
        var cam = Camera.main;
        if (cam != null)
        {
            var click = cam.GetComponent<ClickToPop>();
            if (click != null) click.enabled = true;
        }

        // 7) Start a fresh gameplay session
        PrototypeBootstrapper.StartGameplay();
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        // Stop play mode in the editor
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
