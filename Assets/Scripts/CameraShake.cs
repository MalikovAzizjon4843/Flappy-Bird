using UnityEngine;

// Kamerani qisqa vaqt silkitadi (urilganda). Komponent kerak bo'lganda Main Camera'ga o'zi qo'shiladi —
// sahnada qo'lda sozlash shart emas. Vaqt unscaled: pauza yoki timeScale o'zgarsa ham oxiriga yetadi.
public class CameraShake : MonoBehaviour
{
    private Vector3 basePosition;
    private float duration;
    private float magnitude;
    private float elapsed;
    private bool shaking;

    public static void Shake(float duration, float magnitude)
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            return;
        }

        CameraShake shaker = cam.GetComponent<CameraShake>();
        if (shaker == null)
        {
            shaker = cam.gameObject.AddComponent<CameraShake>();
        }
        shaker.Begin(duration, magnitude);
    }

    void Begin(float newDuration, float newMagnitude)
    {
        // Silkinish ustiga yangi silkinish kelsa, asl joy saqlanib qoladi
        if (!shaking)
        {
            basePosition = transform.localPosition;
        }
        duration = newDuration;
        magnitude = newMagnitude;
        elapsed = 0f;
        shaking = true;
    }

    void LateUpdate()
    {
        if (!shaking)
        {
            return;
        }

        elapsed += Time.unscaledDeltaTime;
        if (elapsed >= duration)
        {
            transform.localPosition = basePosition;
            shaking = false;
            return;
        }

        // Oxiriga qarab so'nadigan tasodifiy siljish (z o'zgarmaydi)
        float strength = magnitude * (1f - elapsed / duration);
        Vector2 offset = Random.insideUnitCircle * strength;
        transform.localPosition = basePosition + new Vector3(offset.x, offset.y, 0f);
    }
}
