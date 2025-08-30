using UnityEngine;

public class PowerUpDropper : MonoBehaviour
{
    [Range(0f,1f)] public float dropChance = 0.1f;
    public PowerUpPickup powerUpPrefab; // ensure PowerUpPickup script exists

    public void TryDrop(Vector3 position){ if(powerUpPrefab==null) return; if(UnityEngine.Random.value <= dropChance){ Instantiate(powerUpPrefab, position, Quaternion.identity); } }
}
