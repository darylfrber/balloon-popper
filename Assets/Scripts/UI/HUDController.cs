// 8/30/2025 AI-Tag
// This was created with the help of Assistant, a Unity Artificial Intelligence product.

using System;
using UnityEngine;
using UnityEngine.UI;

public class HUDController : MonoBehaviour
{
    public Text scoreText;
    public Text powerUpText;
    public Text highScoreText;

    private void Update()
    {
        var gm = GameManager.Instance;
        if (gm != null)
        {
            if (scoreText != null)
                scoreText.text = $"Score: {gm.Score}";
            if (highScoreText != null)
                highScoreText.text = $"High: {gm.HighScore}";
        }
        if (powerUpText != null)
        {
            if (gm != null && gm.DoublePointsActive)
            {
                int remain = Mathf.CeilToInt(gm.DoublePointsRemaining);
                powerUpText.text = $"x2 Dubbele punten: {remain}s";
            }
            else
            {
                powerUpText.text = string.Empty;
            }
        }
    }
}
