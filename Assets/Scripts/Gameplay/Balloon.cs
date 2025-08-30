// 8/30/2025 AI-Tag
// This was created with the help of Assistant, a Unity Artificial Intelligence product.

using System;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Balloon : MonoBehaviour
{
    public int scoreValue = 10; // Punten die deze ballon oplevert
    public float riseSpeed = 1.5f; // Snelheid waarmee de ballon stijgt
    public float swayAmplitude = 0.3f; // Kleine zijwaartse beweging voor levensecht effect
    public float swayFrequency = 1.2f;

    private float swayOffset;
    private Vector3 startPos;

    private void Start()
    {
        startPos = transform.position;
        swayOffset = UnityEngine.Random.Range(0f, 1000f);
        // Klein formaatverschil om diepte te accentueren wanneer op Z dieper gespawned wordt
        float depthScale = Mathf.Clamp01(1f - (transform.position.z / 30f));
        transform.localScale *= Mathf.Lerp(0.5f, 1.2f, depthScale);
    }

    private void Update()
    {
        // Stop gedrag als het spel voorbij is
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;

        // Laat de ballon omhoog bewegen
        transform.Translate(Vector3.up * (riseSpeed * Time.deltaTime), Space.World);

        // Sway links-rechts
        float sway = Mathf.Sin((Time.time + swayOffset) * swayFrequency) * swayAmplitude * Time.deltaTime;
        transform.Translate(new Vector3(sway, 0f, 0f), Space.World);

        // Verliesconditie: ballon verlaat het scherm bovenaan
        if (transform.position.y > 12f)
        {
            // Alleen game over als deze niet gepopt is
            if (!popped && GameManager.Instance != null)
            {
                GameManager.Instance.EndGame();
            }
            Destroy(gameObject);
        }
    }

    private bool popped = false;

    public void Pop()
    {
        if (popped) return;
        popped = true;
        // Voeg hier effecten toe, zoals een geluid of een animatie
        Debug.Log("Balloon popped!");
        GameManager.Instance.AddScore(scoreValue); // Voeg punten toe aan de score
        Destroy(gameObject); // Vernietig de ballon
    }

    private void OnMouseDown()
    {
        // Laat de ballon "poppen" wanneer erop wordt geklikt
        Pop();
    }
}
