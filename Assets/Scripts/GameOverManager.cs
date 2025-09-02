// 8/30/2025 AI-Tag
// This was created with the help of Assistant, a Unity Artificial Intelligence product.

using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverManager : MonoBehaviour
{
    public void RestartGame()
    {
        // Restart current scene
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        Debug.Log($"Restarting scene with index: {currentSceneIndex}");
        SceneManager.LoadScene(currentSceneIndex);
    }

    public void ReturnToMenu()
    {
        // For now, restart the current scene - later we can add a separate menu scene
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        Debug.Log($"Returning to menu (scene index: {currentSceneIndex})");
        SceneManager.LoadScene(currentSceneIndex);
    }
}
