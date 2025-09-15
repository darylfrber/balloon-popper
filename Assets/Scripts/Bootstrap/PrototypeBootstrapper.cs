using UnityEngine;
using UnityEngine.UI;

public static class PrototypeBootstrapper
{
    private static bool hasRun;
    private static GameObject mainMenuCanvas;
    private static readonly Color BackgroundColor = new Color(0.45f, 0.7f, 1f, 1f);

    // Fallback guard to ensure the menu exists when a build starts, even if the first bootstrap is skipped
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void BootstrapMenuFallback()
    {
        EnsureMainMenuVisible();
    }

    // Safe, idempotent menu ensure (creates EventSystem and Menu only if no gameplay or game-over UI is found)
    public static void EnsureMainMenuVisible()
    {
        // If gameplay is active (HUD present) or GameOver UI is present, do nothing
        if (GameObject.Find("HUD") != null) return;
        if (GameObject.Find("GameOverCanvas") != null) return;
        // If main menu already exists, do nothing
        if (GameObject.Find("MainMenuCanvas") != null) return;
        // Create the menu now
        CreateMainMenu();
    }

    // Creates a material compatible with URP/HDRP/Built-in where possible and applies the given color.
    private static Material CreateLitMaterial(Color color)
    {
        // Choose shader based on active render pipeline to avoid magenta in Built-in
        var srp = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
        Shader shader = null;
        if (srp == null)
        {
            // Built-in pipeline
            shader = Shader.Find("Unlit/Color");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
        }
        else
        {
            // SRP (URP/HDRP)
            shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader == null) shader = Shader.Find("Standard");
        }
        var mat = new Material(shader);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
        try { mat.color = color; } catch {}
        return mat;
    }

    // Creates a glossy lit material for balloons to get nice specular highlights with safe fallbacks.
    private static Material CreateGlossyLitMaterial(Color color)
    {
        // Prefer Lit shaders for specular, but choose based on active render pipeline
        var srp = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
        Shader shader = null;
        if (srp == null)
        {
            // Built-in pipeline
            shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Legacy Shaders/Specular");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
        }
        else
        {
            // SRP (URP/HDRP)
            shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Legacy Shaders/Specular");
            if (shader == null) shader = Shader.Find("Unlit/Color");
        }
        var mat = new Material(shader);
        // Color
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
        try { mat.color = color; } catch {}
        // Glossy parameters
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.85f);
        if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.85f);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.02f);
        // Ensure specular highlights/environment reflections are enabled (URP/Standard)
        try
        {
            // Disable any OFF keywords so highlights are visible in builds
            mat.DisableKeyword("_SPECULARHIGHLIGHTS_OFF");
            mat.DisableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
            mat.DisableKeyword("_GLOSSYREFLECTIONS_OFF");
            if (mat.HasProperty("_SpecularHighlights")) mat.SetFloat("_SpecularHighlights", 1f);
            if (mat.HasProperty("_EnvironmentReflections")) mat.SetFloat("_EnvironmentReflections", 1f);
        }
        catch { }
        return mat;
    }

    private static void EnsureCameraSpecularLight(Camera cam)
    {
        if (cam == null) return;
        Transform t = cam.transform;
        var existing = t.Find("SpecularPointLight");
        if (existing != null) return;
        var lightGO = new GameObject("SpecularPointLight");
        lightGO.transform.SetParent(t, false);
        lightGO.transform.localPosition = new Vector3(0.5f, 0.8f, 0.5f);
        var l = lightGO.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = Color.white;
        l.intensity = 0.8f;
        l.range = 60f; // cover gameplay area in front of camera
        l.shadows = LightShadows.Soft;
    }

    // Adds a small realtime reflection probe near the camera to improve specular/environment reflections in builds.
    private static void EnsureCameraReflectionProbe(Camera cam)
    {
        if (cam == null) return;
        var t = cam.transform;
        var existing = t.Find("RuntimeReflectionProbe");
        if (existing != null) return;
        var go = new GameObject("RuntimeReflectionProbe");
        go.transform.SetParent(t, false);
        go.transform.localPosition = Vector3.zero;
        var probe = go.AddComponent<ReflectionProbe>();
        probe.mode = UnityEngine.Rendering.ReflectionProbeMode.Realtime;
        probe.refreshMode = UnityEngine.Rendering.ReflectionProbeRefreshMode.EveryFrame;
        probe.timeSlicingMode = UnityEngine.Rendering.ReflectionProbeTimeSlicingMode.IndividualFaces;
        probe.intensity = 0.6f;
        probe.boxProjection = true;
        probe.size = new Vector3(30f, 20f, 30f);
        probe.clearFlags = UnityEngine.Rendering.ReflectionProbeClearFlags.Skybox;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        // On first run, set up core systems and show the main menu. On every scene load, ensure backdrop exists.
        bool firstRunThisSession = !hasRun;
        if (!hasRun)
        {
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
            // Place/aim camera for a pleasant open-sky view
            mainCam.transform.position = new Vector3(0f, 2f, -10f);
            mainCam.transform.rotation = Quaternion.identity;
            // Sensible clipping planes
            mainCam.nearClipPlane = 0.1f;
            mainCam.farClipPlane = 200f;
            // Ensure a solid color background as a fallback in case materials/shaders fail in build
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = BackgroundColor;
            // Ensure click handler exists for popping balloons via raycast
            if (mainCam.GetComponent<ClickToPop>() == null)
            {
                mainCam.gameObject.AddComponent<ClickToPop>();
            }
            // Add subtle camera parallax for immersion
            if (mainCam.GetComponent<CameraParallax>() == null)
            {
                var par = mainCam.gameObject.AddComponent<CameraParallax>();
                par.maxOffsetX = 1.2f;
                par.maxOffsetY = 0.6f;
                par.lerpSpeed = 2.5f;
                par.swayAmplitude = 0.12f;
                par.swayFrequency = 0.2f;
            }
            // Ensure a small point light exists to create specular highlights on balloons
            EnsureCameraSpecularLight(mainCam);
            // Ensure a realtime reflection probe exists for improved specular/reflections in builds
            EnsureCameraReflectionProbe(mainCam);

            // 2) Ensure Light
            if (Object.FindAnyObjectByType<Light>() == null)
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

            // 0) Ensure ShaderKeeper to reduce shader stripping risk in builds
            ShaderKeeper.Ensure();
        }

        // Always ensure the simple 3D environment/backdrop exists for the current scene
        CreateSimple3DEnvironment();

        // Also enforce camera configuration every scene load
        var camNow = Camera.main;
        if (camNow != null)
        {
            camNow.orthographic = false;
            camNow.fieldOfView = 60f;
            camNow.nearClipPlane = 0.1f;
            camNow.farClipPlane = 200f;
            // Only override transform if it's still at origin (typical default scene state)
            if (camNow.transform.position == Vector3.zero)
            {
                camNow.transform.position = new Vector3(0f, 2f, -10f);
                camNow.transform.rotation = Quaternion.identity;
            }
            camNow.clearFlags = CameraClearFlags.SolidColor;
            camNow.backgroundColor = BackgroundColor;
            EnsureCameraSpecularLight(camNow);
            EnsureCameraReflectionProbe(camNow);
        }

        // Build Main Menu only on first run
        if (firstRunThisSession)
        {
            CreateMainMenu();
        }
    }

    private static void CreateMainMenu()
    {
        // Ensure EventSystem exists for UI interaction
        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
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
        var scaler = mainMenuCanvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        mainMenuCanvas.AddComponent<GraphicRaycaster>();

        // Background (full-screen)
        var bgGO = new GameObject("Background");
        bgGO.transform.SetParent(mainMenuCanvas.transform, false);
        var bgImg = bgGO.AddComponent<Image>();
        bgImg.color = BackgroundColor;
        bgImg.raycastTarget = false;
        var bgRt = bgGO.GetComponent<RectTransform>();
        bgRt.anchorMin = new Vector2(0f, 0f);
        bgRt.anchorMax = new Vector2(1f, 1f);
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;
        bgGO.transform.SetAsFirstSibling();

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

        // High Score (leaderboard: single highest value)
        var hsGO = new GameObject("HighScoreText");
        hsGO.transform.SetParent(mainMenuCanvas.transform, false);
        var hsText = hsGO.AddComponent<Text>();
        hsText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        int hsVal = (GameManager.Instance != null) ? GameManager.Instance.HighScore : PlayerPrefs.GetInt("HighScore", 0);
        hsText.text = $"High Score: {hsVal}";
        hsText.fontSize = 28;
        hsText.alignment = TextAnchor.UpperCenter;
        hsText.color = Color.white;
        var hsrt = hsGO.GetComponent<RectTransform>();
        hsrt.anchorMin = new Vector2(0.5f, 1f);
        hsrt.anchorMax = new Vector2(0.5f, 1f);
        hsrt.pivot = new Vector2(0.5f, 1f);
        hsrt.anchoredPosition = new Vector2(0f, -110f);
        hsrt.sizeDelta = new Vector2(800f, 40f);

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
        MakeButton("QuitButton", "Quit", new Vector2(0f, -120f), () => {
            if (GameManager.Instance != null) GameManager.Instance.QuitGame();
            else Application.Quit();
        });
    }

    public static void StartGameplay()
    {
        Debug.Log("[Bootstrap] StartGameplay called");
        // Ensure a clean game state (in case previous session ended with Game Over)
        if (GameManager.Instance != null)
        {
            GameManager.Instance.PrepareNewGame();
        }
        // Remove menu
        // Remove menu (destroy known reference and any stray instance by name)
        if (mainMenuCanvas != null)
        {
            Object.Destroy(mainMenuCanvas);
            mainMenuCanvas = null;
        }
        var existingMenu = GameObject.Find("MainMenuCanvas");
        if (existingMenu != null)
        {
            Object.Destroy(existingMenu);
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

        // Create PowerUp Spawner
        var puSpawnerGO = new GameObject("PowerUpSpawner");
        puSpawnerGO.AddComponent<PowerUpSpawner>();

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
            mr.material = CreateGlossyLitMaterial(baseColor);
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
            knotMr.material = CreateGlossyLitMaterial(knotColor);
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
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // Score Text (top-left)
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
        rt.sizeDelta = new Vector2(400f, 40f);

        // High Score Text (top-right)
        var highGO = new GameObject("HighScoreTopRight");
        highGO.transform.SetParent(canvasGO.transform, false);
        var highText = highGO.AddComponent<Text>();
        highText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        highText.fontSize = 24;
        highText.alignment = TextAnchor.UpperRight;
        highText.color = Color.white;
        var hrt = highGO.GetComponent<RectTransform>();
        hrt.anchorMin = new Vector2(1f, 1f);
        hrt.anchorMax = new Vector2(1f, 1f);
        hrt.pivot = new Vector2(1f, 1f);
        hrt.anchoredPosition = new Vector2(-10f, -10f);
        hrt.sizeDelta = new Vector2(380f, 30f);

        // Power-up status Text (below High Score on top-right)
        var puGO = new GameObject("PowerUpStatus");
        puGO.transform.SetParent(canvasGO.transform, false);
        var puText = puGO.AddComponent<Text>();
        puText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        puText.fontSize = 22;
        puText.alignment = TextAnchor.UpperRight;
        puText.color = new Color(1f, 1f, 0.6f, 1f);
        var purt = puGO.GetComponent<RectTransform>();
        purt.anchorMin = new Vector2(1f, 1f);
        purt.anchorMax = new Vector2(1f, 1f);
        purt.pivot = new Vector2(1f, 1f);
        purt.anchoredPosition = new Vector2(-10f, -44f);
        purt.sizeDelta = new Vector2(380f, 28f);

        // HUD Controller
        var hud = canvasGO.AddComponent<HUDController>();
        hud.scoreText = scoreText;
        hud.powerUpText = puText;
        hud.highScoreText = highText;
    }

    public static void CreateSimple3DEnvironment()
    {
        // If environment already exists, skip to avoid duplicates
        if (GameObject.Find("Ground") != null && GameObject.Find("BackdropQuad") != null)
        {
            return;
        }
        // Ground Plane
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.position = new Vector3(0f, -6f, 12f);
        // Make the ground (platform) wider and deeper so it overlaps the mountains and fills the edges
        ground.transform.localScale = new Vector3(22f, 1f, 20f);
        var gmr = ground.GetComponent<MeshRenderer>();
        if (gmr != null)
        {
            gmr.material = CreateLitMaterial(new Color(0.15f, 0.35f, 0.15f));
        }

        // Populate ground with simple 3D decor (bushes and rocks)
        var oldDecor = GameObject.Find("GroundDecor");
        if (oldDecor != null) Object.Destroy(oldDecor);
        var decorRoot = new GameObject("GroundDecor");
        // Ground extents (Unity Plane is 10x10 scaled by localScale.x/z)
        float halfSize = 5f * ground.transform.localScale.x; // = 25 when scale 5
        float halfSizeZ = 5f * ground.transform.localScale.z;
        // Scatter improved bushes (denser dome-shaped, varied greens)
        int bushCount = 20;
        for (int i = 0; i < bushCount; i++)
        {
            var bush = new GameObject($"Bush_{i}");
            bush.transform.SetParent(decorRoot.transform, false);
            // Random position within ground bounds, avoid center area
            float x = Random.Range(-halfSize + 2f, halfSize - 2f);
            float z = ground.transform.position.z + Random.Range(-halfSizeZ + 2f, halfSizeZ - 2f);
            if (Mathf.Abs(x) < 3.5f) x = Mathf.Sign(x) * 3.5f;
            bush.transform.position = new Vector3(x, -5.8f, z);
            int puffs = Random.Range(4, 7);
            // base hue variation
            Color baseGreen = Color.Lerp(new Color(0.10f, 0.45f, 0.16f), new Color(0.16f, 0.60f, 0.22f), Random.Range(0f, 1f));
            for (int p = 0; p < puffs; p++)
            {
                var puff = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                puff.transform.SetParent(bush.transform, false);
                puff.transform.localPosition = new Vector3(
                    Random.Range(-0.35f, 0.35f),
                    Random.Range(0.0f, 0.28f) - 0.02f,
                    Random.Range(-0.25f, 0.25f)
                );
                float s = Random.Range(0.38f, 0.75f);
                // squash vertically a bit for dome look
                puff.transform.localScale = new Vector3(s, s * Random.Range(0.7f, 0.95f), s);
                var pmr = puff.GetComponent<MeshRenderer>();
                if (pmr != null)
                {
                    // small per-puff variation
                    Color c = Color.Lerp(baseGreen, new Color(baseGreen.r*0.9f, baseGreen.g*1.05f, baseGreen.b*0.9f), Random.Range(0f, 1f));
                    pmr.material = CreateLitMaterial(c);
                    pmr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    pmr.receiveShadows = false;
                }
                var pcol = puff.GetComponent<Collider>();
                if (pcol != null) { if (Application.isPlaying) Object.Destroy(pcol); else Object.DestroyImmediate(pcol); }
            }
        }
        
        // Scatter some rocks
        int rockCount = 10;
        for (int i = 0; i < rockCount; i++)
        {
            var rock = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            rock.name = $"Rock_{i}";
            rock.transform.SetParent(decorRoot.transform, false);
            float x = Random.Range(-halfSize + 2f, halfSize - 2f);
            float z = ground.transform.position.z + Random.Range(-halfSizeZ + 2f, halfSizeZ - 2f);
            if (Mathf.Abs(x) < 4.5f) x = Mathf.Sign(x) * 4.5f;
            rock.transform.position = new Vector3(x, -5.9f, z);
            rock.transform.rotation = Quaternion.Euler(Random.Range(0f, 12f), Random.Range(0f, 360f), Random.Range(0f, 12f));
            rock.transform.localScale = new Vector3(Random.Range(0.28f, 0.6f), Random.Range(0.22f, 0.5f), Random.Range(0.28f, 0.6f));
            var rmr = rock.GetComponent<MeshRenderer>();
            if (rmr != null)
            {
                rmr.material = CreateLitMaterial(new Color(0.40f, 0.40f, 0.43f));
                rmr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                rmr.receiveShadows = false;
            }
            var rcol = rock.GetComponent<Collider>();
            if (rcol != null) { if (Application.isPlaying) Object.Destroy(rcol); else Object.DestroyImmediate(rcol); }
        }

        // Trees (stems + crowns)
        int treeCount = 900; // much denser forest look per request
        for (int i = 0; i < treeCount; i++)
        {
            var tree = new GameObject($"Tree_{i}");
            tree.transform.SetParent(decorRoot.transform, false);

            // Bias placement to left/right/back bands to keep the central gameplay clear but make edges very dense
            // Choose a band: 0 = left, 1 = right, 2 = back
            int band = Random.Range(0, 3);
            float x;
            float z;
            if (band == 0)
            {
                // Left band
                x = Random.Range(-halfSize + 3f, -10f);
                z = ground.transform.position.z + Random.Range(-halfSizeZ + 4f, halfSizeZ - 4f);
            }
            else if (band == 1)
            {
                // Right band
                x = Random.Range(10f, halfSize - 3f);
                z = ground.transform.position.z + Random.Range(-halfSizeZ + 4f, halfSizeZ - 4f);
            }
            else
            {
                // Back band (far z strip)
                x = Random.Range(-halfSize + 4f, halfSize - 4f);
                float zEdge = Random.value < 0.5f ? (ground.transform.position.z + halfSizeZ - 5f) : (ground.transform.position.z - halfSizeZ + 5f);
                z = zEdge + Random.Range(-2.5f, 2.5f);
            }

            // Extra safeguard to keep direct center area open
            if (Mathf.Abs(x) < 8f)
            {
                x = Mathf.Sign(Random.value - 0.5f) * Random.Range(8f, halfSize - 4f);
            }

            tree.transform.position = new Vector3(x, -6f, z);

            float trunkH = Random.Range(0.8f, 1.4f); // lower trunks
            var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Trunk";
            trunk.transform.SetParent(tree.transform, false);
            trunk.transform.localScale = new Vector3(Random.Range(0.16f, 0.26f), trunkH, Random.Range(0.16f, 0.26f));
            trunk.transform.localPosition = new Vector3(0f, trunkH, 0f);
            var tmr = trunk.GetComponent<MeshRenderer>();
            if (tmr != null)
            {
                tmr.material = CreateLitMaterial(new Color(0.35f, 0.22f, 0.12f)); // brown
                tmr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                tmr.receiveShadows = false;
            }
            var tcol = trunk.GetComponent<Collider>();
            if (tcol != null) { if (Application.isPlaying) Object.Destroy(tcol); else Object.DestroyImmediate(tcol); }

            // Crown: 4–6 overlapping spheres, wider canopy, tighter vertical spacing to feel dense
            int blobs = Random.Range(4, 7);
            float baseY = trunkH * 2f - 0.2f;
            float maxRadius = Random.Range(1.6f, 2.2f);
            for (int b = 0; b < blobs; b++)
            {
                var crown = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                crown.name = $"Crown_{b}";
                crown.transform.SetParent(tree.transform, false);
                float t = (float)b / Mathf.Max(1, blobs - 1);
                float radius = Mathf.Lerp(maxRadius, maxRadius * 0.55f, t) * Random.Range(0.92f, 1.08f);
                float y = baseY + b * Mathf.Lerp(0.14f, 0.24f, Random.Range(0f,1f));
                float xOff = Random.Range(-0.28f, 0.28f) * (1f - t * 0.35f);
                float zOff = Random.Range(-0.22f, 0.22f) * (1f - t * 0.35f);
                crown.transform.localPosition = new Vector3(xOff, y, zOff);
                crown.transform.localScale = new Vector3(radius, radius * Random.Range(0.85f, 0.98f), radius);
                crown.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                var cmr = crown.GetComponent<MeshRenderer>();
                if (cmr != null)
                {
                    Color green = Color.Lerp(new Color(0.10f, 0.50f, 0.16f), new Color(0.06f, 0.38f, 0.12f), Random.Range(0f,1f));
                    cmr.material = CreateLitMaterial(green);
                    cmr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    cmr.receiveShadows = false;
                }
                var ccol = crown.GetComponent<Collider>();
                if (ccol != null) { if (Application.isPlaying) Object.Destroy(ccol); else Object.DestroyImmediate(ccol); }
            }
        }

        // Grass tufts (small cylinders)
        int grassCount = 60;
        for (int i = 0; i < grassCount; i++)
        {
            var tuft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tuft.name = $"Grass_{i}";
            tuft.transform.SetParent(decorRoot.transform, false);
            float x = Random.Range(-halfSize + 2f, halfSize - 2f);
            float z = ground.transform.position.z + Random.Range(-halfSizeZ + 2f, halfSizeZ - 2f);
            if (Mathf.Abs(x) < 2.5f) x = Mathf.Sign(x) * 2.5f;
            tuft.transform.position = new Vector3(x, -5.9f, z);
            tuft.transform.localScale = new Vector3(Random.Range(0.05f, 0.08f), Random.Range(0.12f, 0.2f), Random.Range(0.05f, 0.08f));
            var gmrend = tuft.GetComponent<MeshRenderer>();
            if (gmrend != null)
            {
                gmrend.material = CreateLitMaterial(Color.Lerp(new Color(0.16f,0.58f,0.2f), new Color(0.10f,0.46f,0.16f), Random.Range(0f,1f)));
                gmrend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                gmrend.receiveShadows = false;
            }
            var gcol = tuft.GetComponent<Collider>();
            if (gcol != null) { if (Application.isPlaying) Object.Destroy(gcol); else Object.DestroyImmediate(gcol); }
        }

        // Flowers (stem + small colored sphere)
        int flowerCount = 14;
        for (int i = 0; i < flowerCount; i++)
        {
            var stem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stem.name = $"FlowerStem_{i}";
            stem.transform.SetParent(decorRoot.transform, false);
            float x = Random.Range(-halfSize + 2f, halfSize - 2f);
            float z = ground.transform.position.z + Random.Range(-halfSizeZ + 2f, halfSizeZ - 2f);
            if (Mathf.Abs(x) < 5f) x = Mathf.Sign(x) * 5f; // keep further from middle
            stem.transform.position = new Vector3(x, -5.85f, z);
            stem.transform.localScale = new Vector3(0.03f, Random.Range(0.18f, 0.28f), 0.03f);
            var stemMr = stem.GetComponent<MeshRenderer>();
            if (stemMr != null)
            {
                stemMr.material = CreateLitMaterial(new Color(0.12f,0.6f,0.2f));
                stemMr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                stemMr.receiveShadows = false;
            }
            var stemCol = stem.GetComponent<Collider>();
            if (stemCol != null) { if (Application.isPlaying) Object.Destroy(stemCol); else Object.DestroyImmediate(stemCol); }

            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = $"FlowerHead_{i}";
            head.transform.SetParent(stem.transform, false);
            head.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            head.transform.localScale = Vector3.one * Random.Range(0.08f, 0.12f);
            var headMr = head.GetComponent<MeshRenderer>();
            if (headMr != null)
            {
                Color hc = Random.ColorHSV(0f, 1f, 0.7f, 1f, 0.8f, 1f);
                headMr.material = CreateLitMaterial(hc);
                headMr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                headMr.receiveShadows = false;
            }
            var headCol = head.GetComponent<Collider>();
            if (headCol != null) { if (Application.isPlaying) Object.Destroy(headCol); else Object.DestroyImmediate(headCol); }
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
            quadMr.material = CreateLitMaterial(BackgroundColor);
        }
        var fitter = backdrop.AddComponent<BackgroundFitter>();
        fitter.targetCamera = cam;
        // Place the backdrop behind the end of the ground (in camera forward direction) so hills/mountains can sit at the ground's far edge
        float desiredBackdropDist = 60f;
        if (cam != null)
        {
            float farEdgeZ = ground.transform.position.z + halfSize; // end of ground in camera forward (+Z)
            desiredBackdropDist = Mathf.Max(60f, (farEdgeZ - cam.transform.position.z) + 10f); // add small margin
        }
        fitter.distanceFromCamera = desiredBackdropDist;
        if (cam != null)
        {
            backdrop.transform.SetParent(cam.transform, worldPositionStays: false);
        }

        // Clouds (3D) — remove old and create new per scene
        var oldClouds = GameObject.Find("Clouds");
        if (oldClouds != null) Object.Destroy(oldClouds);
        var cloudsGO = new GameObject("Clouds");
        var clouds = cloudsGO.AddComponent<CloudSystem>();
        clouds.targetCamera = cam;
        // Slightly tune defaults for this game
        clouds.cloudCount = 12;
        clouds.heightRange = new Vector2(6f, 16f);
        clouds.depthRange = new Vector2(25f, 45f);
        clouds.horizontalRange = 60f;

        // Atmospheric fog for depth
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogColor = new Color(0.42f, 0.66f, 0.95f, 1f);
        RenderSettings.fogDensity = 0.007f;

        // Tune main directional light if present
        var sceneLight = Object.FindAnyObjectByType<Light>();
        if (sceneLight != null)
        {
            sceneLight.color = new Color(1.0f, 0.95f, 0.86f, 1f); // warm sun
            sceneLight.intensity = 1.15f;
            sceneLight.shadows = LightShadows.Soft;
            sceneLight.shadowStrength = 0.8f;
        }

        // Distant 3D Hills layer using procedural curved meshes (always IN FRONT of the backdrop)
        var oldHills = GameObject.Find("Hills");
        if (oldHills != null) Object.Destroy(oldHills);
        var hillsRoot = new GameObject("Hills");
        Debug.Log("[Env] Creating Hills root");

        float backdropWorldZ = (cam != null)
            ? cam.transform.position.z + (fitter != null ? fitter.distanceFromCamera : 60f)
            : 50f;
        float backdropDistForLogs = (fitter != null ? fitter.distanceFromCamera : 60f);
        Debug.Log($"[Env] Backdrop distance: {backdropDistForLogs}");

        // Create 3 layers, nearest has more saturation
        // Bring hills closer to camera so they are clearly visible while still behind gameplay
        float backDist = (fitter != null ? fitter.distanceFromCamera : 60f);
        float nearHillDist = Mathf.Clamp(backDist - 60f, 32f, 50f); // target ~35–50 units in front of camera
        for (int i = 0; i < 3; i++)
        {
            var layer = new GameObject($"HillsLayer_{i}");
            layer.transform.SetParent(hillsRoot.transform, false);
            var hm = layer.AddComponent<HillsMesh>();
            hm.targetCamera = cam;
            hm.distanceFromCamera = nearHillDist + i * 7f; // e.g., 35, 42, 49 (if backDist is big)
            // Raise and enlarge hills for clearer visibility
            hm.baseY = 1.2f + i * 0.6f;
            hm.amplitude = 5.5f + i * 0.8f;
            hm.waves = 2 + i;
            hm.segments = 96;
            hm.thickness = 2f;
            hm.color = new Color(0.20f - i * 0.04f, 0.50f - i * 0.06f, 0.70f - i * 0.06f, 1f);
            hm.scrollSpeed = 0.08f - i * 0.02f; // near faster, far slower
            Debug.Log($"[Env] HillsLayer {i}: dist={hm.distanceFromCamera}, baseY={hm.baseY}, amp={hm.amplitude}");
        }

        // Mountains behind hills (closer to backdrop), layered and with more contrast so they are clearly visible
        var oldMounts = GameObject.Find("Mountains");
        if (oldMounts != null) Object.Destroy(oldMounts);
        var mountainsRoot = new GameObject("Mountains");
        Debug.Log("[Env] Creating Mountains root");
        for (int j = 0; j < 2; j++)
        {
            var mgo = new GameObject($"MountainsLayer_{j}");
            mgo.transform.SetParent(mountainsRoot.transform, false);
            var mm = mgo.AddComponent<MountainsMesh>();
            mm.targetCamera = cam;
            float backdropDist = (fitter != null ? fitter.distanceFromCamera : 60f);
            // Place mountains right at the end of the platform (ground) and ensure they remain in front of the backdrop
            float farEdgeZ = ground.transform.position.z + halfSize;
            float desiredMountDist = Mathf.Clamp((farEdgeZ - cam.transform.position.z) + 2f, 10f, backdropDist - 4f); // just beyond platform edge, visible
            mm.distanceFromCamera = Mathf.Min(backdropDist - 4f, desiredMountDist + j * 3f);
            // Raise mountains further and increase height for clear visibility
            mm.baseY = 2.0f + j * 0.6f;
            mm.amplitude = 12.0f + j * 2.5f;
            mm.peaks = 6 + j;
            mm.segments = 128;
            mm.thickness = 2f;
            // Darker/desaturated for strongest contrast vs sky and fog
            mm.color = new Color(0.10f - j * 0.02f, 0.34f - j * 0.05f, 0.54f - j * 0.06f, 1f);
            mm.scrollSpeed = 0.03f - j * 0.01f; // far mountains scroll slowest
            Debug.Log($"[Env] MountainsLayer {j}: dist={mm.distanceFromCamera}, baseY={mm.baseY}, amp={mm.amplitude}");
        }
    }
}