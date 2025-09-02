// 8/30/2025 AI-Tag
// This was created with the help of Assistant, a Unity Artificial Intelligence product.

using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    public void StartGame()
    {
        // Use scene index instead of name for more reliable loading
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        int gameSceneIndex = currentSceneIndex; // For now, reload the same scene
        
        Debug.Log($"Loading scene with index: {gameSceneIndex}");
        SceneManager.LoadScene(gameSceneIndex);
    }

    public void QuitGame()
    {
        Application.Quit(); // Sluit de applicatie
        Debug.Log("Game Quit"); // Alleen zichtbaar in de editor
    }
}
