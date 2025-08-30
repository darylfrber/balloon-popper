using UnityEngine;

// Fits a backdrop quad to fill the camera view at a certain distance.
[ExecuteAlways]
public class BackgroundFitter : MonoBehaviour
{
    public Camera targetCamera;
    [Tooltip("Distance from the camera along its forward direction.")]
    public float distanceFromCamera = 60f;

    private void OnEnable()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        RemoveColliderIfAny();
        FitNow();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying) FitNow();
    }
#endif

    private void LateUpdate()
    {
        // In case aspect/FOV changes (resize), keep it fitted in editor and play.
        FitNow();
    }

    public void FitNow()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        if (targetCamera == null) return;

        // Position in front of camera
        Vector3 camPos = targetCamera.transform.position;
        Vector3 camFwd = targetCamera.transform.forward;
        transform.position = camPos + camFwd * distanceFromCamera;
        transform.rotation = Quaternion.LookRotation(-camFwd, Vector3.up);

        float fovRad = targetCamera.fieldOfView * Mathf.Deg2Rad;
        float height = 2f * Mathf.Tan(fovRad * 0.5f) * distanceFromCamera;
        float width = height * targetCamera.aspect;
        // Quad is 1x1 in local units, so scale directly
        transform.localScale = new Vector3(width, height, 1f);
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
