using UnityEngine;
using UnityEngine.SceneManagement;

// Runtime guards to improve build robustness and avoid Unity Services popups.
// - Disables Analytics (if present) very early to prevent Unity Services init in builds.
// - Ensures our menu bootstrap is attempted on every scene load.
// - Ensures ShaderKeeper is initialized as early as possible to reduce shader stripping issues.
// - Forces minimal visual quality flags so balloons keep their lighting in builds.
public static class RuntimeGuards
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
    private static void EarlyInit()
    {
        // Disable Analytics if the package/define exists (avoids services init warning/popup)
        TryDisableAnalytics();

        // Initialize ShaderKeeper even earlier as a precaution
        ShaderKeeper.Ensure();

        // Enforce a few graphics quality switches very early to keep lighting quality in builds
        ForceVisualQuality();

        // Make sure when any scene loads, we bring up the main menu safely if nothing else is active
        SceneManager.sceneLoaded -= OnSceneLoaded; // avoid double subscribe
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // Enable reflection probes and shadows so glossy balloons look correct in builds
    private static void ForceVisualQuality()
    {
        // These are safe toggles across pipelines; wrap in try/catch to be robust
        try
        {
            // Allow realtime reflection probes globally
            QualitySettings.realtimeReflectionProbes = true;
            // Ensure shadows are enabled
            QualitySettings.shadows = ShadowQuality.All;
            // Allow at least a couple of per-pixel lights to contribute specular
            if (QualitySettings.pixelLightCount < 2) QualitySettings.pixelLightCount = 2;
            // Prefer anisotropic textures for nicer highlights on glancing angles
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
            
            // In URP, make sure specular/environment reflections keywords aren’t globally disabled
            Shader.DisableKeyword("_SPECULARHIGHLIGHTS_OFF");
            Shader.DisableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
            Shader.DisableKeyword("_GLOSSYREFLECTIONS_OFF");
        }
        catch { /* ignore on older Unity or different pipelines */ }
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Ensure our simple 3D environment exists (idempotent) and then ensure main menu
        PrototypeBootstrapper.CreateSimple3DEnvironment();
        PrototypeBootstrapper.EnsureMainMenuVisible();
    }

    private static void TryDisableAnalytics()
    {
        // UNITY_ANALYTICS symbol is defined when Analytics package is present.
        // Wrap in try/catch to be extra safe across package versions.
        try
        {
#if UNITY_ANALYTICS
            // Legacy Analytics API
            UnityEngine.Analytics.Analytics.enabled = false;
            UnityEngine.Analytics.Analytics.deviceStatsEnabled = false;
#endif
        }
        catch { /* ignore if API surface differs */ }
    }
}
