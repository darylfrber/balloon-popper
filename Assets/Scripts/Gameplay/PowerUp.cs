using UnityEngine;

// Simple clickable power-up that grants double points for a fixed duration.
public class PowerUp : MonoBehaviour
{
    [Tooltip("Lifetime in seconds before the power-up disappears if not collected.")]
    public float lifeTime = 3f;

    [Tooltip("Duration in seconds for which double points are active after pickup.")]
    public float doublePointsDuration = 15f;

    private float timer;

    private void Awake()
    {
        BuildVisualIfEmpty();
    }

    private void OnEnable()
    {
        timer = lifeTime;
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
        {
            Destroy(gameObject);
            return;
        }
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            Destroy(gameObject);
        }
        // Optional idle animation
        transform.Rotate(0f, 60f * Time.deltaTime, 0f, Space.World);
    }

    private void OnMouseDown()
    {
        // Allow direct click collection (independent of ClickToPop)
        if (GameManager.Instance != null && !GameManager.Instance.IsGameOver)
        {
            GameManager.Instance.ActivateDoublePoints(doublePointsDuration);
            SpawnActivationBurst();
        }
        Destroy(gameObject);
    }

    private void SpawnActivationBurst()
    {
        var go = new GameObject("PU_ActivationBurst");
        go.transform.position = transform.position;
        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main; main.duration = 0.7f; main.loop = false; main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.7f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 2.2f); main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.14f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f,0.9f,0.5f,1f), new Color(1f,0.65f,0.2f,0.8f));
        var emi = ps.emission; emi.rateOverTime = 0f; emi.SetBursts(new ParticleSystem.Burst[]{ new ParticleSystem.Burst(0f, 40, 60, 1, 0.01f)});
        var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = 0.25f;
        var clr = ps.colorOverLifetime; clr.enabled = true; var grad = new Gradient(); grad.SetKeys(
            new GradientColorKey[]{ new GradientColorKey(new Color(1f,0.9f,0.5f),0f), new GradientColorKey(new Color(1f,0.7f,0.2f),1f) },
            new GradientAlphaKey[]{ new GradientAlphaKey(1f,0f), new GradientAlphaKey(0f,1f) }
        ); clr.color = new ParticleSystem.MinMaxGradient(grad);
        var vel = ps.velocityOverLifetime; vel.enabled = true; vel.y = new ParticleSystem.MinMaxCurve(0.4f, 0.4f); vel.x = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f); vel.z = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f);
        var r = go.AddComponent<ParticleSystemRenderer>();
        var sh = Shader.Find("Particles/Additive"); if (sh == null) sh = Shader.Find("Particles/Standard Unlit"); if (sh == null) sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (sh != null) { var mat = new Material(sh); if (mat.HasProperty("_Color")) mat.SetColor("_Color", new Color(1f,0.85f,0.4f,1f)); r.material = mat; }
        ps.Play();
        GameObject.Destroy(go, 2f);
    }

    private void BuildVisualIfEmpty()
    {
        // If this object has no renderer, create a cooler coin-like badge with glow and sparkles
        bool hasRenderer = GetComponentInChildren<MeshRenderer>() != null;
        bool hasCollider = GetComponent<Collider>() != null && GetComponent<Collider>().enabled;
        if (!hasRenderer)
        {
            // Core coin disk
            var coin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            coin.name = "PowerUpCoin";
            coin.transform.SetParent(transform, false);
            coin.transform.localScale = new Vector3(0.7f, 0.08f, 0.7f);
            coin.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // face camera more often while rotating
            var coinMr = coin.GetComponent<MeshRenderer>();
            if (coinMr != null)
            {
                var gold = SafeMaterial(new Color(1f, 0.85f, 0.2f));
                EnableEmission(gold, new Color(1f, 0.7f, 0.15f) * 0.6f);
                coinMr.material = gold;
                coinMr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                coinMr.receiveShadows = false;
            }
            // Rim
            var rim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rim.name = "Rim";
            rim.transform.SetParent(coin.transform, false);
            rim.transform.localScale = new Vector3(1.02f, 0.52f, 1.02f);
            var rimMr = rim.GetComponent<MeshRenderer>();
            if (rimMr != null)
            {
                var darker = SafeMaterial(new Color(0.95f, 0.72f, 0.12f));
                EnableEmission(darker, new Color(1f, 0.6f, 0.1f) * 0.3f);
                rimMr.material = darker;
                rimMr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                rimMr.receiveShadows = false;
            }
            var rimCol = rim.GetComponent<Collider>();
            if (rimCol != null) { if (Application.isPlaying) Destroy(rimCol); else DestroyImmediate(rimCol); }

            // X2 text badge (3D TextMesh)
            var textGO = new GameObject("X2");
            textGO.transform.SetParent(coin.transform, false);
            textGO.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            var tm = textGO.AddComponent<TextMesh>();
            tm.text = "x2";
            tm.fontSize = 128;
            tm.characterSize = 0.05f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = new Color(1f, 1f, 1f, 1f);
            // Add a slight outline effect by duplicating
            var tmShadow = new GameObject("Shadow").AddComponent<TextMesh>();
            tmShadow.transform.SetParent(textGO.transform, false);
            tmShadow.transform.localPosition = new Vector3(0.01f, -0.01f, 0f);
            tmShadow.text = "x2";
            tmShadow.fontSize = 128;
            tmShadow.characterSize = 0.05f;
            tmShadow.anchor = TextAnchor.MiddleCenter;
            tmShadow.alignment = TextAlignment.Center;
            tmShadow.color = new Color(0f, 0f, 0f, 0.3f);

            // Small point light to fake glow
            var lightGO = new GameObject("PU_Light");
            lightGO.transform.SetParent(transform, false);
            lightGO.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            var l = lightGO.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(1f, 0.85f, 0.4f);
            l.intensity = 1.2f;
            l.range = 3.5f;
            l.shadows = LightShadows.None;

            // Idle sparkle particle system
            var psGO = new GameObject("PU_Sparkles");
            psGO.transform.SetParent(transform, false);
            psGO.transform.localPosition = Vector3.zero;
            var ps = psGO.AddComponent<ParticleSystem>();
            var main = ps.main; main.loop = true; main.startLifetime = 1.0f; main.startSpeed = 0.3f; main.startSize = 0.08f; main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f,0.9f,0.5f,0.9f), new Color(1f,0.7f,0.2f,0.0f));
            var emi = ps.emission; emi.rateOverTime = 6f;
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = 0.4f;
            var clr = ps.colorOverLifetime; clr.enabled = true; var grad = new Gradient(); grad.SetKeys(
                new GradientColorKey[]{ new GradientColorKey(new Color(1f,0.9f,0.5f),0f), new GradientColorKey(new Color(1f,0.7f,0.2f),1f) },
                new GradientAlphaKey[]{ new GradientAlphaKey(0.8f,0f), new GradientAlphaKey(0f,1f) }
            ); clr.color = new ParticleSystem.MinMaxGradient(grad);
            var vel = ps.velocityOverLifetime; vel.enabled = true; vel.y = new ParticleSystem.MinMaxCurve(0.15f, 0.15f); vel.x = new ParticleSystem.MinMaxCurve(-0.05f, 0.05f); vel.z = new ParticleSystem.MinMaxCurve(-0.05f, 0.05f);

            // Remove child colliders so clicks go to root
            var childCols = coin.GetComponentsInChildren<Collider>();
            foreach (var c in childCols)
            {
                if (c != null)
                {
                    if (Application.isPlaying) Destroy(c);
                    else DestroyImmediate(c);
                }
            }
        }
        if (!hasCollider)
        {
            var col = gameObject.AddComponent<SphereCollider>();
            ((SphereCollider)col).radius = 0.7f;
            col.isTrigger = false;
        }
    }

    private static void EnableEmission(Material m, Color emission)
    {
        if (m == null) return;
        try
        {
            m.EnableKeyword("_EMISSION");
            if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", emission);
            if (m.HasProperty("_EmissionColorLDR")) m.SetColor("_EmissionColorLDR", emission);
        }
        catch { }
    }

    private static Material SafeMaterial(Color c)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        var mat = new Material(shader);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
        try { mat.color = c; } catch { }
        return mat;
    }
}
