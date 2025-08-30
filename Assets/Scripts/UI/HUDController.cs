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
        scoreText.text = "Score: " + GameManager.Instance.Score; // Toon de score in de HUD
    }
}
