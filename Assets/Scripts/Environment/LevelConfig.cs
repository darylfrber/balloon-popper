using UnityEngine;

[CreateAssetMenu(menuName = "BalloonBuster/LevelConfig")] 
public class LevelConfig : ScriptableObject
{
    public float levelDuration = 60f;
    public float spawnInterval = 1.2f;
    public BalloonType[] balloonTypes;
}

[System.Serializable]
public class BalloonType
{
    public Balloon balloonPrefab;
    public int weight = 1;
    public int scoreValue = 10;
}
