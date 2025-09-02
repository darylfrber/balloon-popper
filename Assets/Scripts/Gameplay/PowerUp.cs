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
        }
        Destroy(gameObject);
    }

    private void BuildVisualIfEmpty()
    {
        // If this object has no renderer, create a capsule visual as child
        bool hasRenderer = GetComponentInChildren<MeshRenderer>() != null;
        bool hasCollider = GetComponent<Collider>() != null && GetComponent<Collider>().enabled;
        if (!hasRenderer)
        {
            var vis = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            vis.name = "PowerUpCapsule";
            vis.transform.SetParent(transform, false);
            vis.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);
            var mr = vis.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.material = SafeMaterial(new Color(1f, 0.9f, 0.1f)); // golden yellow
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;
            }
            // Keep a collider on the root only
            var childCol = vis.GetComponent<Collider>();
            if (childCol != null)
            {
                if (Application.isPlaying) Destroy(childCol);
                else DestroyImmediate(childCol);
            }
        }
        if (!hasCollider)
        {
            var col = gameObject.AddComponent<SphereCollider>();
            ((SphereCollider)col).radius = 0.6f;
            col.isTrigger = false;
        }
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
