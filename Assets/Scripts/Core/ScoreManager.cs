using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    public int Score { get; private set; }
    public int HighScore { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void AddScore(int amount)
    {
        Score += amount;
        if (Score > HighScore)
        {
            HighScore = Score;
        }
    }

    public void ResetScore()
    {
        Score = 0;
    }
}
