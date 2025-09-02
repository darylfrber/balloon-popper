using System.Collections.Generic;
using UnityEngine;

// Generates a simple wavy low-poly hills mesh strip that spans the view width.
// It is designed to sit in front of the BackdropQuad and behind gameplay.
[ExecuteAlways]
public class HillsMesh : MonoBehaviour
{
    [Tooltip("Camera reference for sizing/placement.")]
    public Camera targetCamera;

    [Tooltip("Distance from camera along forward direction.")]
    public float distanceFromCamera = 52f;

    [Tooltip("World width of the hills strip (auto if 0: fitted to camera width at distance).")]
    public float width = 0f;

    [Tooltip("Base Y position of the hills.")]
    public float baseY = -2.5f;

    [Tooltip("Peak amplitude of the hills (height above base).")]
    public float amplitude = 3.0f;

    [Tooltip("Number of large waves across the width.")]
    public int waves = 3;

    [Tooltip("Horizontal subdivisions (more = smoother silhouette).")]
    public int segments = 64;

    [Tooltip("Thickness (depth on Z) of the strip mesh.")]
    public float thickness = 2f;

    [Tooltip("Main color of the hills.")]
    public Color color = new Color(0.25f, 0.6f, 0.85f, 1f);

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
        if (segments < 8) segments = 8;
        if (waves < 1) waves = 1;
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
        // Keep fitted in case aspect/FOV changes
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

        // Place in front of camera
        Vector3 camPos = targetCamera.transform.position;
        Vector3 camFwd = targetCamera.transform.forward;
        transform.position = camPos + camFwd * distanceFromCamera;
        transform.rotation = Quaternion.LookRotation(-camFwd, Vector3.up);

        // Determine width to fit the camera horizontally at that distance if width == 0
        float fovRad = targetCamera.fieldOfView * Mathf.Deg2Rad;
        float heightAtDist = 2f * Mathf.Tan(fovRad * 0.5f) * distanceFromCamera;
        float widthAtDist = heightAtDist * targetCamera.aspect;
        float w = (width <= 0.001f) ? widthAtDist * 1.1f : width; // little margin

        // Build a 2D polyline for the top silhouette
        int vertsPerRow = segments + 1;
        var verts = new List<Vector3>(vertsPerRow * 2);
        var uvs = new List<Vector2>(vertsPerRow * 2);
        var tris = new List<int>(segments * 6);

        float half = w * 0.5f;
        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;         // 0..1 across width
            float x = Mathf.Lerp(-half, half, t);  // local x

            // Compose a few waves for a more natural shape
            float y = baseY
                      + Mathf.Sin(t * Mathf.PI * 2f * waves + _offset) * amplitude
                      + Mathf.Sin(t * Mathf.PI * 6f + _offset * 2f) * amplitude * 0.25f;

            // Top vertex
            verts.Add(new Vector3(x, y, 0));
            uvs.Add(new Vector2(t, 1));
            // Bottom vertex (extend downward so it fills below)
            verts.Add(new Vector3(x, y - (amplitude + 6f), 0));
            uvs.Add(new Vector2(t, 0));
        }

        // Triangles (quads between pairs of columns)
        for (int i = 0; i < segments; i++)
        {
            int i0 = i * 2;
            int i1 = i0 + 1;
            int i2 = i0 + 2;
            int i3 = i0 + 3;
            // two triangles per quad
            tris.Add(i0); tris.Add(i2); tris.Add(i1);
            tris.Add(i2); tris.Add(i3); tris.Add(i1);
        }

        var mesh = mf.sharedMesh;
        if (mesh == null)
        {
            mesh = new Mesh();
            mesh.name = "HillsMesh";
            mf.sharedMesh = mesh;
        }
        mesh.Clear();
        mesh.SetVertices(verts);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

        // Apply slight thickness by scaling in local Z via transform (billboard toward camera)
        transform.localScale = new Vector3(1f, 1f, thickness);

        // Ensure material color is applied (runtime safety)
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
