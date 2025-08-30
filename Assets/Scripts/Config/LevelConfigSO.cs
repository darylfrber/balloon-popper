using UnityEngine;

namespace BalloonBuster.Core
{
    [CreateAssetMenu(menuName = "BalloonBuster/LevelConfig", fileName = "LevelConfig")] 
    public class LevelConfigSO : ScriptableObject
    {
        public float levelDuration = 30f;
        public float spawnInterval = 1f;
        public BalloonTypeSO[] balloonTypes;
        public AnimationCurve balloonTypeWeights; // optional weighting curve by index

        public BalloonTypeSO GetRandomBalloonType()
        {
            if (balloonTypes == null || balloonTypes.Length == 0) return null;
            if (balloonTypeWeights == null || balloonTypeWeights.keys == null || balloonTypeWeights.keys.Length == 0)
            {
                int idx = UnityEngine.Random.Range(0, balloonTypes.Length);
                return balloonTypes[idx];
            }
            // Weighted by curve value
            float total = 0f;
            float[] weights = new float[balloonTypes.Length];
            for (int i = 0; i < balloonTypes.Length; i++)
            {
                float w = Mathf.Max(0.0001f, balloonTypeWeights.Evaluate((float)i));
                weights[i] = w;
                total += w;
            }
            float r = UnityEngine.Random.value * total;
            for (int i = 0; i < weights.Length; i++)
            {
                r -= weights[i];
                if (r <= 0f) return balloonTypes[i];
            }
            return balloonTypes[balloonTypes.Length - 1];
        }
    }
}
