using UnityEngine;

// Centralized click/touch handler that raycasts into the 3D world and pops balloons.
// Works with both the legacy Input Manager and the new Input System (via scripting define ENABLE_INPUT_SYSTEM).
[DefaultExecutionOrder(-200)]
public class ClickToPop : MonoBehaviour
{
    private Camera cam;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;
    }

    private void Update()
    {
#if ENABLE_INPUT_SYSTEM
        HandleInputSystem();
#else
        HandleLegacyInput();
#endif
    }

#if ENABLE_INPUT_SYSTEM
    private void HandleInputSystem()
    {
        // Use UnityEngine.InputSystem if available
        // We avoid hard dependency when define isn't present
        var mouse = UnityEngine.InputSystem.Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            RayFromScreenAndPop(mouse.position.ReadValue());
        }
        var ts = UnityEngine.InputSystem.Touchscreen.current;
        if (ts != null)
        {
            foreach (var touch in ts.touches)
            {
                if (touch.press.wasPressedThisFrame)
                {
                    RayFromScreenAndPop(touch.position.ReadValue());
                }
            }
        }
    }
#else
    private void HandleLegacyInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            RayFromScreenAndPop(Input.mousePosition);
        }
        // Simple single-touch support
        if (Input.touchCount > 0)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                var t = Input.GetTouch(i);
                if (t.phase == TouchPhase.Began)
                {
                    RayFromScreenAndPop(t.position);
                }
            }
        }
    }
#endif

    private void RayFromScreenAndPop(Vector2 screenPos)
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;
        Ray ray = cam.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit, 200f))
        {
            var balloon = hit.collider.GetComponentInParent<Balloon>();
            if (balloon != null)
            {
                balloon.Pop();
            }
        }
    }
}
