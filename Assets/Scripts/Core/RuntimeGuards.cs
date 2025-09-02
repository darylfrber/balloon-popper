using UnityEngine;
using UnityEngine.SceneManagement;

// Runtime guards to improve build robustness and avoid Unity Services popups.
// - Disables Analytics (if present) very early to prevent Unity Services init in builds.
// - Ensures our menu bootstrap is attempted on every scene load.
// - Ensures ShaderKeeper is initialized as early as possible to reduce shader stripping issues.
public static class RuntimeGuards
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
    private static void EarlyInit()
    {
        // Disable Analytics if the package/define exists (avoids services init warning/popup)
        TryDisableAnalytics();

        // Initialize ShaderKeeper even earlier as a precaution
        ShaderKeeper.Ensure();

        // Make sure when any scene loads, we bring up the main menu safely if nothing else is active
        SceneManager.sceneLoaded -= OnSceneLoaded; // avoid double subscribe
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Idempotent: Will only create MainMenu if no HUD/GameOver/Menu exist
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
