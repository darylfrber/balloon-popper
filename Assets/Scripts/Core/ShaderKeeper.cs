using System.Collections.Generic;
using UnityEngine;

// Keeps references to key shaders/materials alive to reduce risk of shader stripping in builds.
// This is a lightweight runtime guard; for absolute certainty, add shaders to ProjectSettings -> Graphics -> Always Included Shaders.
public class ShaderKeeper : MonoBehaviour
{
    private static ShaderKeeper _instance;

    // Keep materials so the referenced shaders are included and warmed.
    private readonly List<Material> _kept = new List<Material>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoEnsure()
    {
        Ensure();
    }

    public static void Ensure()
    {
        if (_instance != null) return;
        var go = new GameObject("ShaderKeeper")
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        DontDestroyOnLoad(go);
        _instance = go.AddComponent<ShaderKeeper>();
        _instance.Build();
    }

    private void Build()
    {
        // Try to reference commonly used shaders so they are linked into the build.
        TryKeep("Universal Render Pipeline/Unlit");
        TryKeep("Unlit/Color");
        TryKeep("Universal Render Pipeline/Lit");
        TryKeep("Standard");
    }

    private void TryKeep(string shaderName)
    {
        var sh = Shader.Find(shaderName);
        if (sh == null) return;
        var mat = new Material(sh);
        // Assign a color property if available (to avoid being stripped as unused variants)
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", new Color(0.5f, 0.5f, 0.5f, 1f));
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", new Color(0.5f, 0.5f, 0.5f, 1f));
        _kept.Add(mat);
    }
}
