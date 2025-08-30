// 8/30/2025 AI-Tag
// This was created with the help of Assistant, a Unity Artificial Intelligence product.

using System;
using UnityEngine;

public class BalloonSpawner : MonoBehaviour
{
    public GameObject balloonPrefab; // Verwijs naar je ballon-prefab
    public float spawnInterval = 1f; // Tijd tussen spawns
    public Vector2 spawnXRange = new Vector2(-8f, 8f);
    public Vector2 spawnZRange = new Vector2(0f, 25f); // Diepte voor 3D effect

    private float timer;
    private bool isSpawning = false;

    public void Begin(LevelConfig config)
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;
        isSpawning = true;
        spawnInterval = config.spawnInterval; // Stel spawn-interval in vanuit LevelConfig
        Debug.Log("Balloon spawning started.");
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
        float xPosition = UnityEngine.Random.Range(spawnXRange.x, spawnXRange.y); // Willekeurige X-positie
        float zPosition = UnityEngine.Random.Range(spawnZRange.x, spawnZRange.y); // Willekeurige Z-positie
        Vector3 spawnPosition = new Vector3(xPosition, -5f, zPosition); // Spawn onderaan, variërende diepte
        var clone = Instantiate(balloonPrefab, spawnPosition, Quaternion.identity);
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
                    Color t = r.gameObject.name == "String" ? new Color(0.1f,0.1f,0.1f) : c;
                    try { mat.color = t; } catch {}
                    if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", t);
                    if (mat.HasProperty("_Color")) mat.SetColor("_Color", t);
                }
            }
        }
    }
}
