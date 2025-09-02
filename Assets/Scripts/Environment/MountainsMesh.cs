using System.Collections.Generic;
using UnityEngine;

// Generates a spiky mountain silhouette strip to sit behind hills and in front of the backdrop.
// Designed for runtime generation with safe shader fallbacks and no colliders.
[ExecuteAlways]
public class MountainsMesh : MonoBehaviour
{
    [Tooltip("Camera reference for sizing/placement.")]
    public Camera targetCamera;

    [Tooltip("Distance from camera along forward direction (must be < backdrop distance).")]
    public float distanceFromCamera = 58f;

    [Tooltip("World width of the strip (0 = auto fit to camera width at distance).")]
    public float width = 0f;

    [Tooltip("Base Y position of the mountain base.")]
    public float baseY = -3.6f;

    [Tooltip("Peak amplitude for the mountain heights.")]
    public float amplitude = 6.5f;

    [Tooltip("Number of main peaks across the width.")]
    public int peaks = 6;

    [Tooltip("Horizontal subdivisions (more = smoother).")]
    public int segments = 128;

    [Tooltip("Mesh thickness (local Z scale).")]
    public float thickness = 2f;

    [Tooltip("Main color of the mountains.")]
    public Color color = new Color(0.22f, 0.48f, 0.68f, 1f);

    [Tooltip("Horizontal scroll speed for subtle parallax animation (units per second in wave space).")]
    public float scrollSpeed = 0.0f;

    private float _offset;

    private MeshFilter mf;
    private MeshRenderer mr;

    private void OnEnable()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        EnsureComponents();
        RemoveColliderIfAny();
        Build();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!isActiveAndEnabled) return;
        segments = Mathf.Max(16, segments);
        peaks = Mathf.Max(1, peaks);
        if (!Application.isPlaying) Build();
    }
#endif

    private void LateUpdate()
    {
        // Advance horizontal phase for subtle scrolling
        if (Application.isPlaying && Mathf.Abs(scrollSpeed) > 0.0001f)
        {
            _offset += scrollSpeed * Time.deltaTime;
        }
        Build();
    }

    private void EnsureComponents()
    {
        if (mf == null) mf = gameObject.GetComponent<MeshFilter>();
        if (mf == null) mf = gameObject.AddComponent<MeshFilter>();
        if (mr == null) mr = gameObject.GetComponent<MeshRenderer>();
        if (mr == null) mr = gameObject.AddComponent<MeshRenderer>();
        if (mr.sharedMaterial == null)
        {
            mr.sharedMaterial = SafeMaterial(color);
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
        }
    }

    private void Build()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        if (targetCamera == null) return;

        // Position and billboard towards camera
        Vector3 camPos = targetCamera.transform.position;
        Vector3 camFwd = targetCamera.transform.forward;
        transform.position = camPos + camFwd * distanceFromCamera;
        transform.rotation = Quaternion.LookRotation(-camFwd, Vector3.up);

        float fovRad = targetCamera.fieldOfView * Mathf.Deg2Rad;
        float heightAtDist = 2f * Mathf.Tan(fovRad * 0.5f) * distanceFromCamera;
        float widthAtDist = heightAtDist * targetCamera.aspect;
        float w = (width <= 0.001f) ? widthAtDist * 1.12f : width; // slight margin

        int vertsPerCol = 2;
        int cols = segments + 1;
        var verts = new List<Vector3>(cols * vertsPerCol);
        var uvs = new List<Vector2>(cols * vertsPerCol);
        var tris = new List<int>(segments * 6);

        float half = w * 0.5f;
        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments; // 0..1 across
            float x = Mathf.Lerp(-half, half, t);

            // Spiky mountain profile: mixed smooth spline-like and sharper terms with subtle horizontal phase scrolling
            float phaseT = t + _offset * 0.03f;
            float main = Mathf.Abs(Mathf.Sin(phaseT * Mathf.PI * peaks)); // 0..1 spikes
            float detail = Mathf.PerlinNoise(phaseT * peaks * 1.5f, 0.123f) * 0.4f + Mathf.Sin(phaseT * Mathf.PI * peaks * 2f) * 0.15f;
            float yTop = baseY + (main + detail) * amplitude + 1.0f;

            // top
            verts.Add(new Vector3(x, yTop, 0));
            uvs.Add(new Vector2(t, 1));
            // bottom (extend downwards to fill)
            verts.Add(new Vector3(x, baseY - (amplitude * 0.6f + 4f), 0));
            uvs.Add(new Vector2(t, 0));
        }

        for (int i = 0; i < segments; i++)
        {
            int i0 = i * 2;
            int i1 = i0 + 1;
            int i2 = i0 + 2;
            int i3 = i0 + 3;
            tris.Add(i0); tris.Add(i2); tris.Add(i1);
            tris.Add(i2); tris.Add(i3); tris.Add(i1);
        }

        var mesh = mf.sharedMesh;
        if (mesh == null)
        {
            mesh = new Mesh { name = "MountainsMesh" };
            mf.sharedMesh = mesh;
        }
        mesh.Clear();
        mesh.SetVertices(verts);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

        transform.localScale = new Vector3(1f, 1f, thickness);

        if (mr != null && mr.sharedMaterial != null)
        {
            var mat = mr.sharedMaterial;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            try { mat.color = color; } catch { }
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

    private void RemoveColliderIfAny()
    {
        var col = GetComponent<Collider>();
        if (col != null)
        {
            if (Application.isPlaying) Destroy(col);
            else DestroyImmediate(col);
        }
    }
}
