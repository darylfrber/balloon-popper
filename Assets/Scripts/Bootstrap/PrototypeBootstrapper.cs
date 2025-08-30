using UnityEngine;
using UnityEngine.UI;

public static class PrototypeBootstrapper
{
    private static bool hasRun;
    private static GameObject mainMenuCanvas;

    // Creates a material compatible with URP/HDRP/Built-in where possible and applies the given color.
    private static Material CreateLitMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }
        var mat = new Material(shader);
        // Try both common color properties to support URP Lit (_BaseColor) and Built-in (_Color)
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
        // Also set Material.color to be safe
        try { mat.color = color; } catch {}
        return mat;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (hasRun) return;
        hasRun = true;

        // 1) Ensure Camera
        var mainCam = Camera.main;
        if (mainCam == null)
        {
            var camGO = new GameObject("Main Camera");
            mainCam = camGO.AddComponent<Camera>();
            camGO.tag = "MainCamera";
            camGO.transform.position = new Vector3(0, 1.5f, -10f);
            camGO.AddComponent<AudioListener>();
        }
        // Force perspective settings for existing or new camera
        mainCam.orthographic = false;
        mainCam.fieldOfView = 60f;
        // Ensure click handler exists for popping balloons via raycast
        if (mainCam.GetComponent<ClickToPop>() == null)
        {
            mainCam.gameObject.AddComponent<ClickToPop>();
        }

        // 2) Ensure Light
        if (Object.FindObjectOfType<Light>() == null)
        {
            var lightGO = new GameObject("Directional Light");
            var light = lightGO.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1f;
            lightGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        // 3) Ensure GameManager
        if (GameManager.Instance == null)
        {
            var gm = new GameObject("GameManager");
            gm.AddComponent<GameManager>();
        }

        // 4) Create simple 3D environment for a menu backdrop
        CreateSimple3DEnvironment();

        // 5) Build Main Menu UI (3D themed backdrop + UI)
        CreateMainMenu();
    }

    private static void CreateMainMenu()
    {
        // Ensure EventSystem exists for UI interaction
        if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
#if ENABLE_INPUT_SYSTEM
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
#endif
        }

        mainMenuCanvas = new GameObject("MainMenuCanvas");
        var canvas = mainMenuCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        mainMenuCanvas.AddComponent<CanvasScaler>();
        mainMenuCanvas.AddComponent<GraphicRaycaster>();

        // Title
        var titleGO = new GameObject("Title");
        titleGO.transform.SetParent(mainMenuCanvas.transform, false);
        var titleText = titleGO.AddComponent<Text>();
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.text = "Balloon Popper";
        titleText.fontSize = 44;
        titleText.alignment = TextAnchor.UpperCenter;
        titleText.color = Color.white;
        var trt = titleGO.GetComponent<RectTransform>();
        trt.anchorMin = new Vector2(0.5f, 1f);
        trt.anchorMax = new Vector2(0.5f, 1f);
        trt.pivot = new Vector2(0.5f, 1f);
        trt.anchoredPosition = new Vector2(0f, -40f);
        trt.sizeDelta = new Vector2(800f, 80f);

        // Helper to create a button
        GameObject MakeButton(string name, string label, Vector2 anchoredPos, UnityEngine.Events.UnityAction onClick)
        {
            var btnGO = new GameObject(name);
            btnGO.transform.SetParent(mainMenuCanvas.transform, false);
            var img = btnGO.AddComponent<Image>();
            img.color = new Color(0.15f, 0.15f, 0.18f, 0.85f);
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

        MakeButton("StartButton", "Start", new Vector2(0f, -40f), StartGameplay);
        MakeButton("QuitButton", "Quit", new Vector2(0f, -120f), Application.Quit);
    }

    public static void StartGameplay()
    {
        // Remove menu
        if (mainMenuCanvas != null)
        {
            Object.Destroy(mainMenuCanvas);
            mainMenuCanvas = null;
        }
        // Create a runtime Balloon prefab (as a template GameObject)
        GameObject balloonPrefab = CreateBalloonPrefab();

        // Create Spawner and start it
        var spawnerGO = new GameObject("BalloonSpawner");
        var spawner = spawnerGO.AddComponent<BalloonSpawner>();
        spawner.balloonPrefab = balloonPrefab;

        // Create a simple runtime LevelConfig instance to control spawn rate
        var levelCfg = ScriptableObject.CreateInstance<LevelConfig>();
        levelCfg.levelDuration = 60f;
        levelCfg.spawnInterval = 0.8f; // faster spawning for fun
        spawner.Begin(levelCfg);

        // Create HUD
        CreateHUD();
    }

    private static GameObject CreateBalloonPrefab()
    {
        // Create a 3D sphere balloon
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "BalloonPrefab";
        var mr = go.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            var baseColor = new Color(1f, 0.3f, 0.3f);
            mr.material = CreateLitMaterial(baseColor);
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            mr.receiveShadows = true;
        }

        // Slightly squash to look like a balloon
        go.transform.localScale = new Vector3(0.8f, 1.1f, 0.8f);

        // Add a small knot (sphere) and string (cylinder) as children for better silhouette
        var knot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        knot.name = "Knot";
        knot.transform.SetParent(go.transform, false);
        knot.transform.localScale = new Vector3(0.12f, 0.08f, 0.12f);
        knot.transform.localPosition = new Vector3(0f, -0.6f, 0f);
        var knotMr = knot.GetComponent<MeshRenderer>();
        if (knotMr != null)
        {
            var knotColor = (mr != null) ? mr.material.color : new Color(1f, 0.3f, 0.3f);
            knotMr.material = CreateLitMaterial(knotColor);
        }
        // Remove child collider so clicks go to parent sphere collider
        var knotCol = knot.GetComponent<Collider>();
        if (knotCol != null) Object.Destroy(knotCol);

        var stringObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        stringObj.name = "String";
        stringObj.transform.SetParent(go.transform, false);
        stringObj.transform.localScale = new Vector3(0.02f, 0.8f, 0.02f);
        stringObj.transform.localPosition = new Vector3(0f, -1.5f, 0f);
        var strMr = stringObj.GetComponent<MeshRenderer>();
        if (strMr != null)
        {
            strMr.material = CreateLitMaterial(new Color(0.1f,0.1f,0.1f));
        }
        var strCol = stringObj.GetComponent<Collider>();
        if (strCol != null) Object.Destroy(strCol);

        // Add gameplay behaviour but keep template inactive so it doesn't move/destroy itself
        var balloon = go.AddComponent<Balloon>();
        balloon.scoreValue = 10;
        balloon.riseSpeed = 1.5f;

        // Keep template inactive; clones will be activated after instantiation
        go.SetActive(false);

        return go;
    }

    private static void CreateHUD()
    {
        // Canvas
        var canvasGO = new GameObject("HUD");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        // Score Text
        var scoreGO = new GameObject("ScoreText");
        scoreGO.transform.SetParent(canvasGO.transform, false);
        var scoreText = scoreGO.AddComponent<Text>();
        scoreText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        scoreText.fontSize = 28;
        scoreText.alignment = TextAnchor.UpperLeft;
        scoreText.color = Color.white;
        var rt = scoreGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(10f, -10f);
        rt.sizeDelta = new Vector2(400f, 60f);

        // Instruction Text
        var instrGO = new GameObject("Instructions");
        instrGO.transform.SetParent(canvasGO.transform, false);
        var instrText = instrGO.AddComponent<Text>();
        instrText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        instrText.fontSize = 20;
        instrText.alignment = TextAnchor.UpperCenter;
        instrText.color = new Color(1f,1f,1f,0.9f);
        instrText.text = "Klik op de ballonnen om punten te scoren!";
        var irt = instrGO.GetComponent<RectTransform>();
        irt.anchorMin = new Vector2(0.5f, 1f);
        irt.anchorMax = new Vector2(0.5f, 1f);
        irt.pivot = new Vector2(0.5f, 1f);
        irt.anchoredPosition = new Vector2(0f, -10f);
        irt.sizeDelta = new Vector2(600f, 40f);

        // HUD Controller
        var hud = canvasGO.AddComponent<HUDController>();
        hud.scoreText = scoreText;
    }

    private static void CreateSimple3DEnvironment()
    {
        // Ground Plane
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.position = new Vector3(0f, -6f, 12f);
        ground.transform.localScale = new Vector3(5f, 1f, 5f);
        var gmr = ground.GetComponent<MeshRenderer>();
        if (gmr != null)
        {
            gmr.material = CreateLitMaterial(new Color(0.15f, 0.35f, 0.15f));
        }

        // Background Backdrop (fits camera view)
        // Remove old one if present
        var oldBack = GameObject.Find("Background");
        if (oldBack != null) Object.Destroy(oldBack);
        var oldQuad = GameObject.Find("BackdropQuad");
        if (oldQuad != null) Object.Destroy(oldQuad);

        var cam = Camera.main;
        var backdrop = GameObject.CreatePrimitive(PrimitiveType.Quad);
        backdrop.name = "BackdropQuad";
        if (backdrop.TryGetComponent<Collider>(out var backCol)) { Object.Destroy(backCol); }
        var quadMr = backdrop.GetComponent<MeshRenderer>();
        if (quadMr != null)
        {
            quadMr.material = CreateLitMaterial(new Color(0.5f, 0.75f, 1f));
        }
        var fitter = backdrop.AddComponent<BackgroundFitter>();
        fitter.targetCamera = cam;
        fitter.distanceFromCamera = 60f;
        if (cam != null)
        {
            backdrop.transform.SetParent(cam.transform, worldPositionStays: false);
        }
    }
}