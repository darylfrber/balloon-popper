using UnityEngine;
using System.Collections;

namespace BalloonBuster.Core
{
    public class BalloonSpawner : MonoBehaviour
    {
        [SerializeField] private Balloon balloonPrefab;
        [SerializeField] private Vector2 spawnXRange = new Vector2(-3f, 3f);
        [SerializeField] private Vector2 spawnYStartRange = new Vector2(-5f, -4f);
        [SerializeField] private float defaultSpawnInterval = 1f;

        private Coroutine spawnRoutine;
        private LevelConfigSO config;

        public void Configure(LevelConfigSO levelConfig)
        {
            config = levelConfig;
        }

        public void Begin()
        {
            if (spawnRoutine != null) StopCoroutine(spawnRoutine);
            spawnRoutine = StartCoroutine(SpawnLoop());
        }

        public void Stop()
        {
            if (spawnRoutine != null) StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }

        private IEnumerator SpawnLoop()
        {
            while (true)
            {
                SpawnBalloon();
                float interval = config != null ? config.spawnInterval : defaultSpawnInterval;
                yield return new WaitForSeconds(interval);
            }
        }

        private void SpawnBalloon()
        {
            var x = UnityEngine.Random.Range(spawnXRange.x, spawnXRange.y);
            var y = UnityEngine.Random.Range(spawnYStartRange.x, spawnYStartRange.y);
            var pos = new Vector3(x, y, 0f);
            Balloon b = Instantiate(balloonPrefab, pos, Quaternion.identity);
            if (config != null)
            {
                b.Configure(config.GetRandomBalloonType());
            }
        }
    }
}
