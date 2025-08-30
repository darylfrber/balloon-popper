using UnityEngine;

namespace BalloonBuster.Core
{
    [CreateAssetMenu(menuName = "BalloonBuster/BalloonType", fileName = "BalloonType")] 
    public class BalloonTypeSO : ScriptableObject
    {
        public string id;
        public float baseRiseSpeed = 1f;
        public float size = 1f;
        public int scoreValue = 10;
        public Color color = Color.red;
    }
}
