using UnityEngine;

public class PowerUpSpawner : MonoBehaviour
{
    [Tooltip("Power-up prefab (optional). If null, a PowerUp with default visual is created at runtime.")]
    public GameObject powerUpPrefab;

    [Tooltip("Min seconds between spawn attempts.")]
    public float minInterval = 12f;

    [Tooltip("Max seconds between spawn attempts.")]
    public float maxInterval = 20f;

    [Tooltip("Horizontal X range for spawning.")]
    public Vector2 spawnXRange = new Vector2(-8f, 8f);

    [Tooltip("Depth Z range in front of camera.")]
    public Vector2 spawnZRange = new Vector2(5f, 20f);

    [Tooltip("Spawn height (Y)")]
    public float spawnY = 0.5f;

    private float nextSpawnTimer;
    private bool isRunning;

    private void OnEnable()
    {
        ScheduleNext();
        isRunning = true;
    }

    public void Stop()
    {
        isRunning = false;
    }

    private void Update()
    {
        if (!isRunning) return;
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;

        // Ensure only one active at a time
        if (Object.FindAnyObjectByType<PowerUp>() != null)
        {
            return; // wait until current is collected or expired
        }

        nextSpawnTimer -= Time.deltaTime;
        if (nextSpawnTimer <= 0f)
        {
            SpawnPowerUp();
            ScheduleNext();
        }
    }

    private void ScheduleNext()
    {
        nextSpawnTimer = Random.Range(minInterval, maxInterval);
    }

    private void SpawnPowerUp()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;
        float x = Random.Range(spawnXRange.x, spawnXRange.y);
        float z = Random.Range(spawnZRange.x, spawnZRange.y);
        Vector3 pos = new Vector3(x, spawnY, z);

        GameObject go;
        if (powerUpPrefab != null)
        {
            go = Instantiate(powerUpPrefab, pos, Quaternion.identity);
            var pu = go.GetComponent<PowerUp>();
            if (pu == null) go.AddComponent<PowerUp>();
        }
        else
        {
            go = new GameObject("PowerUp");
            go.transform.position = pos;
            go.AddComponent<PowerUp>();
        }
        go.SetActive(true);
    }
}
