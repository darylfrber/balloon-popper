// 8/30/2025 AI-Tag
// This was created with the help of Assistant, a Unity Artificial Intelligence product.

using System;
using UnityEngine;
using UnityEngine.UI;

public class HUDController : MonoBehaviour
{
    public Text scoreText;

    private void Update()
    {
        var gm = GameManager.Instance;
        if (gm != null && scoreText != null)
        {
            scoreText.text = $"Score: {gm.Score}\nHigh: {gm.HighScore}"; // Toon score en hoogste score
        }
    }
}
