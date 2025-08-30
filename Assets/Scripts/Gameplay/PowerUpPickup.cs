// 8/30/2025 AI-Tag
// This was created with the help of Assistant, a Unity Artificial Intelligence product.

using System;
using UnityEngine;

public class PowerUpPickup : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            GameManager.Instance.ActivateDoublePoints(10f); // Voorbeeldfunctie in GameManager
            Destroy(gameObject); // Vernietig de power-up na oppakken
        }
    }
}
