// 8/30/2025 AI-Tag
// This was created with the help of Assistant, a Unity Artificial Intelligence product.

using System;
using UnityEngine;

public class BalloonSpawner : MonoBehaviour
{
    public GameObject balloonPrefab; // Verwijs naar je ballon-prefab
    public float spawnInterval = 1f; // Tijd tussen spawns
    // Breder speelveld over het scherm; X-range wordt bij start automatisch aangepast aan de camera
    public Vector2 spawnXRange = new Vector2(-9f, 9f);
    // Iets dichter bij de camera zodat de ballonnen meer naar voren komen
    public Vector2 spawnZRange = new Vector2(-1f, 22f); // Diepte voor 3D effect

    private float timer;
    private bool isSpawning = false;

    public void Begin(LevelConfig config)
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;
        // Pas spawn-breedte aan op basis van de actuele camera zodat ballonnen het scherm beter vullen
        FitSpawnWidthToCamera();
        isSpawning = true;
        spawnInterval = config.spawnInterval; // Stel spawn-interval in vanuit LevelConfig
        Debug.Log("Balloon spawning started.");
    }

    // Past de horizontale spawn-range aan zodat ballonnen bijna tot aan de schermranden kunnen spawnen
    private void FitSpawnWidthToCamera()
    {
        var cam = Camera.main;
        if (cam == null) return;
        // Gebruik een representatieve diepte dicht bij de speler om te garanderen dat spawns zichtbaar zijn
        float representativeZ = Mathf.Lerp(spawnZRange.x, spawnZRange.y, 0.25f);
        float d = representativeZ - cam.transform.position.z; // afstand langs camera-forward
        d = Mathf.Max(1f, d);
        float halfWidth = Mathf.Tan(cam.fieldOfView * Mathf.Deg2Rad * 0.5f) * d * cam.aspect;
        float margin = 0.4f; // kleine veiligheidsmarge
        spawnXRange = new Vector2(-halfWidth + margin, halfWidth - margin);
    }

    public void Stop()
    {
        isSpawning = false;
        Debug.Log("Balloon spawning stopped.");
    }

    void Update()
    {
        if (!isSpawning) return;
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;

        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            SpawnBalloon();
            timer = 0f;
        }
    }

    void SpawnBalloon()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;
        if (!balloonPrefab)
        {
            Debug.LogWarning("BalloonSpawner: balloonPrefab is missing or destroyed. Skipping spawn.");
            return;
        }
        float zPosition = UnityEngine.Random.Range(spawnZRange.x, spawnZRange.y); // Willekeurige Z-positie
        float xPosition;
        var cam = Camera.main;
        if (cam != null)
        {
            // Bereken zichtbare halve breedte op deze diepte en spawn binnen de randen met een kleine marge
            float d = Mathf.Max(1f, zPosition - cam.transform.position.z);
            float halfW = Mathf.Tan(cam.fieldOfView * Mathf.Deg2Rad * 0.5f) * d * cam.aspect;
            float margin = 0.4f;
            xPosition = UnityEngine.Random.Range(-halfW + margin, halfW - margin);
        }
        else
        {
            xPosition = UnityEngine.Random.Range(spawnXRange.x, spawnXRange.y); // Fallback als er geen camera is
        }
        Vector3 spawnPosition = new Vector3(xPosition, -5f, zPosition); // Spawn onderaan, variërende diepte
        var clone = Instantiate(balloonPrefab, spawnPosition, Quaternion.identity);

        // Depth-aware scaling and speed: near (low Z) = larger/faster, far (high Z) = smaller/slower
        float t = Mathf.InverseLerp(spawnZRange.x, spawnZRange.y, zPosition); // 0 near -> 1 far
        float scaleMul = Mathf.Lerp(1.35f, 0.85f, t);
        clone.transform.localScale = clone.transform.localScale * scaleMul;
        var balloonComp = clone.GetComponent<Balloon>();
        if (balloonComp != null)
        {
            balloonComp.riseSpeed *= Mathf.Lerp(1.2f, 0.9f, t);
        }

        clone.SetActive(true);
        // Randomize color slightly for variety (apply to all parts)
        var renderers = clone.GetComponentsInChildren<MeshRenderer>(true);
        if (renderers != null && renderers.Length > 0)
        {
            Color c = UnityEngine.Random.ColorHSV(0f, 1f, 0.7f, 1f, 0.8f, 1f);
            foreach (var r in renderers)
            {
                if (r != null)
                {
                    // ensure unique material instance to avoid global change
                    var mat = r.material;
                    if (!mat) continue;
                    Color tcol = r.gameObject.name == "String" ? new Color(0.1f,0.1f,0.1f) : c;
                    try { mat.color = tcol; } catch {}
                    if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", tcol);
                    if (mat.HasProperty("_Color")) mat.SetColor("_Color", tcol);
                }
            }
        }
    }
}
