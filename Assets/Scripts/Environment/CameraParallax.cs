using UnityEngine;

// Adds a subtle parallax effect to the camera based on mouse/touch input plus a gentle sway.
[DefaultExecutionOrder(-150)]
public class CameraParallax : MonoBehaviour
{
    [Tooltip("Maximum horizontal offset from the base camera position.")]
    public float maxOffsetX = 1.0f;
    [Tooltip("Maximum vertical offset from the base camera position.")]
    public float maxOffsetY = 0.5f;
    [Tooltip("How fast the camera eases to the target offset.")]
    public float lerpSpeed = 2.0f;

    [Tooltip("Amplitude of gentle idle sway.")]
    public float swayAmplitude = 0.1f;
    [Tooltip("Frequency of gentle idle sway (Hz).")]
    public float swayFrequency = 0.25f;

    private Vector3 basePos;
    private Vector3 velocity;

    private void Start()
    {
        basePos = transform.position;
    }

    private void OnEnable()
    {
        basePos = transform.position;
    }

    private void Update()
    {
        Vector2 norm = ReadPointer01(); // (0..1, 0..1)
        // Center around 0: (-0.5..0.5)
        Vector2 centered = norm - new Vector2(0.5f, 0.5f);

        float targetX = Mathf.Clamp(centered.x * 2f * maxOffsetX, -maxOffsetX, maxOffsetX);
        float targetY = Mathf.Clamp(centered.y * 2f * maxOffsetY, -maxOffsetY, maxOffsetY);

        // Gentle idle sway
        float sway = Mathf.Sin(Time.time * Mathf.PI * 2f * Mathf.Max(0.0001f, swayFrequency)) * swayAmplitude;

        Vector3 target = new Vector3(basePos.x + targetX, basePos.y + targetY + sway, basePos.z);
        transform.position = Vector3.Lerp(transform.position, target, Mathf.Clamp01(Time.deltaTime * lerpSpeed));
    }

    private Vector2 ReadPointer01()
    {
        // Mouse or first touch position normalized to screen (0..1)
#if ENABLE_INPUT_SYSTEM
        var mouse = UnityEngine.InputSystem.Mouse.current;
        if (mouse != null)
        {
            var p = mouse.position.ReadValue();
            return new Vector2(p.x / Mathf.Max(1, Screen.width), p.y / Mathf.Max(1, Screen.height));
        }
        var ts = UnityEngine.InputSystem.Touchscreen.current;
        if (ts != null && ts.touches.Count > 0)
        {
            var p = ts.touches[0].position.ReadValue();
            return new Vector2(p.x / Mathf.Max(1, Screen.width), p.y / Mathf.Max(1, Screen.height));
        }
#else
        if (Input.touchCount > 0)
        {
            var p = Input.GetTouch(0).position;
            return new Vector2(p.x / Mathf.Max(1, Screen.width), p.y / Mathf.Max(1, Screen.height));
        }
        else
        {
            var p = Input.mousePosition;
            return new Vector2(p.x / Mathf.Max(1, Screen.width), p.y / Mathf.Max(1, Screen.height));
        }
#endif
        return new Vector2(0.5f, 0.5f);
    }
}
