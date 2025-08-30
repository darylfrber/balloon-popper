using UnityEngine;

// Simple 3D cloud system made of sphere clusters that drift across the sky.
// Designed to be lightweight and fully runtime-generated with safe materials.
public class CloudSystem : MonoBehaviour
{
    [Tooltip("Camera used to orient/reference positions. Defaults to Camera.main.")]
    public Camera targetCamera;

    [Tooltip("Number of cloud clusters to spawn.")]
    public int cloudCount = 10;

    [Tooltip("Horizontal range (X) from the camera center where clouds can exist.")]
    public float horizontalRange = 60f;

    [Tooltip("Min/Max height (Y) range for cloud centers.")]
    public Vector2 heightRange = new Vector2(6f, 14f);

    [Tooltip("Min/Max depth (Z) range in front of the camera for cloud centers.")]
    public Vector2 depthRange = new Vector2(25f, 70f);

    [Tooltip("Min/Max horizontal speed for drifting clouds.")]
    public Vector2 speedRange = new Vector2(0.4f, 1.2f);

    [Tooltip("Color of the clouds.")]
    public Color cloudColor = new Color(1f, 1f, 1f, 1f);

    [Tooltip("Seed for deterministic cloud generation (0 = random).")]
    public int seed = 0;

    private class Cloud
    {
        public Transform root;
        public float speed;
    }

    private Cloud[] clouds;

    private void Awake()
    {
        if (targetCamera == null) targetCamera = Camera.main;
    }

    private void Start()
    {
        BuildClouds();
    }

    private void Update()
    {
        if (clouds == null || clouds.Length == 0) return;
        float dt = Time.deltaTime;
        foreach (var c in clouds)
        {
            if (c == null || c.root == null) continue;
            var p = c.root.position;
            p.x += c.speed * dt;
            c.root.position = p;

            // Wrap-around when moving out of range to the right
            if (p.x > CenterX + horizontalRange)
            {
                float newX = CenterX - horizontalRange;
                float newY = Random.Range(heightRange.x, heightRange.y);
                float newZ = Random.Range(depthRange.x, depthRange.y);
                c.root.position = new Vector3(newX, newY, newZ);
            }
        }
    }

    private float CenterX => (targetCamera != null) ? targetCamera.transform.position.x : 0f;

    private void BuildClouds()
    {
        // Clean previous
        foreach (Transform child in transform)
        {
            if (Application.isPlaying) Destroy(child.gameObject);
            else DestroyImmediate(child.gameObject);
        }

        if (seed != 0) Random.InitState(seed);

        clouds = new Cloud[cloudCount];
        for (int i = 0; i < cloudCount; i++)
        {
            clouds[i] = CreateCloudCluster($"Cloud_{i}");
        }
    }

    private Cloud CreateCloudCluster(string name)
    {
        var rootGO = new GameObject(name);
        rootGO.transform.SetParent(transform, false);
        float x = Random.Range(-horizontalRange, horizontalRange);
        float y = Random.Range(heightRange.x, heightRange.y);
        float z = Random.Range(depthRange.x, depthRange.y);
        rootGO.transform.position = new Vector3(CenterX + x, y, z);

        // Build a blobby cloud from a few spheres
        int puffs = Random.Range(4, 8);
        for (int i = 0; i < puffs; i++)
        {
            var puff = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            puff.name = $"Puff_{i}";
            puff.transform.SetParent(rootGO.transform, false);

            // Random offset and scale to create a fluffy shape
            var offset = new Vector3(
                Random.Range(-2.2f, 2.2f),
                Random.Range(-0.6f, 0.8f),
                Random.Range(-0.8f, 0.8f)
            );
            puff.transform.localPosition = offset;
            float baseScale = Random.Range(1.2f, 2.8f);
            puff.transform.localScale = new Vector3(baseScale * Random.Range(0.8f, 1.2f),
                                                    baseScale * Random.Range(0.7f, 1.1f),
                                                    baseScale * Random.Range(0.8f, 1.3f));

            // Material and shadow settings
            var mr = puff.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.material = SafeMaterial(cloudColor);
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;
            }
            // Remove collider to avoid click blocking
            var col = puff.GetComponent<Collider>();
            if (col != null)
            {
                if (Application.isPlaying) Destroy(col);
                else DestroyImmediate(col);
            }
        }

        // Slight random overall rotation/scale to avoid repetition
        rootGO.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        float overall = Random.Range(0.9f, 1.3f);
        rootGO.transform.localScale = Vector3.one * overall;

        return new Cloud
        {
            root = rootGO.transform,
            speed = Random.Range(speedRange.x, speedRange.y) * (Random.value < 0.5f ? 1f : 0.6f)
        };
    }

    private static Material SafeMaterial(Color c)
    {
        // Keep in sync with PrototypeBootstrapper.CreateLitMaterial to avoid magenta in builds
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
